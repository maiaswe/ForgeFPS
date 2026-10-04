namespace ForgeFPS.Application;

using Domain;

/// <summary>
/// Orchestrates a full optimization session: preflight -> preview -> consent -> apply -> verify.
/// This is the service the UI calls; it never bypasses consent or the transaction engine.
/// </summary>
public class OptimizationSessionService(
    OptimizationCatalog catalog,
    OptimizationTransactionEngine engine,
    DiagnosticService diagnostics,
    ILogger logger,
    ITransactionLog transactionLog,
    IFileSystem fileSystem,
    IRegistryRegistry registry,
    IPowerPlanProvider powerPlans,
    IHistoryStore historyStore)
{
    /// <summary>
    /// Runs detection for all (or selected) actions without modifying anything.
    /// Returns per-action current state and compatibility.
    /// </summary>
    public async Task<IReadOnlyList<ActionInspection>> InspectAsync(
        IReadOnlyList<string>? actionIds = null,
        CancellationToken ct = default)
    {
        var hardware = await diagnostics.DetectHardwareAsync(ct);
        var games = await diagnostics.DetectGamesAsync(ct);
        var context = BuildContext("INSPECT", isConsented: false, hardware, games);

        var actions = SelectActions(actionIds);
        var inspections = new List<ActionInspection>();

        foreach (var action in actions)
        {
            ct.ThrowIfCancellationRequested();
            var compatibility = await action.CheckCompatibilityAsync(context);
            var state = await action.DetectCurrentStateAsync(context);

            inspections.Add(new ActionInspection(
                action.Id,
                action.DisplayName,
                compatibility,
                state));
        }

        return inspections;
    }

    /// <summary>
    /// Runs a full simulation: detect + preview for each action, no modifications.
    /// </summary>
    public async Task<IReadOnlyList<ActionPlan>> SimulateAsync(
        IReadOnlyList<string>? actionIds = null,
        CancellationToken ct = default)
    {
        var hardware = await diagnostics.DetectHardwareAsync(ct);
        var games = await diagnostics.DetectGamesAsync(ct);
        var context = new SimulationContext(logger, fileSystem, registry, powerPlans, hardware, games);

        var actions = SelectActions(actionIds);
        var plans = new List<ActionPlan>();

        foreach (var action in actions)
        {
            ct.ThrowIfCancellationRequested();
            var compatibility = await action.CheckCompatibilityAsync(context);
            if (!compatibility.IsCompatible)
            {
                plans.Add(new ActionPlan(action.Id, action.DisplayName, compatibility, null));
                continue;
            }
            var preview = await action.PreviewAsync(context);
            plans.Add(new ActionPlan(action.Id, action.DisplayName, compatibility, preview));
        }

        logger.Information("Simulação concluída para " + plans.Count + " ações.");
        return plans;
    }

    /// <summary>
    /// Applies the selected actions after consent. Runs the full transaction engine
    /// (preview -> apply -> verify -> rollback on failure) and records history.
    /// </summary>
    public async Task<TransactionOutcome> ApplyWithConsentAsync(
        IReadOnlyList<string> actionIds,
        CancellationToken ct = default)
    {
        if (actionIds.Count == 0)
            throw new ArgumentException("Nenhuma ação selecionada.", nameof(actionIds));

        var actions = SelectActions(actionIds);
        var incompatible = new List<string>();
        foreach (var action in actions)
        {
            var compat = await action.CheckCompatibilityAsync(BuildInspectContext());
            if (!compat.IsCompatible)
                incompatible.Add(action.DisplayName);
        }
        if (incompatible.Count > 0)
            throw new InvalidOperationException("Ações incompatíveis: " + string.Join(", ", incompatible));

        var sessionId = await engine.BeginSessionAsync();
        var hardware = await diagnostics.DetectHardwareAsync(ct);
        var games = await diagnostics.DetectGamesAsync(ct);

        var backupRoot = BuildBackupRoot(sessionId, fileSystem);
        var context = BuildContext(sessionId, isConsented: true, hardware, games, backupRoot);

        var outcome = await engine.ExecuteTransactionAsync(actions, context, ct);
        await engine.EndSessionAsync(sessionId);

        historyStore.Record(sessionId, outcome);
        logger.Information("Sessão " + sessionId + " finalizada com estado " + outcome.State + ".");

        return outcome;
    }

    private IDetectionContext BuildInspectContext()
    {
        var hardware = diagnostics.DetectHardwareAsync().Result;
        var games = diagnostics.DetectGamesAsync().Result;
        return BuildContext("INSPECT", false, hardware, games);
    }

    private IApplyContext BuildContext(
        string sessionId,
        bool isConsented,
        HardwareSnapshot hardware,
        IReadOnlyList<GameInstance> games,
        string? backupRoot = null)
    {
        return new ApplyContext(
            sessionId,
            backupRoot ?? string.Empty,
            transactionLog,
            logger,
            fileSystem,
            registry,
            powerPlans,
            hardware,
            games,
            isConsented);
    }

    private IReadOnlyList<IOptimizationAction> SelectActions(IReadOnlyList<string>? actionIds)
    {
        if (actionIds is null || actionIds.Count == 0)
            return catalog.All;

        var selected = new List<IOptimizationAction>();
        foreach (var id in actionIds)
        {
            var action = catalog.Get(id)
                ?? throw new ArgumentException($"Ação desconhecida: {id}", nameof(actionIds));
            selected.Add(action);
        }
        return selected;
    }

    private static string BuildBackupRoot(string sessionId, IFileSystem fs)
    {
        var root = fs.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForgeFPS", "backups", sessionId);
        if (!fs.DirectoryExists(root))
            fs.CreateDirectory(root);
        return root;
    }
}

/// <summary>
/// Read-only inspection of a single action.
/// </summary>
public record ActionInspection(
    string ActionId,
    string DisplayName,
    CompatibilityResult Compatibility,
    CurrentState CurrentState);

/// <summary>
/// Simulation plan for a single action (preview included when compatible).
/// </summary>
public record ActionPlan(
    string ActionId,
    string DisplayName,
    CompatibilityResult Compatibility,
    ActionPreview? Preview);
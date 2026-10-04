namespace ForgeFPS.Infrastructure.Windows.Actions;

using Domain;

/// <summary>
/// Sets high-performance GPU preference for a specific executable.
/// Uses documented registry key per executable. Only applicable when
/// more than one GPU is present.
/// </summary>
public class GpuPreferenceAction(
    IRegistryRegistry registry,
    ILogger logger,
    string? targetExe = null) : IOptimizationAction
{
    // Documented key: HKCU\Software\Microsoft\DirectX\UserGpuPreferences
    // Value name = full path to exe, value data = "GpuPreference=2;" (high performance)
    private const string GpuPrefsKey = @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences";

    private readonly string? _targetExe = targetExe;

    private IRegistryRegistry Registry { get; } = registry;
    private ILogger Logger { get; } = logger;

    public string Id => "GPU_PREFERENCE_HIGH_PERFORMANCE";
    public int SchemaVersion => 1;
    public string DisplayName => "Preferência de GPU de alto desempenho";
    public string Description => "Define a GPU de alto desempenho como preferida para o executável do jogo selecionado. Aplica-se somente quando há mais de uma GPU.";
    public OptimizationCategory Category => OptimizationCategory.Performance;
    public RiskLevel Risk => RiskLevel.Low;
    public bool RequiresAdministrator => false;
    public bool RequiresRestart => false;
    public bool IsTemporary => false;
    public IReadOnlyList<string> CompatibilityTags => ["MultiGpu", "Windows10", "Windows11"];
    public string? ExpectedBenefit => "Garante que o jogo use a GPU dedicada em sistemas com GPU integrada + dedicada.";

    public Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context)
    {
        var gpuCount = context.Hardware.Gpus.Count;
        if (gpuCount < 2)
            return Task.FromResult(new CompatibilityResult(
                false, CompatibilityStatus.Unsupported, ["MultiGpu"], ["MultiGpu"],
                ["Apenas uma GPU detectada — a preferência de GPU não é aplicável."]));

        if (string.IsNullOrEmpty(_targetExe))
            return Task.FromResult(new CompatibilityResult(
                false, CompatibilityStatus.Unknown, ["TargetExe"], ["TargetExe"],
                ["Nenhum executável de jogo selecionado."]));

        return Task.FromResult(new CompatibilityResult(true, CompatibilityStatus.Supported, ["MultiGpu", "TargetExe"], [], []));
    }

    public Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context)
    {
        var snapshots = new List<ActionSnapshot>();
        var observations = new List<string>();

        if (!string.IsNullOrEmpty(_targetExe))
        {
            var current = Registry.GetValue(GpuPrefsKey, _targetExe!);
            snapshots.Add(new ActionSnapshot($"GpuPreference:{_targetExe}", GpuPrefsKey, _targetExe, "string", current, current is not null, null, null, 0, DateTimeOffset.UtcNow));
            observations.Add(current is not null
                ? $"Preferência atual para {_targetExe}: {current}"
                : $"Nenhuma preferência de GPU definida para {_targetExe}.");
        }

        return Task.FromResult(new CurrentState(snapshots, observations, true));
    }

    public Task<ActionPreview> PreviewAsync(IApplyContext context)
    {
        var state = DetectCurrentStateAsync(context).Result;
        var notes = new List<string>
        {
            $"Será definido 'GpuPreference=2;' (alto desempenho) para: {_targetExe}",
            "Altera apenas o Registro do usuário (HKCU), sem necessidade de admin.",
            "Reversível: o valor anterior será restaurado no rollback."
        };

        return Task.FromResult(new ActionPreview(Id, state.Snapshots, [], [], notes, true));
    }

    public Task<ActionResult> ApplyAsync(IApplyContext context)
    {
        if (!context.IsConsented)
            return Task.FromResult(new ActionResult(Id, false, [], "Consentimento não concedido.", []));

        if (string.IsNullOrEmpty(_targetExe))
            return Task.FromResult(new ActionResult(Id, false, [], "Nenhum executável selecionado.", []));

        try
        {
            var before = Registry.GetValue(GpuPrefsKey, _targetExe!);
            var snapshots = new List<ActionSnapshot>
            {
                new($"GpuPreference:{_targetExe}", GpuPrefsKey, _targetExe, "string", before, before is not null, null, null, 0, DateTimeOffset.UtcNow)
            };

            Registry.SetValue(GpuPrefsKey, _targetExe!, "GpuPreference=2;");
            Logger.Information($"Preferência de GPU de alto desempenho definida para {_targetExe}.");

            return Task.FromResult(new ActionResult(Id, true, snapshots, null, []));
        }
        catch (Exception ex)
        {
            Logger.Error($"Falha ao definir preferência de GPU: {ex.Message}");
            return Task.FromResult(new ActionResult(Id, false, [], ex.Message, []));
        }
    }

    public Task<VerificationResult> VerifyAsync(IApplyContext context)
    {
        var current = Registry.GetValue(GpuPrefsKey, _targetExe ?? string.Empty);
        var verified = current as string == "GpuPreference=2;";

        return Task.FromResult(new VerificationResult(
            Id, verified,
            verified ? [] : [$"Valor atual: '{current}', esperado 'GpuPreference=2;'."],
            "GpuPreference=2;"));
    }

    public Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context)
    {
        try
        {
            foreach (var snapshot in snapshots)
            {
                if (snapshot.WasPresent)
                    Registry.SetValue(GpuPrefsKey, snapshot.RegistryValueName!, snapshot.OldValue!);
                else
                    Registry.DeleteValue(GpuPrefsKey, snapshot.RegistryValueName!);
            }

            Logger.Information("Preferência de GPU restaurada.");
            return Task.FromResult(new RollbackResult(Id, true, [], null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RollbackResult(Id, false, [$"Falha: {ex.Message}"], ex.Message));
        }
    }
}
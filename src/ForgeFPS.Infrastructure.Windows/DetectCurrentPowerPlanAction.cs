namespace ForgeFPS.Infrastructure.Windows;

using Domain;

/// <summary>
/// Demo action that detects and reports the current active power plan.
/// Does NOT modify any system settings.
/// </summary>
public class DetectCurrentPowerPlanAction(IPowerPlanProvider powerPlans, ILogger logger) : IOptimizationAction
{
    public string Id => "DETECT_POWER_PLAN";
    public int SchemaVersion => 1;
    public string DisplayName => "Detectar plano de energia atual";
    public string Description => "Identifica o plano de energia atualmente ativo sem realizar alterações.";
    public OptimizationCategory Category => OptimizationCategory.Energy;
    public RiskLevel Risk => RiskLevel.Low;
    public bool RequiresAdministrator => false;
    public bool RequiresRestart => false;
    public bool IsTemporary => true;
    public IReadOnlyList<string> CompatibilityTags => ["Windows"];
    public string? ExpectedBenefit => "Informação sobre configuração de energia atual para referência do usuário.";

    public Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context)
    {
        return Task.FromResult(new CompatibilityResult(true, CompatibilityStatus.Supported, ["Windows"], [], ["Esta ação é compatível com todas as versões do Windows suportadas."]));
    }

    public Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context)
    {
        var activePlan = powerPlans.GetActivePlan();
        var snapshots = new List<ActionSnapshot>();

        if (activePlan is not null)
        {
            snapshots.Add(new ActionSnapshot("PowerPlan.Active", null, "PowerPlanGuid", "string", activePlan.Guid, true, null, null, 0, DateTimeOffset.UtcNow));
        }

        var observations = activePlan is not null
            ? new List<string> { $"Plano ativo: {activePlan.Name}", $"GUID: {activePlan.Guid}" }
            : new List<string> { "Nenhum plano de energia identificado." };

        return Task.FromResult(new CurrentState(snapshots, observations, true));
    }

    public Task<ActionPreview> PreviewAsync(IApplyContext context)
    {
        // This action is read-only - no preview needed beyond detection.
        var state = DetectCurrentStateAsync((IDetectionContext)context).Result;

        return Task.FromResult(new ActionPreview(Id, state.Snapshots, [], [], ["Esta é uma ação apenas de leitura. Nenhuma alteração será realizada."], true));
    }

    public Task<ActionResult> ApplyAsync(IApplyContext context)
    {
        // No-op for read-only action
        logger.Information("Action " + Id + ": Read-only detection complete.");
        return Task.FromResult(new ActionResult(Id, true, [], null, []));
    }

    public Task<VerificationResult> VerifyAsync(IApplyContext context)
    {
        // Nothing to verify for read-only action
        return Task.FromResult(new VerificationResult(Id, true, [], "Read-only action - no verification needed."));
    }

    public Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context)
    {
        // No rollback needed for read-only action
        return Task.FromResult(new RollbackResult(Id, true, [], null));
    }
}
namespace ForgeFPS.Infrastructure.Windows.Actions;

using Domain;

/// <summary>
/// Creates a dedicated "ForgeFPS Gaming" power plan by duplicating an existing
/// high-performance plan. Never deletes user plans. Fully reversible.
/// </summary>
public class CreateGamingPowerPlanAction(
    IPowerPlanProvider powerPlans,
    ISnapshotService snapshotService,
    ILogger logger) : IOptimizationAction
{
    private const string GamingPlanName = "ForgeFPS Gaming";
    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    public string Id => "CREATE_GAMING_POWER_PLAN";
    public int SchemaVersion => 1;
    public string DisplayName => "Criar plano de energia ForgeFPS Gaming";
    public string Description => "Duplica um plano de alto desempenho existente para uso em sessões de jogo. Não destrói planos existentes.";
    public OptimizationCategory Category => OptimizationCategory.Energy;
    public RiskLevel Risk => RiskLevel.Low;
    public bool RequiresAdministrator => true;
    public bool RequiresRestart => false;
    public bool IsTemporary => false;
    public IReadOnlyList<string> CompatibilityTags => ["Windows"];
    public string? ExpectedBenefit => "Mantém CPU em frequência mais alta durante jogos, reduzindo quedas de desempenho.";

    public Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context)
    {
        var existing = powerPlans.ListPlans().FirstOrDefault(p => p.Guid == HighPerformanceGuid || p.IsRecommendedForHighPerformance);
        if (existing is null)
            return Task.FromResult(new CompatibilityResult(false, CompatibilityStatus.Unsupported, ["Windows"], ["PowerPlan:HighPerformance"], ["Nenhum plano de alto desempenho base encontrado."]));

        return Task.FromResult(new CompatibilityResult(true, CompatibilityStatus.Supported, ["Windows"], [], []));
    }

    public Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context)
    {
        var plans = powerPlans.ListPlans();
        var gaming = plans.FirstOrDefault(p => p.Name == GamingPlanName);
        var active = plans.FirstOrDefault(p => p.IsActive);

        var observations = new List<string>
        {
            gaming is not null
                ? $"Plano '{GamingPlanName}' já existe (GUID: {gaming.Guid})."
                : $"Plano '{GamingPlanName}' ainda não existe.",
            active is not null
                ? $"Plano ativo atual: {active.Name} (GUID: {active.Guid})."
                : "Não foi possível identificar o plano ativo."
        };

        var snapshots = new List<ActionSnapshot>
        {
            new("PowerPlan.Active", null, "ActivePlanGuid", "string", active?.Guid, active is not null, null, null, 0, DateTimeOffset.UtcNow)
        };

        return Task.FromResult(new CurrentState(snapshots, observations, true));
    }

    public Task<ActionPreview> PreviewAsync(IApplyContext context)
    {
        var state = DetectCurrentStateAsync(context).Result;
        var notes = new List<string>
        {
            $"Um novo plano '{GamingPlanName}' será criado duplicando o plano de alto desempenho.",
            "O plano atual do usuário NÃO será removido nem modificado.",
            "Requer elevação de administrador para criar/ativar planos de energia."
        };

        return Task.FromResult(new ActionPreview(Id, state.Snapshots, [], [], notes, true));
    }

    public Task<ActionResult> ApplyAsync(IApplyContext context)
    {
        if (!context.IsConsented)
            return Task.FromResult(new ActionResult(Id, false, [], "Consentimento não concedido.", []));

        try
        {
            var plans = powerPlans.ListPlans();
            var existingGaming = plans.FirstOrDefault(p => p.Name == GamingPlanName);
            string gamingGuid;

            if (existingGaming is not null)
            {
                // Idempotent: plan already exists
                gamingGuid = existingGaming.Guid;
                logger.Information($"Plano '{GamingPlanName}' já existe, reutilizando GUID {gamingGuid}.");
            }
            else
            {
                var basePlan = plans.FirstOrDefault(p => p.Guid == HighPerformanceGuid)
                               ?? plans.FirstOrDefault(p => p.IsRecommendedForHighPerformance);
                if (basePlan is null)
                    return Task.FromResult(new ActionResult(Id, false, [], "Plano base de alto desempenho não encontrado.", []));

                powerPlans.DuplicatePlan(basePlan.Guid, GamingPlanName, out gamingGuid);
                logger.Information($"Plano '{GamingPlanName}' criado com GUID {gamingGuid}.");
            }

            // Activate the gaming plan
            powerPlans.ActivatePlan(gamingGuid);

            var snapshots = new List<ActionSnapshot>
            {
                new("PowerPlan.Active", null, "ActivePlanGuid", "string", gamingGuid, true, null, null, 0, DateTimeOffset.UtcNow)
            };

            return Task.FromResult(new ActionResult(Id, true, snapshots, null, []));
        }
        catch (Exception ex)
        {
            logger.Error($"Falha ao criar plano de energia: {ex.Message}");
            return Task.FromResult(new ActionResult(Id, false, [], ex.Message, []));
        }
    }

    public Task<VerificationResult> VerifyAsync(IApplyContext context)
    {
        var active = powerPlans.GetActivePlan();
        var verified = active is not null && (active.Name == GamingPlanName || powerPlans.ListPlans().Any(p => p.Name == GamingPlanName && p.Guid == active.Guid));

        return Task.FromResult(new VerificationResult(
            Id,
            verified,
            verified ? [] : [$"Plano ativo é '{active?.Name}', esperado '{GamingPlanName}'."],
            $"Plano '{GamingPlanName}' ativo"));
    }

    public Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context)
    {
        try
        {
            // Restore the previously active plan
            var previousGuid = snapshots.FirstOrDefault(s => s.Key == "PowerPlan.Active")?.OldValue as string;
            if (!string.IsNullOrEmpty(previousGuid))
            {
                powerPlans.ActivatePlan(previousGuid);
                logger.Information($"Plano anterior restaurado: {previousGuid}");
            }

            // Note: we do NOT delete the ForgeFPS Gaming plan on rollback - user may want to keep it.
            return Task.FromResult(new RollbackResult(Id, true, [], null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RollbackResult(Id, false, [$"Falha ao restaurar plano: {ex.Message}"], ex.Message));
        }
    }
}
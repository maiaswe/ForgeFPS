namespace ForgeFPS.Infrastructure.Windows;

using Domain;

/// <summary>
/// Mock power plan provider for testing and demonstration.
/// </summary>
public class MockPowerPlanProvider : IPowerPlanProvider
{
    private static readonly PowerPlan[] _knownPlans =
    [
        new PowerPlan(
            Guid.NewGuid().ToString("N"),
            "Equilibrado",
            "Configuração recomendada para uso geral.",
            false,
            true,
            "SYSTEM"),
        new PowerPlan(
            Guid.NewGuid().ToString("N"),
            "Alto desempenho",
            "Máximo desempenho do processador.",
            true,
            false,
            "SYSTEM"),
        new PowerPlan(
            Guid.NewGuid().ToString("N"),
            "Econômico",
            "Economia de energia.",
            false,
            false,
            "SYSTEM"),
    ];

    public IReadOnlyList<PowerPlan> ListPlans() => _knownPlans;

    public PowerPlan? GetActivePlan() => _knownPlans.First(p => p.IsActive);

    public string ExportPlan(string planGuid, string targetPath)
    {
        var plan = ListPlans().FirstOrDefault(p => p.Guid == planGuid);
        if (plan is null) throw new ArgumentException($"Plan not found: {planGuid}");
        System.IO.File.WriteAllText(targetPath, $"PowerPlan:{planGuid}:{plan.Name}");
        return targetPath;
    }

    public void ImportPlan(string path, out string importedGuid)
    {
        importedGuid = Guid.NewGuid().ToString("N");
        // Mock import - in real implementation, this would parse and register
    }

    public void ActivatePlan(string planGuid)
    {
        // Mock - in real implementation, this would call powercfg.exe
    }

    public string DuplicatePlan(string existingPlanGuid, string newPlanName, out string newPlanGuid)
    {
        newPlanGuid = Guid.NewGuid().ToString("N");
        // Mock duplicate - in real implementation, this would call powercfg.exe
        return newPlanGuid;
    }
}
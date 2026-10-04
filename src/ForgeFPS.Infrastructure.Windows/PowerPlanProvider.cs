namespace ForgeFPS.Infrastructure.Windows;

using Domain;
using System.Diagnostics;

/// <summary>
/// Real power plan provider using powercfg.exe - no admin required for listing/reading,
/// admin required for duplicating/activating plans.
/// </summary>
public class PowerPlanProvider(ILogger logger) : IPowerPlanProvider
{
    private const string PowercfgPath = "powercfg.exe";

    public IReadOnlyList<PowerPlan> ListPlans()
    {
        var output = RunPowercfg("/list");
        var plans = new List<PowerPlan>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Format: Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced) *
            if (!line.Contains("Power Scheme GUID:", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(new[] { ": " }, 2, StringSplitOptions.None);
            if (parts.Length < 2) continue;

            var rest = parts[1];
            var guid = rest[..36].Trim();
            var nameStart = rest.IndexOf('(');
            var nameEnd = rest.IndexOf(')');
            var name = nameStart >= 0 && nameEnd > nameStart
                ? rest[(nameStart + 1)..nameEnd]
                : guid;
            var isActive = rest.Contains('*');

            plans.Add(new PowerPlan(
                Guid: guid,
                Name: name,
                Description: string.Empty,
                IsRecommendedForHighPerformance: name.Contains("High", StringComparison.OrdinalIgnoreCase) ||
                                                  name.Contains("Ultimate", StringComparison.OrdinalIgnoreCase),
                IsActive: isActive,
                Owner: "SYSTEM"));
        }

        return plans;
    }

    public PowerPlan? GetActivePlan()
    {
        var plans = ListPlans();
        return plans.FirstOrDefault(p => p.IsActive);
    }

    public string ExportPlan(string planGuid, string targetPath)
    {
        RunPowercfg($"/export \"{targetPath}\" {planGuid}");
        return targetPath;
    }

    public void ImportPlan(string path, out string importedGuid)
    {
        var output = RunPowercfg($"/import \"{path}\"");
        importedGuid = string.Empty;
        // powercfg /import does not output the GUID; caller must list to find it
        // For now, parse from output if available
        if (output.Contains("Power Scheme GUID:", StringComparison.OrdinalIgnoreCase))
        {
            var idx = output.IndexOf("Power Scheme GUID:", StringComparison.OrdinalIgnoreCase);
            var rest = output[(idx + 18)..].Trim();
            importedGuid = rest[..Math.Min(36, rest.Length)];
        }
    }

    public void ActivatePlan(string planGuid)
    {
        RunPowercfg($"/setactive {planGuid}");
    }

    public string DuplicatePlan(string existingPlanGuid, string newPlanName, out string newPlanGuid)
    {
        var output = RunPowercfg($"/duplicatescheme {existingPlanGuid}");
        // Output: Power Scheme GUID: <new-guid>  (New plan name)
        var idx = output.IndexOf("Power Scheme GUID:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            throw new InvalidOperationException("Failed to duplicate power plan: no GUID in output.");

        var rest = output[(idx + 18)..].Trim();
        newPlanGuid = rest[..36].Trim();

        // Rename the duplicated plan
        RunPowercfg($"/changename {newPlanGuid} \"{newPlanName}\" \"Plano criado pelo ForgeFPS\"");

        return newPlanGuid;
    }

    private string RunPowercfg(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = PowercfgPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
            throw new InvalidOperationException("Failed to start powercfg.exe");

        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit(10000);

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            logger.Error($"powercfg {arguments} failed with exit code {process.ExitCode}: {stderr}");
            throw new InvalidOperationException($"powercfg failed: {stderr}");
        }

        return stdout;
    }
}
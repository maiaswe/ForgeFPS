namespace ForgeFPS.Application;

using Domain;

/// <summary>
/// Diagnostic service for hardware and system information.
/// </summary>
public class DiagnosticService(
    ILogger logger,
    IFileSystem fileSystem)
{
    public Task<HardwareSnapshot> DetectHardwareAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder for real hardware detection
        // In production, this would use WMI, PowerShell, or native APIs
        logger.Information($"Hardware detection placeholder executed.");

        return Task.FromResult(new HardwareSnapshot(
            new OsInfo("Microsoft Windows", Environment.OSVersion.Version.ToString(), Environment.OSVersion.Version.Build.ToString(), "Professional", Environment.Is64BitOperatingSystem ? "x64" : "x86", false, false),
            [new CpuInfo("CPU-placeholder", 8, 16, 3600)],
            [new GpuInfo("GPU-placeholder", "1.0.0", DateTimeOffset.Now, 8L * 1024 * 1024 * 1024, true)],
            16L * 1024 * 1024 * 1024,
            [new MonitorInfo("Monitor-placeholder", 1920, 1080, 60, "Primary")],
            [new DiskInfo("C:", "System", "NTFS", 500L * 1024 * 1024 * 1024, 100L * 1024 * 1024 * 1024, "Fixed")],
            Environment.MachineName,
            false,
            false,
            DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<GameInstance>> DetectGamesAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder for game detection
        logger.Information($"Game detection placeholder executed.");
        return Task.FromResult<IReadOnlyList<GameInstance>>([]);
    }
}
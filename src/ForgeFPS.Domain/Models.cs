namespace ForgeFPS.Domain;

/// <summary>
/// Category of optimization action.
/// </summary>
public enum OptimizationCategory
{
    Energy,
    GameMode,
    Performance,
    Privacy,
    Maintenance,
    Display,
    Network,
    Process,
    GameSpecific,
}

/// <summary>
/// Risk level for an optimization action.
/// </summary>
public enum RiskLevel
{
    Low,
    Moderate,
    Experimental,
}

/// <summary>
/// Compatibility status of an action on the current machine.
/// </summary>
public enum CompatibilityStatus
{
    Supported,
    Unsupported,
    AlreadyConfigured,
    RequiresRestart,
    RequiresElevation,
    Unknown,
}

/// <summary>
/// Session state in the optimization transaction lifecycle.
/// </summary>
public enum SessionState
{
    Planned,
    AwaitingConsent,
    Applying,
    Applied,
    PartiallyApplied,
    Failed,
    RollingBack,
    RolledBack,
    RollbackFailed,
}

/// <summary>
/// Hardware component type.
/// </summary>
public enum HardwareComponentType
{
    Cpu,
    Gpu,
    Memory,
    Disk,
    Monitor,
    Motherboard,
    NetworkAdapter,
    Sound,
}

/// <summary>
/// OS Edition string (e.g. Windows 11 Pro).
/// </summary>
public record OsInfo(
    string Name,
    string Version,
    string BuildNumber,
    string Edition,
    string Architecture,
    bool IsEnterprise,
    bool IsLTSB);

/// <summary>
/// CPU information from detection.
/// </summary>
public record CpuInfo(
    string Name,
    int PhysicalCores,
    int LogicalCores,
    int MaxFrequencyMHz);

/// <summary>
/// GPU information from detection.
/// </summary>
public record GpuInfo(
    string Name,
    string DriverVersion,
    DateTimeOffset DriverDate,
    long DedicatedVideoMemoryBytes,
    bool HasHardwareAcceleration)
{
    /// <summary>
    /// Dedicated video memory in megabytes.
    /// </summary>
    public long DedicatedVideoMemoryMb => DedicatedVideoMemoryBytes / (1024 * 1024);
}

/// <summary>
/// Monitor information.
/// </summary>
public record MonitorInfo(
    string Name,
    int ResolutionWidth,
    int ResolutionHeight,
    int RefreshRateHz,
    string PrimaryMonitorName);

/// <summary>
/// Disk drive info.
/// </summary>
public record DiskInfo(
    string DriveLetter,
    string VolumeName,
    string FileSystem,
    long TotalBytes,
    long FreeBytes,
    string DriveType);

/// <summary>
/// Complete hardware snapshot for a machine.
/// </summary>
public record HardwareSnapshot(
    OsInfo Os,
    IReadOnlyList<CpuInfo> Cpus,
    IReadOnlyList<GpuInfo> Gpus,
    long TotalMemoryBytes,
    IReadOnlyList<MonitorInfo> Monitors,
    IReadOnlyList<DiskInfo> Disks,
    string MachineName,
    bool IsOnBattery,
    bool IsVirtualMachine,
    DateTimeOffset CapturedAtUtc);

/// <summary>
/// Detected game instance.
/// </summary>
public record GameInstance(
    string Name,
    string ExecutablePath,
    string? LibraryPath,
    string? SteamAppId,
    string? InstalledVersion,
    bool IsPrimary,
    bool IsOnlineProtected);

/// <summary>
/// Represents a single optimization action definition in the catalog.
/// </summary>
public record OptimizationAction(
    string Id,
    int SchemaVersion,
    string DisplayName,
    string Description,
    OptimizationCategory Category,
    RiskLevel Risk,
    bool RequiresAdministrator,
    bool RequiresRestart,
    bool IsTemporary,
    IReadOnlyList<string> CompatibilityTags,
    string? ExpectedBenefit,
    string? HelpUrl,
    string? DocumentationSource,
    DateTimeOffset ValidatedAtUtc);

/// <summary>
/// Snapshot of a system value before modification.
/// </summary>
public record ActionSnapshot(
    string Key,
    string? RegistryPath,
    string? RegistryValueName,
    string? ValueType,
    object? OldValue,
    bool WasPresent,
    string? FilePath,
    string? FileHash,
    long FileSize,
    DateTimeOffset FileTimestamp);

/// <summary>
/// Preview of changes before application.
/// </summary>
public record ActionPreview(
    string ActionId,
    IReadOnlyList<ActionSnapshot> BeforeSnapshots,
    IReadOnlyList<ActionSnapshot> AfterSnapshots,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Notes,
    bool IsConsistent);

/// <summary>
/// Result after attempting to apply an action.
/// </summary>
public record ActionResult(
    string ActionId,
    bool Success,
    IReadOnlyList<ActionSnapshot> Snapshots,
    string? FailureReason,
    IReadOnlyList<string> VerificationFailures);

/// <summary>
/// Verification result after application.
/// </summary>
public record VerificationResult(
    string ActionId,
    bool Verified,
    IReadOnlyList<string> Discrepancies,
    string? ExpectedValueSummary);

/// <summary>
/// Rollback result after reverting.
/// </summary>
public record RollbackResult(
    string ActionId,
    bool Success,
    IReadOnlyList<string> Warnings,
    string? FailureReason);

/// <summary>
/// Single benchmark data point from a frame capture.
/// </summary>
public record FrameSample(
    long FrameIndex,
    double TimestampMs,
    double FrameTimeMs,
    double CpuTimeMs,
    double GpuTimeMs,
    bool IsLateFrame);

/// <summary>
/// Summary metrics from a benchmark run.
/// </summary>
public record BenchmarkMetrics(
    double AverageFps,
    double OnePercentLowFps,
    double PointOnePercentLowFps,
    double AverageFrameTimeMs,
    double P95FrameTimeMs,
    double P99FrameTimeMs,
    double StdDevFrameTimeMs,
    int TotalFrames,
    double DurationMs,
    IReadOnlyList<FrameSample> Samples);

/// <summary>
/// A benchmark capture with before/after metadata.
/// </summary>
public record BenchmarkRun(
    string Id,
    string Title,
    string GameName,
    string? ProfileUsed,
    bool? IsBeforeCapture,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    BenchmarkMetrics Metrics,
    IReadOnlyList<string> Notes);
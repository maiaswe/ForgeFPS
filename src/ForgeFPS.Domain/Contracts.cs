namespace ForgeFPS.Domain;

/// <summary>
/// Compatibility result for an action on current hardware.
/// </summary>
public record CompatibilityResult(
    bool IsCompatible,
    CompatibilityStatus Status,
    IReadOnlyList<string> RequiredTags,
    IReadOnlyList<string> MissingTags,
    IReadOnlyList<string> Notes);

/// <summary>
/// Current system state for a specific action.
/// </summary>
public record CurrentState(
    IReadOnlyList<ActionSnapshot> Snapshots,
    IReadOnlyList<string> Observations,
    bool IsReady);

/// <summary>
/// IOptimizationAction defines the contract each catalog item must implement.
/// </summary>
public interface IOptimizationAction
{
    string Id { get; }
    int SchemaVersion { get; }
    string DisplayName { get; }
    string Description { get; }
    OptimizationCategory Category { get; }
    RiskLevel Risk { get; }
    bool RequiresAdministrator { get; }
    bool RequiresRestart { get; }
    bool IsTemporary { get; }
    IReadOnlyList<string> CompatibilityTags { get; }
    string? ExpectedBenefit { get; }

    Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context);
    Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context);
    Task<ActionPreview> PreviewAsync(IApplyContext context);
    Task<ActionResult> ApplyAsync(IApplyContext context);
    Task<VerificationResult> VerifyAsync(IApplyContext context);
    Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context);
}

/// <summary>
/// Context injected into detection/apply/verify/rollback phases.
/// </summary>
public interface IDetectionContext
{
    IReadOnlyList<GameInstance> DetectedGames { get; }
    HardwareSnapshot Hardware { get; }
    ILogger Logger { get; }
    IFileSystem FileSystem { get; }
    IRegistryRegistry Registry { get; }
    IPowerPlanProvider PowerPlans { get; }
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Apply context adds mutable operations and consent checks.
/// </summary>
public interface IApplyContext : IDetectionContext
{
    string SessionId { get; }
    string BackupRoot { get; }
    ITransactionLog TransactionLog { get; }
    bool IsConsented { get; }
    void AddToTransactionLog(string actionId, string message);
}

/// <summary>
/// Simple interface for registry access used by actions.
/// </summary>
public interface IRegistryRegistry
{
    object? GetValue(string keyPath, string valueName);
    void SetValue(string keyPath, string valueName, object value);
    void DeleteValue(string keyPath, string valueName);
    bool Exists(string keyPath, string? valueName = null);
}

/// <summary>
/// Simple interface for file system access used by actions.
/// </summary>
public interface IFileSystem
{
    bool Exists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
    void Copy(string sourcePath, string destinationPath, bool overwrite = false);
    void Move(string sourcePath, string destinationPath);
    void Delete(string path);
    string GetFileName(string path);
    string? GetExtension(string path);
    string GetFullPath(string path);
    string GetDirectoryName(string path);
    string Combine(params string[] parts);
    string[] ReadLines(string path);
    void WriteLines(string path, string[] lines);
    string[] EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    DirectoryEntry? GetDirectoryInfo(string path);
    FileEntry? GetFileInfo(string path);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    string GetCurrentDirectory();
    void CreateDirectory(string path);
    string GetTempFileName();
    string GetTempPath();
    long GetFileSize(string path);
    string GetSha256Hash(string path);
    void ReplaceFile(string sourcePath, string targetPath);
}

public record DirectoryEntry(string Path, DateTimeOffset LastWriteTime, long Length);
public record FileEntry(string Path, DateTimeOffset LastWriteTime, long Length);

/// <summary>
/// Interface for power plan operations.
/// </summary>
public interface IPowerPlanProvider
{
    IReadOnlyList<PowerPlan> ListPlans();
    PowerPlan? GetActivePlan();
    string ExportPlan(string planGuid, string targetPath);
    void ImportPlan(string path, out string importedGuid);
    void ActivatePlan(string planGuid);
    string DuplicatePlan(string existingPlanGuid, string newPlanName, out string newPlanGuid);
}

public record PowerPlan(
    string Guid,
    string Name,
    string Description,
    bool IsRecommendedForHighPerformance,
    bool IsActive,
    string Owner);

/// <summary>
/// Transaction log for recording session events.
/// </summary>
public interface ITransactionLog
{
    void Log(string sessionId, string actionId, string message);
    void LogMetadata(string sessionId, string key, string value);
    IReadOnlyList<string> GetSessionLogEntries(string sessionId);
}

/// <summary>
/// ILogger abstraction for the app.
/// </summary>
public interface ILogger
{
    void Information(string message);
    void Warning(string message);
    void Error(string message);
    void Debug(string message);
}
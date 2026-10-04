namespace ForgeFPS.Domain;

/// <summary>
/// Registry value types supported by Windows Registry API.
/// </summary>
public enum RegistryValueType
{
    None = 0,
    String = 1,
    ExpandString = 2,
    Binary = 3,
    DWord = 4,
    MultiString = 5,
    QWord = 6,
    Unknown = 99,
}

/// <summary>
/// Extended snapshot for registry values with proper type tracking.
/// </summary>
public record RegistrySnapshot(
    string Key,
    string RegistryPath,
    string? ValueName,
    RegistryValueType ValueType,
    object? OldValue,
    bool WasPresent);

/// <summary>
/// Snapshot for file system operations.
/// </summary>
public record FileSnapshot(
    string Key,
    string OriginalPath,
    string BackupPath,
    string Sha256Hash,
    long FileSize,
    DateTimeOffset FileTimestamp);

/// <summary>
/// Result of a snapshot/restore operation.
/// </summary>
public record SnapshotResult(
    bool Success,
    string? Error,
    IReadOnlyList<RegistrySnapshot> RegistrySnapshots,
    IReadOnlyList<FileSnapshot> FileSnapshots);

/// <summary>
/// Service for capturing and restoring system state snapshots.
/// </summary>
public interface ISnapshotService
{
    Task<SnapshotResult> CaptureAsync(IReadOnlyList<string> registryPaths, IReadOnlyList<string> filePaths, CancellationToken ct = default);
    Task<SnapshotResult> RestoreAsync(IReadOnlyList<RegistrySnapshot> registrySnapshots, IReadOnlyList<FileSnapshot> fileSnapshots, CancellationToken ct = default);
}

/// <summary>
/// Path safety validator - prevents path traversal and symlink attacks.
/// </summary>
public interface IPathValidator
{
    bool IsValid(string path, string allowedRoot);
    string Canonicalize(string path, string allowedRoot);
    bool IsSafeSymlink(string path);
}

/// <summary>
/// Atomic file writer that writes to temp, flushes, then replaces.
/// </summary>
public interface IAtomicWriter
{
    void WriteAtomic(string targetPath, string contents);
    void WriteAtomic(string targetPath, byte[] data);
    void ReplaceAtomic(string sourcePath, string targetPath);
}
namespace ForgeFPS.Infrastructure.Windows;

using Domain;
using System.Security.AccessControl;
using System.Security.Principal;

/// <summary>
/// Captures and restores registry/file snapshots with full type tracking.
/// </summary>
public class SnapshotService(IRegistryRegistry registry, IFileSystem fileSystem, ILogger logger) : ISnapshotService
{
    public async Task<SnapshotResult> CaptureAsync(
        IReadOnlyList<string> registryPaths,
        IReadOnlyList<string> filePaths,
        CancellationToken ct = default)
    {
        var regSnapshots = new List<RegistrySnapshot>();
        var fileSnapshots = new List<FileSnapshot>();

        foreach (var path in registryPaths)
        {
            ct.ThrowIfCancellationRequested();
            var snapshot = CaptureRegistry(path);
            if (snapshot is not null) regSnapshots.Add(snapshot);
        }

        foreach (var path in filePaths)
        {
            ct.ThrowIfCancellationRequested();
            var snapshot = CaptureFile(path);
            if (snapshot is not null) fileSnapshots.Add(snapshot);
        }

        await Task.CompletedTask;
        return new SnapshotResult(true, null, regSnapshots, fileSnapshots);
    }

    public async Task<SnapshotResult> RestoreAsync(
        IReadOnlyList<RegistrySnapshot> registrySnapshots,
        IReadOnlyList<FileSnapshot> fileSnapshots,
        CancellationToken ct = default)
    {
        var errors = new List<string>();

        // Restore registry in reverse order
        foreach (var snapshot in registrySnapshots.Reverse())
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (!snapshot.WasPresent)
                {
                    registry.DeleteValue(snapshot.RegistryPath, snapshot.ValueName!);
                }
                else
                {
                    registry.SetValue(snapshot.RegistryPath, snapshot.ValueName!, snapshot.OldValue!);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Registry {snapshot.RegistryPath}\\{snapshot.ValueName}: {ex.Message}");
                logger.Error($"Failed to restore registry {snapshot.RegistryPath}: {ex.Message}");
            }
        }

        // Restore files from backup
        foreach (var snapshot in fileSnapshots)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (fileSystem.Exists(snapshot.BackupPath))
                {
                    fileSystem.Copy(snapshot.BackupPath, snapshot.OriginalPath, overwrite: true);
                }
                else
                {
                    logger.Warning($"Backup file missing for {snapshot.OriginalPath}, skipping restore.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"File {snapshot.OriginalPath}: {ex.Message}");
                logger.Error($"Failed to restore file {snapshot.OriginalPath}: {ex.Message}");
            }
        }

        await Task.CompletedTask;
        var error = errors.Count > 0 ? string.Join("; ", errors) : null;
        return new SnapshotResult(errors.Count == 0, error, [], []);
    }

    private RegistrySnapshot? CaptureRegistry(string path)
    {
        try
        {
            // Path format: HKEY_CURRENT_USER\Software\Key\ValueName
            var lastSlash = path.LastIndexOf('\\');
            if (lastSlash < 0) return null;

            var keyPath = path[..lastSlash];
            var valueName = path[(lastSlash + 1)..];

            var exists = registry.Exists(keyPath, valueName);
            if (!exists)
            {
                return new RegistrySnapshot(
                    Key: path,
                    RegistryPath: keyPath,
                    ValueName: valueName,
                    ValueType: RegistryValueType.None,
                    OldValue: null,
                    WasPresent: false);
            }

            var value = registry.GetValue(keyPath, valueName);
            var type = DetectRegistryType(value);

            return new RegistrySnapshot(
                Key: path,
                RegistryPath: keyPath,
                ValueName: valueName,
                ValueType: type,
                OldValue: value,
                WasPresent: true);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to capture registry {path}: {ex.Message}");
            return null;
        }
    }

    private static RegistryValueType DetectRegistryType(object? value)
    {
        return value switch
        {
            null => RegistryValueType.None,
            int => RegistryValueType.DWord,
            long => RegistryValueType.QWord,
            string[] => RegistryValueType.MultiString,
            string => RegistryValueType.String,
            byte[] => RegistryValueType.Binary,
            _ => RegistryValueType.Unknown,
        };
    }

    private FileSnapshot? CaptureFile(string path)
    {
        try
        {
            if (!fileSystem.FileExists(path)) return null;

            var hash = fileSystem.GetSha256Hash(path);
            var size = fileSystem.GetFileSize(path);
            var info = fileSystem.GetFileInfo(path);

            return new FileSnapshot(
                Key: path,
                OriginalPath: path,
                BackupPath: string.Empty, // Backup path assigned by caller
                Sha256Hash: hash,
                FileSize: size,
                FileTimestamp: info?.LastWriteTime ?? DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to capture file {path}: {ex.Message}");
            return null;
        }
    }
}
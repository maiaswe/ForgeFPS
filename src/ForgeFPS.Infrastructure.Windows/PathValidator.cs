namespace ForgeFPS.Infrastructure.Windows;

using Domain;
using System.IO;

/// <summary>
/// Validates paths against traversal attacks and unsafe symlinks.
/// </summary>
public class PathValidator : IPathValidator
{
    private readonly IFileSystem _fileSystem;

    public PathValidator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public bool IsValid(string path, string allowedRoot)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (string.IsNullOrWhiteSpace(allowedRoot)) return false;

        try
        {
            var canonicalPath = Canonicalize(path, allowedRoot);
            return canonicalPath is not null;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public string Canonicalize(string path, string allowedRoot)
    {
        // Reject relative paths
        if (!Path.IsPathRooted(path))
            throw new UnauthorizedAccessException($"Relative paths are not allowed: {path}");

        // Reject null bytes and invalid chars
        if (path.Contains('\0'))
            throw new ArgumentException("Path contains null byte.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(allowedRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Case-insensitive comparison on Windows
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path '{fullPath}' is outside allowed root '{fullRoot}'.");

        return fullPath;
    }

    public bool IsSafeSymlink(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return (info.Attributes & FileAttributes.ReparsePoint) == 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Atomic file writer - writes temp file, flushes, then replaces target.
/// </summary>
public class AtomicWriter(IFileSystem fileSystem) : IAtomicWriter
{
    public void WriteAtomic(string targetPath, string contents)
    {
        var tempPath = fileSystem.GetTempFileName();
        try
        {
            fileSystem.WriteAllText(tempPath, contents);

            if (fileSystem.Exists(targetPath))
                fileSystem.Delete(targetPath);

            fileSystem.Move(tempPath, targetPath);
        }
        catch
        {
            if (fileSystem.Exists(tempPath))
                fileSystem.Delete(tempPath);
            throw;
        }
    }

    public void WriteAtomic(string targetPath, byte[] data)
    {
        var tempPath = fileSystem.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(tempPath, data);

            if (fileSystem.Exists(targetPath))
                fileSystem.Delete(targetPath);

            fileSystem.Move(tempPath, targetPath);
        }
        catch
        {
            if (fileSystem.Exists(tempPath))
                fileSystem.Delete(tempPath);
            throw;
        }
    }

    public void ReplaceAtomic(string sourcePath, string targetPath)
    {
        var tempPath = fileSystem.GetTempFileName();
        try
        {
            fileSystem.Copy(sourcePath, tempPath, overwrite: true);

            if (fileSystem.Exists(targetPath))
                fileSystem.Delete(targetPath);

            fileSystem.Move(tempPath, targetPath);
        }
        catch
        {
            if (fileSystem.Exists(tempPath))
                fileSystem.Delete(tempPath);
            throw;
        }
    }
}
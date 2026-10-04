namespace ForgeFPS.Infrastructure.Windows;

using Domain;

/// <summary>
/// File system implementation using System.IO.
/// </summary>
public class FileSystem : IFileSystem
{
    public bool Exists(string path) => System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
    public bool DirectoryExists(string path) => System.IO.Directory.Exists(path);
    public bool FileExists(string path) => System.IO.File.Exists(path);
    public void CreateDirectory(string path) => System.IO.Directory.CreateDirectory(path);
    public string GetCurrentDirectory() => System.IO.Directory.GetCurrentDirectory();
    public string GetTempFileName() => System.IO.Path.GetTempFileName();
    public string GetTempPath() => System.IO.Path.GetTempPath();
    public string Combine(params string[] parts) => System.IO.Path.Combine(parts);
    public string GetFullPath(string path) => System.IO.Path.GetFullPath(path);
    public string GetFileName(string path) => System.IO.Path.GetFileName(path);
    public string? GetExtension(string path) => System.IO.Path.GetExtension(path);
    public string GetDirectoryName(string path) => System.IO.Path.GetDirectoryName(path) ?? string.Empty;
    public string ReadAllText(string path) => System.IO.File.ReadAllText(path);
    public void WriteAllText(string path, string contents) => System.IO.File.WriteAllText(path, contents);
    public void Copy(string sourcePath, string destinationPath, bool overwrite = false)
        => System.IO.File.Copy(sourcePath, destinationPath, overwrite);
    public void Move(string sourcePath, string destinationPath) => System.IO.File.Move(sourcePath, destinationPath);
    public void Delete(string path)
    {
        if (DirectoryExists(path)) System.IO.Directory.Delete(path, true);
        else if (FileExists(path)) System.IO.File.Delete(path);
    }
    public string[] ReadLines(string path) => System.IO.File.ReadAllLines(path);
    public void WriteLines(string path, string[] lines) => System.IO.File.WriteAllLines(path, lines);
    public string[] EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        => System.IO.Directory.GetFiles(path, searchPattern, searchOption);
    public DirectoryEntry? GetDirectoryInfo(string path)
    {
        if (!DirectoryExists(path)) return null;
        var info = new System.IO.DirectoryInfo(path);
        return new DirectoryEntry(path, info.LastWriteTimeUtc, info.EnumerateFiles().Sum(f => f.Length));
    }
    public FileEntry? GetFileInfo(string path)
    {
        if (!FileExists(path)) return null;
        var info = new System.IO.FileInfo(path);
        return new FileEntry(path, info.LastWriteTimeUtc, info.Length);
    }
    public long GetFileSize(string path) => new System.IO.FileInfo(path).Length;
    public string GetSha256Hash(string path)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = System.IO.File.OpenRead(path);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(sha256.ComputeHash(stream)).ToLowerInvariant();
    }
    public void ReplaceFile(string sourcePath, string targetPath)
    {
        var tempPath = GetTempFileName();
        try
        {
            Copy(sourcePath, tempPath, true);
            if (FileExists(targetPath)) Delete(targetPath);
            Move(tempPath, targetPath);
        }
        catch
        {
            if (FileExists(tempPath)) Delete(tempPath);
            throw;
        }
    }
}
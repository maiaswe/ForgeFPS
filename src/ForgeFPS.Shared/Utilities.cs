namespace ForgeFPS.Shared;

/// <summary>
/// Thread-safe logger adapter.
/// </summary>
public static class LoggerExtensions
{
    public static void LogInformation(this ForgeFPS.Domain.ILogger logger, string message, params object?[] args)
        => logger.Information(string.Format(message, args));

    public static void LogWarning(this ForgeFPS.Domain.ILogger logger, string message, params object?[] args)
        => logger.Warning(string.Format(message, args));

    public static void LogError(this ForgeFPS.Domain.ILogger logger, string message, Exception? ex = null, params object?[] args)
        => logger.Error(ex is not null ? $"{string.Format(message, args)}: {ex.Message}" : string.Format(message, args));
}

/// <summary>
/// Utility methods for path validation.
/// </summary>
public static class PathValidation
{
    public static bool IsSafePath(string path, string allowedRoot)
    {
        if (string.IsNullOrEmpty(path)) return false;
        var normalizedPath = System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
        var normalizedRoot = System.IO.Path.GetFullPath(allowedRoot).TrimEnd(System.IO.Path.DirectorySeparatorChar);
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static string Canonicalize(string path, string allowedRoot)
    {
        var full = System.IO.Path.GetFullPath(path);
        if (!IsSafePath(full, allowedRoot))
            throw new UnauthorizedAccessException($"Path '{full}' is outside allowed root '{allowedRoot}'.");
        return full;
    }
}
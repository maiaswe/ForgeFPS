namespace ForgeFPS.Infrastructure.Windows;

using Domain;

/// <summary>
/// In-memory transaction log for testing.
/// </summary>
public class InMemoryTransactionLog : ITransactionLog
{
    private readonly Dictionary<string, List<string>> _logs = new();

    public void Log(string sessionId, string actionId, string message)
    {
        var key = $"{sessionId}:{actionId}";
        if (!_logs.ContainsKey(key)) _logs[key] = new();
        _logs[key].Add($"{DateTimeOffset.UtcNow:O} - {message}");
    }

    public void LogMetadata(string sessionId, string key, string value)
    {
        Log(sessionId, "metadata", $"{key}={value}");
    }

    public IReadOnlyList<string> GetSessionLogEntries(string sessionId)
    {
        return _logs
            .Where(kvp => kvp.Key.StartsWith($"{sessionId}:"))
            .SelectMany(kvp => kvp.Value)
            .ToList();
    }
}
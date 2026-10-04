namespace ForgeFPS.Shared;

/// <summary>
/// Null logger for environments where logging is not required.
/// </summary>
public class NullLogger : ForgeFPS.Domain.ILogger
{
    public static readonly NullLogger Instance = new();
    private NullLogger() { }
    public void Information(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
    public void Debug(string message) { }
}
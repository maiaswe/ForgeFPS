namespace ForgeFPS.Application;

using Domain;
using Shared;

/// <summary>
/// Read-only context used in "Simulate changes" mode.
/// Detection and preview only — never modifies anything.
/// </summary>
public class SimulationContext(
    ILogger logger,
    IFileSystem fileSystem,
    IRegistryRegistry registry,
    IPowerPlanProvider powerPlans,
    HardwareSnapshot hardware,
    IReadOnlyList<GameInstance> games) : IApplyContext
{
    public string SessionId => "SIMULATION-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
    public string BackupRoot => string.Empty;
    public ITransactionLog TransactionLog => new InMemoryTransactionLogBridge();
    public bool IsConsented => false;
    public IReadOnlyList<GameInstance> DetectedGames => games;
    public HardwareSnapshot Hardware => hardware;
    public ILogger Logger => logger;
    public IFileSystem FileSystem => fileSystem;
    public IRegistryRegistry Registry => registry;
    public IPowerPlanProvider PowerPlans => powerPlans;
    public CancellationToken CancellationToken => CancellationToken.None;

    public void AddToTransactionLog(string actionId, string message) { }

    private class InMemoryTransactionLogBridge : ITransactionLog
    {
        public void Log(string sessionId, string actionId, string message) { }
        public void LogMetadata(string sessionId, string key, string value) { }
        public IReadOnlyList<string> GetSessionLogEntries(string sessionId) => [];
    }
}

/// <summary>
/// Context for real apply operations with consent and backup root.
/// </summary>
public class ApplyContext(
    string sessionId,
    string backupRoot,
    ITransactionLog transactionLog,
    ILogger logger,
    IFileSystem fileSystem,
    IRegistryRegistry registry,
    IPowerPlanProvider powerPlans,
    HardwareSnapshot hardware,
    IReadOnlyList<GameInstance> games,
    bool isConsented) : IApplyContext
{
    public string SessionId { get; } = sessionId;
    public string BackupRoot { get; } = backupRoot;
    public ITransactionLog TransactionLog { get; } = transactionLog;
    public bool IsConsented { get; } = isConsented;
    public IReadOnlyList<GameInstance> DetectedGames { get; } = games;
    public HardwareSnapshot Hardware { get; } = hardware;
    public ILogger Logger { get; } = logger;
    public IFileSystem FileSystem { get; } = fileSystem;
    public IRegistryRegistry Registry { get; } = registry;
    public IPowerPlanProvider PowerPlans { get; } = powerPlans;
    public CancellationToken CancellationToken => CancellationToken.None;

    public void AddToTransactionLog(string actionId, string message) =>
        TransactionLog.Log(SessionId, actionId, message);
}
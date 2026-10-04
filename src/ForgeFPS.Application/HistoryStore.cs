namespace ForgeFPS.Application;

using Domain;

/// <summary>
/// Stores completed transaction outcomes as session history (in-memory for MVP).
/// </summary>
public class HistoryStore : IHistoryStore
{
    private readonly List<HistoryEntry> _entries = [];
    private readonly object _lock = new();

    public void Record(string sessionId, TransactionOutcome outcome)
    {
        lock (_lock)
        {
            _entries.Add(new HistoryEntry(
                sessionId,
                outcome.CompletedAtUtc,
                outcome.State,
                outcome.Outcomes.Select(o => new HistoryActionOutcome(
                    o.ActionId,
                    o.Success,
                    o.VerificationResult?.Verified,
                    o.RollbackResult?.Success)).ToList(),
                outcome.Summary));
        }
    }

    public IReadOnlyList<HistoryEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.OrderByDescending(e => e.CompletedAtUtc).ToList();
        }
    }

    public HistoryEntry? Get(string sessionId)
    {
        lock (_lock)
        {
            return _entries.FirstOrDefault(e => e.SessionId == sessionId);
        }
    }
}

public interface IHistoryStore
{
    void Record(string sessionId, TransactionOutcome outcome);
    IReadOnlyList<HistoryEntry> GetAll();
    HistoryEntry? Get(string sessionId);
}

public record HistoryEntry(
    string SessionId,
    DateTimeOffset CompletedAtUtc,
    SessionState State,
    IReadOnlyList<HistoryActionOutcome> Actions,
    string? Summary);

public record HistoryActionOutcome(
    string ActionId,
    bool ApplySuccess,
    bool? Verified,
    bool? RollbackSuccess);
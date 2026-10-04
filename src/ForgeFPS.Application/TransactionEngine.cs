namespace ForgeFPS.Application;

using Domain;

/// <summary>
/// Engine that orchestrates optimization transactions with safety guarantees.
/// </summary>
public class OptimizationTransactionEngine(
    ILogger logger,
    ITransactionLog transactionLog)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _activeSessionId;

    public bool IsLocked => _activeSessionId is not null;

    public async Task<string> BeginSessionAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _activeSessionId ??= Guid.NewGuid().ToString("N");
            transactionLog.LogMetadata(_activeSessionId!, "session", "started");
            return _activeSessionId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task EndSessionAsync(string sessionId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_activeSessionId == sessionId)
            {
                logger.Information("Session " + sessionId + " ended.");
                transactionLog.LogMetadata(sessionId, "session", "ended");
                _activeSessionId = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Executes a transaction: detect -> preview -> apply -> verify -> (rollback on failure).
    /// </summary>
    public async Task<TransactionOutcome> ExecuteTransactionAsync(
        IReadOnlyList<IOptimizationAction> actions,
        IApplyContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = context.SessionId;
        transactionLog.Log(sessionId, "transaction", $"Starting transaction with {actions.Count} actions.");

        var outcomes = new List<ActionOutcome>();
        var appliedActions = new List<(IOptimizationAction Action, ICollection<ActionSnapshot> Snapshots)>();

        try
        {
            // Phase 1: Detect and preview
            foreach (var action in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var preview = await action.PreviewAsync(context);
                outcomes.Add(new ActionOutcome(action.Id, true, preview, null));
                logger.Information("Previewed action " + action.Id + ".");
            }

            // Phase 2: Apply
            for (var i = 0; i < actions.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var action = actions[i];
                var result = await action.ApplyAsync(context);
                outcomes[i] = new ActionOutcome(action.Id, result.Success, null, result);

                if (!result.Success)
                {
                    logger.Error("Action " + action.Id + " failed: " + result.FailureReason + ".");
                    transactionLog.Log(sessionId, action.Id, $"Failed: {result.FailureReason}");
                    throw new TransactionException($"Action {action.Id} failed: {result.FailureReason}");
                }

                appliedActions.Add((action, result.Snapshots.ToList()));
                transactionLog.Log(sessionId, action.Id, "Applied successfully.");
            }

            // Phase 3: Verify
            for (var i = 0; i < actions.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var action = actions[i];
                var verify = await action.VerifyAsync(context);
                outcomes[i] = outcomes[i] with { VerificationResult = verify };

                if (!verify.Verified)
                {
                    logger.Warning("Action " + action.Id + " verification failed: " + string.Join(", ", verify.Discrepancies) + ".");
                }
            }

            transactionLog.Log(sessionId, "transaction", "Transaction completed successfully.");
            return new TransactionOutcome(
                sessionId,
                SessionState.Applied,
                outcomes,
                null,
                null,
                DateTimeOffset.UtcNow);

        }
        catch (Exception ex) when (ex is not TransactionException)
        {
            logger.Error("Transaction failed: " + ex.Message + ".");
            transactionLog.Log(sessionId, "transaction", "Failed: " + ex.Message);
            return await RollbackAsync(actions, appliedActions, context);
        }
        catch (TransactionException txEx)
        {
            // Action failed - rollback any successfully applied actions first
            if (appliedActions.Count > 0)
            {
                logger.Error(txEx.Message);
                transactionLog.Log(sessionId, "transaction", "Failed: " + txEx.Message);
                return await RollbackAsync(actions, appliedActions, context);
            }
            else
            {
                // No actions applied - just return Failed
                logger.Error(txEx.Message);
                transactionLog.Log(sessionId, "transaction", "Failed: " + txEx.Message);
                return new TransactionOutcome(
                    sessionId,
                    SessionState.Failed,
                    outcomes,
                    txEx,
                    txEx.Message,
                    DateTimeOffset.UtcNow);
            }
        }
    }

    private async Task<TransactionOutcome> RollbackAsync(
        IReadOnlyList<IOptimizationAction> actions,
        List<(IOptimizationAction Action, ICollection<ActionSnapshot> Snapshots)> appliedActions,
        IApplyContext context)
    {
        var sessionId = context.SessionId;
        transactionLog.Log(sessionId, "transaction", "Starting rollback.");

        var rollbackOutcomes = new List<ActionOutcome>();
        var successCount = 0;

        for (var i = appliedActions.Count - 1; i >= 0; i--)
        {
            var (action, snapshots) = appliedActions[i];
            var rollback = await action.RollbackAsync(snapshots, context);
            rollbackOutcomes.Insert(0, new ActionOutcome(action.Id, rollback.Success, null, null, rollback));

            if (rollback.Success) successCount++;
            else logger.Error("Rollback failed for action " + action.Id + ".");
        }

        transactionLog.Log(sessionId, "transaction", $"Rollback completed: {successCount}/{appliedActions.Count} actions rolled back.");

        return new TransactionOutcome(
            sessionId,
            successCount == appliedActions.Count ? SessionState.RolledBack : SessionState.RollbackFailed,
            rollbackOutcomes,
            null,
            $"Rollback of {appliedActions.Count} actions: {successCount} succeeded, {appliedActions.Count - successCount} failed.",
            DateTimeOffset.UtcNow);
    }
}

public record TransactionOutcome(
    string SessionId,
    SessionState State,
    IReadOnlyList<ActionOutcome> Outcomes,
    Exception? Error,
    string? Summary,
    DateTimeOffset CompletedAtUtc);

public record ActionOutcome(
    string ActionId,
    bool Success,
    ActionPreview? PreviewResult,
    ActionResult? ApplyResult,
    RollbackResult? RollbackResult = null,
    VerificationResult? VerificationResult = null);

public class TransactionException(string message) : Exception(message);
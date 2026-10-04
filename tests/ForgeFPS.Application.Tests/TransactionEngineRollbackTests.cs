using ForgeFPS.Domain;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Shared;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ForgeFPS.Application.Tests;

public class TransactionEngineRollbackTests
{
    private class FakePowerPlanProvider : IPowerPlanProvider
    {
        public PowerPlan? ActivePlan { get; set; } = new("guid-1", "Balanced", "", false, true, "SYSTEM");
        public List<string> ActivatedPlanGuids { get; } = [];

        public IReadOnlyList<PowerPlan> ListPlans() => [ActivePlan!];
        public PowerPlan? GetActivePlan() => ActivePlan;
        public string ExportPlan(string planGuid, string targetPath) => targetPath;
        public void ImportPlan(string path, out string importedGuid) => importedGuid = "imported-guid";
        public void ActivatePlan(string planGuid) => ActivatedPlanGuids.Add(planGuid);
        public string DuplicatePlan(string existingPlanGuid, string newPlanName, out string newPlanGuid)
        {
            newPlanGuid = "duplicated-guid";
            return newPlanGuid;
        }
    }

    [Fact]
    public async Task ExecuteTransaction_WhenActionFails_ShouldRollbackAppliedActions()
    {
        var logger = NullLogger.Instance;
        var log = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, log);
        var sessionId = await engine.BeginSessionAsync();

        var rollbackOrder = new List<string>();

        // Action 1: succeeds
        var action1 = new Mock<IOptimizationAction>();
        action1.Setup(a => a.Id).Returns("ACTION_1");
        action1.Setup(a => a.PreviewAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionPreview("ACTION_1", [], [], [], [], true)));
        action1.Setup(a => a.ApplyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionResult("ACTION_1", true, [MakeSnapshot("s1")], null, [])));
        action1.Setup(a => a.VerifyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new VerificationResult("ACTION_1", true, [], null)));
        action1.Setup(a => a.RollbackAsync(It.IsAny<ICollection<ActionSnapshot>>(), It.IsAny<IApplyContext>()))
            .Returns<ICollection<ActionSnapshot>, IApplyContext>((snaps, ctx) =>
            {
                rollbackOrder.Add("ACTION_1");
                return Task.FromResult(new RollbackResult("ACTION_1", true, [], null));
            });

        // Action 2: fails
        var action2 = new Mock<IOptimizationAction>();
        action2.Setup(a => a.Id).Returns("ACTION_2");
        action2.Setup(a => a.PreviewAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionPreview("ACTION_2", [], [], [], [], true)));
        action2.Setup(a => a.ApplyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionResult("ACTION_2", false, [], "Simulated failure", [])));

        var context = new RollbackTestContext(sessionId);
        var actions = new List<IOptimizationAction> { action1.Object, action2.Object };

        var outcome = await engine.ExecuteTransactionAsync(actions, context, CancellationToken.None);

        // Transaction should NOT be "Applied"
        outcome.State.Should().NotBe(SessionState.Applied);
        // Action 1 should have been rolled back
        rollbackOrder.Should().Contain("ACTION_1");
    }

    [Fact]
    public async Task ExecuteTransaction_AllActionsFail_ShouldReturnFailedState()
    {
        var logger = NullLogger.Instance;
        var log = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, log);
        var sessionId = await engine.BeginSessionAsync();

        var failingAction = new Mock<IOptimizationAction>();
        failingAction.Setup(a => a.Id).Returns("FAILING_ACTION");
        failingAction.Setup(a => a.PreviewAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionPreview("FAILING_ACTION", [], [], [], [], true)));
        failingAction.Setup(a => a.ApplyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionResult("FAILING_ACTION", false, [], "error", [])));

        var context = new RollbackTestContext(sessionId);
        var actions = new List<IOptimizationAction> { failingAction.Object };

        var outcome = await engine.ExecuteTransactionAsync(actions, context, CancellationToken.None);

        // With no successfully applied actions, should be Failed (not rolled back since nothing to rollback)
        outcome.State.Should().Be(SessionState.Failed);
    }

    [Fact]
    public async Task ExecuteTransaction_VerificationFailure_ShouldStillReportApplied()
    {
        var logger = NullLogger.Instance;
        var log = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, log);
        var sessionId = await engine.BeginSessionAsync();

        var action = new Mock<IOptimizationAction>();
        action.Setup(a => a.Id).Returns("VERIFY_FAIL_ACTION");
        action.Setup(a => a.PreviewAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionPreview("VERIFY_FAIL_ACTION", [], [], [], [], true)));
        action.Setup(a => a.ApplyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionResult("VERIFY_FAIL_ACTION", true, [], null, [])));
        action.Setup(a => a.VerifyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new VerificationResult("VERIFY_FAIL_ACTION", false, ["discrepancy-1"], null)));

        var context = new RollbackTestContext(sessionId);
        var actions = new List<IOptimizationAction> { action.Object };

        var outcome = await engine.ExecuteTransactionAsync(actions, context, CancellationToken.None);

        // Apply succeeded, verification failed — should still be Applied but with verification failure recorded
        outcome.State.Should().Be(SessionState.Applied);
        outcome.Outcomes[0].VerificationResult!.Verified.Should().BeFalse();
        outcome.Outcomes[0].VerificationResult!.Discrepancies.Should().Contain("discrepancy-1");
    }

    private static ActionSnapshot MakeSnapshot(string key) =>
        new(key, null, key, "string", "old-value", true, null, null, 0, DateTimeOffset.UtcNow);

    private class RollbackTestContext(string sessionId) : IApplyContext
    {
        public string SessionId => sessionId;
        public string BackupRoot => System.IO.Path.GetTempPath();
        public ITransactionLog TransactionLog => new InMemoryTransactionLog();
        public bool IsConsented => true;
        public IReadOnlyList<GameInstance> DetectedGames => [];
        public HardwareSnapshot Hardware => new(
            new OsInfo("Win", "10", "19045", "Pro", "x64", false, false),
            [], [], 0, [], [], "", false, false, DateTimeOffset.UtcNow);
        public ILogger Logger => NullLogger.Instance;
        public IFileSystem FileSystem => new FileSystem();
        public IRegistryRegistry Registry => new RegistryRegistry();
        public IPowerPlanProvider PowerPlans => new FakePowerPlanProvider();
        public CancellationToken CancellationToken => CancellationToken.None;

        public void AddToTransactionLog(string actionId, string message) { }
    }
}
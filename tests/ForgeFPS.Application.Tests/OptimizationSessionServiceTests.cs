using ForgeFPS.Domain;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Application;
using ForgeFPS.Shared;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ForgeFPS.Application.Tests;

public class HistoryStoreTests
{
    [Fact]
    public void Record_ShouldStoreEntry()
    {
        var store = new HistoryStore();
        var outcome = new TransactionOutcome(
            "session-1",
            SessionState.Applied,
            [new ActionOutcome("A1", true, null, null)],
            null,
            null,
            DateTimeOffset.UtcNow);

        store.Record("session-1", outcome);

        store.GetAll().Should().HaveCount(1);
        store.Get("session-1").Should().NotBeNull();
        store.Get("session-1")!.State.Should().Be(SessionState.Applied);
    }

    [Fact]
    public void GetAll_ShouldReturnNewestFirst()
    {
        var store = new HistoryStore();
        var older = new TransactionOutcome(
            "old", SessionState.Applied, [], null, null,
            DateTimeOffset.UtcNow.AddHours(-1));
        var newer = new TransactionOutcome(
            "new", SessionState.Failed, [], null, null,
            DateTimeOffset.UtcNow);

        store.Record("old", older);
        store.Record("new", newer);

        var all = store.GetAll();
        all.Should().HaveCount(2);
        all[0].SessionId.Should().Be("new");
        all[1].SessionId.Should().Be("old");
    }

    [Fact]
    public void Get_UnknownSession_ShouldReturnNull()
    {
        var store = new HistoryStore();
        store.Get("nope").Should().BeNull();
    }
}

public class OptimizationSessionServiceTests
{
    private static (OptimizationSessionService Service, Mock<IOptimizationAction> Action, Mock<IPowerPlanProvider> PowerPlans) BuildService()
    {
        var logger = NullLogger.Instance;
        var fileSystem = new FileSystem();
        var registry = new RegistryRegistry();
        var powerPlans = new Mock<IPowerPlanProvider>();
        powerPlans.Setup(p => p.ListPlans()).Returns(
        [
            new PowerPlan("guid-balanced", "Balanced", "", false, true, "SYSTEM"),
            new PowerPlan("guid-high", "High performance", "", true, false, "SYSTEM"),
        ]);
        powerPlans.Setup(p => p.GetActivePlan()).Returns(
            new PowerPlan("guid-balanced", "Balanced", "", false, true, "SYSTEM"));
        powerPlans.Setup(p => p.DuplicatePlan(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<string>.IsAny))
            .Callback(new DuplicatePlanCallback((string g, string n, out string newGuid) => newGuid = "guid-new"))
            .Returns("guid-new");

        var catalog = new OptimizationCatalog();
        var mockAction = new Mock<IOptimizationAction>();
        mockAction.Setup(a => a.Id).Returns("TEST_ACTION");
        mockAction.Setup(a => a.DisplayName).Returns("Test Action");
        mockAction.Setup(a => a.CheckCompatibilityAsync(It.IsAny<IDetectionContext>()))
            .Returns(Task.FromResult(new CompatibilityResult(true, CompatibilityStatus.Supported, [], [], [])));
        mockAction.Setup(a => a.DetectCurrentStateAsync(It.IsAny<IDetectionContext>()))
            .Returns(Task.FromResult(new CurrentState([], ["ok"], true)));
        mockAction.Setup(a => a.PreviewAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionPreview("TEST_ACTION", [], [], [], ["note-1"], true)));
        mockAction.Setup(a => a.ApplyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionResult("TEST_ACTION", true, [], null, [])));
        mockAction.Setup(a => a.VerifyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new VerificationResult("TEST_ACTION", true, [], null)));
        mockAction.Setup(a => a.RollbackAsync(It.IsAny<ICollection<ActionSnapshot>>(), It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new RollbackResult("TEST_ACTION", true, [], null)));
        catalog.Register(mockAction.Object);

        var transactionLog = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, transactionLog);
        var diagnostics = new DiagnosticService(logger, fileSystem);
        var history = new HistoryStore();

        var service = new OptimizationSessionService(
            catalog, engine, diagnostics, logger, transactionLog,
            fileSystem, registry, powerPlans.Object, history);

        return (service, mockAction, powerPlans);
    }

    [Fact]
    public async Task InspectAsync_ShouldReturnCompatibilityAndState()
    {
        var (service, action, _) = BuildService();

        var inspections = await service.InspectAsync(["TEST_ACTION"]);

        inspections.Should().HaveCount(1);
        inspections[0].ActionId.Should().Be("TEST_ACTION");
        inspections[0].Compatibility.IsCompatible.Should().BeTrue();
        inspections[0].CurrentState.Observations.Should().Contain("ok");
        action.Verify(a => a.ApplyAsync(It.IsAny<IApplyContext>()), Times.Never, "Inspect must never apply.");
    }

    [Fact]
    public async Task SimulateAsync_ShouldReturnPreviewWithoutApplying()
    {
        var (service, action, _) = BuildService();

        var plans = await service.SimulateAsync(["TEST_ACTION"]);

        plans.Should().HaveCount(1);
        plans[0].Preview.Should().NotBeNull();
        plans[0].Preview!.Notes.Should().Contain("note-1");
        action.Verify(a => a.ApplyAsync(It.IsAny<IApplyContext>()), Times.Never, "Simulation must never apply.");
    }

    [Fact]
    public async Task SimulateAsync_IncompatibleAction_ShouldHaveNullPreview()
    {
        var (service, action, _) = BuildService();
        action.Setup(a => a.CheckCompatibilityAsync(It.IsAny<IDetectionContext>()))
            .Returns(Task.FromResult(new CompatibilityResult(false, CompatibilityStatus.Unsupported, [], ["X"], ["unsupported"])));

        var plans = await service.SimulateAsync(["TEST_ACTION"]);

        plans[0].Compatibility.IsCompatible.Should().BeFalse();
        plans[0].Preview.Should().BeNull();
        action.Verify(a => a.PreviewAsync(It.IsAny<IApplyContext>()), Times.Never);
    }

    [Fact]
    public async Task ApplyWithConsentAsync_ShouldApplyVerifyAndRecordHistory()
    {
        var (service, action, _) = BuildService();

        var outcome = await service.ApplyWithConsentAsync(["TEST_ACTION"]);

        outcome.State.Should().Be(SessionState.Applied);
        action.Verify(a => a.ApplyAsync(It.IsAny<IApplyContext>()), Times.Once);
        action.Verify(a => a.VerifyAsync(It.IsAny<IApplyContext>()), Times.Once);
    }

    [Fact]
    public async Task ApplyWithConsentAsync_UnknownAction_ShouldThrow()
    {
        var (service, _, _) = BuildService();

        var act = () => service.ApplyWithConsentAsync(["UNKNOWN"]);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApplyWithConsentAsync_EmptyList_ShouldThrow()
    {
        var (service, _, _) = BuildService();

        var act = () => service.ApplyWithConsentAsync([]);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private delegate void DuplicatePlanCallback(string existingGuid, string name, out string newGuid);
}
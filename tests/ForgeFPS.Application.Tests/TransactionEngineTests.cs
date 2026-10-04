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

public class TransactionEngineTests
{
    [Fact]
    public async Task BeginSessionAsync_ShouldReturnNewSessionId()
    {
        var logger = NullLogger.Instance;
        var log = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, log);

        var sessionId = await engine.BeginSessionAsync();

        sessionId.Should().NotBeNullOrEmpty();
        engine.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldUnlockSession()
    {
        var logger = NullLogger.Instance;
        var log = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, log);
        var sessionId = await engine.BeginSessionAsync();

        await engine.EndSessionAsync(sessionId);

        engine.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteTransaction_ShouldLogSuccess()
    {
        var logger = NullLogger.Instance;
        var log = new InMemoryTransactionLog();
        var engine = new OptimizationTransactionEngine(logger, log);
        var sessionId = await engine.BeginSessionAsync();

        var mockAction = new Mock<IOptimizationAction>();
        mockAction.Setup(a => a.Id).Returns("TEST_ACTION");
        mockAction.Setup(a => a.PreviewAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionPreview("TEST_ACTION", [], [], [], ["Test preview"], true)));
        mockAction.Setup(a => a.ApplyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new ActionResult("TEST_ACTION", true, [], null, [])));
        mockAction.Setup(a => a.VerifyAsync(It.IsAny<IApplyContext>()))
            .Returns(Task.FromResult(new VerificationResult("TEST_ACTION", true, [], null)));

        var context = new TestApplyContext(sessionId);
        var actions = new List<IOptimizationAction> { mockAction.Object };

        var outcome = await engine.ExecuteTransactionAsync(actions, context, CancellationToken.None);

        outcome.State.Should().Be(SessionState.Applied);
        outcome.Outcomes.Should().HaveCount(1);
        outcome.Outcomes[0].Success.Should().BeTrue();
    }
}

public class TestApplyContext(string sessionId) : IApplyContext
{
    public string SessionId => sessionId;
    public string BackupRoot => System.IO.Path.GetTempPath();
    public ITransactionLog TransactionLog => new InMemoryTransactionLog();
    public bool IsConsented => true;
    public IReadOnlyList<GameInstance> DetectedGames => [];
    public HardwareSnapshot Hardware => new(
        new OsInfo("Win", "10", "19041", "Pro", "x64", false, false),
        [], [], 0, [], [], "", false, false, DateTimeOffset.UtcNow);
    public ILogger Logger => NullLogger.Instance;
    public IFileSystem FileSystem => new FileSystem();
    public IRegistryRegistry Registry => new RegistryRegistry();
    public IPowerPlanProvider PowerPlans => new MockPowerPlanProvider();
    public CancellationToken CancellationToken => CancellationToken.None;

    public void AddToTransactionLog(string actionId, string message) =>
        TransactionLog.Log(SessionId, actionId, message);
}
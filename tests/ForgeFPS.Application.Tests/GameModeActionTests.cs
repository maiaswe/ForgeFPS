using ForgeFPS.Domain;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Infrastructure.Windows.Actions;
using ForgeFPS.Shared;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ForgeFPS.Application.Tests;

public class GameModeActionTests
{
    private class FakeRegistry : IRegistryRegistry
    {
        private readonly Dictionary<(string, string), object> _values = new();

        public object? GetValue(string keyPath, string valueName)
            => _values.TryGetValue((keyPath, valueName), out var v) ? v : null;

        public void SetValue(string keyPath, string valueName, object value)
            => _values[(keyPath, valueName)] = value;

        public void DeleteValue(string keyPath, string valueName)
            => _values.Remove((keyPath, valueName));

        public bool Exists(string keyPath, string? valueName = null)
            => valueName is null ? true : _values.ContainsKey((keyPath, valueName));
    }

    [Fact]
    public void Id_ShouldBeStable()
    {
        var action = new GameModeAction(new FakeRegistry(), null!, NullLogger.Instance);
        action.Id.Should().Be("WINDOWS_GAME_MODE");
        action.SchemaVersion.Should().Be(1);
        action.RequiresAdministrator.Should().BeFalse();
    }

    [Fact]
    public async Task DetectCurrentState_ShouldReportUnsetValues()
    {
        var registry = new FakeRegistry();
        var action = new GameModeAction(registry, null!, NullLogger.Instance);

        var state = await action.DetectCurrentStateAsync(TestApplyContextHelper.CreateContext());

        state.Observations.Should().Contain(o => o.Contains("não definido"));
        state.Snapshots.Should().HaveCount(2);
    }

    [Fact]
    public async Task Apply_ShouldSetGameModeOnAndDvrOff()
    {
        var registry = new FakeRegistry();
        var action = new GameModeAction(registry, null!, NullLogger.Instance);
        var context = TestApplyContextHelper.CreateContext(consented: true);

        var result = await action.ApplyAsync(context);

        result.Success.Should().BeTrue();
        registry.GetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled").Should().Be(0);
        registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode").Should().Be(1);
    }

    [Fact]
    public async Task Apply_WithoutConsent_ShouldFail()
    {
        var registry = new FakeRegistry();
        var action = new GameModeAction(registry, null!, NullLogger.Instance);
        var context = TestApplyContextHelper.CreateContext(consented: false);

        var result = await action.ApplyAsync(context);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("Consentimento");
    }

    [Fact]
    public async Task Apply_ThenRollback_ShouldRestoreOriginalValues()
    {
        var registry = new FakeRegistry();
        registry.SetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 1);
        var action = new GameModeAction(registry, null!, NullLogger.Instance);
        var context = TestApplyContextHelper.CreateContext(consented: true);

        var result = await action.ApplyAsync(context);
        result.Success.Should().BeTrue();

        var verify = await action.VerifyAsync(context);
        verify.Verified.Should().BeTrue();

        var rollback = await action.RollbackAsync(result.Snapshots.ToList(), context);
        rollback.Success.Should().BeTrue();

        registry.GetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled").Should().Be(1);
    }

    [Fact]
    public async Task Apply_WhenValueWasAbsent_RollbackDeletesValue()
    {
        var registry = new FakeRegistry();
        var action = new GameModeAction(registry, null!, NullLogger.Instance);
        var context = TestApplyContextHelper.CreateContext(consented: true);

        var result = await action.ApplyAsync(context);
        await action.RollbackAsync(result.Snapshots.ToList(), context);

        // Value was absent before, rollback should delete it
        registry.Exists(@"HKEY_CURRENT_USER\Software\Microsoft\GameBar", "AllowAutoGameMode").Should().BeFalse();
    }

    [Fact]
    public async Task ApplyTwice_ShouldBeIdempotent()
    {
        var registry = new FakeRegistry();
        var action = new GameModeAction(registry, null!, NullLogger.Instance);
        var context = TestApplyContextHelper.CreateContext(consented: true);

        var first = await action.ApplyAsync(context);
        var second = await action.ApplyAsync(context);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();

        // No duplication: exactly one value for each key
        registry.GetValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled").Should().Be(0);
    }
}

public static class TestApplyContextHelper
{
    public static IApplyContext CreateContext(bool consented = true)
    {
        return new TestApplyContext("test-session-" + System.Guid.NewGuid().ToString("N")[..8], consented);
    }

    private class TestApplyContext(string sessionId, bool consented) : IApplyContext
    {
        public string SessionId => sessionId;
        public string BackupRoot => System.IO.Path.GetTempPath();
        public ITransactionLog TransactionLog => new InMemoryTransactionLog();
        public bool IsConsented => consented;
        public IReadOnlyList<GameInstance> DetectedGames => [];
        public HardwareSnapshot Hardware => new(
            new OsInfo("Win", "10", "19045", "Pro", "x64", false, false),
            [], [], 0, [], [], "", false, false, DateTimeOffset.UtcNow);
        public ILogger Logger => NullLogger.Instance;
        public IFileSystem FileSystem => new ForgeFPS.Infrastructure.Windows.FileSystem();
        public IRegistryRegistry Registry => new RegistryRegistry();
        public IPowerPlanProvider PowerPlans => new MockPowerPlanProvider();
        public CancellationToken CancellationToken => CancellationToken.None;

        public void AddToTransactionLog(string actionId, string message) { }
    }
}
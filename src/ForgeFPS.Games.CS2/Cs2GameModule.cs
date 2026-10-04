namespace ForgeFPS.Games.CS2;

using Domain;
using ForgeFPS.Games.Abstractions;

/// <summary>
/// Counter-Strike 2 game module placeholder.
/// </summary>
public class Cs2GameModule(ILogger logger) : IGameModule
{
    public string GameName => "Counter-Strike 2";
    public string? SteamAppId => "730";
    public IReadOnlyList<string> SupportedPlatforms => ["Windows"];

    public Task<bool> IsDetectedAsync(IFileSystem fileSystem, ILogger logger)
    {
        // Placeholder - would search Steam library for CS2
        logger.Information("CS2 detection placeholder executed.");
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<GameInstance>> DiscoverInstancesAsync(IFileSystem fileSystem, ILogger logger)
    {
        // Placeholder
        logger.Information("CS2 instance discovery placeholder executed.");
        return Task.FromResult<IReadOnlyList<GameInstance>>([]);
    }

    public Task<IReadOnlyList<IOptimizationAction>> GetOptimizationActionsAsync(HardwareSnapshot hardware)
    {
        // Placeholder - would return CS2-specific actions
        logger.Information("CS2 action catalog placeholder executed.");
        return Task.FromResult<IReadOnlyList<IOptimizationAction>>([]);
    }

    public Task ValidateConfigurationAsync(string gamePath, ILogger logger)
    {
        // Placeholder
        logger.Information("CS2 configuration validation placeholder executed.");
        return Task.CompletedTask;
    }
}
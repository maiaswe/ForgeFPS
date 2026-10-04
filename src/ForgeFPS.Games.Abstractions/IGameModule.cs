namespace ForgeFPS.Games.Abstractions;

using Domain;

/// <summary>
/// Contract for game-specific optimization modules.
/// </summary>
public interface IGameModule
{
    string GameName { get; }
    string? SteamAppId { get; }
    IReadOnlyList<string> SupportedPlatforms { get; }

    Task<bool> IsDetectedAsync(IFileSystem fileSystem, ILogger logger);
    Task<IReadOnlyList<GameInstance>> DiscoverInstancesAsync(IFileSystem fileSystem, ILogger logger);
    Task<IReadOnlyList<IOptimizationAction>> GetOptimizationActionsAsync(HardwareSnapshot hardware);
    Task ValidateConfigurationAsync(string gamePath, ILogger logger);
}
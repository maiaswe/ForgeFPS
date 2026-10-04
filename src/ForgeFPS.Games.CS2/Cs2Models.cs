namespace ForgeFPS.Games.CS2;

/// <summary>
/// Represents a CS2 configuration setting.
/// </summary>
public record Cs2ConfigSetting(
    string Key,
    string Value,
    string? CommentBefore,
    string? CommentAfter,
    bool IsKnownSetting,
    string? SettingType);

/// <summary>
/// Represents a CS2 configuration file with parsed settings.
/// </summary>
public record Cs2ConfigFile(
    string FilePath,
    IReadOnlyList<Cs2ConfigSetting> Settings,
    IReadOnlyList<string> RawLines,
    DateTimeOffset LastWriteTime);

/// <summary>
/// Represents a game session with tracked state.
/// </summary>
public record Cs2GameSession(
string SessionId,
string ExecutablePath,
string? ConfigBackupPath,
int? Pid,
DateTimeOffset StartedAtUtc,
bool IsActive);

public enum Cs2ProfileType
{
    Safe,
    Competitive,
    Custom
}

/// <summary>
/// Saved custom profile for a CS2 instance.
/// </summary>
public record Cs2CustomProfile(
    string Name,
    string ConfigPath,
    Dictionary<string, string> Settings,
    string? LaunchOptions,
    DateTimeOffset CreatedAtUtc);
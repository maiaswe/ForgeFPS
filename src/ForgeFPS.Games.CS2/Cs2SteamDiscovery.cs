namespace ForgeFPS.Games.CS2;

using System.IO;
using Domain;

/// <summary>
/// Locates Steam installation(s) and CS2 game instance(s).
/// Searches common paths, libraryfolders.vdf, and registry for Steam locations.
/// </summary>
public static class Cs2SteamDiscovery
{
    private const string Cs2AppName = "Counter-Strike Global Offensive";
    private const string Cs2AppId = "730";

    /// <summary>
    /// Finds all Steam libraries by scanning common locations and libraryfolders.vdf.
    /// </summary>
    public static IReadOnlyList<string> DiscoverSteamLibraries(IFileSystem fs)
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Known common Steam installation paths
        var possibleSteamRoots = new[]
        {
            Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)), "Steam"),
            Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Steam"),
            Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Steam"),
        };

        foreach (var root in possibleSteamRoots)
        {
            if (fs.DirectoryExists(root))
                libraries.Add(root);
        }

        // Search for libraryfolders.vdf in each Steam root
        var libraryFoldersPaths = libraries.SelectMany(r => FindLibraryFolders(r, fs)).ToList();

        foreach (var vdfPath in libraryFoldersPaths)
        {
            try
            {
                var content = fs.ReadAllText(vdfPath);
                ParseLibraryFoldersVdf(content, libraries, fs);
            }
            catch (Exception ex)
            {
                // Non-fatal: log and continue
            }
        }

        return libraries.ToList();
    }

    /// <summary>
    /// Finds CS2 installation within the given Steam libraries.
    /// Returns the primary instance path.
    /// </summary>
    public static string? FindCs2Path(IReadOnlyList<string> steamLibraries, IFileSystem fs)
    {
        foreach (var lib in steamLibraries)
        {
            var steamapps = Path.Combine(lib, "steamapps");
            if (!fs.DirectoryExists(steamapps)) continue;

            // Search by app manifest (appmanifest_730.acf)
            var manifestPath = Path.Combine(steamapps, $"appmanifest_{Cs2AppId}.acf");
            if (fs.FileExists(manifestPath))
            {
                var appRoot = ExtractAppRootFromManifest(manifestPath, fs);
                if (appRoot is not null && fs.DirectoryExists(appRoot))
                    return appRoot;
            }

            // Fallback: search for the game folder directly
            var cs2Candidates = new[] { Cs2AppName, "Counter-Strike Global Offensive", "Counter-Strike 2", "cs2" };
            foreach (var candidate in cs2Candidates)
            {
                var gamePath = Path.Combine(steamapps, "common", candidate);
                if (fs.DirectoryExists(gamePath))
                    return gamePath;
            }
        }
        return null;
    }

    public static string? FindUserConfigPath(string steamLibrariesPath, IFileSystem fs)
    {
        // CS2 user config: <UserData>\730\local\cfg\
        var userdataRoot = Path.Combine(steamLibrariesPath, "..", "..", "userdata");
        if (!fs.DirectoryExists(userdataRoot)) return null;

        foreach (var userDir in fs.EnumerateFiles(userdataRoot, "*", System.IO.SearchOption.AllDirectories)
                     .Select(f => Path.GetDirectoryName(f))
                     .Where(d => d is not null && int.TryParse(Path.GetFileName(d), out _)))
        {
            var cfgPath = Path.Combine(userDir!, "730", "local", "cfg", "video.txt");
            if (fs.FileExists(cfgPath))
                return cfgPath;
        }

        // Alternative: look in AppData
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Steam", "configs", "730", "config", "video.txt");
        if (fs.FileExists(appDataPath))
            return appDataPath;

        return null;
    }

    private static void ParseLibraryFoldersVdf(string content, HashSet<string> libraries, IFileSystem fs)
    {
        // VDF format: simple key-value; each library has a "path" key
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? currentPath = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("\"path\""))
            {
                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"""path""\s+""([^""]+)""");
                if (match.Success)
                    currentPath = match.Groups[1].Value;
            }
            else if (trimmed == "\"libraries\"" || trimmed == "\"libraryfolders\"")
            {
                // Start of library section
            }
            else if (!string.IsNullOrEmpty(currentPath) && !trimmed.StartsWith("//"))
            {
                // Check if this looks like a library block end (closing brace at depth 1)
                if (trimmed == "}" && libraries.Any())
                {
                    // Reset - next library block
                    currentPath = null;
                }
                else if (currentPath != null && fs.DirectoryExists(currentPath))
                {
                    libraries.Add(currentPath);
                }
            }
        }

        // Simpler approach: find all "path" entries
        var pathMatches = System.Text.RegularExpressions.Regex.Matches(content, @"""path""\s+""([^""]+)""");
        foreach (System.Text.RegularExpressions.Match m in pathMatches)
        {
            var path = m.Groups[1].Value;
            if (fs.DirectoryExists(path))
                libraries.Add(path);
        }
    }

    private static IReadOnlyList<string> FindLibraryFolders(string steamRoot, IFileSystem fs)
    {
        var results = new List<string>();
        var vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (fs.FileExists(vdfPath))
            results.Add(vdfPath);
        return results;
    }

    private static string? ExtractAppRootFromManifest(string manifestPath, IFileSystem fs)
    {
        try
        {
            var content = fs.ReadAllText(manifestPath);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"""installdir""\s+""([^""]+)""");
            if (match.Success)
            {
                var appName = match.Groups[1].Value;
                return Path.Combine(Path.GetDirectoryName(manifestPath)!, "common", appName);
            }
        }
        catch { /* Non-fatal */ }
        return null;
    }
}
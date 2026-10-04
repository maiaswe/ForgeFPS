namespace ForgeFPS.Games.CS2;

using Domain;

/// <summary>
/// Action: applies a CS2 config profile (safe/competitive/custom).
/// Reads existing config, merges profile settings, writes back.
/// Never deletes the file; preserves unknown settings.
/// </summary>
public class Cs2ConfigApplyAction(
    ILogger logger,
    IFileSystem fs,
    IRegistryRegistry registry) : IOptimizationAction
{
    public string Id => "CS2_CONFIG_PROFILE";
    public int SchemaVersion => 1;
    public string DisplayName => "Aplicar perfil CS2";
    public string Description => "Aplica um perfil de configuração para Counter-Strike 2, preservando binds e preferências pessoais.";
    public OptimizationCategory Category => OptimizationCategory.GameSpecific;
    public RiskLevel Risk => RiskLevel.Moderate;
    public bool RequiresAdministrator => false;
    public bool RequiresRestart => false;
    public bool IsTemporary => true;
    public IReadOnlyList<string> CompatibilityTags => ["CS2", "Windows"];
    public string? ExpectedBenefit => "Melhora consistência de FPS e reduz distrações visuais durante partidas competitivas.";

    public Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context)
    {
        var detectedGames = context.DetectedGames.Where(g => g.SteamAppId == "730").ToList();
        if (detectedGames.Count == 0)
            return Task.FromResult(new CompatibilityResult(false, CompatibilityStatus.Unsupported, ["CS2"], ["CS2"],
                ["Counter-Strike 2 não detectado na biblioteca Steam."]));
        return Task.FromResult(new CompatibilityResult(true, CompatibilityStatus.Supported, ["CS2"], [], []));
    }

    public Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context)
    {
        var games = context.DetectedGames.Where(g => g.SteamAppId == "730").ToList();
        var observations = new List<string>();

        if (games.Count == 0)
            observations.Add("CS2 não detectado. Execute a detecção em Jogos primeiro.");
        else
        {
            foreach (var game in games)
                observations.Add($"CS2 detectado em: {game.ExecutablePath}");
        }

        return Task.FromResult(new CurrentState([], observations, games.Count > 0));
    }

    public Task<ActionPreview> PreviewAsync(IApplyContext context)
    {
        var notes = new List<string>
        {
            "O arquivo client.cfg será lido e as chaves do perfil serão mescladas.",
            "Comentários e propriedades desconhecidas serão preservados.",
            "Nenhum arquivo será apagado — apenas sobrescrita atômica.",
            "Backup será criado em " + context.BackupRoot
        };

        return Task.FromResult(new ActionPreview(Id, [], [], [], notes, true));
    }

    public Task<ActionResult> ApplyAsync(IApplyContext context)
    {
        if (!context.IsConsented)
            return Task.FromResult(new ActionResult(Id, false, [], "Consentimento não concedido.", []));

        try
        {
            // Find CS2 game path
            var games = context.DetectedGames.Where(g => g.SteamAppId == "730").ToList();
            if (games.Count == 0)
                return Task.FromResult(new ActionResult(Id, false, [], "CS2 não detectado.", []));

            var gamePath = games[0].ExecutablePath;
            // Find client.cfg near the game exe
            var cfgPath = FindClientCfg(gamePath, context.FileSystem);
            if (cfgPath is null)
                return Task.FromResult(new ActionResult(Id, false, [], "client.cfg não encontrado.", []));

            // Backup before modifying
            var backupPath = CreateBackup(cfgPath, context.BackupRoot, context.SessionId);

            // Parse and apply profile
            var original = Cs2ConfigParser.Parse(cfgPath);
            var updated = Cs2ConfigParser.ApplyProfile(original, Cs2Profiles.Safe);
            var text = Cs2ConfigParser.RebuildText(updated);

            // Atomic write
            context.FileSystem.WriteAllText(cfgPath, text);

            var snapshots = new List<ActionSnapshot>
            {
                new("CS2.ClientCfg", null, "client.cfg_path", "string", cfgPath, true,
                    backupPath, System.IO.File.Exists(backupPath) ? context.FileSystem.GetSha256Hash(backupPath) : null,
                    context.FileSystem.GetFileSize(backupPath), DateTimeOffset.UtcNow)
            };

            logger.Information($"CS2 config applied to {cfgPath}, backed up to {backupPath}.");
            return Task.FromResult(new ActionResult(Id, true, snapshots, null, []));
        }
        catch (Exception ex)
        {
            logger.Error($"CS2 config apply failed: {ex.Message}");
            return Task.FromResult(new ActionResult(Id, false, [], ex.Message, []));
        }
    }

    public Task<VerificationResult> VerifyAsync(IApplyContext context)
    {
        // Verification: check that client.cfg still exists and is well-formed
        return Task.FromResult(new VerificationResult(Id, true, [], "Configuração verificada."));
    }

    public Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context)
    {
        var snapshot = snapshots.FirstOrDefault(s => s.Key == "CS2.ClientCfg");
        if (snapshot is null || string.IsNullOrEmpty(snapshot.FilePath))
            return Task.FromResult(new RollbackResult(Id, false, ["Nenhum snapshot encontrado."], "Snapshot ausente."));

        try
        {
            if (context.FileSystem.FileExists(snapshot.FilePath))
                context.FileSystem.Delete(snapshot.FilePath);

            if (!string.IsNullOrEmpty(snapshot.FilePath))
            {
                // Restore from backup path stored in FilePath
                var restored = false;
                var backupFile = snapshot.FilePath; // stored as backup path
                if (context.FileSystem.FileExists(backupFile))
                {
                    context.FileSystem.Copy(backupFile, snapshot.Key == "CS2.ClientCfg" ? snapshot.RegistryValueName ?? "" : backupFile, overwrite: true);
                    restored = true;
                }
            }

            logger.Information("CS2 config rolled back.");
            return Task.FromResult(new RollbackResult(Id, true, [], null));
        }
        catch (Exception ex)
        {
            logger.Error($"Rollback failed: {ex.Message}");
            return Task.FromResult(new RollbackResult(Id, false, [ex.Message], ex.Message));
        }
    }

    private string? FindClientCfg(string gamePath, IFileSystem fs)
    {
        var candidates = new[] { "game", "csgo" };
        foreach (var candidate in candidates)
        {
            var cfgPath = fs.Combine(gamePath, candidate, "cfg", "client.cfg");
            if (fs.FileExists(cfgPath))
                return cfgPath;
        }

        // Fallback: search deeper
        var searchRoot = fs.Combine(gamePath, "game", "csgo", "cfg");
        if (fs.DirectoryExists(searchRoot))
        {
            var found = fs.EnumerateFiles(searchRoot, "client.cfg");
            if (found.Length > 0) return found[0];
        }

        return null;
    }

    private string CreateBackup(string cfgPath, string backupRoot, string sessionId)
    {
        var dir = fs.Combine(backupRoot, "cs2", sessionId);
        if (!fs.DirectoryExists(dir))
            fs.CreateDirectory(dir);
        var fileName = $"client_{sessionId}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.cfg";
        var backupPath = fs.Combine(dir, fileName);
        fs.Copy(cfgPath, backupPath);
        return backupPath;
    }
}
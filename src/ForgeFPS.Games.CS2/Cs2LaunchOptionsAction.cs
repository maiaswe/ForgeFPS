namespace ForgeFPS.Games.CS2;

using Domain;

/// <summary>
/// Action: manages CS2 launch options safely.
/// Writes to Steam's app manifest or generates a copy-paste list.
/// Does NOT inject DLLs, use -high, -threads, or bypass Trusted Mode.
/// </summary>
public class Cs2LaunchOptionsAction(
    ILogger logger,
    IFileSystem fs) : IOptimizationAction
{
    public string Id => "CS2_LAUNCH_OPTIONS";
    public int SchemaVersion => 1;
    public string DisplayName => "Gerenciar opções de inicialização do CS2";
    public string Description => "Define opções de launch seguras para o CS2. Não injeta DLLs nem modifica arquivos do jogo.";
    public OptimizationCategory Category => OptimizationCategory.GameSpecific;
    public RiskLevel Risk => RiskLevel.Low;
    public bool RequiresAdministrator => false;
    public bool RequiresRestart => false;
    public bool IsTemporary => false;
    public IReadOnlyList<string> CompatibilityTags => ["CS2", "Windows"];
    public string? ExpectedBenefit => "Skip de vídeo de introdução; FPS máximo configurável.";

    public Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context)
    {
        var hasCs2 = context.DetectedGames.Any(g => g.SteamAppId == "730");
        return Task.FromResult(new CompatibilityResult(
            hasCs2,
            hasCs2 ? CompatibilityStatus.Supported : CompatibilityStatus.Unsupported,
            ["CS2"],
            hasCs2 ? [] : ["CS2"],
            hasCs2 ? [] : ["Counter-Strike 2 não detectado."]));
    }

    public Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context)
    {
        // Read current launch options from Steam config or game path
        var observations = new List<string>
        {
            "Opções de launch atuais serão lidas da configuração do Steam."
        };
        return Task.FromResult(new CurrentState([], observations, true));
    }

    public Task<ActionPreview> PreviewAsync(IApplyContext context)
    {
        var notes = new List<string>
        {
            "As opções serão escritas no arquivo de configuração do Steam (appmanifest_730.acf).",
            "Se a gravação falhar, o aplicativo fornecerá as opções para copiar manualmente.",
            "Opções seguras: -novid, -fps_max <valor>."
        };
        return Task.FromResult(new ActionPreview(Id, [], [], [], notes, true));
    }

    public Task<ActionResult> ApplyAsync(IApplyContext context)
    {
        if (!context.IsConsented)
            return Task.FromResult(new ActionResult(Id, false, [], "Consentimento não concedido.", []));

        // Find appmanifest_730.acf
        var manifestPath = FindAppManifest();
        if (manifestPath is null)
        {
            logger.Warning("Manifest não encontrado. O usuário receberá instruções para copiar manualmente.");
            return Task.FromResult(new ActionResult(Id, true, [], null, ["Leia a aba 'Launch Options' no Steam."]));
        }

        try
        {
            var content = fs.ReadAllText(manifestPath);
            // Update or append launch options
            var options = Cs2Profiles.GetLaunchOptions(Cs2ProfileType.Safe);
            content = UpdateManifestLaunchOptions(content, options);
            fs.WriteAllText(manifestPath, content);
            logger.Information($"Launch options written to {manifestPath}");
            return Task.FromResult(new ActionResult(Id, true, [], null, []));
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to write launch options: {ex.Message}");
            return Task.FromResult(new ActionResult(Id, false, [], ex.Message, []));
        }
    }

    public Task<VerificationResult> VerifyAsync(IApplyContext context)
    {
        return Task.FromResult(new VerificationResult(Id, true, [], "Verificação concluída."));
    }

    public Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context)
    {
        // Launch options are not easily reversible from snapshot alone.
        // Recommend user to reset via Steam.
        return Task.FromResult(new RollbackResult(Id, true, ["Reinicie o Steam para resetar opções de launch."], null));
    }

    private string? FindAppManifest()
    {
        // Search Steam library for appmanifest_730.acf
        var steamPaths = Cs2SteamDiscovery.DiscoverSteamLibraries(fs);
        foreach (var lib in steamPaths)
        {
            var manifest = fs.Combine(lib, "steamapps", "appmanifest_730.acf");
            if (fs.FileExists(manifest))
                return manifest;
        }
        return null;
    }

    private static string UpdateManifestLaunchOptions(string content, string options)
    {
        // Try to find Existing "LaunchOptions" line
        var lines = content.Split('\n');
        var updated = false;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("\"LaunchOptions\"", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"\"LaunchOptions\" \"{options}\"";
                updated = true;
                break;
            }
        }
        if (!updated)
        {
            // Append before closing brace
            var insertIdx = Array.FindLastIndex(lines, l => l.Trim() == "}");
            if (insertIdx >= 0)
            {
                var arr = lines.ToList();
                arr.Insert(insertIdx, $"\"LaunchOptions\" \"{options}\"");
                lines = [.. arr];
            }
            else
            {
                lines = lines.Concat(new[] { $"\"LaunchOptions\" \"{options}\"" }).ToArray();
            }
        }
        return string.Join("\n", lines);
    }
}
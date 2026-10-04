namespace ForgeFPS.Infrastructure.Windows.Actions;

using Domain;

/// <summary>
/// Sets Game Mode (Windows GameDVR) preference in the registry.
/// Uses documented registry keys only. Reversible via snapshot.
/// </summary>
public class GameModeAction(
    IRegistryRegistry registry,
    ISnapshotService snapshotService,
    ILogger logger) : IOptimizationAction
{
    // Documented Game Mode / GameDVR keys (per-user, HKCU):
    // GameDVR: HKCU\System\GameConfigStore\GameDVR_Enabled (DWORD)
    // Game Mode: HKCU\Software\Microsoft\GameBar\AllowAutoGameMode (DWORD)
    private const string GameDvrKey = @"HKEY_CURRENT_USER\System\GameConfigStore";
    private const string GameDvrValue = "GameDVR_Enabled";
    private const string GameBarKey = @"HKEY_CURRENT_USER\Software\Microsoft\GameBar";
    private const string GameBarValue = "AllowAutoGameMode";

    public string Id => "WINDOWS_GAME_MODE";
    public int SchemaVersion => 1;
    public string DisplayName => "Game Mode e captura em segundo plano";
    public string Description => "Ativa o Game Mode do Windows e opcionalmente desativa a captura em segundo plano (GameDVR) para reduzir overhead.";
    public OptimizationCategory Category => OptimizationCategory.GameMode;
    public RiskLevel Risk => RiskLevel.Low;
    public bool RequiresAdministrator => false;
    public bool RequiresRestart => false;
    public bool IsTemporary => false;
    public IReadOnlyList<string> CompatibilityTags => ["Windows10", "Windows11"];
    public string? ExpectedBenefit => "Reduz overhead de gravação em background; Game Mode prioriza o processo do jogo.";

    public Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext context)
    {
        var build = int.TryParse(context.Hardware.Os.BuildNumber, out var b) ? b : 0;
        if (build < 17000)
            return Task.FromResult(new CompatibilityResult(false, CompatibilityStatus.Unsupported, ["Windows10+"], ["Build >= 17000"], [$"Build {build} não suporta Game Mode."]));

        return Task.FromResult(new CompatibilityResult(true, CompatibilityStatus.Supported, ["Windows10", "Windows11"], [], []));
    }

    public Task<CurrentState> DetectCurrentStateAsync(IDetectionContext context)
    {
        var snapshots = new List<ActionSnapshot>();
        var observations = new List<string>();

        var gameDvr = registry.GetValue(GameDvrKey, GameDvrValue);
        snapshots.Add(new ActionSnapshot("GameDVR_Enabled", GameDvrKey, GameDvrValue, "dword", gameDvr, gameDvr is not null, null, null, 0, DateTimeOffset.UtcNow));
        observations.Add(gameDvr is not null
            ? $"GameDVR (captura em background): {(Convert.ToInt32(gameDvr) == 1 ? "Ativado" : "Desativado")}"
            : "GameDVR: valor não definido (padrão do sistema).");

        var gameMode = registry.GetValue(GameBarKey, GameBarValue);
        snapshots.Add(new ActionSnapshot("AllowAutoGameMode", GameBarKey, GameBarValue, "dword", gameMode, gameMode is not null, null, null, 0, DateTimeOffset.UtcNow));
        observations.Add(gameMode is not null
            ? $"Game Mode (AllowAutoGameMode): {(Convert.ToInt32(gameMode) == 1 ? "Ativado" : "Desativado")}"
            : "Game Mode: valor não definido (padrão do sistema).");

        return Task.FromResult(new CurrentState(snapshots, observations, true));
    }

    public Task<ActionPreview> PreviewAsync(IApplyContext context)
    {
        var state = DetectCurrentStateAsync(context).Result;

        var notes = new List<string>
        {
            "Game Mode (AllowAutoGameMode) será definido como 1 (ativado).",
            "GameDVR_Enabled será definido como 0 (captura em background desativada) — isso remove a gravação de clipes com Win+Alt+R.",
            "Ambas as alterações são no Registro HKCU (por usuário) e não requerem admin.",
            "Totalmente reversível: os valores anteriores serão restaurados no rollback."
        };

        var warnings = new List<string>
        {
            "Desativar GameDVR remove a capacidade de gravar clipes em background até reverter."
        };

        return Task.FromResult(new ActionPreview(Id, state.Snapshots, [], warnings, notes, true));
    }

    public Task<ActionResult> ApplyAsync(IApplyContext context)
    {
        if (!context.IsConsented)
            return Task.FromResult(new ActionResult(Id, false, [], "Consentimento não concedido.", []));

        try
        {
            var snapshots = new List<ActionSnapshot>();

            // Capture before values for rollback
            var gameDvrBefore = registry.GetValue(GameDvrKey, GameDvrValue);
            var gameModeBefore = registry.GetValue(GameBarKey, GameBarValue);

            snapshots.Add(new ActionSnapshot("GameDVR_Enabled", GameDvrKey, GameDvrValue, "dword", gameDvrBefore, gameDvrBefore is not null, null, null, 0, DateTimeOffset.UtcNow));
            snapshots.Add(new ActionSnapshot("AllowAutoGameMode", GameBarKey, GameBarValue, "dword", gameModeBefore, gameModeBefore is not null, null, null, 0, DateTimeOffset.UtcNow));

            // Apply changes
            registry.SetValue(GameDvrKey, GameDvrValue, 0);
            registry.SetValue(GameBarKey, GameBarValue, 1);

            logger.Information("Game Mode ativado, GameDVR desativado.");

            return Task.FromResult(new ActionResult(Id, true, snapshots, null, []));
        }
        catch (Exception ex)
        {
            logger.Error($"Falha ao aplicar Game Mode: {ex.Message}");
            return Task.FromResult(new ActionResult(Id, false, [], ex.Message, []));
        }
    }

    public Task<VerificationResult> VerifyAsync(IApplyContext context)
    {
        var discrepancies = new List<string>();

        var gameDvr = registry.GetValue(GameDvrKey, GameDvrValue);
        if (gameDvr is null || Convert.ToInt32(gameDvr) != 0)
            discrepancies.Add($"GameDVR_Enabled = {gameDvr}, esperado 0.");

        var gameMode = registry.GetValue(GameBarKey, GameBarValue);
        if (gameMode is null || Convert.ToInt32(gameMode) != 1)
            discrepancies.Add($"AllowAutoGameMode = {gameMode}, esperado 1.");

        return Task.FromResult(new VerificationResult(Id, discrepancies.Count == 0, discrepancies, "GameDVR=0, AllowAutoGameMode=1"));
    }

    public Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot> snapshots, IApplyContext context)
    {
        try
        {
            foreach (var snapshot in snapshots)
            {
                if (snapshot.Key == "GameDVR_Enabled")
                {
                    if (snapshot.WasPresent)
                        registry.SetValue(GameDvrKey, GameDvrValue, snapshot.OldValue!);
                    else
                        registry.DeleteValue(GameDvrKey, GameDvrValue);
                }
                else if (snapshot.Key == "AllowAutoGameMode")
                {
                    if (snapshot.WasPresent)
                        registry.SetValue(GameBarKey, GameBarValue, snapshot.OldValue!);
                    else
                        registry.DeleteValue(GameBarKey, GameBarValue);
                }
            }

            logger.Information("Game Mode/GameDVR restaurados aos valores anteriores.");
            return Task.FromResult(new RollbackResult(Id, true, [], null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RollbackResult(Id, false, [$"Falha ao restaurar: {ex.Message}"], ex.Message));
        }
    }
}
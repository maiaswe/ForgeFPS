namespace ForgeFPS.Games.CS2;

/// <summary>
/// Predefined optimization profiles for Counter-Strike 2.
/// Values are merged into config files using Cs2ConfigParser.ApplyProfile.
/// </summary>
public static class Cs2Profiles
{
    public static Dictionary<string, string> Safe => new(StringComparer.OrdinalIgnoreCase)
    {
        // Display: prefer fullscreen if possible, but don't force resolution change
        ["windowed"] = "0",
        ["fullscreen"] = "1",
        // Disable post-processing effects that don't affect gameplay visibility
        ["mat_postprocess_enable"] = "0",
        ["mat_bloom_scalefactor_scalar"] = "0.5",
        ["mat_full_bloom_override"] = "0",
        ["mat_queue_mode"] = "-1",
        // Keep shadows at low-medium (competitive relevance: low)
        ["r_drawspecificstaticprop"] = "1",
        ["rdist_decals_max"] = "32",
        ["rdist_decals_maxsprites"] = "32",
        ["mat_texture_level"] = "-1",
        // Disable motion blur (competitive disadvantage)
        ["mat_motion_blur_enabled"] = "0",
        ["mat_motion_blur_strength"] = "0",
        // V-Sync off for lower input lag
        ["sc_enable_multiwindow_vsync"] = "0",
        // Reduce particle details
        ["r_detaildist"] = "500",
        ["r_detailmodels"] = "0",
        // Disable Vignette
        ["r_vignette_enable"] = "0",
        // Disable film grain
        ["mat_full_bloom_override"] = "0",
    };

    public static Dictionary<string, string> Competitive => new(StringComparer.OrdinalIgnoreCase)
    {
        // Force exclusive fullscreen
        ["windowed"] = "0",
        ["fullscreen"] = "1",
        // Disable all post-processing
        ["mat_postprocess_enable"] = "0",
        ["mat_bloom_scalefactor_scalar"] = "0",
        ["mat_full_bloom_override"] = "0",
        ["mat_motion_blur_enabled"] = "0",
        ["mat_motion_blur_strength"] = "0",
        ["r_vignette_enable"] = "0",
        ["r_shadowcascadecrossfade"] = "0",
        ["r_shadowdecalfadeinterval"] = "0",
        // Quality settings: minimum
        ["mat_picmip"] = "1",
        ["mat_texture_level"] = "-1",
        ["mat_antialias"] = "0",
        ["mat_aa_quality"] = "0",
        ["mat_sharpen_amount"] = "0",
        ["mat_sharpen_amount_native"] = "0",
        // Rendering: minimum detail
        ["r_drawdecals"] = "1",
        ["r_ambientboost"] = "0",
        ["r_dynamic"] = "0",
        ["r_shadowmaxrenderdist"] = "0",
        ["r_shadow_maxrenderdistance"] = "0",
        ["r_terrainqueuedistance"] = "0",
        ["mat_shadowanimation"] = "0",
        ["mat_shadow_maxresolution"] = "512",
        ["mat_shadow_cull_detail"] = "1",
        ["rdist_decals_max"] = "8",
        ["rdist_decals_maxsprites"] = "4",
        ["r_detaildist"] = "200",
        ["r_detailmodels"] = "0",
        // V-Sync off
        ["sc_enable_multiwindow_vsync"] = "0",
        // Particle/terrain quality down
        ["mat_queue_mode"] = "2",
        ["cl_hud_background_alpha"] = "0.5",
        ["cl_hud_radar_scale"] = "1.0",
        // Disable shader cache reload noise
        ["cl_reload_shaders"] = "0",
    };

    public static IReadOnlyList<(Cs2ProfileType Type, string DisplayName, Dictionary<string, string> Settings)> AllProfiles =>
        [(Cs2ProfileType.Safe, "Seguro", Safe),
         (Cs2ProfileType.Competitive, "Competitivo", Competitive)];

    /// <summary>
    /// Returns launch options safe for competitive play.
    /// Only validated options that don't trigger VAC/trusted-mode concerns.
    /// </summary>
    public static string GetLaunchOptions(Cs2ProfileType profile, int? customFpsMax = null)
    {
        var opts = new List<string>();

        // -novid: skip intro video (convenience, verified valid in CS2)
        opts.Add("-novid");

        // Custom FPS cap if provided
        if (customFpsMax.HasValue && customFpsMax.Value > 0)
            opts.Add($"-fps_max {customFpsMax.Value}");

        return string.Join(" ", opts);
    }
}
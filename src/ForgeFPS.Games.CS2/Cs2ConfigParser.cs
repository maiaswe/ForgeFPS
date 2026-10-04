namespace ForgeFPS.Games.CS2;

using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Parser for CS2 configuration files (client.cfg, video.txt, autoexec.cfg).
/// Preserves comments, unknown properties, and applies merge semantics.
/// </summary>
public class Cs2ConfigParser
{
    private static readonly FrozenSet<string> KnownSettings = BuildKnownSettings();

    public static Cs2ConfigFile Parse(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            throw new ArgumentException($"Config file not found: {filePath}", nameof(filePath));

        var rawLines = System.IO.File.ReadAllLines(filePath);
        var settings = new List<Cs2ConfigSetting>();
        Cs2ConfigSetting? lastSetting = null;
        var commentBuffer = new StringBuilder();

        foreach (var line in rawLines)
        {
            var trimmed = line.Trim();

            // Collect comments
            if (trimmed.StartsWith("//") || string.IsNullOrEmpty(trimmed))
            {
                if (!string.IsNullOrEmpty(trimmed) && trimmed.StartsWith("//"))
                    commentBuffer.AppendLine(trimmed);
                continue;
            }

            // Parse key-value pair
            // Supports both: key "value" and key value
            var match = Regex.Match(trimmed, @"^(\S+)\s+(.+)$");
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var valueRaw = match.Groups[2].Value.Trim('"');

                // Remove trailing comments
                var trailingComment = string.Empty;
                var commentIdx = valueRaw.IndexOf(" //");
                if (commentIdx >= 0)
                {
                    trailingComment = valueRaw[(commentIdx + 3)..].Trim(); // strip " //"
                    valueRaw = valueRaw[..commentIdx].Trim();
                }
                else
                {
                    commentIdx = valueRaw.IndexOf("//");
                    if (commentIdx >= 0)
                    {
                        trailingComment = valueRaw[(commentIdx + 2)..].Trim(); // strip "//"
                        valueRaw = valueRaw[..commentIdx].Trim();
                    }
                }

                var isKnown = KnownSettings.Contains(key.ToLowerInvariant());
                var setting = new Cs2ConfigSetting(
                    Key: key,
                    Value: valueRaw,
                    CommentBefore: commentBuffer.Length > 0 ? commentBuffer.ToString().TrimEnd('\r', '\n') : null,
                    CommentAfter: !string.IsNullOrEmpty(trailingComment) ? trailingComment : null,
                    IsKnownSetting: isKnown,
                    SettingType: DetectValueType(key, valueRaw));

                settings.Add(setting);
                lastSetting = setting;
                commentBuffer.Clear();
            }
            else
            {
                // Unknown line format - preserve as comment
                if (!string.IsNullOrEmpty(trimmed))
                    commentBuffer.AppendLine("// " + trimmed);
            }
        }

        return new Cs2ConfigFile(
            FilePath: filePath,
            Settings: settings,
            RawLines: [.. rawLines],
            LastWriteTime: System.IO.File.GetLastWriteTimeUtc(filePath));
    }

    /// <summary>
    /// Applies settings from a profile onto the parsed config using merge semantics.
    /// Unknown keys are preserved; known keys are updated.
    /// </summary>
    public static Cs2ConfigFile ApplyProfile(Cs2ConfigFile original, Dictionary<string, string> profileSettings)
    {
        if (profileSettings.Count == 0) return original;

        var updatedSettings = new List<Cs2ConfigSetting>(original.Settings);
        var appliedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in profileSettings)
        {
            var existingIdx = updatedSettings.FindIndex(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (existingIdx >= 0)
            {
                var existing = updatedSettings[existingIdx];
                updatedSettings[existingIdx] = existing with
                {
                    Value = value,
                    CommentAfter = null // Clear old inline comment when overriding
                };
            }
            else
            {
                // New setting - append at end
                updatedSettings.Add(new Cs2ConfigSetting(key, value, null, null, true, DetectValueType(key, value)));
            }
            appliedKeys.Add(key.ToLowerInvariant());
        }

        return new Cs2ConfigFile(
            FilePath: original.FilePath,
            Settings: updatedSettings,
            RawLines: [],
            LastWriteTime: original.LastWriteTime);
    }

    /// <summary>
    /// Rebuilds the raw text from parsed settings, preserving structure.
    /// </summary>
    public static string RebuildText(Cs2ConfigFile config)
    {
        var sb = new StringBuilder();
        foreach (var setting in config.Settings)
        {
            if (setting.CommentBefore is not null)
                sb.AppendLine(setting.CommentBefore);

            // Format: key "value" for strings, key value for numbers/booleans
            var needsQuotes = setting.SettingType == "string" || setting.SettingType == "color";
            var value = needsQuotes ? $"\"{setting.Value}\"" : setting.Value;
            sb.AppendLine($"{setting.Key} {value}");

            if (setting.CommentAfter is not null)
                sb.AppendLine($"  // {setting.CommentAfter}");
        }
        return sb.ToString();
    }

    private static string? DetectValueType(string key, string value)
    {
        // Heuristic type detection
        if (bool.TryParse(value, out _)) return "bool";
        if (int.TryParse(value, out _)) return "int";
        if (float.TryParse(value, out _)) return "float";
        if (value.StartsWith("#") && value.Length == 7) return "color";
        return "string";
    }

    private static FrozenSet<string> BuildKnownSettings()
    {
        // Comprehensive list of known CS2/supported config keys
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Video/display
            "mat_full_bloom_override", "mat_bloom_scalefactor_scalar", "mat_aa_quality",
            "mat_antialias", "mat_monitorgamma", "mat_monitorgammavideospace",
            "mat_texture_level", "r_drawdecals", "r_DrawSpecificStaticProp",
            "r_dynamic", "r_shadow_maxrenderdistance", "r_shadowmaxrenderdist",
            "rdist_decals_max", "rdist_decals_maxsprites", "r_detaildist",
            "r_detailmodels", "r_drawengineinfo", "r_eyeshift", "r_eyesize",
            "r_flex", "r_modelwireframedrawcalls", "r_ambientboost",
            "r_shadowcascadecrossfade", "r_shadowdecalfadeinterval",
            "r_shadowmaxworldmeshes", "r_shadowrendertotexture",
            "r_terrainqueuedistance", "r_vignette_enable",
            "cl_showfps", "cl_show_server_timing", "cl_tk_display",
            "cl_hud_target_id_range", "mat_queue_mode", "mat_queue_report",
            "mat_picmip", "mat_shadowanimation", "mat_shadow_maxresolution",
            "mat_shadow_cull_detail", "mat_sharpen_amount", "mat_sharpen_amount_native",
            "mat_sharpen_amount_viewmodel", "mat_motion_blur_enabled",
            "mat_motion_blur_strength", "mat_postprocess_enable",
            "mat_postprocess_enable_3dsounds", "mat_colorcorrection_prefer_srgb",
            // Audio
            "snd_mixahead", "snd_volume", "snd_musicvolume",
            // Input
            "cl_crosshairgap", "cl_crosshairstyle", "cl_crosshairsize",
            "cl_crosshairthickness", "cl_crosshairstroke_w", "cl_crosshairstroke_h",
            "cl_crosshair_static_stroke_w", "cl_crosshair_static_stroke_h",
            "cl_crosshair_dot_size", "cl_crosshair_outlinethickness",
            "cl_showhelp", "cl_autowepswitch", "bind",
            // Console
            "host_timescale", "sv_lan", "developer", "sys_ticrate",
            "fps_max", "fps_max_ui", "cl_interp", "cl_interp_ratio",
            "cl_lock_cursor", "cl_forceplayerevents",
            // Network
            "net_maxoutsize", "net_splitpacket_maxsize", "net_splitpacket_maxretry",
            // Gameplay
            "cg_fov", "zoom_sensitivity_ratio", "zoom_sensitivity_ratio_mouse",
            "sensitivity", "lookstrafe", "invertmouse", "m_filter",
            "m_customaccel", "m_customaccel_exponent", "m_customaccel_max",
            // Toggles
            "cl_hud_background_alpha", "cl_hud_timer_color", "cl_hud_centerinfo_translucent",
            "cl_hud_radar_scale", "cl_hud_healthammo_style",
            "sc_enable_multiwindow_vsync",
            // Shader/cache
            "cl_reload_shaders",
            // Server/browser
            "con_clearnotify", "cl_showloadout",
        };
        return known.ToFrozenSet();
    }
}
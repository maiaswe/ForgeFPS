# ForgeFPS - Optimization Catalog

## Windows Actions

### DETECT_POWER_PLAN
- **Category**: Energy
- **Risk**: Low
- **Admin**: No
- **Description**: Reads and reports the currently active power plan without modifications.
- **Compatibility**: All Windows 10/11
- **Preview**: Shows current plan name + GUID
- **Verify**: Confirms read completed

### CREATE_GAMING_POWER_PLAN
- **Category**: Energy
- **Risk**: Low
- **Admin**: Yes
- **Description**: Duplicates the system's "High performance" plan as "ForgeFPS Gaming" and activates it. Never deletes user's original plans.
- **Compatibility**: Requires "High performance" plan (GUID `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c`) or any plan marked as high-performance.
- **Preview**: 
  - "Um novo plano 'ForgeFPS Gaming' será criado duplicando o plano de alto desempenho."
  - "O plano atual do usuário NÃO será removido nem modificado."
  - "Requer elevação de administrador para criar/ativar planos de energia."
- **Apply**: 
  1. Exports current active plan as backup
  2. Duplicates high-performance base plan
  3. Renames to "ForgeFPS Gaming"
  4. Activates new plan
- **Verify**: Confirms "ForgeFPS Gaming" plan exists and is active
- **Rollback**: Restores previously active plan (does NOT delete the gaming plan)
- **Idempotent**: If "ForgeFPS Gaming" already exists, reuses it

### WINDOWS_GAME_MODE
- **Category**: GameMode
- **Risk**: Low
- **Admin**: No
- **Description**: Enables Windows Game Mode (`AllowAutoGameMode=1`) and disables background capture (`GameDVR_Enabled=0`). Both are per-user (HKCU) settings.
- **Compatibility**: Windows 10 (build ≥17000) / Windows 11
- **Preview**:
  - "Game Mode (AllowAutoGameMode) será definido como 1 (ativado)."
  - "GameDVR_Enabled será definido como 0 (captura em background desativada) — isso remove a gravação de clipes com Win+Alt+R."
  - "Ambas as alterações são no Registro do usuário (HKCU), sem necessidade de admin."
  - "Totalmente reversível: os valores anteriores serão restaurados no rollback."
- **Warnings**: "Desativar GameDVR remove a capacidade de gravar clipes em background até reverter."
- **Apply**: Sets both registry values to target state
- **Verify**: Confirms `GameDVR_Enabled=0` and `AllowAutoGameMode=1`
- **Rollback**: Restores exact previous values (deletes if was absent)

### GPU_PREFERENCE_HIGH_PERFORMANCE
- **Category**: Performance
- **Risk**: Low
- **Admin**: No
- **Description**: Sets `GpuPreference=2` (high performance) for a specific game executable in `HKCU\Software\Microsoft\DirectX\UserGpuPreferences`. Only applies when ≥2 GPUs detected.
- **Compatibility**: Multi-GPU systems, Windows 10/11
- **Preview**:
  - "Será definido 'GpuPreference=2;' (alto desempenho) para: <executable>"
  - "Altera apenas o Registro do usuário (HKCU), sem necessidade de admin."
  - "Reversível: o valor anterior será restaurado no rollback."
- **Apply**: Sets registry value for target executable
- **Verify**: Confirms `GpuPreference=2;` for target exe
- **Rollback**: Restores previous value or deletes if was absent

## CS2 Actions

### CS2_CONFIG_PROFILE
- **Category**: GameSpecific (CS2)
- **Risk**: Moderate
- **Admin**: No
- **Description**: Applies a predefined configuration profile (Safe or Competitive) to `client.cfg`. Uses merge semantics: preserves comments, unknown keys, and personal binds.
- **Compatibility**: CS2 installed via Steam (AppID 730)
- **Profiles**:
  - **Safe**: Disables post-processing, bloom, motion blur; keeps shadows low-medium; V-Sync off; texture level -1.
  - **Competitive**: All Safe settings + minimum texture/picmip/AA; shadows disabled; particle detail minimal; queue mode 2.
- **Preview**: Lists all settings that will be changed, notes that personal binds are preserved.
- **Apply**: 
  1. Locates `client.cfg` near game executable
  2. Creates backup with SHA-256 in session folder
  3. Parses existing config (preserves comments/unknown keys)
  4. Merges profile settings (known keys updated, unknown preserved)
  5. Atomic write (temp → flush → replace)
- **Verify**: Confirms file exists and is well-formed
- **Rollback**: Restores backup file atomically

### CS2_LAUNCH_OPTIONS
- **Category**: GameSpecific (CS2)
- **Risk**: Low
- **Admin**: No
- **Description**: Manages safe launch options in Steam's `appmanifest_730.acf`. Only validated options: `-novid` (skip intro), `-fps_max <n>` (cap FPS).
- **Compatibility**: CS2 installed via Steam
- **Preview**:
  - "As opções serão escritas no arquivo de configuração do Steam (appmanifest_730.acf)."
  - "Se a gravação falhar, o aplicativo fornecerá as opções para copiar manualmente."
  - "Opções seguras: -novid, -fps_max <valor>."
- **Apply**: Updates or appends `LaunchOptions` in manifest
- **Rollback**: Advises user to reset via Steam UI (not automated due to Steam Cloud sync complexity)

## Action Cards (UI)

Each action renders as a card with:
- **Title** + Risk Badge (green/amber/red)
- **Admin Badge** (orange) if `RequiresAdministrator`
- **Description**
- **Expected Benefit** (italic, green)
- **Current State** (detected via `DetectCurrentStateAsync`)
- **Buttons**:
  - "Simular alterações" → Shows `PreviewAsync` dialog
  - "Aplicar com segurança" → Consent dialog → `ApplyWithConsentAsync` → Result dialog

## Compatibility Tags

| Tag | Meaning |
|-----|---------|
| `Windows` | All supported Windows versions |
| `Windows10` | Windows 10 only |
| `Windows11` | Windows 11 only |
| `MultiGpu` | Requires ≥2 GPUs |
| `CS2` | Counter-Strike 2 |
| `PowerPlan:HighPerformance` | Requires high-performance power plan as base |

## Future Actions (Planned)

| Action | Category | Notes |
|--------|----------|-------|
| `DISABLE_FULLSCREEN_OPTIMIZATIONS` | Performance | Per-exe, documented API |
| `HAGS_TOGGLE` | Experimental | Hardware-Accelerated GPU Scheduling |
| `DISABLE_BACKGROUND_APPS` | Process | User-selected apps, session-scoped |
| `CS2_CUSTOM_PROFILE` | GameSpecific | User-defined profile with per-key editor |
| `CS2_VIDEO_TXT` | GameSpecific | video.txt parsing (resolution, refresh rate) |
| `STEAM_CLOUD_CONFLICT_WARNING` | GameSpecific | Detect & warn on Steam Cloud sync issues |
# ForgeFPS - Safety Model

## Threat Model

### Assets
1. **User's system integrity** - Windows registry, files, power plans
2. **Game configurations** - CS2 client.cfg, Steam launch options
3. **User privacy** - No telemetry, no personal data collection
4. **Application integrity** - Code signing, update validation

### Trust Boundaries
```
┌─────────────────────────────────────────────────────────────┐
│                    User Session (Medium IL)                 │
│  ┌─────────────────┐    ┌─────────────────────────────┐    │
│  │  ForgeFPS.App   │───►│  OptimizationSessionService  │    │
│  │  (WinUI 3 UI)   │    │  (Transaction Engine)        │    │
│  └─────────────────┘    └──────────────┬──────────────┘    │
│                                        │                   │
│                                        ▼                   │
│  ┌─────────────────────────────────────────────────────┐  │
│  │              ForgeFPS.Infrastructure.Windows         │  │
│  │  (Registry, FileSystem, PowerPlanProvider, Actions)  │  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                        │
                    ┌───────────────────┴───────────────────┐
                    ▼                                       ▼
           ┌─────────────────────┐              ┌─────────────────────┐
           │   Windows Registry  │              │     File System     │
           │    (HKCU / HKLM)    │              │   (User directories)│
           └─────────────────────┘              └─────────────────────┘
                    │                                       │
                    ▼                                       ▼
           ┌─────────────────────┐              ┌─────────────────────┐
           │   powercfg.exe      │              │    Steam Files      │
           │   (Elevated)        │              │   (client.cfg,      │
           │                     │              │    appmanifest)     │
           └─────────────────────┘              └─────────────────────┘
```

### Elevated Helper Boundary
- Separate process: `ForgeFPS.ElevatedHost.exe`
- Launched on-demand with `runas` (UAC prompt)
- Named Pipe IPC with:
  - Protocol version + Request ID
  - Ephemeral token (5 min TTL, single-use)
  - JSON schema versioned
  - No polymorphic deserialization
  - ACL: Current User + Administrators only
- **Does NOT** accept arbitrary commands, paths, or PowerShell

## Risk Classification

| Risk Level | Criteria | Examples |
|------------|----------|----------|
| **Low** | Read-only or HKCU changes, reversible, no restart | Game Mode, GPU preference, CS2 launch options |
| **Moderate** | HKLM changes, requires admin, may need restart | Power plan creation/activation |
| **Experimental** | Unverified behavior, potential instability | (None in MVP - reserved for future) |

## Action Safety Checklist (Per Action)

Before any action is added to catalog:

- [ ] **Allowlisted** - Explicitly registered in DI + `OptimizationCatalog`
- [ ] **Immutable ID** - `string Id` + `int SchemaVersion`
- [ ] **Compatibility Check** - `CheckCompatibilityAsync` validates hardware/OS
- [ ] **Preview** - `PreviewAsync` shows exact changes without applying
- [ ] **Snapshot** - `ApplyAsync` captures `ActionSnapshot` before change
- [ ] **Verify** - `VerifyAsync` confirms change took effect
- [ ] **Rollback** - `RollbackAsync` restores exact prior state
- [ ] **Idempotent** - Re-applying doesn't duplicate/corrupt
- [ ] **Fail-Safe** - Partial failure triggers rollback of applied actions
- [ ] **Consent** - `IApplyContext.IsConsented` must be true for `ApplyAsync`
- [ ] **No Admin by Default** - `RequiresAdministrator = false` unless justified

## Specific Action Risk Assessments

### CREATE_GAMING_POWER_PLAN
- **Risk**: Moderate (admin, HKLM, powercfg)
- **Mitigations**:
  - Duplicates existing plan, never deletes user plans
  - Exports current plan before change
  - Rollback restores previous active plan
  - Does NOT delete the gaming plan on rollback (user may want it)

### WINDOWS_GAME_MODE
- **Risk**: Low
- **Mitigations**:
  - HKCU only (per-user, no admin)
  - Only modifies documented keys: `GameDVR_Enabled`, `AllowAutoGameMode`
  - Full rollback via snapshot
  - Clear warning: "Disables background recording (Win+Alt+R)"

### GPU_PREFERENCE_HIGH_PERFORMANCE
- **Risk**: Low
- **Mitigations**:
  - Only applies when ≥2 GPUs detected
  - HKCU `DirectX\UserGpuPreferences` per executable
  - No effect on single-GPU systems
  - Clear note: "Only for hybrid GPU systems"

### CS2_CONFIG_PROFILE
- **Risk**: Moderate (modifies game config)
- **Mitigations**:
  - Parser preserves comments & unknown keys (merge semantics)
  - Atomic write (temp → flush → replace)
  - Backup created in session folder with SHA-256
  - Rollback restores exact file
  - Never deletes config file

### CS2_LAUNCH_OPTIONS
- **Risk**: Low
- **Mitigations**:
  - Only writes `-novid`, `-fps_max <n>` (validated safe options)
  - Never writes `-high`, `-threads`, `-freq`, `cl_forcepreload`
  - Never enables `-allow_third_party_software`
  - If Steam manifest write fails, shows options for manual copy

## Data Handling

### No Telemetry by Default
- `Telemetry` setting default: `false`
- No crash reporting, no usage analytics
- All data stays local

### Path Sanitization
- `PathValidator.Canonicalize(path, allowedRoot)` throws on:
  - Relative paths
  - Null bytes
  - Paths outside allowed root
  - Symlinks/junctions (`FileAttributes.ReparsePoint`)

### File Operations
- `AtomicWriter.WriteAtomic()`:
  1. Write to temp file (`Path.GetTempFileName()`)
  2. `Flush()` + close
  3. Delete target if exists
  4. Move temp → target
  5. On any failure: delete temp, re-throw

### Registry Snapshots
- Captures: `RegistryValueType` (String, DWord, QWord, Binary, MultiString, None)
- Records `WasPresent` flag (distinguishes "value=0" from "value absent")
- Rollback: `WasPresent=false` → `DeleteValue`, else `SetValue(oldValue)`

## Update Security (Planned)

### Requirements
- Manifest via HTTPS (TLS 1.2+)
- Package signed (Authenticode)
- Signature verified before install
- Rollback to previous version on failure
- Channels: Stable / Beta

### Not in MVP
- Auto-update disabled until signed publication exists
- `IUpdateService` interface exists, implementation stubbed

## Compliance

### Windows Security Features Respected
- ✅ UAC (elevation only when needed)
- ✅ Secure Boot (no driver)
- ✅ Windows Defender (not disabled)
- ✅ Windows Update (not disabled)
- ✅ SmartScreen (signed binary)
- ✅ VAC/Trusted Mode (no injection, no bypass)

### GDPR / LGPD
- No personal data collected
- No identifiers stored (Steam IDs sanitized in logs)
- Local-only operation
- User controls backup location & retention
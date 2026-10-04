# ForgeFPS - Architecture Document

## Overview

ForgeFPS is a Windows desktop application for game optimization, built with C#/.NET 8, WinUI 3, and Windows App SDK. The architecture follows Clean Architecture principles with clear separation between domain, application, infrastructure, and presentation layers.

## Solution Structure

```
ForgeFPS.sln
├── src/
│   ├── ForgeFPS.App/                    # WinUI 3 presentation layer
│   ├── ForgeFPS.Domain/                 # Pure domain models & contracts
│   ├── ForgeFPS.Contracts/              # IPC DTOs & versioned schemas
│   ├── ForgeFPS.Application/            # Use cases, transaction engine, services
│   ├── ForgeFPS.Infrastructure.Windows/ # Windows-specific implementations
│   ├── ForgeFPS.ElevatedHost/           # Minimal elevated helper process
│   ├── ForgeFPS.Games.Abstractions/     # Game module contracts
│   ├── ForgeFPS.Games.CS2/              # CS2-specific module
│   └── ForgeFPS.Benchmarking/           # Performance capture & comparison
├── tests/
│   ├── ForgeFPS.Domain.Tests/
│   ├── ForgeFPS.Application.Tests/
│   ├── ForgeFPS.Benchmarking.Tests/
│   └── ForgeFPS.Games.CS2.Tests/
└── docs/
```

## Core Concepts

### 1. Optimization Actions (`IOptimizationAction`)

Every optimization is an independent, auditable action implementing:

```csharp
interface IOptimizationAction {
    string Id { get; }
    int SchemaVersion { get; }
    string DisplayName { get; }
    string Description { get; }
    OptimizationCategory Category { get; }
    RiskLevel Risk { get; }
    bool RequiresAdministrator { get; }
    bool RequiresRestart { get; }
    bool IsTemporary { get; }
    IReadOnlyList<string> CompatibilityTags { get; }
    string? ExpectedBenefit { get; }

    Task<CompatibilityResult> CheckCompatibilityAsync(IDetectionContext);
    Task<CurrentState> DetectCurrentStateAsync(IDetectionContext);
    Task<ActionPreview> PreviewAsync(IApplyContext);
    Task<ActionResult> ApplyAsync(IApplyContext);
    Task<VerificationResult> VerifyAsync(IApplyContext);
    Task<RollbackResult> RollbackAsync(ICollection<ActionSnapshot>, IApplyContext);
}
```

### 2. Transaction Engine (`OptimizationTransactionEngine`)

Executes actions atomically with full rollback support:

1. **Preflight** - Check compatibility for all actions
2. **Preview** - Build complete preview without modifications
3. **Apply** - Execute each action, capture snapshots
4. **Verify** - Confirm each action took effect
5. **Rollback (on failure)** - Reverse actions in reverse order

States: `Planned` → `AwaitingConsent` → `Applying` → `Applied` | `PartiallyApplied` → `RollingBack` → `RolledBack` | `RollbackFailed`

### 3. Snapshots (`ActionSnapshot`)

Every modification is preceded by a snapshot capturing:
- Registry: key path, value name, type, old value, presence flag
- Files: path, SHA-256 hash, size, timestamps
- Power plans: GUID of active plan before change

Snapshots are stored per-session with UTC timestamps for audit trail.

### 4. Contexts

| Context | Purpose | Consent |
|---------|---------|---------|
| `IDetectionContext` | Read-only detection & compatibility | No |
| `IApplyContext` | Real apply with mutable operations | Yes |
| `SimulationContext` | Preview mode, never modifies | No |

## Implemented Actions (Fases 1-5)

| Action ID | Category | Risk | Admin | Description |
|-----------|----------|------|-------|-------------|
| `DETECT_POWER_PLAN` | Energy | Low | No | Read current active power plan |
| `CREATE_GAMING_POWER_PLAN` | Energy | Low | Yes | Duplicate high-perf plan as "ForgeFPS Gaming" |
| `WINDOWS_GAME_MODE` | GameMode | Low | No | Enable Game Mode, disable GameDVR (HKCU) |
| `GPU_PREFERENCE_HIGH_PERFORMANCE` | Performance | Low | No | Set GPU preference per executable (HKCU) |
| `CS2_CONFIG_PROFILE` | GameSpecific | Moderate | No | Apply Safe/Competitive config to client.cfg |
| `CS2_LAUNCH_OPTIONS` | GameSpecific | Low | No | Set -novid, -fps_max in Steam manifest |

## Benchmark System

### Metrics Computed
- **Average FPS** = frames / duration
- **1% Low FPS** = 1000 / P99 frametime
- **0.1% Low FPS** = 1000 / P99.9 frametime
- **Avg FrameTime** = mean frametime
- **P95/P99 FrameTime** = percentiles
- **StdDev** = standard deviation of frametime

### Comparison Logic
- Requires ≥10 frames per run
- Same game required
- Significant if |ΔFPS| > 2% or |ΔFrameTime| > 2%
- Verdicts: "Melhora significativa" / "Inconclusivo"

### PresentMon CSV Parser
- Auto-detects decimal separator (`,` vs `.`)
- Handles quoted fields, trailing comments
- Culture-aware parsing (pt-BR / Invariant)

## Safety Model

### Allowlist Architecture
- Only pre-registered actions can execute
- Each action has immutable `Id` + `SchemaVersion`
- No arbitrary command execution

### Path Safety
- `PathValidator.Canonicalize()` prevents traversal
- Symlink detection via `FileAttributes.ReparsePoint`
- Atomic writes: temp → flush → replace

### Elevated Helper
- Separate process (`ForgeFPS.ElevatedHost.exe`)
- Communicates via Named Pipes with:
  - Protocol version + request ID
  - Ephemeral token (5 min TTL)
  - JSON payloads, strict schema
  - No polymorphic deserialization
- ACL restricted to current user + administrators

### No-Go List (Enforced)
- ❌ No DLL injection
- ❌ No memory manipulation
- ❌ No VAC/Trusted Mode bypass
- ❌ No kernel driver
- ❌ No security feature disabling
- ❌ No arbitrary command execution
- ❌ No remote script download

## Data Persistence

### SQLite (Planned)
- Tables: `AppSettings`, `HardwareSnapshots`, `OptimizationSessions`, `OptimizationActions`, `ActionSnapshots`, `BenchmarkRuns`, `BenchmarkMetrics`, `AppEvents`
- UTC storage, local display
- Migrations versioned

### In-Memory (MVP)
- `HistoryStore` - session history
- `BenchmarkRunner` - benchmark runs
- `InMemoryTransactionLog` - session audit

## Deployment

### CI/CD (GitHub Actions)
```yaml
on: [push, pull_request]
jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - checkout
      - setup-dotnet@8.0.x
      - dotnet restore
      - dotnet build -c Release
      - dotnet test -c Release
```

### Distribution Targets
- **MSIX** for Microsoft Store (auto-signed)
- **Self-contained x64** for direct distribution
- **Code signing** via Azure Key Vault / GitHub Secrets (certificate not in repo)

## Versioning
- Semantic versioning (Major.Minor.Patch)
- Conventional Commits for changelog
- Schema version per action (`SchemaVersion`)

## Extensibility

### Adding a New Game Module
1. Implement `IGameModule` in `src/ForgeFPS.Games.<Game>/`
2. Register in `App.xaml.cs` DI container
2. Add to `OptimizationCatalog` via DI factory
3. Create game-specific `IOptimizationAction`s
4. Add tests in `tests/ForgeFPS.Games.<Game>.Tests/`

### Adding a New Windows Action
1. Create class implementing `IOptimizationAction` in `Infrastructure.Windows/Actions/`
2. Register in `App.xaml.cs` DI + `OptimizationCatalog`
3. Add tests in `Application.Tests/`

## References
- [Windows App SDK / WinUI 3](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- [powercfg command-line](https://learn.microsoft.com/windows-hardware/design/device-experiences/powercfg-command-line-options)
- [PresentMon](https://github.com/GameTechDev/PresentMon)
- [Steam Trusted Mode](https://help.steampowered.com/en/faqs/view/09A0-4879-4353-EF95)
- [Verify Game Files](https://help.steampowered.com/en/faqs/view/0C48-FCBD-DA71-93EB)
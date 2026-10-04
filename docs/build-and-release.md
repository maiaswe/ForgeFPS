# ForgeFPS - Build & Release

## Prerequisites

- **Windows 10/11** (build 19041+)
- **.NET 8 SDK** (8.0.400+)
- **Visual Studio 2022** (17.8+) with:
  - Windows App SDK workload
  - .NET Desktop Development workload
- **Git** for version control

## Build Commands

```bash
# Clone
git clone https://github.com/ForgeFPS/forgefps.git
cd forgefps

# Restore dependencies
dotnet restore ForgeFPS.sln

# Build Release x64
dotnet build ForgeFPS.sln -c Release -p:Platform=x64

# Run tests
dotnet test ForgeFPS.sln -c Release --no-build

# Run application (from src/ForgeFPS.App)
dotnet run --project src/ForgeFPS.App/ForgeFPS.App.csproj -c Release
```

## Project Configuration

### Target Frameworks
| Project | Target |
|---------|--------|
| ForgeFPS.App | net8.0-windows10.0.19041.0 |
| All others | net8.0 |

### Key Properties (ForgeFPS.App.csproj)
```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<Platforms>x64</Platforms>
<PlatformTarget>x64</PlatformTarget>
<OutputType>WinExe</OutputType>
<UseWinUI>true</UseWinUI>
<EnableMsixTooling>true</EnableMsixTooling>
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
<SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
```

### Package Versions (Centralized in Directory.Packages.props)
| Package | Version |
|---------|---------|
| Microsoft.WindowsAppSDK | 1.7.250606001 |
| Microsoft.Windows.SDK.BuildTools | 10.0.26100.1742 |
| Microsoft.Extensions.DependencyInjection | 9.0.3 |
| Microsoft.Extensions.Logging | 9.0.3 |
| CommunityToolkit.Mvvm | 8.2.2 |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.10 |
| Serilog | 4.2.0 |
| Serilog.Sinks.File | 6.0.0 |
| FluentAssertions | 8.0.0 |
| Moq | 4.20.72 |
| xunit | 2.9.2 |

## Build Output

```
artifacts/
├── ForgeFPS.App/
│   ├── bin/Release/net8.0-windows10.0.19041.0/win-x64/
│   │   ├── ForgeFPS.App.exe
│   │   ├── ForgeFPS.*.dll
│   │   ├── WinUI 3 resources
│   │   └── Assets/
│   └── obj/...
├── ForgeFPS.ElevatedHost/
│   └── bin/Release/net8.0/win-x64/
│       └── ForgeFPS.ElevatedHost.exe
└── test-results/
    └── *.trx
```

## MSIX Packaging (Store)

### Prerequisites
- Microsoft Partner Center account
- Certificate (.pfx) from trusted CA or Microsoft Store auto-signing

### Build MSIX
```bash
# From solution root
dotnet publish src/ForgeFPS.App/ForgeFPS.App.csproj \
  -c Release \
  -p:Platform=x64 \
  -p:AppxPackageDir="artifacts/msix" \
  -p:AppxBundle=Never \
  -p:GenerateAppxPackageOnBuild=true
```

### Manual Signing (if not using Store auto-sign)
```bash
signtool sign /fd SHA256 /f certificate.pfx /p <password> \
  /tr http://timestamp.digicert.com /td SHA256 \
  artifacts/msix/ForgeFPS.App_1.0.0.0_x64.msixbundle
```

### AppxManifest (Key Settings)
```xml
<Package>
  <Identity Name="ForgeFPS" Publisher="CN=ForgeFPS" Version="1.0.0.0" />
  <Properties>
    <DisplayName>ForgeFPS</DisplayName>
    <PublisherDisplayName>ForgeFPS Team</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  <Resources>
    <Resource Language="pt-BR" />
    <Resource Language="en-US" />
  </Resources>
  <Capabilities>
    <Capability Name="runFullTrust" />  <!-- Required for registry/powercfg -->
  </Capabilities>
</Package>
```

## Self-Contained Distribution (Direct Download)

```bash
dotnet publish src/ForgeFPS.App/ForgeFPS.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/portable
```

Output: ~150-200 MB folder with all dependencies.

## CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI
on: [push, pull_request]
jobs:
  build-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore ForgeFPS.sln
      - run: dotnet build ForgeFPS.sln -c Release
      - run: dotnet test ForgeFPS.sln -c Release --logger "trx"
```

## Release Checklist

### Pre-Release
- [ ] All tests pass (`dotnet test`)
- [ ] Version bumped in `Directory.Build.props` / `ForgeFPS.App.csproj`
- [ ] Changelog updated (`CHANGELOG.md`)
- [ ] `README.md` version badge updated
- [ ] No debug code, no `TODO` in production paths

### Release
- [ ] Tag created: `git tag v1.0.0`
- [ ] GitHub Release created with:
  - Changelog excerpt
  - MSIX download link (or portable zip)
  - SHA-256 checksums
- [ ] MSIX uploaded to Microsoft Store (if applicable)
- [ ] Portable zip uploaded to GitHub Releases

### Post-Release
- [ ] Verify install on clean Windows 10/11 VM
- [ ] Smoke test: launch → analyze → apply safe action → verify → rollback
- [ ] Monitor crash reports (if telemetry enabled)

## Versioning

### Semantic Versioning
```
MAJOR.MINOR.PATCH
```
- **MAJOR**: Breaking API changes, major UI overhaul
- **MINOR**: New actions, new game modules, significant features
- **PATCH**: Bug fixes, minor improvements, doc updates

### Action Schema Versioning
Each `IOptimizationAction` has `SchemaVersion`:
- Increment on breaking changes to action contract
- UI shows version, can migrate old sessions

### Changelog Format
```markdown
## [1.2.0] - 2025-01-15
### Added
- New action: GPU_PREFERENCE_HIGH_PERFORMANCE
- CS2 Competitive profile

### Fixed
- Parser handles comma decimal separator
- Rollback restores exact registry type

### Changed
- Transaction engine logs session ID in all events
```

## Code Signing

### Certificate Storage
- **Never** commit certificates to repository
- Store in:
  - GitHub Secrets (for CI)
  - Azure Key Vault (for release pipeline)
  - Local developer machine only

### Signing in CI
```yaml
- name: Sign artifacts
  if: github.event_name == 'release'
  run: |
    signtool sign /fd SHA256 /f ${{ secrets.CERT_FILE }} /p ${{ secrets.CERT_PASSWORD }} \
      /tr http://timestamp.digicert.com /td SHA256 \
      artifacts/portable/ForgeFPS.App.exe
```

## Troubleshooting Build

| Issue | Solution |
|-------|----------|
| `XamlCompiler.exe` failed | Install Windows App SDK workload in VS Installer; ensure .NET 8 SDK |
| `MSB4126: Release|x64 invalid` | Build with `-p:Platform=x64` or use `Any CPU` for non-WinUI projects |
| `NU1603` version conflict | Update `Directory.Packages.props` to latest compatible versions |
| Test discovery fails | Ensure `Microsoft.NET.Test.Sdk` ≥17.12, xunit ≥2.9 |
| `CA1416` platform warnings | Expected for Windows-only APIs (Registry, powercfg); suppress or `#if WINDOWS` |

## Performance Notes

- **Startup**: <2s cold start on SSD (WinUI 3 + self-contained)
- **Memory**: ~80-120 MB typical
- **Disk**: ~150 MB installed (self-contained)
- **Admin prompt**: Only for actions with `RequiresAdministrator=true`
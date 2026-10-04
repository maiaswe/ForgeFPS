# ForgeFPS - Troubleshooting

## Common Issues

### Application Won't Start

| Symptom | Cause | Solution |
|---------|-------|----------|
| "This app can't run on your PC" | Architecture mismatch | Ensure x64 build on 64-bit Windows |
| Missing `WindowsAppRuntime` | WinUI 3 dependency missing | Install Windows App SDK 1.7+ via winget: `winget install Microsoft.WindowsAppRuntime.1.7` |
| `FileNotFoundException` for DLLs | Self-contained publish missing files | Use `dotnet publish --self-contained -r win-x64` |
| UAC prompt doesn't appear | ElevatedHelper not registered | Run as Admin once, or install MSIX |

### Actions Not Applying

| Issue | Cause | Fix |
|-------|-------|-----|
| "Consentimento não concedido" | User clicked Cancel | Click "Aplicar" in consent dialog |
| "Privilégios de administrador necessários" | Action needs Admin | Click "Aplicar", approve UAC prompt |
| "Plano base de alto desempenho não encontrado" | No High Performance plan | Create one in Control Panel → Power Options |
| "CS2 não detectado" | Steam/CS2 not in standard locations | Verify Steam install; check `libraryfolders.vdf` |
| "client.cfg não encontrado" | Wrong CS2 install path | Verify `game/csgo/cfg/client.cfg` exists |

### Benchmark Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| "CSV vazio" | PresentMon didn't capture | Run PresentMon with correct process name: `PresentMon.exe -process_name cs2` |
| "Coluna FrameTime_ms ausente" | Wrong CSV format | Use PresentMon v3+ CSV output |
| "Melhora inconclusiva" | Run variance > 2% | Increase capture duration (60s+), close background apps |
| FPS implausivelmente alto | CSV com separador errado | Parser auto-detecta, mas force pt-BR se necessário |

### Elevated Helper (ForgeFPS.ElevatedHost.exe)

| Error | Meaning |
|-------|---------|
| "Access denied" | Pipe ACL mismatch — restart app as same user |
| "Token expired" | Request took >5 min — retry |
| "Invalid protocol version" | Version mismatch — update app + helper together |
| Helper crashes | Check Windows Event Viewer → Application logs |

## Logs & Diagnostics

### Log Location
```
%LOCALAPPDATA%\ForgeFPS\logs\forgefps_YYYYMMDD.log
```

### Log Levels
- **Debug**: Verbose internal state
- **Information**: Normal operations (detect, apply, verify)
- **Warning**: Non-fatal issues (fallback paths, missing optional keys)
- **Error**: Failed actions, rollback failures, exceptions

### Enable Debug Logging
Edit `%LOCALAPPDATA%\ForgeFPS\appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ForgeFPS": "Debug"
    }
  }
}
```

### Export Support Package
UI: **Diagnóstico → Exportar pacote de suporte**
Creates ZIP with:
- Sanitized logs (no usernames, paths truncated)
- Hardware snapshot
- Action compatibility matrix
- Recent session history (last 50)
- App version + .NET runtime info

## Known Limitations

| Limitation | Workaround |
|------------|------------|
| Steam Cloud overwrites local config | Disable Cloud Sync for CS2 in Steam Properties, or re-apply after sync |
| PresentMon not bundled | Manual download from GitHub Releases |
| No auto-update | Check GitHub Releases manually |
| No Linux/macOS support | Windows-only by design |
| No overlay/FPS counter | Use PresentMon / RTSS / Steam FPS counter |
| Benchmark needs manual PresentMon run | Automate via CLI in future version |

## Debugging Tips

### Enable Verbose Logging
```powershell
$env:FORGEFPS_LOG_LEVEL = "Debug"
dotnet run --project src/ForgeFPS.App/ForgeFPS.App.csproj
```

### Inspect Snapshots
```powershell
# Session backups
ls "$env:LOCALAPPDATA\ForgeFPS\backups\<session-id>\"
```

### Test Action in Isolation
```csharp
// In Immediate Window or test
var action = serviceProvider.GetRequiredService<CreateGamingPowerPlanAction>();
var context = new ApplyContext(...);
var result = await action.ApplyAsync(context);
```

### Verify Registry Changes
```powershell
# Game Mode
reg query "HKCU\System\GameConfigStore" /v GameDVR_Enabled
reg query "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode

# GPU Preference
reg query "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "C:\path\to\cs2.exe"
```

### Verify Power Plan
```powershell
powercfg /list
powercfg /getactivescheme
```

## Reporting Bugs

### Required Information
1. ForgeFPS version (About dialog)
2. Windows version (`winver`)
3. Hardware (CPU, GPU, RAM)
4. Steps to reproduce
5. Expected vs actual behavior
6. Relevant log excerpt (Diagnóstico → Exportar)
6. Screenshot if UI issue

### Where to Report
- GitHub Issues: `https://github.com/ForgeFPS/forgefps/issues`
- Template: Bug Report / Feature Request / Documentation

## Advanced: Manual Action Testing

```powershell
# Test power plan creation (requires Admin)
powercfg /duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
powercfg /changename <new-guid> "ForgeFPS Gaming" "Teste"
powercfg /setactive <new-guid>

# Test Game Mode
reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 1 /f

# Test GPU Preference
reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v "C:\Games\CS2\game\bin\client_windows64.exe" /t REG_SZ /d "GpuPreference=2;" /f

# Verify CS2 config
type "%USERPROFILE%\AppData\Local\Steam\userdata\<id>\730\local\cfg\video.txt"
```
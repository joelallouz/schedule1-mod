# Testing

## Prerequisites

1. **Schedule I** installed via Steam (Windows PC)
2. **MelonLoader 0.6.x** installed into the game directory
   - Download from https://github.com/LavaGang/MelonLoader/releases
   - Run the installer targeting your Schedule I directory
   - Launch the game once to generate `MelonLoader/` subdirectory with DLLs
3. **.NET 6.0+ SDK** on the build machine (macOS)
   - Install: `brew install dotnet@6`
   - Verify: `dotnet --version`
4. **SSH access** to the Windows PC (`jlall@192.168.1.141`)
   - OpenSSH Server enabled on Windows
   - Key-based auth configured (no password prompts)

## Build & Deploy (one command)

```bash
./deploy.sh
```

This builds the project and pushes the DLL to the Windows PC's `Mods` folder via SCP.

## Build Only (no deploy)

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:CopyToMods=false
```

References resolve from the flat `libs/` directory. Required DLLs in `libs/`:
- `MelonLoader.dll`
- `0Harmony.dll`
- `Il2CppInterop.Runtime.dll`
- `Il2Cppmscorlib.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.InputLegacyModule.dll`

Output: `bin/Debug/net6.0/ClientAssignmentOptimizer.dll`

## Pulling Logs

```bash
./deploy.sh --logs
```

Saves the PC's `Latest.log` to `./Latest.log` in the project directory.

## Live Tailing Logs

```bash
./deploy.sh --tail
```

Builds, deploys, then live-streams the log file. Press Ctrl+C to stop tailing.

## Running

1. Launch Schedule I on Windows
2. MelonLoader console window appears alongside the game
3. Look for `[ClientOptimizer]` lines in the console
4. Press F9 to toggle the customer panel, F10 to refresh data

## Key Paths (Windows PC)

| Path | Purpose |
|---|---|
| `C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods` | DLL goes here |
| `C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Latest.log` | Current session log |
| `C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Logs` | Historical logs |

## Troubleshooting

| Symptom | Likely Cause |
|---|---|
| `deploy.sh` can't connect | Check SSH: `ssh jlall@192.168.1.141 "echo ok"` |
| No MelonLoader console | MelonLoader not installed. Re-run installer. |
| No `[ClientOptimizer]` lines | DLL missing from `Mods/` or wrong .NET version. |
| 0 matching types in search | Assembly names may differ from expected. Check assembly list in log. |
| Type shapes show 0 fields/props | IL2CPP reflection may not expose members. May need Il2CppInterop-specific reflection. |

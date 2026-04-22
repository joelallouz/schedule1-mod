# Testing

## Prerequisites

1. **Schedule I** installed via Steam
2. **MelonLoader 0.6.x** installed into the game directory
   - Download from https://github.com/LavaGang/MelonLoader/releases
   - Run the installer and point it at your Schedule I installation
   - After first launch with MelonLoader, the `MelonLoader/` subdirectory will be populated with managed DLLs
3. **.NET 6.0 SDK** installed (for building)

## Building

### Option A: Command Line

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
```

Replace the `GameDir` path with your actual game installation directory.

If `CopyToMods` is `true` (default), the built DLL is automatically copied to `<GameDir>\Mods\`.

### Option B: IDE

Open `ClientAssignmentOptimizer.csproj` in Visual Studio or Rider. Set the `GameDir` property in the `.csproj` to your game path, then build normally.

### Build Without Game Installed

If you're developing on a machine without the game (e.g., macOS), you can copy the required reference DLLs to a local folder and point `GameDir` there. You need at minimum:
- `MelonLoader/net6/MelonLoader.dll`
- `MelonLoader/Il2CppAssemblies/Il2Cppmscorlib.dll`
- `MelonLoader/net6/Il2CppInterop.Runtime.dll`

## Deploying

If auto-copy is disabled, manually copy the built DLL:

```
cp bin/Debug/net6.0/ClientAssignmentOptimizer.dll "<GameDir>\Mods\"
```

## Verifying the Mod Loads

1. Launch Schedule I with MelonLoader installed
2. The MelonLoader console window should appear (separate from the game window)
3. Look for these log lines in the console:

```
[ClientOptimizer] ========================================
[ClientOptimizer] Client Assignment Optimizer v0.1.0
[ClientOptimizer] ========================================
[ClientOptimizer] Debug logging: ON
[ClientOptimizer] Discovery mode: ON
[ClientOptimizer] === Discovery Phase Starting ===
[ClientOptimizer] --- Loaded Assemblies ---
[ClientOptimizer] Total loaded assemblies: <number>
[ClientOptimizer] Game-related assemblies found: <number>
[ClientOptimizer]   [GAME] <assembly names...>
[ClientOptimizer] --- Type Counts (Game Assemblies) ---
[ClientOptimizer]   <assembly>: <number> types
[ClientOptimizer] === Discovery Phase Complete ===
[ClientOptimizer] Initialization complete.
```

4. If you see `Initialization complete.` the mod is working.

## Troubleshooting

| Symptom | Likely Cause |
|---|---|
| No MelonLoader console appears | MelonLoader not installed correctly. Re-run installer. |
| Console appears but no `[ClientOptimizer]` lines | DLL not in `Mods/` folder, or build failed silently. Check `Mods/` contains `ClientAssignmentOptimizer.dll`. |
| Mod logs but discovery shows 0 game assemblies | Assembly filter in `ReflectionUtils` may be too aggressive. Check debug log for full assembly list. |
| `MelonGame` attribute warning | The company/game name may not match. Try `[assembly: MelonGame(null, null)]` as a fallback to load for any game. |
| Build fails with missing references | MelonLoader DLLs not found at `GameDir` path. Check path and ensure MelonLoader was run at least once. |

## Log File Location

MelonLoader writes logs to:
```
<GameDir>\MelonLoader\Latest.log
```

This file contains the same output as the console window and persists after the game closes. Useful for capturing discovery output to paste into FINDINGS.md.

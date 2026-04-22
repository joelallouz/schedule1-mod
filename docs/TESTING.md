# Testing

## Prerequisites

1. **Schedule I** installed via Steam (Windows)
2. **MelonLoader 0.6.x** installed into the game directory
   - Download from https://github.com/LavaGang/MelonLoader/releases
   - Run the installer and point it at your Schedule I installation
   - **Important:** Launch the game once with MelonLoader before trying to build the mod. The first launch generates the `MelonLoader/` subdirectory with managed DLLs that the build needs as references.
3. **.NET 6.0 SDK** installed (for building)
   - Verify with: `dotnet --version` (should be 6.x or higher)

## Building

### On a Windows machine with the game installed

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
```

Replace the `GameDir` path with your actual game installation directory.

If `CopyToMods` is `true` (default), the built DLL is automatically copied to `<GameDir>\Mods\`.

### Compile-only (no game installed, e.g., macOS dev machine)

Copy these three DLLs from a machine that has MelonLoader installed into a local `libs/` folder, mirroring the expected directory structure:

```
libs/
  MelonLoader/
    net6/
      MelonLoader.dll
      Il2CppInterop.Runtime.dll
    Il2CppAssemblies/
      Il2Cppmscorlib.dll
```

Then build with:

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="./libs" -p:CopyToMods=false
```

This verifies the code compiles but won't attempt to copy the DLL anywhere.

### IDE

Open `ClientAssignmentOptimizer.csproj` in Visual Studio or Rider. Either edit `GameDir` in the `.csproj` or set it as an MSBuild property in your IDE's build settings.

## Deploying

If auto-copy is disabled or you built on a different machine, manually copy the DLL:

```
copy bin\Debug\net6.0\ClientAssignmentOptimizer.dll "<GameDir>\Mods\"
```

The `Mods\` folder should already exist after installing MelonLoader. If it doesn't, create it.

## Verifying the Mod Loads

1. Launch Schedule I (the game, not the MelonLoader installer)
2. A MelonLoader console window should appear alongside the game window
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
[ClientOptimizer]   [GAME] <assembly names will appear here>
[ClientOptimizer] --- Type Counts (Game Assemblies) ---
[ClientOptimizer]   <assembly>: <number> types
[ClientOptimizer] === Discovery Phase Complete ===
[ClientOptimizer] Initialization complete.
```

4. If you see `Initialization complete.` — the mod loaded and ran successfully.
5. With `DebugLogging = true`, you'll also see individual assembly names between the "Total loaded" and "Game-related" lines.

## Troubleshooting

| Symptom | Likely Cause |
|---|---|
| No MelonLoader console appears | MelonLoader not installed correctly. Re-run the installer targeting your Schedule I directory. |
| Console appears but no `[ClientOptimizer]` lines | DLL not in `Mods/` folder, or DLL was built against wrong .NET version. Verify `ClientAssignmentOptimizer.dll` exists in `<GameDir>\Mods\`. |
| Mod logs but discovery shows 0 game assemblies | Assembly filter in `ReflectionUtils` may be too aggressive. Set `DebugLogging = true` and check the full assembly list in the log. |
| `MelonGame` attribute warning / mod doesn't load | The company/game name in `[assembly: MelonGame("TVGS", "Schedule I")]` may not match. Try `[assembly: MelonGame(null, null)]` as a fallback (loads for any game). |
| Build fails: "Could not resolve reference" | MelonLoader DLLs not found at the `GameDir` path. Verify the path, and make sure you launched the game once with MelonLoader installed (it generates the DLLs on first run). |
| Build fails: duplicate compile items | If you edited the `.csproj`, make sure `EnableDefaultCompileItems` is set to `false`. SDK-style projects auto-include `*.cs` files otherwise. |

## Log File Location

MelonLoader writes a persistent log to:

```
<GameDir>\MelonLoader\Latest.log
```

This contains the same output as the console window and survives after the game closes. Copy the contents of this file to paste into future Claude sessions or into `docs/FINDINGS.md`.

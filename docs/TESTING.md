# Testing

## Prerequisites

1. **Schedule I** installed via Steam (Windows PC)
2. **MelonLoader 0.6.x** installed into the game directory
   - Download from https://github.com/LavaGang/MelonLoader/releases
   - Run the installer targeting your Schedule I directory
   - Launch the game once to generate `MelonLoader/` subdirectory with DLLs
3. **.NET 6.0+ SDK** on the build machine (macOS or Windows)
   - Verify: `dotnet --version`

## Building (macOS)

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:CopyToMods=false
```

References are resolved from the flat `libs/` directory. Required DLLs in `libs/`:
- `MelonLoader.dll`
- `0Harmony.dll`
- `Il2CppInterop.Runtime.dll`
- `Il2Cppmscorlib.dll`

Output: `bin/Debug/net6.0/ClientAssignmentOptimizer.dll`

## Building (Windows, with game)

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
```

With `CopyToMods=true` (default), the DLL is auto-copied to `<GameDir>\Mods\`.

## Deploy (macOS → Windows)

1. Build on Mac
2. Copy `bin/Debug/net6.0/ClientAssignmentOptimizer.dll` to the Windows PC
3. Place it in `<GameDir>\Mods\` (overwrite existing if present)

## Running

1. Launch Schedule I on Windows
2. MelonLoader console window appears alongside the game
3. Look for `[ClientOptimizer]` lines in the console

## Expected Log Output (Session 2 — Type Search)

```
[ClientOptimizer] ========================================
[ClientOptimizer] Client Assignment Optimizer v0.1.0
[ClientOptimizer] ========================================
[ClientOptimizer] Debug logging: ON
[ClientOptimizer] Discovery mode: ON
[ClientOptimizer] === Discovery Phase Starting ===
[ClientOptimizer] --- Loaded Assemblies ---
[ClientOptimizer] Total loaded assemblies: ~235
[ClientOptimizer] Game-related assemblies found: <N>
[ClientOptimizer]   [GAME] Assembly-CSharp
[ClientOptimizer]   [GAME] <others...>
[ClientOptimizer] --- Type Counts (Game Assemblies) ---
[ClientOptimizer]   Assembly-CSharp: ~3705 types
[ClientOptimizer]   <others...>
[ClientOptimizer] === Targeted Type Search ===
[ClientOptimizer]   Keywords: Client, Customer, Dealer, Assign, ...
[ClientOptimizer]   Scanning <N> assemblies:
[ClientOptimizer]     Assembly-CSharp (<N> types)
[ClientOptimizer]     Il2CppScheduleOne.Core (<N> types)
[ClientOptimizer]   Total matching types: <N>
[ClientOptimizer] --- Matches by Keyword ---
[ClientOptimizer]   [Client] (<N> types):
[ClientOptimizer]     Namespace.ClassName : BaseType (in AssemblyName)
[ClientOptimizer]   [Dealer] (<N> types):
[ClientOptimizer]     ...
[ClientOptimizer] --- Full Type Shapes (<N> high-priority types) ---
[ClientOptimizer] --- Type Shape: Namespace.ClassName ---
[ClientOptimizer]   Base type: ...
[ClientOptimizer]   Public fields (...):
[ClientOptimizer]     ...
[ClientOptimizer]   Public properties (...):
[ClientOptimizer]     ...
[ClientOptimizer] === Targeted Type Search Complete ===
[ClientOptimizer] === Discovery Phase Complete ===
[ClientOptimizer] Initialization complete.
```

## What To Capture

Copy the **entire** contents of `<GameDir>\MelonLoader\Latest.log` after running.

The critical sections for Session 2 analysis:
1. Everything between `=== Targeted Type Search ===` and `=== Targeted Type Search Complete ===`
2. All `[MATCH]` lines and type shape dumps
3. Any warnings or errors

## Troubleshooting

| Symptom | Likely Cause |
|---|---|
| No MelonLoader console | MelonLoader not installed. Re-run installer. |
| No `[ClientOptimizer]` lines | DLL missing from `Mods/` or wrong .NET version. |
| 0 matching types in search | Assembly names may differ from expected. Check "Scanning N assemblies" line. |
| 0 assemblies scanned | Target assembly prefixes don't match. Full assembly list (Debug) will show actual names. |
| Type shapes show 0 fields/props | IL2CPP reflection may not expose members. May need Il2CppInterop-specific reflection. |

## Log File

```
<GameDir>\MelonLoader\Latest.log
```

Persists after game closes. This is the file to copy back to Mac.

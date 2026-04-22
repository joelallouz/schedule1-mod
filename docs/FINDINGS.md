# Findings

This document tracks what we've confirmed, what we suspect, and what remains unknown about Schedule I's internals — specifically around client data, assignments, and spending.

**Update this file every session with new discoveries. Cite evidence (log output, class names, field names) for everything in the Confirmed section.**

---

## Confirmed

### Runtime Environment (Session 1)
- **Game type:** IL2CPP
- **Unity version:** 2022.3.62f2
- **Game version:** 0.4.5f2
- **MelonGame attribute:** `("TVGS", "Schedule I")` — loads correctly
- **Evidence:** MelonLoader console output on first successful mod load

### Assembly Landscape (Session 1)
- **Total loaded assemblies:** ~235
- **Assembly-CSharp:** present, ~3705 types — this is the primary game logic assembly
- **Il2CppScheduleOne.Core:** present, ~46 types — game-specific namespace under Il2Cpp
- **Many Il2Cpp\* assemblies:** present (framework wrappers, third-party libs, and game-specific)
- **Evidence:** RuntimeScanService.LogLoadedAssemblies() output

### Build System (Session 1)
- **macOS build works** with flat `libs/` directory containing: MelonLoader.dll, 0Harmony.dll, Il2CppInterop.Runtime.dll, Il2Cppmscorlib.dll
- **DLL deploys successfully** via manual copy to Windows `<GameDir>\Mods\`
- **Evidence:** `dotnet build` succeeds with 0 warnings, 0 errors; mod loads in game

---

## Suspected

- Game likely has dedicated client/customer classes in Assembly-CSharp or Il2CppScheduleOne.*
- Assignment is probably a reference to a dealer object or the player, or an enum/flag
- "Weekly spend" may be computed from transaction history rather than stored directly
- Client data is probably accessible through a singleton manager or static collection
- `Il2CppScheduleOne.Core` may contain base types or managers; domain-specific types may be in other `Il2CppScheduleOne.*` sub-assemblies

_These are educated guesses. None have been verified yet. Session 2 discovery scan will test these._

---

## Unknown

See [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) for the full list of unknowns.

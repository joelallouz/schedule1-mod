# Architecture

## Project Structure

```
schedule1-mod/
├── ClientAssignmentOptimizer.csproj    # Build configuration
├── CLAUDE.md                           # AI session context (read this first)
├── README.md                           # Project overview
├── .gitignore
│
├── libs/                               # Reference DLLs for macOS builds (gitignored)
│   ├── MelonLoader.dll
│   ├── 0Harmony.dll
│   ├── Il2CppInterop.Runtime.dll
│   └── Il2Cppmscorlib.dll
│
├── src/
│   ├── Core/                           # Mod lifecycle and infrastructure
│   │   ├── ModEntry.cs                 # MelonMod entry point
│   │   ├── ModLogger.cs               # Centralized logging
│   │   └── ModConfig.cs               # Configuration flags
│   │
│   ├── Discovery/                      # Reverse engineering tools (temporary)
│   │   ├── DiscoveryOrchestrator.cs    # Entry point for all scans
│   │   ├── RuntimeScanService.cs       # Assembly and type scanning
│   │   ├── TypeSearchService.cs        # Keyword-based type search in game assemblies
│   │   ├── ReflectionUtils.cs          # Safe reflection helpers
│   │   └── DumpUtils.cs               # Structured type/value dumping
│   │
│   ├── Domain/                         # Data models (future — Phase 2)
│   ├── Services/                       # Business logic (future — Phase 2)
│   ├── Patches/                        # Harmony patches (future — Phase 2+)
│   └── UI/                             # In-game interface (future — Phase 2)
│
└── docs/                               # Persistent documentation
    ├── PRD.md                          # Product requirements
    ├── ROADMAP.md                      # Phased development plan
    ├── SESSION_LOG.md                  # Per-session work record
    ├── FINDINGS.md                     # Confirmed/suspected game internals
    ├── OPEN_QUESTIONS.md               # Tracked unknowns
    ├── ARCHITECTURE.md                 # This file
    └── TESTING.md                      # Build, deploy, and verify instructions
```

## Dual-Machine Workflow

| Machine | Role | Tools |
|---|---|---|
| macOS | Code editing, builds | dotnet SDK, git, Claude |
| Windows PC | Game runtime, log capture | Schedule I, MelonLoader |

DLL is built on Mac, manually transferred to Windows `<GameDir>\Mods\`, game is launched, logs are captured from `<GameDir>\MelonLoader\Latest.log` and brought back to Mac.

## Separation of Concerns

### Core vs. Discovery

`Core/` is permanent infrastructure:
- `ModEntry` — MelonLoader lifecycle hook
- `ModLogger` — all logging goes through here (`[ClientOptimizer]` prefix)
- `ModConfig` — feature flags gating behavior

`Discovery/` is temporary reverse-engineering code:
- `DiscoveryOrchestrator` — single entry point, calls scan methods in sequence
- `RuntimeScanService` — broad assembly/type enumeration
- `TypeSearchService` — targeted keyword search for client-related types (Session 2)
- `ReflectionUtils` — safe reflection helpers
- `DumpUtils` — bounded type shape and instance value logging

**Rule:** Discovery code must NEVER mutate game state. It is read-only by design.

### Future: Domain / Services / Patches / UI

Empty until Phase 2+:
- **Domain:** Our models mirroring discovered game structures
- **Services:** Logic for reading game state, computing derived values, performing actions
- **Patches:** Harmony patches hooking into game methods
- **UI:** In-game interface (IMGUI or similar)

## Logging Strategy

All logging goes through `ModLogger`, which wraps `MelonLogger` with `[ClientOptimizer]` prefix.

| Level | When | Gated? |
|---|---|---|
| `Info` | Key events, summaries, match results | Always shown |
| `Warning` | Unexpected but non-fatal conditions | Always shown |
| `Error` | Failures | Always shown |
| `Debug` | Per-assembly details, verbose output | Behind `ModConfig.DebugLogging` |

**Bounding rules:**
- DumpUtils caps at 50 members per type
- TypeSearchService caps at 15 full type dumps
- RuntimeScanService shows full list only at Debug level

## Build Output

Single `ClientAssignmentOptimizer.dll` targeting net6.0. References resolve from `libs/` (flat layout) first, then `$(GameDir)/MelonLoader/...` subdirectory layout.

## Dependencies

- **MelonLoader 0.6.x** — mod framework (`MelonMod`, `MelonLogger`, Harmony)
- **Il2CppInterop** — interop layer for IL2CPP games
- **0Harmony** — method patching (for future Patches/)
- No NuGet packages. All references come from MelonLoader.

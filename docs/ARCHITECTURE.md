# Architecture

## Project Structure

```
schedule1-mod/
├── ClientAssignmentOptimizer.csproj    # Build configuration
├── CLAUDE.md                           # AI session context
├── README.md                           # Project overview
├── .gitignore
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
│   │   ├── ReflectionUtils.cs          # Safe reflection helpers
│   │   └── DumpUtils.cs               # Structured type/value dumping
│   │
│   ├── Domain/                         # Data models (future)
│   ├── Services/                       # Business logic (future)
│   ├── Patches/                        # Harmony patches (future)
│   └── UI/                             # In-game interface (future)
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

## Separation of Concerns

### Core vs. Discovery

`Core/` contains code that will persist throughout the mod's lifetime:
- `ModEntry` is the MelonLoader lifecycle hook
- `ModLogger` is used by all other code for consistent logging
- `ModConfig` gates optional behavior

`Discovery/` contains code that exists only for reverse engineering. Once we understand the game's internals well enough to build features, discovery code can be disabled via `ModConfig.DiscoveryEnabled = false` or removed entirely.

**Rule:** Discovery code must NEVER mutate game state. It is read-only by design.

### Domain / Services / Patches / UI

These folders are currently empty (placeholders). They will be populated in Phases 2-4:

- **Domain:** C# classes that model the game data we care about (client, dealer, assignment). These are OUR models, not the game's classes directly.
- **Services:** Logic for reading game state, computing derived values (e.g., "should be player"), and performing actions (e.g., reassignment).
- **Patches:** Harmony patches that hook into game methods. Used when we need to intercept or observe game behavior that isn't accessible through simple reflection.
- **UI:** In-game interface using Unity's IMGUI or similar. Rendered on top of the game.

## Logging Strategy

All logging goes through `ModLogger`, which wraps `MelonLogger` with a consistent `[ClientOptimizer]` prefix.

Four levels:
- `Info` — always shown. Key lifecycle events, discovery summaries.
- `Warning` — always shown. Unexpected but non-fatal conditions.
- `Error` — always shown. Failures.
- `Debug` — gated behind `ModConfig.DebugLogging`. Verbose detail (e.g., every loaded assembly name).

**Guideline:** A fresh run with `DebugLogging = false` should produce fewer than 30 lines of output. Debug mode can be verbose but must still be bounded (DumpUtils caps at 50 members per type).

## Build Output

The .csproj builds a single `ClientAssignmentOptimizer.dll` targeting net6.0. The post-build step copies it to `$(GameDir)\Mods\` for immediate testing.

## Dependencies

- **MelonLoader 0.6.x** — mod framework (provides `MelonMod`, `MelonLogger`, Harmony)
- **Il2CppInterop** — interop layer for IL2CPP games (comes with MelonLoader)
- No NuGet packages. All references come from the MelonLoader installation.

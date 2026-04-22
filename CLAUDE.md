# Client Assignment Optimizer — Schedule I Mod

## Project Summary

A MelonLoader mod for **Schedule I** (Unity IL2CPP, v0.4.5f2, Unity 2022.3.62f2). Gives players visibility into client assignments and lets them optimize which clients are served by the player vs. dealers.

**Repo:** `joelallouz/schedule1-mod`

## Current Phase

**Phase 1: Discovery** — Mod loads and runs. Running targeted type scans to find client/dealer/assignment classes.

Phase 0 (Bootstrap) is complete: scaffold, build, logging, runtime validation all confirmed working.

## Architecture

```
src/
  Core/           ModEntry (MelonMod lifecycle), ModLogger, ModConfig
  Discovery/      Read-only reverse engineering tools (temporary)
  Domain/         Data models (future — Phase 2+)
  Services/       Business logic (future — Phase 2+)
  Patches/        Harmony patches (future — Phase 2+)
  UI/             In-game interface (future — Phase 2+)
docs/             Persistent cross-session documentation
libs/             Reference DLLs for macOS builds (gitignored)
```

## Development Workflow

**Dual-machine setup:**
- **macOS** — code editing, builds (`dotnet build -p:CopyToMods=false`)
- **Windows PC** — runs Schedule I with the mod, produces logs

**Loop:**
1. Claude writes code on Mac
2. Human builds on Mac: `dotnet build ClientAssignmentOptimizer.csproj -p:CopyToMods=false`
3. Human copies `bin/Debug/net6.0/ClientAssignmentOptimizer.dll` to Windows `<GameDir>\Mods\`
4. Human launches game, captures `<GameDir>\MelonLoader\Latest.log`
5. Human pastes log back to Claude
6. Claude interprets, updates docs, writes next change

## Key Conventions

- **Namespace:** `ClientAssignmentOptimizer` (`.Core`, `.Discovery`, `.Domain`, `.Services`, `.Patches`, `.UI`)
- **Logging:** Always use `ModLogger`, never `MelonLogger` directly. Prefix: `[ClientOptimizer]`.
- **Discovery code** is in `src/Discovery/`, gated behind `ModConfig.DiscoveryEnabled`. Must NEVER mutate game state.
- **All output must be bounded** — DumpUtils caps at 50 members, TypeSearchService caps at 15 full dumps.

## Documentation Rules

All persistent knowledge lives in `docs/`. Every session must:

1. **Read** `SESSION_LOG.md` and `FINDINGS.md` at start
2. **Append** to `SESSION_LOG.md` at end (never overwrite previous entries)
3. **Promote** confirmed discoveries to `FINDINGS.md` with evidence
4. **Update** `OPEN_QUESTIONS.md` as questions are answered or new ones emerge
5. **Update** `ROADMAP.md` checkboxes as work completes
6. Never fabricate findings — evidence required

## Build

```bash
# macOS (compile only)
dotnet build ClientAssignmentOptimizer.csproj -p:CopyToMods=false

# Windows (compile + deploy)
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="C:\...\Schedule I"
```

References resolve from `libs/` (flat layout) first, then `$(GameDir)/MelonLoader/...` layout.

## Known Runtime Facts

- Game: IL2CPP, Unity 2022.3.62f2, Schedule I v0.4.5f2
- MelonGame attribute: `("TVGS", "Schedule I")` — confirmed working
- ~235 assemblies loaded at runtime
- Key game assemblies: `Assembly-CSharp` (~3705 types), `Il2CppScheduleOne.Core` (~46 types)
- MelonLoader console logging works

## Known Risks

- Game updates may rename/remove classes — discovery findings are version-specific (v0.4.5f2)
- Il2Cpp reflection may not expose all members the same way as Mono reflection
- Assembly filter may need tuning as we discover more about assembly layout

# Session Log

---

## Session 1 — Bootstrap (2026-04-22)

### Goals
- Create clean project structure for a MelonLoader mod
- Build logging and discovery infrastructure
- Populate documentation for multi-session continuity
- Do NOT implement any gameplay features

### Files Created

**Source — Core:**
- `src/Core/ModEntry.cs` — MelonMod entry point, calls discovery on init
- `src/Core/ModLogger.cs` — Centralized logger with `[ClientOptimizer]` prefix
- `src/Core/ModConfig.cs` — Static config flags (DebugLogging, DiscoveryEnabled)

**Source — Discovery:**
- `src/Discovery/DiscoveryOrchestrator.cs` — Single entry point for all discovery scans
- `src/Discovery/RuntimeScanService.cs` — Logs loaded assemblies and game assembly type counts
- `src/Discovery/ReflectionUtils.cs` — Safe reflection helpers (type enumeration, assembly filtering)
- `src/Discovery/DumpUtils.cs` — Structured type/instance dumping with bounded output

**Source — Placeholders:**
- `src/Domain/Placeholder.txt`
- `src/Services/Placeholder.txt`
- `src/Patches/Placeholder.txt`
- `src/UI/Placeholder.txt`

**Project:**
- `ClientAssignmentOptimizer.csproj` — .NET 6 class library targeting MelonLoader

**Documentation:**
- `docs/PRD.md`, `docs/ROADMAP.md`, `docs/SESSION_LOG.md`
- `docs/FINDINGS.md`, `docs/OPEN_QUESTIONS.md`
- `docs/ARCHITECTURE.md`, `docs/TESTING.md`

### Decisions Made
1. **MelonLoader over BepInEx** — Schedule I uses Unity IL2CPP; MelonLoader is the standard framework for IL2CPP mods.
2. **net6.0 target** — Required by MelonLoader 0.6.x for IL2CPP games.
3. **Static config over MelonPreferences** — Simpler for now; can migrate to MelonPreferences later for in-game configurability.
4. **Discovery is separate from feature code** — `src/Discovery/` is intentionally isolated so it can be disabled once reverse engineering is complete.
5. **Conservative assembly filter** — `ReflectionUtils.IsGameAssembly()` excludes known framework prefixes but errs on the side of showing too much rather than hiding game assemblies.
6. **Bounded output everywhere** — DumpUtils caps member lists at 50 entries to prevent log spam.

### Known Limitations
- **Not yet compiled** — Requires MelonLoader DLLs on disk to build. The .csproj references them from the game install directory.
- **MelonGame attribute unverified** — `[assembly: MelonGame("TVGS", "Schedule I")]` is based on community convention; needs verification on first load.
- **No MelonPreferences** — Config is hardcoded. Fine for dev; should be externalized later.
- **Assembly filter may be too broad or too narrow** — First real scan will tell us if game assemblies are being hidden or if too much noise is shown.

### Next Steps
1. Install Schedule I and MelonLoader
2. Build the mod and place DLL in `Mods/` folder
3. Launch game, check MelonLoader console for log output
4. Capture discovery output and update FINDINGS.md
5. Begin Phase 1: targeted discovery of client/customer classes

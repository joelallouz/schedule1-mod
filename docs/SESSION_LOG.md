# Session Log

---

## Session 1 — Bootstrap & Build-Ready Scaffold (2026-04-22)

### Goals
- Create the full project structure on disk
- Write all source files as real, compilable C#
- Populate /docs with meaningful documentation
- Ensure the .csproj is correct and build-ready
- Do NOT implement gameplay features or start discovery

### What Happened

Wrote the complete project scaffold in a single session. All source files, documentation, and build configuration are on disk and committed.

Fixed a .csproj bug: SDK-style projects auto-include `*.cs` files by default, which would have caused duplicate compile item errors with the explicit `<Compile Include="src/**/*.cs" />` glob. Fixed by setting `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>`.

### Files Created

**Source — Core (3 files):**
- `src/Core/ModEntry.cs` — MelonMod entry point, logs version, calls DiscoveryOrchestrator
- `src/Core/ModLogger.cs` — Centralized logger wrapping MelonLogger with `[ClientOptimizer]` prefix
- `src/Core/ModConfig.cs` — Static config: DebugLogging and DiscoveryEnabled flags

**Source — Discovery (4 files):**
- `src/Discovery/DiscoveryOrchestrator.cs` — Single entry point that calls RuntimeScanService methods
- `src/Discovery/RuntimeScanService.cs` — Logs loaded assemblies (full list at Debug, game assemblies at Info) and type counts per game assembly
- `src/Discovery/ReflectionUtils.cs` — Assembly filtering (IsGameAssembly), safe GetTypes, type formatting
- `src/Discovery/DumpUtils.cs` — DumpTypeShape (fields/props/methods) and DumpInstanceValues, both bounded at 50 members

**Source — Placeholders (4 files):**
- `src/Domain/Placeholder.txt`, `src/Services/Placeholder.txt`, `src/Patches/Placeholder.txt`, `src/UI/Placeholder.txt`

**Build:**
- `ClientAssignmentOptimizer.csproj` — net6.0, references MelonLoader/Il2Cppmscorlib/Il2CppInterop.Runtime from GameDir, auto-copies DLL to Mods/

**Docs (7 files):**
- `docs/PRD.md` — Product requirements
- `docs/ROADMAP.md` — Phases 0–4 with checklists
- `docs/SESSION_LOG.md` — This file
- `docs/FINDINGS.md` — Confirmed/suspected/unknown sections (empty — no scan yet)
- `docs/OPEN_QUESTIONS.md` — Key unknowns about client data, assignment, spending, addiction, preferences
- `docs/ARCHITECTURE.md` — Folder structure, separation of concerns, logging strategy
- `docs/TESTING.md` — Build instructions, deploy steps, expected logs, troubleshooting

**Config:**
- `.gitignore` — bin/, obj/, *.dll, *.pdb, IDE files, .DS_Store, libs/
- `CLAUDE.md` — Session continuity rules for future Claude sessions
- `README.md` — Project overview with build command

### Decisions Made
1. **MelonLoader 0.6.x + net6.0** — Standard for Unity IL2CPP mods.
2. **EnableDefaultCompileItems=false** — Explicit source inclusion avoids SDK-style auto-include conflicts.
3. **Static config (not MelonPreferences)** — Simpler for Phase 0. Can migrate later.
4. **Discovery isolated in src/Discovery/** — Gated behind ModConfig.DiscoveryEnabled. Read-only, bounded, no game mutation.
5. **Conservative assembly filter** — Excludes known framework prefixes; errs toward showing extra assemblies rather than hiding game code.
6. **GameDir as MSBuild property** — Overridable via `-p:GameDir=...` so it works on any machine without editing the .csproj.
7. **libs/ in .gitignore** — Supports building on macOS by copying reference DLLs locally.

### Known Limitations
- **Not yet compiled or run** — Requires MelonLoader DLLs to resolve references.
- **MelonGame attribute unverified** — `("TVGS", "Schedule I")` is community convention; may need adjustment.
- **No MelonPreferences integration** — Config changes require recompilation.
- **Assembly filter untested** — May need tuning after first real scan.

### Next Steps (for Session 2)
1. Install Schedule I and MelonLoader on a Windows machine
2. Build the mod: `dotnet build -p:GameDir="<path>"`
3. Launch game with mod, capture MelonLoader log output
4. Paste log output into the next Claude session for analysis
5. Update FINDINGS.md with confirmed assemblies
6. Begin Phase 1: targeted scans for client/customer classes

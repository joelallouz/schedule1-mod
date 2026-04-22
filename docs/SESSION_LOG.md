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

---

## Session 2 — Context Reconstruction & Targeted Discovery (2026-04-22)

### Goals
- Reconstruct full project state from existing code + runtime context provided by human
- Promote confirmed runtime findings to FINDINGS.md (Phase 0 was already complete)
- Normalize all documentation to reflect actual state
- Implement Session 2 discovery: targeted type search for client/dealer/assignment classes
- Prepare for first Phase 1 log capture

### What Happened

**Context received from human:** The mod has already been run successfully on Windows. Key runtime facts confirmed:
- Game is IL2CPP, Unity 2022.3.62f2, Schedule I v0.4.5f2
- ~235 assemblies loaded, Assembly-CSharp has ~3705 types, Il2CppScheduleOne.Core has ~46 types
- MelonGame attribute ("TVGS", "Schedule I") works
- Logging and discovery framework both work

**Phase 0 declared complete.** All checkboxes satisfied. Moved to Phase 1.

**Documentation overhaul:** All 7 docs files updated to reflect confirmed runtime state. FINDINGS.md now has actual confirmed data instead of "nothing confirmed yet." ROADMAP.md Phase 0 marked complete. OPEN_QUESTIONS.md has two technical questions marked resolved.

**New code: TypeSearchService.cs** — Targeted type search across Assembly-CSharp and Il2CppScheduleOne.* assemblies. Searches for keywords: Client, Customer, Dealer, Assign, Owner, NPC, Buyer, Order, Relationship, Contact. For types matching Client/Customer/Dealer, dumps full type shape (fields, properties, methods). Output is grouped by keyword and bounded (max 15 full dumps).

**DiscoveryOrchestrator updated** — Now calls TypeSearchService.SearchForClientRelatedTypes() after the existing assembly scan.

**CLAUDE.md rewritten** — Now reflects dual-machine workflow, current phase, confirmed runtime facts, and documentation rules.

### Files Created
- `src/Discovery/TypeSearchService.cs` — keyword-based type search with full dumps for high-priority matches

### Files Modified
- `src/Discovery/DiscoveryOrchestrator.cs` — added TypeSearchService call
- `CLAUDE.md` — full rewrite for current state
- `docs/PRD.md` — added v1 simplification note, game version
- `docs/ROADMAP.md` — Phase 0 complete, Phase 1 in progress
- `docs/FINDINGS.md` — promoted confirmed runtime data
- `docs/OPEN_QUESTIONS.md` — marked 2 resolved, added new technical questions
- `docs/ARCHITECTURE.md` — added dual-machine workflow, TypeSearchService, libs/ dir
- `docs/TESTING.md` — rewritten for Mac→PC workflow, Session 2 expected output
- `docs/SESSION_LOG.md` — this entry

### Decisions Made
1. **Phase 0 is complete** — all exit criteria met (confirmed by human's runtime report).
2. **TypeSearchService targets specific assemblies by prefix** rather than relying on the general IsGameAssembly filter — more precise, avoids Il2Cpp framework noise.
3. **Keywords chosen for breadth** — 10 keywords covering client, dealer, assignment, and relationship concepts.
4. **Full dumps only for Client/Customer/Dealer matches** — other keyword matches get one-line summaries to keep logs bounded.
5. **Max 15 full dumps** — enough to see all client/dealer types without overwhelming the log.

### Hypotheses Going Into This Scan
1. Assembly-CSharp likely contains the main game classes (NPC, Client, Dealer, etc.)
2. Il2CppScheduleOne.Core likely contains base types, enums, or managers
3. There may be other Il2CppScheduleOne.* assemblies we haven't enumerated yet
4. Client assignment is probably a field on a client object referencing a dealer or player
5. "Client" or "Customer" is the most likely class name for what we're looking for

### Known Limitations
- IL2CPP reflection may not expose all fields/properties the way Mono would — type shapes could be incomplete
- We don't know if there are Il2CppScheduleOne.* assemblies beyond .Core until we see the scan output
- The keyword list may miss game-specific terminology (e.g., if the game calls clients "contacts" or "buyers" internally)

### Next Steps (for human)
1. Build: `dotnet build ClientAssignmentOptimizer.csproj -p:CopyToMods=false`
2. Copy `bin/Debug/net6.0/ClientAssignmentOptimizer.dll` to Windows `<GameDir>\Mods\`
3. Launch game
4. Copy `<GameDir>\MelonLoader\Latest.log` and paste the full contents back here
5. Critical section: everything between "=== Targeted Type Search ===" and "=== Targeted Type Search Complete ==="

### Log Analysis (Session 2, continued)

Human ran the mod and returned full MelonLoader log. Results:

**95 types matched** across 10 keywords. Key matches by category:

| Keyword | Count | Notable Types |
|---|---|---|
| Customer | 11 | `Economy.Customer`, `Economy.CustomerData`, `Economy.CustomerAffinityData`, `Economy.CustomerPreference`, `Economy.CustomerSatisfaction`, `Economy.ECustomerStandard` |
| Dealer | 12 | `Economy.Dealer`, `Economy.EDealerType`, `Persistence.Datas.DealerData`, `UI.Phone.Messages.DealerManagementApp` |
| NPC | 55 | `NPCs.NPC`, `NPCs.NPCManager`, `NPCs.Relation.NPCRelationData` and many schedule/behaviour types |
| Assign | 1 | `UI.Management.AssignedWorkerDisplay` |
| Client | 2 | FishySteamworks networking types (NOT game clients — false positive) |
| Contact | 3 | Phone contacts UI |
| Relationship | 5 | `NPCs.Relation.ERelationshipCategory`, `NPCs.Relation.RelationshipCategory` |
| Order | 5 | Sort order enums (not purchase orders) |
| Owner | 1 | `ItemFramework.IItemSlotOwner` |

**Critical discovery:** The game uses "Customer" not "Client" for its entities. Our mod name "Client Assignment Optimizer" is fine for the user-facing name, but internally we reference `Customer` and `Dealer` classes.

**Full type shapes confirmed for primary classes.** See FINDINGS.md for complete property maps of Customer, CustomerData, Dealer, and supporting types.

**Almost all Phase 1 discovery questions answered in a single scan.** Assignment is a direct Dealer reference. Addiction is a float. Weekly spend is a min/max range with a computed method. Preferences are stored as affinities and preferred properties. Enumeration uses static lists.

### What Changed After Log Analysis
- `docs/FINDINGS.md` — major update with all confirmed class/field data
- `docs/OPEN_QUESTIONS.md` — 11 questions resolved, new questions about runtime access and reassignment API
- `docs/ROADMAP.md` — Phase 1 mostly complete (10 of 14 items checked)
- `CLAUDE.md` — updated with confirmed class names

### Next Steps (for Session 3)
1. Write a focused discovery scan to:
   - Read `Customer.UnlockedCustomers` at runtime and log count + names
   - Check if `AssignedDealer == null` means player-assigned
   - Dump the `NPC` base class to find display name field
   - Look for assign/remove customer methods on `Dealer` (need full method dump)
2. Verify we can access Il2Cpp properties from mod code
3. If runtime reading works → begin Phase 2 domain models

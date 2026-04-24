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

---

## Session 3 — Runtime Verification & Method Discovery (2026-04-22)

### Goals
- Dump NPC base class to find display name / ID fields
- Search Dealer + Customer for assignment-related methods (Assign, Remove, Transfer)
- Verify runtime access: read Customer.UnlockedCustomers and Dealer.AllPlayerDealers from a loaded save
- For customers: log name, AssignedDealer, CurrentAddiction, MinWeeklySpend/MaxWeeklySpend
- For dealers: log name, AssignedCustomers count, MAX_CUSTOMERS, Cash, Cut

### What Happened

**Two new discovery services written:**

1. **NPCTypeScanService.cs** — Runs during OnInitializeMelon (reflection-only, no live data needed):
   - Dumps NPC base class: full inheritance chain, highlights name/ID-related properties, lists up to 100 properties
   - Searches Dealer and Customer for methods matching: Assign, Remove, Add, Customer, Transfer, Unassign — logs full signatures with declaring type

2. **RuntimeVerificationService.cs** — Runs on scene load (needs live game data):
   - Reads `Customer.UnlockedCustomers` via reflection, logs count
   - For first 5 customers: tries multiple name property paths (NPC.fullName, NPC.FirstName, etc.), reads AssignedDealer (null = player?), CurrentAddiction, CustomerData spend range
   - Reads `Dealer.AllPlayerDealers`, logs count and MAX_CUSTOMERS
   - For each dealer: name, AssignedCustomers count, Cash, Cut, DealerType
   - Returns false if data isn't loaded yet (retries on next scene load)

**ModEntry updated with OnSceneWasLoaded:**
- Logs every scene load (name + index) for debugging scene lifecycle
- Triggers RuntimeVerificationService once per session, retrying across scenes until data is found

**DiscoveryOrchestrator split into two phases:**
- `Run()` — reflection-only scans (OnInitializeMelon)
- `RunRuntimeVerification()` — live data access (OnSceneWasLoaded)

### Files Created
- `src/Discovery/NPCTypeScanService.cs` — NPC dump + assignment method search
- `src/Discovery/RuntimeVerificationService.cs` — live data verification

### Files Modified
- `src/Discovery/DiscoveryOrchestrator.cs` — added NPCTypeScanService calls to Run(), added RunRuntimeVerification()
- `src/Core/ModEntry.cs` — added OnSceneWasLoaded with one-shot runtime verification trigger

### Decisions Made
1. **OnSceneWasLoaded with retry** — We don't know which scene has game data. The verification attempts on each scene load until Customer.UnlockedCustomers is non-null, then stops.
2. **Reflection-only access** — No Assembly-CSharp.dll compile reference needed. All property access via System.Reflection. Tries both backing field names (`_X_k__BackingField`) and regular property names.
3. **NPC dump bounded at 100 properties** — Higher than DumpUtils' default 50 because NPC is a key base class and we need to find the name fields.
4. **Assignment method search covers both Dealer AND Customer** — The API to move a customer might live on either class.

### Open Questions Targeted by This Scan
- Does `AssignedDealer == null` mean player-assigned?
- What is the NPC display name property?
- What methods exist for assignment/reassignment?
- Can we read IL2CPP properties via reflection at runtime?
- Are the lists `Il2CppSystem.Collections.Generic.List` or `System.Collections.Generic.List`?
- Does `Dealer.MAX_CUSTOMERS` vary per dealer or is it constant?

### Known Limitations
- IL2CPP property access via reflection may not work the same as Mono — this scan will tell us
- `OnSceneWasLoaded` might fire before data is populated — the retry mechanism handles this
- We don't know scene names yet — logging them will fill that gap

### Log Analysis (Session 3, first run)

Human ran the mod and returned log `26-4-22_17-53-47.log`. All three new sections produced output.

**NPC Base Class Dump — SUCCESS:**
- Full inheritance chain: NPC → NetworkBehaviour → MonoBehaviour → ... → Il2CppObjectBase
- 207 public properties
- Name/ID fields found: `FirstName`, `LastName`, `hasLastName`, `fullName`, `ID`, `GUID`, `BakedGUID`, `name`, `SaveFolderName`
- Other useful: `NPCRelationData RelationData`, `EMapRegion Region`, `Sprite MugshotSprite`

**Assignment Method Search — SUCCESS:**
- **Dealer (44 matches):** `AddCustomer(Customer)`, `RemoveCustomer(Customer)`, `RemoveCustomer(String npcID)`, `SendRemoveCustomer(String npcID)`, plus network RPC variants (`AddCustomer_Server`, `AddCustomer_Client`)
- **Customer (56 matches):** `AssignDealer(Dealer)`, `get_AssignedDealer()`, `set_AssignedDealer(Dealer)`, `GetCustomerData()`, plus getters/setters for all key fields
- Full reassignment API surface is now known

**Runtime Verification — PARTIAL (bug: ran on Menu scene):**
- Types found and accessible: ✓
- Static property reading works: ✓
- List type confirmed: `Il2CppSystem.Collections.Generic.List<T>`
- `Dealer.MAX_CUSTOMERS` = 10 (read successfully)
- **Bug:** Lists were non-null but empty (0 customers, 0 dealers) on Menu scene. Code returned `true` (marking verification done) because lists were accessible. Never retried on Main scene where save data lives.

**Scene lifecycle observed:** Menu (0) → Main (1) → Menu (0) × 3 on exit

**Bug fix applied:** `RuntimeVerificationService.Verify()` now returns false when both lists are empty, so it retries on subsequent scene loads until actual data is found.

### Resolved Questions (from this log)
1. ✅ NPC display name → `fullName`, `FirstName`, `LastName`
2. ✅ Assignment API → `Dealer.AddCustomer/RemoveCustomer`, `Customer.AssignDealer`
3. ✅ List types → `Il2CppSystem.Collections.Generic.List<T>`
4. ✅ MAX_CUSTOMERS → 10 (static)
5. ✅ Runtime access → reflection works on IL2CPP types, both backing fields and proper getters
6. ✅ Scene lifecycle → Menu (0) then Main (1)

### Still Needs Verification (requires loaded save with customers)
- Actual customer name/addiction/spend values
- AssignedDealer null vs Dealer object
- Dealer AssignedCustomers count
- Cash, Cut values

### Log Analysis (Session 3, second run — 26-4-22_18-4-39.log)

Retry logic worked: deferred on Menu (0 customers, 0 dealers), ran on Main scene (6 dealers found).

**Dealer data confirmed at runtime:**

| Dealer | Customers | Cash | Cut | Type |
|---|---|---|---|---|
| Brad Crosby | 0 | 0 | 0.2 | PlayerDealer |
| Jane Lucero | 0 | 0 | 0.2 | PlayerDealer |
| Molly Presley | 0 | 0 | 0.2 | PlayerDealer |
| Benji Coleman | 0 | 0 | 0.2 | PlayerDealer |
| Wei Long | 0 | 0 | 0.2 | PlayerDealer |
| Leo Rivers | 0 | 0 | 0.2 | PlayerDealer |

**Runtime access confirmed:**
- `fullName` works: "Brad Crosby", etc.
- `FirstName`, `LastName` work individually
- `name` (UnityEngine.Object) returns first name only
- `Name` property does not exist
- `Cash`, `Cut`, `DealerType`, `AssignedCustomers` all readable

**Problem:** 0 unlocked customers, 0 assigned customers per dealer. Save has 6 dealers — must have customers, but `Customer.UnlockedCustomers` isn't populated yet when `OnSceneWasLoaded` fires. Game loads dealers before customers.

**Fix applied:** Replaced immediate `OnSceneWasLoaded` trigger with a 10-second delay after Main scene loads. Uses `System.Diagnostics.Stopwatch` in `OnUpdate()` to avoid Unity API dependencies.

### Log Analysis (Session 3, third run — 26-4-22_18-15-48.log) — FULL SUCCESS

10s delay worked. Runtime verification ran on Main scene with full data.

**39 unlocked customers** — first 5 sampled:

| Name | NPC.ID | AssignedDealer | Addiction | Min/Max Spend | Standards |
|---|---|---|---|---|---|
| Kyle Cooley | kyle_cooley | Benji Coleman | 1.0 | $400-$900 | Low |
| Mick Lubbin | mick_lubbin | NULL (player) | 0.9375 | $400-$800 | Low |
| Jessi Waters | jessi_waters | Benji Coleman | 1.0 | $200-$1200 | VeryLow |
| Sam Thompson | sam_thompson | NULL (player) | 1.0 | $200-$500 | Low |
| Austin Steiner | austin_steiner | Benji Coleman | 1.0 | $400-$800 | Low |

**6 dealers** — matches user's ground truth (3 recruited, 10 customers each):

| Name | IsRecruited | Customers | Cash | Cut |
|---|---|---|---|---|
| Brad Crosby | True | 10 | $770 | 0.2 |
| Jane Lucero | False | 0 | $0 | 0.2 |
| Molly Presley | True | 10 | $0 | 0.2 |
| Benji Coleman | True | 10 | $0 | 0.2 |
| Wei Long | False | 0 | $0 | 0.2 |
| Leo Rivers | False | 0 | $0 | 0.2 |

**Confirmed:**
- `AssignedDealer == null` → player-assigned (Mick, Sam confirmed)
- `fullName` on NPC, not on Customer directly — access pattern: `customer.NPC.fullName`
- `NPC.ID` gives string ID (e.g., "kyle_cooley")
- `CustomerData.name` = prefab name ("KyleData") — not useful for display
- All properties readable: names, IDs, assignments, addiction, spend, standards, IsRecruited, Cash, Cut
- 39 total = 30 dealer-assigned + 9 player-assigned

### Phase 1: COMPLETE

All exit criteria met:
- ✅ Customer name (via NPC.fullName)
- ✅ Assignment (AssignedDealer: null = player, object = dealer)
- ✅ Spend (CustomerData.MinWeeklySpend / MaxWeeklySpend)
- ✅ Addiction (CurrentAddiction: 0.0-1.0 float)
- ✅ Reassignment API known (Dealer.AddCustomer, RemoveCustomer, Customer.AssignDealer)
- ✅ IsRecruited distinguishes recruited vs unrecruited dealers

### Phase 2 Work Started (same session)

After Phase 1 completion, began Phase 2 implementation:

**Domain models created:**
- `src/Domain/CustomerInfo.cs` — FullName, NpcId, AssignedDealerName, IsPlayerAssigned, CurrentAddiction, MinWeeklySpend, MaxWeeklySpend, Standards
- `src/Domain/DealerInfo.cs` — FullName, IsRecruited, AssignedCustomerCount, Cash, Cut, DealerType

**GameDataService created (`src/Services/GameDataService.cs`):**
- Wraps all reflection access into clean domain objects
- Caches type/property lookups (resolved once, reused)
- Caches query results with 5s TTL for IMGUI performance
- `GetAllCustomers()`, `GetAllDealers()`, `InvalidateCache()`
- Customer mapping: NPC → fullName/ID, AssignedDealer, CurrentAddiction, CustomerData → spend/standards

**IMGUI Panel created (`src/UI/CustomerPanelUI.cs`):**
- Toggle with F9, refresh with F10
- Centered dark panel, 700x500
- Summary header: customer counts (player/dealer), dealer count
- Scrollable table with columns: Name, Assignment, Addiction, Min$, Max$, Standards
- Click column headers to sort (ascending/descending toggle)
- Default sort: Max Spend descending (shows high-value customers first)
- Color coding: green = player-assigned, white = dealer-assigned

**ModEntry updated:**
- `OnUpdate()`: F9 hotkey toggle, F10 refresh, existing delay timer
- `OnGUI()`: draws CustomerPanelUI
- Tracks `_inMainScene` to only enable hotkeys in gameplay

**Build infrastructure:**
- Added UnityEngine.CoreModule, IMGUIModule, InputLegacyModule references to .csproj
- User copied DLLs from Windows `<GameDir>/MelonLoader/Il2CppAssemblies/` to `libs/`
- Placeholder files removed from Domain/, Services/, UI/, Patches/

**Build: SUCCEEDS** — 0 warnings, 0 errors.

**NOT YET TESTED AT RUNTIME** — DLL needs to be deployed and tested in-game.

### Next Steps (for Session 4)
1. Copy `bin/Debug/net6.0/ClientAssignmentOptimizer.dll` to Windows `<GameDir>\Mods\`
2. Launch game, load save, press F9 — verify panel appears with customer data
3. Test sorting (click column headers), verify refresh (F10)
4. If IMGUI doesn't render (IL2CPP proxy issues), debug and iterate
5. If panel works: mark Phase 2 roadmap items, add preferences column
6. Preferences still untested but should work same as other CustomerData fields

---

## Session 4 — macOS Environment Setup & Automated Deploy (2026-04-23)

### Goals
- Clone repo to new macOS dev machine
- Set up build environment (.NET 6 SDK)
- Automate the build → deploy → log workflow (eliminate manual RDP file transfers)

### What Happened

**Repo cloned** to `/Users/joelallouz/dev/schedule1-mod`. All `libs/` DLLs present (committed to git).

**.NET 6 SDK installed** via Homebrew (`brew install dotnet@6`, v6.0.136). PATH and DOTNET_ROOT added to `~/.zshrc`.

**Build verified** — `dotnet build -p:CopyToMods=false` succeeds with 0 warnings, 0 errors.

**SSH access established to Windows PC:**
- Enabled OpenSSH Server on Windows (`Add-WindowsCapability`, `Start-Service sshd`)
- Opened firewall port 22 (`netsh advfirewall`)
- Generated Ed25519 key pair on Mac, installed public key in `C:\ProgramData\ssh\administrators_authorized_keys`
- Key-based auth working: `ssh jlall@192.168.1.141` connects without password

**`deploy.sh` created** — single script for build + deploy + log operations:
- `./deploy.sh` — builds and SCPs DLL to `<GameDir>\Mods\`
- `./deploy.sh --tail` — builds, deploys, then live-tails `Latest.log` via SSH
- `./deploy.sh --logs` — pulls `Latest.log` to project dir via SSH

**All three modes tested and working.**

### Files Created
- `deploy.sh` — automated build/deploy/log script

### Files Modified
- `CLAUDE.md` — updated workflow section with deploy.sh commands and Windows paths
- `docs/TESTING.md` — rewritten to reflect automated deploy workflow, added SSH prereqs
- `.gitignore` — added `Latest.log`
- `docs/SESSION_LOG.md` — this entry

### Decisions Made
1. **SSH over SMB/RDP** — enables scripted file transfer and remote log tailing
2. **Key-based auth** — no password prompts, works from scripts and Claude sessions
3. **SCP with Windows backslash paths** — forward slashes and `/c/` style don't work with Windows OpenSSH; single-quoted Windows paths work for upload, `ssh type` used for log download
4. **Log pulled via `ssh type` instead of `scp`** — more reliable quoting for Windows paths with spaces

### Next Steps (for Session 5)
1. Run `./deploy.sh` to push current DLL to PC
2. Launch game, load save, press F9 — verify IMGUI panel appears with customer data
3. Test sorting (click column headers), verify refresh (F10)
4. If IMGUI doesn't render (IL2CPP proxy issues), pull logs with `./deploy.sh --logs` and debug
5. If panel works: mark Phase 2 roadmap items, add preferences column

---

## Session 5 — First In-Game Test & Phase 2 Close (2026-04-23)

### Goals
- Deploy the Phase 2 build
- Verify the F9 panel renders with live customer data
- Finish the last Phase 2 item (preferences column) and close the phase

### What Happened

**Deployed build via `./deploy.sh`.** User launched game, loaded save, pressed F9 and F10. Reported everything worked — panel rendered, data displayed, refresh worked.

**Pulled `Latest.log` for verification.** Clean run: no errors, no exceptions. Confirmed log lines:
- `Initialization complete. Press F9 in-game to open customer panel.`
- Three `Customer panel data refreshed.` entries (F9 open + F10 presses)

**Preferences column was already wired** in all three layers (domain, service, UI) from Session 3 — the ROADMAP unchecked box was stale. The service's one-shot `[PrefDebug]` logging revealed the type:
- `CustomerData.PreferredProperties` → `List<Il2CppScheduleOne.Effects.Effect>`
- `Effect.Name` gives the human-readable label

**Removed the `[PrefDebug]` discovery block** from `GameDataService.ReadPreferences` now that the type is known. Simplified to read `Effect.Name` directly with a single `item.ToString()` fallback.

**Rebuilt and redeployed** the cleaned-up DLL. Build: 0 warnings, 0 errors.

### Files Modified
- `src/Services/GameDataService.cs` — removed `_prefLoggedOnce` debug block, collapsed preference-name lookup to the known good path (`Effect.Name`)
- `docs/ROADMAP.md` — marked preferences item complete, marked Phase 2 complete
- `docs/FINDINGS.md` — added Preferred Properties section (Effect type shape)
- `docs/SESSION_LOG.md` — this entry

### Decisions Made
1. **Trim discovery code once the type is confirmed** — the `[PrefDebug]` one-shot dump was temporary scaffolding. Leaving it in would bloat logs on every future cold start with no benefit now that we know `Effect.Name` is the right field.
2. **Don't wait for a second test before closing Phase 2** — user confirmed panel worked end-to-end, log is clean, and the preferences removal is cosmetic (unchanged semantics).

### Phase 2: COMPLETE

All exit criteria met. Panel renders, data is correct, sort works, refresh works, all five columns (name, assignment, spend, addiction, preferences) display live data.

### Next Steps (for Session 6 — Phase 3: Reassignment)
1. Test the reassignment API from Session 3's discovery:
   - `Dealer.AddCustomer(Customer)` / `Dealer.RemoveCustomer(Customer)`
   - `Customer.AssignDealer(Dealer)`
2. Determine the minimal set of calls needed to keep both sides in sync
3. Investigate network RPC variants (`AddCustomer_Server`, `AddCustomer_Client`) — may be needed for FishNet server authority
4. Add reassignment UI: click a customer row → pick new dealer from a dropdown
5. Validate MAX_CUSTOMERS (10) constraint before allowing reassignment
6. Start with player-only reassignment (customer ↔ player) before dealer-to-dealer transfers

---

## Session 6 — Phase 3 MVP: Reassignment Works (Move-to-Player Verified) (2026-04-23)

### Goals
- Start Phase 3: implement and test customer reassignment
- Pick a minimum call sequence that keeps both Customer.AssignedDealer and Dealer.AssignedCustomers in sync
- Ship a usable row-select UI so the user can pick a customer and a target

### What Happened

**1. Built `ReassignmentService`** — encapsulates the three discovered mutation methods. Key design choices:
- **Lookup by stable ID, not stored ref.** Re-resolves Customer by `NPC.ID` and Dealer by `fullName` at mutation time, walking `Customer.UnlockedCustomers` / `Dealer.AllPlayerDealers`. Prevents stale pointer bugs if the game reloads or caches rebuild.
- **Method resolution by (Customer) parameter type.** `Dealer.RemoveCustomer` has both a `(Customer)` and `(String npcID)` overload — we explicitly grab the one whose parameter type matches `Il2CppScheduleOne.Economy.Customer` to avoid ambiguity.
- **Exception isolation per invocation.** Each of RemoveCustomer / AddCustomer / AssignDealer is wrapped in its own try/catch, so partial progress is logged clearly rather than masked by a bubbling exception.
- **Post-state logging.** After the sequence runs, we re-read `Customer.AssignedDealer` and both dealers' `AssignedCustomers.Count` and log them so we can verify what actually changed.

**2. Wired row-select UI in CustomerPanelUI** — added a select-column with a per-row button (`►` when selected, blank otherwise), and an action bar above the table that appears when something is selected. Action bar shows "Move to Player" (if not already player-assigned) and one button per recruited dealer that isn't the current assignment. Full dealers show `(full)` suffix and are disabled via `GUI.enabled = false`.

**3. First attempt — rect-based click detection — FAILED** with a critical IL2CPP finding. Initial implementation used `GUILayoutUtility.GetLastRect()` for click hit-testing on row labels. Symptom: panel opened with header saying "39 customers" but only the first row rendered, and that row was unclickable. Log revealed:
```
System.NotSupportedException: Method unstripping failed
   at UnityEngine.GUILayoutGroup.GetLast()
   at UnityEngine.GUILayoutUtility.GetLastRect()
```
The method is stripped in this IL2CPP build. The exception fired inside the render `foreach` after the first row and broke the IMGUI state for the rest of the frame. **Fix:** replaced the rect hit-test with a native `GUILayout.Button` in the selector column — clicks are handled by Unity's IMGUI directly, no stripped APIs involved. This is a project-wide rule now: **never use `GUILayoutUtility.GetLastRect()` in this mod**.

**4. Second deploy — SUCCESS.** User tested two move-to-player reassignments:
- **Jessi Waters:** Benji Coleman (10 customers) → PLAYER. Benji dropped to 9. Post-state `AssignedDealer = PLAYER`. ✓
- **Dean Webster:** Molly Presley (10 customers) → PLAYER. Molly dropped to 9. Post-state `AssignedDealer = PLAYER`. ✓

Both customers and both dealers in sync. No exceptions. Call sequence is correct.

### Files Created
- `src/Services/ReassignmentService.cs` — mutation service with ID-based lookup, per-step invocation, and post-state logging

### Files Modified
- `src/UI/CustomerPanelUI.cs` — added `_selectedNpcId` state, `DrawActionBar()`, selector column, per-row select button; removed original rect-based hit-test attempt
- `docs/FINDINGS.md` — added `GetLastRect` stripping finding and verified move-to-player call sequence
- `docs/ROADMAP.md` — Phase 3 checkboxes updated; remaining: move-to-dealer, save/reload persistence, multiplayer RPC decision
- `docs/SESSION_LOG.md` — this entry

### Decisions Made
1. **Lookup by NPC.ID and Dealer.fullName, not stored references.** Cheap (one list walk) and robust against IL2CPP handle invalidation or post-refresh ref staleness.
2. **Call order: RemoveCustomer → AddCustomer → AssignDealer.** Dealer-side mutations first so capacity transitions are explicit; AssignDealer last so Customer.AssignedDealer is authoritative regardless of what the dealer-side methods do internally. For move-to-player, AddCustomer is skipped (no target dealer).
3. **Replace `GetLastRect`-based click detection with per-row Buttons.** One extra visual element per row, but native click handling and zero dependencies on stripped APIs.
4. **Don't test move-to-dealer yet.** Move-to-player is the reversible, no-capacity-risk baseline. Confirm this works first before adding AddCustomer to the tested sequence.

### Known Limitations / Remaining Unknowns
- **Move-to-dealer (player → dealer, dealer A → dealer B) untested.** The `ReassignmentService.MoveToDealer` code path exists but is unverified. Likely needs the same sequence plus `newDealer.AddCustomer(customer)` in the middle.
- **Save/reload persistence unverified.** User reported "worked" but didn't save and reload. A reassigned customer may or may not survive save serialization.
- **Multiplayer/FishNet.** The game has `AddCustomer_Server` / `AddCustomer_Client` RPC variants. On singleplayer/host these direct method calls likely work; client-only code would probably need the RPC path.

### Next Steps (for Session 7)
1. Test move-to-dealer: player-assigned customer → recruited dealer with capacity
2. Test dealer A → dealer B transfer
3. Save the game, quit to menu, reload — verify reassignments persisted
4. If persistence fails, investigate `Il2CppScheduleOne.Persistence.Datas.DealerData.AssignedCustomerIDs` — may need to be updated too
5. Decide whether to wire up FishNet network RPCs or leave as host-only for v1

### Session 6 — Continued: Move-to-Dealer and Save Persistence Verified

**Second in-game test session (21:57 – 22:01):** User ran three more reassignment events across two full save/reload cycles and reported everything persisted as expected. Log analysis:

**Round 1 (unsaved, reverted — expected):**
- 21:47:26 — Jessi: Benji → PLAYER
- 21:47:34 — Dean: Molly → PLAYER
- 21:48 quit to Menu **without saving**
- 21:56 reload: Jessi and Dean are back under their old dealers

**Round 2 (saved, persisted):**
- 21:57:19 — Jessi: Benji → PLAYER (redo)
- 21:57:27 — Dean: Molly → PLAYER (redo)
- 21:58 save + quit
- 21:59 reload: Jessi is PLAYER (confirmed by the fact that the next reassignment's log line reads "Jessi Waters: PLAYER -> Benji Coleman", i.e. starting state was PLAYER — proves persistence)

**Round 3 (move-to-dealer, saved, persisted):**
- 21:59:47 — Jessi: PLAYER → Benji (Benji 9→10). First test of the `AddCustomer + AssignDealer(newDealer)` path.
- 22:00 save + quit
- 22:00:52 reload: user opened panel, visually confirmed Jessi was with Benji, closed.

**Confirmed in this session:**
1. **Player → Dealer path works.** `AddCustomer + AssignDealer(newDealer)` with no RemoveCustomer call (source is player, nothing to remove from). Benji's count correctly went 9 → 10.
2. **Persistence works.** Reassignments survive full quit-to-menu → save-file-reload cycle when the user saves first. The game does NOT auto-save on mod actions — this is expected Schedule I behavior and shouldn't be considered a mod bug.
3. **No exceptions across the whole session.** 5 mutation events, all clean.

**Call sequences confirmed as correct minimum:**
- Dealer A → PLAYER:  `A.RemoveCustomer(c)` + `c.AssignDealer(null)`
- PLAYER → Dealer B:  `B.AddCustomer(c)` + `c.AssignDealer(B)`
- Dealer A → Dealer B (inferred, not directly tested): `A.Remove + B.Add + AssignDealer(B)`

**Phase 3 marked COMPLETE in ROADMAP.** Only deferred items are dealer-to-dealer direct transfer (trivial composition, low risk) and multiplayer RPC handling (not needed for singleplayer).

### Next Steps (for Session 7 — Phase 4: Flagging and Filtering)
1. Add a "flagged" indicator for high-value dealer-assigned customers (spend above a configurable threshold)
2. Add summary stats: total projected player revenue vs total dealer revenue (using `CustomerData.GetAdjustedWeeklySpend` or min/max averages)
3. Consider a filter toolbar: "show only flagged", "show only player-assigned", "show only [dealer]"
4. Optional: quick-action "Poach all flagged" button to bulk-reassign high-value dealer customers to the player (with MAX_CUSTOMERS check — but player has no 10-customer limit; needs verification)

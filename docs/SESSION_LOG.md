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

---

## Session 7 — Phone UI Discovery & Environment Setup (2026-04-23)

### Goals
- Set up macOS dev environment (clone repo, install .NET 6, automate deploy)
- Discover the in-game phone UI system to integrate our optimizer into the dealer management app
- Plan the optimizer tab implementation (Option C: hybrid tab on existing DealerManagementApp)

### What Happened

**Environment setup (Session 4 work, same day):**
- Cloned repo, installed .NET 6 SDK via Homebrew, verified build
- Set up SSH to Windows gaming PC (OpenSSH Server + key auth)
- Created `deploy.sh` — build + SCP deploy + log pull in one command

**Phone UI discovery — two scan passes:**

**Pass 1:** `PhoneUIDiscoveryService.SearchPhoneTypes()` found 517 matching types. Key phone app types identified: `DealerManagementApp`, `ContactsApp`, `DeliveryApp`, `JournalApp`, `MapApp`, `MessagesApp`, `ProductManagerApp` — all extending `App<T>`. Deep dump of `DealerManagementApp` revealed full inheritance chain: `DealerManagementApp → App<T> → PlayerSingleton<T> → MonoBehaviour`.

**Pass 2:** `DumpPhoneInfrastructure()` dumped the remaining critical types:
- **`Phone`** (`PlayerSingleton`) — the main phone controller with `SetIsHorizontal(bool)` for orientation animation, `ActiveApp` tracking, `orientation_Vertical`/`orientation_Horizontal` transform targets
- **`HomeScreen`** (`PlayerSingleton`) — manages app icons via `GenerateAppIcon(App<T>)`, has `appIconContainer` and `appIconPrefab`
- **`AppsCanvas`** — manages the app rendering canvas
- **`EOrientation`** — enum: `Horizontal = 0`, `Vertical = 1`
- **`CustomerSelector`** — existing customer picker with `onCustomerSelected` event, reusable
- **`DropdownUI`** — extends Unity `Dropdown` with `OnOpen` event

**Design decision: Option C (hybrid optimizer tab on DealerManagementApp)**
- User wants an "Optimize" button in the dealer app that flips to landscape and shows a data-rich table view
- Reassignment via per-row dealer dropdown (matching vanilla UX pattern)
- Orientation switching is natively supported via `Phone.SetIsHorizontal(bool)`

All findings promoted to `docs/FINDINGS.md` under "Phone UI System (Session 7)".

### Files Created
- `src/Discovery/PhoneUIDiscoveryService.cs` — phone type search + DealerManagementApp deep dump + phone infrastructure dump
- `src/Patches/DealerAppPatches.cs` — Harmony postfix on `DealerManagementApp.SetOpen(bool)`, verified firing
- `deploy.sh` — build/deploy/log automation script

### Files Modified
- `src/Discovery/DiscoveryOrchestrator.cs` — added PhoneUIDiscoveryService calls
- `src/Core/ModEntry.cs` — added `DealerAppPatches.Apply(HarmonyInstance)` call in OnInitializeMelon
- `ClientAssignmentOptimizer.csproj` — added references for 0Harmony.dll, UnityEngine.UI.dll, UnityEngine.UIModule.dll
- `docs/FINDINGS.md` — added full Phone UI System section + Harmony patching verification
- `docs/SESSION_LOG.md` — this entry
- `CLAUDE.md` — updated workflow section with deploy.sh
- `docs/TESTING.md` — rewritten for automated deploy workflow
- `.gitignore` — added Latest.log
- `~/.zshrc` — added .NET 6 PATH

### New libs/ DLLs added
- `UnityEngine.UI.dll` — uGUI components (Button, Text, Image, ScrollRect, etc.)
- `UnityEngine.UIModule.dll` — Unity UI module

### Decisions Made
1. **Option C: hybrid tab on DealerManagementApp** — add an "Optimize" toggle to the existing dealer app. Landscape mode for table view, portrait for vanilla. Best UX: appears where players already go, doesn't add app clutter, vanilla functionality preserved.
2. **Orientation switching via `Phone.SetIsHorizontal(bool)`** — the game has built-in animated orientation switching. No need to hack transforms.
3. **Harmony patches on DealerManagementApp** — patch `SetOpen()` to inject our toggle button, `Refresh()` to swap content when in optimizer mode.
4. **Harmony patch verified firing** — `SetOpen(bool)` postfix successfully intercepts app open/close events at runtime.

### Next Steps (for Session 8 — Build Optimizer UI)
1. In `SetOpen_Postfix(open=true)`: inject an "Optimize" toggle button into the dealer app's UI hierarchy
2. When optimize button clicked: call `Phone.Instance.SetIsHorizontal(true)` to flip to landscape, hide vanilla content, show optimizer panel
3. Build the optimizer panel as uGUI elements injected into the `Content` RectTransform — scrollable table with columns: Name, Assignment, Addiction, Min$, Max$, Preferences
4. Add per-row dealer dropdown for reassignment (Player + each recruited dealer)
5. When toggling back to vanilla: `SetIsHorizontal(false)`, restore original content
6. Wire up `GameDataService` for customer/dealer data, `ReassignmentService` for mutations
7. Test end-to-end: open dealer app → tap Optimize → landscape table → reassign → tap back → portrait vanilla

---

## Session 8 — Optimizer Tab Built, Deployed, Awaiting Test (2026-04-23)

### Goals
- Build the OptimizerTab UI per Session 7 plan: toggle button, landscape panel, scrollable customer table, per-row reassign popup
- Wire Harmony postfix for `DealerManagementApp.SetOpen(bool)` to drive the tab lifecycle
- Ship a first deployable DLL; hand to user for in-game test

### What Was Built

**`src/UI/OptimizerTab.cs` (new, ~570 lines)** — Static class that owns the tab UI end-to-end.
- **Toggle button injection** — on `SetOpen(true)`, resolves `appContainer` (RectTransform) via reflection off the DealerApp instance, creates a uGUI `Button` anchored top-right of `appContainer` labeled "Optimize". Re-injects if the DealerApp instance changes between opens.
- **Orientation flip** — on toggle, invokes `Phone.Instance.SetIsHorizontal(true/false)` via cached `PropertyInfo`/`MethodInfo` reflection. `Phone.Instance` is a `PlayerSingleton` static property.
- **Vanilla hide/show** — caches the DealerApp's `Content.gameObject` and flips `SetActive(false/true)` on toggle. The optimizer panel is a sibling under `appContainer`, drawn over everything with a solid dark background.
- **Table view** — `ScrollView` + `Viewport` (Mask) + `Content` (VerticalLayoutGroup + ContentSizeFitter) tree built from scratch. One row per customer, 7 columns: Name, Assignment, Addiction, Min $, Max $, Preferences, and a "Reassign ▾" button that opens a popup.
- **Reassign popup** — centered modal with a button per option: "Player", then each recruited dealer (disabled with `(full)` suffix if at `MAX_CUSTOMERS=10`, disabled if it's the current assignment), plus Cancel. On click, invokes `ReassignmentService.MoveToPlayer` / `MoveToDealer` and refreshes the table.
- **Data pull** — uses existing `GameDataService.GetAllCustomers() / GetAllDealers()` (read-only reflection) and `ReassignmentService` (verified in Session 6) for mutations. Table calls `GameDataService.InvalidateCache()` before each refresh.

**`src/Patches/DealerAppPatches.cs` (updated)** — `SetOpen_Postfix` now calls `OptimizerTab.OnDealerAppOpened/Closed(__instance)`. No other changes to the patch logic.

**`ClientAssignmentOptimizer.csproj` (updated)** — added `UnityEngine.TextRenderingModule` reference (needed for `Font` and `TextAnchor`, which are separate from UIModule/CoreModule).

**`libs/UnityEngine.TextRenderingModule.dll`** — pulled from Windows PC (`MelonLoader/Il2CppAssemblies/`) via scp for macOS compile.

### Key IL2CPP-specific Findings

- **`new GameObject(name, typeof(RectTransform))` does NOT compile** against Il2Cpp-stripped `UnityEngine.CoreModule.dll`. The constructor signature is `GameObject(string, Il2CppReferenceArray<Il2CppSystem.Type>)`, not `(string, params Type[])`. Standard C# `Type[]` can't be converted implicitly.
  - **Fix:** introduced `NewUIGameObject(string)` helper that builds a 1-element `Il2CppReferenceArray<Il2CppSystem.Type>` via `Il2CppType.Of<RectTransform>()`. Use this everywhere a UI GameObject needs a RectTransform from the start (you can't add RectTransform after Transform exists — Unity refuses).
- **`Font` and `TextAnchor` live in `UnityEngine.TextRenderingModule`**, not `UnityEngine.UIModule`. Missing this reference gives `CS0246: 'Font' could not be found` when using `UnityEngine.UI.Text`.
- **`UnityAction` delegate from managed C# lambda** — `button.onClick.AddListener((UnityAction)(() => { ... }))` compiles and (per Il2CppInterop auto-conversion) should work at runtime. Not yet verified in-game.

### Files Modified
- `src/Patches/DealerAppPatches.cs` — hooked OptimizerTab into the SetOpen postfix
- `ClientAssignmentOptimizer.csproj` — added `UnityEngine.TextRenderingModule` reference
- `docs/SESSION_LOG.md` — this entry

### Files Created
- `src/UI/OptimizerTab.cs` — optimizer tab UI (this session's main deliverable)
- `libs/UnityEngine.TextRenderingModule.dll` — compile-time reference pulled from game install

### Decisions Made
1. **Build UI entirely from code with uGUI primitives** (not clone a game prefab, not use IMGUI). Pros: no dependency on the game's specific UI hierarchy, no risk of ruining vanilla when cleanup fails. Cons: ~570 lines to set up components; first-paint polish will need a second pass.
2. **Custom dropdown as a popup of buttons**, NOT Unity's `Dropdown` component. Unity's `Dropdown` requires a full template hierarchy that's painful to construct from scratch; a simple vertical-button popup covers the reassignment UX without the boilerplate.
3. **Hide vanilla by toggling `Content.gameObject.SetActive(false)`**, not by destroying or reparenting. Trivially reversible on close.
4. **Don't patch `Refresh()`** — the table pulls fresh data from `GameDataService` on each open and after each mutation. Simpler and enough for v1.

### Known Risks / Open Questions (pre-test)
- **Orientation flip at the right time.** `SetIsHorizontal(true)` is called synchronously before the panel is shown. If the rotation animation takes a while and our panel is already anchored under `appContainer`, it may rotate correctly. But if `appContainer` is positioned oddly in landscape, the panel may end up off-screen. Need in-game verification.
- **Button click delegates via Il2CppInterop.** Straight `(UnityAction)(() => ...)` cast may or may not survive the Il2Cpp boundary. If clicks don't fire, fall back to `Il2CppInterop.Runtime.Injection.DelegateSupport.ConvertDelegate<UnityAction>(...)`.
- **Font resolution.** `Resources.GetBuiltinResource<Font>("Arial.ttf")` may return null in Unity 2022. Fallback uses `Resources.FindObjectsOfTypeAll<Text>()[0].font` — should find something since the dealer app has Text labels.
- **`DealerApp.Content` vs. `appContainer`.** Per FINDINGS Session 7 both exist on DealerManagementApp. Our panel is placed under `appContainer` (sibling of `Content`) so hiding `Content` doesn't hide our panel. If `appContainer` isn't what we think it is, no UI will appear.

### Deployed DLL: 2026-04-23 (Session 8)
Build clean (1 warning: unused `_initialized` field in DealerAppPatches, benign). DLL pushed to PC via `./deploy.sh`. Awaiting first in-game test.

### Next Steps (for Session 8 continued)
1. User launches game → opens phone → opens DealerApp → taps "Optimize"
2. Pull `Latest.log` via `./deploy.sh --logs` and check for:
   - `[OptimizerTab] Optimize toggle button injected.`
   - `[OptimizerTab] Entering optimizer mode.`
   - `[OptimizerTab] Phone.SetIsHorizontal(True).`
   - `[OptimizerTab] Table refreshed: N customers, M recruited dealers.`
3. Iterate on any exceptions, especially around Il2Cpp delegate conversion, null fonts, or panel positioning in landscape mode.

---

### Session 8 — Continued: Tested In-Game, Full Loop Works (2026-04-23, 23:45–23:52)

**In-game test outcome: success.** Full open → optimize → landscape → reassign → close → portrait → re-open cycle works clean. ~8 reassignments across all three directions (Dealer→Player, Player→Dealer, Dealer→Dealer) completed without a single exception.

#### Iteration log (what broke, what fixed it)

Deploys were blocked on DLL file-lock while the game process held the mod; implemented a background `scp` retry loop so pushes auto-succeed the moment the user quits to desktop.

1. **First deploy — `InvalidCastException` on `appContainer` read (23:17).**
   - Cause: `ReflectGet<RectTransform>(dealerApp, "appContainer")` — Il2CppInterop's reflection proxy returned the property value wrapped as base `Transform`, then our explicit cast / `as` path threw.
   - Fix: refactored `ReflectGet<T>` to constrain to `Il2CppSystem.Object` and use `Il2CppObjectBase.TryCast<T>()` for narrowing. Still threw — exception was **before** TryCast ran, inside Il2CppInterop's property getter.
   - Real fix: pull the raw value as the base `Transform`, then `Transform.gameObject.GetComponent<RectTransform>()` — Unity's native GetComponent path returns the correct wrapper type.

2. **Second deploy — same `InvalidCastException` but in `CreateButton` (23:37).**
   - Stack trace showed the throw originated at `CreateButton` → the line `FillParent((RectTransform)textGO.transform)`. Same class of bug: explicit `(RectTransform)someTransform` cast fails in IL2CPP because the `.transform` property's wrapper is typed as `Transform`.
   - Fix: replaced every `(RectTransform)foo.transform` across the file with `foo.gameObject.GetComponent<RectTransform>()` (or `foo.GetComponent<RectTransform>()` when we already held the GameObject).

3. **Third deploy — "Phone not resolved" (23:45).**
   - Cause: `Phone.Instance` is declared on the generic base `PlayerSingleton<T>`, not on `Phone`. My `GetProperty("Instance", Static)` missed it even with `FlattenHierarchy` (Il2CppInterop doesn't always flatten over generic-base statics).
   - Fix: if the direct lookup fails, walk `_phoneType.BaseType` up the chain and search each base for `Instance`. Works.

4. **Fourth deploy — font unreadable, no way to close (23:51).**
   - Cause A: text sizes were 12–13 px — fine on a 1080p editor preview, unreadable on the in-game phone.
   - Cause B: toggle button ("Close" label) was a sibling of the optimizer panel under `appContainer`, added first → drawn under the panel, un-clickable.
   - Fix A: bumped all font sizes (title 18→28, header 13→20, rows 12→18, buttons 13→18); row height 30→44; toggle button 120×36 → 160×48.
   - Fix B: `_toggleButtonGO.transform.SetAsLastSibling()` on `EnterOptimizerMode()`. Last sibling in uGUI wins for both draw order and raycast priority.

#### In-game reassignment events observed (all clean, no exceptions)

| Time | Customer | From | To | Source Count | Target Count |
|---|---|---|---|---|---|
| 23:45:36 | Sam Thompson | PLAYER | Molly Presley | — | 9 → 10 |
| 23:45:40 | Austin Steiner | Benji Coleman | PLAYER | 10 → 9 | — |
| 23:45:47 | Geraldine Poon | PLAYER | Benji Coleman | — | 9 → 10 |
| 23:45:51 | Kim Delaney | Molly Presley | PLAYER | 10 → 9 | — |
| 23:50:52 | Chloe Bowers | Benji Coleman | PLAYER | 10 → 9 | — |
| 23:50:55 | Trent Sherman | Molly Presley | PLAYER | 9 → 8 | — |
| 23:50:58 | Kevin Oakley | Brad Crosby | PLAYER | 10 → 9 | — |
| **23:51:01** | **Eugene Buckley** | **Brad Crosby** | **Molly Presley** | **9 → 8** | **8 → 9** |
| 23:51:04 | Trent Sherman | PLAYER | Benji Coleman | — | 9 → 10 |
| 23:51:12 | Elizabeth Homley | Brad Crosby | PLAYER | 8 → 7 | — |

**Eugene Buckley (23:51:01) is the first verified dealer-to-dealer direct transfer** — previously deferred as a trivial composition, now confirmed. Call sequence: `Brad.RemoveCustomer + Molly.AddCustomer + AssignDealer(Molly)`, all three methods returned successfully, both dealers' counts adjusted correctly, customer's AssignedDealer matches the new target.

#### Confirmed working end-to-end

1. **Toggle button injection** — top-right of dealer app, rendered with 18pt "Optimize" label.
2. **Phone orientation flip** — landscape on enter, portrait on exit. Native animation plays cleanly.
3. **Vanilla content hide/show** — `Content.gameObject.SetActive(false/true)` is a reversible, non-destructive swap.
4. **Customer table** — 39 rows × 7 columns rendered in the ScrollView. All data populated (name, assignment, addiction, min/max spend, preferences).
5. **Reassign popup** — button-per-option list with "Player" + 3 recruited dealers, current assignment and full (10/10) dealers correctly disabled.
6. **ReassignmentService mutations** — all three directions work, counts stay in sync, no exceptions.
7. **Close flow** — toggle button flips to "Close", `SetAsLastSibling` keeps it on top of the panel, tapping it exits optimizer mode cleanly.
8. **Re-toggle** — closing and re-opening the dealer app, or toggling optimizer mode twice in one session, both work cleanly.

#### Documentation updates (this session)
- `docs/FINDINGS.md` — added "uGUI GameObject Construction in IL2CPP" (Il2CppReferenceArray + TextRenderingModule reference); will add "Explicit casts on Transform throw" learning
- `docs/ROADMAP.md` — Phase 3.5 checkboxes all checked; dealer-to-dealer verification promoted from Phase 3 deferred → done
- `docs/SESSION_LOG.md` — this continuation

#### Known polish items (post-Session-8)
- Data refresh cadence — currently only refreshes on optimizer mode enter + after each mutation. If the player reassigns customers via the vanilla UI while the optimizer panel is hidden, the cached data may briefly go stale on re-enter (InvalidateCache does clear it, so next enter picks up fresh state). Probably fine.
- Column widths are proportional to fixed weights — long customer names or preference lists overflow. Overflow mode is "Overflow" horizontally so nothing gets clipped, but layout may look uneven.
- No sort / filter / flag UI yet (Phase 4 work).

---

### Session 8 — Continued: Phone Orientation Pivot & Canvas Rendering Issue (2026-04-23 → 2026-04-24)

**User feedback after first successful render:** "the phone switched to landscape, the content was still rendering in portrait (in essence the phone was sideways, but the content on it was still in portrait so it didn't rotate the 90 degrees it needed to). the text is better, but still really hard to see."

#### What this means

`Phone.SetIsHorizontal(true)` rotates the **phone model in world space** (animated via `orientation_Vertical` / `orientation_Horizontal` transform targets). It does NOT re-layout app content. Because our optimizer panel was parented under `appContainer` (a child of the rotating phone), the panel rotated with the phone and appeared as sideways text from the user's viewing angle.

The game's native landscape apps (e.g. `ProductManagerApp`) must have their content authored IN horizontal orientation (their `Orientation` property is `EOrientation.Horizontal` and their `appContainer` is shaped for landscape). Vanilla `DealerManagementApp` is portrait — trying to retrofit landscape onto it via `SetIsHorizontal` just rotates its appContainer sideways.

#### Pivot: standalone fullscreen Canvas

Dropped `Phone.SetIsHorizontal` entirely. New design:
- Create a new top-level `Canvas` (`RenderMode.ScreenSpaceOverlay`) that is NOT a child of the phone, NOT affected by phone rotation, and gets the whole screen
- `CanvasScaler` with `ScaleWithScreenSize`, reference 1920×1080, so text scales naturally on any resolution
- High `sortingOrder` (first tried 1000, then bumped to 32000 with `overrideSorting=true`) to draw above everything
- The `Close` button lives inside this overlay itself, so user can always dismiss regardless of phone state
- `GameObject.DontDestroyOnLoad` on the canvas so it survives scene transitions

#### Current blocker: canvas builds but nothing renders

After deploying the standalone-canvas version (23:57 build), user reported: **"when I click the button, nothing actually happens on screen. the button changes to close, but the content doesnt change."**

What we know:
- `[OptimizerTab] Entering optimizer mode.` → fires on click (click handler reaches us)
- `[OptimizerTab] Table refreshed: 39 customers, 3 recruited dealers.` → fires (data layer works)
- The dealer-app toggle button's label flips Optimize → Close correctly (confirming our code runs)
- BUT: the fullscreen overlay is not visible on screen — dealer app still fully visible, user can still tap the (now "Close"-labeled) toggle button to exit

Hypotheses (in order of likelihood):
1. **Sort order still too low** — Schedule 1's phone/HUD canvases may have custom high sortingOrders. Deployed a diagnostic build (00:01 Session 8 continued) that sets `sortingOrder=32000` and `overrideSorting=true`, and adds a log line: `Canvas built: enabled=..., sortingOrder=..., rectSize=..., screen=...`. **Not yet verified in-game** — user going to bed.
2. **Canvas RectTransform has zero size** — ScreenSpaceOverlay should auto-size from Screen.width/height, but if the Canvas component was added before Unity auto-attached a RectTransform, the rect could be 0×0 and nothing would render. The diagnostic log line will tell us.
3. **IL2CPP GraphicRaycaster / Canvas interaction** — less likely; `ScreenSpaceOverlay` canvases don't need a raycaster unless we want UI clicks. We do, so the GraphicRaycaster is added. Should be fine.
4. **Phone or game UI uses camera-space / world-space canvases and renders on top via sortingLayerName** — possible but unusual.

#### Active state at time of handoff

- Latest deployed DLL (00:01): fullscreen canvas, `sortingOrder=32000`, diagnostic log for canvas dimensions. **NOT TESTED YET.**
- Open GitHub issues filed for: canvas rendering bug, Phase 4 work, the deferred items. See repo issue tracker.
- Test save: 39 unlocked customers, 3 recruited dealers (Brad, Molly, Benji). All 10 reassignments from earlier Session 8 testing DID persist across the Phase 3 verification, so the save state is useful for further testing.

#### Next session first steps

1. Launch game, open phone → DealerApp → tap Optimize. Check log for `Canvas built: ...` line.
   - If `rectSize=0x0` → the RectTransform isn't getting auto-sized. Need to set its size manually or adjust how we build the Canvas GameObject.
   - If `rectSize=1920x1080` (or screen size) but still not visible → sort order hypothesis wrong; investigate other canvases / check RenderMode
2. If canvas is rendering but in wrong coordinate system, consider: the `_optimizerRootGO`'s Image may need `raycastTarget=true` AND a visible sprite (not just color). Verify with a larger, more contrasty Image first.
3. Worst case, fall back to parenting the panel under the phone's `AppsCanvas.canvas` (which is already rendering) with a very high sibling/sortingOrder override there.

#### Files edited in this continuation
- `src/UI/OptimizerTab.cs` — removed SetIsHorizontal calls, added new top-level Canvas build path, added close button in-panel, bumped fonts for fullscreen reference resolution, added canvas diagnostic logging
- `docs/SESSION_LOG.md` — this handoff entry
- `docs/ROADMAP.md` — Phase 3.5 re-opened (verification block)
- GitHub issues — filed (see tracker)

---

## Session 9 — Canvas Render Fixed, Phase 3.5 Complete (2026-04-25, 00:24–00:31)

**Goal:** Unblock issue #1 (optimizer overlay invisible) and finish Phase 3.5.

#### Two bugs found by reading the code, both fixed in one build

1. **Canvas GameObject lacked RectTransform.** `BuildOptimizerPanel` was constructing the top-level canvas with `new GameObject("OptimizerCanvas")` followed by `AddComponent<Canvas>()`. In IL2CPP-stripped Unity, Canvas's `[RequireComponent(typeof(RectTransform))]` does not promote the GO's Transform → RectTransform. Result: ScreenSpaceOverlay never auto-sized, no rect to render into. Fix: use `NewUIGameObject("OptimizerCanvas")` so the GO has a RectTransform from the constructor — same pattern already used everywhere else in this file.

2. **Inner panel root permanently inactive.** `BuildOptimizerPanel` ended with `_optimizerRootGO.SetActive(false)`, but `EnterOptimizerMode` only called `_optimizerCanvasGO.SetActive(true)`. The canvas became visible but its only child (everything in the optimizer) stayed hidden forever. Fix: deactivate the canvas GO at end of build instead, and toggle visibility on the canvas. Root stays active.

#### Verification (2026-04-25 00:30, in-game)

Diagnostic line confirmed canvas now correctly sized:

```
[OptimizerTab] Canvas built: enabled=True, sortingOrder=32000, rectSize=3440x1440, screen=3440x1440, canvasActive=True
```

Then live in-game test ran 7 reassignments through the visible overlay in 50 seconds with the table refreshing between each:

| Time | Customer | From → To | Sequence | Result |
|---|---|---|---|---|
| 00:30:23 | Mick Lubbin | Player → Molly | AddCustomer + AssignDealer | ok (Molly 9→10) |
| 00:30:29 | Jessi Waters | Benji → Player | RemoveCustomer + AssignDealer(null) | ok (Benji 10→9) |
| 00:30:31 | Donna Martin | Benji → Player | RemoveCustomer + AssignDealer(null) | ok (Benji 9→8) |
| 00:30:34 | Kathy Henderson | Benji → Player | RemoveCustomer + AssignDealer(null) | ok (Benji 8→7) |
| 00:30:48 | Austin Steiner | Benji → Player | RemoveCustomer + AssignDealer(null) | ok (Benji 7→6) |
| 00:30:55 | Kim Delaney | Molly → Player | RemoveCustomer + AssignDealer(null) | ok (Molly 10→9) |
| 00:30:57 | Beth Penn | Benji → Player | RemoveCustomer + AssignDealer(null) | ok (Benji 6→5) |
| 00:31:00 | Kevin Oakley | Brad → Molly | Remove + Add + AssignDealer | ok (Brad 10→9, Molly 9→10) |

All three call directions (Dealer→Player, Player→Dealer, Dealer→Dealer) verified live with the new visible overlay. Zero exceptions. The user did mass-reassignment as a stress test — overlay stayed responsive.

#### Cleanup landed in same change

- `OnDealerAppClosed` no longer calls the abandoned `SetPhoneHorizontal(false)` — replaced with `_optimizerCanvasGO.SetActive(false)` so closing the dealer app while the optimizer is open hides the overlay correctly. The legacy SetPhoneHorizontal call was a no-op (phone was always already vertical) but kept the dead-code path live.

#### What's still in the file but unused

- `SetPhoneHorizontal` / `ResolvePhone` and the `_phoneType` / `_phoneInstanceProp` / `_phoneSetIsHorizontal` reflection still exist in `OptimizerTab.cs` but are no longer called anywhere. Deferred — leaving for now since it's harmless and future "real phone app" work (issue #10) may want similar reflection. If we close issue #10 by NOT going down that path, strip these.

#### Findings promoted

- `docs/FINDINGS.md` — new section "ScreenSpaceOverlay Canvas Needs RectTransform-from-Construction (Session 9)" generalizes the existing Session-8 uGUI-construction rule to top-level Canvas roots, with the `rectSize=3440×1440` evidence.

#### Issue / roadmap state

- Closing `joelallouz/schedule1-mod#1` ("Optimizer overlay canvas not visible on screen") — root cause identified, fixed, verified.
- ROADMAP Phase 3.5 marked COMPLETE.
- Phase 4 (issues #2–#4) is the next target.

#### Files edited in Session 9
- `src/UI/OptimizerTab.cs` — Canvas via NewUIGameObject; toggle canvas instead of root; OnDealerAppClosed cleanup
- `docs/FINDINGS.md` — new ScreenSpaceOverlay Canvas finding
- `docs/ROADMAP.md` — Phase 3.5 marked complete
- `docs/SESSION_LOG.md` — this entry

---

## Session 9 — Continued: Re-parented Inside Phone, Sort + Spend Columns (2026-04-25, 00:34–00:45)

After the fullscreen-canvas version landed, user feedback: the overlay covered the whole screen instead of fitting inside the phone, and the Close button + Esc didn't dismiss it. Also asked for header sort and clearer Min/Max $ labels.

#### Pivot back to in-phone parenting

The Session-8 reason we went fullscreen was `Phone.SetIsHorizontal` rotating the phone model and dragging the panel sideways. The fullscreen path solved that but introduced "covers entire screen" — wrong UX for a phone app. Resolution: parent the optimizer panel back under the dealer app's `appContainer` (where the toggle button already lives), but **never call `SetIsHorizontal`**. The phone stays portrait, the panel inherits the phone's rect for free, no standalone canvas needed.

#### Bugs fixed in this iteration

1. **Close button blocked by Title's raycast.** `CreateButton`'s inner Text was created with default `raycastTarget=true`, so the Text on TOP of every Button was eating the click before it reached the Button's Image (which is the actual `targetGraphic`). The reassign-row buttons happened to work despite this in the previous build because they're below the title's vertical band, but the Close button was directly under the Title text. Fix: `txt.raycastTarget=false` on the inner Text in `CreateButton`, plus `raycastTarget=false` on the Title text. This unblocks every button across the panel.

2. **Empty panel after re-parenting.** First in-phone build showed an empty rect — toggle button worked but panel had no visible content. Diagnostic confirmed panel rect was correct (`655x1201` matching `appContainer`) and active. Cause was sibling order combined with possible LayoutGroup interference: panel had been added with `SetAsFirstSibling` (drawn first / behind) and other dealer-app siblings were rendering over it. Fix: `SetAsLastSibling` on the panel in `EnterOptimizerMode` (then re-`SetAsLastSibling` on the toggle so it stays clickable on top), plus `LayoutElement.ignoreLayout=true` on the panel so any LayoutGroup on `appContainer` doesn't overwrite our full-stretch anchors.

#### New behavior

- **Header click-to-sort.** Each sortable column header is a Button. First click sets ascending; another click on the same column flips to descending. Clicking a different column resets it to that column ascending. Active column shows ▲/▼ indicator. The Reassign column is not sortable. Sort applies in `RefreshTable` before rendering rows.
- **Merged spend column.** Was `Min $` + `Max $` (six total columns). Now one `Weekly $` column showing `$min – $max`. Saves a column on the narrower portrait phone screen and clarifies what the values represent. Sorts by max.
- **Vanilla content hidden while optimizer is open**, restored on close. The dealer-app's `Content` GO is what the dealer view normally displays; we toggle its `activeSelf`.

#### Verification (2026-04-25 00:43, in-game, in-phone build)

User confirmed: panel renders inside the phone with full content. Reassignment works:
- Sam Thompson Player → Molly Presley (Molly 9→10) — ok
- Kevin Oakley Brad → Player (Brad 10→9) — ok

Close button dismisses the panel cleanly. Multiple open/close cycles in the log without state corruption.

#### Code cleanup

- Dropped all `Phone.SetIsHorizontal` reflection (`_phoneType`, `_phoneInstanceProp`, `_phoneSetIsHorizontal`, `_phoneResolved`, `ResolvePhone`, `SetPhoneHorizontal`). It's been dead since the fullscreen pivot — and confirmed unneeded by the in-phone path.
- Removed the temporary `DiagnosePanel` log helper after it served its purpose; can be re-added if a layout regression appears.
- Removed the speculative sub-Canvas / overrideSorting addition I drafted before the re-parent fix was confirmed sufficient. Sibling order alone works once `SetAsLastSibling` + `LayoutElement.ignoreLayout` are in place.

#### Findings

- **Inner-Text raycastTarget always blocks the parent Button.** Every uGUI button built as `Image+Button` with a Text child needs `txt.raycastTarget = false` on the inner Text, or the Text intercepts every click and the Button's onClick never fires. This is a well-known Unity gotcha but it bit us specifically because the Close button sat in the same vertical band as a wide Title text. Documented in FINDINGS.
- **Full-stretch panels under `appContainer` need `LayoutElement.ignoreLayout=true`.** Whatever layout grouping the dealer app's container uses, our full-stretch overlay needs to opt out so its anchors aren't overwritten. Documented in FINDINGS.

#### Files edited
- `src/UI/OptimizerTab.cs` — full rewrite of the build path: panel parents under appContainer; sort state + click handlers; merged spend column; raycastTarget=false on inner button Texts and Title; LayoutElement.ignoreLayout on panel; SetAsLastSibling on panel + toggle in EnterOptimizerMode; dropped all SetIsHorizontal reflection
- `docs/FINDINGS.md` — append two findings: inner-Text raycast blocking, and panel-under-LayoutGroup ignoreLayout
- `docs/SESSION_LOG.md` — this entry

---

## Session 9 — Continued: Landscape Mode + Font Readability (2026-04-25, ~13:50–later)

User feedback after the in-phone build landed: "showing up in portrait mode and never landscape." Native phone apps that are landscape-authored (e.g. ProductManagerApp) physically rotate the phone via `Phone.SetIsHorizontal(true)`. The dealer app is portrait-authored, but we want our optimizer to render landscape inside it.

#### Approach: rotate phone + counter-rotate panel

1. **Phone rotation:** call `Phone.SetIsHorizontal(true)` on `EnterOptimizerMode` and `false` on `ExitOptimizerMode`. This animates the phone model to a landscape pose in 3D world space.
2. **Counter-rotation:** without compensation, our panel — parented under `appContainer` — rotates with the phone, so text reads sideways from the user's POV (this was the Session 8 problem). Apply `localRotation = Quaternion.Euler(0, 0, -90)` to the panel so the local rotation cancels the world rotation; combined panel-world rotation is 0° and text reads normally.
3. **Sized for landscape:** panel `sizeDelta = (appContainer.rect.height, appContainer.rect.width)` — a swap so that, after the -90° local rotation, the panel's bounding box in `appContainer` local space exactly fills `appContainer`'s rect (and after world rotation, it's landscape-shaped from the user's view).
4. **Anchors at center, pivot center:** so the rotation pivots cleanly around the panel's middle.
5. **Reset on exit:** `localRotation = identity` + `FillParent(rt)` to restore portrait when the optimizer closes (vanilla content shows up, phone rotates back).

Phone reflection (`_phoneType`, `_phoneInstanceProp`, `_phoneSetIsHorizontal`) re-added with the same base-walk pattern from Session 8 (FindType + walk `BaseType` for `Instance` because `PlayerSingleton<T>.Instance` lives on the generic base and `FlattenHierarchy` doesn't reliably cross it in Il2CppInterop).

#### Rotation direction calibration

First iteration used `Euler(0, 0, +90)`. User reported: "the landscape view of the dealer optimizer was upside down." Combined panel-world rotation was +180° (panel local +90 + phone +90 = 180 = upside-down). Flipped to `Euler(0, 0, -90)` so the rotations cancel. Confirmed correct on next test.

#### Font bump for in-phone readability

After landscape worked, user feedback: "the font in the list is still very hard to see and read." The previous values were calibrated for the standalone-fullscreen build (which got a 1920×1080 reference). Inside the phone canvas — which renders at a much smaller pixel scale on screen — the same point sizes appear small.

New sizes (all increased ~40-50%):
- Title: 28 → 36
- Headers (sortable + non-sortable): 22 → 28
- Row cells: 18 → 26
- Button text default (Close, Reassign row buttons, popup buttons): 18 → 24
- Reassign popup title: 18 → 26

Layout dimensions adjusted to fit:
- Title height: 36 → 44, anchor unchanged (-8 from top)
- Header anchor: -52 → -64, height 36 → 44
- Scroll view top offset: -92 → -116 (pushed down to clear larger title+header band)
- Scroll view bottom offset: 56 → 68 (room for larger close button)
- Close button: 140×40 → 160×52
- Row height: 44 → 58
- Popup row height: 44 → 56, vertical spacing 6 → 8

#### Verification (in-game, 2026-04-25 13:53)

Logged via filtered grep on Latest.log:
- `[13:53:09] Entering optimizer mode.` — landscape transition fires
- `[13:53:17] Sort: column=3 (Weekly $), ascending=True` — sort works (first time verified live)
- `[13:53:18] Sort: column=3 (Weekly $), ascending=False` — toggle direction works
- `[13:53:45] Reassign Peggy Myers PLAYER → Leo Rivers` — reassign works in landscape
- `[13:53:51] Closed` — clean exit, no errors

User confirmed: rotation is correct after the +90 → -90 flip, and the optimizer is "functionally working well." Font bump going out next (sent to PC, awaiting visual confirmation).

#### What still uses the legacy F9 IMGUI path

- `src/UI/CustomerPanelUI.cs` (Phase 2 hotkey panel) is still wired up via ModEntry. F9 toggles it. Visible in the same log session at 13:53:58 and 13:54:31 — user fell back to it for fast bulk reassignment after closing the dealer app. This path remains until issue #9 ("Replace F9 IMGUI fallback once phone UI is stable") is done. No change needed yet — it's an intentional fallback.

#### Files edited
- `src/UI/OptimizerTab.cs` — added Phone reflection + SetPhoneHorizontal + ApplyLandscapePanelLayout; rotation +90 → -90; font bumps + layout offset bumps across panel/header/rows/popup/close button
- `docs/SESSION_LOG.md` — this entry

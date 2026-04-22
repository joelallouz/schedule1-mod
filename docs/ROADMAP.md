# Roadmap

## Phase 0: Bootstrap — COMPLETE

**Goal:** Create a clean, buildable project with logging, discovery infrastructure, and documentation.

- [x] Repository structure
- [x] MelonLoader mod entry point
- [x] Logging system
- [x] Discovery framework (DiscoveryOrchestrator, RuntimeScanService, ReflectionUtils, DumpUtils)
- [x] Documentation foundation (PRD, roadmap, architecture, session log, etc.)
- [x] Verify mod loads in-game
- [x] Verify discovery output is useful

**Completed:** Session 1 (2026-04-22)

---

## Phase 1: Discovery (Current)

**Goal:** Reverse-engineer how the game stores client data, assignments, spending, and addiction.

- [x] Run initial discovery scan and capture output
- [x] Identify key assemblies: Assembly-CSharp (~3705 types), Il2CppScheduleOne.Core (~46 types)
- [ ] **Find the class(es) representing a client/customer** ← Session 2 target
- [ ] Map fields: name, assignment, spend, addiction, preferences
- [ ] Determine how "assignment" is stored (reference to dealer? enum? flag?)
- [ ] Determine whether weekly spend is a direct field or computed
- [ ] Determine how to enumerate all clients at runtime
- [ ] Document all findings in FINDINGS.md
- [x] Build targeted discovery scans (TypeSearchService — keyword search across game assemblies)

**Exit criteria:** We know exactly which classes and fields to read, documented with evidence.

---

## Phase 2: Read-Only Client View

**Goal:** Display a list of all clients with their key stats. No mutation.

- [ ] Domain models mirroring discovered game structures
- [ ] Service to enumerate clients and read their properties
- [ ] Basic UI panel (UnityEngine.GUI or similar)
- [ ] Display: name, assignment, weekly spend, addiction, preferences
- [ ] Toggle panel with a hotkey

**Exit criteria:** Player can press a key and see a list of all clients with correct data.

---

## Phase 3: Reassignment

**Goal:** Allow the player to reassign clients between dealers and themselves.

- [ ] Discover how reassignment works in game code
- [ ] Identify the method or state change required
- [ ] Implement reassignment action (button per client or drag-drop)
- [ ] Validate constraints (can every client be reassigned? cooldowns? limits?)
- [ ] Error handling for failed reassignment

**Exit criteria:** Player can select a client and move them to a different dealer or to themselves.

---

## Phase 4: Flagging and Filtering

**Goal:** Automatically highlight high-value clients that should be player-assigned.

- [ ] Configurable spend threshold
- [ ] "Should Be Player" flag on dealer clients above threshold
- [ ] Sort by: spend, assignment, addiction, flag status
- [ ] Filter by: assigned-to, flagged, product preference
- [ ] Summary stats (total player revenue, total dealer revenue, potential gains)

**Exit criteria:** Player can instantly see which dealer clients to poach for maximum revenue.

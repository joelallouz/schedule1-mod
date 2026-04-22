# Roadmap

## Phase 0: Bootstrap — COMPLETE

**Goal:** Create a clean, buildable project with logging, discovery infrastructure, and documentation.

- [x] Repository structure
- [x] MelonLoader mod entry point
- [x] Logging system
- [x] Discovery framework
- [x] Documentation foundation
- [x] Verify mod loads in-game
- [x] Verify discovery output is useful

**Completed:** Session 1 (2026-04-22)

---

## Phase 1: Discovery (Current)

**Goal:** Reverse-engineer how the game stores client data, assignments, spending, and addiction.

- [x] Run initial discovery scan and capture output
- [x] Identify key assemblies: Assembly-CSharp (3705 types), Il2CppScheduleOne.Core (46 types)
- [x] Build targeted discovery scans (TypeSearchService)
- [x] **Find the class(es) representing a customer** → `Il2CppScheduleOne.Economy.Customer`
- [x] **Find the class(es) representing a dealer** → `Il2CppScheduleOne.Economy.Dealer`
- [x] **Map fields: assignment** → `Customer._AssignedDealer_k__BackingField` (Dealer ref)
- [x] **Map fields: addiction** → `Customer._CurrentAddiction_k__BackingField` (float)
- [x] **Map fields: spend** → `CustomerData.MinWeeklySpend/MaxWeeklySpend` + `GetAdjustedWeeklySpend()`
- [x] **Map fields: preferences** → `CustomerData.PreferredProperties` + `CustomerAffinityData`
- [x] **Determine how to enumerate all customers** → `Customer.UnlockedCustomers` (static list)
- [x] **Determine how to enumerate all dealers** → `Dealer.AllPlayerDealers` (static list)
- [x] Document all findings in FINDINGS.md
- [ ] **Verify runtime access** — can we actually read these properties from mod code?
- [ ] **Identify assignment/reassignment API** — find the method to move a customer between dealers
- [ ] **Dump NPC base class** — get display name, ID fields
- [ ] **Verify null-dealer = player-assigned hypothesis**

**Exit criteria:** We can read customer name, assignment, spend, addiction, and preferences from mod code at runtime. We know the reassignment method.

---

## Phase 2: Read-Only Client View

**Goal:** Display a list of all clients with their key stats. No mutation.

- [ ] Domain models mirroring discovered game structures
- [ ] Service to enumerate customers and read their properties
- [ ] Basic UI panel (UnityEngine.GUI or IMGUI)
- [ ] Display: name, assignment, weekly spend, addiction, preferences
- [ ] Toggle panel with a hotkey

**Exit criteria:** Player can press a key and see a list of all customers with correct data.

---

## Phase 3: Reassignment

**Goal:** Allow the player to reassign customers between dealers and themselves.

- [ ] Discover how reassignment works in game code (partially done — need method name)
- [ ] Implement reassignment action
- [ ] Validate constraints (MAX_CUSTOMERS, cooldowns)
- [ ] Error handling for failed reassignment

**Exit criteria:** Player can select a customer and move them to a different dealer or to themselves.

---

## Phase 4: Flagging and Filtering

**Goal:** Automatically highlight high-value customers that should be player-assigned.

- [ ] Configurable spend threshold
- [ ] "Should Be Player" flag on dealer customers above threshold (v1: spend only)
- [ ] Sort by: spend, assignment, addiction, flag status
- [ ] Filter by: assigned-to, flagged, product preference
- [ ] Summary stats (total player revenue, total dealer revenue, potential gains)

**Exit criteria:** Player can instantly see which dealer customers to poach for maximum revenue.

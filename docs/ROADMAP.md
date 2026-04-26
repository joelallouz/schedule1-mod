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

## Phase 1: Discovery — COMPLETE

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
- [x] **Verify runtime access** — YES: static lists readable via reflection, Il2CppSystem.Collections.Generic.List with working Count/indexer (Session 3)
- [x] **Identify assignment/reassignment API** — `Dealer.AddCustomer(Customer)`, `Dealer.RemoveCustomer(Customer)`, `Customer.AssignDealer(Dealer)` (Session 3)
- [x] **Dump NPC base class** — `fullName`, `FirstName`, `LastName`, `ID`, `GUID` all found (Session 3)
- [x] **Verify null-dealer = player-assigned hypothesis** — CONFIRMED: null = player-assigned (Session 3, third run with loaded save)

**Exit criteria:** We can read customer name, assignment, spend, addiction, and preferences from mod code at runtime. We know the reassignment method.

**Completed:** Session 3 (2026-04-22)

---

## Phase 2: Read-Only Client View — COMPLETE

**Goal:** Display a list of all clients with their key stats. No mutation.

- [x] Domain models mirroring discovered game structures *(Session 3: CustomerInfo, DealerInfo)*
- [x] Service to enumerate customers and read their properties *(Session 3: GameDataService with cached reflection)*
- [x] Basic UI panel (UnityEngine.GUI or IMGUI) *(Session 3: CustomerPanelUI)*
- [x] Display: name, assignment, weekly spend, addiction, preferences *(Session 5: preferences confirmed rendering — `Il2CppScheduleOne.Effects.Effect.Name`)*
- [x] Toggle panel with a hotkey *(Session 3: F9 toggle, F10 refresh)*

**Exit criteria:** Player can press a key and see a list of all customers with correct data.

**Completed:** Session 5 (2026-04-23)

---

## Phase 3: Reassignment — COMPLETE (core functionality)

**Goal:** Allow the player to reassign customers between dealers and themselves.

- [x] Discover how reassignment works in game code *(Session 6: `Dealer.RemoveCustomer(customer)` + `Customer.AssignDealer(target)`)*
- [x] Implement reassignment action *(Session 6: `ReassignmentService` + row-select UI + per-dealer action buttons)*
- [x] Validate constraints (MAX_CUSTOMERS, cooldowns) *(Session 6: MAX_CUSTOMERS checked pre-add; no cooldowns observed)*
- [x] Error handling for failed reassignment *(Session 6: per-invocation try/catch with log lines; returns false on partial failure)*
- [x] Verify move-to-player (Dealer → Player) *(Session 6: Jessi × 2, Dean × 2 — clean)*
- [x] Verify move-to-dealer (Player → Dealer) *(Session 6: Jessi → Benji, 9→10 — clean)*
- [x] Verify save/reload persistence *(Session 6: changes survive full quit-to-menu + reload cycle when user saves first)*
- [x] Verify dealer A → dealer B direct transfer *(Session 8: Eugene Buckley Brad → Molly, clean; both counts adjusted)*
- [ ] Decide on multiplayer / FishNet RPC handling *(deferred — singleplayer works; co-op may need `AddCustomer_Server` RPC variant)*

**Exit criteria:** Player can select a customer and move them to a different dealer or to themselves. **Met.**

**Completed:** Session 6 (2026-04-23)

---

## Phase 3.5: Phone UI Integration — COMPLETE

**Goal:** Replace the IMGUI hotkey panel with an in-phone optimizer tab on the existing DealerManagementApp.

- [x] Discover phone UI system (Session 7 — Phone, App<T>, HomeScreen, EOrientation)
- [x] Harmony-patch `DealerManagementApp.SetOpen(bool)` — postfix firing verified (Session 7)
- [x] Add `UnityEngine.UI` / `UIModule` / `TextRenderingModule` references for uGUI (Session 7–8)
- [x] Build `OptimizerTab` class — toggle button, scrollable table, reassign popup (Session 8)
- [x] Verify reassignment end-to-end (Session 8 — 10 reassignments across all three directions, zero exceptions)
- [x] Readable font sizes on in-game display (Session 8 — bumped 12–13 → 18–28, then 22–42 on fullscreen canvas)
- [x] Close button reachable while optimizer mode is active (Session 8 — now an in-panel button)
- [x] **Pivoted:** dropped `Phone.SetIsHorizontal` — it rotates the phone model but not its app content, which caused the panel to render sideways. Replaced with a standalone fullscreen `Canvas` (ScreenSpaceOverlay, sortingOrder 32000).
- [x] **Canvas render fix (Session 9, 2026-04-25):** two bugs landed together — (1) Canvas GameObject was constructed via `new GameObject(name)` then `AddComponent<Canvas>()`, but in IL2CPP Unity refuses to swap the existing `Transform` for a `RectTransform`, leaving ScreenSpaceOverlay with no rect to size; (2) `_optimizerRootGO.SetActive(false)` at end of `BuildOptimizerPanel` left the panel permanently inactive while only the canvas was being toggled. Fixed by constructing the canvas via `NewUIGameObject(...)` (RectTransform from creation) and toggling visibility on the canvas GO instead of the inner root. Diagnostic confirmed `rectSize=3440×1440` matching screen.
- [x] **Re-parented inside phone (Session 9 cont):** dropped the standalone fullscreen canvas; panel parents directly under `appContainer` so it inherits the phone's rect, render mode, and sort order — renders inside the phone like a native app. Required `LayoutElement.ignoreLayout=true` and `SetAsLastSibling` to override the dealer app's container layout. Vanilla content hides while open, restores on close.
- [x] **Header sort + merged spend column (Session 9 cont):** column headers are now Buttons; click cycles asc/desc with ▲/▼ indicator. Sort verified live in-game on column 3 ("Weekly $"). Merged Min $ + Max $ → single "Weekly $" column showing `$min – $max` range to clarify what the values represent and free up horizontal space.
- [x] **Landscape mode (Session 9 cont):** `Phone.SetIsHorizontal(true)` on enter rotates the phone model; panel applies `localRotation = Euler(0, 0, -90)` + sizeDelta swap to counter-rotate so text reads correctly in the landscape phone. Initial +90 rotation was upside-down (combined +180°), corrected to -90.
- [x] **Font bump for in-phone readability (Session 9 cont):** Title 28→36, headers 22→28, rows 18→26, button text 18→24; row height 44→58; close button 140×40→160×52; layout offsets adjusted to fit.

**Exit criteria:** Player opens phone → DealerApp → taps Optimize → phone rotates to landscape and shows the optimizer panel with the customer table and reassignment dropdowns → taps a column header to sort → taps a reassign dropdown to reassign → taps Close → returns to vanilla dealer view. **Met (Session 9, 2026-04-25).**

---

## Phase 4: Flagging and Filtering

**Goal:** Automatically highlight high-value customers that should be player-assigned.

- [ ] Configurable spend threshold
- [ ] "Should Be Player" flag on dealer customers above threshold (v1: spend only)
- [ ] Sort by: spend, assignment, addiction, flag status
- [ ] Filter by: assigned-to, flagged, product preference
- [ ] Summary stats (total player revenue, total dealer revenue, potential gains)

**Exit criteria:** Player can instantly see which dealer customers to poach for maximum revenue.

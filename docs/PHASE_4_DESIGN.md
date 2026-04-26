# Phase 4 — Flagging & Filtering: Design

**Status:** Design draft. No code yet.
**Audience:** the implementation session (Mac-side Claude) and the human running the mod.
**Prereq:** Phase 3.5 phone UI integration (Sessions 7–9) is landed. The optimizer panel is parented inside `DealerManagementApp.appContainer`, sortable per-column, with an in-panel reassign popup.

---

## 1. Goals (from ROADMAP)

> Automatically highlight high-value customers that should be player-assigned.

Concrete deliverables:

1. **Configurable spend threshold** — persisted across game launches.
2. **"Should Be Player" flag** — a per-customer boolean, computed each refresh, that fires when a *dealer-assigned* customer's spend exceeds the threshold.
3. **Sort by:** spend, assignment, addiction, flag status. (Name + preferences sort already exists.)
4. **Filter by:** assignment (All / Player / specific dealer), flag (All / Flagged-only).
   *Defer* per-preference filtering to v1.1 — preferences are stored as a comma-joined display string today; filtering needs structural data.
5. **Summary stats** — total player weekly $, total dealer weekly $, "potential gain if all flagged → player" weekly $.

Exit criteria, restated: *open the optimizer, glance at the screen, know which dealer customers to poach without doing arithmetic.*

---

## 2. What's already in place (don't redesign)

- **Data layer:** `GameDataService.GetAllCustomers()` returns `List<CustomerInfo>` with `FullName, NpcId, IsPlayerAssigned, AssignedDealerName, CurrentAddiction, MinWeeklySpend, MaxWeeklySpend, Preferences`.
  → Phase 4 needs no new game-side reflection. All flagging is computed from existing fields.
- **Sort infra:** `OptimizerTab._sortColumn / _sortAscending` + `SortCustomers(...)` switch. Adding a new sortable column = add a header cell + a switch case.
- **UI scaffold:** Title (top, 36px), Header row (below title, 36px), ScrollView (rest), Close button (bottom-right, 140×40). Anchored layout — adding a toolbar row only requires shifting the scroll-view's `offsetMin/offsetMax`.
- **Settings infra:** `ModConfig` is currently static C# fields. Phase 4 needs persistence (see §5).

---

## 3. The "Should Be Player" rule (v1)

```
ShouldBePlayer(c) :=
    !c.IsPlayerAssigned          // already player-assigned → already optimal
    AND c.MaxWeeklySpend > T     // T = configured threshold
```

**Why `MaxWeeklySpend` and not `GetAdjustedWeeklySpend(...)`:**
`GetAdjustedWeeklySpend` takes a normalized-relationship float. We don't currently compute that — it would mean reading `Customer.Relationship` (or equivalent), normalizing, and depending on FINDINGS we don't have yet. Max spend is the upper bound and is plenty for "this is a high-value customer." Revisit in v1.1 if false positives are common.

**Why exclude already-player customers:**
A flag that fires on customers you can't move is noise. The whole flag exists to point at *poach candidates*.

**Threshold default:** `$700`. Reasoning: from the verified Session 3 sample (39 customers), max-spend distribution is roughly $500–$1200 with most clusters around $800–$1000. $700 catches the upper third without firing on every dealer customer. User-overridable.

---

## 4. UI placement

The phone is a tight canvas. Panel currently looks like:

```
┌──────────────────────────────────────────┐
│ Customer Optimizer            (title)    │ 36px
├──────────────────────────────────────────┤
│ Name | Assigned | Add | $ | Prefs | Rea  │ 36px (header, sortable)
├──────────────────────────────────────────┤
│ … rows …                                 │
│                                          │
│                                          │ scroll fills middle
│                                          │
│                              [ Close ]   │ 40px (anchored bottom-right)
└──────────────────────────────────────────┘
```

Phase 4 layout:

```
┌──────────────────────────────────────────────┐
│ Customer Optimizer                           │ 36px  title
├──────────────────────────────────────────────┤
│ Threshold $[700] -+   Show:[All ▾]           │ 36px  toolbar  (NEW)
├──────────────────────────────────────────────┤
│ ★ | Name | Assigned | Add | $ | Prefs | Rea  │ 36px  header   (★ col is NEW)
├──────────────────────────────────────────────┤
│ ★ Jessi Waters  Benji   1.0  $200–$1200 …    │
│   Mick Lubbin   Player  0.9  $400–$800  …    │ scroll
│ ★ Eugene B.     Molly   1.0  $500–$1100 …    │
├──────────────────────────────────────────────┤
│ Player $4,200 · Dealers $18,500 · Flag $6,300│ 24px  summary  (NEW)
│                                  [ Close ]   │ 40px  (existing)
└──────────────────────────────────────────────┘
```

### Vertical math

- Title: top 0 → -36
- Toolbar: -36 → -72         (NEW)
- Header: -72 → -108
- Scroll top inset: 108 (was 56) → adjust `scrollRT.offsetMax` from `-92` to `-108`-ish via top anchor; see §7.
- Summary strip: bottom 56 → 80   (NEW, 24 tall, sits *above* the close button)
- Close button: bottom-right, unchanged

### Column changes

7 columns instead of 6. Add `★` (flag) at index 0, narrow weight (≈0.3). Resulting weights:

```csharp
ColumnWeights  = { 0.3f, 1.7f, 1.3f, 0.8f, 1.4f, 2.0f, 1.4f };
ColumnHeaders  = { "★",  "Name", "Assigned", "Addiction", "Weekly $", "Preferences", "Reassign" };
ColumnSortable = { true, true,   true,        true,        true,       true,          false };
```

Cell content for the flag column: `"★"` if flagged, `""` otherwise. Cheap. Optionally tint the whole row's background slightly amber when flagged (e.g. `Color(0.30f, 0.22f, 0.10f, 0.9f)` instead of the default `0.18f, 0.18f, 0.20f, 0.9f`) — that's the "glance and know" affordance.

---

## 5. Settings persistence (MelonPreferences)

Currently `ModConfig` holds runtime-only static fields. For Phase 4 we add user-tunable values that must survive game restart, so we migrate to `MelonPreferences`:

```csharp
// Core/ModConfig.cs (post-Phase-4)
public static class ModConfig
{
    private static MelonPreferences_Category _category;
    private static MelonPreferences_Entry<bool>  _debugLogging;
    private static MelonPreferences_Entry<bool>  _discoveryEnabled;
    private static MelonPreferences_Entry<int>   _spendThreshold;
    private static MelonPreferences_Entry<bool>  _enableFlagging;

    public static void Initialize()  // call from ModEntry.OnInitializeMelon
    {
        _category = MelonPreferences.CreateCategory("ClientOptimizer");
        _debugLogging     = _category.CreateEntry("DebugLogging",     true,  "Verbose debug logging");
        _discoveryEnabled = _category.CreateEntry("DiscoveryEnabled", false, "Run discovery scans on startup");
        _spendThreshold   = _category.CreateEntry("SpendThreshold",   700,   "Flag dealer customers with MaxWeeklySpend above this");
        _enableFlagging   = _category.CreateEntry("EnableFlagging",   true,  "Show the ★ flag column and amber row tint");
    }

    public static bool DebugLogging      { get => _debugLogging.Value;     set => _debugLogging.Value = value; }
    public static bool DiscoveryEnabled  { get => _discoveryEnabled.Value; set => _discoveryEnabled.Value = value; }
    public static int  SpendThreshold    { get => _spendThreshold.Value;   set => _spendThreshold.Value = value; }
    public static bool EnableFlagging    { get => _enableFlagging.Value;   set => _enableFlagging.Value = value; }
}
```

**Notes:**
- `MelonPreferences` writes to `UserData/MelonPreferences.cfg` (TOML). Survives mod updates.
- Default `DiscoveryEnabled` flips to `false` now that discovery is done — it's a debug toggle from here on.
- Setter on `SpendThreshold` writes immediately; we call `_category.SaveToFile(false)` on change to flush. (Without that, the config flushes only on game close.)
- All existing call sites of `ModConfig.DebugLogging` / `ModConfig.DiscoveryEnabled` keep working — same property names, just backed by MelonPreferences now.

**Risk:** if `MelonPreferences.CreateCategory` is called twice in the same process (mod hot-reload during development), the second call no-ops and returns the existing category. Safe. But don't move `Initialize()` into `OnSceneLoaded` — keep it in `OnInitializeMelon` so it runs exactly once per game launch.

---

## 6. Filter state machine

```
enum FilterMode { All, Flagged, Player, ByDealer }

class FilterState {
    FilterMode Mode = FilterMode.All;
    string ByDealerName = null;   // valid only when Mode == ByDealer
}
```

UI: a single button in the toolbar labeled `"Show: All ▾"` / `"Show: Flagged ▾"` / `"Show: Player ▾"` / `"Show: <dealer> ▾"`. Tap opens a popup (reuse the `OpenReassignPopup` styling) with one row per option:

- `All`
- `Flagged only`
- `Player`
- one row per recruited dealer (`recruited` list, same source as the reassign popup)

Filter is applied **after** sort (sort then filter — order doesn't matter mathematically since neither operation reorders the visible result, but keeping sort first means switching filters is cheap: just hide rows from the cached sorted list).

**Implementation:** filter inside `RefreshTable` after the sort step. Don't render rows that fail the predicate. No runtime cost for hidden rows — keeps things simple. (~39 customers max, no perf concern.)

---

## 7. Summary stats

Computed inside `RefreshTable` over the *unfiltered* customer list:

```csharp
float playerWeekly  = sum of c.MaxWeeklySpend where c.IsPlayerAssigned
float dealerWeekly  = sum of c.MaxWeeklySpend where !c.IsPlayerAssigned
float flagPotential = sum of c.MaxWeeklySpend where ShouldBePlayer(c)
```

Display string (single Text component, fontSize 16, bottom-left strip):

```
Player $4,200/wk  ·  Dealers $18,500/wk  ·  Flag $6,300/wk
```

**On `MaxWeeklySpend` not actual revenue:** these numbers are upper-bound projections, not realized revenue. The label says "/wk" not "earned" to communicate that. If we want true earned, we'd integrate `Customer._WeeklyPurchaseRecord_k__BackingField` — defer; the projection is the actionable number.

**On dealer cut not subtracted:** dealer revenue is the customer's spend × (1 - dealer.Cut). We're showing customer-side spend so player and dealer columns are comparable. Document this in a tooltip if/when we add tooltips. Acceptable v1 simplification.

---

## 8. Implementation order (suggested PR breakdown)

Each step is independently runnable in-game. The other session can build/test each before moving to the next.

| # | Step | Files touched | Test |
|---|------|---------------|------|
| 1 | MelonPreferences migration | `ModConfig.cs`, `ModEntry.cs` (call `Initialize()`) | Launch mod → check `UserData/MelonPreferences.cfg` exists with `[ClientOptimizer]` section. |
| 2 | `ShouldBePlayer` computation | `Domain/CustomerInfo.cs` (add property) OR inline in OptimizerTab | Log-only; print flag count on RefreshTable. |
| 3 | Flag column + row tint | `OptimizerTab.cs` (column array changes, AddCustomerRow tint) | Open optimizer → flagged dealer customers show ★ and amber row. |
| 4 | Filter dropdown | `OptimizerTab.cs` (toolbar build, popup reuse, filter predicate in RefreshTable) | Cycle through filter options, verify visible row counts. |
| 5 | Threshold input control | `OptimizerTab.cs` (toolbar) | Tap +/-, watch flag set update live; quit + relaunch, verify persistence. |
| 6 | Summary stats strip | `OptimizerTab.cs` (BuildOptimizerPanel + new RefreshSummary method) | Numbers update on every RefreshTable; reassign a customer, verify totals shift. |

**Step 1 is independent** of the rest — could land standalone as plumbing. Steps 2 + 3 are also a tidy first user-visible PR.

---

## 9. Open design questions (decide before implementing)

1. **Threshold input control mechanism.** Three options:
   - **(a) +/- stepper** in $50 increments. Simple, one tap per change, but tedious for big jumps. *Recommended.*
   - **(b) Number-entry text field** (`InputField`). More flexible, but `UnityEngine.UI.InputField` interaction in IL2CPP-stripped Unity isn't proven yet — adds risk.
   - **(c) Slider** (range $200–$1500). Visual, but imprecise.
   - Default: ship (a); revisit if user asks for finer control.

2. **Should `ShouldBePlayer` consider `MAX_CUSTOMERS=10`?** If the player already has 10 customers (the dealer cap — does it apply to the player slot?), flagging is moot because they can't take more. *Decision: ignore for v1.* In practice the player-slot doesn't appear capped (it's a null-dealer assignment, not a Dealer instance with a list). Confirm during step 2 testing — log if player customer count > 10.

3. **Where do we put the flag column — leftmost or before Reassign?** Leftmost (index 0) makes scanning easier (eye lands on ★ first). Putting it next to Reassign keeps the action cluster on the right. *Recommended: leftmost.*

4. **Row tint vs star-only.** Tint is more discoverable, but on a fullscreen phone canvas it can dominate visual hierarchy if half the rows are flagged. *Recommended: ship both, controlled by a single `EnableFlagging` toggle in MelonPreferences.* If the user finds tint noisy they can turn it off without losing the ★.

5. **Sort tie-breaker on flag column.** If user sorts by ★, what's the secondary sort? *Recommended: secondary by `MaxWeeklySpend` descending* — within the flagged group, biggest fish first. Implement as a stable sort in step 3.

---

## 10. Out of scope for Phase 4

Defer to Phase 5 or v1.1:

- Filter by product preference (needs structural data, not just display string).
- Realized revenue from `WeeklyPurchaseRecord` (needs a separate FINDINGS push).
- Per-dealer summary subtotals.
- Auto-rebalancing ("move all flagged to me with one click").
- Multi-select reassignment.
- The deferred FishNet RPC variant for multiplayer (separate research thread per the parallelization plan).

---

## 11. Notes for the implementation session

- **Don't break the column-index → sort-key map silently.** When you add the flag column at index 0, every existing case in `SortCustomers` shifts by +1. Audit all switch cases and the `_sortColumn = -1` sentinel.
- **`RefreshTable` already calls `GameDataService.InvalidateCache()`** every refresh — no caching concern when threshold changes.
- **The reassign popup positioning math** (`OpenReassignPopup`) hardcodes a row count for `1 title + 1 player + recruited.Count + 1 cancel`. The new filter popup has the same shape; reuse the helper, don't duplicate.
- **Toolbar gotcha:** anchor it `AnchorTopStretch(rt, -52f, 36f)` — same pattern as the existing header at -52, then push the header down to `-88` and the scroll inset's `offsetMax` from `-92` to `-128`. Compute, don't eyeball.

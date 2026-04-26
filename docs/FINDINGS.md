# Findings

**Update this file every session with new discoveries. Cite evidence (log output, class names, field names) for everything in the Confirmed section.**

---

## Confirmed

### Runtime Environment (Session 1)
- **Game type:** IL2CPP
- **Unity version:** 2022.3.62f2
- **Game version:** 0.4.5f2
- **MelonLoader:** v0.7.2 Open-Beta
- **MelonGame attribute:** `("TVGS", "Schedule I")` — confirmed working
- **Evidence:** MelonLoader console header on load

### Assembly Landscape (Session 1–2)
- **Total loaded assemblies:** 235
- **Assembly-CSharp:** 3705 types — primary game logic
- **Assembly-CSharp-firstpass:** 625 types — third-party plugins (Curvy, etc.)
- **Il2CppScheduleOne.Core:** 46 types — game-specific namespace
- **Evidence:** RuntimeScanService output, Session 2 log

### Customer Class (Session 2) — PRIMARY TARGET
- **Class:** `Il2CppScheduleOne.Economy.Customer`
- **Assembly:** Assembly-CSharp
- **Base type:** `NetworkBehaviour`
- **Size:** 182 properties, 305 methods
- **Key properties (instance):**
  - `_AssignedDealer_k__BackingField` → `Dealer` — **THIS IS HOW ASSIGNMENT IS STORED**
  - `_CurrentAddiction_k__BackingField` → `Single` (float) — current addiction level
  - `_WeeklyPurchaseRecord_k__BackingField` → `List` — actual purchase tracking
  - `_NPC_k__BackingField` → `NPC` — link to underlying NPC
  - `customerData` → `CustomerData` (ScriptableObject) — static config data
  - `offeredContractInfo` → `ContractInfo`
  - `_CurrentContract_k__BackingField` → `Contract`
  - `_IsAwaitingDelivery_k__BackingField` → `Boolean`
  - `_CompletedDeliveries_k__BackingField` → `Int32`
  - `_OfferedDeals_k__BackingField` → `Int32`
  - `_HasBeenRecommended_k__BackingField` → `Boolean`
  - `DefaultDeliveryLocation` → `DeliveryLocation`
- **Key properties (static):**
  - `UnlockedCustomers` → `List` — **ALL UNLOCKED CUSTOMERS**
  - `LockedCustomers` → `List` — all locked customers
  - `onCustomerUnlocked` → `Action` — event when customer unlocked
  - `ADDICTION_DRAIN_PER_DAY` → `Single`
  - `DEAL_COOLDOWN` → `Int32`
  - `MAX_ORDER_QUANTITY_PER_PRODUCT` → `Int32` (static `MaxOrderQuantityPerProduct`)
- **Evidence:** TypeSearchService full dump, Session 2 log

### CustomerData ScriptableObject (Session 2) — CONFIG/STATS
- **Class:** `Il2CppScheduleOne.Economy.CustomerData`
- **Base type:** `ScriptableObject`
- **Key properties:**
  - `MinWeeklySpend` → `Single` — **WEEKLY SPEND RANGE (MIN)**
  - `MaxWeeklySpend` → `Single` — **WEEKLY SPEND RANGE (MAX)**
  - `MinOrdersPerWeek` → `Int32`
  - `MaxOrdersPerWeek` → `Int32`
  - `PreferredProperties` → `List` — **PRODUCT PREFERENCES**
  - `DefaultAffinityData` → `CustomerAffinityData` — product type affinities
  - `Standards` → `ECustomerStandard` — quality standards (VeryLow..VeryHigh)
  - `BaseAddiction` → `Single` — starting addiction
  - `DependenceMultiplier` → `Single`
  - `OrderTime` → `Int32`
  - `PreferredOrderDay` → `EDay`
  - `CallPoliceChance` → `Single`
- **Key methods:**
  - `GetAdjustedWeeklySpend(Single normalizedRelationship)` → `Single` — **COMPUTES ACTUAL SPEND**
  - `GetOrderDays(Single dependence, Single normalizedRelationship)` → `List`
- **Evidence:** TypeSearchService full dump, Session 2 log

### Dealer Class (Session 2) — ASSIGNMENT TARGET
- **Class:** `Il2CppScheduleOne.Economy.Dealer`
- **Assembly:** Assembly-CSharp
- **Base type:** `NPC`
- **Size:** 250 properties, 225 methods
- **Key properties (instance):**
  - `_AssignedCustomers_k__BackingField` → `List` — **CUSTOMERS ASSIGNED TO THIS DEALER**
  - `_IsRecruited_k__BackingField` → `Boolean`
  - `_Cash_k__BackingField` → `Single`
  - `_ActiveContracts_k__BackingField` → `List`
  - `DealerType` → `EDealerType` (PlayerDealer or CartelDealer)
  - `Cut` → `Single` — dealer's revenue cut
  - `SigningFee` → `Single`
  - `Home` → `NPCEnterableBuilding`
  - `AssignCustomersDialogue` → `DialogueContainer` — game has dialogue for assignment!
- **Key properties (static):**
  - `AllPlayerDealers` → `List` — **ALL HIRED DEALERS**
  - `MAX_CUSTOMERS` → `Int32` — **MAX CUSTOMERS PER DEALER**
  - `onDealerRecruited` → `Action`
- **Evidence:** TypeSearchService full dump, Session 2 log

### Enums (Session 2)
- **EDealerType:** `PlayerDealer`, `CartelDealer`
- **ECustomerStandard:** `VeryLow`, `Low`, `Moderate`, `High`, `VeryHigh`
- **ERelationshipCategory:** exists (not yet dumped)
- **Evidence:** TypeSearchService enum dumps

### CustomerAffinityData (Session 2)
- **Class:** `Il2CppScheduleOne.Economy.CustomerAffinityData`
- **Key:** `ProductAffinities` (List), `GetAffinity(EDrugType type)` → `Single`
- **Evidence:** TypeSearchService full dump

### Persistence/Save Data (Session 2)
- **DealerData** (`Il2CppScheduleOne.Persistence.Datas.DealerData`):
  - `AssignedCustomerIDs` → `Il2CppStringArray` — customer assignment persisted as string IDs
  - `Cash`, `Recruited`, `ActiveContractGUIDs`
- **CustomerData save** (`Il2CppScheduleOne.Persistence.Datas.CustomerData`):
  - `Dependence` (Single), `ProductAffinities`, `CompletedDeals`, `OfferedDeals`
- **Evidence:** TypeSearchService full dump

### Manager Pattern (Session 2)
- `Il2CppScheduleOne.NPCs.NPCManager` → `NetworkSingleton` — singleton NPC manager exists
- But customers use **static lists on the Customer class itself** for enumeration
- **Evidence:** TypeSearchService match list

### NPC Base Class (Session 3) — NAME/ID FIELDS
- **Class:** `Il2CppScheduleOne.NPCs.NPC`
- **Base type:** `Il2CppFishNet.Object.NetworkBehaviour`
- **Inheritance chain:** NPC → NetworkBehaviour → MonoBehaviour → Behaviour → Component → UnityEngine.Object → Il2CppSystem.Object → Il2CppObjectBase
- **207 public properties total**
- **Name/ID properties:**
  - `String FirstName` — instance
  - `String LastName` — instance
  - `Boolean hasLastName` — instance
  - `String fullName` — instance (likely computed: FirstName + LastName)
  - `String ID` — instance
  - `Guid GUID` / `Guid _GUID_k__BackingField` — instance
  - `String BakedGUID` — instance
  - `String name` — inherited from UnityEngine.Object
  - `String SaveFolderName` — instance
  - `String SaveFileName` — instance
- **Other notable:** `NPCRelationData RelationData`, `EMapRegion Region`, `Sprite MugshotSprite`, `Single Aggression`
- **Evidence:** NPCTypeScanService dump, Session 3 log

### Assignment API (Session 3) — HOW TO MOVE CUSTOMERS
- **Dealer side:**
  - `Void AddCustomer(Customer customer)` — **ADD a customer to this dealer**
  - `Void RemoveCustomer(Customer customer)` — **REMOVE a customer from this dealer**
  - `Void RemoveCustomer(String npcID)` — overload by NPC ID string
  - `Void SendRemoveCustomer(String npcID)` — network send variant
  - Network RPCs: `AddCustomer_Server(String npcID)`, `AddCustomer_Client(NetworkConnection, String npcID)`
- **Customer side:**
  - `Void AssignDealer(Dealer dealer)` — **SET which dealer serves this customer**
  - `Dealer get_AssignedDealer()` — getter
  - `Void set_AssignedDealer(Dealer value)` — setter
- **Reassignment sequence (likely):** Call `oldDealer.RemoveCustomer(customer)`, then `newDealer.AddCustomer(customer)`, and/or `customer.AssignDealer(newDealer)`. Need to test which calls are needed.
- **Evidence:** NPCTypeScanService.SearchAssignmentMethods, Session 3 log

### MAX_CUSTOMERS (Session 3)
- `Dealer.MAX_CUSTOMERS` = **10** (static property, read at runtime)
- Has both getter and setter (`get_MAX_CUSTOMERS()`, `set_MAX_CUSTOMERS(Int32)`) — could potentially be changed, but likely a global constant
- **Evidence:** RuntimeVerificationService, Session 3 log

### IL2CPP List Types (Session 3)
- `Customer.UnlockedCustomers` type: `Il2CppSystem.Collections.Generic.List<Customer>` (NOT System.Collections.Generic.List)
- `Dealer.AllPlayerDealers` type: `Il2CppSystem.Collections.Generic.List<Dealer>`
- Both accessible via reflection: `.Count` property and `Item` indexer work
- **Evidence:** RuntimeVerificationService list type logging, Session 3 log

### Scene Lifecycle (Session 3)
- **'Menu' (index 0):** Main menu. Customer/Dealer lists exist but are empty (0 items).
- **'Main' (index 1):** Gameplay scene. Loaded after menu. Save data populated here.
- Mod fires OnSceneWasLoaded for each transition. Runtime verification must wait for 'Main' scene with populated data.
- **Evidence:** Scene loaded logging, Session 3 log

### Getter Methods (Session 3) — RUNTIME ACCESS PATTERN
- IL2CPP properties accessible via standard .NET reflection
- Both backing field properties (`_X_k__BackingField`) and proper getters (`get_X()`) exist
- Proper getters confirmed: `get_AssignedDealer()`, `get_CurrentAddiction()`, `get_customerData()`, `get_UnlockedCustomers()`, `get_AssignedCustomers()`, `get_MAX_CUSTOMERS()`
- Static property reading works: `UnlockedCustomers`, `AllPlayerDealers`, `MAX_CUSTOMERS` all readable
- **Evidence:** RuntimeVerificationService + Assignment Method Search, Session 3 log

---

### IMGUI Method Availability (Session 6)
- **`UnityEngine.GUILayoutUtility.GetLastRect()` is STRIPPED** in this IL2CPP build — calling it throws `System.NotSupportedException: Method unstripping failed` every invocation.
- **Impact:** Rect-based click hit-testing (the standard IMGUI selection pattern in Mono) does not work. Use native controls (`GUILayout.Button`) for clickable UI elements instead.
- Most `GUILayout.*` / `GUI.*` controls work fine (Button, Label, Box, BeginHorizontal/EndHorizontal, BeginScrollView/EndScrollView, BeginArea/EndArea). Only the rect-query helpers are affected so far.
- Stack trace confirmed: `GUILayoutGroup.GetLast()` is the underlying stripped method.
- **Evidence:** Phase 3 first UI attempt, Session 6 log

### Reassignment API Verified — FULL ROUND-TRIP + PERSISTENCE (Session 6)
**All three direction/sequence combinations confirmed:**

| Direction | Call sequence | Effect |
|---|---|---|
| Dealer A → PLAYER | `A.RemoveCustomer(customer)` + `customer.AssignDealer(null)` | A's count -1; Customer.AssignedDealer = null |
| PLAYER → Dealer B | `B.AddCustomer(customer)` + `customer.AssignDealer(B)` | B's count +1; Customer.AssignedDealer = B |
| Dealer A → Dealer B | A.Remove + B.Add + AssignDealer(B) (composed from above, high confidence, not directly stress-tested) | both sides updated |

**Confirmed properties after every sequence:** `Customer.AssignedDealer` matches the target AND the relevant `Dealer.AssignedCustomers.Count` is adjusted correctly. Both sides stay in sync.

**Save/reload persistence: CONFIRMED.** User ran multiple mutations → saved → quit to menu → reloaded → verified state held. The game must be **manually saved** for reassignments to persist; quit-without-save reverts (expected Schedule I behavior, not a mod issue).

**Minimal call set is sufficient.** We call two methods per direction; we do NOT also need to call the dealer-side helper on both dealers or fire network RPCs. `AddCustomer` / `RemoveCustomer` do not appear to update `Customer.AssignedDealer` internally (otherwise our subsequent `AssignDealer` would be redundant but still observable; it is necessary here).

**Observed in-game (Session 6):** 5 separate reassignment events across 3 play sessions — Jessi Waters (× 3: Benji→Player, Player→Benji), Dean Webster (× 2: Molly→Player twice), all with clean [Reassign] log output and no exceptions.

**Still not verified:**
- Dealer A → Dealer B direct transfer (code path untouched; trivially composed from the two we did test)
- Multiplayer/FishNet — singleplayer only tested. `AddCustomer_Server` / `AddCustomer_Client` network RPC variants exist but were not invoked.

**Evidence:** Session 6 log, `[Reassign]` entries from 21:47–22:01

### Preferred Properties (Session 5) — PRODUCT PREFERENCE TYPE
- `CustomerData.PreferredProperties` is a `List<Il2CppScheduleOne.Effects.Effect>`
- Each `Effect` has: `String Name`, `String Description`, `String ID`, `Int32 Tier`, `Single Addictiveness`, `Color ProductColor`, `Color LabelColor`, `Int32 ValueChange`, `Single ValueMultiplier`, `Vector2 MixDirection`, `Single MixMagnitude`
- Use `Effect.Name` for display (human-readable label)
- Access pattern: `customer.customerData.PreferredProperties[i].Name`
- **Evidence:** `[PrefDebug]` log output (Session 5, `26-4-23_21-26-23.log` range)

---

### Runtime Data Verification (Session 3, third run) — FULL SUCCESS

**Test save:** 39 unlocked customers, 3 recruited dealers (Brad, Molly, Benji) with 10 customers each, 9 player-assigned customers.

**Customer data confirmed at runtime (first 5 of 39):**

| Name | NPC.ID | AssignedDealer | Addiction | Min/Max Spend | Standards |
|---|---|---|---|---|---|
| Kyle Cooley | kyle_cooley | Benji Coleman | 1.0 | $400-$900 | Low |
| Mick Lubbin | mick_lubbin | NULL (player) | 0.9375 | $400-$800 | Low |
| Jessi Waters | jessi_waters | Benji Coleman | 1.0 | $200-$1200 | VeryLow |
| Sam Thompson | sam_thompson | NULL (player) | 1.0 | $200-$500 | Low |
| Austin Steiner | austin_steiner | Benji Coleman | 1.0 | $400-$800 | Low |

**Dealer data confirmed at runtime (all 6):**

| Name | IsRecruited | AssignedCustomers | Cash | Cut | Type |
|---|---|---|---|---|---|
| Brad Crosby | True | 10 | $770 | 0.2 | PlayerDealer |
| Jane Lucero | False | 0 | $0 | 0.2 | PlayerDealer |
| Molly Presley | True | 10 | $0 | 0.2 | PlayerDealer |
| Benji Coleman | True | 10 | $0 | 0.2 | PlayerDealer |
| Wei Long | False | 0 | $0 | 0.2 | PlayerDealer |
| Leo Rivers | False | 0 | $0 | 0.2 | PlayerDealer |

**Key runtime access patterns confirmed:**
- Customer name: `customer._NPC_k__BackingField` → NPC → `.fullName` (e.g., "Kyle Cooley")
- Customer ID: NPC → `.ID` (e.g., "kyle_cooley")
- `fullName` is on NPC, NOT on Customer directly
- `CustomerData.name` returns prefab name (e.g., "KyleData") — not useful for display
- `AssignedDealer == null` → **confirmed: means player-assigned**
- Both backing field and property getter return identical values
- 10s delay after Main scene load is sufficient for data population
- **Evidence:** RuntimeVerificationService output, Session 3 log (26-4-22_18-15-48.log)

---

### Phone UI System (Session 7) — APP FRAMEWORK FOR OPTIMIZER TAB

**Architecture:** Each phone app extends `App<T>` → `PlayerSingleton<T>` → `MonoBehaviour`. Apps are singletons attached to the phone's scene hierarchy. The phone has a home screen with auto-generated icons for each app.

**Phone class** (`Il2CppScheduleOne.UI.Phone.Phone`, `PlayerSingleton`):
- `ActiveApp` (static, `GameObject`) — currently active app
- `IsOpen` (bool) — whether phone is visible
- `isHorizontal` (bool) — current orientation state
- `isOpenable` (bool) — whether phone can be opened
- `orientation_Vertical` / `orientation_Horizontal` (Transform) — target transforms for each orientation
- `rotationTime` (float) — animation duration for orientation switch
- `SetIsOpen(bool)` — open/close the phone
- `SetIsHorizontal(bool)` — **triggers orientation animation** with coroutine `SetIsHorizontal_Process`
- `RequestCloseApp()` — close the active app
- `onPhoneOpened` / `onPhoneClosed` (Action) — lifecycle events
- `closeApps` (Action) — event to close all apps
- **Evidence:** PhoneUIDiscoveryService dump, Session 7 log

**EOrientation enum:** `Horizontal = 0`, `Vertical = 1`

**App<T> base class** (`Il2CppScheduleOne.UI.App`1`, extends `PlayerSingleton<T>`):
- `AppName` (string) — display name on home screen
- `IconLabel` (string) — icon label text
- `AppIcon` (Sprite) — icon image
- `Orientation` (EOrientation) — app's preferred orientation
- `AvailableInTutorial` (bool)
- `appContainer` (RectTransform) — the app's UI root container
- `isOpen` (bool)
- `Apps` (static List) — all registered app instances
- `appIconButton` (Button) — home screen icon button
- `GenerateHomeScreenIcon()` — creates icon on home screen
- `SetOpen(bool)` — show/hide the app (virtual)
- `Exit(ExitAction)` — close the app (virtual)
- `OnPhoneOpened()` — lifecycle hook (virtual)
- `SetNotificationCount(int)` — badge notifications
- `Close()` — close the app
- **Evidence:** PhoneUIDiscoveryService dump, Session 7 log

**Existing phone apps** (all extend `App<T>`):
- `DealerManagementApp` — dealer/customer management (our target)
- `ContactsApp` — NPC contacts
- `DeliveryApp` — delivery management
- `JournalApp` — player journal
- `MapApp` — in-game map
- `MessagesApp` — messaging
- `ProductManagerApp` — product management

**HomeScreen** (`PlayerSingleton`):
- `appIconContainer` (RectTransform) — where app icons go
- `appIconPrefab` (GameObject) — template for app icons
- `appIcons` (List) — list of icon buttons
- `GenerateAppIcon(App<T>)` → Button — creates an icon for an app

**AppsCanvas** (`PlayerSingleton`):
- `canvas` (Canvas) — the Unity canvas for app rendering
- `isOpen` (bool)
- `SetIsOpen(bool)` — show/hide the app canvas
- `PhoneOpened()` / `PhoneClosed()` — lifecycle hooks

**DealerManagementApp specifics:**
- `SelectedDealer` (Dealer) — currently selected dealer
- `Content` (RectTransform) — main content area
- `CustomerSelector` (CustomerSelector) — customer picker component
- `_dropdown` (DropdownUI) — dealer dropdown
- `AssignCustomerButton` (Button) — assign button
- `BackButton` / `NextButton` (Button) — pagination
- `CashLabel` / `CutLabel` / `HomeLabel` (Text) — dealer info labels
- `CustomerEntries` (array) — customer display entries
- `InventoryEntries` (array) — inventory display entries
- `dealers` (List) — tracked dealers
- `_isOpen` (bool)
- `SetOpen(bool)` — virtual, opens/closes app
- `Refresh()` — updates displayed data
- `SetDisplayedDealer(Dealer)` — switch displayed dealer
- `AddDealer(Dealer)` / `AddCustomer(Customer)` / `RemoveCustomer(Customer)` — modify display
- `AssignCustomer()` — perform assignment
- `RefreshDropdown()` — update dealer dropdown

**CustomerSelector** (`MonoBehaviour`):
- `ButtonPrefab` (GameObject) — template for customer buttons
- `EntriesContainer` (RectTransform) — button container
- `onCustomerSelected` (UnityEvent<Customer>) — selection event
- `customerEntries` (List) — button entries
- `entryToCustomer` (Dictionary) — maps entries to customers
- `Open()` / `Close()` — show/hide selector
- `CreateEntry(Customer)` — add a customer button
- `CustomerSelected(Customer)` — handle selection

**DropdownUI** (`extends Dropdown`):
- `OnOpen` (Action) — event when dropdown opens
- Wraps Unity's standard Dropdown with an open callback

**Orientation switching mechanism:**
- Each app declares preferred `Orientation` (Horizontal/Vertical)
- `Phone.SetIsHorizontal(bool)` triggers animated rotation via coroutine
- Phone has two Transform targets: `orientation_Vertical` and `orientation_Horizontal`
- `rotationTime` controls animation speed

**Implementation plan for optimizer tab (Option C):**
1. Harmony-patch `DealerManagementApp.SetOpen(bool)` — inject "Optimize" toggle button
2. When optimizer mode activated: call `Phone.Instance.SetIsHorizontal(true)` to flip to landscape
3. Hide vanilla content, show our table view in the `Content` RectTransform
4. When toggling back: `SetIsHorizontal(false)`, restore vanilla content
5. Reassignment via dropdown per customer row, using existing `ReassignmentService`

### Harmony Patching Verified (Session 7)
- **Harmony patches work on IL2CPP game methods** via MelonLoader's built-in `HarmonyInstance`
- `DealerManagementApp.SetOpen(bool)` successfully patched with a postfix
- Method found via reflection: `_dealerAppType.GetMethod("SetOpen", new[] { typeof(bool) })`
- Postfix receives `object __instance` and `bool open` — both accessible
- `SetOpen(false)` fires once on init (app starts closed), then `SetOpen(true)` on open, `SetOpen(false)` on close
- **Evidence:** Session 7 log — patch applied at init, OPENED/CLOSED logged on user interaction

### uGUI GameObject Construction in IL2CPP (Session 8)
- **`new GameObject(string, params Type[])` is NOT available** against Il2Cpp-stripped `UnityEngine.CoreModule.dll`. The exposed constructor is `GameObject(string, Il2CppReferenceArray<Il2CppSystem.Type>)` — a managed `Type[]` won't implicitly convert.
- **Workaround:** build a 1-element `Il2CppReferenceArray<Il2CppSystem.Type>` and fill via `Il2CppType.Of<RectTransform>()`:
  ```csharp
  var types = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
  types[0] = Il2CppType.Of<RectTransform>();
  var go = new GameObject(name, types);
  ```
- Why not `new GameObject(name)` + `AddComponent<RectTransform>()`? Unity refuses to add a RectTransform to a GameObject that already has a Transform. The params-constructor path is the only way to get RectTransform on a freshly-created GameObject.
- **`UnityEngine.TextRenderingModule`** is a separate assembly from `UIModule`/`CoreModule` and must be referenced to use `Font` / `TextAnchor` / `FontStyle` / `HorizontalWrapMode` / `VerticalWrapMode` with `UnityEngine.UI.Text`. Pulled from `MelonLoader/Il2CppAssemblies/`.
- **Evidence:** Session 8 build failures before the workaround, compile-clean after.

### Narrowing Transform → RectTransform Casts Throw in IL2CPP (Session 8)
- **`(RectTransform)foo.transform` and `someRectProp.GetValue(obj) as RectTransform`** both throw `InvalidCastException: Unable to cast object of type 'UnityEngine.Transform' to type 'UnityEngine.RectTransform'` at runtime in IL2CPP, even when the underlying Il2Cpp object is a RectTransform. Il2CppInterop wraps the return value as the declared static type (`Transform`) and the narrowing cast fails before any C# `as`/try-catch can intercept.
- **Fix:** always resolve a `RectTransform` via the Unity component system, never via a cast:
  ```csharp
  var rt = go.GetComponent<RectTransform>();
  // not: var rt = (RectTransform)go.transform;
  ```
- Same applies to reflection reads. Read the property as its base Unity type (`Transform`) and then GetComponent:
  ```csharp
  var t = ReflectGet<Transform>(obj, "appContainer");
  var rt = t?.GetComponent<RectTransform>();
  ```
- Related: `Il2CppObjectBase.TryCast<T>()` CAN do this safely when `T` is a derived Il2Cpp type — but only if you have the Il2CppObjectBase in hand before the narrowing cast. Reflection's `PropertyInfo.GetValue` already performs the failing cast internally, so you can't TryCast your way out of a `ReflectGet<RectTransform>(...)`.
- **Evidence:** Session 8 stack traces — throws at `CreateButton` / `InjectToggleButton` on explicit `(RectTransform)...transform` lines.

### PlayerSingleton<T>.Instance via Reflection (Session 8)
- **`Phone.Instance` lives on the generic base `PlayerSingleton<T>`**, not on `Phone` itself. `GetProperty("Instance", Public | Static | FlattenHierarchy)` on the `Phone` Il2Cpp type does NOT find it — FlattenHierarchy doesn't consistently cross generic-base statics in Il2CppInterop.
- **Fix:** if the direct lookup fails, walk `type.BaseType` up the chain and search each for `Instance` (no FlattenHierarchy needed at that level). Same pattern likely needed for other PlayerSingleton/NetworkSingleton statics.
- **Evidence:** Session 8 "Phone not resolved" warning, fixed in `ResolvePhone()` after switching to the base-walk fallback.

### Phase 3 Dealer-to-Dealer Transfer Confirmed (Session 8)
- **Eugene Buckley (Brad → Molly, 23:51:01, Session 8):** first in-game dealer-to-dealer direct transfer test. Call sequence `Brad.RemoveCustomer(eugene) + Molly.AddCustomer(eugene) + eugene.AssignDealer(Molly)` succeeded; both dealers' counts adjusted (Brad 9→8, Molly 8→9) and customer's `AssignedDealer` = Molly. This closes the last deferred item from Phase 3.

### ScreenSpaceOverlay Canvas Needs RectTransform-from-Construction (Session 9)
- **`new GameObject("OptimizerCanvas")` + `AddComponent<Canvas>()` does NOT auto-promote the GO's `Transform` to a `RectTransform` in IL2CPP-stripped Unity.** Canvas's `[RequireComponent(typeof(RectTransform))]` semantics that work in stock Unity Editor do not reliably trigger here — the Canvas ends up backed by a plain `Transform`, ScreenSpaceOverlay never sizes itself to screen, and nothing renders even with `sortingOrder=32000` and `overrideSorting=true`.
- **Fix:** construct the Canvas GameObject the same way as any other uGUI element — via the `NewUIGameObject` helper that uses the `(string, Il2CppReferenceArray<Il2CppSystem.Type>)` constructor with `Il2CppType.Of<RectTransform>()`. With a RectTransform present at Canvas init, ScreenSpaceOverlay correctly auto-sizes via `RectTransform.SetSizeWithCurrentAnchors`-equivalent internal logic.
- **Evidence:** Session 9 diagnostic log line `Canvas built: enabled=True, sortingOrder=32000, rectSize=3440x1440, screen=3440x1440, canvasActive=True` after the fix; pre-fix runs (Session 8) showed an invisible canvas with no diagnostic line yet captured.
- **Generalization of the Session 8 finding:** the "Unity refuses to add RectTransform to a GO that already has Transform" rule applies to ALL uGUI roots, not just visual elements. Use `NewUIGameObject` for top-level Canvases too, not just children.

### Inner-Text raycastTarget Blocks Parent Button Clicks (Session 9)
- **A uGUI Button built as `Image + Button` with a Text child silently swallows clicks if the Text's `raycastTarget` is left at the default (`true`).** The inner Text becomes the topmost graphic over the Image, so the GraphicRaycaster hits the Text first; the Text has no Button component, the click is consumed there, and the parent Button's `onClick` never fires.
- **Fix:** in any helper that creates a Button-with-label, set `txt.raycastTarget = false` on the inner Text. Same rule for any decorative Title/header/instructional Text that overlaps a clickable Button — set its `raycastTarget` to false too, otherwise it'll mask the button beneath it.
- **Evidence:** Session 9 — Close button at panel top-right was non-functional because the wide Title text spanned the same vertical band. Reassign-row buttons (lower in the scroll) worked because no Text overlapped them. Once `raycastTarget=false` was applied to inner button Texts and the Title Text, every button across the panel became clickable, including Close.

### Panels Under Dealer-App `appContainer` Need `LayoutElement.ignoreLayout=true` (Session 9)
- **Adding a full-stretch (`anchorMin=0, anchorMax=1, offset=0`) child to `Il2CppScheduleOne.UI.Phone.Messages.DealerManagementApp.appContainer` produces an empty/invisible panel** unless the child opts out of the parent's layout. The dealer-app's appContainer has some LayoutGroup-type behavior that overwrites the child's RectTransform values during the layout pass, even when the child is at the correct sibling slot for sort order.
- **Fix:** add a `LayoutElement` to the panel and set `ignoreLayout = true`. Combined with `SetAsLastSibling` on the panel (run from `EnterOptimizerMode`, after the panel is shown), the panel renders correctly at full appContainer rect.
- **Evidence:** Session 9 diagnostic showed the panel sized correctly at 655×1201 (matching appContainer) and active in hierarchy, but visually empty — until both `ignoreLayout=true` and `SetAsLastSibling` were applied. Sibling list at the time: `[0]Background(A) [1]CustomerSelector(I, vanilla) [2]OptimizerPanel(A) [3]OptimizerToggle(A)`. After the fix, the user-confirmed panel renders the full table inside the phone like a native app.

---

## Suspected

- Reassignment likely requires calling both `Dealer.RemoveCustomer()` / `Dealer.AddCustomer()` AND `Customer.AssignDealer()` to keep both sides in sync. The network RPC variants suggest this is a multiplayer-aware operation.
- The `AddCustomer_Server` / `AddCustomer_Client` RPC pattern suggests the game uses FishNet server authority — mods may need to call the server variant for proper sync.
- `Customer._WeeklyPurchaseRecord_k__BackingField` contains actual transaction records, not just a computed total. The `CustomerData.GetAdjustedWeeklySpend()` method likely computes the expected/target spend.

---

## Unknown

See [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) for remaining unknowns.

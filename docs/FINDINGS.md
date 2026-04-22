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

## Suspected

- Reassignment likely requires calling both `Dealer.RemoveCustomer()` / `Dealer.AddCustomer()` AND `Customer.AssignDealer()` to keep both sides in sync. The network RPC variants suggest this is a multiplayer-aware operation.
- The `AddCustomer_Server` / `AddCustomer_Client` RPC pattern suggests the game uses FishNet server authority — mods may need to call the server variant for proper sync.
- `Customer._WeeklyPurchaseRecord_k__BackingField` contains actual transaction records, not just a computed total. The `CustomerData.GetAdjustedWeeklySpend()` method likely computes the expected/target spend.

---

## Unknown

See [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) for remaining unknowns.

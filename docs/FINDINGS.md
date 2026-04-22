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

---

## Suspected

- `Customer._AssignedDealer_k__BackingField == null` means the customer is player-assigned (no dealer). Needs runtime verification.
- `Customer._WeeklyPurchaseRecord_k__BackingField` contains actual transaction records, not just a computed total. The `CustomerData.GetAdjustedWeeklySpend()` method likely computes the expected/target spend.
- Reassignment may be possible by calling a method on Customer or Dealer, or by directly setting `_AssignedDealer_k__BackingField`. The existence of `Dealer.AssignCustomersDialogue` suggests the game has built-in assignment UI/flow.
- `Dealer.MAX_CUSTOMERS` limits how many customers a dealer can serve — reassignment must respect this.

---

## Unknown

See [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) for remaining unknowns.

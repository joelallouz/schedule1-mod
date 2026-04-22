# Open Questions

Track unknowns here. When a question is answered, move the answer to [FINDINGS.md](FINDINGS.md) and mark the question as resolved.

---

## Resolved

- [x] What class represents a client/customer? → `Il2CppScheduleOne.Economy.Customer` (Session 2)
- [x] What assembly is it in? → `Assembly-CSharp` (Session 2)
- [x] Is there a list/collection of all clients? → `Customer.UnlockedCustomers` and `Customer.LockedCustomers` (static lists) (Session 2)
- [x] How is assignment stored? → `Customer._AssignedDealer_k__BackingField` — direct `Dealer` reference (Session 2)
- [x] How is addiction level stored? → `Customer._CurrentAddiction_k__BackingField` — `Single` (float) (Session 2)
- [x] Is "weekly spend" a stored field or computed? → Both. `CustomerData.MinWeeklySpend`/`MaxWeeklySpend` are config ranges; `GetAdjustedWeeklySpend()` computes the actual value; `Customer._WeeklyPurchaseRecord_k__BackingField` tracks real purchases (Session 2)
- [x] How are preferences stored? → `CustomerData.PreferredProperties` (List) + `CustomerAffinityData.ProductAffinities` (List) with `GetAffinity(EDrugType)` (Session 2)
- [x] Is there a singleton manager? → `NPCManager` (NetworkSingleton) exists, but customers use static lists on `Customer` itself (Session 2)
- [x] What is the correct MelonGame attribute? → `("TVGS", "Schedule I")` (Session 1)
- [x] Which assemblies contain game logic? → Assembly-CSharp (3705 types), Il2CppScheduleOne.Core (46 types) (Session 1)
- [x] Are there other Il2CppScheduleOne.* sub-assemblies? → Only `Il2CppScheduleOne.Core` (Session 2)
- [x] Is there an `AssignCustomer()` or `RemoveCustomer()` method on `Dealer`? → Yes: `Dealer.AddCustomer(Customer)`, `Dealer.RemoveCustomer(Customer)`, `Dealer.RemoveCustomer(String npcID)`. Customer side: `Customer.AssignDealer(Dealer)`. (Session 3)
- [x] Does `Dealer.MAX_CUSTOMERS` vary per dealer or is it constant? → Static property, value = 10. Has a setter so could be changed globally, but appears to be a constant. (Session 3)
- [x] Can we safely read `Customer.UnlockedCustomers` at runtime? → Yes. List is `Il2CppSystem.Collections.Generic.List<Customer>`, accessible via reflection. Empty on Menu scene, populated after save loads on Main scene. (Session 3)
- [x] Can we read `Dealer.AllPlayerDealers` at runtime? → Yes. Same pattern as UnlockedCustomers. (Session 3)
- [x] Do the backing field properties work with Il2Cpp reflection, or do we need to use the getter methods? → Both work. Proper getters also exist (e.g., `get_AssignedDealer()`, `get_CurrentAddiction()`). (Session 3)
- [x] Are the `List` types `Il2CppSystem.Collections.Generic.List` or .NET `System.Collections.Generic.List`? → `Il2CppSystem.Collections.Generic.List`. (Session 3)
- [x] How do we get a customer's display name? → NPC base class has: `String fullName`, `String FirstName`, `String LastName`, `Boolean hasLastName`, `String ID`. Customer has-a NPC ref. Dealer IS-A NPC. (Session 3)
- [x] Does the game use Il2Cpp generics that require special handling for our List access? → No special handling needed. `.Count` and `Item` indexer work via standard reflection. (Session 3)

---

## Open — Assignment Mechanics

- [ ] Does `AssignedDealer == null` mean "player-assigned"? (Need runtime check on actual customer with loaded save)
- [ ] What happens when you set `_AssignedDealer_k__BackingField` directly? Does it update the dealer's `_AssignedCustomers_k__BackingField` too, or do both need updating?
- [ ] What is the correct call sequence for reassignment? `RemoveCustomer` + `AddCustomer` + `AssignDealer`? Or does one call handle everything?
- [ ] Are there cooldowns or restrictions on reassignment?
- [ ] Does the FishNet RPC pattern (`AddCustomer_Server`/`AddCustomer_Client`) mean we need server authority to reassign? Or can we call the local methods directly in single-player?

## Open — Spend Data

- [ ] What does `_WeeklyPurchaseRecord_k__BackingField` contain? (What type is the list element?)
- [ ] Is `GetAdjustedWeeklySpend()` the best single number for "how much does this customer spend"?
- [ ] Does the relationship value (normalizedRelationship parameter) affect spend significantly?

## Open — Runtime Data (needs loaded save with customers)

- [ ] Can we read `fullName`, `FirstName`, `LastName` on live NPC/Customer/Dealer instances?
- [ ] What does `CustomerData.name` return? (Inherited from ScriptableObject — might be a prefab name)
- [ ] What values do CurrentAddiction, MinWeeklySpend, MaxWeeklySpend show for real customers?
- [ ] What does a dealer's AssignedCustomers list look like at runtime?

## Open — Technical

- [ ] Are there existing open-source mods that read customer data we can reference?

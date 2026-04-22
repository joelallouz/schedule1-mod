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

---

## Open — Assignment Mechanics

- [ ] Does `AssignedDealer == null` mean "player-assigned"? (Needs runtime check on an actual customer)
- [ ] Is there an `AssignCustomer()` or `RemoveCustomer()` method on `Dealer`? (Truncated at 50 methods — need targeted dump)
- [ ] What happens when you set `_AssignedDealer_k__BackingField` directly? Does it update the dealer's `_AssignedCustomers_k__BackingField` too, or do both need updating?
- [ ] Does `Dealer.MAX_CUSTOMERS` vary per dealer or is it constant?
- [ ] Are there cooldowns or restrictions on reassignment?
- [ ] Does the game's `AssignCustomersDialogue` flow provide clues about the proper reassignment API?

## Open — Spend Data

- [ ] What does `_WeeklyPurchaseRecord_k__BackingField` contain? (What type is the list element?)
- [ ] Is `GetAdjustedWeeklySpend()` the best single number for "how much does this customer spend"?
- [ ] Does the relationship value (normalizedRelationship parameter) affect spend significantly?

## Open — Runtime Access

- [ ] Can we safely read `Customer.UnlockedCustomers` at runtime? (Is the list populated by the time our mod runs?)
- [ ] Can we read `Dealer.AllPlayerDealers` at runtime?
- [ ] Do the backing field properties work with Il2Cpp reflection, or do we need to use the getter methods?
- [ ] Are the `List` types `Il2CppSystem.Collections.Generic.List` or .NET `System.Collections.Generic.List`?

## Open — UI/Display

- [ ] How do we get a customer's display name? (Likely via the `NPC` base class — need to dump NPC type shape)
- [ ] What does `CustomerData.name` return? (Inherited from ScriptableObject)

## Open — Technical

- [ ] Does the game use Il2Cpp generics that require special handling for our List access?
- [ ] Are there existing open-source mods that read customer data we can reference?

# Open Questions

Track unknowns here. When a question is answered, move the answer to [FINDINGS.md](FINDINGS.md) and mark the question as resolved.

---

## Client Data Model

- [ ] What class represents a client/customer?
- [ ] What assembly is it in?
- [ ] What fields does it have? (name, spend, addiction, preferences, etc.)
- [ ] Is there a list/collection of all clients accessible at runtime?
- [ ] Is there a singleton manager for clients?

## Assignment

- [ ] How is a client's assignment stored? (reference to dealer? enum? string?)
- [ ] Can a client be assigned to the player directly, or is the player treated as a special dealer?
- [ ] Is assignment a field on the client object or managed elsewhere?
- [ ] Are there constraints on reassignment? (cooldown, max clients per dealer, etc.)

## Spending

- [ ] Is "weekly spend" a stored field or computed from transaction history?
- [ ] What is the spending unit? (per transaction? per day? per week?)
- [ ] Is spend tracked per-client or only in aggregate?

## Addiction

- [ ] How is addiction level stored? (float 0-1? int level? enum?)
- [ ] Does addiction affect spending behavior?
- [ ] Does addiction change over time or only through player actions?

## Product Preferences

- [ ] How are preferences stored? (list of product IDs? single preferred product? weighted?)
- [ ] Can preferences change?
- [ ] Do preferences affect which dealer a client visits?

## Technical

- [ ] What is the correct value for `MelonGame` attribute? (company name, game name)
- [ ] Which assemblies contain the game's client/dealer logic?
- [ ] Does the game use Il2Cpp generics that require special handling?
- [ ] Are there existing mods that touch client/assignment code we can reference?

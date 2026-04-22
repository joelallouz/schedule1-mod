# Findings

This document tracks what we've confirmed, what we suspect, and what remains unknown about Schedule I's internals — specifically around client data, assignments, and spending.

**Update this file every session with new discoveries. Cite evidence (log output, class names, field names) for everything in the Confirmed section.**

---

## Confirmed

_Nothing confirmed yet. First discovery scan has not been run._

<!-- Template for confirmed findings:
### [Topic]
- **Class:** `Namespace.ClassName`
- **Assembly:** `AssemblyName`
- **Evidence:** [paste relevant log output or describe how confirmed]
- **Session:** [which session discovered this]
-->

---

## Suspected

- The game likely has a class representing a client/customer with fields for name, assignment, and spending.
- Assignment is probably a reference to a dealer object or the player, or an enum.
- "Weekly spend" may be computed from transaction history rather than stored as a single field.
- Client data is probably accessible through a singleton manager or a static collection.

_These are educated guesses. None have been verified._

---

## Unknown

See [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) for the full list of unknowns.

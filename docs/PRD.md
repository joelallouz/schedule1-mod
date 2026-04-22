# Product Requirements: Client Assignment Optimizer

## Overview

A MelonLoader mod for **Schedule I** (Unity IL2CPP, v0.4.5f2) that gives players visibility and control over client assignments.

In the base game, clients are assigned to either the player or a hired dealer. The game provides limited visibility into which clients are where, how much they spend, and whether a high-value client is "wasted" on a dealer instead of being served directly by the player.

## Problem

Players have no consolidated view of:
- All clients and their current assignment (player vs. dealer)
- Weekly spend per client
- Addiction level per client
- Product preferences per client
- Which dealer-assigned clients would be more profitable if reassigned to the player

This makes it difficult to optimize revenue, especially as the business scales.

## Solution

### Read-Only Client View (Phase 2)
- List all known clients
- Show current assignment (player or which dealer)
- Show weekly spend
- Show addiction level
- Show preferred product(s)

### Reassignment (Phase 3)
- Allow the player to reassign a client from one dealer to another, or to themselves
- Respect any game constraints on reassignment (to be discovered)

### Flagging / Filtering (Phase 4)
- Automatically flag dealer-assigned clients whose weekly spend exceeds a configurable threshold
- Label these as "Should Be Player"
- Sort/filter by spend, assignment, addiction, etc.
- Summary stats (total player revenue vs. dealer revenue, potential gains from reassignment)

**v1 simplification:** Flagging is based ONLY on weekly spend threshold. No composite scoring.

## Non-Goals (For Now)
- Auto-reassignment without player input
- Price optimization or product recommendation
- Modifying client stats (spend, addiction, preferences)
- Multiplayer-specific features
- Composite scoring (multi-factor flagging beyond spend)

## Technical Constraints
- Must not mutate game state during discovery phase
- Must degrade gracefully if game internals change between updates
- Must be safe to run alongside other MelonLoader mods
- All findings are version-specific (currently v0.4.5f2)

## Open Questions
See [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md) for the current list of unknowns.

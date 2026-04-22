# Client Assignment Optimizer — Schedule I Mod

## What This Is

A MelonLoader mod for Schedule I (Unity IL2CPP game). The mod will let players see and manage client assignments, spending, and dealer allocation.

## Current Phase

**Phase 0: Bootstrap** — Scaffold complete. Discovery infrastructure exists but has not been run against the live game yet.

## Key Conventions

- **Namespace:** `ClientAssignmentOptimizer` (sub-namespaces: `.Core`, `.Discovery`, `.Domain`, `.Services`, `.Patches`, `.UI`)
- **Logging:** Always use `ModLogger` (in `src/Core/ModLogger.cs`), never `MelonLogger` directly. Prefix is `[ClientOptimizer]`.
- **Discovery code** lives in `src/Discovery/` and is gated behind `ModConfig.DiscoveryEnabled`. It must NEVER mutate game state.
- **Feature code** will go in `Domain/`, `Services/`, `Patches/`, `UI/` — these are empty until Phase 2+.

## Documentation

All persistent project knowledge lives in `docs/`:
- `PRD.md` — what the mod does
- `ROADMAP.md` — phased plan with checklists
- `SESSION_LOG.md` — append an entry every session
- `FINDINGS.md` — confirmed/suspected game internals (cite evidence)
- `OPEN_QUESTIONS.md` — tracked unknowns
- `ARCHITECTURE.md` — structure and design rationale
- `TESTING.md` — how to build, deploy, and verify

## Session Continuity Rules

1. Read `docs/SESSION_LOG.md` and `docs/FINDINGS.md` at the start of every session.
2. Append to `SESSION_LOG.md` at the end of every session.
3. Never fabricate findings — only add to `FINDINGS.md` with evidence.
4. Update `OPEN_QUESTIONS.md` as questions are answered.
5. Update `ROADMAP.md` checkboxes as work completes.

## Build

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="<path to Schedule I>"
```

Output: `ClientAssignmentOptimizer.dll` → copy to `<GameDir>/Mods/`.

# Client Assignment Optimizer — Schedule I Mod

## Project Summary

A MelonLoader mod for **Schedule I** (Unity IL2CPP, v0.4.5f2, Unity 2022.3.62f2). Gives players visibility into customer assignments and lets them optimize which customers are served by the player vs. dealers.

**Repo:** `joelallouz/schedule1-mod`

## Current Phase

**Phone UI Integration (in progress, Session 7–8)** — Pivoted from standalone IMGUI panel to integrating into the game's phone dealer management app. Harmony patch on `DealerManagementApp.SetOpen(bool)` is verified firing. Next: build the optimizer tab UI (landscape table view with reassignment dropdowns).

**Prior phases:** Phase 2 (read-only panel) ✓, Phase 3 (reassignment) ✓, both verified in-game.

**⚠️ IL2CPP gotcha:** `UnityEngine.GUILayoutUtility.GetLastRect()` is stripped — throws `NotSupportedException: Method unstripping failed`. Use `GUILayout.Button` for clickable UI elements instead.

## Key Game Classes (Confirmed)

| Class | Namespace | Purpose |
|---|---|---|
| `Customer` | `Il2CppScheduleOne.Economy` | The customer entity. Has `_AssignedDealer_k__BackingField`, `_CurrentAddiction_k__BackingField`, `_WeeklyPurchaseRecord_k__BackingField`. Static lists: `UnlockedCustomers`, `LockedCustomers`. |
| `CustomerData` | `Il2CppScheduleOne.Economy` | ScriptableObject config. Has `MinWeeklySpend`, `MaxWeeklySpend`, `PreferredProperties`, `Standards`, `BaseAddiction`. Method: `GetAdjustedWeeklySpend()`. |
| `Dealer` | `Il2CppScheduleOne.Economy` | The dealer entity (extends NPC). Has `_AssignedCustomers_k__BackingField`, `Cash`, `Cut`, `DealerType`. Static: `AllPlayerDealers`, `MAX_CUSTOMERS`. |
| `CustomerAffinityData` | `Il2CppScheduleOne.Economy` | Product affinities. `GetAffinity(EDrugType)`. |
| `EDealerType` | `Il2CppScheduleOne.Economy` | Enum: `PlayerDealer`, `CartelDealer`. |
| `ECustomerStandard` | `Il2CppScheduleOne.Economy` | Enum: `VeryLow`, `Low`, `Moderate`, `High`, `VeryHigh`. |
| `NPCManager` | `Il2CppScheduleOne.NPCs` | Singleton NPC manager (`NetworkSingleton`). |

**Important:** The game uses "Customer" internally, not "Client." Our mod name is user-facing; code references should use "Customer."

## Architecture

```
src/
  Core/           ModEntry, ModLogger, ModConfig
  Discovery/      Read-only reverse engineering tools (temporary)
  Domain/         Data models (future — Phase 2+)
  Services/       Business logic (future — Phase 2+)
  Patches/        Harmony patches (future — Phase 2+)
  UI/             In-game interface (future — Phase 2+)
docs/             Persistent cross-session documentation
libs/             Reference DLLs for macOS builds (gitignored)
```

## Development Workflow

**Dual-machine setup:**
- **macOS** — code editing, builds, automated deploy via SSH
- **Windows PC** (`jlall@192.168.1.141`) — runs Schedule I with the mod, produces logs

**Deploy script (`deploy.sh`):**
```bash
./deploy.sh          # Build + push DLL to PC
./deploy.sh --tail   # Build + push + live-tail the log file
./deploy.sh --logs   # Just pull Latest.log locally (no build)
```

**Loop:** Claude writes code → `./deploy.sh` builds and pushes DLL → human launches game on PC → `./deploy.sh --logs` pulls log back → Claude interprets and iterates.

## Key Conventions

- **Namespace:** `ClientAssignmentOptimizer` (`.Core`, `.Discovery`, `.Domain`, `.Services`, `.Patches`, `.UI`)
- **Logging:** Always use `ModLogger`, never `MelonLogger` directly. Prefix: `[ClientOptimizer]`.
- **Discovery code** is in `src/Discovery/`, gated behind `ModConfig.DiscoveryEnabled`. Must NEVER mutate game state.
- **All output must be bounded** — DumpUtils caps at 50 members, TypeSearchService caps at 15 full dumps.

## Documentation Rules

All persistent knowledge lives in `docs/`. Every session must:

1. **Read** `SESSION_LOG.md` and `FINDINGS.md` at start
2. **Append** to `SESSION_LOG.md` at end (never overwrite previous entries)
3. **Promote** confirmed discoveries to `FINDINGS.md` with evidence
4. **Update** `OPEN_QUESTIONS.md` as questions are answered
5. **Update** `ROADMAP.md` checkboxes as work completes
6. Never fabricate findings — evidence required

## Build & Deploy

```bash
./deploy.sh       # build + deploy in one step
```

Manual build only (no deploy):
```bash
dotnet build ClientAssignmentOptimizer.csproj -p:CopyToMods=false
```

Output: `bin/Debug/net6.0/ClientAssignmentOptimizer.dll`

**Windows paths:**
- Mods: `C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods`
- Logs: `C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Latest.log`

# Client Assignment Optimizer

A [MelonLoader](https://melonwiki.xyz/) mod for [Schedule I](https://store.steampowered.com/app/3164500/Schedule_I/) that gives players visibility and control over client assignments.

> **Status:** Phase 0 (Bootstrap) — project scaffold and discovery infrastructure in place. No gameplay features yet.

## What It Will Do

- List all clients with their assignment (player vs. dealer), weekly spend, addiction, and product preferences
- Allow reassigning clients between dealers and the player
- Flag high-spend dealer clients as "Should Be Player"

See [docs/PRD.md](docs/PRD.md) for full product requirements and [docs/ROADMAP.md](docs/ROADMAP.md) for the phased plan.

## Project Structure

```
src/
  Core/          Mod entry point, logging, config
  Discovery/     Reverse engineering tools (temporary)
  Domain/        Data models (future)
  Services/      Business logic (future)
  Patches/       Harmony patches (future)
  UI/            In-game interface (future)
docs/            Project documentation (PRD, roadmap, findings, architecture)
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for details.

## Tech Stack

- **Game:** Schedule I (Unity, IL2CPP)
- **Mod Framework:** MelonLoader 0.6.x
- **Language:** C# / .NET 6.0

## Building

```bash
dotnet build ClientAssignmentOptimizer.csproj -p:GameDir="<path to Schedule I>"
```

See [docs/TESTING.md](docs/TESTING.md) for full build, deploy, and verification instructions.

## License

TBD

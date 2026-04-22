# Project Plan

## Context

This is a mod for **Schedule 1**, a Unity (IL2CPP) drug dealer simulation game on Steam. Mods are written in C# and loaded via MelonLoader.

This is a first-time game modding project. The developer has extensive programming experience but no prior PC game mod development.

## Open Questions

These need to be answered before writing any code:

### Mod Concept
- [ ] What does this mod do? (gameplay feature, QoL improvement, visual change, etc.)
- [ ] Who is the target audience? (casual players, power users, multiplayer groups, etc.)
- [ ] Does anything similar already exist on Nexus Mods or Thunderstore?

### Technical Discovery
- [ ] Install and play Schedule 1 to understand the base game
- [ ] Set up MelonLoader and verify it works with the current game version
- [ ] Explore existing open-source mods to understand patterns and conventions
- [ ] Identify relevant game assemblies and APIs to hook into
- [ ] Understand the IL2CPP decompilation workflow (e.g., Il2CppDumper, cpp2il)

### Development Environment
- [ ] .NET SDK version and target framework
- [ ] MelonLoader mod template / boilerplate
- [ ] Local build and test workflow (build DLL -> copy to Mods folder -> launch game)
- [ ] Decide on IDE (Visual Studio, Rider, VS Code)

### Distribution
- [ ] Nexus Mods vs. Thunderstore vs. both
- [ ] Versioning scheme
- [ ] License choice

## Next Steps

1. Decide on a mod concept
2. Install the game and MelonLoader
3. Study 2-3 existing open-source mods
4. Set up the dev environment and build a "hello world" mod
5. Come back here and flesh out the plan with specific design and implementation details

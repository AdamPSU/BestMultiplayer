# BestMultiplayer mod scaffold

This is a feature-neutral tModLoader 1.4.4.9 source scaffold with VS Code build support.

## First use

1. Launch tModLoader.
2. Open `Workshop -> Develop Mods -> Create Mod` and create a temporary mod once. This initializes the correct `ModSources` folder and version-specific project files.
3. Use `Workshop -> Develop Mods -> Open Sources` to reveal that folder.
4. Copy this scaffold into `ModSources`, or use the generated project file as the authoritative `.csproj` and copy the source files into it.
5. Keep the mod folder, `build.txt`, `.cs`, `.csproj`, namespace, and class name aligned if you rename the mod later.
6. Open `BestMultiplayer.csproj` in VS Code. Do not open only an individual `.cs` file.

## Build and test

- VS Code: run the `tModLoader: build mod` task.
- In tModLoader: use `Workshop -> Develop Mods -> Build + Reload`.
- Test changes in a throwaway world first.

The local `.dotnet` SDK is used by the VS Code task. If the project is moved to another machine, install the .NET SDK required by that tModLoader version and regenerate the project through tModLoader.

## Add features

Suggested folders:

```text
Common/Configs/                 ModConfig classes
Common/Systems/                 world or lifecycle systems
Common/Players/                 ModPlayer state and sync
Common/GlobalItems/             cross-cutting item behavior
Common/GlobalNPCs/              cross-cutting NPC behavior
Common/GlobalProjectiles/       cross-cutting projectile behavior
Content/                        new items, projectiles, NPCs, tiles, and buffs
Localization/                   .hjson labels, tooltips, and text
```

Keep client presentation settings in `ConfigScope.ClientSide` configs and shared gameplay/server policy in `ConfigScope.ServerSide` configs. Add network packets only for runtime state that is not already covered by tModLoader synchronization.

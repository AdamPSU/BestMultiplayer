# BestMultiplayer

tModLoader 1.4.4.9 multiplayer QoL mod (scaffold stage).

## Goal

Build a stronger multiplayer experience than [Better Multiplayer](https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993) (kittenchilly).

| | |
|---|---|
| Workshop | https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993 |
| Source | https://github.com/kittenchilly/BetterMultiplayer |

Baseline (source v1.6.3 — full inventory in `wiki/entities/better-multiplayer-baseline.md`):

1. **TeamToJoin** — force join team (`None`/`Red`/`Green`/`Blue`/`Yellow`/`Pink`, default Red)
2. **NoBossFightRespawn** — while dead + boss/EoW active, keep resetting respawn timer (default on)
3. **WitchDoctorWormhole** — Witch Doctor sells Wormhole Potions (default on)

## Architecture

One mod (`side = Both`). Split **authority** (server) from **presentation** (client). Pattern matches ExampleMod, Calamity, Magic Storage, Team Spectate.

```text
BestMultiplayer.cs                 thin Mod entry (packets/lifecycle only)
Common/Configs/ServerConfig.cs     ConfigScope.ServerSide — host policy
Common/Configs/ClientConfig.cs     ConfigScope.ClientSide — local UX
Common/Players/                    ModPlayer hooks (team, respawn, spectate)
Common/Systems/                    session helpers (boss-fight detection)
Common/GlobalNPCs/                 shop and cross-NPC hooks
Common/UI/                         client-only UI (add when needed)
Content/                           new items/NPCs/tiles (add when needed)
Localization/                      .hjson labels
wiki/                              design notes and baseline inventory
```

| Concern | Config | Runtime home |
|---|---|---|
| Auto team, boss respawn, shops | `ServerConfig` | Players / Systems / GlobalNPCs |
| Spectate, HUD prefs | `ClientConfig` | Players / UI (local only) |
| Custom packets | — | only if tML does not already sync the state |

Server config changes are host-only (`AcceptClientChanges`).

## Status

| Piece | State |
|---|---|
| Folder + config scaffold | Done |
| Config labels (en-US) | Done |
| Team / respawn / shop logic | Not implemented |
| Spectate logic | Not implemented |

## First use

1. Launch tModLoader.
2. Open `Workshop -> Develop Mods -> Create Mod` once if needed (initializes `ModSources`).
3. Place this repo under `ModSources` (or open it from there).
4. Keep mod folder, `build.txt`, main type/namespace, and `.csproj` names aligned on rename.
5. Open `BestMultiplayer.csproj` in VS Code (not a single `.cs` file).

## Build and test

- VS Code: `tModLoader: build mod` task.
- In-game: `Workshop -> Develop Mods -> Build + Reload`.
- Test in a throwaway multiplayer world.

Local `.dotnet` is used by the VS Code task. On another machine, install the .NET SDK required by that tModLoader version and regenerate the project through tModLoader if needed.

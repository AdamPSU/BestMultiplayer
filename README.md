# BestMultiplayer

tModLoader 1.4.4.9 multiplayer QoL mod.

## Goal

Stronger multiplayer experience than [Better Multiplayer](https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993) (kittenchilly) — [source](https://github.com/kittenchilly/BetterMultiplayer).

BM baseline (v1.6.3): TeamToJoin soft-lock, NoBossFightRespawn, Witch Doctor wormholes.  
BestMultiplayer: join-once team; unlimited team teleport; boss lives (budget then lock); death spectate + optional respawn at spectate target.

## Architecture

One mod (`side = Both`). Server policy vs client presentation.

```text
BestMultiplayer.cs              thin Mod entry (packets)
Common/Configs/                 ServerConfig + ClientConfig
Common/Players/                 team, lives lock, boss respawn, spectate
Common/Systems/                 boss pools, death UI, wormhole hooks
Common/UI/                      spectate head grid
Localization/                   en-US hjson
wiki/                           design notes
```

| Concern | Config | Runtime |
|---|---|---|
| Team, boss lives, teammate respawn, unlimited TP | `ServerConfig` | Players / Systems |
| Spectate prefs | `ClientConfig` | SpectatePlayer / death UI |
| Custom packets | — | section load + preferred respawn target |

Server config edits are host-only (`AcceptClientChanges`).

## Status

| Piece | State |
|---|---|
| TeamToJoin (join-once) | Done |
| Unlimited team teleport | Done |
| Boss fight lives (PerPlayer / PerTeam) | Done |
| Death UI + team spectate (dead only) | Done |
| Respawn at spectate target (boss deaths) | Done |

## Build

- In-game: `Workshop → Develop Mods → Build + Reload`
- VS Code: `tModLoader: build mod` task
- Repo under `ModSources`; keep mod folder / `build.txt` / type names aligned

Local `.dotnet` is used by the VS Code task (gitignored).

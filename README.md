# The Definitive Multiplayer Mod

tModLoader 1.4.4.9 multiplayer QoL mod.

## Goal

Stronger multiplayer experience than [Better Multiplayer](https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993) (kittenchilly) — [source](https://github.com/kittenchilly/BetterMultiplayer).

BM baseline (v1.6.3): TeamToJoin soft-lock, NoBossFightRespawn, Witch Doctor wormholes.  
DefinitiveMultiplayer: join-once team; unlimited team teleport; boss lives (budget then lock); death spectate (teammates + bosses, smooth cam) + optional respawn at spectate target.

## Architecture

One mod (`side = Both`). Server policy vs client presentation.

```text
DefinitiveMultiplayer.cs     thin Mod entry (packets)
Common/Configs/                 ServerConfig + ClientConfig
Common/Players/                 team, lives lock, boss respawn, spectate
Common/Systems/                 boss pools, fight stats, death UI, wormhole hooks
Common/UI/                      spectate grid, fight stats feed
Localization/                   en-US hjson
wiki/                           design notes
```

| Concern | Config | Runtime |
|---|---|---|
| Team, boss lives, teammate respawn, unlimited TP | `ServerConfig` | Players / Systems |
| Spectate prefs | `ClientConfig` | SpectatePlayer / death UI |
| Custom packets | — | section load + preferred respawn target |

Server config edits are allowed from any client (`AcceptClientChanges` always accepts) so cloud/dedicated hosts work without an in-game host.

## Status

| Piece | State |
|---|---|
| TeamToJoin (join-once) | Done |
| Unlimited team teleport | Done |
| Boss fight lives (PerPlayer / PerTeam) | Done |
| Death UI + team spectate (dead only) | Done |
| Boss spectate + smooth camera | Done |
| Respawn at spectate target (boss deaths) | Done |
| Instant respawn when boss ends | Done |
| Boss fight stats feed (live) | Done |
| Respawn timer policy + boss-death escalate | Done |
| HP% / mana% on respawn | Done |

## Build

- In-game: `Workshop → Develop Mods → Build + Reload`
- VS Code: `tModLoader: build mod` task
- Repo under `ModSources`; keep mod folder / `build.txt` / type names aligned

Local `.dotnet` is used by the VS Code task (gitignored).

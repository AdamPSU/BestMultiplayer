---
title: Multiplayer Config Rules
description: Client vs server config scope, host-only edits, and when custom packets are allowed.
date: 2026-07-27
tags: [multiplayer, networking, config, tmodloader]
---

Implemented as two `ModConfig` classes.[^1]

| Concern | Rule | Type |
|---|---|---|
| Client presentation prefs | `ConfigScope.ClientSide` | `ClientConfig` |
| Shared gameplay / server policy | `ConfigScope.ServerSide` | `ServerConfig` |
| Who may edit server config in MP | Host only (`AcceptClientChanges`) | `ServerConfig` |
| Custom network packets | Only for runtime state tModLoader does not already sync | — |
| Load side | `side = Both` in `build.txt`[^2] | — |

```mermaid
flowchart LR
  Client["Client"] -->|"ClientSide prefs local"| UI["Presentation"]
  Client -->|"ServerSide / gameplay"| Server["Server authority"]
  Server -->|"tModLoader sync first"| Client
  Server -->|"Custom packet only if needed"| Client
```

## ServerConfig fields

| Field | Default | Status | Intent |
|---|---|---|---|
| `TeamToJoin` | `Red` | **Done** (join-once) | Assign team on world enter; `None` disables; no soft-lock — see [TeamToJoin](team-to-join.md) |
| `BossFightLivesMode` | `PerPlayer` | **Done** | `Off` / `PerPlayer` / `PerTeam` — see [Boss fight lives](boss-fight-lives.md) |
| `BossFightRespawns` | `1` | **Done** | PerPlayer budget; `0` = BM first-death lock; ignored in PerTeam (pool = team size) |
| `RespawnAtTeammateDuringBoss` | `true` | **Done** | Boss-death respawn beside **spectate target** only; else vanilla spawn |
| `InstantRespawnOnBossEnd` | `true` | **Done** | Zero `respawnTimer` for all dead players when boss fight ends |
| `BossFightStatsEnabled` | `true` | **Done** | Live boss-fight stats feed — see [boss fight stats](boss-fight-stats.md) |
| `SharedHealthEnabled` | `false` | **Done** | Shared team HP — see [shared team health](shared-boss-health.md) |
| `SharedHealthBossesOnly` | `false` | **Done** | Limit shared HP to boss fights |
| `SharedHealthMultiplier` | `1.0` | **Done** | Pool max × Σ living max HP (0.5–1.5) |
| `UnlimitedTeamTeleport` | `true` | **Done** | Virtual wormhole for map team-TP — see [Unlimited team teleport](unlimited-team-teleport.md) |
| `BlockUnlimitedTeleportDuringBoss` | `false` | **Done** | When true, unlimited rule off during boss/EoW |

## ClientConfig fields

| Field | Default | Intent |
|---|---|---|
| `SpectateOnDeath` | `true` | Auto-follow teammate after death intro |
| `ShowBossFightStats` | `true` | Show boss-fight stats feed locally |

## Related

- [TeamToJoin](team-to-join.md)
- [Boss fight lives](boss-fight-lives.md)
- [Unlimited team teleport](unlimited-team-teleport.md)
- [Mod anatomy](mod-anatomy.md)
- [Scaffold conventions](scaffold-conventions.md)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: Common/Configs/ServerConfig.cs, Common/Configs/ClientConfig.cs, Common/Players/BestMultiplayerPlayer.cs
[^2]: build.txt

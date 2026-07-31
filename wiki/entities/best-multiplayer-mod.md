---
title: BestMultiplayer Mod
description: Concrete mod identity — entry class, packaging metadata, and feature surface.
date: 2026-07-27
tags: [mod, entity, tmodloader, terraria]
---

**BestMultiplayer** is a tModLoader mod package. Version **0.1**, author placeholder **Your Name**, display name **BestMultiplayer**.[^1][^2]

**Goal:** surpass [Better Multiplayer](better-multiplayer-baseline.md) (kittenchilly) — Workshop + [source](https://github.com/kittenchilly/BetterMultiplayer).

## Entry class

```text
namespace BestMultiplayer
public sealed class BestMultiplayer : Mod { }
```

Thin sealed `Mod` subclass. Features live under `Common/`.[^1]

## Packaging

| Field | Value |
|---|---|
| displayName | BestMultiplayer |
| author | Your Name |
| version | 0.1 |
| side | Both |
| description | Multiplayer QoL (teams, boss lives, spectate, unlimited team TP).[^2][^3] |

## Feature status

| Feature | Status |
|---|---|
| [TeamToJoin](../concepts/team-to-join.md) (join-once) | Done |
| [Unlimited team teleport](../concepts/unlimited-team-teleport.md) | Done |
| [Boss fight lives](../concepts/boss-fight-lives.md) | Done |
| Respawn at spectate target (boss deaths) | Done (`RespawnAtTeammateDuringBoss`) |
| Instant respawn when boss ends | Done (always on) |
| [Shared team health](../concepts/shared-boss-health.md) | Done (`SharedHealthEnabled`, default off) |
| Spectate (client) | Death-only team camera + custom death UI |
| [Boss fight stats feed](../concepts/boss-fight-stats.md) | Done (layout M live feed) |
| Collection add-on | [Shared World Map](../concepts/collection-addons.md) |

## Surface

| Type | Path |
|---|---|
| Server policy config | `Common/Configs/ServerConfig.cs` |
| Client UX config | `Common/Configs/ClientConfig.cs` |
| Player hooks | `Common/Players/BestMultiplayerPlayer.cs`, `SpectatePlayer.cs`, `SharedHealthPlayer.cs`, `FightStatsPlayer.cs` |
| Shared HP | `Common/Systems/SharedHealthSystem.cs` |
| Boss session | `Common/Systems/BossFightSystem.cs` |
| Fight stats | `Common/Systems/FightStatsSystem.cs`, `FightStatsUISystem.cs`, `Common/UI/FightStatsFeedState.cs` |
| Death UI + grid host | `Common/Systems/DeathScreenSystem.cs` |
| Wormhole hooks | `Common/Systems/WormholeSystem.cs` |
| Spectate grid | `Common/UI/SpectateGridState.cs` |
| Packet ids | `Common/Packets.cs` |
| Localization | `Localization/en-US_Mods.BestMultiplayer.hjson` |

## Related

- [Mod anatomy](../concepts/mod-anatomy.md)
- [Roadmap](../concepts/roadmap.md) · [Collection](../concepts/collection-addons.md) · [Catalog ack](../concepts/catalog-ack.md)
- [TeamToJoin](../concepts/team-to-join.md)
- [Better Multiplayer baseline](better-multiplayer-baseline.md)
- [Build pipeline](build-pipeline.md)

[^1]: BestMultiplayer.cs
[^2]: build.txt
[^3]: description.txt

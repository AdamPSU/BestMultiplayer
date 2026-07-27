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
| description | Multiplayer QoL; TeamToJoin done, more pending.[^2][^3] |

## Feature status

| Feature | Status |
|---|---|
| [TeamToJoin](../concepts/team-to-join.md) (join-once) | Done |
| [Unlimited team teleport](../concepts/unlimited-team-teleport.md) | Done |
| [Boss fight lives](../concepts/boss-fight-lives.md) | Done |
| Spectate (client) | Death-only team camera + custom death UI |
| Planned (boss/death/modes) | See [roadmap](../concepts/roadmap.md) |
| Collection add-on | [Shared World Map](../concepts/collection-addons.md) |

## Scaffold surface

| Type | Path |
|---|---|
| Server policy config | `Common/Configs/ServerConfig.cs` |
| Client UX config | `Common/Configs/ClientConfig.cs` |
| Player hooks | `Common/Players/BestMultiplayerPlayer.cs` |
| Boss session helper | `Common/Systems/BossFightSystem.cs` |
| Wormhole hooks | `Common/Systems/WormholeSystem.cs` |
| Shop hooks (unused) | `Common/GlobalNPCs/ShopGlobalNPC.cs` |
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

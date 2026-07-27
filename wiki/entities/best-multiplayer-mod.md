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
| [Free team wormhole](../concepts/free-team-wormhole.md) | Done |
| [Boss fight lives](../concepts/boss-fight-lives.md) | Done |
| Spectate (client) | Config only |

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
- [TeamToJoin](../concepts/team-to-join.md)
- [Better Multiplayer baseline](better-multiplayer-baseline.md)
- [Build pipeline](build-pipeline.md)

[^1]: BestMultiplayer.cs
[^2]: build.txt
[^3]: description.txt

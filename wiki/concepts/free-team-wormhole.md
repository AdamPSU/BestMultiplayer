---
title: Free Team Wormhole
description: Virtual infinite wormhole via vanilla HasUnityPotion/TakeUnityPotion hooks — no custom teleport.
date: 2026-07-27
tags: [feature, wormhole, multiplayer, server-config]
---

Map-click teleport to **same-team** players works without carrying Wormhole Potions. Vanilla UI, same-team gate, chat announce, and net teleport stay untouched.[^1]

## Config

| Field | Default | Meaning |
|---|---|---|
| `FreeTeamWormhole` | `true` | Pretend the player has a wormhole (no inventory slot) |
| `BlockFreeWormholeDuringBoss` | `false` | When true, free rule off while boss/EoW active |

Real potions remain craftable/lootable. While free is on, `TakeUnityPotion` no-ops so real stacks are not consumed on TP.

## How

Vanilla map TP checks legacy names:

1. `Player.HasUnityPotion()` — any wormhole in inventory/void bag?
2. After TP: `Player.TakeUnityPotion()` — consume one

`WormholeSystem` hooks both (`On_Player.*`):

- Has → `true` when `ShouldFakeWormhole`
- Take → skip consume when faking

`ShouldFakeWormhole`: alive, `FreeTeamWormhole`, and not (`BlockFreeWormholeDuringBoss` && `BossFightSystem.IsBossFightActive()`).

## vs Better Multiplayer

| | BestMultiplayer | Better Multiplayer |
|---|---|---|
| Approach | Free virtual potion | Witch Doctor sells potions |
| Inventory | No slot needed | Must buy/carry stacks |
| Boss gate | Optional host toggle (default allow) | N/A |

## Code

- `Common/Systems/WormholeSystem.cs`
- `Common/Systems/BossFightSystem.cs` — shared boss detect
- `Common/Configs/ServerConfig.cs`

## Related

- [Multiplayer config rules](multiplayer-config-rules.md)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: Common/Systems/WormholeSystem.cs; vanilla `Player.HasUnityPotion` / `TakeUnityPotion`

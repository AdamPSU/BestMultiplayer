---
title: Boss fight defense
description: Host slider for how much player defense applies during boss fights.
date: 2026-08-04
tags: [boss, defense, multiplayer, server-config]
---

Server-side challenge/tuning knob: while any boss segment is alive, incoming **non-PvP** damage uses only a fraction of each player’s defense.[^1]

## Config

| Field | Scope | Default | Range |
|---|---|---|---|
| `BossFightDefenseMultiplier` | Server | `1.0` | `0.1`–`1.0` (step `0.05`) |

| Value | Meaning |
|---|---|
| `1.0` | Full vanilla defense (no change) |
| `0.80` | 80% of player defense applies |
| `0.1` | 10% of player defense applies |

No separate master toggle — minimum is `0.1` during boss fights. Raise to `1.0` for vanilla feel.

## Behavior

| Rule | Detail |
|---|---|
| When | `BossFightSystem.IsBossFightActive()` (any boss / EoW segment) |
| Who | All players (not team-gated) |
| What | Non-PvP hurts only (`HurtModifiers.PvP` skipped) |
| Math | `ScalingArmorPenetration += 1f - clamp(multiplier, 0.1, 1)` |
| Stacking | Additive with other mods’ scaling armor penetration |
| Independence | Does not alter shared HP, lives, or stats feed |

## Code

- `Common/Configs/ServerConfig.cs` — `BossFightDefenseMultiplier`
- `Common/Players/BossFightDefensePlayer.cs` — `ModifyHurt`
- `Common/Systems/BossFightSystem.cs` — fight-active gate

## Related

- [Boss fight lives](boss-fight-lives.md)
- [Shared team health](shared-boss-health.md)
- [Multiplayer config rules](multiplayer-config-rules.md)

[^1]: Common/Players/BossFightDefensePlayer.cs

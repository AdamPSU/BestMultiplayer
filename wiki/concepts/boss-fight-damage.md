---
title: Boss fight damage
description: Host sliders for damage taken, damage dealt, and boss HP during boss fights (0.5×–3×).
date: 2026-08-07
tags: [boss, damage, multiplayer, server-config]
---

Server-side challenge/tuning knobs while any boss segment is alive: scale incoming non-PvP damage, outgoing player damage, and boss max HP at spawn.[^1]

## Config

| Field | Scope | Default | Range |
|---|---|---|---|
| `BossFightDamageMultiplier` | Server | `1.0` | `0.5`–`3.0` (step `0.1`) |
| `BossFightDamageDealtMultiplier` | Server | `1.0` | `0.5`–`3.0` (step `0.1`) |
| `BossFightBossHealthMultiplier` | Server | `1.0` | `0.5`–`3.0` (step `0.1`) |

| Value | Meaning |
|---|---|
| `1.0` | Vanilla (no change) |
| `0.5` | Half |
| `2.0` | Double |
| `3.0` | Triple |

No separate master toggle — set each to `1.0` for vanilla feel.

## Behavior

### Damage taken (`BossFightDamageMultiplier`)

| Rule | Detail |
|---|---|
| When | `BossFightSystem.IsBossFightActive()` (any boss / EoW segment) |
| Who | All players (not team-gated) |
| What | Non-PvP hurts only (`HurtModifiers.PvP` skipped) |
| Math | `FinalDamage *= clamp(mult, 0.5, 3)` when mult ≠ 1 |

### Damage dealt (`BossFightDamageDealtMultiplier`)

| Rule | Detail |
|---|---|
| When | `BossFightSystem.IsBossFightActive()` |
| Who | All players |
| What | All NPC hits (item, projectile, etc.) while a boss is alive |
| Math | `FinalDamage *= clamp(mult, 0.5, 3)` when mult ≠ 1 |

### Boss HP (`BossFightBossHealthMultiplier`)

| Rule | Detail |
|---|---|
| When | Boss segment `OnSpawn` (after difficulty scaling) |
| Who | `BossNpc.IsAnySegment` only |
| What | `lifeMax` and `life` multiplied at spawn |
| Math | `lifeMax = max(1, (int)(lifeMax * mult))`; `life = lifeMax` when mult ≠ 1 |
| Note | Spawn-time only — changing the slider mid-fight does not retune living bosses |

### Shared rules

| Rule | Detail |
|---|---|
| Stacking | Multiplies with other mods’ final-damage / life modifiers |
| Independence | Does not alter shared HP, lives, or stats feed (scaled hits still flow into the feed) |

## Code

- `Common/Configs/ServerConfig.cs` — three multipliers
- `Common/Players/BossFightDamagePlayer.cs` — `ModifyHurt`, `ModifyHitNPC`
- `Common/NPCs/BossFightBossHealthGlobalNPC.cs` — `OnSpawn` life scale
- `Common/Systems/BossFightSystem.cs` — fight-active gate
- `Common/BossNpc.cs` — boss-segment check

## Related

- [Boss fight lives](boss-fight-lives.md)
- [Shared team health](shared-boss-health.md)
- [Multiplayer config rules](multiplayer-config-rules.md)

[^1]: Common/Players/BossFightDamagePlayer.cs; Common/NPCs/BossFightBossHealthGlobalNPC.cs

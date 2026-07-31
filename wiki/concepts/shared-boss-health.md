---
title: Shared Team Health
description: Optional shared HP pool for teams — always-on or bosses-only, with pool size slider.
date: 2026-07-30
tags: [feature, challenge, multiplayer, server-config]
---

Challenge mode (**off by default**). Teamed players (1–5) share one HP pool; everyone’s vanilla life bar shows the pool. Independent of [boss fight lives](boss-fight-lives.md). Unteamed players are unchanged.[^1]

## Config (header: Shared Health Mode)

| Field | Default | Meaning |
|---|---|---|
| `SharedHealthEnabled` | `false` | Master toggle |
| `SharedHealthBossesOnly` | `false` | If true, only arm during boss fights; if false, whole session |
| `SharedHealthMultiplier` | `0.5` | Pool max = Σ living `statLifeMax2` × multiplier. Range **0.5–1.5** (native tMod slider) |

Lives mode is **not** turned off when this is enabled — both can run together.

## Pool math

- **Max** = `round(Σ living natural max HP × multiplier)`, at least 1.
- **Arm:** when mode should be active and a team has living members — start **full**.
- **Join:** new living teammate expands max; their current life is **added** to pool current (capped).
- **Leave:** max recalculated from remaining living; current scales to new max.
- **Damage / heal:** server/SP reconcile (min life = damage, max life = heal; damage wins same frame).
- **Multiplier change** while armed: scale current by `newMax/oldMax`.

## Wipe

Any living team member dying while the pool is armed and not already wiped **DeathLinks** the whole team (`PreKill`/`Kill` + server death-edge fallback). Pool is marked wiped (not dropped) so it does not silently re-arm mid-challenge.

| Context | Behavior |
|---|---|
| Boss fight active | Team dies; **hard-lock** until boss ends (ignores remaining lives budget for that wipe). Death text: “Your team fell together…”. |
| Outside boss | Team dies; **normal respawn** (no hard-lock). Pool re-arms full when someone is living again. |

Works with always-on instant respawn on boss end for hard-locks.

## Code

- `Common/Systems/SharedHealthSystem.cs`
- `Common/Players/SharedHealthPlayer.cs`
- `Common/Configs/ServerConfig.cs`
- `BossFightSystem.IsPlayerHardLocked` only when wipe **and** boss active

## Related

- [Boss fight lives](boss-fight-lives.md)
- [Roadmap](roadmap.md)

[^1]: Common/Systems/SharedHealthSystem.cs

---
title: Shared Team Health
description: Optional shared HP pool for teams — always-on or bosses-only, event-based server pool projected onto vanilla bars.
date: 2026-08-06
tags: [feature, challenge, multiplayer, server-config]
---

Challenge mode (**off by default**). Teamed players (1–5) share one absolute HP pool; each living member’s vanilla life bar shows the pool. Independent of [boss fight lives](boss-fight-lives.md). Unteamed players are unchanged.[^1]

## Config (header: Shared Health)

| Field | Default | Meaning |
|---|---|---|
| `SharedHealthEnabled` | `false` | Master toggle |
| `SharedHealthBossesOnly` | `false` | If true, only arm during boss fights; if false, whole session |
| `SharedHealthMultiplier` | `0.75` | 2+ living: pool max = Σ natural × mult (0.5–2). **Solo = full personal max** (mult ignored). |

## Model (v2 — event pool, not bar reconcile)

- **Server owns** `Current` / `Max` / `Wiped` / monotonic `poolSeq` per team.
- **Max:** solo living → `NaturalMax`; 2+ living → `round(Σ NaturalMax × mult)` (e.g. two 400s at 0.75 → 600). Grow-only while armed (death never shrinks); late join scales Current by fill ratio.
- **Pool moves only via events:** `OnHurt` damage; potion consume / heart pickup heals. No min/max-of-bars sensing.
- **Clients** send idempotent damage/heal packets (`eventId`); server acks via meta. Display = `clamp(server − unackedDmg + unackedHeal, 0, Max)`.
- **Paint:** client paints **local** bar only; server paints living members for sim. No per-tick `PlayerLifeMana`.
- **Hearts UI:** visual max = `min(20, poolMax/20) × 20` (never extra rows past 400). Fill % = real pool (`cur×visualMax/poolMax`). Hover/feed still show real pool cur/max.
- **Regen:** `lifeRegen` zeroed while linked (potions/hearts are the heal path).
- **Hearts:** heal pool, no team potion sickness.
- **Potions:** heal pool; drinker vanilla sick; other living teammates get sick ~1s later **iff** server applied heal &gt; 0.
- **Wipe:** `Current ≤ 0` → DeathLink; boss active → hard-lock (`RespawnGate.SharedHealthWipe`), **no boss-life spend**. Outside boss → normal respawn and re-arm.

## Code

- `Common/Systems/SharedHealthSystem.cs`
- `Common/Players/SharedHealthPlayer.cs` (+ `SharedHealthHealItem`)
- `Common/Configs/ServerConfig.cs`

## Related

- [Boss fight lives](boss-fight-lives.md)
- [Roadmap](roadmap.md)

[^1]: Common/Systems/SharedHealthSystem.cs

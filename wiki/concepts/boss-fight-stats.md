---
title: Boss fight stats feed
description: Live one-line team damage feed during boss fights (layout M).
date: 2026-07-31
tags: [boss, stats, ui, multiplayer]
---

Compact live feed during boss fights: mini head strip + selected player’s dealt % / taken % / deaths.

## Behavior

| Rule | Detail |
|---|---|
| When | Boss fight active (`BossFightSystem.IsBossFightActive`) + 6s freeze after |
| Who | Same team as local (team 0 = self only) |
| Stats | `N% dealt` · `N% taken` · `N deaths` — % of **team totals** |
| Dealt | Damage to boss segments only (`BossNpc.IsAnySegment`) |
| Taken | Share of team damage taken (always; not gated on shared HP) |
| Layout | **M** — head strip (dim non-selected ~40%) + labeled stats for selected |
| Place | Left of Settings; **Y-centered on vanilla boss bar** (`screenH - 50`) |
| Click head | Select that player |
| Click panel / right-click | Next / previous in dealt-desc order |
| Hide | Inventory open; server or client toggle off |

## Config

| Field | Scope | Default |
|---|---|---|
| `BossFightStatsEnabled` | Server | on |
| `ShowBossFightStats` | Client | on |

## Authority

tML calls `ModPlayer.OnHitNPC` / `OnHurt` **only on the local client** that hit/was hit. Clients apply deltas locally for instant UI, send `FightStatsDelta` to the server; server aggregates and broadcasts `FightStatsSnapshot` (~15 ticks when dirty + fight end). SP applies locally only. First hit can arm tracking before `PostUpdatePlayers` (ordering race).

## Files

- `Common/Systems/FightStatsSystem.cs` — track + packet
- `Common/Players/FightStatsPlayer.cs` — OnHitNPC / OnHurt
- `Common/UI/FightStatsFeedState.cs` — layout M
- `Common/Systems/FightStatsUISystem.cs` — client host

## Related

- [Roadmap](roadmap.md)
- [Boss fight lives](boss-fight-lives.md)
- [Team spectate](team-spectate.md) (shared head chrome language)

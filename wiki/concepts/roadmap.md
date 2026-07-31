---
title: Feature roadmap
description: Planned BestMultiplayer features after multiplayer pain-point research (2026-07-31).
date: 2026-07-31
tags: [roadmap, planned, multiplayer]
---

Planned work only — **not implemented yet**. Active todo from the 2026-07-31 research pass. Prior pass items are archived under **Done**.

## Active todo (priority order)

1. **Pings**
2. **Party UI**
3. **Shared World Map**
4. **Improved Respawning (subset)**

### Pings

- Team point-of-interest markers: self, tile, NPC/enemy, dropped item.
- World + map visibility; optional hold-to-chat notify.
- Server config: duration, limit, cooldown; client visuals.
- **Inspiration:** [Pings](https://steamcommunity.com/sharedfiles/filedetails/?id=2803799129) (direwolf420) — clean-room.

### Party UI — **partial (boss-only HP under heads)**

- **Done:** fight-stats feed heads enlarged (36px) + HP bar under each head during boss fights.
- Still open: always-on frames; mana / potion CD / respawn on frames; gear peek.
- **Inspiration:** [Party UI](https://steamcommunity.com/sharedfiles/filedetails/?id=3520267377), Team Info, ETUD — clean-room.

### Shared World Map

- Exploration fog sync for co-op (live and/or share/request).
- **Open decision:** absorb a minimal in-mod path vs keep [collection add-on](collection-addons.md) ([Shared World Map](https://steamcommunity.com/sharedfiles/filedetails/?id=2815010161)).
- Ship whichever path closes the fog gap for BestMultiplayer players.

### Improved Respawning (subset)

Cherry-pick remaining IR features — **not** full Improved Respawning. Already covered elsewhere: boss lives, respawn-at-spectate-target (boss), instant respawn on boss end.

Candidates still open (confirm when implementing):

- Ghost free-move while waiting to respawn
- Respawn near living teammate outside boss fights (distance + team gates)
- Configurable respawn timer (boss / non-boss)
- HP% / mana% on respawn
- Keep buffs on death (optional multiplier)
- Freeze respawn timer while hard-locked (lives lock UX)
- Lives remaining HUD

**Inspiration:** [Improved Respawning](https://steamcommunity.com/sharedfiles/filedetails/?id=3098184209) — clean-room, host-togglable slices only.

## Done (2026-07-27 pass)

| Item | Notes |
|---|---|
| Respawn at teammate (boss deaths) | Spectate target only; see [boss fight lives](boss-fight-lives.md) |
| Boss spectate (dead only) | Bosses + teammates; A/D ring |
| Smooth camera lerp | ~0.3s smoothstep |
| Fight stats feed | Layout M; see [boss fight stats](boss-fight-stats.md) |
| Instant respawn on boss end | Always on |
| Shared Team Health | Optional mode; see [shared team health](shared-boss-health.md) |
| TeamToJoin, unlimited team TP, boss lives, death UI | Core BM baseline+ |

## Explicitly not in this roadmap

See [catalog acknowledgements](catalog-ack.md). Examples: InfraSonic, Dtboss projectile hide, full IR hardcore world-lives suite, Magic Storage / general QoL, connection/mod-sync fixes.

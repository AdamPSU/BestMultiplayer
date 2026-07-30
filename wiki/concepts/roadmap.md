---
title: Feature roadmap
description: Planned BestMultiplayer features selected after multiplayer catalog research (2026-07-27).
date: 2026-07-27
tags: [roadmap, planned, multiplayer]
---

Planned work only — **not implemented yet**. Selected from the multiplayer catalog pass; everything else is either done, [collection add-on](collection-addons.md), or [acknowledged without commit](catalog-ack.md).

## Planned in-mod

### Boss death → respawn at teammate (optional) — **done**

- **Scope:** boss-fight deaths only (not exploration/casual deaths).
- **Behavior:** `RespawnAtTeammateDuringBoss` (default **on**): respawn beside the **current spectate target** only if still living same-team. No target → vanilla spawn. No nearest fallback.
- See [boss fight lives](boss-fight-lives.md).

### Boss spectate (dead only) — **done**

- Death spectate cycles **active bosses** + teammates in one A/D ring; boss heads on grid.
- Preferred respawn remains player-only.
- **Inspiration:** Team Spectate boss list; Multiplayer Boss Spectator — clean-room.

### Smooth camera lerp — **done**

- ~0.3s smoothstep when switching spectate targets (player ↔ player, player ↔ boss); then hard-follow.
- Client presentation only.

### Mid-fight / end-fight damage board

- Track per-player damage dealt / taken / deaths (and optionally DPS) while a boss fight is active.
- **End of fight:** print a short board to chat (or UI).
- **Mid-fight:** keybind dumps “so far” stats.
- Host toggles which columns appear.
- **Inspiration:** Multiplayer Boss-Fight Stats (Workshop `2822937879`) — clean-room; may supersede that add-on later.

### Instant respawn when boss dies

- When the boss fight ends (boss dead/despawned and fight no longer active), dead players who were waiting or hard-locked are released and may respawn immediately (or with a minimal timer), instead of sitting out a full vanilla countdown.
- Pairs with [boss fight lives](boss-fight-lives.md) hard-lock UX.
- **Inspiration:** Improved Respawning “respawn on boss death”; BM community requests.

### Shared HP / DeathLink (togglable game mode)

- **Off by default.** Optional co-op challenge mode, not core QoL.
- **Shared HP:** damage to one linked player affects a shared pool (or mirrored HP) — exact model TBD at implement time.
- **DeathLink:** when the pool hits zero (or one linked player dies, mode-dependent), all linked players die.
- Server config only; clear opt-in so casual hosts never hit it by accident.
- **Inspiration:** Suffer Together / Shared Health Between Players — clean-room; separate mode, not default path.

## Priority order (suggested)

1. Instant respawn when boss dies  
2. Damage board (end-fight first, then mid-fight keybind)  
3. Shared HP / DeathLink mode  
4. ~~Boss spectate + smooth lerp~~ done  
5. ~~Boss-death respawn-at-teammate~~ done  


Order can change; implement when scheduled.

## Explicitly not in this roadmap

See [catalog acknowledgements](catalog-ack.md) and [collection add-ons](collection-addons.md). Examples: lives HUD polish, party frames, quick wormhole keybind, team soft-lock, full Improved Respawning, InfraSonic, Dtboss projectile hide (collection/ack only unless revisited).

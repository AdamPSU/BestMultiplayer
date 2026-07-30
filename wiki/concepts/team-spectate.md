---
title: Team Spectate
description: Death-only camera follow of living teammates and bosses, with smooth lerp and a custom death intro.
date: 2026-07-27
tags: [spectate, death, client, multiplayer, boss]
---

Local presentation only. Inspired by [Team Spectate](https://github.com/NotLe0n/TeamSpectate) and Multiplayer Boss Spectator — clean-room; not a soft dependency.[^1]

## Flow

1. **Intro (3s)** — hide vanilla death text. Title + `spectating in N`.
   - Normal: `You were slain...`
   - Boss hard-lock: `No lives left...`
2. **Spectate** — camera on target; clear death chrome; bottom-center **digits only** when respawn allowed. Hard-lock: no bottom counter.
3. **Corpse** — no target (Stop, or nothing valid). Bottom timer still applies when allowed.

Hotkeys (dead only): previous / next **target**, stop. Next/prev during intro skips remaining countdown.

**Grid UI (dead, multiplayer):** bottom band above respawn text. Self + teammates, then active boss heads. Click living teammate or boss to follow; click self or current target to stop. Dead heads greyed out.

## Targets

| Kind | Valid when |
|---|---|
| Player | Active, living, same team, not self |
| Boss | Active, not `dontCountMe`, head of multi-segment (`realLife`), and (`boss` or EoW head or boss-head texture index ≥ 0) |

**Unified A/D ring:** living teammates by `whoAmI`, then bosses by npc index (wrap).

**Auto after intro** (`SpectateOnDeath`): next living teammate first; if none, first valid boss; else corpse.

**Camera:** ~0.3s smoothstep lerp on target change; then hard-follow target center. Section packet every 10 ticks while following (MP).

## Config (`ClientConfig`)

- `SpectateOnDeath` (default true) — auto enter spectate after intro
- Spectate always clears on respawn (dead-only)

## Boss-death respawn

Preferred respawn whoAmI is set only while spectating a **player**. Boss or corpse → no preferred target → vanilla spawn. See [RespawnAtTeammateDuringBoss](boss-fight-lives.md).

## Code

- `Common/Players/SpectatePlayer.cs` — kind, step ring, lerp, hotkeys, section packet; nested `SpectateKeybinds`
- `Common/Systems/DeathScreenSystem.cs` — death text + grid UI host
- `Common/UI/SpectateGridState.cs` — player + boss head buttons
- `BestMultiplayer.HandlePacket` / `Packets` — section + preferred respawn

[^1]: Team Spectate Workshop 2563098343; Multiplayer Boss Spectator 2822925665; BM design 2026-07-27

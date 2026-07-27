---
title: Team Spectate
description: Death-only camera follow of living teammates, with a custom death intro and bottom respawn timer.
date: 2026-07-27
tags: [spectate, death, client, multiplayer]
---

Local presentation only. Inspired by [Team Spectate](https://github.com/NotLe0n/TeamSpectate) (NotLe0n) — clean-room; not a soft dependency.[^1]

## Flow

1. **Intro (3s)** — hide vanilla death text. Title + `spectating in N`.
   - Normal: `You were slain...`
   - Boss hard-lock: `No lives left...`
2. **Spectate** — camera on next living teammate by `whoAmI` slot (wrap). Clear death chrome; bottom-center **digits only** (small death font) when a respawn is allowed. Hard-lock: no bottom counter.
3. **Corpse** — no target (Stop, or no valid teammate). Bottom timer still applies when allowed.

Hotkeys (dead only): previous / next teammate, stop. Next/prev during intro skips remaining countdown.

## Rules

| Rule | Behavior |
|---|---|
| Valid target | Active, living, same team, not self |
| Auto after intro | Next teammate after local slot (`SpectateOnDeath`) |
| Target invalid | Re-pick next by slot; else corpse cam |
| Alive | No spectate (v1) |
| Intro tick hook | `ModPlayer.UpdateDead` (not `PostUpdate` — skipped while dead) |
| MP chunks | While dead + target, section packet every 10 ticks → `RemoteClient.CheckSection` |

## Config (`ClientConfig`)

- `SpectateOnDeath` (default true) — auto enter spectate after intro
- `StopSpectateOnRespawn` — always clear on respawn in v1 (dead-only)

## Code

- `Common/Players/SpectatePlayer.cs` — state, camera, hotkeys, `UpdateDead` tick, packets; nested `SpectateKeybinds`
- `Common/Systems/DeathScreenSystem.cs` — draw only
- `BestMultiplayer.HandlePacket` — section load

[^1]: Team Spectate Workshop 2563098343; BM design session 2026-07-27

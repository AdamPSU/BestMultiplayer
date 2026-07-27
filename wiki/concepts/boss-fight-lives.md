---
title: Boss Fight Lives
description: Configurable per-player or per-team respawn budget during boss fights, then BM-style hard lock. Locked death screen replaces the vanilla countdown.
date: 2026-07-27
tags: [feature, boss, respawn, multiplayer, server-config]
---

Deaths during a boss fight cost **respawns** from a budget. Empty budget → hard-lock `respawnTimer` (Better Multiplayer style) until the fight ends. Outside boss fights → vanilla. Death presentation is shared with [team spectate](team-spectate.md): hard-lock intro is **“No lives left…”** (no bottom respawn digits while locked).[^1]

## Config

| Field | Default | Meaning |
|---|---|---|
| `BossFightLivesMode` | `PerPlayer` | `Off` \| `PerPlayer` \| `PerTeam` |
| `BossFightRespawns` | `1` | **PerPlayer only.** Budget size; `0` = lock on first death (BM). Config UI shows “team size at fight start” under PerTeam (not this number). |

- **PerPlayer:** each player gets `BossFightRespawns`.
- **PerTeam:** shared pool per team = player count on that team at fight start; unteamed (`team == 0`) solo pool of `1`.
- **Off:** vanilla infinite respawns.

## Life math

Budget = **respawns remaining** (not including the current life).

Default `1`: die → may respawn once → die again → locked.

Spend on **death rising edge** (`dead` false→true) while boss active. If remaining &gt; 0, decrement and allow this respawn (`RespawnAllowedThisDeath`). If remaining was 0, lock.

## Fight edge

- Boss active and pools not ready → `InitPools` (covers fight start and mid-join into an active fight).
- Boss inactive → clear pools.
- Mid-join: PerPlayer gets full budget; PerTeam does **not** top up the shared pool.
- Already dead at init/join: allowed to finish vanilla respawn without spending.

Boss detect: `BossFightSystem.IsBossFightActive` — `npc.boss` + EoW segments (no `HasValidTarget`; that cleared pools when everyone was dead).

## Lock values (BM)

While locked, each `UpdateDead` sets `respawnTimer` to:

| Mode | Ticks |
|---|---|
| Classic | 1200 (20s) |
| Expert | 1800 (30s) |
| For the Worthy | 3600 (60s) |

## vs Better Multiplayer

| | BestMultiplayer | Better Multiplayer |
|---|---|---|
| Default | 1 respawn then lock | Always lock |
| Modes | Off / PerPlayer / PerTeam | Ban only |
| UX | Locked death text | None |

## Code

- `Common/Systems/BossFightSystem.cs` — detect, pools, death edges
- `Common/Systems/DeathScreenSystem.cs` — replace death text while hard-locked
- `Common/Players/BestMultiplayerPlayer.cs` — `UpdateDead` clamp
- `Common/Configs/ServerConfig.cs`

## Related

- [Multiplayer config rules](multiplayer-config-rules.md)
- [Unlimited team teleport](unlimited-team-teleport.md) (shared boss detect)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: Common/Systems/BossFightSystem.cs; Common/Players/BestMultiplayerPlayer.cs

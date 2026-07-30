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
| `RespawnAtTeammateDuringBoss` | `true` | After a death that occurred during a boss fight, respawn beside the teammate you were **spectating** (if still living, same team). No spectate target / Stop → vanilla bed/world spawn. No nearest fallback. |
| `InstantRespawnOnBossEnd` | `true` | When the boss fight ends (killed or despawned), set every dead player's `respawnTimer` to `0` so they respawn immediately (includes hard-lock). |

- **PerPlayer:** each player gets `BossFightRespawns`.
- **PerTeam:** shared pool per team = player count on that team at fight start; unteamed (`team == 0`) solo pool of `1`.
- **Off:** vanilla infinite respawns.

## Respawn at teammate

- Flag `DiedDuringBossFight` on `Kill` when `IsBossFightActive`.
- While dead, local spectate target is stored as `PreferredRespawnWhoAmI` and synced to the server (packet).
- On `OnRespawn` (server/SP): if config on and flag set, stash target (preferred if valid, else first living teammate). **Teleport runs in `PostUpdate`** — `OnRespawn` is before `Spawn_SetPosition`, so teleporting there is overwritten by bed/world spawn.
- Server relays via `TeleportEntity` + `CheckSection`.
- Outside boss deaths: unchanged vanilla spawn.

## Life math

Budget = **respawns remaining** (not including the current life).

Default `1`: die → may respawn once → die again → locked.

Spend on **death rising edge** (`dead` false→true) while boss active. If remaining &gt; 0, decrement and allow this respawn (`RespawnAllowedThisDeath`). If remaining was 0, lock.

## Fight edge

- Boss active and pools not ready → `InitPools` (covers fight start and mid-join into an active fight).
- Boss inactive → clear pools; on **active→inactive** edge, if `InstantRespawnOnBossEnd`, zero `respawnTimer` for all dead players (after that frame’s hard-lock `UpdateDead`).
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

## Interaction with Shared Boss Health

When shared team health is on and a team wipes **during a boss**, that team is hard-locked for the rest of the fight **even if** lives budget remains. Lives mode is not disabled. See [shared team health](shared-boss-health.md).

## vs Better Multiplayer

| | BestMultiplayer | Better Multiplayer |
|---|---|---|
| Default | 1 respawn then lock | Always lock |
| Modes | Off / PerPlayer / PerTeam | Ban only |
| UX | Locked death text | None |

## Code

- `Common/Systems/BossFightSystem.cs` — detect, pools, death edges
- `Common/Systems/DeathScreenSystem.cs` — replace death text while hard-locked
- `Common/Players/BestMultiplayerPlayer.cs` — `UpdateDead` clamp; teammate respawn teleport
- `Common/Players/SpectatePlayer.cs` — preferred target for teammate respawn
- `Common/Configs/ServerConfig.cs`

## Related

- [Multiplayer config rules](multiplayer-config-rules.md)
- [Unlimited team teleport](unlimited-team-teleport.md) (shared boss detect)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: Common/Systems/BossFightSystem.cs; Common/Players/BestMultiplayerPlayer.cs

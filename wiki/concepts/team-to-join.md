---
title: TeamToJoin
description: Join-once auto team assignment from ServerConfig — deliberate divergence from Better Multiplayer soft-lock.
date: 2026-07-27
tags: [feature, teams, multiplayer, server-config]
---

Host-chosen team assigned **once** when a player enters the world. Players may change teams afterward via vanilla UI.[^1]

## Config

| | |
|---|---|
| Field | `ServerConfig.TeamToJoin` |
| Scope | ServerSide (host-only edits) |
| Options | `None`, `Red`, `Green`, `Blue`, `Yellow`, `Pink` |
| Default | `Red` |
| Off | `None` |

Vanilla team ids: None→0, Red→1, Green→2, Blue→3, Yellow→4, Pink→5. No white team exists in Terraria.[^2]

## Behavior

1. `BestMultiplayerPlayer.OnEnterWorld` reads live `ServerConfig.Instance.TeamToJoin`.
2. If the value maps to a color (`1..5`), set `Player.team` and, when not singleplayer, `NetMessage.SendData(MessageID.PlayerTeam, …, Player.whoAmI)`.
3. If `None` or unknown → do nothing.
4. No further enforcement (`CopyClientState` / `SendClientChanges` **not** used).

| Event | Result |
|---|---|
| Join with Red | Player enters on Red |
| Player switches to Blue in UI | Stays Blue |
| Host changes Red→Green mid-session | In-world players unchanged until re-enter |
| Host changes → None | Stop assigning on future joins; current teams left as-is |

## vs Better Multiplayer

| | BestMultiplayer | Better Multiplayer |
|---|---|---|
| On enter | Assign once | Map team id |
| Stay on team | Optional (player choice) | Soft-lock via continuous client sync |
| Live config | Read at enter | Field init can freeze value at player create |

## Code

- `Common/Players/BestMultiplayerPlayer.cs` — `OnEnterWorld` + private `TryTeamId`
- `Common/Configs/ServerConfig.cs` — field only

## Related

- [Multiplayer config rules](multiplayer-config-rules.md)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: Common/Players/BestMultiplayerPlayer.cs
[^2]: Vanilla `Player.team` / team UI

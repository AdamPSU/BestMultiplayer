---
title: Mod Anatomy
description: How BestMultiplayer splits server policy vs client presentation — grounded in real tModLoader mods.
date: 2026-07-27
tags: [architecture, multiplayer, config, conventions]
---

BestMultiplayer is one package (`side = Both`). It loads on host, joiners, and dedicated server. Features differ by **authority**, not by shipping two DLLs.[^1]

## Authority split

| Layer | Scope | Who decides | Examples |
|---|---|---|---|
| Server policy | `ServerConfig` (`ServerSide`) | Host; synced to all | Team on join, unlimited team teleport, boss lives, boss-death teammate respawn |
| Client presentation | `ClientConfig` (`ClientSide`) | Each local player | Spectate on death |
| Runtime state | Players / Systems / packets | Server writes when shared | Respawn timers, team id, preferred respawn target |

Rule: if two players disagreeing would cause **desync or unfairness** → server. If it is **this screen only** → client.[^1]

## Folder map

```text
BestMultiplayer.cs
Common/Packets.cs
Common/Configs/ServerConfig.cs
Common/Configs/ClientConfig.cs
Common/Players/BestMultiplayerPlayer.cs
Common/Players/SpectatePlayer.cs
Common/Systems/BossFightSystem.cs
Common/Systems/DeathScreenSystem.cs   # death text + spectate grid host (client)
Common/Systems/WormholeSystem.cs
Common/UI/SpectateGridState.cs
Localization/en-US_Mods.BestMultiplayer.hjson
```

| Path | Role |
|---|---|
| `ServerConfig` | Host toggles + host `AcceptClientChanges` |
| `ClientConfig` | `SpectateOnDeath` |
| `BestMultiplayerPlayer` | Team on enter; lives lock; boss-death respawn at spectate target |
| `SpectatePlayer` (+ keybinds) | Death camera (players + bosses), lerp, hotkeys, section packet |
| `DeathScreenSystem` | Custom death text + MP player/boss head grid |
| `BossFightSystem` | Boss detect + lives pools |
| `WormholeSystem` | Unlimited team teleport hooks |
| `Packets` | Custom packet ids |

## Runtime gates

```text
Server / listen-server authority:  Main.netMode != MultiplayerClient
Local presentation only:           !Main.dedServ && player.whoAmI == Main.myPlayer
```

Custom packets only when tModLoader does not already sync the state.[^1]

## Related

- [Multiplayer config rules](multiplayer-config-rules.md)
- [Scaffold conventions](scaffold-conventions.md)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: README.md; tModLoader ModConfig docs (`ConfigScope.ClientSide` / `ServerSide`)

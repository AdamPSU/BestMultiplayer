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
| Server policy | `ServerConfig` (`ServerSide`) | Host; synced to all | Team on join, free wormhole, boss respawn lock |
| Client presentation | `ClientConfig` (`ClientSide`) | Each local player | Spectate on death, HUD prefs |
| Runtime state | Players / Systems / packets | Server writes when shared | Respawn timers, team id |

Rule: if two players disagreeing would cause **desync or unfairness** → server. If it is **this screen only** → client.[^1]

## Folder map (implemented scaffold)

```text
BestMultiplayer.cs
Common/Configs/ServerConfig.cs
Common/Configs/ClientConfig.cs
Common/Players/BestMultiplayerPlayer.cs
Common/Systems/BossFightSystem.cs
Common/Systems/WormholeSystem.cs
Common/GlobalNPCs/ShopGlobalNPC.cs
Localization/en-US_Mods.BestMultiplayer.hjson
```

| Path | Role | Status |
|---|---|---|
| `ServerConfig` | Host toggles | Fields + host `AcceptClientChanges` |
| `ClientConfig` | Spectate prefs | Fields only |
| `BestMultiplayerPlayer` | Team on enter; later respawn / spectate | TeamToJoin done |
| `BossFightSystem` | Boss-active detection | `IsBossFightActive` done |
| `WormholeSystem` | Free team wormhole hooks | Done |
| `ShopGlobalNPC` | Unused (shop path dropped) | Empty stub |
| `Common/UI/` | Spectate menus | Not created yet |
| `Content/` | New game content | Not created yet |

## Runtime gates

```text
Server / listen-server authority:  Main.netMode != MultiplayerClient
Local presentation only:           !Main.dedServ && player.whoAmI == Main.myPlayer
```

Custom packets only when tModLoader does not already sync the state (prefer vanilla team/life/NPC sync first).[^1]

## Real-mod precedents

| Mod | Pattern |
|---|---|
| ExampleMod | `Common/` + `Content/`; thin `Mod` |
| Calamity | `CalamityClientConfig` + `CalamityServerConfig` |
| Magic Storage | Client + server configs; `Common/Players|Systems`; `AcceptClientChanges` for ops |
| Team Spectate | Client-only config; `ModPlayer` camera; UI systems |
| Better Multiplayer | Server-only config (3 rules) — our baseline, not our ceiling |

## Related

- [Multiplayer config rules](multiplayer-config-rules.md)
- [Scaffold conventions](scaffold-conventions.md)
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)

[^1]: README.md; tModLoader ModConfig docs (`ConfigScope.ClientSide` / `ServerSide`)

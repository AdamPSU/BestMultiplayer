---
title: Multiplayer Config Rules
description: Client vs server config scope and when custom network packets are allowed.
date: 2026-07-27
tags: [multiplayer, networking, config, tmodloader]
---

BestMultiplayer is named for multiplayer-safe design. The README states the policy; no configs or packets are implemented yet.[^1]

| Concern | Rule |
|---|---|
| Client presentation prefs | `ConfigScope.ClientSide` |
| Shared gameplay / server policy | `ConfigScope.ServerSide` |
| Custom network packets | Only for runtime state tModLoader does not already sync |
| Load side | `side = Both` in `build.txt`[^2] |

```mermaid
flowchart LR
  Client["Client"] -->|"ClientSide prefs local"| UI["Presentation"]
  Client -->|"ServerSide / gameplay"| Server["Server authority"]
  Server -->|"tModLoader sync first"| Client
  Server -->|"Custom packet only if needed"| Client
```

## Why it matters

Splitting scopes early avoids desync and “host-only feels different” bugs. Prefer built-in tModLoader synchronization before inventing packet types.[^1]

## Related

- [Scaffold conventions](scaffold-conventions.md)
- [BestMultiplayer mod](../entities/best-multiplayer-mod.md)

[^1]: README.md
[^2]: build.txt

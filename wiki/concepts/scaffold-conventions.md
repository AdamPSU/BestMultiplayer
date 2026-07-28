---
title: Scaffold Conventions
description: How features are added to BestMultiplayer without bloating the Mod entry class.
date: 2026-07-27
tags: [conventions, architecture, tmodloader, scaffold]
---

New work goes into typed folders under `Common/` (and later `Content/` if needed). The root `Mod` class stays limited to lifecycle or networking.[^1][^2]

```mermaid
flowchart TB
  Mod["BestMultiplayer : Mod"]
  Mod --> Configs["Common/Configs"]
  Mod --> Systems["Common/Systems"]
  Mod --> Players["Common/Players"]
  Mod --> UI["Common/UI"]
  Mod --> Loc["Localization/"]
```

## Folders

| Path | Responsibility |
|---|---|
| `Common/Configs/` | `ServerConfig`, `ClientConfig`, custom config elements |
| `Common/Systems/` | Boss lives, death UI host, wormhole hooks |
| `Common/Players/` | Team join, lives lock, spectate, boss-death respawn |
| `Common/UI/` | Spectate head grid state |
| `Common/Packets.cs` | Custom packet ids |
| `Localization/` | `.hjson` labels and tooltips |
| `Content/` | Items/NPCs/tiles — only when needed |

## Naming lockstep

On rename, keep aligned: mod folder, `build.txt`, main `.cs` type/namespace, `.csproj` `AssemblyName` / `RootNamespace`.[^1][^3]

## Related

- [Mod anatomy](mod-anatomy.md)
- [Multiplayer config rules](multiplayer-config-rules.md)
- [BestMultiplayer mod](../entities/best-multiplayer-mod.md)

[^1]: README.md
[^2]: BestMultiplayer.cs
[^3]: BestMultiplayer.csproj

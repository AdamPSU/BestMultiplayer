---
title: BestMultiplayer Mod
description: Concrete mod identity — entry class, packaging metadata, and current emptiness.
date: 2026-07-27
tags: [mod, entity, tmodloader, terraria]
---

**BestMultiplayer** is a tModLoader mod package: one C# entry type plus packaging/build metadata. Version **0.1**, author placeholder **Your Name**, display name **BestMultiplayer**.[^1][^2]

## Entry class

```text
namespace BestMultiplayer
public sealed class BestMultiplayer : Mod { }
```

Empty sealed `Mod` subclass. Comment directs features into separate folders/types; keep this class for lifecycle/networking only.[^1]

## Packaging

| Field | Value |
|---|---|
| displayName | BestMultiplayer |
| author | Your Name |
| version | 0.1 |
| side | Both |
| description | A feature-neutral tModLoader mod scaffold.[^2][^3] |

`buildIgnore` excludes project/IDE/build dirs from the packaged mod.[^2]

## Project identity

| Property | Value |
|---|---|
| AssemblyName | BestMultiplayer |
| RootNamespace | BestMultiplayer |
| Targets import | Steam-local `tMLMod.targets` (machine path)[^4] |

## Related

- [Build pipeline](build-pipeline.md)
- [Scaffold conventions](../concepts/scaffold-conventions.md)

[^1]: BestMultiplayer.cs
[^2]: build.txt
[^3]: description.txt
[^4]: BestMultiplayer.csproj

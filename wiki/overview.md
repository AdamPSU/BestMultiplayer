---
title: Overview
description: Repo wiki for the BestMultiplayer tModLoader 1.4.4.9 mod scaffold.
date: 2026-07-27
tags: [overview, wiki, best-multiplayer, tmodloader]
---

This wiki covers **only** the BestMultiplayer repository — a feature-neutral Terraria tModLoader mod scaffold aimed at multiplayer-safe feature work.

**Sources:** scaffold source files in repo root (README, entry class, csproj, build metadata, VS Code tasks).  
**Wiki pages:** overview, concepts, entities, log.

## Key Findings

- Scaffold only (v0.1): sealed empty `Mod` entry; no content, configs, systems, or custom netcode yet.
- Target stack: tModLoader **1.4.4.9**, `side = Both` (client + server).
- Build: VS Code task uses vendored `.dotnet/dotnet`; `.csproj` imports machine-local Steam `tMLMod.targets`.
- Conventions (documented, not implemented): `ClientSide` vs `ServerSide` configs; custom packets only when tModLoader sync is insufficient; standard `Common/` + `Content/` layout.

## Page index

| Area | Pages |
|---|---|
| Concepts | [Scaffold conventions](concepts/scaffold-conventions.md) · [Multiplayer config rules](concepts/multiplayer-config-rules.md) |
| Entities | [BestMultiplayer mod](entities/best-multiplayer-mod.md) · [Build pipeline](entities/build-pipeline.md) |

## Recent Updates

- 2026-07-27 — Initialized repo-local llmwiki; ingested scaffold sources into concepts/entities.

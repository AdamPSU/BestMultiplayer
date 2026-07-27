---
title: Overview
description: Repo wiki for the BestMultiplayer tModLoader multiplayer QoL mod.
date: 2026-07-27
tags: [overview, wiki, best-multiplayer, tmodloader]
---

This wiki covers **only** the BestMultiplayer repository — a Terraria tModLoader mod that aims to beat kittenchilly’s Better Multiplayer.

## Key Findings

- **Goal:** better multiplayer QoL than [Better Multiplayer](entities/better-multiplayer-baseline.md) (Workshop `2634682993`, [GitHub](https://github.com/kittenchilly/BetterMultiplayer)).
- **Implemented:** [TeamToJoin](concepts/team-to-join.md); [Unlimited team teleport](concepts/unlimited-team-teleport.md); [Boss fight lives](concepts/boss-fight-lives.md).
- **Scaffold / not built:** spectate.
- Baseline is tiny (v1.6.3 source): **exactly 3** ServerSide features — see [full inventory](entities/better-multiplayer-baseline.md).
- Architecture: [mod anatomy](concepts/mod-anatomy.md). Target: tModLoader **1.4.4.9**, `side = Both`.

## Page index

| Area | Pages |
|---|---|
| Concepts | [Mod anatomy](concepts/mod-anatomy.md) · [TeamToJoin](concepts/team-to-join.md) · [Unlimited team teleport](concepts/unlimited-team-teleport.md) · [Boss fight lives](concepts/boss-fight-lives.md) · [Scaffold conventions](concepts/scaffold-conventions.md) · [Multiplayer config rules](concepts/multiplayer-config-rules.md) |
| Entities | [BestMultiplayer mod](entities/best-multiplayer-mod.md) · [Better Multiplayer baseline](entities/better-multiplayer-baseline.md) · [Build pipeline](entities/build-pipeline.md) |

## Recent Updates

- 2026-07-27 — Boss fight lives: PerPlayer/PerTeam budget then BM hard lock; no UX v1.
- 2026-07-27 — UnlimitedTeamTeleport via `HasUnityPotion`/`TakeUnityPotion` hooks; optional boss block.
- 2026-07-27 — TeamToJoin join-once implemented (`BestMultiplayerPlayer.OnEnterWorld`).
- 2026-07-27 — Fleshed scaffold: dual configs, Common stubs, localization, anatomy docs.
- 2026-07-27 — Recorded Better Multiplayer baseline (Workshop + GitHub) as competitive target.
- 2026-07-27 — Initialized repo-local llmwiki.

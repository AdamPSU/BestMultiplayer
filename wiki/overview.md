---
title: Overview
description: Repo wiki for the BestMultiplayer tModLoader multiplayer QoL mod.
date: 2026-07-31
tags: [overview, wiki, best-multiplayer, tmodloader]
---

This wiki covers **only** the BestMultiplayer repository — a Terraria tModLoader mod that aims to beat kittenchilly’s Better Multiplayer.

## Key Findings

- **Goal:** better multiplayer QoL than [Better Multiplayer](entities/better-multiplayer-baseline.md) (Workshop `2634682993`, [GitHub](https://github.com/kittenchilly/BetterMultiplayer)).
- **Implemented:** [TeamToJoin](concepts/team-to-join.md); [Unlimited team teleport](concepts/unlimited-team-teleport.md); [Boss fight lives](concepts/boss-fight-lives.md) (+ teammate respawn); [team spectate](concepts/team-spectate.md); [shared team health](concepts/shared-boss-health.md); [boss fight stats feed](concepts/boss-fight-stats.md).
- **Roadmap:** [roadmap](concepts/roadmap.md) — 2026-07-27 planned set complete.
- **Collection:** [Shared World Map](concepts/collection-addons.md) as primary add-on; [catalog ack](concepts/catalog-ack.md) for the rest.
- Baseline is tiny (v1.6.3 source): **exactly 3** ServerSide features — see [full inventory](entities/better-multiplayer-baseline.md).
- Architecture: [mod anatomy](concepts/mod-anatomy.md). Target: tModLoader **1.4.4.9**, `side = Both`.

## Page index

| Area | Pages |
|---|---|
| Concepts | [Mod anatomy](concepts/mod-anatomy.md) · [TeamToJoin](concepts/team-to-join.md) · [Unlimited team teleport](concepts/unlimited-team-teleport.md) · [Boss fight lives](concepts/boss-fight-lives.md) · [Shared team health](concepts/shared-boss-health.md) · [Boss fight stats](concepts/boss-fight-stats.md) · [Team spectate](concepts/team-spectate.md) · [Scaffold conventions](concepts/scaffold-conventions.md) · [Multiplayer config rules](concepts/multiplayer-config-rules.md) |
| Planning | [Roadmap](concepts/roadmap.md) · [Collection add-ons](concepts/collection-addons.md) · [Catalog acknowledgements](concepts/catalog-ack.md) |
| Entities | [BestMultiplayer mod](entities/best-multiplayer-mod.md) · [Better Multiplayer baseline](entities/better-multiplayer-baseline.md) · [Build pipeline](entities/build-pipeline.md) |

## Recent Updates

- 2026-07-31 — Boss fight stats feed (layout M, left of Settings).
- 2026-07-27 — Catalog pass: roadmap + Shared World Map collection + acknowledgements (wiki only).
- 2026-07-27 — Boss fight lives: PerPlayer/PerTeam budget then BM hard lock; no UX v1.
- 2026-07-27 — UnlimitedTeamTeleport via `HasUnityPotion`/`TakeUnityPotion` hooks; optional boss block.
- 2026-07-27 — TeamToJoin join-once implemented (`BestMultiplayerPlayer.OnEnterWorld`).
- 2026-07-27 — Fleshed scaffold: dual configs, Common stubs, localization, anatomy docs.
- 2026-07-27 — Recorded Better Multiplayer baseline (Workshop + GitHub) as competitive target.
- 2026-07-27 — Initialized repo-local llmwiki.

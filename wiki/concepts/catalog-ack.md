---
title: Multiplayer catalog acknowledgements
description: Researched multiplayer mods/features not scheduled for DefinitiveMultiplayer implementation (2026-07-27 pass).
date: 2026-07-27
tags: [research, catalogue, acknowledgement, multiplayer]
---

Expansive catalog pass (Workshop, GitHub, guides, local subscriptions). **Acknowledgement only** — no implement commitment unless moved to [roadmap](roadmap.md) or [collection add-ons](collection-addons.md).

## Already covered by DefinitiveMultiplayer

| Topic | Notes |
|---|---|
| Team auto-join | [TeamToJoin](team-to-join.md) join-once (not BM soft-lock) |
| Unlimited team wormhole | [Unlimited team teleport](unlimited-team-teleport.md) |
| Boss lives budget | [Boss fight lives](boss-fight-lives.md) PerPlayer/PerTeam |
| Death team spectate + death UI | [Team spectate](team-spectate.md) |

## Scheduled (see roadmap)

Respawn-at-teammate (boss deaths only); boss spectate targets; camera lerp; damage board; instant respawn on boss end; Shared HP/DeathLink mode. Details: [roadmap](roadmap.md).

## Collection (committed add-on)

| Mod | ID | Notes |
|---|---|---|
| Shared World Map | `2815010161` | [Collection add-ons](collection-addons.md) |

## Acknowledged multiplayer-tailored mods (not in collection yet)

| Mod | Workshop / source | Role | Why not BM / not collection (this pass) |
|---|---|---|---|
| Team Spectate | `2563098343` | Alive + death spectate, boss list UI | Partial overlap; we death-spectate in-mod |
| Multiplayer Boss Spectator | `2822925665` | Dead boss/player cam, freeze timer | Ideas stolen into roadmap; full mod optional |
| Multiplayer Boss-Fight Stats | `2822937879` | Chat DPS/damage board | Roadmap damage board may supersede |
| Boss Fight Lives / MP Boss Player Lives | various | Per-player lives | Superseded by our lives system |
| Better Multiplayer | `2634682993` | Baseline 3 features | Competitive target |
| Dtboss' no friendly projectiles | `3231435221` | Hide other friendly proj/dust | Keep external; Calamity IL not in-scope |
| Legible Bossfights | `3606734513` | Boss readability | Visual; external |
| Party UI | `3520267377` | Party HP/mana/potion/respawn frames | Strong candidate later; not this pass |
| Team Info (yonkoma) | GitHub | Teammate HP bars | Ancestor of Party UI ideas |
| EnhancedMultiPlayer | GitHub (WIP) | Ally HP; planned spectate/team/boss lock | Validates demand; incomplete |
| Ghost Respawn | Workshop packs | Ghost move + respawn near player | Roadmap takes boss-only teammate respawn slice |
| Improved Respawning | `3098184209` | Full respawn sandbox + hardcore | Too broad; thin ideas on roadmap only |
| Auto Team Join | `2826802614` | Client team join | Redundant with TeamToJoin |
| No Wormholes Required / Always Have Wormhole / Portable Wormhole / Quick Wormhole keybind | various | Team TP without potions | Overlap unlimited TP; keybind idea deferred |
| Map Markers | `2737693253` | Player/world map markers | Nice with Shared World Map later |
| Persistent Map Markers | `3402848154` | Named markers + optional TP | Later collection candidate |
| InfraSonic | `3148310222` | In-game proximity voice | Wrong stack to reimplement |
| Player Interaction | `3451346666` | View others’ inventories | Privacy-heavy; optional later |
| Suffer Together / Shared Health | various | Shared HP / DeathLink | Roadmap as **optional mode** only |
| Kepples Item Lockout | `3484615436` | Team item bingo mode | Separate game mode |
| Fusion | `3545714024` | Two-player fusion | Novelty |
| High FPS Support | subscribed | Client FPS | Perf, not MP design |
| Wormhole To Grave | `3668162827` | TP to own death marker | Solo-leaning QoL |

## Acknowledged feature ideas (not scheduled)

| Idea | Notes |
|---|---|
| Lives remaining HUD | Natural polish; not selected this pass |
| Party frames in-mod | Prefer Party UI external for now |
| Team soft-lock (BM parity) | Optional later |
| Quick wormhole keybind | Deferred |
| Freeze respawn timer while hard-locked | Related to instant-on-boss-death |
| Alive spectate | Out of death-only v1 scope unless revisited |
| Teammate compass / offscreen arrows | Later |
| Team death markers on map | Later |
| Vanilla-only hide ally projectiles | Prefer Dtboss full mod |
| Persistent minions across death | Cheese risk |
| Thorium healer / revive hooks | Compatibility layer, high cost |
| Mid-join PerTeam pool top-up | Explicitly not wanted (current design) |
| Full host “co-op panel” UI | Meta; later |

## Explicitly out of MP collection (general QoL / content)

Magic Storage, Recipe Browser, Boss Checklist, Census, Ore Excavator, Shop Expander, AlchemistNPC, Fargo’s, Calamity/Thorium/content packs, cheat mods — fine in a playthrough pack, **not** DefinitiveMultiplayer’s multiplayer-purpose collection.

## Research sources (non-exhaustive)

Workshop feature pages; NotLe0n Team Spectate + tMLDB; kittenchilly Better Multiplayer; Seedonator Boss Spectator / Boss-Fight Stats; Shared World Map (`2815010161`); Improved Respawning; Party UI; Dtboss `3231435221` decompile notes; Vortex multiplayer QoL list; local Workshop subscriptions (42 mods as of pass).

## Related

- [Roadmap](roadmap.md)  
- [Collection add-ons](collection-addons.md)  
- [Better Multiplayer baseline](../entities/better-multiplayer-baseline.md)  

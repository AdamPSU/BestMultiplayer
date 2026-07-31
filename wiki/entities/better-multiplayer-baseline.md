---
title: Better Multiplayer (baseline)
description: Complete feature inventory of kittenchilly/BetterMultiplayer from source — single reference so we need not re-lookup later.
date: 2026-07-27
tags: [baseline, competitor, multiplayer, tmodloader, reference, feature-list]
---

**Better Multiplayer** (`BetterMultiplayer`) by **kittenchilly** is the multiplayer QoL mod this project aims to beat.

Inventoried from source clone of [kittenchilly/BetterMultiplayer](https://github.com/kittenchilly/BetterMultiplayer) (`main`, version **1.6.3** in `build.txt`). Last known Workshop update aligned with 2024-08-29 push. Source surface is small: 4 C# files + config localization.[^1]

## Links

| | |
|---|---|
| Steam Workshop | https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993 |
| Source | https://github.com/kittenchilly/BetterMultiplayer |
| Homepage (build.txt) | same GitHub URL |
| Author | kittenchilly |
| Version (build.txt) | 1.6.3 |
| Config class | `BMConfig` — **`ConfigScope.ServerSide` only** |

## Complete feature list (exhaustive)

There are **exactly three** configurable behaviors. No other gameplay systems exist in source. Empty `Mod` entry class; logic lives in `BMPlayer` + `BMGlobalNPC`.

### 1. Auto team join — config: `TeamToJoin`

| | |
|---|---|
| Type | `string` option list |
| Options | `None`, `Red`, `Green`, `Blue`, `Yellow`, `Pink` |
| Default | `"Red"` |
| UI label | “Team To Join” |
| Off switch | choose **`None`** (no separate bool) |

**Behavior**

- On `OnEnterWorld`, maps the string to vanilla team id: None→0, Red→1, Green→2, Blue→3, Yellow→4, Pink→5.
- Uses `CopyClientState` / `SendClientChanges` to force `Player.team` and send `MessageID.PlayerTeam` when local team differs from the chosen team.
- Effect: players are pushed onto the configured team (unless `None`).

**Not present:** per-player team choice, client-side team config, or “lock team so players cannot leave.”

### 2. Disable respawning during boss fights — config: `NoBossFightRespawn`

| | |
|---|---|
| Type | `bool` |
| Default | `true` |
| UI label | “Disable Respawning during Boss Fights” |

**Behavior** (`BMPlayer.UpdateDead`)

- When enabled, while the player is dead, if **any** active NPC with a valid target is:
  - `npc.boss == true`, **or**
  - Eater of Worlds segment (`EaterofWorldsHead` / `Body` / `Tail`),
- then continuously **reset** `Player.respawnTimer` so it never reaches zero:
  - Classic: `1200` ticks (20s)
  - Expert: `1800` ticks (30s)
  - For the Worthy (`Main.getGoodWorld`): `3600` ticks (60s)

**Implications**

- Respawn is blocked for the whole fight, not “one life then out forever after wipe.”
- When no matching boss/EoW is active, the hook stops resetting; vanilla respawn timer can finish (so after full wipe or boss death, players can respawn).
- Does **not** implement camera lock / boss spectate (older marketing copy mentioned that; **not in current source**).
- Does **not** implement life banks, N-respawn caps, custom timer lengths, or modded-revive (Thorium etc.) compatibility hooks.
- Boss detection is vanilla `boss` flag + hard-coded EoW segments only — modded bosses that omit `boss` may not count unless they set that flag.

### 3. Witch Doctor sells Wormhole Potions — config: `WitchDoctorWormhole`

| | |
|---|---|
| Type | `bool` |
| Default | `true` |
| UI label | “Witch Doctor sells Wormhole Potions” |

**Behavior** (`BMGlobalNPC.ModifyShop`)

- When enabled and shop NPC is `NPCID.WitchDoctor`, adds `ItemID.WormholePotion` to the shop.
- No custom price/condition beyond default shop add.

## Config / multiplayer rules (how features are governed)

| Rule | Implementation |
|---|---|
| Scope | Entire config is **ServerSide** (`BMConfig.Mode`) |
| Who may change | `AcceptClientChanges` allows only host (`NetMessage.DoesPlayerSlotCountAsAHost`); others get `tModLoader.ModConfigRejectChangesNotHost` |
| Client-only settings | **None** |
| Custom packets | **None** (team uses vanilla `PlayerTeam` message) |
| Localization | `en-US_Mods.BetterMultiplayer.hjson` — labels only; tooltips empty strings |

## Explicit non-features (confirmed absent in source)

Do not assume these ship in Better Multiplayer **1.6.3**:

- Boss death camera / fixate spectate
- Built-in spectator mode (author recommends separate **Team Spectate** mod)
- Reaver Shark / pickaxe nerfs (appeared in older mod-browser blurbs only)
- Respawn life bank / N lives per boss
- Custom respawn duration config
- PvP defense ratio config
- Thorium/other revive integration
- Inventory/health ally UI, map pings, etc.
- Client-side config surface
- Any content (items, NPCs, tiles) beyond shop injection

## Source file map

| File | Role |
|---|---|
| `BetterMultiplayer.cs` | Empty `Mod` subclass |
| `BMConfig.cs` | ServerSide config + host-only accept |
| `BMPlayer.cs` | Team force + boss respawn lock |
| `BMGlobalNPC.cs` | Witch Doctor wormhole shop line |
| `Localization/en-US_Mods.BetterMultiplayer.hjson` | Config labels |
| `build.txt` | displayName, author, version 1.6.3, homepage |
| `description.txt` | Workshop feature marketing copy (3 features) |
| `workshop.json` | Workshop packaging metadata |

## Workshop marketing vs source

Workshop/`description.txt` claims match the three implemented features. Extra historical blurbs (spectate camera, Reaver Shark) are **not** in current GitHub `main` and must not be treated as baseline requirements unless re-verified on a tagged release.

## Community requests (Workshop comments only — not shipped)

Useful product gaps; **not** Better Multiplayer features:

- Per-player respawn caps (e.g. 2 lives per boss)
- Longer/custom respawn timers instead of hard lock
- Thorium Revivify / healer revive compatibility
- Configurable PvP defense effectiveness
- Respawn countdown ticking while boss still alive (prep after death)

## Related

- [The Definitive Multiplayer Mod](definitive-multiplayer-mod.md)
- [Multiplayer config rules](../concepts/multiplayer-config-rules.md)

[^1]: Local inventory from `https://github.com/kittenchilly/BetterMultiplayer` clone; files `BMConfig.cs`, `BMPlayer.cs`, `BMGlobalNPC.cs`, `BetterMultiplayer.cs`, `build.txt`, `description.txt`, `Localization/en-US_Mods.BetterMultiplayer.hjson`.

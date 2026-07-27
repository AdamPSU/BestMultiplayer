Chronological record of ingests, queries, and maintenance passes.

## [2026-07-27] feature | Boss fight lives
- Replaced `NoBossFightRespawn` ban with configurable lives: `Off` / `PerPlayer` / `PerTeam`
- Defaults: PerPlayer, 1 respawn, AutoTeamSize on (PerTeam pool = team size at fight start)
- Spend on `dead` rising edge; `RespawnAllowedThisDeath` so last spend does not lock same death
- Hard lock via BM `respawnTimer` clamp (1200/1800/3600); no HUD/death messages v1
- Init when boss active && pools missing (mid-join safe); PerTeam no mid-join top-up
- Created: [Boss fight lives](concepts/boss-fight-lives.md)
- Updated: ServerConfig, loc, README, overview, multiplayer config rules, mod anatomy, mod entity

## [2026-07-27] feature | Free team wormhole
- Replaced BM-style Witch Doctor shop config with virtual infinite wormhole
- `On_Player.HasUnityPotion` / `TakeUnityPotion`: fake possession + skip consume
- Config: `FreeTeamWormhole` (default true), `BlockFreeWormholeDuringBoss` (default false)
- `BossFightSystem.IsBossFightActive` — boss flag + EoW segments (shared later with respawn lock)
- Real potions kept; not deleted from game
- Created: [Free team wormhole](concepts/free-team-wormhole.md)
- Updated: ServerConfig, loc, README, overview, multiplayer config rules, mod anatomy, mod entity

## [2026-07-27] feature | TeamToJoin join-once
- Behavior: on `OnEnterWorld`, if `ServerConfig.TeamToJoin` is a color, set `Player.team` once and `MessageID.PlayerTeam` when not SP
- Not BM soft-lock: no `CopyClientState` / `SendClientChanges`; players may change teams afterward
- `None` skips; mid-session config applies on next enter only; default remains `Red`
- Code: `Common/Players/BestMultiplayerPlayer.cs`
- Created: [TeamToJoin](concepts/team-to-join.md)
- Updated: overview, multiplayer config rules, mod anatomy, mod entity, README, localization tooltip

## [2026-07-27] scaffold | Dual-config Common layout
- Added `Common/Configs/ServerConfig.cs` (TeamToJoin, NoBossFightRespawn, WitchDoctorWormhole; host-only AcceptClientChanges)
- Added `Common/Configs/ClientConfig.cs` (SpectateOnDeath, StopSpectateOnRespawn)
- Added stubs: `BestMultiplayerPlayer`, `BossFightSystem`, `ShopGlobalNPC`
- Added `Localization/en-US_Mods.BestMultiplayer.hjson`
- Created: [Mod anatomy](concepts/mod-anatomy.md)
- Updated: overview, scaffold conventions, multiplayer config rules, mod entity, README, description
- Key takeaway: Config surface ready; gameplay/spectate logic still TODO

## [2026-07-27] ingest | Better Multiplayer full feature inventory
- Cloned kittenchilly/BetterMultiplayer (v1.6.3); read all C# + config loc
- Exhaustive list: exactly 3 ServerSide features (TeamToJoin, NoBossFightRespawn, WitchDoctorWormhole)
- Documented defaults, team id map, boss detection (boss flag + EoW), respawnTimer reset values, host-only AcceptClientChanges
- Recorded explicit non-features (spectate, Reaver Shark, life bank, packets, etc.)
- Updated: [Better Multiplayer baseline](entities/better-multiplayer-baseline.md)

## [2026-07-27] ingest | Better Multiplayer baseline
- Goal set: beat kittenchilly Better Multiplayer
- Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993
- Source: https://github.com/kittenchilly/BetterMultiplayer
- Created: [Better Multiplayer baseline](entities/better-multiplayer-baseline.md)
- Updated: [Overview](overview.md), [BestMultiplayer mod](entities/best-multiplayer-mod.md), README
- Key takeaway: Public GitHub exists; baseline = auto team, boss no-respawn, Witch Doctor wormholes (host toggles)

## [2026-07-27] ingest | BestMultiplayer scaffold init
- Initialized llmwiki at repo root (not parent `/Users/adam/dev`)
- Created: [Scaffold conventions](concepts/scaffold-conventions.md), [Multiplayer config rules](concepts/multiplayer-config-rules.md)
- Created: [BestMultiplayer mod](entities/best-multiplayer-mod.md), [Build pipeline](entities/build-pipeline.md)
- Updated: [Overview](overview.md)
- Key takeaway: Empty tModLoader 1.4.4.9 scaffold with multiplayer-oriented conventions documented in README; no features yet.

Chronological record of ingests, queries, and maintenance passes.

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

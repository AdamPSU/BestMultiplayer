Chronological record of ingests, queries, and maintenance passes.

## [2026-07-31] rebrand | The Definitive Multiplayer Mod

- Internal slug: `DefinitiveMultiplayer` (was `BestMultiplayer`)
- Display name: **The Definitive Multiplayer Mod**
- Official `icon.png` (80×80) + `icon_small.png` (30×30)
- Namespace, localization, player class, packaging, wiki entity renamed

## [2026-07-31] feature | Boss fight stats feed
- Layout M: head strip (dim non-selected) + selected dealt%/taken%/deaths
- Left of Settings; boss-only; 5s freeze on end; click head/cycle
- Server `BossFightStatsEnabled` + client `ShowBossFightStats` (both default on)
- `FightStatsSystem` + `FightStatsPlayer` + snapshot packet; `FightStatsFeedState` UI
- Roadmap damage board → done

## [2026-07-30] feature | Shared Team Health (redesign)
- Master `SharedHealthEnabled` + `SharedHealthBossesOnly` + pool size 50–150%
- Always-on by default when enabled; bosses-only optional; lives mode untouched
- Join expands pool; boss wipe hard-locks; outside boss = normal respawn + re-arm
- Config UI collapses sub-options when master off

## [2026-07-29] feature | Shared Boss Health
- Initial boss-only shared pool (superseded by 2026-07-30 redesign)

## [2026-07-27] feature | Instant respawn when boss ends
- `ServerConfig.InstantRespawnOnBossEnd` default **true**
- On active→inactive fight edge: `respawnTimer = 0` for all dead players (waiting + hard-locked)
- Runs in `BossFightSystem.PostUpdatePlayers` after hard-lock `UpdateDead`

## [2026-07-27] feature | Boss spectate + smooth camera
- Death-only unified ring: teammates then bosses; grid boss heads
- Smoothstep lerp (~0.3s) on target switch; preferred respawn player-only
- Clean-room of Team Spectate / Multiplayer Boss Spectator ideas

## [2026-07-27] chore | In-place cleanup
- Removed empty `ShopGlobalNPC` + unused `StopSpectateOnRespawn`
- Merged spectate grid host into `DeathScreenSystem` (dropped `SpectateUISystem`)
- Centralized packet ids (`Common/Packets.cs`); shared `IsLivingTeammate`
- README / anatomy / config docs refreshed

## [2026-07-27] feature | Respawn at spectate target (boss deaths)
- `ServerConfig.RespawnAtTeammateDuringBoss` default **true**
- Death during boss → flag; while dead preferred whoAmI tracks spectate target (MP packet)
- On respawn (server/SP): teleport beside preferred if still valid; else vanilla spawn — **no nearest**
- Docs: boss-fight-lives, roadmap

## [2026-07-27] research | Multiplayer catalog → roadmap + collection
- Expansive Workshop/GitHub pass for MP-only features and add-ons
- **Roadmap (in-mod, not coded yet):** boss-death respawn-at-teammate; boss spectate targets; smooth camera lerp; mid/end damage board; instant respawn when boss dies; Shared HP/DeathLink as optional mode
- **Collection add-on:** Shared World Map (`2815010161`)
- **Ack only:** Team Spectate, Boss Spectator, Boss-Fight Stats, Dtboss projectiles, Party UI, InfraSonic, Improved Respawning, map markers, etc. — see catalog-ack
- Wiki only (no code): [roadmap](concepts/roadmap.md), [collection-addons](concepts/collection-addons.md), [catalog-ack](concepts/catalog-ack.md)

## [2026-07-27] fix | Boss fight active detection
- Removed `HasValidTarget` from `IsBossFightActive` — dead players left boss with no target, pools cleared, PerTeam lives reset (infinite respawn)

## [2026-07-27] ui | Spectate head grid
- Dead multiplayer: top-right teammate head grid (TS-inspired, vanilla DrawPlayerHead, no custom assets)
- Click to spectate / click self-or-current to stop

## [2026-07-27] fix | Spectate lifecycle + slim layout
- Intro/auto-target now ticks in `UpdateDead` (PostUpdate skipped while dead — stuck timer / no camera)
- Collapse to SpectatePlayer (+ nested keybinds) + DeathScreenSystem; deleted SpectateSession / KeybindSystem
- Smaller death subtitle + bottom digits (scale 0.45); TS-style camera math; section packet from UpdateDead

## [2026-07-27] feature | Team spectate + custom death UI
- All deaths: hide vanilla death text; 3s intro then auto-spectate next living teammate by slot
- Intro copy: slain vs “No lives left…” (hard lock); “spectating in N”
- Spectate: clear chrome; bottom digits-only respawn timer (none when hard-locked)
- Hotkeys next/prev/stop while dead; section packet for far chunks
- Inspired by Team Spectate (NotLe0n), clean-room port
- Created: [Team spectate](concepts/team-spectate.md)

## [2026-07-27] ux | Locked death screen
- While hard-locked out of boss lives, hide `Vanilla: Death Text` and draw “No lives left...”
- No coins / countdown; clamp gameplay unchanged
- `DeathScreenSystem` + `BossFightSystem.IsLocalHardLocked`

## [2026-07-27] feature | Boss fight lives
- Replaced `NoBossFightRespawn` ban with configurable lives: `Off` / `PerPlayer` / `PerTeam`
- Defaults: PerPlayer, 1 respawn, AutoTeamSize on (PerTeam pool = team size at fight start)
- Spend on `dead` rising edge; `RespawnAllowedThisDeath` so last spend does not lock same death
- Hard lock via BM `respawnTimer` clamp (1200/1800/3600); no HUD/death messages v1
- Init when boss active && pools missing (mid-join safe); PerTeam no mid-join top-up
- Created: [Boss fight lives](concepts/boss-fight-lives.md)
- Updated: ServerConfig, loc, README, overview, multiplayer config rules, mod anatomy, mod entity

## [2026-07-27] simplify | PerTeam always uses team size
- Removed `BossFightLivesAutoTeamSize` toggle
- PerTeam pool = players on team at fight start; unteamed solo = 1
- `BossFightRespawns` is PerPlayer-only

## [2026-07-27] rename | Unlimited team teleport
- Renamed FreeTeamWormhole → UnlimitedTeamTeleport (config + loc + docs)
- BlockFreeWormholeDuringBoss → BlockUnlimitedTeleportDuringBoss
- Wiki: free-team-wormhole.md → unlimited-team-teleport.md

## [2026-07-27] feature | Unlimited team teleport (was Free team wormhole)
- Replaced BM-style Witch Doctor shop config with virtual infinite wormhole
- `On_Player.HasUnityPotion` / `TakeUnityPotion`: fake possession + skip consume
- Config: `UnlimitedTeamTeleport` (default true), `BlockUnlimitedTeleportDuringBoss` (default false)
- `BossFightSystem.IsBossFightActive` — boss flag + EoW segments (shared with respawn lock)
- Real potions kept; not deleted from game
- Created: [Unlimited team teleport](concepts/unlimited-team-teleport.md)
- Updated: ServerConfig, loc, README, overview, multiplayer config rules, mod anatomy, mod entity

## [2026-07-27] feature | TeamToJoin join-once
- Behavior: on `OnEnterWorld`, if `ServerConfig.TeamToJoin` is a color, set `Player.team` once and `MessageID.PlayerTeam` when not SP
- Not BM soft-lock: no `CopyClientState` / `SendClientChanges`; players may change teams afterward
- `None` skips; mid-session config applies on next enter only; default remains `Red`
- Code: `Common/Players/DefinitiveMultiplayerPlayer.cs`
- Created: [TeamToJoin](concepts/team-to-join.md)
- Updated: overview, multiplayer config rules, mod anatomy, mod entity, README, localization tooltip

## [2026-07-27] scaffold | Dual-config Common layout
- Added `Common/Configs/ServerConfig.cs` (TeamToJoin, NoBossFightRespawn, WitchDoctorWormhole; host-only AcceptClientChanges)
- Added `Common/Configs/ClientConfig.cs` (SpectateOnDeath, StopSpectateOnRespawn)
- Added stubs: `DefinitiveMultiplayerPlayer`, `BossFightSystem`, `ShopGlobalNPC`
- Added `Localization/en-US_Mods.DefinitiveMultiplayer.hjson`
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
- Updated: [Overview](overview.md), [The Definitive Multiplayer Mod](entities/definitive-multiplayer-mod.md), README
- Key takeaway: Public GitHub exists; baseline = auto team, boss no-respawn, Witch Doctor wormholes (host toggles)

## [2026-07-27] ingest | DefinitiveMultiplayer scaffold init
- Initialized llmwiki at repo root (not parent `/Users/adam/dev`)
- Created: [Scaffold conventions](concepts/scaffold-conventions.md), [Multiplayer config rules](concepts/multiplayer-config-rules.md)
- Created: [The Definitive Multiplayer Mod](entities/definitive-multiplayer-mod.md), [Build pipeline](entities/build-pipeline.md)
- Updated: [Overview](overview.md)
- Key takeaway: Empty tModLoader 1.4.4.9 scaffold with multiplayer-oriented conventions documented in README; no features yet.

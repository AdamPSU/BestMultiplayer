using System;
using System.Collections.Generic;
using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Boss-fight detection and respawn-budget pools for wormhole / lives rules.
/// Player and team life limits are independent; when both are on, a death must clear both.
/// </summary>
public sealed class BossFightSystem : ModSystem
{
	private static readonly DeathEdgeTracker DeathTracker = new();
	private static readonly Dictionary<int, int> TeamRespawnsLeft = new();
	private static bool _poolsReady;
	private static bool _fightWasActive;

	// Several systems poll IsBossFightActive multiple times per tick; cache the O(maxNPCs)
	// scan result for the current tick instead of rescanning on every call.
	private static long _activeCacheTick = -1;
	private static bool _activeCache;

	public static bool IsBossFightActive()
	{
		if (Main.GameUpdateCount == _activeCacheTick)
			return _activeCache;

		_activeCacheTick = Main.GameUpdateCount;

		// Do NOT require HasValidTarget: when everyone is dead the boss often has no target,
		// which would clear lives pools and allow infinite respawns.
		_activeCache = false;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && BossNpc.IsAnySegment(npc))
			{
				_activeCache = true;
				break;
			}
		}

		return _activeCache;
	}

	public override void PostUpdatePlayers()
	{
		bool active = IsBossFightActive();
		if (!active)
		{
			if (_poolsReady)
				ClearPools();
			if (_fightWasActive)
				OnBossFightEnded();
			_fightWasActive = false;
			DeathTracker.Snapshot();
			return;
		}

		_fightWasActive = true;

		if (!_poolsReady)
			InitPools();
		else
			EnsureNewPlayers();

		DeathTracker.ForEachNewDeath(OnPlayerDied);
	}

	public override void ClearWorld()
	{
		ClearPools();
		_fightWasActive = false;
		DeathTracker.Reset();
	}

	/// <summary>
	/// Fight just ended: release dead players immediately.
	/// Runs after hard-lock UpdateDead for this frame (PostUpdatePlayers).
	/// </summary>
	private static void OnBossFightEnded()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active && p.dead)
				p.respawnTimer = 0;
		}
	}

	internal static bool IsLivesModeActive()
	{
		ServerConfig cfg = ServerConfig.Instance;
		return cfg.BossFightPlayerLivesEnabled || cfg.BossFightTeamLivesEnabled;
	}

	private static void InitPools()
	{
		TeamRespawnsLeft.Clear();
		ServerConfig cfg = ServerConfig.Instance;
		if (!IsLivesModeActive())
		{
			_poolsReady = true;
			DeathTracker.Snapshot();
			return;
		}

		if (cfg.BossFightTeamLivesEnabled)
			SeedTeamPools();

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active)
				AssignPlayer(p, cfg);
		}

		_poolsReady = true;
		DeathTracker.Snapshot();
	}

	private static void SeedTeamPools()
	{
		int[] counts = new int[Teams.Max + 1];
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active)
				counts[p.team]++;
		}

		// 0 = team size at fight start; >0 = fixed shared pool.
		int fixedPool = ServerConfig.Instance.BossFightTeamLives;
		for (int team = Teams.Min; team <= Teams.Max; team++)
		{
			if (counts[team] > 0)
				TeamRespawnsLeft[team] = fixedPool > 0 ? fixedPool : counts[team];
		}
	}

	private static void EnsureNewPlayers()
	{
		ServerConfig cfg = ServerConfig.Instance;
		if (!IsLivesModeActive())
			return;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active)
				continue;

			DefinitiveMultiplayerPlayer mp = p.GetModPlayer<DefinitiveMultiplayerPlayer>();
			if (mp.LivesInitialized)
				continue;

			// Mid-join: full personal budget; team pool is not topped up.
			AssignPlayer(p, cfg);
			if (cfg.BossFightTeamLivesEnabled && Teams.IsReal(p.team)
			    && !TeamRespawnsLeft.ContainsKey(p.team))
				TeamRespawnsLeft[p.team] = 0;

			DeathTracker.Seed(i, p.dead);
		}
	}

	/// <summary>Personal field holds player lives, or a solo "team" budget when team-only + unteamed.</summary>
	private static bool UsesPersonalBudget(ServerConfig cfg, bool realTeam) =>
		cfg.BossFightPlayerLivesEnabled || (cfg.BossFightTeamLivesEnabled && !realTeam);

	private static void AssignPlayer(Player player, ServerConfig cfg)
	{
		DefinitiveMultiplayerPlayer mp = player.GetModPlayer<DefinitiveMultiplayerPlayer>();
		mp.LivesInitialized = true;
		// Already dead at fight start / join: finish vanilla respawn without spending.
		mp.RespawnAllowedThisDeath = player.dead;

		bool realTeam = Teams.IsReal(player.team);
		if (cfg.BossFightPlayerLivesEnabled)
		{
			// Config is total lives; runtime budget is respawns (lives − 1).
			mp.RespawnsRemaining = Math.Max(0, cfg.BossFightLives - 1);
			return;
		}

		if (UsesPersonalBudget(cfg, realTeam))
		{
			int teamLives = cfg.BossFightTeamLives;
			mp.RespawnsRemaining = teamLives > 0 ? teamLives : 1;
			return;
		}

		mp.RespawnsRemaining = 0;
	}

	private static void ClearPools()
	{
		TeamRespawnsLeft.Clear();
		_poolsReady = false;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active)
				continue;

			DefinitiveMultiplayerPlayer mp = p.GetModPlayer<DefinitiveMultiplayerPlayer>();
			mp.RespawnsRemaining = 0;
			mp.RespawnAllowedThisDeath = false;
			mp.LivesInitialized = false;
			mp.BossDeathsThisFight = 0;
		}
	}

	private static void OnPlayerDied(Player player)
	{
		if (!IsLivesModeActive())
			return;

		// Shared-health wipe deaths do not spend boss lives and stay hard-locked.
		if (player.active && Teams.IsReal(player.team) && SharedHealthSystem.IsTeamWiped(player.team))
		{
			player.GetModPlayer<DefinitiveMultiplayerPlayer>().RespawnAllowedThisDeath = false;
			return;
		}

		DefinitiveMultiplayerPlayer mp = player.GetModPlayer<DefinitiveMultiplayerPlayer>();
		mp.RespawnAllowedThisDeath = false;

		ServerConfig cfg = ServerConfig.Instance;
		bool realTeam = Teams.IsReal(player.team);
		bool usePersonal = UsesPersonalBudget(cfg, realTeam);
		bool useTeam = cfg.BossFightTeamLivesEnabled && realTeam;

		if (usePersonal && mp.RespawnsRemaining <= 0)
			return;

		int teamLeft = 0;
		if (useTeam)
		{
			if (!TeamRespawnsLeft.TryGetValue(player.team, out teamLeft) || teamLeft <= 0)
				return;
		}

		if (usePersonal)
			mp.RespawnsRemaining--;

		if (useTeam)
			TeamRespawnsLeft[player.team] = teamLeft - 1;

		mp.RespawnAllowedThisDeath = true;

		// Kill may have left a hard-lock timer while allow was still false — apply policy now.
		player.respawnTimer = RespawnPolicy.ComputeTimerTicks(mp.PriorBossDeathsForTimer);
	}
}

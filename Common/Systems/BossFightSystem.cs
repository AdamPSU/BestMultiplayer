using System;
using System.Collections.Generic;
using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Boss-fight detection and respawn-budget pools for wormhole / lives rules.
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
	/// Fight just ended: release dead players immediately when config allows.
	/// Runs after hard-lock UpdateDead for this frame (PostUpdatePlayers).
	/// </summary>
	private static void OnBossFightEnded()
	{
		// Always instant-respawn when a boss fight ends (no longer a config toggle).
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active && p.dead)
				p.respawnTimer = 0;
		}
	}

	internal static bool IsLivesModeActive() =>
		ServerConfig.Instance.BossFightLivesMode is BossFightLivesMode.PerPlayer or BossFightLivesMode.PerTeam;

	private static void InitPools()
	{
		TeamRespawnsLeft.Clear();
		ServerConfig config = ServerConfig.Instance;
		if (!IsLivesModeActive())
		{
			_poolsReady = true;
			DeathTracker.Snapshot();
			return;
		}

		if (config.BossFightLivesMode == BossFightLivesMode.PerTeam)
			SeedTeamPools();

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active)
				AssignPlayer(p, config);
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
		ServerConfig config = ServerConfig.Instance;
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

			// Mid-join: PerPlayer full budget; PerTeam does not top up shared pool.
			AssignPlayer(p, config);
			if (config.BossFightLivesMode == BossFightLivesMode.PerTeam && Teams.IsReal(p.team)
			    && !TeamRespawnsLeft.ContainsKey(p.team))
				TeamRespawnsLeft[p.team] = 0;

			DeathTracker.Seed(i, p.dead);
		}
	}

	private static void AssignPlayer(Player player, ServerConfig config)
	{
		DefinitiveMultiplayerPlayer mp = player.GetModPlayer<DefinitiveMultiplayerPlayer>();
		mp.LivesInitialized = true;
		// Already dead at fight start / join: finish vanilla respawn without spending.
		mp.RespawnAllowedThisDeath = player.dead;

		if (config.BossFightLivesMode == BossFightLivesMode.PerPlayer)
		{
			// Config is total lives; runtime budget is respawns (lives − 1).
			mp.RespawnsRemaining = Math.Max(0, config.BossFightLives - 1);
			return;
		}

		// PerTeam: teams 1–5 use shared map; unteamed uses personal pool (fixed or 1).
		int teamLives = config.BossFightTeamLives;
		mp.RespawnsRemaining = player.team == 0 ? (teamLives > 0 ? teamLives : 1) : 0;
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
		}
	}

	private static void OnPlayerDied(Player player)
	{
		if (!IsLivesModeActive())
			return;

		// Shared-health wipe deaths do not spend boss lives and stay hard-locked.
		if (SharedHealthSystem.SuppressLivesSpend(player))
		{
			player.GetModPlayer<DefinitiveMultiplayerPlayer>().RespawnAllowedThisDeath = false;
			return;
		}

		DefinitiveMultiplayerPlayer mp = player.GetModPlayer<DefinitiveMultiplayerPlayer>();
		mp.RespawnAllowedThisDeath = false;

		ServerConfig config = ServerConfig.Instance;
		if (config.BossFightLivesMode == BossFightLivesMode.PerPlayer || player.team == 0)
		{
			if (mp.RespawnsRemaining > 0)
			{
				mp.RespawnsRemaining--;
				mp.RespawnAllowedThisDeath = true;
			}

			return;
		}

		if (TeamRespawnsLeft.TryGetValue(player.team, out int left) && left > 0)
		{
			TeamRespawnsLeft[player.team] = left - 1;
			mp.RespawnAllowedThisDeath = true;
		}
	}
}

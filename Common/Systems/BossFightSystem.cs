using System.Collections.Generic;
using BestMultiplayer.Common.Configs;
using BestMultiplayer.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Boss-fight detection and respawn-budget pools for wormhole / lives rules.
/// </summary>
public sealed class BossFightSystem : ModSystem
{
	private static readonly DeathEdgeTracker DeathTracker = new();
	private static readonly Dictionary<int, int> TeamRespawnsLeft = new();
	private static bool _poolsReady;
	private static bool _fightWasActive;

	public static bool IsBossFightActive()
	{
		// Do NOT require HasValidTarget: when everyone is dead the boss often has no target,
		// which would clear lives pools and allow infinite respawns.
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && BossNpc.IsAnySegment(npc))
				return true;
		}

		return false;
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

	internal static bool IsLivesModeActive()
	{
		ServerConfig config = ServerConfig.Instance;
		return config is not null && config.BossFightLivesMode is BossFightLivesMode.PerPlayer or BossFightLivesMode.PerTeam;
	}

	private static void InitPools()
	{
		TeamRespawnsLeft.Clear();
		ServerConfig config = ServerConfig.Instance;
		if (config is null || !IsLivesModeActive())
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
		int[] counts = new int[6];
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active)
				counts[p.team]++;
		}

		// Shared pool = players on that team at fight start.
		for (int team = 1; team <= 5; team++)
		{
			if (counts[team] > 0)
				TeamRespawnsLeft[team] = counts[team];
		}
	}

	private static void EnsureNewPlayers()
	{
		ServerConfig config = ServerConfig.Instance;
		if (config is null || !IsLivesModeActive())
			return;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active)
				continue;

			BestMultiplayerPlayer mp = p.GetModPlayer<BestMultiplayerPlayer>();
			if (mp.LivesInitialized)
				continue;

			// Mid-join: PerPlayer full budget; PerTeam does not top up shared pool.
			AssignPlayer(p, config);
			if (config.BossFightLivesMode == BossFightLivesMode.PerTeam && p.team is >= 1 and <= 5
			    && !TeamRespawnsLeft.ContainsKey(p.team))
				TeamRespawnsLeft[p.team] = 0;

			DeathTracker.Seed(i, p.dead);
		}
	}

	private static void AssignPlayer(Player player, ServerConfig config)
	{
		BestMultiplayerPlayer mp = player.GetModPlayer<BestMultiplayerPlayer>();
		mp.LivesInitialized = true;
		// Already dead at fight start / join: finish vanilla respawn without spending.
		mp.RespawnAllowedThisDeath = player.dead;

		if (config.BossFightLivesMode == BossFightLivesMode.PerPlayer)
		{
			mp.RespawnsRemaining = config.BossFightRespawns;
			return;
		}

		// PerTeam: teams 1–5 use shared map; unteamed = solo pool of 1.
		mp.RespawnsRemaining = player.team == 0 ? 1 : 0;
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

			BestMultiplayerPlayer mp = p.GetModPlayer<BestMultiplayerPlayer>();
			mp.RespawnsRemaining = 0;
			mp.RespawnAllowedThisDeath = false;
			mp.LivesInitialized = false;
		}
	}

	private static void OnPlayerDied(Player player)
	{
		if (!IsLivesModeActive())
			return;

		BestMultiplayerPlayer mp = player.GetModPlayer<BestMultiplayerPlayer>();
		mp.RespawnAllowedThisDeath = false;

		ServerConfig config = ServerConfig.Instance;
		if (config is null)
			return;

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

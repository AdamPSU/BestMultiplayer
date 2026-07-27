using System.Collections.Generic;
using BestMultiplayer.Common.Configs;
using BestMultiplayer.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Boss-fight detection and respawn-budget pools for wormhole / lives rules.
/// </summary>
public sealed class BossFightSystem : ModSystem
{
	private static readonly bool[] WasDead = new bool[Main.maxPlayers];
	private static readonly Dictionary<int, int> TeamRespawnsLeft = new();
	private static bool _poolsReady;

	public static bool IsBossFightActive()
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || !npc.HasValidTarget)
				continue;

			if (npc.boss)
				return true;

			if (npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
				return true;
		}

		return false;
	}

	public override void PostUpdatePlayers()
	{
		if (!IsBossFightActive())
		{
			if (_poolsReady)
				ClearPools();
			SnapshotDeadFlags();
			return;
		}

		if (!_poolsReady)
			InitPools();
		else
			EnsureNewPlayers();

		ProcessDeathEdges();
	}

	public override void ClearWorld()
	{
		ClearPools();
		for (int i = 0; i < WasDead.Length; i++)
			WasDead[i] = false;
	}

	internal static bool IsLivesModeActive()
	{
		ServerConfig config = ServerConfig.Instance;
		return config is not null && config.BossFightLivesMode is BossFightLivesMode.PerPlayer or BossFightLivesMode.PerTeam;
	}

	/// <summary>
	/// True when this death may complete the vanilla respawn timer.
	/// </summary>
	internal static bool MayRespawnThisDeath(Player player)
	{
		if (!IsBossFightActive() || !IsLivesModeActive())
			return true;

		return player.GetModPlayer<BestMultiplayerPlayer>().RespawnAllowedThisDeath;
	}

	/// <summary>
	/// Local player is dead and out of boss-fight lives (hard lock).
	/// </summary>
	internal static bool IsLocalHardLocked()
	{
		Player player = Main.LocalPlayer;
		return player.active && player.dead && !MayRespawnThisDeath(player);
	}

	private static void InitPools()
	{
		TeamRespawnsLeft.Clear();
		ServerConfig config = ServerConfig.Instance;
		if (config is null || !IsLivesModeActive())
		{
			_poolsReady = true;
			SnapshotDeadFlags();
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
		SnapshotDeadFlags();
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

			WasDead[i] = p.dead;
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

	private static void ProcessDeathEdges()
	{
		if (!IsLivesModeActive())
		{
			SnapshotDeadFlags();
			return;
		}

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			bool dead = p.active && p.dead;
			if (dead && !WasDead[i])
				OnPlayerDied(p);
			WasDead[i] = dead;
		}
	}

	private static void SnapshotDeadFlags()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			WasDead[i] = p.active && p.dead;
		}
	}

	private static void OnPlayerDied(Player player)
	{
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

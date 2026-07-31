using System;
using System.IO;
using BestMultiplayer.Common;
using BestMultiplayer.Common.Configs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Per-player boss-fight stats.
/// Hit/hurt hooks run on the local client only (tML docs) — clients apply locally and
/// forward deltas to the server; server aggregates and broadcasts snapshots.
/// </summary>
public sealed class FightStatsSystem : ModSystem
{
	private const int SyncInterval = 15;
	private const int FreezeTicks = 360; // 6s

	private static readonly int[] Dealt = new int[Main.maxPlayers];
	private static readonly int[] Taken = new int[Main.maxPlayers];
	private static readonly int[] Deaths = new int[Main.maxPlayers];
	private static readonly DeathEdgeTracker DeathTracker = new();

	private static bool _tracking;
	private static int _freezeLeft;
	private static int _syncTimer;
	private static bool _dirty;
	private static bool _clientHadFight;

	public static bool IsTracking => _tracking;

	public static bool ShouldShowFeed =>
		(ServerConfig.Instance?.BossFightStatsEnabled ?? true)
		&& (_tracking || _freezeLeft > 0);

	public static int GetDealt(int whoAmI) => Dealt[whoAmI];
	public static int GetTaken(int whoAmI) => Taken[whoAmI];
	public static int GetDeaths(int whoAmI) => Deaths[whoAmI];

	public static int Pct(int part, int total) =>
		total <= 0 ? 0 : (int)Math.Round(100.0 * part / total);

	public static void TeamTotals(int team, int localWho, out int dealt, out int taken)
	{
		dealt = 0;
		taken = 0;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active || !SameRoster(p, team, localWho))
				continue;
			dealt += Dealt[i];
			taken += Taken[i];
		}
	}

	public static bool SameRoster(Player p, int team, int localWho) =>
		team == 0 ? p.whoAmI == localWho : p.team == team;

	/// <summary>
	/// Called from ModPlayer.OnHitNPC — runs on the client that landed the hit (incl. SP).
	/// </summary>
	internal static void NotifyDealt(Player player, NPC target, int damageDone)
	{
		if (damageDone <= 0 || !target.active || !BossNpc.IsAnySegment(target))
			return;
		if (!TryEnsureTracking())
			return;

		int who = player.whoAmI;
		Dealt[who] += damageDone;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			SendDelta(dealtAdd: damageDone, takenAdd: 0);
		else
			_dirty = true;
	}

	/// <summary>
	/// Called from ModPlayer.OnHurt — runs on the client that took the hit (incl. SP).
	/// </summary>
	internal static void NotifyTaken(Player player, int damage)
	{
		if (damage <= 0)
			return;
		if (!TryEnsureTracking())
			return;

		int who = player.whoAmI;
		Taken[who] += damage;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			SendDelta(dealtAdd: 0, takenAdd: damage);
		else
			_dirty = true;
	}

	public override void PostUpdatePlayers()
	{
		if (!(ServerConfig.Instance?.BossFightStatsEnabled ?? true))
		{
			if (_tracking || _freezeLeft > 0)
				ResetAll();
			return;
		}

		bool boss = BossFightSystem.IsBossFightActive();

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			TickClient(boss);
			return;
		}

		// SP + listen/dedicated server authority path
		if (boss)
		{
			if (!_tracking)
				BeginTracking();
			DeathTracker.ForEachNewDeath(OnDeath);
			if (++_syncTimer >= SyncInterval && _dirty)
			{
				_syncTimer = 0;
				_dirty = false;
				Broadcast();
			}

			return;
		}

		if (_tracking)
		{
			_tracking = false;
			_freezeLeft = FreezeTicks;
			Broadcast();
			DeathTracker.Snapshot();
			return;
		}

		if (_freezeLeft > 0 && --_freezeLeft == 0)
			ClearArrays();
	}

	public override void ClearWorld() => ResetAll();

	internal static void HandleSnapshotPacket(BinaryReader reader)
	{
		Array.Clear(Dealt);
		Array.Clear(Taken);
		Array.Clear(Deaths);
		int n = reader.ReadByte();
		for (int i = 0; i < n; i++)
		{
			int who = reader.ReadByte();
			int dealt = reader.ReadInt32();
			int taken = reader.ReadInt32();
			byte deaths = reader.ReadByte();
			if (who is < 0 or >= Main.maxPlayers)
				continue;
			Dealt[who] = dealt;
			Taken[who] = taken;
			Deaths[who] = deaths;
		}
	}

	/// <summary>Server: apply a client-reported delta. whoAmI must match the sender.</summary>
	internal static void HandleDeltaPacket(BinaryReader reader, int sender)
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		int dealtAdd = reader.ReadInt32();
		int takenAdd = reader.ReadInt32();
		if (dealtAdd < 0 || takenAdd < 0)
			return;
		// Cap absurd packets
		if (dealtAdd > 1_000_000)
			dealtAdd = 1_000_000;
		if (takenAdd > 1_000_000)
			takenAdd = 1_000_000;

		if (!TryEnsureTracking())
			return;

		if (sender is < 0 or >= Main.maxPlayers)
			return;

		Dealt[sender] += dealtAdd;
		Taken[sender] += takenAdd;
		_dirty = true;
	}

	private static void SendDelta(int dealtAdd, int takenAdd)
	{
		ModPacket packet = ModContent.GetInstance<BestMultiplayer>().GetPacket();
		packet.Write(Packets.FightStatsDelta);
		packet.Write(dealtAdd);
		packet.Write(takenAdd);
		packet.Send();
	}

	/// <summary>Arm tracking if a boss fight is live. Returns false if stats should not count.</summary>
	private static bool TryEnsureTracking()
	{
		if (!(ServerConfig.Instance?.BossFightStatsEnabled ?? true))
			return false;
		if (_freezeLeft > 0 && !_tracking)
			return false; // post-fight freeze — ignore stragglers
		if (_tracking)
			return true;
		if (!BossFightSystem.IsBossFightActive())
			return false;

		// First hit can arrive before PostUpdatePlayers arms us — arm without wiping.
		_tracking = true;
		_freezeLeft = 0;
		_syncTimer = 0;
		DeathTracker.Snapshot();
		return true;
	}

	private static void TickClient(bool boss)
	{
		if (boss)
		{
			_clientHadFight = true;
			_tracking = true;
			_freezeLeft = 0;
			return;
		}

		if (_clientHadFight)
		{
			_clientHadFight = false;
			_tracking = false;
			_freezeLeft = FreezeTicks;
			return;
		}

		if (_freezeLeft > 0 && --_freezeLeft == 0)
		{
			ClearArrays();
			_tracking = false;
		}
	}

	private static void BeginTracking()
	{
		ClearArrays();
		_tracking = true;
		_freezeLeft = 0;
		_syncTimer = 0;
		_dirty = false;
		DeathTracker.Snapshot();
	}

	private static void OnDeath(Player player)
	{
		Deaths[player.whoAmI]++;
		_dirty = true;
	}

	private static void Broadcast()
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		ModPacket packet = ModContent.GetInstance<BestMultiplayer>().GetPacket();
		packet.Write(Packets.FightStatsSnapshot);

		int count = 0;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (Main.player[i].active)
				count++;
		}

		packet.Write((byte)count);
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (!Main.player[i].active)
				continue;
			packet.Write((byte)i);
			packet.Write(Dealt[i]);
			packet.Write(Taken[i]);
			packet.Write((byte)Math.Min(255, Deaths[i]));
		}

		packet.Send();
	}

	private static void ResetAll()
	{
		_tracking = false;
		_freezeLeft = 0;
		_syncTimer = 0;
		_dirty = false;
		_clientHadFight = false;
		ClearArrays();
		DeathTracker.Reset();
	}

	private static void ClearArrays()
	{
		Array.Clear(Dealt);
		Array.Clear(Taken);
		Array.Clear(Deaths);
	}
}

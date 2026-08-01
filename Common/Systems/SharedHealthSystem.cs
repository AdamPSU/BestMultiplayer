using System;
using System.Collections.Generic;
using System.IO;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Per-team shared HP pools (teams 1–5). Server/SP authoritative; clients mirror via packet.
/// Always-on or bosses-only; independent of boss-fight lives.
/// </summary>
public sealed class SharedHealthSystem : ModSystem
{
	private struct Pool
	{
		public int Current;
		public int Max;
		public bool Wiped;
		/// <summary>Bitmask of player slots currently contributing to this pool.</summary>
		public int MemberMask;
	}

	private static readonly Dictionary<int, Pool> Pools = new();
	private static readonly DeathEdgeTracker DeathTracker = new();
	private static bool _armed;
	private static bool _wiping;
	private static int _wipeIgnoreWhoAmI = -1;
	private static float _armedMultiplier = 1f;

	public static bool IsEnabled() => ServerConfig.Instance.SharedHealthEnabled;

	public static bool IsBossesOnly() => ServerConfig.Instance.SharedHealthBossesOnly;

	/// <summary>Config on and (not bosses-only, or a boss fight is active).</summary>
	public static bool ShouldBeActive() =>
		IsEnabled() && (!IsBossesOnly() || BossFightSystem.IsBossFightActive());

	public static bool IsLinked(Player player) =>
		ShouldBeActive()
		&& _armed
		&& player.active
		&& !player.dead
		&& Teams.IsReal(player.team)
		&& Pools.TryGetValue(player.team, out Pool pool)
		&& !pool.Wiped;

	public static bool IsTeamWiped(int team) =>
		ShouldBeActive() && _armed && Pools.TryGetValue(team, out Pool p) && p.Wiped;

	/// <summary>Hard-lock only while a boss fight is active (wipe during exploration uses normal respawn).</summary>
	public static bool IsPlayerHardLocked(Player player) =>
		player.active
		&& player.dead
		&& Teams.IsReal(player.team)
		&& IsTeamWiped(player.team)
		&& BossFightSystem.IsBossFightActive();

	public static bool TryGetPool(int team, out int current, out int max)
	{
		if (Pools.TryGetValue(team, out Pool p))
		{
			current = p.Current;
			max = p.Max;
			return true;
		}

		current = 0;
		max = 0;
		return false;
	}

	/// <summary>
	/// True when this player's death should DeathLink the team.
	/// Does not require !dead — safe from PreKill and Kill.
	/// </summary>
	public static bool ShouldWipeOnMemberDeath(Player player) =>
		ShouldBeActive()
		&& _armed
		&& player.active
		&& Teams.IsReal(player.team)
		&& Pools.TryGetValue(player.team, out Pool pool)
		&& !pool.Wiped;

	/// <summary>PreKill/Kill entry: any member death while pool is live wipes the team.</summary>
	internal static void NotifyMemberDeath(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (!ShouldWipeOnMemberDeath(player))
			return;

		// Avoid re-entrant KillMe on the player already inside PreKill/Kill.
		int prev = _wipeIgnoreWhoAmI;
		_wipeIgnoreWhoAmI = player.whoAmI;
		try
		{
			ForceWipeTeam(player.team);
		}
		finally
		{
			_wipeIgnoreWhoAmI = prev;
		}
	}

	public override void PostUpdatePlayers()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			DeathTracker.Snapshot();
			return;
		}

		if (!ShouldBeActive())
		{
			if (_armed)
				DisarmAll();
			DeathTracker.Snapshot();
			return;
		}

		if (!_armed)
		{
			ArmAllTeams();
			DeathTracker.Snapshot();
			return;
		}

		// Catch deaths that skipped PreKill wipe (net order, one-shots, etc.).
		ProcessDeathEdges();

		// MP: KillMe alone often does not kill remote clients — keep enforcing.
		EnforceWipedTeams();

		float mult = Utils.Clamp(ServerConfig.Instance.SharedHealthMultiplier, 1f, 3f);
		if (Math.Abs(mult - _armedMultiplier) > 0.0001f)
			ApplyMultiplierChange(mult);

		UpdateRostersAndReconcile();
	}

	public override void ClearWorld()
	{
		DisarmAll();
		DeathTracker.Reset();
	}

	private static void ProcessDeathEdges()
	{
		if (_wiping)
			return;

		DeathTracker.ForEachNewDeath(WipeIfShould);
	}

	private static void WipeIfShould(Player p)
	{
		if (ShouldWipeOnMemberDeath(p))
			ForceWipeTeam(p.team);
	}

	internal static void HandlePoolPacket(BinaryReader reader)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		byte flags = reader.ReadByte();
		bool armed = (flags & 1) != 0;
		if (!armed)
		{
			DisarmClientOnly();
			return;
		}

		_armed = true;
		Pools.Clear();
		int count = reader.ReadByte();
		for (int i = 0; i < count; i++)
		{
			int team = reader.ReadByte();
			int current = reader.ReadInt32();
			int max = reader.ReadInt32();
			bool wiped = reader.ReadBoolean();
			Pools[team] = new Pool { Current = current, Max = max, Wiped = wiped, MemberMask = 0 };
		}
	}

	private static void ArmAllTeams()
	{
		Pools.Clear();
		_armedMultiplier = Utils.Clamp(ServerConfig.Instance.SharedHealthMultiplier, 1f, 3f);
		for (int team = Teams.Min; team <= Teams.Max; team++)
			TryArmTeam(team);
		_armed = true;
		BroadcastPools();
	}

	private static void TryArmTeam(int team)
	{
		int sumMax = SumNaturalMax(team, out int mask);
		if (mask == 0 || sumMax <= 0)
			return;

		int max = ScaleMax(sumMax, _armedMultiplier);
		Pools[team] = new Pool { Current = max, Max = max, Wiped = false, MemberMask = mask };
		MirrorTeam(team);
	}

	private static int NaturalMax(Player p)
	{
		SharedHealthPlayer sp = p.GetModPlayer<SharedHealthPlayer>();
		if (sp.HasSnapshot && sp.RealLifeMax > 0)
			return sp.RealLifeMax;
		return Math.Max(1, p.statLifeMax2);
	}

	private static int ScaleMax(int sumNaturalMax, float mult)
	{
		int max = (int)Math.Round(sumNaturalMax * mult);
		return Math.Max(1, max);
	}

	private static void ApplyMultiplierChange(float mult)
	{
		_armedMultiplier = mult;
		List<int> teams = new(Pools.Keys);
		foreach (int team in teams)
		{
			if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
				continue;

			int sumMax = SumNaturalMax(team, out int mask);
			if (sumMax <= 0)
			{
				Pools.Remove(team);
				continue;
			}

			int newMax = ScaleMax(sumMax, mult);
			int newCurrent = pool.Max > 0
				? (int)Math.Round(pool.Current * (double)newMax / pool.Max)
				: newMax;
			newCurrent = Utils.Clamp(newCurrent, 0, newMax);
			pool.Max = newMax;
			pool.Current = newCurrent;
			pool.MemberMask = mask;
			Pools[team] = pool;
			if (newCurrent <= 0)
				WipeTeam(team);
			else
				MirrorTeam(team);
		}

		BroadcastPools();
	}

	private static bool IsLivingTeamMember(Player p, int team) => p.active && !p.dead && p.team == team;

	private static int SumNaturalMax(int team, out int mask)
	{
		int sum = 0;
		mask = 0;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!IsLivingTeamMember(p, team))
				continue;
			mask |= 1 << i;
			sum += Math.Max(1, NaturalMax(p));
		}

		return sum;
	}

	private static void UpdateRostersAndReconcile()
	{
		if (_wiping)
			return;

		// Ensure teams with living members have a pool (first join after empty / post-wipe).
		for (int team = Teams.Min; team <= Teams.Max; team++)
		{
			bool found = Pools.TryGetValue(team, out Pool existing);
			if (found && !existing.Wiped)
				continue;
			if (found)
			{
				// Outside boss: wiped teams re-arm once someone is living again.
				if (!BossFightSystem.IsBossFightActive() && HasLivingMember(team))
				{
					Pools.Remove(team);
					TryArmTeam(team);
				}

				continue;
			}

			if (HasLivingMember(team))
				TryArmTeam(team);
		}

		bool changed = false;
		for (int team = Teams.Min; team <= Teams.Max; team++)
		{
			if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
				continue;

			int sumMax = 0;
			int mask = 0;
			int minLife = int.MaxValue;
			int maxLife = 0;
			int living = 0;
			int joinLife = 0;

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!IsLivingTeamMember(p, team))
					continue;

				living++;
				int bit = 1 << i;
				mask |= bit;
				int nat = Math.Max(1, NaturalMax(p));
				sumMax += nat;

				if ((pool.MemberMask & bit) == 0)
				{
					// New contributor: add their current life into the pool.
					int add = p.statLife;
					// If already mirrored to old pool, prefer natural proportion of personal bar.
					if (pool.Max > 0 && p.statLifeMax2 == pool.Max)
						add = Utils.Clamp((int)Math.Round(nat * (double)pool.Current / pool.Max), 1, nat);
					else
						add = Utils.Clamp(p.statLife, 1, nat);
					joinLife += add;
				}

				if (p.statLife < minLife)
					minLife = p.statLife;
				if (p.statLife > maxLife)
					maxLife = p.statLife;
			}

			if (living == 0)
			{
				// Everyone dead without a wipe flag (missed PreKill) — mark wipe, don't drop pool.
				if (!pool.Wiped)
				{
					WipeTeam(team);
					changed = true;
				}

				continue;
			}

			int newMax = ScaleMax(sumMax, _armedMultiplier);
			bool rosterChanged = mask != pool.MemberMask || newMax != pool.Max;

			int next = pool.Current;
			if (joinLife > 0)
				next += joinLife;

			if (rosterChanged && newMax != pool.Max && pool.Max > 0 && joinLife == 0)
			{
				// Leave-only: scale current to new max.
				next = (int)Math.Round(pool.Current * (double)newMax / pool.Max);
			}

			// Damage / heal reconcile among mirrored bars.
			if (minLife < next)
				next = minLife;
			else if (maxLife > next && joinLife == 0)
				next = maxLife;

			next = Utils.Clamp(next, 0, newMax);

			if (next == pool.Current && newMax == pool.Max && mask == pool.MemberMask)
				continue;

			pool.Current = next;
			pool.Max = newMax;
			pool.MemberMask = mask;
			Pools[team] = pool;
			changed = true;

			if (next <= 0)
			{
				WipeTeam(team);
				continue;
			}

			MirrorTeam(team);
		}

		if (changed)
			BroadcastPools();
	}

	private static bool HasLivingMember(int team)
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (IsLivingTeamMember(Main.player[i], team))
				return true;
		}

		return false;
	}

	private static void DisarmAll()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!p.active)
					continue;

				SharedHealthPlayer sp = p.GetModPlayer<SharedHealthPlayer>();
				if (!sp.HasSnapshot)
					continue;

				int restoreMax = sp.RealLifeMax > 0 ? sp.RealLifeMax : Math.Max(1, p.statLifeMax);
				int life = p.statLife;
				if (Teams.IsReal(p.team) && Pools.TryGetValue(p.team, out Pool pool) && pool.Max > 0 && !p.dead)
					life = (int)Math.Round(restoreMax * (double)pool.Current / pool.Max);

				if (p.dead)
					life = 0;
				else
					life = Utils.Clamp(life, 1, restoreMax);

				p.statLifeMax2 = restoreMax;
				if (!p.dead)
					p.statLife = life;

				sp.ClearSnapshot();

				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, p.whoAmI);
			}

			bool wasArmed = _armed;
			ResetPoolState();
			if (wasArmed)
				BroadcastPools();
			return;
		}

		DisarmClientOnly();
	}

	private static void DisarmClientOnly()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active)
				p.GetModPlayer<SharedHealthPlayer>().ClearSnapshot();
		}

		ResetPoolState();
	}

	private static void ResetPoolState()
	{
		Pools.Clear();
		_armed = false;
		_wiping = false;
		_armedMultiplier = 1f;
		DeathTracker.Snapshot();
	}

	private static void MirrorTeam(int team)
	{
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!IsLivingTeamMember(p, team))
				continue;

			p.statLifeMax2 = pool.Max;
			p.statLife = Utils.Clamp(pool.Current, 0, pool.Max);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, p.whoAmI);
		}
	}

	/// <summary>Called when a linked player takes a fatal hit (bar emptied).</summary>
	internal static void ForceWipeTeam(int team)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;
		pool.Current = 0;
		Pools[team] = pool;
		WipeTeam(team);
	}

	private static void WipeTeam(int team)
	{
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;

		_wiping = true;
		pool.Current = 0;
		pool.Wiped = true;
		Pools[team] = pool;

		KillLivingTeamMembers(team);
		_wiping = false;
		BroadcastPools();
	}

	/// <summary>
	/// While a team is wiped, any still-living member must die (MP KillMe often fails without SendPlayerDeath).
	/// </summary>
	private static void EnforceWipedTeams()
	{
		if (_wiping)
			return;

		foreach (KeyValuePair<int, Pool> kv in Pools)
		{
			if (!kv.Value.Wiped)
				continue;
			KillLivingTeamMembers(kv.Key);
		}
	}

	private static void KillLivingTeamMembers(int team)
	{
		bool hardLock = BossFightSystem.IsBossFightActive();
		// No custom death reason — avoids a chat death line. Center UI still shows
		// "Your team fell together..." via DeathScreenSystem when hard-locked.
		PlayerDeathReason reason = PlayerDeathReason.LegacyEmpty();

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!IsLivingTeamMember(p, team))
				continue;

			if (hardLock)
				p.GetModPlayer<DefinitiveMultiplayerPlayer>().RespawnAllowedThisDeath = false;

			// Player already inside their own PreKill/Kill — don't re-enter KillMe.
			if (p.whoAmI == _wipeIgnoreWhoAmI)
				continue;

			p.statLife = 0;
			p.KillMe(reason, 9999.0, 0);

			// Required in multiplayer: server KillMe does not reliably kill remote clients.
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendPlayerDeath(p.whoAmI, reason, 9999, 0, pvp: false);
		}
	}

	private static void BroadcastPools()
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		ModPacket packet = Packets.Begin(Packets.SharedHealthPool);
		if (!_armed)
		{
			packet.Write((byte)0);
			packet.Send();
			return;
		}

		packet.Write((byte)1);
		packet.Write((byte)Pools.Count);
		foreach (KeyValuePair<int, Pool> kv in Pools)
		{
			packet.Write((byte)kv.Key);
			packet.Write(kv.Value.Current);
			packet.Write(kv.Value.Max);
			packet.Write(kv.Value.Wiped);
		}

		packet.Send();
	}
}

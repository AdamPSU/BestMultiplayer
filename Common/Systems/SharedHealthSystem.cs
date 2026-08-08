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

internal enum SharedHealCause : byte
{
	Other = 0,
	Potion = 1,
}

/// <summary>
/// Per-team shared HP pools (teams 1–5). Server/SP authoritative.
/// Clients predict via unacked event ids; meta carries poolSeq + ack id.
/// Bars are projectors only — never sensors.
/// </summary>
public sealed class SharedHealthSystem : ModSystem
{
	private const int HeartbeatTicks = 45;
	private const int PotionSickDelay = 60;
	private const int MaxDamageEvent = 10_000;
	private const int MaxHealEvent = 500;
	private const int RecentEventCapacity = 64;
	/// <summary>Vanilla: lifeRegenCount threshold per 1 HP (≈ lifeRegen/2 HP per second at 60fps).</summary>
	private const int RegenCountsPerHp = 120;

	private struct Pool
	{
		public int Current;
		public int Max;
		public bool Wiped;
		public uint Seq;
		public int MemberMask;
	}

	/// <summary>Signed pending delta: damage negative, heal positive.</summary>
	private struct PendingEvent
	{
		public ushort Id;
		public int Delta;
	}

	private struct SickJob
	{
		public int Team;
		public int TicksLeft;
		public int Duration;
		public int SkipWho;
	}

	private struct ClientMirror
	{
		public bool Armed;
		public bool Wiped;
		public int Team;
		public int Current;
		public int Max;
		public uint Seq;

		public void Clear()
		{
			Armed = false;
			Wiped = false;
			Team = 0;
			Current = 0;
			Max = 0;
			Seq = 0;
		}
	}

	private static readonly Dictionary<int, Pool> Pools = new();
	/// <summary>Per-team lifeRegen accumulators (vanilla units; index = team 1–5).</summary>
	private static readonly float[] RegenCounts = new float[Teams.Max + 1];
	private static readonly List<SickJob> SickJobs = new();
	private static readonly HashSet<uint>[] RecentEvents = new HashSet<uint>[Main.maxPlayers];
	private static readonly Queue<uint>[] RecentEventOrder = new Queue<uint>[Main.maxPlayers];
	private static readonly List<PendingEvent> LocalPending = new();

	private static bool _armed;
	private static bool _wiping;
	private static bool _metaDirty;
	private static int _wipeIgnoreWhoAmI = -1;
	private static int _heartbeat;
	private static ushort _nextEventId = 1;
	private static uint _poolSeqCounter;
	private static ClientMirror _client;
	private static int _pendingDelta;

	private static float Multiplier =>
		Utils.Clamp(ServerConfig.Instance.SharedHealthMultiplier, 0.5f, 2f);

	static SharedHealthSystem()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			RecentEvents[i] = new HashSet<uint>();
			RecentEventOrder[i] = new Queue<uint>();
		}
	}

	public static bool IsEnabled() => ServerConfig.Instance.SharedHealthEnabled;

	public static bool ShouldBeActive() =>
		IsEnabled() && (!ServerConfig.Instance.SharedHealthBossesOnly || BossFightSystem.IsBossFightActive());

	public static bool IsArmed() =>
		Main.netMode == NetmodeID.MultiplayerClient ? _client.Armed : _armed;

	public static bool IsLinked(Player player) =>
		IsArmed()
		&& player.active
		&& !player.dead
		&& Teams.IsReal(player.team)
		&& !IsTeamWiped(player.team);

	public static bool IsTeamWiped(int team)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return _client.Armed && _client.Wiped && _client.Team == team;

		return _armed && Pools.TryGetValue(team, out Pool p) && p.Wiped;
	}

	public static bool IsPlayerHardLocked(Player player) =>
		player.active
		&& player.dead
		&& Teams.IsReal(player.team)
		&& IsTeamWiped(player.team)
		&& BossFightSystem.IsBossFightActive();

	public static bool TryGetPool(int team, out int current, out int max)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (_client.Armed && _client.Team == team && _client.Max > 0)
			{
				current = _client.Current;
				max = _client.Max;
				return true;
			}

			current = 0;
			max = 0;
			return false;
		}

		if (Pools.TryGetValue(team, out Pool p) && p.Max > 0)
		{
			current = p.Current;
			max = p.Max;
			return true;
		}

		current = 0;
		max = 0;
		return false;
	}

	/// <summary>UI pool for a player. Local body uses predicted current.</summary>
	public static bool TryGetPoolForPlayer(Player player, out int current, out int max)
	{
		current = 0;
		max = 0;
		if (!IsArmed() || !Teams.IsReal(player.team))
			return false;
		if (!TryGetPool(player.team, out current, out max) || max <= 0)
			return false;
		if (player.whoAmI == Main.myPlayer)
			current = GetDisplayCurrent(player.team);
		return true;
	}

	/// <summary>Bar projector: server current + local unacked prediction.</summary>
	public static int GetDisplayCurrent(int team)
	{
		if (!TryGetPool(team, out int serverCur, out int max) || max <= 0)
			return 0;

		if (Main.netMode != NetmodeID.MultiplayerClient || Main.LocalPlayer.team != team)
			return Utils.Clamp(serverCur, 0, max);

		return Utils.Clamp(serverCur + _pendingDelta, 0, max);
	}

	public static void PaintLocalBar(Player player, int team)
	{
		if (!TryGetPool(team, out int cur, out int max) || max <= 0)
			return;

		if (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer)
			cur = Utils.Clamp(cur + _pendingDelta, 0, max);

		player.GetModPlayer<SharedHealthPlayer>().ApplyPoolLifePaint(cur, max);
	}

	public override void PostUpdatePlayers()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		if (!ShouldBeActive())
		{
			if (_armed)
				DisarmAll();
			TickSickJobs();
			return;
		}

		if (!_armed)
			ArmAllTeams();
		else
			RefreshRosters();

		TickTeamRegen();
		EnforceWipedTeams();
		PaintServerMembers();
		TickSickJobs();

		if (++_heartbeat >= HeartbeatTicks)
		{
			_heartbeat = 0;
			_metaDirty = true;
		}

		FlushMeta();
	}

	public override void ClearWorld() => ResetAll();

	internal static void NotifyDamage(Player victim, int amount)
	{
		if (amount <= 0 || !IsLinked(victim) || !IsLocalAuthority(victim))
			return;

		int capped = Math.Min(amount, MaxDamageEvent);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ushort id = NextEventId();
			PushPending(id, -capped);
			SendDamage(id, capped);
			return;
		}

		ApplyDamage(victim.team, capped, eventId: 0, sender: victim.whoAmI);
	}

	internal static void NotifyHeal(Player healer, int amount, SharedHealCause cause = SharedHealCause.Other)
	{
		if (amount <= 0 || !IsLinked(healer) || !IsLocalAuthority(healer))
			return;

		int capped = Math.Min(amount, MaxHealEvent);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ushort id = NextEventId();
			PushPending(id, capped);
			SendHeal(id, capped, cause);
			return;
		}

		ApplyHeal(healer.team, capped, cause, eventId: 0, sender: healer.whoAmI);
	}

	/// <summary>SP always; MP only the local body (client or listen-host).</summary>
	private static bool IsLocalAuthority(Player player) =>
		Main.netMode == NetmodeID.SinglePlayer || player.whoAmI == Main.myPlayer;

	internal static void NotifyLocalRespawn()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			ClearPending();
	}

	internal static void NotifyMemberDeath(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (!_armed || !Teams.IsReal(player.team))
			return;
		if (!Pools.TryGetValue(player.team, out Pool pool) || pool.Wiped)
			return;

		BeginWipe(player.team, player.whoAmI);
	}

	/// <summary>
	/// Scripted lethal (Hot Potato fuse) that bypasses <c>OnHurt</c>.
	/// Zeros the team pool and kills teammates first so PreKill will not cancel the caller's death.
	/// Caller still <c>KillMe</c>s <paramref name="source"/> (custom death reason). Server/SP only.
	/// </summary>
	internal static void PrepareScriptedTeamWipe(Player source)
	{
		if (source is null || Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (!_armed || !Teams.IsReal(source.team))
			return;
		if (!Pools.TryGetValue(source.team, out Pool pool) || pool.Wiped || pool.Max <= 0)
			return;

		BeginWipe(source.team, ignoreWhoAmI: source.whoAmI);
	}

	internal static void HandleMetaPacket(BinaryReader reader)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		byte flags = reader.ReadByte();
		if ((flags & 1) == 0)
		{
			DisarmClientOnly();
			return;
		}

		_client.Armed = true;
		int count = reader.ReadByte();
		int localTeam = Main.LocalPlayer.team;
		bool sawLocal = false;

		for (int i = 0; i < count; i++)
		{
			int team = reader.ReadByte();
			uint seq = reader.ReadUInt32();
			int current = reader.ReadInt32();
			int max = reader.ReadInt32();
			bool wiped = reader.ReadBoolean();

			if (team != localTeam)
				continue;

			sawLocal = true;
			bool wasWiped = _client.Wiped;
			if (seq < _client.Seq && !(wasWiped && !wiped))
				continue;

			if (seq >= _client.Seq)
				_client.Seq = seq;
			_client.Current = current;
			_client.Max = max;
			_client.Wiped = wiped;
			_client.Team = team;
			if (wiped || wasWiped)
				ClearPending();
		}

		ushort ack = reader.ReadUInt16();
		if (ack != 0)
			ApplyAck(ack);

		if (!sawLocal && Teams.IsReal(localTeam))
		{
			_client.Current = 0;
			_client.Max = 0;
			_client.Wiped = false;
			_client.Team = localTeam;
		}
	}

	internal static void HandleDamagePacket(BinaryReader reader, int sender)
	{
		ushort eventId = reader.ReadUInt16();
		int amount = reader.ReadInt32();
		if (!TryGetSenderTeam(sender, out Player p))
			return;
		ApplyDamage(p.team, amount, eventId, sender);
	}

	internal static void HandleHealPacket(BinaryReader reader, int sender)
	{
		ushort eventId = reader.ReadUInt16();
		int amount = reader.ReadInt32();
		SharedHealCause cause = (SharedHealCause)reader.ReadByte();
		if (!TryGetSenderTeam(sender, out Player p))
			return;
		ApplyHeal(p.team, amount, cause, eventId, sender);
	}

	internal static void HandleTeamSickPacket(BinaryReader reader)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		int team = reader.ReadByte();
		int duration = reader.ReadInt32();
		int skipWho = reader.ReadByte();
		if (skipWho == 255)
			skipWho = -1;

		ApplyTeamSickLocal(team, duration, skipWho);
	}

	private static bool TryGetSenderTeam(int sender, out Player player)
	{
		player = null!;
		if (Main.netMode != NetmodeID.Server || sender is < 0 or >= Main.maxPlayers)
			return false;

		player = Main.player[sender];
		return player.active && Teams.IsReal(player.team);
	}

	private static bool TryGetLivePool(int team, ushort eventId, int sender, out Pool pool)
	{
		pool = default;
		if (!_armed || !Teams.IsReal(team))
			return false;
		if (!Pools.TryGetValue(team, out pool) || pool.Wiped)
			return false;
		if (eventId != 0 && !AcceptEvent(sender, eventId))
			return false;
		return true;
	}

	private static void ApplyDamage(int team, int amount, ushort eventId, int sender)
	{
		if (!TryGetLivePool(team, eventId, sender, out Pool pool))
			return;

		amount = Utils.Clamp(amount, 0, MaxDamageEvent);
		if (amount <= 0)
			return;

		pool.Current = Math.Max(0, pool.Current - amount);
		pool.Seq = NextPoolSeq();
		Pools[team] = pool;

		if (pool.Current <= 0)
			BeginWipe(team, ignoreWhoAmI: sender, ackPlayer: sender, ackEventId: eventId);
		else
			BroadcastMeta(sender, eventId);
	}

	private static void ApplyHeal(int team, int amount, SharedHealCause cause, ushort eventId, int sender)
	{
		if (!TryGetLivePool(team, eventId, sender, out Pool pool))
			return;

		amount = Utils.Clamp(amount, 0, MaxHealEvent);
		int applied = Math.Min(amount, Math.Max(0, pool.Max - pool.Current));
		if (applied > 0)
		{
			pool.Current += applied;
			pool.Seq = NextPoolSeq();
			Pools[team] = pool;

			if (cause == SharedHealCause.Potion)
				ScheduleTeamSick(team, sender);
		}

		// Ack even when applied == 0 so client prediction clears (immediate — not coalesced).
		BroadcastMeta(sender, eventId);
	}

	/// <summary>Average lifeRegen of living teammates → one organism rate (120 counts → ±1 HP).</summary>
	private static void TickTeamRegen()
	{
		if (!_armed)
			return;

		for (int team = Teams.Min; team <= Teams.Max; team++)
		{
			if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped || pool.Max <= 0)
			{
				RegenCounts[team] = 0f;
				continue;
			}

			int sum = 0;
			int living = 0;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!IsLivingOnTeam(p, team))
					continue;
				sum += p.GetModPlayer<SharedHealthPlayer>().CapturedLifeRegen;
				living++;
			}

			if (living <= 0)
			{
				RegenCounts[team] = 0f;
				continue;
			}

			float avg = sum / (float)living;
			if (avg == 0f)
				continue;

			float count = RegenCounts[team] + avg;
			int before = pool.Current;

			if (count >= RegenCountsPerHp || count <= -RegenCountsPerHp)
			{
				int steps = (int)(count / RegenCountsPerHp);
				count -= steps * RegenCountsPerHp;
				if (steps > 0)
					pool.Current = Math.Min(pool.Max, pool.Current + steps);
				else if (steps < 0)
					pool.Current = Math.Max(0, pool.Current + steps);
			}

			RegenCounts[team] = count;

			if (pool.Current == before)
				continue;

			if (pool.Current <= 0)
			{
				Pools[team] = pool;
				BeginWipe(team, ignoreWhoAmI: -1);
				continue;
			}

			pool.Seq = NextPoolSeq();
			Pools[team] = pool;
			_metaDirty = true;
		}
	}

	private static void BeginWipe(int team, int ignoreWhoAmI, int ackPlayer = -1, ushort ackEventId = 0)
	{
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;

		pool.Current = 0;
		pool.Wiped = true;
		pool.Seq = NextPoolSeq();
		Pools[team] = pool;

		// Meta before kills so clients set wiped and do not PreKill-cancel DeathLink.
		if (ackEventId != 0)
			BroadcastMeta(ackPlayer, ackEventId);
		else
		{
			_metaDirty = false;
			BroadcastMeta();
		}

		_wiping = true;
		int prev = _wipeIgnoreWhoAmI;
		_wipeIgnoreWhoAmI = ignoreWhoAmI;
		try
		{
			KillLivingTeamMembers(team);
		}
		finally
		{
			_wipeIgnoreWhoAmI = prev;
			_wiping = false;
		}
	}

	private static void EnforceWipedTeams()
	{
		if (_wiping || !_armed)
			return;

		foreach (KeyValuePair<int, Pool> kv in Pools)
		{
			if (kv.Value.Wiped)
				KillLivingTeamMembers(kv.Key);
		}
	}

	private static void KillLivingTeamMembers(int team)
	{
		bool hardLock = BossFightSystem.IsBossFightActive();
		PlayerDeathReason reason = PlayerDeathReason.LegacyEmpty();

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!IsLivingOnTeam(p, team) || p.whoAmI == _wipeIgnoreWhoAmI)
				continue;

			if (hardLock)
				p.GetModPlayer<DefinitiveMultiplayerPlayer>().RespawnAllowedThisDeath = false;

			p.statLife = 0;
			p.KillMe(reason, 9999.0, 0);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendPlayerDeath(p.whoAmI, reason, 9999, 0, pvp: false);
		}
	}

	private static void ArmAllTeams()
	{
		_armed = true;
		_heartbeat = 0;
		float mult = Multiplier;

		for (int team = Teams.Min; team <= Teams.Max; team++)
			TryArmTeam(team, mult, includeDeadContributors: false);

		_metaDirty = true;
	}

	private static void RefreshRosters()
	{
		float mult = Multiplier;
		for (int team = Teams.Min; team <= Teams.Max; team++)
		{
			if (Pools.TryGetValue(team, out Pool existing))
			{
				if (existing.Wiped)
				{
					// Outside boss: first living respawn re-arms. Count dead so duo keeps shared max.
					if (!BossFightSystem.IsBossFightActive()
					    && SumNaturalMax(team, out _, out int living, out _, includeDead: false) > 0
					    && living > 0)
					{
						Pools.Remove(team);
						if (TryArmTeam(team, mult, includeDeadContributors: true))
							_metaDirty = true;
					}

					continue;
				}

				if (SyncTeamMax(team, mult))
					_metaDirty = true;
				continue;
			}

			if (TryArmTeam(team, mult, includeDeadContributors: false))
				_metaDirty = true;
		}
	}

	private static bool TryArmTeam(int team, float mult, bool includeDeadContributors)
	{
		if (Pools.ContainsKey(team))
			return false;

		int sum = SumNaturalMax(team, out int mask, out int contributors, out _, includeDeadContributors);
		if (sum <= 0 || contributors <= 0)
			return false;

		// Dead-inclusive arm still needs at least one living body.
		if (includeDeadContributors
		    && (SumNaturalMax(team, out _, out int living, out _, includeDead: false) <= 0 || living <= 0))
			return false;

		int max = ComputePoolMax(sum, contributors, mult);
		Pools[team] = new Pool
		{
			Current = max,
			Max = max,
			Wiped = false,
			Seq = NextPoolSeq(),
			MemberMask = mask,
		};
		if (team >= 0 && team < RegenCounts.Length)
			RegenCounts[team] = 0f;
		return true;
	}

	/// <summary>
	/// Max = formula(living roster). Solo = full natural; 2+ = sum × mult.
	/// Death freezes shrink; join/leave recomputes (solo 400 + join 100 @ 0.75 → 375).
	/// </summary>
	private static bool SyncTeamMax(int team, float mult)
	{
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return false;

		int sum = SumNaturalMax(team, out int mask, out int living, out bool hasDead, includeDead: false);
		if (sum <= 0 || living <= 0)
			return false;

		int newMax = ComputePoolMax(sum, living, mult);

		if (newMax < pool.Max && hasDead)
		{
			pool.MemberMask = mask;
			Pools[team] = pool;
			return false;
		}

		if (newMax == pool.Max && mask == pool.MemberMask)
			return false;

		int newCurrent = pool.Max > 0
			? (int)Math.Round(pool.Current * (double)newMax / pool.Max)
			: newMax;
		pool.Current = Utils.Clamp(newCurrent, 0, newMax);
		pool.Max = newMax;
		pool.MemberMask = mask;
		pool.Seq = NextPoolSeq();
		Pools[team] = pool;
		return true;
	}

	private static uint NextPoolSeq() => ++_poolSeqCounter;

	/// <summary>Solo → natural (nearest 5). 2+ → sum × mult, nearest 5. Min 5.</summary>
	private static int ComputePoolMax(int sumNatural, int livingCount, float mult)
	{
		if (sumNatural <= 0)
			return 5;
		int raw = livingCount <= 1
			? sumNatural
			: (int)Math.Round(sumNatural * (double)mult);
		return Math.Max(5, RoundToNearest5(raw));
	}

	private static int RoundToNearest5(int value) =>
		(int)(Math.Round(value / 5.0) * 5);

	private static int SumNaturalMax(int team, out int mask, out int contributorCount, out bool hasDead,
		bool includeDead)
	{
		int sum = 0;
		mask = 0;
		contributorCount = 0;
		hasDead = false;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active || p.team != team)
				continue;
			if (p.dead)
			{
				hasDead = true;
				if (!includeDead)
					continue;
			}

			mask |= 1 << i;
			contributorCount++;
			sum += GetNaturalMax(p);
		}

		return sum;
	}

	private static int GetNaturalMax(Player p)
	{
		int n = p.GetModPlayer<SharedHealthPlayer>().NaturalMax;
		if (n > 0)
			return n;
		return Math.Max(1, p.statLifeMax);
	}

	private static bool IsLivingOnTeam(Player p, int team) =>
		p.active && !p.dead && p.team == team;

	private static void DisarmAll()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active)
				continue;

			SharedHealthPlayer sp = p.GetModPlayer<SharedHealthPlayer>();
			if (sp.NaturalMax <= 0)
				continue;

			int restoreMax = Math.Max(1, sp.NaturalMax);
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

			sp.ClearNatural();

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, p.whoAmI);
		}

		bool was = _armed;
		ResetPoolState();
		if (was && Main.netMode == NetmodeID.Server)
			BroadcastMetaDisarmed();
	}

	private static void DisarmClientOnly()
	{
		_client.Clear();
		ClearPending();
		if (Main.LocalPlayer.active)
			Main.LocalPlayer.GetModPlayer<SharedHealthPlayer>().ClearNatural();
	}

	private static void ResetAll()
	{
		ResetPoolState();
		DisarmClientOnly();
		SickJobs.Clear();
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			RecentEvents[i].Clear();
			RecentEventOrder[i].Clear();
		}
	}

	private static void ResetPoolState()
	{
		Pools.Clear();
		Array.Clear(RegenCounts);
		_armed = false;
		_wiping = false;
		_metaDirty = false;
		_heartbeat = 0;
		_wipeIgnoreWhoAmI = -1;
		_poolSeqCounter = 0;
	}

	private static void PaintServerMembers()
	{
		if (!_armed)
			return;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active || p.dead || !Teams.IsReal(p.team))
				continue;
			if (!Pools.TryGetValue(p.team, out Pool pool) || pool.Wiped || pool.Max <= 0)
				continue;

			// Same fruit-split paint for local UI and remote sim copies.
			int cur = Main.netMode == NetmodeID.SinglePlayer || p.whoAmI == Main.myPlayer
				? GetDisplayCurrent(p.team)
				: pool.Current;
			p.GetModPlayer<SharedHealthPlayer>().ApplyPoolLifePaint(cur, pool.Max);
		}
	}

	private static void ScheduleTeamSick(int team, int drinkerWho)
	{
		int duration = 3600;
		if (drinkerWho is >= 0 and < Main.maxPlayers)
		{
			Player drinker = Main.player[drinkerWho];
			if (drinker.active)
			{
				int idx = drinker.FindBuffIndex(BuffID.PotionSickness);
				if (idx >= 0)
					duration = Math.Max(1, drinker.buffTime[idx]);
			}
		}

		SickJobs.Add(new SickJob
		{
			Team = team,
			TicksLeft = PotionSickDelay,
			Duration = duration,
			SkipWho = drinkerWho,
		});
	}

	private static void TickSickJobs()
	{
		for (int i = SickJobs.Count - 1; i >= 0; i--)
		{
			SickJob job = SickJobs[i];
			job.TicksLeft--;
			if (job.TicksLeft > 0)
			{
				SickJobs[i] = job;
				continue;
			}

			SickJobs.RemoveAt(i);
			if (Main.netMode == NetmodeID.Server)
				BroadcastTeamSick(job.Team, job.Duration, job.SkipWho);
			ApplyTeamSickLocal(job.Team, job.Duration, job.SkipWho);
		}
	}

	private static void ApplyTeamSickLocal(int team, int duration, int skipWho)
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (i == skipWho)
				continue;
			Player p = Main.player[i];
			if (!IsLivingOnTeam(p, team))
				continue;
			if (Main.netMode == NetmodeID.MultiplayerClient && p.whoAmI != Main.myPlayer)
				continue;

			p.AddBuff(BuffID.PotionSickness, duration, quiet: false);
		}
	}

	private static void BroadcastTeamSick(int team, int duration, int skipWho)
	{
		ModPacket packet = Packets.Begin(Packets.SharedHealthTeamSick);
		packet.Write((byte)team);
		packet.Write(duration);
		packet.Write((byte)(skipWho < 0 ? 255 : skipWho));
		packet.Send();
	}

	private static void FlushMeta()
	{
		if (!_metaDirty)
			return;
		_metaDirty = false;
		BroadcastMeta();
	}

	private static void BroadcastMeta(int ackPlayer = -1, ushort ackEventId = 0)
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		for (int target = 0; target < Main.maxPlayers; target++)
		{
			if (!Main.player[target].active)
				continue;
			ushort ack = target == ackPlayer ? ackEventId : (ushort)0;
			SendMetaTo(target, ack);
		}
	}

	private static void BroadcastMetaDisarmed()
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		for (int target = 0; target < Main.maxPlayers; target++)
		{
			if (Main.player[target].active)
				SendMetaTo(target, ackEventId: 0);
		}
	}

	private static void SendMetaTo(int toClient, ushort ackEventId)
	{
		ModPacket packet = Packets.Begin(Packets.SharedHealthMeta);
		if (!_armed)
		{
			packet.Write((byte)0);
			packet.Send(toClient);
			return;
		}

		packet.Write((byte)1);
		packet.Write((byte)Pools.Count);
		foreach (KeyValuePair<int, Pool> kv in Pools)
		{
			packet.Write((byte)kv.Key);
			packet.Write(kv.Value.Seq);
			packet.Write(kv.Value.Current);
			packet.Write(kv.Value.Max);
			packet.Write(kv.Value.Wiped);
		}

		packet.Write(ackEventId);
		packet.Send(toClient);
	}

	private static void SendDamage(ushort eventId, int amount)
	{
		ModPacket packet = Packets.Begin(Packets.SharedHealthDamage);
		packet.Write(eventId);
		packet.Write(amount);
		packet.Send();
	}

	private static void SendHeal(ushort eventId, int amount, SharedHealCause cause)
	{
		ModPacket packet = Packets.Begin(Packets.SharedHealthHeal);
		packet.Write(eventId);
		packet.Write(amount);
		packet.Write((byte)cause);
		packet.Send();
	}

	private static ushort NextEventId()
	{
		ushort id = _nextEventId++;
		if (_nextEventId == 0)
			_nextEventId = 1;
		return id;
	}

	private static void PushPending(ushort id, int delta)
	{
		LocalPending.Add(new PendingEvent { Id = id, Delta = delta });
		_pendingDelta += delta;
	}

	private static void ClearPending()
	{
		LocalPending.Clear();
		_pendingDelta = 0;
	}

	private static bool AcceptEvent(int sender, ushort eventId)
	{
		if (sender is < 0 or >= Main.maxPlayers || eventId == 0)
			return false;

		uint key = eventId;
		HashSet<uint> set = RecentEvents[sender];
		if (!set.Add(key))
			return false;

		Queue<uint> order = RecentEventOrder[sender];
		order.Enqueue(key);
		while (order.Count > RecentEventCapacity)
		{
			uint old = order.Dequeue();
			set.Remove(old);
		}

		return true;
	}

	private static void ApplyAck(ushort ack)
	{
		for (int i = LocalPending.Count - 1; i >= 0; i--)
		{
			if (!PendingIdLessOrEqual(LocalPending[i].Id, ack))
				continue;
			_pendingDelta -= LocalPending[i].Delta;
			LocalPending.RemoveAt(i);
		}
	}

	private static bool PendingIdLessOrEqual(ushort id, ushort ack)
	{
		if (id == ack)
			return true;
		return (ushort)(ack - id) < 32768;
	}
}

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
	Heart = 2,
}

/// <summary>
/// Per-team shared HP pools (teams 1–5). Server/SP authoritative.
/// Clients predict via unacked event ids; meta carries poolSeq + ack id.
/// Bars are projectors only — never sensors. No per-tick PlayerLifeMana.
/// </summary>
public sealed class SharedHealthSystem : ModSystem
{
	private const int HeartbeatTicks = 45;
	private const int PotionSickDelay = 60;
	private const int MaxDamageEvent = 10_000;
	private const int MaxHealEvent = 500;
	private const int RecentEventCapacity = 64;

	private struct Pool
	{
		public int Current;
		public int Max;
		public bool Wiped;
		public uint Seq;
		/// <summary>Bitmask of living contributor slots at last max sync.</summary>
		public int MemberMask;
	}

	private struct PendingEvent
	{
		public ushort Id;
		public int Amount;
		public bool IsHeal;
	}

	private struct SickJob
	{
		public int Team;
		public int TicksLeft;
		public int Duration;
		public int SkipWho;
	}

	private static readonly Dictionary<int, Pool> Pools = new();
	private static readonly List<SickJob> SickJobs = new();
	private static readonly HashSet<uint>[] RecentEvents = new HashSet<uint>[Main.maxPlayers];
	private static readonly Queue<uint>[] RecentEventOrder = new Queue<uint>[Main.maxPlayers];

	private static bool _armed;
	private static bool _wiping;
	private static int _wipeIgnoreWhoAmI = -1;
	private static int _heartbeat;
	private static ushort _nextEventId = 1;
	/// <summary>Monotonic pool seq — never resets on wipe re-arm (clients drop meta if seq goes backwards).</summary>
	private static uint _poolSeqCounter;

	private static uint _lastSeq;
	private static int _serverCurrent;
	private static int _serverMax;
	private static bool _clientArmed;
	private static bool _clientWiped;
	private static int _clientTeam;
	private static readonly List<PendingEvent> LocalPending = new();

	static SharedHealthSystem()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			RecentEvents[i] = new HashSet<uint>();
			RecentEventOrder[i] = new Queue<uint>();
		}
	}

	public static bool IsEnabled() => ServerConfig.Instance.SharedHealthEnabled;

	public static bool IsBossesOnly() => ServerConfig.Instance.SharedHealthBossesOnly;

	public static bool ShouldBeActive() =>
		IsEnabled() && (!IsBossesOnly() || BossFightSystem.IsBossFightActive());

	public static bool IsArmed() =>
		Main.netMode == NetmodeID.MultiplayerClient ? _clientArmed : _armed;

	public static bool IsLinked(Player player) =>
		IsArmed()
		&& player.active
		&& !player.dead
		&& Teams.IsReal(player.team)
		&& !IsTeamWiped(player.team);

	public static bool IsTeamWiped(int team)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return _clientArmed && _clientWiped && _clientTeam == team;

		return _armed && Pools.TryGetValue(team, out Pool p) && p.Wiped;
	}

	public static bool IsPlayerHardLocked(Player player) =>
		player.active
		&& player.dead
		&& Teams.IsReal(player.team)
		&& IsTeamWiped(player.team)
		&& BossFightSystem.IsBossFightActive();

	/// <summary>True when a death should not spend boss-fight lives (shared wipe).</summary>
	public static bool SuppressLivesSpend(Player player) =>
		player.active
		&& Teams.IsReal(player.team)
		&& (Main.netMode == NetmodeID.MultiplayerClient
			? _clientArmed && _clientWiped && _clientTeam == player.team
			: _armed && Pools.TryGetValue(player.team, out Pool pool) && pool.Wiped);

	public static bool TryGetPool(int team, out int current, out int max)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (_clientArmed && _clientTeam == team && _serverMax > 0)
			{
				current = _serverCurrent;
				max = _serverMax;
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

	/// <summary>Bar projector value: server current ± local unacked prediction.</summary>
	public static int GetDisplayCurrent(int team)
	{
		if (!TryGetPool(team, out int serverCur, out int max) || max <= 0)
			return 0;

		if (Main.netMode != NetmodeID.MultiplayerClient || Main.LocalPlayer.team != team)
			return Utils.Clamp(serverCur, 0, max);

		int pendingDmg = 0;
		int pendingHeal = 0;
		for (int i = 0; i < LocalPending.Count; i++)
		{
			PendingEvent e = LocalPending[i];
			if (e.IsHeal)
				pendingHeal += e.Amount;
			else
				pendingDmg += e.Amount;
		}

		return Utils.Clamp(serverCur - pendingDmg + pendingHeal, 0, max);
	}

	/// <summary>
	/// Vanilla heart UI values: at most 20 hearts (≤400). Heart count = poolMax/20 (e.g. 365 → 18).
	/// Fill % matches the real pool so 300/600 paints as 200/400.
	/// Both statLifeMax and statLifeMax2 must be set to visualMax (see SharedHealthPlayer.ApplyVisualLifePaint).
	/// </summary>
	public static void GetVisualLife(int poolCurrent, int poolMax, out int visualCurrent, out int visualMax)
	{
		if (poolMax <= 0)
		{
			visualCurrent = 0;
			visualMax = 20;
			return;
		}

		// 20 HP per heart; never more than 20 hearts (no extra rows / life-fruit tier).
		int hearts = Utils.Clamp(poolMax / 20, 1, 20);
		visualMax = hearts * 20;
		visualCurrent = (int)Math.Round(poolCurrent * (double)visualMax / poolMax);
		visualCurrent = Utils.Clamp(visualCurrent, 0, visualMax);
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

		EnforceWipedTeams();
		PaintServerMembers();
		TickSickJobs();

		if (++_heartbeat >= HeartbeatTicks)
		{
			_heartbeat = 0;
			if (_armed)
				BroadcastMetaAll();
		}
	}

	public override void ClearWorld() => ResetAll();

	internal static void NotifyDamage(Player victim, int amount)
	{
		if (amount <= 0 || !IsLinked(victim))
			return;

		// Clients: predict + packet only. Server never applies from their OnHurt.
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (victim.whoAmI != Main.myPlayer)
				return;

			ushort id = NextEventId();
			int capped = Math.Min(amount, MaxDamageEvent);
			LocalPending.Add(new PendingEvent { Id = id, Amount = capped, IsHeal = false });
			SendDamage(id, capped);
			return;
		}

		// Dedicated/remote server copies: owning client sends Damage packet (avoid double-apply).
		// SP + listen-host local player: apply here (no client packet path).
		if (Main.netMode == NetmodeID.Server && victim.whoAmI != Main.myPlayer)
			return;

		ApplyDamage(victim.team, amount, eventId: 0, sender: victim.whoAmI);
	}

	internal static void NotifyHeal(Player healer, int amount, SharedHealCause cause)
	{
		if (amount <= 0 || !IsLinked(healer))
			return;

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			if (healer.whoAmI != Main.myPlayer)
				return;

			ushort id = NextEventId();
			int capped = Math.Min(amount, MaxHealEvent);
			LocalPending.Add(new PendingEvent { Id = id, Amount = capped, IsHeal = true });
			SendHeal(id, capped, cause);
			return;
		}

		// Same authority split as damage — remotes use Heal packet only.
		if (Main.netMode == NetmodeID.Server && healer.whoAmI != Main.myPlayer)
			return;

		ApplyHeal(healer.team, amount, cause, eventId: 0, sender: healer.whoAmI);
	}

	/// <summary>Client respawn: drop stale prediction so the bar snaps to the shared pool.</summary>
	internal static void NotifyLocalRespawn()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			LocalPending.Clear();
	}

	internal static void NotifyMemberDeath(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (!_armed || !Teams.IsReal(player.team))
			return;
		if (!Pools.TryGetValue(player.team, out Pool pool) || pool.Wiped)
			return;

		// One-shot / missed PreKill: any real death while pool live wipes the organism.
		BeginWipe(player.team, player.whoAmI);
	}

	internal static void HandleMetaPacket(BinaryReader reader)
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

		_clientArmed = true;
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
			bool wasWiped = _clientWiped;
			// Accept newer seq, or wipe→live re-arm even if seq ever went backwards.
			if (seq < _lastSeq && !(wasWiped && !wiped))
				continue;

			if (seq >= _lastSeq)
				_lastSeq = seq;
			_serverCurrent = current;
			_serverMax = max;
			_clientWiped = wiped;
			_clientTeam = team;
			// Wipe or re-arm: drop prediction tied to the previous organism.
			if (wiped || wasWiped)
				LocalPending.Clear();
		}

		ushort ack = reader.ReadUInt16();
		if (ack != 0)
			ApplyAck(ack);

		if (!sawLocal && Teams.IsReal(localTeam))
		{
			_serverCurrent = 0;
			_serverMax = 0;
			_clientWiped = false;
			_clientTeam = localTeam;
		}
	}

	internal static void HandleDamagePacket(BinaryReader reader, int sender)
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		ushort eventId = reader.ReadUInt16();
		int amount = reader.ReadInt32();
		if (sender is < 0 or >= Main.maxPlayers)
			return;

		Player p = Main.player[sender];
		if (!p.active || !Teams.IsReal(p.team))
			return;

		ApplyDamage(p.team, amount, eventId, sender);
	}

	internal static void HandleHealPacket(BinaryReader reader, int sender)
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		ushort eventId = reader.ReadUInt16();
		int amount = reader.ReadInt32();
		SharedHealCause cause = (SharedHealCause)reader.ReadByte();
		if (sender is < 0 or >= Main.maxPlayers)
			return;

		Player p = Main.player[sender];
		if (!p.active || !Teams.IsReal(p.team))
			return;

		ApplyHeal(p.team, amount, cause, eventId, sender);
	}

	internal static void HandlePotionSickRequest(BinaryReader reader, int sender)
	{
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

	private static void ApplyDamage(int team, int amount, ushort eventId, int sender)
	{
		if (!_armed || !Teams.IsReal(team))
			return;
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;
		if (eventId != 0 && !AcceptEvent(sender, eventId))
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
			BroadcastMetaAck(sender, eventId);
	}

	private static void ApplyHeal(int team, int amount, SharedHealCause cause, ushort eventId, int sender)
	{
		if (!_armed || !Teams.IsReal(team))
			return;
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;
		if (eventId != 0 && !AcceptEvent(sender, eventId))
			return;

		amount = Utils.Clamp(amount, 0, MaxHealEvent);
		int room = Math.Max(0, pool.Max - pool.Current);
		int applied = Math.Min(amount, room);
		if (applied > 0)
		{
			pool.Current += applied;
			pool.Seq = NextPoolSeq();
			Pools[team] = pool;
		}
		else if (eventId != 0)
		{
			pool.Seq = NextPoolSeq();
			Pools[team] = pool;
		}

		if (cause == SharedHealCause.Potion && applied > 0)
			ScheduleTeamSick(team, sender);

		BroadcastMetaAck(sender, eventId);
	}

	private static void BeginWipe(int team, int ignoreWhoAmI, int ackPlayer = -1, ushort ackEventId = 0)
	{
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return;

		pool.Current = 0;
		pool.Wiped = true;
		pool.Seq = NextPoolSeq();
		Pools[team] = pool;

		// Meta before kills so clients set _clientWiped and do not PreKill-cancel DeathLink.
		if (ackPlayer >= 0 && ackEventId != 0)
			BroadcastMetaAck(ackPlayer, ackEventId);
		else
			BroadcastMetaAll();

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
			if (!p.active || p.dead || p.team != team)
				continue;
			if (p.whoAmI == _wipeIgnoreWhoAmI)
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
		float mult = Utils.Clamp(ServerConfig.Instance.SharedHealthMultiplier, 0.5f, 2f);

		for (int team = Teams.Min; team <= Teams.Max; team++)
			TryArmTeam(team, mult, includeDeadContributors: false);

		// Rosters may still be settling the same tick; sync max immediately.
		for (int team = Teams.Min; team <= Teams.Max; team++)
			SyncTeamMax(team, mult);

		BroadcastMetaAll();
	}

	private static void RefreshRosters()
	{
		float mult = Utils.Clamp(ServerConfig.Instance.SharedHealthMultiplier, 0.5f, 2f);
		bool changed = false;
		for (int team = Teams.Min; team <= Teams.Max; team++)
		{
			if (Pools.TryGetValue(team, out Pool existing))
			{
				if (existing.Wiped)
				{
					// Outside boss: first living respawn re-arms. Count dead teammates so duo
					// stays at shared max (600) instead of snapping to solo natural.
					if (!BossFightSystem.IsBossFightActive() && HasLivingMember(team))
					{
						Pools.Remove(team);
						if (TryArmTeam(team, mult, includeDeadContributors: true))
							changed = true;
					}

					continue;
				}

				if (SyncTeamMax(team, mult))
					changed = true;
				continue;
			}

			if (TryArmTeam(team, mult, includeDeadContributors: false))
				changed = true;
		}

		if (changed)
			BroadcastMetaAll();
	}

	/// <summary>Returns true if a new pool was created.</summary>
	private static bool TryArmTeam(int team, float mult, bool includeDeadContributors)
	{
		if (Pools.ContainsKey(team))
			return false;
		if (!HasLivingMember(team))
			return false;

		int sum = SumNaturalMax(team, out int mask, out int contributors, includeDeadContributors);
		if (sum <= 0 || contributors <= 0)
			return false;

		// Solo: full natural max (no mult). 2+: round(Σ natural × mult) e.g. (400+400)×0.75 = 600
		int max = ComputePoolMax(sum, contributors, mult);
		Pools[team] = new Pool
		{
			Current = max,
			Max = max,
			Wiped = false,
			// Must advance global seq — re-arm after wipe must not reuse Seq=1 (clients ignore older seq).
			Seq = NextPoolSeq(),
			MemberMask = mask,
		};
		return true;
	}

	/// <summary>
	/// Keep Max = formula(living roster). Solo = full natural; 2+ = sum × mult.
	/// Death does not shrink Max (organism stays sized). Join/leave/respawn recomputes —
	/// so solo 400 + join 100 at 0.75 becomes 375, not stuck at 400.
	/// </summary>
	private static bool SyncTeamMax(int team, float mult)
	{
		if (!Pools.TryGetValue(team, out Pool pool) || pool.Wiped)
			return false;

		int sum = SumNaturalMax(team, out int mask, out int living, includeDead: false);
		if (sum <= 0 || living <= 0)
			return false;

		int newMax = ComputePoolMax(sum, living, mult);

		// Mid-fight death: keep Max/Current until wipe or roster truly changes without dead.
		if (newMax < pool.Max && HasActiveDeadTeammate(team))
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
		newCurrent = Utils.Clamp(newCurrent, 0, newMax);

		pool.Current = newCurrent;
		pool.Max = newMax;
		pool.MemberMask = mask;
		pool.Seq = NextPoolSeq();
		Pools[team] = pool;
		return true;
	}

	private static bool HasActiveDeadTeammate(int team)
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active && p.dead && p.team == team)
				return true;
		}

		return false;
	}

	private static uint NextPoolSeq() => ++_poolSeqCounter;

	/// <summary>Solo living member → natural sum (mult ignored). 2+ → sum × mult, nearest multiple of 5.</summary>
	private static int ComputePoolMax(int sumNatural, int livingCount, float mult)
	{
		if (sumNatural <= 0)
			return 5;
		if (livingCount <= 1)
			return Math.Max(5, RoundToNearest5(sumNatural));
		return Math.Max(5, RoundToNearest5((int)Math.Round(sumNatural * (double)mult)));
	}

	private static int RoundToNearest5(int value) =>
		(int)(Math.Round(value / 5.0) * 5);

	private static int SumNaturalMax(int team, out int mask, out int contributorCount, bool includeDead)
	{
		int sum = 0;
		mask = 0;
		contributorCount = 0;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active || p.team != team)
				continue;
			if (p.dead && !includeDead)
				continue;

			mask |= 1 << i;
			contributorCount++;
			sum += GetNaturalMax(p);
		}

		return sum;
	}

	/// <summary>True gear max — never the painted pool bar.</summary>
	private static int GetNaturalMax(Player p)
	{
		SharedHealthPlayer sp = p.GetModPlayer<SharedHealthPlayer>();
		if (sp.HasNatural && sp.NaturalMax > 0)
			return sp.NaturalMax;
		// Pre-snapshot fallback: base life crystals (statLifeMax2 may already be painted this tick).
		return Math.Max(1, p.statLifeMax);
	}

	private static bool HasLivingMember(int team)
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active && !p.dead && p.team == team)
				return true;
		}

		return false;
	}

	private static void DisarmAll()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (!p.active)
				continue;

			SharedHealthPlayer sp = p.GetModPlayer<SharedHealthPlayer>();
			if (!sp.HasNatural)
				continue;

			int restoreMax = Math.Max(1, sp.NaturalMax > 0 ? sp.NaturalMax : p.statLifeMax);
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
		_clientArmed = false;
		_clientWiped = false;
		_serverCurrent = 0;
		_serverMax = 0;
		_lastSeq = 0;
		_clientTeam = 0;
		LocalPending.Clear();
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
		_armed = false;
		_wiping = false;
		_heartbeat = 0;
		_wipeIgnoreWhoAmI = -1;
		// Clients reset _lastSeq on disarm meta / ClearWorld; counter may restart with them.
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

			// NaturalMax is snapshotted in PostUpdateEquips (pre-paint). Do not capture here.
			// Local UI (SP / listen host): visual heart count. Remotes: real pool for sim.
			bool localUi = Main.netMode == NetmodeID.SinglePlayer || p.whoAmI == Main.myPlayer;
			if (localUi)
			{
				GetVisualLife(pool.Current, pool.Max, out int vCur, out int vMax);
				p.GetModPlayer<SharedHealthPlayer>().ApplyVisualLifePaint(vCur, vMax);
			}
			else
			{
				p.statLifeMax2 = pool.Max;
				p.statLife = Utils.Clamp(pool.Current, 0, pool.Max);
			}
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
			if (!p.active || p.dead || p.team != team)
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

	private static void BroadcastMetaAll()
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		for (int target = 0; target < Main.maxPlayers; target++)
		{
			if (Main.player[target].active)
				SendMetaTo(target, ackEventId: 0);
		}
	}

	private static void BroadcastMetaAck(int ackPlayer, ushort ackEventId)
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

		ModPacket packet = Packets.Begin(Packets.SharedHealthMeta);
		packet.Write((byte)0);
		packet.Send();
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
			if (LocalPending[i].Id == ack || PendingIdLessOrEqual(LocalPending[i].Id, ack))
				LocalPending.RemoveAt(i);
		}
	}

	private static bool PendingIdLessOrEqual(ushort id, ushort ack)
	{
		if (id == ack)
			return true;
		ushort forward = (ushort)(ack - id);
		return forward < 32768;
	}
}

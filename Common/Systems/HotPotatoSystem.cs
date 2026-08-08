using System.Collections.Generic;
using System.IO;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Hot Potato: timed holder among living players; touch-pass; fuse kill + reassign.
/// Server/SP authoritative; clients mirror whoAmI + fuse seconds (world timer).
/// </summary>
public sealed class HotPotatoSystem : ModSystem
{
	private const int MinIntervalSeconds = 30;
	private const int MaxIntervalSeconds = 300;
	private const int IntervalStepSeconds = 15;
	private const int TicksPerSecond = 60;
	private const int PassBackImmuneTicks = 120;

	/// <summary>Outline + transfer popup accent.</summary>
	internal static readonly Color Accent = new(255, 220, 40);

	private static int _holderWhoAmI = -1;
	private static int _ticksLeft = -1;
	private static bool _wasActive;
	private static int _lastIntervalSeconds = -1;
	private static int _lastStatusSeconds = int.MinValue;
	private static int _displaySeconds = -1; // client-mirrored fuse seconds for world timer / outline
	private static int _lastSentWhoAmI = int.MinValue;
	private static int _passBackBlockedWhoAmI = -1;
	private static int _passBackImmuneTicksLeft;
	private static bool _killingHolder;

	private static readonly List<int> Pool = new(Main.maxPlayers);

	/// <summary>Mirrored on clients via <see cref="Packets.HotPotatoState"/>.</summary>
	internal static int HolderWhoAmI => _holderWhoAmI;

	/// <summary>Mirrored fuse seconds (−1 inactive). World timer + outline urgency.</summary>
	internal static int DisplaySeconds => _displaySeconds;

	internal static bool IsActive()
	{
		ServerConfig cfg = ServerConfig.Instance;
		if (!cfg.HotPotatoModeActive)
			return false;
		if (cfg.HotPotatoBossesOnly && !BossFightSystem.IsBossFightActive())
			return false;
		return true;
	}

	internal static bool IsHolder(Player player) =>
		player is not null
		&& IsActive()
		&& player.active
		&& !player.dead
		&& player.whoAmI == _holderWhoAmI;

	public override void OnWorldLoad() => ResetAll();

	public override void OnWorldUnload() => ResetAll();

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		ServerConfig cfg = ServerConfig.Instance;
		bool want = IsActive();
		int interval = SnapIntervalSeconds(cfg.HotPotatoIntervalSeconds);

		if (!want)
		{
			if (_wasActive || _holderWhoAmI >= 0 || _ticksLeft >= 0)
				Deactivate();
			return;
		}

		if (!_wasActive || interval != _lastIntervalSeconds)
		{
			_wasActive = true;
			_lastIntervalSeconds = interval;
			RestartFuse(interval);
			PickHolder(excludeWho: -1);
			PublishStatus(force: true);
			return;
		}

		if (!_killingHolder && !IsValidLiving(_holderWhoAmI))
		{
			RestartFuse(interval);
			PickHolder(excludeWho: _holderWhoAmI);
			PublishStatus(force: true);
			return;
		}

		TryPassByTouch();

		if (_passBackImmuneTicksLeft > 0 && --_passBackImmuneTicksLeft == 0)
			_passBackBlockedWhoAmI = -1;

		_ticksLeft--;
		if (_ticksLeft > 0)
		{
			PublishStatus();
			return;
		}

		int exploded = _holderWhoAmI;
		ExplodeHolder();
		RestartFuse(interval);
		PickHolder(excludeWho: exploded);
		PublishStatus(force: true);
	}

	internal static void HandleStatePacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		int who = reader.ReadInt32();
		int prev = _holderWhoAmI;
		_holderWhoAmI = who;
		if (who >= 0 && who != prev && who < Main.maxPlayers)
		{
			TransferPopup.Show(
				Main.player[who],
				"Mods.DefinitiveMultiplayer.UI.HotPotato.TransferPopup",
				Accent);
		}
	}

	internal static void HandleCountdownPacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		_displaySeconds = reader.ReadInt32();
	}

	private static void Deactivate()
	{
		_wasActive = false;
		_ticksLeft = -1;
		_lastIntervalSeconds = -1;
		ClearPassBackImmune();
		SetHolder(-1);
		PublishStatus(force: true, clear: true);
	}

	private static void ResetAll()
	{
		_holderWhoAmI = -1;
		_ticksLeft = -1;
		_wasActive = false;
		_lastIntervalSeconds = -1;
		_lastStatusSeconds = int.MinValue;
		_displaySeconds = -1;
		_lastSentWhoAmI = int.MinValue;
		TransferPopup.Clear();
		ClearPassBackImmune();
		_killingHolder = false;
	}

	private static void ClearPassBackImmune()
	{
		_passBackBlockedWhoAmI = -1;
		_passBackImmuneTicksLeft = 0;
	}

	private static void RestartFuse(int intervalSeconds)
	{
		_ticksLeft = intervalSeconds * TicksPerSecond;
		ClearPassBackImmune();
	}

	private static int SnapIntervalSeconds(int raw)
	{
		int clamped = Utils.Clamp(raw, MinIntervalSeconds, MaxIntervalSeconds);
		int steps = (clamped - MinIntervalSeconds + IntervalStepSeconds / 2) / IntervalStepSeconds;
		return MinIntervalSeconds + steps * IntervalStepSeconds;
	}

	private static bool IsValidLiving(int who) =>
		who >= 0 && who < Main.maxPlayers
		&& Main.player[who].active
		&& !Main.player[who].dead;

	private static bool IsInPool(Player p)
	{
		if (p?.active != true || p.dead)
			return false;

		if (!ServerConfig.Instance.HotPotatoTeamOnly)
			return true;

		if (!Teams.IsReal(p.team))
			return false;

		if (!IsValidLiving(_holderWhoAmI))
			return true;

		return p.team == Main.player[_holderWhoAmI].team;
	}

	private static void CollectPool()
	{
		Pool.Clear();
		bool teamOnly = ServerConfig.Instance.HotPotatoTeamOnly;
		// Team-only with no holder: lock to first living real team so we don't mix colors.
		int lockTeam = teamOnly && IsValidLiving(_holderWhoAmI)
			? Main.player[_holderWhoAmI].team
			: -1;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p?.active != true || p.dead)
				continue;

			if (teamOnly)
			{
				if (!Teams.IsReal(p.team))
					continue;
				if (lockTeam < 0)
					lockTeam = p.team;
				else if (p.team != lockTeam)
					continue;
			}

			Pool.Add(i);
		}
	}

	private static void PickHolder(int excludeWho)
	{
		CollectPool();
		if (Pool.Count == 0)
		{
			SetHolder(-1);
			return;
		}

		if (excludeWho >= 0)
		{
			int idx = Pool.IndexOf(excludeWho);
			if (idx >= 0 && Pool.Count > 1)
				Pool.RemoveAt(idx);
		}

		SetHolder(Pool[Main.rand.Next(Pool.Count)]);
	}

	private static void TryPassByTouch()
	{
		if (!IsValidLiving(_holderWhoAmI))
			return;

		Player holder = Main.player[_holderWhoAmI];
		Rectangle hit = holder.Hitbox;

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (i == _holderWhoAmI)
				continue;
			if (_passBackImmuneTicksLeft > 0 && i == _passBackBlockedWhoAmI)
				continue;
			if (!IsInPool(Main.player[i]))
				continue;
			if (!hit.Intersects(Main.player[i].Hitbox))
				continue;

			PassTo(i);
			return;
		}
	}

	private static void PassTo(int newWho)
	{
		int prev = _holderWhoAmI;
		SetHolder(newWho);
		_passBackBlockedWhoAmI = prev;
		_passBackImmuneTicksLeft = PassBackImmuneTicks;
	}

	private static void ExplodeHolder()
	{
		int who = _holderWhoAmI;
		if (!IsValidLiving(who))
			return;

		Player p = Main.player[who];
		_killingHolder = true;
		try
		{
			PlayerDeathReason reason = PlayerDeathReason.ByCustomReason(
				NetworkText.FromKey("Mods.DefinitiveMultiplayer.UI.HotPotato.Exploded", p.name));
			p.statLife = 0;
			p.KillMe(reason, 9999.0, 0);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendPlayerDeath(p.whoAmI, reason, 9999, 0, pvp: false);
		}
		finally
		{
			_killingHolder = false;
		}
	}

	private static void SetHolder(int whoAmI)
	{
		if (_holderWhoAmI == whoAmI && _lastSentWhoAmI == whoAmI)
			return;

		int prev = _holderWhoAmI;
		_holderWhoAmI = whoAmI;
		SendState(whoAmI);

		// SP / listen host (clients popup from HotPotatoState packet).
		if (!Main.dedServ && whoAmI >= 0 && whoAmI != prev && whoAmI < Main.maxPlayers)
		{
			TransferPopup.Show(
				Main.player[whoAmI],
				"Mods.DefinitiveMultiplayer.UI.HotPotato.TransferPopup",
				Accent);
		}
	}

	private static void SendState(int whoAmI)
	{
		_lastSentWhoAmI = whoAmI;

		if (Main.netMode != NetmodeID.Server)
			return;

		ModPacket packet = Packets.Begin(Packets.HotPotatoState);
		packet.Write(whoAmI);
		packet.Send();
	}

	private static int SecondsFromTicks(int ticks) =>
		ticks <= 0 ? 0 : (ticks + 59) / 60;

	/// <summary>Sync fuse seconds for world timer + outline (no chat).</summary>
	private static void PublishStatus(bool force = false, bool clear = false)
	{
		int seconds = clear ? -1 : SecondsFromTicks(_ticksLeft);

		if (!force && seconds == _lastStatusSeconds)
			return;

		_lastStatusSeconds = seconds;
		_displaySeconds = seconds;

		if (Main.netMode == NetmodeID.Server)
		{
			// Late joiners pick up holder + countdown within one second change.
			if (seconds >= 0)
				SendState(_holderWhoAmI);

			ModPacket packet = Packets.Begin(Packets.HotPotatoCountdown);
			packet.Write(seconds);
			packet.Send();
		}
	}
}

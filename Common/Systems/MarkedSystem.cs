using System.Collections.Generic;
using System.IO;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Boss-only mark rotation among living players. Server/SP authoritative; clients mirror whoAmI + seconds.
/// </summary>
public sealed class MarkedSystem : ModSystem
{
	private const int MinIntervalSeconds = 15;
	private const int MaxIntervalSeconds = 120;
	private const int IntervalStepSeconds = 5;
	private const int TicksPerSecond = 60;
	internal const int CountdownVisibleSeconds = 5;

	/// <summary>Outline + transfer popup accent.</summary>
	internal static readonly Color Accent = new(255, 36, 36);

	private static int _markedWhoAmI = -1;
	private static int _ticksLeft = -1;
	private static bool _wasActive;
	private static int _lastIntervalSeconds = -1;
	private static int _lastPublishedSeconds = int.MinValue;
	private static int _lastSentWhoAmI = int.MinValue;
	private static int _displaySeconds = -1;

	private static readonly List<int> Living = new(Main.maxPlayers);

	internal static bool IsActive() =>
		ServerConfig.Instance.MarkedModeActive && BossFightSystem.IsBossFightActive();

	internal static bool IsMarked(Player player) =>
		player is not null
		&& player.whoAmI == _markedWhoAmI
		&& _markedWhoAmI >= 0
		&& IsActive()
		&& player.active
		&& !player.dead;

	/// <summary>Mirrored seconds (−1 inactive). Outline urgency + last-5s world timer.</summary>
	internal static int DisplaySeconds => _displaySeconds;

	public override void OnWorldLoad() => ResetAll();

	public override void OnWorldUnload() => ResetAll();

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		bool want = IsActive();
		int interval = SnapIntervalSeconds(ServerConfig.Instance.MarkedIntervalSeconds);

		if (!want)
		{
			if (_wasActive || _markedWhoAmI >= 0 || _ticksLeft >= 0)
				Deactivate();
			return;
		}

		if (!_wasActive || interval != _lastIntervalSeconds)
		{
			_wasActive = true;
			_lastIntervalSeconds = interval;
			_ticksLeft = interval * TicksPerSecond;
			PickMark(excludeWho: -1);
			PublishSeconds(SecondsFromTicks(_ticksLeft));
			return;
		}

		// Mark left / died → immediate re-pick from living pool.
		if (!IsValidMark(_markedWhoAmI))
			PickMark(excludeWho: -1);

		_ticksLeft--;
		if (_ticksLeft > 0)
		{
			PublishSeconds(SecondsFromTicks(_ticksLeft));
			return;
		}

		_ticksLeft = interval * TicksPerSecond;
		PickMark(excludeWho: _markedWhoAmI);
		PublishSeconds(SecondsFromTicks(_ticksLeft));
	}

	internal static void HandleStatePacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		int who = reader.ReadInt32();
		int prev = _markedWhoAmI;
		_markedWhoAmI = who;
		if (who >= 0 && who != prev && who < Main.maxPlayers)
		{
			TransferPopup.Show(
				Main.player[who],
				"Mods.DefinitiveMultiplayer.UI.Marked.TransferPopup",
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
		SetMark(-1);
		PublishSeconds(-1);
	}

	private static void ResetAll()
	{
		_markedWhoAmI = -1;
		_ticksLeft = -1;
		_wasActive = false;
		_lastIntervalSeconds = -1;
		_lastPublishedSeconds = int.MinValue;
		_lastSentWhoAmI = int.MinValue;
		_displaySeconds = -1;
		TransferPopup.Clear();
	}

	private static int SnapIntervalSeconds(int raw)
	{
		int clamped = Utils.Clamp(raw, MinIntervalSeconds, MaxIntervalSeconds);
		int steps = (clamped - MinIntervalSeconds + IntervalStepSeconds / 2) / IntervalStepSeconds;
		return MinIntervalSeconds + steps * IntervalStepSeconds;
	}

	private static bool IsValidMark(int who) =>
		who >= 0 && who < Main.maxPlayers
		&& Main.player[who].active
		&& !Main.player[who].dead;

	private static void CollectLiving()
	{
		Living.Clear();
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p?.active == true && !p.dead)
				Living.Add(i);
		}
	}

	private static void PickMark(int excludeWho)
	{
		CollectLiving();
		if (Living.Count == 0)
		{
			SetMark(-1);
			return;
		}

		// Prefer someone other than excludeWho when possible.
		if (excludeWho >= 0)
		{
			int idx = Living.IndexOf(excludeWho);
			if (idx >= 0 && Living.Count > 1)
				Living.RemoveAt(idx);
		}

		SetMark(Living[Main.rand.Next(Living.Count)]);
	}

	private static void SetMark(int whoAmI)
	{
		if (_markedWhoAmI == whoAmI && _lastSentWhoAmI == whoAmI)
			return;

		int prev = _markedWhoAmI;
		_markedWhoAmI = whoAmI;
		SendState(whoAmI);

		// SP / listen host (clients popup from MarkedState packet).
		if (!Main.dedServ && whoAmI >= 0 && whoAmI != prev && whoAmI < Main.maxPlayers)
		{
			TransferPopup.Show(
				Main.player[whoAmI],
				"Mods.DefinitiveMultiplayer.UI.Marked.TransferPopup",
				Accent);
		}
	}

	private static void SendState(int whoAmI)
	{
		_lastSentWhoAmI = whoAmI;

		if (Main.netMode != NetmodeID.Server)
			return;

		ModPacket packet = Packets.Begin(Packets.MarkedState);
		packet.Write(whoAmI);
		packet.Send();
	}

	private static int SecondsFromTicks(int ticks) =>
		ticks <= 0 ? 0 : (ticks + 59) / 60;

	private static void PublishSeconds(int seconds)
	{
		if (seconds == _lastPublishedSeconds)
			return;

		_lastPublishedSeconds = seconds;

		if (!Main.dedServ)
			_displaySeconds = seconds;

		if (Main.netMode == NetmodeID.Server)
		{
			// Late joiners pick up mark + countdown within one second change.
			if (seconds >= 0)
				SendState(_markedWhoAmI);

			ModPacket packet = Packets.Begin(Packets.MarkedCountdown);
			packet.Write(seconds);
			packet.Send();
		}
	}
}

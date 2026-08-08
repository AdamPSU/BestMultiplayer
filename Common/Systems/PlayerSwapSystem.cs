using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Drawing;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Periodically shuffles living teammates' positions (server/SP). Each team is independent.
/// Local chat shows a single in-place countdown line while the mode is on.
/// </summary>
public sealed class PlayerSwapSystem : ModSystem
{
	private const int MinIntervalMinutes = 1;
	private const int MaxIntervalMinutes = 30;
	private const int TicksPerMinute = 60 * 60;
	private const int ChatLineLife = 600;
	/// <summary>Only show the chat countdown in the final N seconds.</summary>
	private const int CountdownVisibleSeconds = 5;
	/// <summary>Gold countdown number (chat tag RRGGBB).</summary>
	private const string CountdownColorHex = "FFD700";

	private static int _ticksLeft = -1;
	private static bool _wasEnabled;
	private static int _lastIntervalMinutes = -1;
	private static int _lastPublishedSeconds = int.MinValue;

	private static readonly List<int> Members = new(Main.maxPlayers);
	private static readonly List<Vector2> Positions = new(Main.maxPlayers);
	private static readonly int[] Perm = new int[Main.maxPlayers];

	private static FieldInfo _timeLeftField;
	private static ChatMessageContainer _countdownLine;

	public override void OnWorldLoad() => ResetTimer();

	public override void OnWorldUnload()
	{
		ClearCountdownChat();
		ResetTimer();
	}

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		ServerConfig cfg = ServerConfig.Instance;
		bool enabled = cfg.PlayerSwapModeActive;
		int interval = Utils.Clamp(cfg.PlayerSwapIntervalMinutes, MinIntervalMinutes, MaxIntervalMinutes);

		if (!enabled)
		{
			if (_wasEnabled || _ticksLeft >= 0)
				PublishSeconds(-1);

			_wasEnabled = false;
			_ticksLeft = -1;
			_lastIntervalMinutes = -1;
			return;
		}

		if (!_wasEnabled || interval != _lastIntervalMinutes || _ticksLeft < 0)
		{
			_wasEnabled = true;
			_lastIntervalMinutes = interval;
			_ticksLeft = interval * TicksPerMinute;
			PublishSeconds(SecondsFromTicks(_ticksLeft));
			return;
		}

		_ticksLeft--;
		if (_ticksLeft > 0)
		{
			PublishSeconds(SecondsFromTicks(_ticksLeft));
			return;
		}

		_ticksLeft = interval * TicksPerMinute;
		SwapAllTeams();
		PublishSeconds(SecondsFromTicks(_ticksLeft));
	}

	public override void PostUpdateEverything()
	{
		// Keep the single countdown line from fading between second ticks.
		if (Main.dedServ || _countdownLine == null)
			return;

		if (!ChatMonitorAccess.TryGetMessages(out List<ChatMessageContainer> messages) || !messages.Contains(_countdownLine))
		{
			_countdownLine = null;
			return;
		}

		SetTimeLeft(_countdownLine, ChatLineLife);
	}

	internal static void ResetTimer()
	{
		_ticksLeft = -1;
		_wasEnabled = false;
		_lastIntervalMinutes = -1;
		_lastPublishedSeconds = int.MinValue;
	}

	internal static void HandleCountdownPacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		int seconds = reader.ReadInt32();
		ApplyCountdownSeconds(seconds);
	}

	private static int SecondsFromTicks(int ticks) =>
		ticks <= 0 ? 0 : (ticks + 59) / 60;

	private static void PublishSeconds(int seconds)
	{
		if (seconds == _lastPublishedSeconds)
			return;

		_lastPublishedSeconds = seconds;

		if (!Main.dedServ)
			ApplyCountdownSeconds(seconds);

		if (Main.netMode == NetmodeID.Server)
			SendCountdown(seconds);
	}

	private static void SendCountdown(int seconds)
	{
		ModPacket packet = Packets.Begin(Packets.PlayerSwapCountdown);
		packet.Write(seconds);
		packet.Send();
	}

	private static void ApplyCountdownSeconds(int seconds)
	{
		if (Main.dedServ)
			return;

		if (seconds < 0 || seconds > CountdownVisibleSeconds)
		{
			ClearCountdownChat();
			return;
		}

		string time = PlayerWorldTimer.FormatTime(seconds);
		string colored = $"[c/{CountdownColorHex}:{time}]";
		string text = Language.GetTextValue("Mods.DefinitiveMultiplayer.UI.PlayerSwap.Countdown", colored);
		UpsertCountdownChat(text);
	}

	private static void UpsertCountdownChat(string text)
	{
		if (!ChatMonitorAccess.TryGetMessages(out List<ChatMessageContainer> messages))
		{
			Main.NewText(text);
			return;
		}

		if (_countdownLine != null && messages.Contains(_countdownLine))
		{
			_countdownLine.OriginalText = text;
			_countdownLine.MarkToNeedRefresh();
			SetTimeLeft(_countdownLine, ChatLineLife);
			return;
		}

		Main.NewText(text);
		if (messages.Count > 0)
		{
			_countdownLine = messages[0];
			_countdownLine.OriginalText = text;
			_countdownLine.MarkToNeedRefresh();
			SetTimeLeft(_countdownLine, ChatLineLife);
		}
	}

	private static void ClearCountdownChat()
	{
		if (_countdownLine == null)
			return;

		if (ChatMonitorAccess.TryGetMessages(out List<ChatMessageContainer> messages) && messages.Contains(_countdownLine))
		{
			_countdownLine.OriginalText = string.Empty;
			_countdownLine.MarkToNeedRefresh();
			SetTimeLeft(_countdownLine, 0);
		}

		_countdownLine = null;
	}

	private static void SetTimeLeft(ChatMessageContainer line, int ticks)
	{
		_timeLeftField ??= typeof(ChatMessageContainer).GetField("_timeLeft", BindingFlags.Instance | BindingFlags.NonPublic);
		_timeLeftField?.SetValue(line, ticks);
	}

	private static void SwapAllTeams()
	{
		for (int team = Teams.Min; team <= Teams.Max; team++)
			SwapTeam(team);
	}

	private static void SwapTeam(int team)
	{
		Members.Clear();
		Positions.Clear();

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p?.active != true || p.dead || p.team != team)
				continue;

			Members.Add(i);
			Positions.Add(p.position);
		}

		int n = Members.Count;
		if (n < 2)
			return;

		// Sattolo: single n-cycle derangement so nobody stays put.
		for (int i = 0; i < n; i++)
			Perm[i] = i;

		for (int i = n - 1; i > 0; i--)
		{
			int j = Main.rand.Next(i);
			(Perm[i], Perm[j]) = (Perm[j], Perm[i]);
		}

		for (int i = 0; i < n; i++)
			TeleportPlayer(Main.player[Members[i]], Positions[Perm[i]]);
	}

	private static void TeleportPlayer(Player player, Vector2 pos)
	{
		int style = TeleportationStyleID.TeleportationPotion;
		player.Teleport(pos, style);
		player.velocity = Vector2.Zero;
		player.fallStart = (int)(player.position.Y / 16f);

		if (Main.netMode == NetmodeID.Server)
		{
			RemoteClient.CheckSection(player.whoAmI, pos);
			NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, pos.X, pos.Y, style);
		}
	}
}

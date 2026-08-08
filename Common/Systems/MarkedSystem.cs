using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Boss-only mark rotation among living players. Server/SP authoritative; clients mirror whoAmI + countdown chat.
/// </summary>
public sealed class MarkedSystem : ModSystem
{
	private const int MinIntervalSeconds = 15;
	private const int MaxIntervalSeconds = 120;
	private const int IntervalStepSeconds = 5;
	private const int TicksPerSecond = 60;
	private const int ChatLineLife = 600;
	private const int CountdownVisibleSeconds = 5;
	private const string MarkColorHex = "FF4444";

	private static int _markedWhoAmI = -1;
	private static int _ticksLeft = -1;
	private static bool _wasActive;
	private static int _lastIntervalSeconds = -1;
	private static int _lastPublishedSeconds = int.MinValue;
	private static int _lastSentWhoAmI = int.MinValue;

	private static readonly List<int> Living = new(Main.maxPlayers);

	private static FieldInfo _messagesField;
	private static FieldInfo _timeLeftField;
	private static ChatMessageContainer _countdownLine;

	internal static bool IsActive()
	{
		ServerConfig cfg = ServerConfig.Instance;
		// Mutually exclusive with Hot Potato (shared in-place chat line).
		return cfg.MarkedEnabled && !cfg.HotPotatoEnabled && BossFightSystem.IsBossFightActive();
	}

	internal static bool IsMarked(Player player) =>
		player is not null
		&& player.whoAmI == _markedWhoAmI
		&& _markedWhoAmI >= 0
		&& IsActive()
		&& player.active
		&& !player.dead;

	public override void OnWorldLoad() => ResetAll();

	public override void OnWorldUnload()
	{
		ClearCountdownChat();
		ResetAll();
	}

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

	public override void PostUpdateEverything()
	{
		if (Main.dedServ || _countdownLine == null)
			return;

		if (!TryGetMessages(out List<ChatMessageContainer> messages) || !messages.Contains(_countdownLine))
		{
			_countdownLine = null;
			return;
		}

		SetTimeLeft(_countdownLine, ChatLineLife);
	}

	internal static void HandleStatePacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		int who = reader.ReadInt32();
		int prev = _markedWhoAmI;
		_markedWhoAmI = who;
		if (who >= 0 && who != prev)
			AnnounceMark(who);
	}

	internal static void HandleCountdownPacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		ApplyCountdownSeconds(reader.ReadInt32());
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

		// SP / listen host (clients announce from MarkedState packet).
		if (!Main.dedServ && whoAmI >= 0 && whoAmI != prev)
			AnnounceMark(whoAmI);
	}

	private static void AnnounceMark(int whoAmI)
	{
		if (Main.dedServ || whoAmI < 0 || whoAmI >= Main.maxPlayers)
			return;

		Player p = Main.player[whoAmI];
		if (!p.active)
			return;

		// Finish countdown line first so this is a fresh chat entry.
		ClearCountdownChat();
		_lastPublishedSeconds = int.MinValue;

		string name = $"[c/{MarkColorHex}:{p.name}]";
		string text = Language.GetTextValue("Mods.DefinitiveMultiplayer.UI.Marked.Assigned", name);
		Main.NewText(text);
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
			ApplyCountdownSeconds(seconds);

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

	private static void ApplyCountdownSeconds(int seconds)
	{
		if (Main.dedServ)
			return;

		if (seconds < 0 || seconds > CountdownVisibleSeconds)
		{
			ClearCountdownChat();
			return;
		}

		string time = FormatTime(seconds);
		string colored = $"[c/{MarkColorHex}:{time}]";
		string text = Language.GetTextValue("Mods.DefinitiveMultiplayer.UI.Marked.Countdown", colored);
		UpsertCountdownChat(text);
	}

	private static string FormatTime(int totalSeconds)
	{
		if (totalSeconds < 60)
			return totalSeconds.ToString();

		int m = totalSeconds / 60;
		int s = totalSeconds % 60;
		return $"{m}:{s:D2}";
	}

	private static void UpsertCountdownChat(string text)
	{
		if (!TryGetMessages(out List<ChatMessageContainer> messages))
		{
			Main.NewText(text);
			return;
		}

		if (_countdownLine != null && messages.Contains(_countdownLine))
		{
			WriteCountdownLine(_countdownLine, text, ChatLineLife);
			return;
		}

		Main.NewText(text);
		if (messages.Count > 0)
		{
			_countdownLine = messages[0];
			WriteCountdownLine(_countdownLine, text, ChatLineLife);
		}
	}

	private static void ClearCountdownChat()
	{
		if (_countdownLine == null)
			return;

		if (TryGetMessages(out List<ChatMessageContainer> messages) && messages.Contains(_countdownLine))
			WriteCountdownLine(_countdownLine, string.Empty, 0);

		_countdownLine = null;
	}

	private static void WriteCountdownLine(ChatMessageContainer line, string text, int life)
	{
		line.OriginalText = text;
		line.MarkToNeedRefresh();
		SetTimeLeft(line, life);
	}

	private static bool TryGetMessages(out List<ChatMessageContainer> messages)
	{
		messages = null;
		if (Main.chatMonitor is not RemadeChatMonitor monitor)
			return false;

		_messagesField ??= typeof(RemadeChatMonitor).GetField("_messages", BindingFlags.Instance | BindingFlags.NonPublic);
		if (_messagesField?.GetValue(monitor) is not List<ChatMessageContainer> list)
			return false;

		messages = list;
		return true;
	}

	private static void SetTimeLeft(ChatMessageContainer line, int ticks)
	{
		_timeLeftField ??= typeof(ChatMessageContainer).GetField("_timeLeft", BindingFlags.Instance | BindingFlags.NonPublic);
		_timeLeftField?.SetValue(line, ticks);
	}
}

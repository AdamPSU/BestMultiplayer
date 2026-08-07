using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Hot Potato: timed holder among living players; touch-pass; fuse kill + reassign.
/// Server/SP authoritative; clients mirror whoAmI + countdown chat.
/// </summary>
public sealed class HotPotatoSystem : ModSystem
{
	private const int MinIntervalSeconds = 30;
	private const int MaxIntervalSeconds = 300;
	private const int IntervalStepSeconds = 15;
	private const int TicksPerSecond = 60;
	private const int PassBackImmuneTicks = 120;
	private const int ChatLineLife = 600;
	private const string PotatoColorHex = "FFAA33";

	private static int _holderWhoAmI = -1;
	private static int _ticksLeft = -1;
	private static bool _wasActive;
	private static int _lastIntervalSeconds = -1;
	private static int _lastStatusWhoAmI = int.MinValue;
	private static int _lastStatusSeconds = int.MinValue;
	private static int _displaySeconds = -1; // client-mirrored fuse seconds for status line
	private static int _lastSentWhoAmI = int.MinValue;
	private static int _passBackBlockedWhoAmI = -1;
	private static int _passBackImmuneTicksLeft;
	private static bool _killingHolder;

	private static readonly List<int> Pool = new(Main.maxPlayers);

	private static FieldInfo _messagesField;
	private static FieldInfo _timeLeftField;
	private static ChatMessageContainer _countdownLine;

	/// <summary>Mirrored on clients via <see cref="Packets.HotPotatoState"/>.</summary>
	internal static int HolderWhoAmI => _holderWhoAmI;

	internal static bool IsActive()
	{
		ServerConfig cfg = ServerConfig.Instance;
		// Mutually exclusive with Marked (shared in-place chat line).
		if (!cfg.HotPotatoEnabled || cfg.MarkedEnabled)
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

	public override void OnWorldUnload()
	{
		ClearCountdownChat();
		ResetAll();
	}

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
			_ticksLeft = interval * TicksPerSecond;
			_passBackBlockedWhoAmI = -1;
			_passBackImmuneTicksLeft = 0;
			PickHolder(excludeWho: -1);
			PublishStatus(force: true);
			return;
		}

		if (!_killingHolder && !IsValidLiving(_holderWhoAmI))
		{
			_ticksLeft = interval * TicksPerSecond;
			PickHolder(excludeWho: _holderWhoAmI);
			_passBackBlockedWhoAmI = -1;
			_passBackImmuneTicksLeft = 0;
			PublishStatus(force: true);
			return;
		}

		TryPassByTouch();

		if (_passBackImmuneTicksLeft > 0)
		{
			_passBackImmuneTicksLeft--;
			if (_passBackImmuneTicksLeft <= 0)
				_passBackBlockedWhoAmI = -1;
		}

		_ticksLeft--;
		if (_ticksLeft > 0)
		{
			PublishStatus();
			return;
		}

		int exploded = _holderWhoAmI;
		ExplodeHolder();
		_ticksLeft = interval * TicksPerSecond;
		_passBackBlockedWhoAmI = -1;
		_passBackImmuneTicksLeft = 0;
		PickHolder(excludeWho: exploded);
		PublishStatus(force: true);
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

		_holderWhoAmI = reader.ReadInt32();
		ApplyStatusLine(_holderWhoAmI, _displaySeconds, force: true);
	}

	internal static void HandleCountdownPacket(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
			return;

		_displaySeconds = reader.ReadInt32();
		ApplyStatusLine(_holderWhoAmI, _displaySeconds);
	}

	private static void Deactivate()
	{
		_wasActive = false;
		_ticksLeft = -1;
		_lastIntervalSeconds = -1;
		_passBackBlockedWhoAmI = -1;
		_passBackImmuneTicksLeft = 0;
		SetHolder(-1);
		PublishStatus(force: true, clear: true);
	}

	private static void ResetAll()
	{
		_holderWhoAmI = -1;
		_ticksLeft = -1;
		_wasActive = false;
		_lastIntervalSeconds = -1;
		_lastStatusWhoAmI = int.MinValue;
		_lastStatusSeconds = int.MinValue;
		_displaySeconds = -1;
		_lastSentWhoAmI = int.MinValue;
		_passBackBlockedWhoAmI = -1;
		_passBackImmuneTicksLeft = 0;
		_killingHolder = false;
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
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (IsInPool(Main.player[i]))
				Pool.Add(i);
		}

		// Team-only with no holder: lock to one team so we don't mix colors.
		if (ServerConfig.Instance.HotPotatoTeamOnly && Pool.Count > 0 && !IsValidLiving(_holderWhoAmI))
		{
			int team = Main.player[Pool[0]].team;
			for (int i = Pool.Count - 1; i >= 0; i--)
			{
				if (Main.player[Pool[i]].team != team)
					Pool.RemoveAt(i);
			}
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

		_holderWhoAmI = whoAmI;
		SendState(whoAmI, force: true);
	}

	private static void SendState(int whoAmI, bool force = false)
	{
		if (!force && _lastSentWhoAmI == whoAmI)
			return;

		_lastSentWhoAmI = whoAmI;

		if (Main.netMode != NetmodeID.Server)
			return;

		ModPacket packet = Packets.Begin(Packets.HotPotatoState);
		packet.Write(whoAmI);
		packet.Send();
	}

	private static int SecondsFromTicks(int ticks) =>
		ticks <= 0 ? 0 : (ticks + 59) / 60;

	/// <summary>
	/// Single in-place chat line: "{name} has the potato! {timer}" — always visible while active.
	/// </summary>
	private static void PublishStatus(bool force = false, bool clear = false)
	{
		int seconds = clear ? -1 : SecondsFromTicks(_ticksLeft);
		int who = clear ? -1 : _holderWhoAmI;

		if (!force && who == _lastStatusWhoAmI && seconds == _lastStatusSeconds)
			return;

		_lastStatusWhoAmI = who;
		_lastStatusSeconds = seconds;
		_displaySeconds = seconds;

		if (!Main.dedServ)
			ApplyStatusLine(who, seconds, force: true);

		if (Main.netMode == NetmodeID.Server)
		{
			if (seconds >= 0)
				SendState(_holderWhoAmI, force: true);

			ModPacket packet = Packets.Begin(Packets.HotPotatoCountdown);
			packet.Write(seconds);
			packet.Send();
		}
	}

	private static void ApplyStatusLine(int whoAmI, int seconds, bool force = false)
	{
		if (Main.dedServ)
			return;

		if (seconds < 0 || whoAmI < 0 || whoAmI >= Main.maxPlayers || !Main.player[whoAmI].active)
		{
			ClearCountdownChat();
			_lastStatusWhoAmI = int.MinValue;
			_lastStatusSeconds = int.MinValue;
			return;
		}

		if (!force && whoAmI == _lastStatusWhoAmI && seconds == _lastStatusSeconds)
			return;

		_lastStatusWhoAmI = whoAmI;
		_lastStatusSeconds = seconds;

		Player p = Main.player[whoAmI];
		string name = $"[c/{PotatoColorHex}:{p.name}]";
		string time = $"[c/{PotatoColorHex}:{FormatTime(seconds)}]";
		string text = Language.GetTextValue("Mods.DefinitiveMultiplayer.UI.HotPotato.Status", name, time);
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

		if (TryGetMessages(out List<ChatMessageContainer> messages) && messages.Contains(_countdownLine))
		{
			_countdownLine.OriginalText = string.Empty;
			_countdownLine.MarkToNeedRefresh();
			SetTimeLeft(_countdownLine, 0);
		}

		_countdownLine = null;
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

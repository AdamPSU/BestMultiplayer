using DefinitiveMultiplayer.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Colored chat when Modes toggles flip, and strips tML's cryptic
/// "Shared config changed: Message: Accepted..." line for our ServerConfig.
/// </summary>
public sealed class ConfigModeChatSystem : ModSystem
{
	private static readonly Color SharedHealthColor = new(255, 120, 140);
	private static readonly Color PlayerSwapColor = new(255, 215, 0);

	private static bool _stripSharedConfigLine;

	internal static void ArmSharedConfigStrip() => _stripSharedConfigLine = true;

	internal static void AnnounceModeDiffs(ServerConfig cfg, bool prevShared, bool prevSwap, bool prevMarked, bool prevPotato)
	{
		if (Main.dedServ)
			return;

		Announce(cfg.SharedHealthEnabled, prevShared, "Mods.DefinitiveMultiplayer.UI.Modes.SharedHealth", SharedHealthColor);
		Announce(cfg.PlayerSwapEnabled, prevSwap, "Mods.DefinitiveMultiplayer.UI.Modes.PlayerSwap", PlayerSwapColor);
		Announce(cfg.MarkedEnabled, prevMarked, "Mods.DefinitiveMultiplayer.UI.Modes.Marked", MarkedSystem.Accent);
		Announce(cfg.HotPotatoEnabled, prevPotato, "Mods.DefinitiveMultiplayer.UI.Modes.HotPotato", HotPotatoSystem.Accent);
	}

	private static void Announce(bool now, bool was, string keyBase, Color color)
	{
		if (now == was)
			return;

		string key = now ? keyBase + ".On" : keyBase + ".Off";
		string text = Language.GetTextValue(key);
		if (string.IsNullOrEmpty(text) || text == key)
			return;

		Main.NewText(text, color.R, color.G, color.B);
	}

	public override void PostUpdateEverything()
	{
		if (!_stripSharedConfigLine)
			return;

		_stripSharedConfigLine = false;
		TryStripSharedConfigLine();
	}

	private static void TryStripSharedConfigLine()
	{
		if (Main.netMode == NetmodeID.SinglePlayer
			|| !ChatMonitorAccess.TryGetMessages(out var list)
			|| list.Count == 0)
			return;

		ChatMessageContainer last = list[list.Count - 1];
		string text = last.OriginalText ?? "";
		// tML: "Shared config changed: Message: {0}, Mod: {1}, Config: {2}"
		if (text.Contains("DefinitiveMultiplayer", System.StringComparison.Ordinal)
			&& text.Contains("ServerConfig", System.StringComparison.Ordinal))
		{
			list.RemoveAt(list.Count - 1);
		}
	}
}

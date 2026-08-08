using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>Transfer assign FX: CombatText + dink + outline blink window.</summary>
internal static class TransferPopup
{
	private const int FlashTicks = 48;

	// Mixkit "quick win video game notification" — transfer assign cue.
	private static readonly SoundStyle Dink = new("DefinitiveMultiplayer/Sounds/TransferDink")
	{
		Volume = 0.85f,
		PitchVariance = 0.03f,
	};

	private static int _flashUntil;

	internal static bool IsFlashing => Main.GameUpdateCount < _flashUntil;

	internal static void Clear() => _flashUntil = 0;

	internal static void Show(Player player, string localizationKey, Color color)
	{
		if (Main.dedServ || player is null || !player.active)
			return;

		string text = Language.GetTextValue(localizationKey);
		if (string.IsNullOrEmpty(text))
			return;

		_flashUntil = (int)Main.GameUpdateCount + FlashTicks;
		CombatText.NewText(player.Hitbox, color, text, dramatic: true);
		SoundEngine.PlaySound(Dink, player.Center);
	}
}

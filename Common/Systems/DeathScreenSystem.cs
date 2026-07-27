using System.Collections.Generic;
using BestMultiplayer.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Replaces vanilla death text: intro (title + spectating-in), then bottom respawn digits while dead.
/// </summary>
public sealed class DeathScreenSystem : ModSystem
{
	private const float TitleScale = 0.8f;
	private const float SmallScale = 0.45f;

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Death Text");
		if (idx == -1)
			return;

		if (Main.LocalPlayer is { active: true, dead: true })
			layers[idx].Active = false;

		layers.Insert(idx, new LegacyGameInterfaceLayer(
			"BestMultiplayer: DeathText",
			delegate
			{
				if (Main.LocalPlayer is { active: true, dead: true })
					DrawDeathUi();
				return true;
			},
			InterfaceScaleType.UI));
	}

	private static void DrawDeathUi()
	{
		Player player = Main.LocalPlayer;
		Color color = player.GetDeathAlpha(Color.Transparent);
		DynamicSpriteFont font = FontAssets.DeathText.Value;

		if (SpectatePlayer.IsIntro)
		{
			bool hardLock = BossFightSystem.IsLocalHardLocked();
			string title = Language.GetTextValue(hardLock
				? "Mods.BestMultiplayer.UI.Death.OutOfLives"
				: "Mods.BestMultiplayer.UI.Death.Slain");
			string subtitle = Language.GetTextValue(
				"Mods.BestMultiplayer.UI.Death.SpectatingIn",
				SpectatePlayer.IntroSeconds);

			DrawCentered(font, title, Main.screenHeight / 2f - 60f, color, TitleScale);
			DrawCentered(font, subtitle, Main.screenHeight / 2f + 10f, color * 0.85f, SmallScale);
			return;
		}

		if (BossFightSystem.IsLocalHardLocked() || player.respawnTimer <= 0)
			return;

		int seconds = (player.respawnTimer + 59) / 60;
		string text = Language.GetTextValue("Mods.BestMultiplayer.UI.Death.RespawnIn", seconds);
		DrawCentered(font, text, Main.screenHeight - 140f, color, SmallScale);
	}

	private static void DrawCentered(DynamicSpriteFont font, string text, float y, Color color, float scale)
	{
		Vector2 size = font.MeasureString(text) * scale;
		Vector2 position = new(Main.screenWidth / 2f - size.X / 2f, y);
		Main.spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

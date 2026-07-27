using System.Collections.Generic;
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
/// Replaces the vanilla death countdown with a static lock message when out of boss lives.
/// </summary>
public sealed class DeathScreenSystem : ModSystem
{
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Death Text");
		if (idx == -1)
			return;

		if (BossFightSystem.IsLocalHardLocked())
			layers[idx].Active = false;

		layers.Insert(idx, new LegacyGameInterfaceLayer(
			"BestMultiplayer: LockedDeathText",
			delegate
			{
				if (BossFightSystem.IsLocalHardLocked())
					DrawLockedDeathText();
				return true;
			},
			InterfaceScaleType.UI));
	}

	private static void DrawLockedDeathText()
	{
		Player player = Main.LocalPlayer;
		string text = Language.GetTextValue("Mods.BestMultiplayer.UI.Death.OutOfLives");
		DynamicSpriteFont font = FontAssets.DeathText.Value;
		Vector2 size = font.MeasureString(text);
		Vector2 position = new(
			Main.screenWidth / 2f - size.X / 2f,
			Main.screenHeight / 2f - 60f);
		Color color = player.GetDeathAlpha(Color.Transparent);
		Main.spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
	}
}

using System.Collections.Generic;
using BestMultiplayer.Common.Players;
using BestMultiplayer.Common.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Client death chrome: custom death text + dead-only teammate head grid (MP).
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class DeathScreenSystem : ModSystem
{
	private const float TitleScale = 0.8f;
	private const float SmallScale = 0.45f;

	/// <summary>Y of the bottom "Respawn in N" line; spectate grid sits above this.</summary>
	internal static float RespawnTextY => Main.screenHeight - 140f;

	private UserInterface _gridUi;
	private SpectateGridState _gridState;
	private GameTime _lastTime = new();

	public override void Load()
	{
		if (Main.dedServ)
			return;

		_gridState = new SpectateGridState();
		_gridState.Activate();
		_gridUi = new UserInterface();
		_gridUi.SetState(_gridState);
	}

	public override void Unload()
	{
		_gridUi = null;
		_gridState = null;
	}

	public override void UpdateUI(GameTime gameTime)
	{
		_lastTime = gameTime;
		if (ShouldShowGrid())
			_gridUi?.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int deathIdx = layers.FindIndex(layer => layer.Name == "Vanilla: Death Text");
		if (deathIdx != -1)
		{
			if (Main.LocalPlayer is { active: true, dead: true })
				layers[deathIdx].Active = false;

			layers.Insert(deathIdx, new LegacyGameInterfaceLayer(
				"BestMultiplayer: DeathText",
				delegate
				{
					if (Main.LocalPlayer is { active: true, dead: true })
						DrawDeathUi();
					return true;
				},
				InterfaceScaleType.UI));
		}

		int mouseIdx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (mouseIdx != -1)
		{
			layers.Insert(mouseIdx, new LegacyGameInterfaceLayer(
				"BestMultiplayer: SpectateGrid",
				delegate
				{
					if (ShouldShowGrid())
						_gridUi?.Draw(Main.spriteBatch, _lastTime);
					return true;
				},
				InterfaceScaleType.UI));
		}
	}

	private static bool ShouldShowGrid() =>
		!Main.dedServ
		&& Main.netMode != NetmodeID.SinglePlayer
		&& Main.LocalPlayer is { active: true, dead: true };

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
		DrawCentered(font, text, RespawnTextY, color, SmallScale);
	}

	private static void DrawCentered(DynamicSpriteFont font, string text, float y, Color color, float scale)
	{
		Vector2 size = font.MeasureString(text) * scale;
		Vector2 position = new(Main.screenWidth / 2f - size.X / 2f, y);
		Main.spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

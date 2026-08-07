using System;
using System.Collections.Generic;
using DefinitiveMultiplayer.Common.Players;
using DefinitiveMultiplayer.Common.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace DefinitiveMultiplayer.Common.Systems;

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
	private LegacyGameInterfaceLayer _deathTextLayer;
	private LegacyGameInterfaceLayer _spectateGridLayer;

	public override void Load()
	{
		if (Main.dedServ)
			return;

		_gridState = new SpectateGridState();
		_gridState.Activate();
		_gridUi = new UserInterface();
		_gridUi.SetState(_gridState);

		_deathTextLayer = new LegacyGameInterfaceLayer(
			"DefinitiveMultiplayer: DeathText",
			delegate
			{
				if (Main.LocalPlayer is { active: true, dead: true })
					DrawDeathUi();
				return true;
			},
			InterfaceScaleType.UI);

		_spectateGridLayer = new LegacyGameInterfaceLayer(
			"DefinitiveMultiplayer: SpectateGrid",
			delegate
			{
				if (ShouldShowGrid())
					_gridUi?.Draw(Main.spriteBatch, _lastTime);
				return true;
			},
			InterfaceScaleType.UI);
	}

	public override void Unload()
	{
		_gridUi = null;
		_gridState = null;
		_deathTextLayer = null;
		_spectateGridLayer = null;
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

			layers.Insert(deathIdx, _deathTextLayer);
		}

		int mouseIdx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (mouseIdx != -1)
			layers.Insert(mouseIdx, _spectateGridLayer);
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

		RespawnGate.LockReason lockReason = RespawnGate.GetLockReason(player);

		if (SpectatePlayer.IsIntro)
		{
			string title = Language.GetTextValue(TitleKey(lockReason));
			string subtitle = Language.GetTextValue(
				"Mods.DefinitiveMultiplayer.UI.Death.SpectatingIn",
				SpectatePlayer.IntroSeconds);

			float titleY = Main.screenHeight / 2f - 60f;
			DrawCentered(font, title, titleY, color, TitleScale);

			float subtitleY = Main.screenHeight / 2f + 10f;
			if (player.lostCoins > 0)
			{
				string coins = Language.GetTextValue("Game.DroppedCoins", player.lostCoinString);
				float coinsY = titleY + 50f;
				DrawCentered(FontAssets.MouseText.Value, coins, coinsY, color, 1f);
				subtitleY = Math.Max(subtitleY, coinsY + 28f);
			}

			DrawCentered(font, subtitle, subtitleY, color * 0.85f, SmallScale);
			return;
		}

		if (lockReason != RespawnGate.LockReason.None || player.respawnTimer <= 0)
			return;

		int seconds = (player.respawnTimer + 59) / 60;
		string text = Language.GetTextValue("Mods.DefinitiveMultiplayer.UI.Death.RespawnIn", seconds);
		DrawCentered(font, text, RespawnTextY, color, SmallScale);
	}

	private static string TitleKey(RespawnGate.LockReason reason) => reason switch
	{
		RespawnGate.LockReason.SharedHealthWipe => "Mods.DefinitiveMultiplayer.UI.Death.SharedHealthWipe",
		RespawnGate.LockReason.OutOfLives => "Mods.DefinitiveMultiplayer.UI.Death.OutOfLives",
		_ => "Mods.DefinitiveMultiplayer.UI.Death.Slain",
	};

	private static void DrawCentered(DynamicSpriteFont font, string text, float y, Color color, float scale)
	{
		Vector2 size = font.MeasureString(text) * scale;
		Vector2 position = new(Main.screenWidth / 2f - size.X / 2f, y);
		Main.spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

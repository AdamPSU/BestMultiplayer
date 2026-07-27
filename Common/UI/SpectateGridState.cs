using System;
using BestMultiplayer.Common.Players;
using BestMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace BestMultiplayer.Common.UI;

/// <summary>
/// Teammate head grid while dead. Above respawn text normally; screen-center when hard-locked.
/// Head draw matches Team Spectate (TextureAssets player layers + 40×56 crop).
/// </summary>
public sealed class SpectateGridState : UIState
{
	private const int Cell = 52;
	private const int Gap = 6;
	private const int Cols = 4;
	private const float GapAboveRespawn = 20f;

	private readonly UIPanel _panel = new();
	private readonly UIElement _grid = new();
	private int _lastSig = int.MinValue;

	public override void OnInitialize()
	{
		_panel.SetPadding(8f);
		_panel.BackgroundColor = new Color(33, 43, 79) * 0.85f;
		_panel.BorderColor = new Color(89, 116, 213) * 0.9f;
		_panel.HAlign = 0.5f;
		Append(_panel);

		_grid.Width.Set(0f, 1f);
		_grid.Height.Set(0f, 1f);
		_panel.Append(_grid);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		int sig = BuildSignature();
		if (sig != _lastSig)
		{
			_lastSig = sig;
			Rebuild();
		}

		// Hard-lock (no lives): grid replaces center death chrome.
		// Otherwise: sit just above "Respawn in N".
		float top;
		if (BossFightSystem.IsLocalHardLocked() && !SpectatePlayer.IsIntro)
			top = Main.screenHeight / 2f - _panel.Height.Pixels / 2f;
		else
			top = DeathScreenSystem.RespawnTextY - GapAboveRespawn - _panel.Height.Pixels;
		_panel.Top.Set(Math.Max(40f, top), 0f);

		if (_panel.ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;
	}

	private static int BuildSignature()
	{
		unchecked
		{
			int h = SpectatePlayer.Target ?? -1;
			h = h * 397 ^ Main.myPlayer;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!p.active || p.team != Main.LocalPlayer.team)
					continue;
				h = h * 397 ^ i;
				h = h * 397 ^ (p.dead ? 1 : 0);
				h = h * 397 ^ p.hair;
				h = h * 397 ^ p.hairColor.PackedValue.GetHashCode();
				h = h * 397 ^ p.skinColor.PackedValue.GetHashCode();
			}

			return h;
		}
	}

	private void Rebuild()
	{
		_grid.RemoveAllChildren();

		var indices = new System.Collections.Generic.List<int>(8) { Main.myPlayer };
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (i == Main.myPlayer)
				continue;
			Player p = Main.player[i];
			if (p.active && p.team == Main.LocalPlayer.team)
				indices.Add(i);
		}

		int count = indices.Count;
		int cols = System.Math.Min(Cols, System.Math.Max(1, count));
		int rows = (count + cols - 1) / cols;
		float width = cols * Cell + (cols - 1) * Gap;
		float height = rows * Cell + (rows - 1) * Gap;
		_panel.Width.Set(width + 16f, 0f);
		_panel.Height.Set(height + 16f, 0f);

		for (int n = 0; n < count; n++)
		{
			int who = indices[n];
			int col = n % cols;
			int row = n / cols;
			var btn = new SpectateHeadButton(who);
			btn.Left.Set(col * (Cell + Gap), 0f);
			btn.Top.Set(row * (Cell + Gap), 0f);
			btn.Width.Set(Cell, 0f);
			btn.Height.Set(Cell, 0f);
			_grid.Append(btn);
		}
	}
}

public sealed class SpectateHeadButton : UIElement
{
	// Team Spectate PlayerHeadButton crop of player sheet frames.
	private static readonly Rectangle HeadBounds = new(0, 0, 40, 56);

	private readonly int _whoAmI;

	public SpectateHeadButton(int whoAmI)
	{
		_whoAmI = whoAmI;
		OnLeftClick += (_, _) => Clicked();
	}

	private void Clicked()
	{
		if (_whoAmI == Main.myPlayer || SpectatePlayer.Target == _whoAmI)
		{
			SpectatePlayer.StopFollowing();
			return;
		}

		if (SpectatePlayer.IsValid(_whoAmI))
			SpectatePlayer.SelectTarget(_whoAmI);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle d = GetDimensions();
		Player player = Main.player[_whoAmI];
		bool usable = SpectatePlayer.IsValid(_whoAmI) || _whoAmI == Main.myPlayer;
		bool selected = SpectatePlayer.Target == _whoAmI
			|| (SpectatePlayer.Target is null && _whoAmI == Main.myPlayer);

		Color back = selected
			? new Color(80, 160, 80, 180)
			: new Color(40, 50, 80, 160);
		Utils.DrawInvBG(spriteBatch, d.ToRectangle(), back);

		if (selected)
			DrawBorder(spriteBatch, d.ToRectangle(), Color.LightGreen);

		// TS layer stack at scale 1. Face sits in upper half of the 40×56 crop — pin that
		// visual center to the cell center (not the full sprite rect center).
		Color mul = usable ? Color.White : Color.Gray;
		const float faceCenterX = 20f; // mid of 40-wide crop
		const float faceCenterY = 16f; // eyes/face band, not mid of 56
		Vector2 pos = new(
			d.X + d.Width / 2f - faceCenterX,
			d.Y + d.Height / 2f - faceCenterY);

		DrawLayer(spriteBatch, TextureAssets.Players[0, 0], pos, player.skinColor.MultiplyRGBA(mul));
		DrawLayer(spriteBatch, TextureAssets.Players[0, 2], pos, player.eyeColor.MultiplyRGBA(mul));
		DrawLayer(spriteBatch, TextureAssets.Players[0, 1], pos, Color.White.MultiplyRGBA(mul));
		DrawLayer(spriteBatch, TextureAssets.PlayerHair[player.hair], pos, player.hairColor.MultiplyRGBA(mul));

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			string tip = _whoAmI == Main.myPlayer
				? $"{player.name} ({Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.You")})"
				: player.dead
					? Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.Dead", player.name)
					: player.name;
			Main.hoverItemName = tip;
		}
	}

	private static void DrawLayer(SpriteBatch sb, Asset<Texture2D> asset, Vector2 pos, Color color)
	{
		if (!asset.IsLoaded)
			return;

		sb.Draw(asset.Value, pos, HeadBounds, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
	}

	private static void DrawBorder(SpriteBatch sb, Rectangle r, Color c)
	{
		Texture2D px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, 2), c);
		sb.Draw(px, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), c);
		sb.Draw(px, new Rectangle(r.X, r.Y, 2, r.Height), c);
		sb.Draw(px, new Rectangle(r.Right - 2, r.Y, 2, r.Height), c);
	}
}

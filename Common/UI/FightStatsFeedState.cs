using System;
using System.Collections.Generic;
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
/// Layout M: dim head strip + selected stats. Y-centered on vanilla boss bar; left of Settings.
/// </summary>
public sealed class FightStatsFeedState : UIState
{
	// Vanilla BigProgressBarHelper.DrawBareBonesBar centers the bar at (screenW/2, screenH - 50).
	private const float BossBarCenterYFromBottom = 50f;

	private const int Head = 22;
	private const int HeadGap = 4;
	private const int Pad = 12;
	private const float Dim = 0.4f;
	private const float RightReserve = 100f;
	private const float GapBeforeSettings = 10f;
	private const float TextScale = 0.8f;

	private static readonly Rectangle HeadSrc = new(0, 0, 40, 56);

	private readonly UIPanel _panel = new();
	private readonly List<int> _roster = new(8);
	private int _selected = -1;
	private bool _leftWas;
	private bool _rightWas;
	private bool _wasLocalDead;

	public override void OnInitialize()
	{
		_panel.SetPadding(Pad);
		_panel.BackgroundColor = new Color(33, 43, 79) * 0.85f;
		_panel.BorderColor = new Color(89, 116, 213) * 0.9f;
		_panel.HAlign = 1f;
		Append(_panel);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		BuildRoster();
		SnapOnLocalDeath();
		EnsureSelection();
		LayoutPanel();
		HandleClicks();

		if (_panel.ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;
	}

	protected override void DrawChildren(SpriteBatch spriteBatch)
	{
		base.DrawChildren(spriteBatch);
		if (_roster.Count == 0 || _selected < 0)
			return;

		CalculatedStyle d = _panel.GetInnerDimensions();
		float x = d.X;
		float y = d.Y + (d.Height - Head) / 2f;

		for (int i = 0; i < _roster.Count; i++)
		{
			int who = _roster[i];
			var rect = new Rectangle((int)x, (int)y, Head, Head);
			bool sel = who == _selected;
			DrawHead(spriteBatch, who, rect, sel ? 1f : Dim, sel);
			x += Head + HeadGap;
		}

		x += 8f;
		// Same vertical midline as the head row.
		float midY = d.Y + d.Height * 0.5f;
		TeamTotals(out int teamDealt, out int teamTaken);
		int dealtPct = FightStatsSystem.Pct(FightStatsSystem.GetDealt(_selected), teamDealt);
		int deaths = FightStatsSystem.GetDeaths(_selected);
		// Vanilla combat / mouse-text colors (not custom palette).
		Color dealtColor = CombatText.DamagedHostile;
		Color takenColor = CombatText.DamagedFriendly;
		Color mute = Main.MouseTextColorReal * 0.65f;

		int takenPct = FightStatsSystem.Pct(FightStatsSystem.GetTaken(_selected), teamTaken);

		x = DrawLabel(spriteBatch, $"{dealtPct}% dealt", x, midY, dealtColor);
		x = DrawLabel(spriteBatch, "·", x, midY, mute);
		x = DrawLabel(spriteBatch, $"{takenPct}% taken", x, midY, takenColor);
		x = DrawLabel(spriteBatch, "·", x, midY, mute);
		string deathText = deaths == 1 ? "1 death" : $"{deaths} deaths";
		DrawLabel(spriteBatch, deathText, x, midY, deaths > 0 ? takenColor : mute);
	}

	private void BuildRoster()
	{
		_roster.Clear();
		int team = Main.LocalPlayer.team;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			if (p.active && FightStatsSystem.SameRoster(p, team, Main.myPlayer))
				_roster.Add(i);
		}

		_roster.Sort((a, b) =>
		{
			int cmp = FightStatsSystem.GetDealt(b).CompareTo(FightStatsSystem.GetDealt(a));
			return cmp != 0 ? cmp : a.CompareTo(b);
		});
	}

	private void SnapOnLocalDeath()
	{
		bool dead = Main.LocalPlayer.dead;
		if (dead && !_wasLocalDead && _roster.Contains(Main.myPlayer))
			_selected = Main.myPlayer;
		_wasLocalDead = dead;
	}

	private void EnsureSelection()
	{
		if (_roster.Count == 0)
		{
			_selected = -1;
			return;
		}

		if (_roster.Contains(_selected))
			return;

		_selected = _roster.Contains(Main.myPlayer) ? Main.myPlayer : _roster[0];
	}

	private void LayoutPanel()
	{
		// "100% dealt · 100% taken · 99 deaths" ≈ 220
		const float statsW = 220f;
		float width = _roster.Count * Head + Math.Max(0, _roster.Count - 1) * HeadGap + 8f + statsW;
		float height = Head + Pad * 2f;

		_panel.Width.Set(width + Pad * 2, 0f);
		_panel.Height.Set(height, 0f);
		_panel.Left.Set(-(RightReserve + GapBeforeSettings), 0f);

		// Match vanilla boss bar vertical center (screenH - 50).
		float barCenterY = Main.screenHeight - BossBarCenterYFromBottom;
		_panel.Top.Set(barCenterY - height / 2f, 0f);
	}

	private void HandleClicks()
	{
		bool left = Main.mouseLeft;
		bool right = Main.mouseRight;
		bool hover = _panel.ContainsPoint(Main.MouseScreen);

		if (left && !_leftWas && hover)
		{
			int hit = HitHead(Main.MouseScreen);
			if (hit >= 0)
				_selected = hit;
			else
				Cycle(1);
		}

		if (right && !_rightWas && hover)
			Cycle(-1);

		_leftWas = left;
		_rightWas = right;
	}

	private int HitHead(Vector2 mouse)
	{
		CalculatedStyle d = _panel.GetInnerDimensions();
		float x = d.X;
		float y = d.Y + (d.Height - Head) / 2f;
		for (int i = 0; i < _roster.Count; i++)
		{
			var rect = new Rectangle((int)x, (int)y, Head, Head);
			if (rect.Contains((int)mouse.X, (int)mouse.Y))
				return _roster[i];
			x += Head + HeadGap;
		}

		return -1;
	}

	private void Cycle(int delta)
	{
		if (_roster.Count == 0)
			return;
		int idx = _roster.IndexOf(_selected);
		if (idx < 0)
			idx = 0;
		idx = (idx + delta % _roster.Count + _roster.Count) % _roster.Count;
		_selected = _roster[idx];
	}

	private void TeamTotals(out int dealt, out int taken) =>
		FightStatsSystem.TeamTotals(Main.LocalPlayer.team, Main.myPlayer, out dealt, out taken);

	/// <summary>Draw left-aligned label optically centered on midY (same midline as heads).</summary>
	private static float DrawLabel(SpriteBatch sb, string text, float x, float midY, Color color)
	{
		// MouseText MeasureString is descent-heavy: geometric anchory 0.5 sits glyphs high
		// (looks like the baseline/bottom is centered). Bias the anchor upward in the
		// cell so the visible caps/digits share the head midline.
		const float OpticalAnchorY = 0.35f;
		Vector2 size = Utils.DrawBorderString(sb, text, new Vector2(x, midY), color, TextScale, 0f, OpticalAnchorY);
		return x + size.X + 6f;
	}

	private static void DrawHead(SpriteBatch sb, int whoAmI, Rectangle rect, float opacity, bool selected)
	{
		Utils.DrawInvBG(sb, rect, selected
			? new Color(80, 160, 80, 180)
			: new Color(40, 50, 80, 160));

		if (selected)
			SpectateHeadButton.DrawSelectionBorder(sb, rect, Color.LightGreen);

		Player player = Main.player[whoAmI];
		Color mul = (player.dead ? Color.Gray : Color.White) * opacity;
		float scale = Head / 40f;
		var pos = new Vector2(rect.X + (rect.Width - 40 * scale) / 2f, rect.Y + (rect.Height - 28 * scale) / 2f);
		DrawLayer(sb, TextureAssets.Players[0, 0], pos, player.skinColor.MultiplyRGBA(mul), scale);
		DrawLayer(sb, TextureAssets.Players[0, 2], pos, player.eyeColor.MultiplyRGBA(mul), scale);
		DrawLayer(sb, TextureAssets.Players[0, 1], pos, Color.White.MultiplyRGBA(mul), scale);
		DrawLayer(sb, TextureAssets.PlayerHair[player.hair], pos, player.hairColor.MultiplyRGBA(mul), scale);

		if (rect.Contains(Main.MouseScreen.ToPoint()))
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = whoAmI == Main.myPlayer
				? $"{player.name} ({Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.You")})"
				: player.name;
		}
	}

	private static void DrawLayer(SpriteBatch sb, Asset<Texture2D> asset, Vector2 pos, Color color, float scale)
	{
		if (asset.IsLoaded)
			sb.Draw(asset.Value, pos, HeadSrc, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

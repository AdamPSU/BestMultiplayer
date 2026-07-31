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
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace BestMultiplayer.Common.UI;

/// <summary>
/// Layout M: settings-style UIPanel, vanilla-ish heads + health bars, selected stats.
/// Y-centered on vanilla boss bar; left of Settings.
/// </summary>
public sealed class FightStatsFeedState : UIState
{
	// Vanilla BigProgressBarHelper.DrawBareBonesBar centers the bar at (screenW/2, screenH - 50).
	private const float BossBarCenterYFromBottom = 50f;

	// UIPanel 9-slice: cornerSize=12, barSize=4 (vanilla settings inset).
	private const int Head = 32;
	private const int HeadGap = 4;
	private const int BarH = 6;
	private const int StackGap = 2;
	private const int CellH = Head + StackGap + BarH;
	private const int Pad = 8;
	private const float DimAlpha = 0.72f; // non-selected only; selected = 1
	private const float RightReserve = 100f;
	private const float GapBeforeSettings = 8f;
	private const float TextScale = 0.85f;

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
		// Same chrome as settings menu / ExampleCoinsUI (UIPanel 9-slice, corner 12 / bar 4).
		_panel.BackgroundColor = UICommon.DefaultUIBlue;
		_panel.BorderColor = UICommon.DefaultUIBorder;
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

		float blink = FightStatsSystem.FeedBlinkAlpha;
		_panel.BackgroundColor = UICommon.DefaultUIBlue * blink;
		_panel.BorderColor = UICommon.DefaultUIBorder * blink;

		if (_panel.ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;
	}

	protected override void DrawChildren(SpriteBatch spriteBatch)
	{
		base.DrawChildren(spriteBatch);
		if (_roster.Count == 0 || _selected < 0)
			return;

		float blink = FightStatsSystem.FeedBlinkAlpha;
		CalculatedStyle d = _panel.GetInnerDimensions();
		float x = d.X;
		float y = d.Y + Math.Max(0f, (d.Height - CellH) * 0.5f);

		for (int i = 0; i < _roster.Count; i++)
		{
			int who = _roster[i];
			bool sel = who == _selected;
			float alpha = (sel ? 1f : DimAlpha) * blink;
			var headRect = new Rectangle((int)x, (int)y, Head, Head);
			DrawHead(spriteBatch, who, headRect, alpha, sel);

			var barRect = new Rectangle((int)x, (int)(y + Head + StackGap), Head, 6);
			DrawHpBar(spriteBatch, who, barRect, alpha);

			x += Head + HeadGap;
		}

		float textX = x + 6f;
		// Panel inner vertical center (matches earlier layout-M that sat on the boss-bar midline).
		float midY = d.Y + d.Height * 0.5f;
		TeamTotals(out int teamDealt, out int teamTaken);
		int dealtPct = FightStatsSystem.Pct(FightStatsSystem.GetDealt(_selected), teamDealt);
		int deaths = FightStatsSystem.GetDeaths(_selected);
		Color dealtColor = CombatText.DamagedHostile * blink;
		Color takenColor = CombatText.DamagedFriendly * blink;
		Color mute = Main.MouseTextColorReal * (0.65f * blink);
		int takenPct = FightStatsSystem.Pct(FightStatsSystem.GetTaken(_selected), teamTaken);

		textX = DrawLabel(spriteBatch, $"{dealtPct}% dealt", textX, midY, dealtColor);
		textX = DrawLabel(spriteBatch, "·", textX, midY, mute);
		textX = DrawLabel(spriteBatch, $"{takenPct}% taken", textX, midY, takenColor);
		textX = DrawLabel(spriteBatch, "·", textX, midY, mute);
		string deathText = deaths == 1 ? "1 death" : $"{deaths} deaths";
		DrawLabel(spriteBatch, deathText, textX, midY, mute);
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
		// Measure stats string so panel hugs content (no huge empty right side).
		float statsW = MeasureStatsWidth();
		float headsW = _roster.Count * Head + Math.Max(0, _roster.Count - 1) * HeadGap;
		float width = headsW + 6f + statsW;
		float height = CellH + Pad * 2f;

		_panel.Width.Set(width + Pad * 2, 0f);
		_panel.Height.Set(height, 0f);
		_panel.Left.Set(-(RightReserve + GapBeforeSettings), 0f);

		float barCenterY = Main.screenHeight - BossBarCenterYFromBottom;
		_panel.Top.Set(barCenterY - height / 2f, 0f);
	}

	private float MeasureStatsWidth()
	{
		if (_selected < 0)
			return 180f;
		TeamTotals(out int teamDealt, out int teamTaken);
		int dealtPct = FightStatsSystem.Pct(FightStatsSystem.GetDealt(_selected), teamDealt);
		int takenPct = FightStatsSystem.Pct(FightStatsSystem.GetTaken(_selected), teamTaken);
		int deaths = FightStatsSystem.GetDeaths(_selected);
		string deathText = deaths == 1 ? "1 death" : $"{deaths} deaths";
		string s = $"{dealtPct}% dealt · {takenPct}% taken · {deathText}";
		return FontAssets.MouseText.Value.MeasureString(s).X * TextScale + 4f;
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
		float y = d.Y + Math.Max(0f, (d.Height - CellH) * 0.5f);
		for (int i = 0; i < _roster.Count; i++)
		{
			var rect = new Rectangle((int)x, (int)y, Head, CellH);
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

	private static float DrawLabel(SpriteBatch sb, string text, float x, float midY, Color color)
	{
		const float OpticalAnchorY = 0.35f;
		Vector2 size = Utils.DrawBorderString(sb, text, new Vector2(x, midY), color, TextScale, 0f, OpticalAnchorY);
		return x + size.X + 5f;
	}

	private static void DrawHead(SpriteBatch sb, int whoAmI, Rectangle rect, float opacity, bool selected)
	{
		Player player = Main.player[whoAmI];
		// Soft plate behind head (inventory-slot language, not a second panel).
		Color plate = selected
			? new Color(90, 120, 200, 160)
			: new Color(40, 55, 110, 120);
		Utils.DrawInvBG(sb, rect, plate);

		if (selected)
		{
			// UICommon.MainPanelBackground RGB (#212B4F) — dark blue settings chrome.
			Color edge = new Color(33, 43, 79);
			Texture2D px = TextureAssets.MagicPixel.Value;
			sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), edge);
			sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), edge);
			sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), edge);
			sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), edge);
		}

		Color mul = (player.dead ? Color.Gray : Color.White) * opacity;
		float scale = Head / 40f;
		var pos = new Vector2(
			rect.X + (rect.Width - 40 * scale) / 2f,
			rect.Y + (rect.Height - 28 * scale) / 2f);
		DrawLayer(sb, TextureAssets.Players[0, 0], pos, player.skinColor.MultiplyRGBA(mul), scale);
		DrawLayer(sb, TextureAssets.Players[0, 2], pos, player.eyeColor.MultiplyRGBA(mul), scale);
		DrawLayer(sb, TextureAssets.Players[0, 1], pos, Color.White.MultiplyRGBA(mul), scale);
		DrawLayer(sb, TextureAssets.PlayerHair[player.hair], pos, player.hairColor.MultiplyRGBA(mul), scale);

		if (rect.Contains(Main.MouseScreen.ToPoint()))
			SetHover(whoAmI, player);
	}

	/// <summary>
	/// Screen-space HP bar using vanilla Hb2 (track) + Hb1 (fill).
	/// <see cref="Main.DrawHealthBar"/> expects world coords and is unusable in UI layers.
	/// </summary>
	private static void DrawHpBar(SpriteBatch sb, int whoAmI, Rectangle rect, float alpha)
	{
		Player player = Main.player[whoAmI];
		float frac = 0f;
		if (!player.dead && player.statLifeMax2 > 0)
			frac = MathHelper.Clamp(player.statLife / (float)player.statLifeMax2, 0f, 1f);

		Color trackColor = Color.White * alpha;
		Color fillColor = new Color(60, 200, 70) * alpha;
		Texture2D track = TextureAssets.Hb2.Value;
		Texture2D fill = TextureAssets.Hb1.Value;
		sb.Draw(track, rect, trackColor);
		if (frac > 0f)
		{
			int srcW = Math.Max(1, (int)(fill.Width * frac));
			int fillW = Math.Max(1, (int)(rect.Width * frac));
			sb.Draw(fill, new Rectangle(rect.X, rect.Y, fillW, rect.Height),
				new Rectangle(0, 0, srcW, fill.Height), fillColor);
		}

		if (rect.Contains(Main.MouseScreen.ToPoint()))
			SetHover(whoAmI, player);
	}

	private static void SetHover(int whoAmI, Player player)
	{
		Main.LocalPlayer.mouseInterface = true;
		string label = whoAmI == Main.myPlayer
			? $"{player.name} ({Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.You")})"
			: player.name;
		if (!player.dead && player.statLifeMax2 > 0)
			label += $" ({player.statLife}/{player.statLifeMax2})";
		Main.hoverItemName = label;
	}

	private static void DrawLayer(SpriteBatch sb, Asset<Texture2D> asset, Vector2 pos, Color color, float scale)
	{
		if (asset.IsLoaded)
			sb.Draw(asset.Value, pos, HeadSrc, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

using System;
using System.Collections.Generic;
using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace DefinitiveMultiplayer.Common.UI;

/// <summary>
/// Layout M: settings-style UIPanel, vanilla-ish heads + health bars, selected stats.
/// Y-centered on vanilla boss bar; X right edge matches player hearts (max HP row).
/// </summary>
public sealed class FightStatsFeedState : UIState
{
	// Boss bar Y: BigProgressBarHelper centers at (screenW/2, screenH - 50).
	private const float BossBarCenterYFromBottom = 50f;

	// Player hearts right edge at max HP (ClassicPlayerResourcesDisplaySet):
	//   UI_ScreenAnchorX = screenWidth - 800
	//   hover/row: X in [500 + anchor, 500 + 260 + anchor] for a full 10-heart row
	//   → right = screenWidth - 40
	private const float PlayerHeartsRightInset = 40f;

	// Vanilla Settings button (DrawInterface_29): anchor (screenW-110, screenH-20) when dead.
	private const float SettingsClearance = 52f;

	// UIPanel 9-slice: cornerSize=12, barSize=4 (vanilla settings inset).
	private const int Head = 32;
	private const int HeadGap = 4;
	private const int BarH = 6;
	private const int StackGap = 2;
	private const int CellH = Head + StackGap + BarH;
	private const int Pad = 8;
	private const float DimAlpha = 0.72f; // non-selected only; selected = 1
	private const float TextScale = 0.85f;

	private static readonly Color PanelBg = UiStyle.PanelBg;
	private static readonly Color PanelBorder = UiStyle.PanelBorder;

	private readonly UIPanel _panel = new();
	private readonly List<int> _roster = new(8);
	private readonly int[] _respawnMax = new int[Main.maxPlayers];
	private readonly bool[] _wasDead = new bool[Main.maxPlayers];
	private int _selected = -1;
	private bool _leftWas;
	private bool _rightWas;
	private bool _wasLocalDead;

	private int _dealtPct;
	private int _takenPct;
	private int _deaths;
	private string _deathText = "";
	private float _statsWidth = 180f;

	public override void OnInitialize()
	{
		_panel.SetPadding(Pad);
		_panel.BackgroundColor = PanelBg;
		_panel.BorderColor = PanelBorder;
		_panel.HAlign = 1f; // right edge anchors to parent; Left is inset from screen right
		Append(_panel);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		BuildRoster();
		TrackRespawnSnapshots();
		SnapOnLocalDeath();
		EnsureSelection();
		RefreshStats();
		LayoutPanel();
		HandleClicks();

		float blink = FightStatsSystem.FeedBlinkAlpha;
		_panel.BackgroundColor = WithAlpha(PanelBg, blink);
		_panel.BorderColor = WithAlpha(PanelBorder, blink);

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

			var barRect = new Rectangle((int)x, (int)(y + Head + StackGap), Head, BarH);
			DrawHpBar(spriteBatch, who, barRect, alpha, _respawnMax[who]);

			x += Head + HeadGap;
		}

		float textX = x + 6f;
		// Panel inner vertical center (matches earlier layout-M that sat on the boss-bar midline).
		float midY = d.Y + d.Height * 0.5f;
		Color dealtColor = CombatText.DamagedHostile * blink;
		Color takenColor = CombatText.DamagedFriendly * blink;
		Color mute = Main.MouseTextColorReal * (0.65f * blink);

		textX = DrawLabel(spriteBatch, $"{_dealtPct}% dealt", textX, midY, dealtColor);
		textX = DrawLabel(spriteBatch, "·", textX, midY, mute);
		textX = DrawLabel(spriteBatch, $"{_takenPct}% taken", textX, midY, takenColor);
		textX = DrawLabel(spriteBatch, "·", textX, midY, mute);
		DrawLabel(spriteBatch, _deathText, textX, midY, Color.White * blink);
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

	private void TrackRespawnSnapshots()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			bool dead = p.active && p.dead;
			if (dead && !_wasDead[i])
				_respawnMax[i] = Math.Max(1, p.respawnTimer);
			if (!dead)
				_respawnMax[i] = 0;
			_wasDead[i] = dead;
		}
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
		float headsW = _roster.Count * Head + Math.Max(0, _roster.Count - 1) * HeadGap;
		float width = headsW + 6f + _statsWidth + Pad * 2f;
		float height = CellH + Pad * 2f;

		_panel.Width.Set(width, 0f);
		_panel.Height.Set(height, 0f);
		// Match Classic hearts row right edge (max HP): screenWidth - 40.
		_panel.Left.Set(-PlayerHeartsRightInset, 0f);

		float barCenterY = Main.screenHeight - BossBarCenterYFromBottom;
		float top = barCenterY - height / 2f;
		// Dead: Settings button appears bottom-right — keep feed above it.
		if (Main.LocalPlayer.dead)
			top = Math.Min(top, Main.screenHeight - SettingsClearance - height);
		_panel.Top.Set(top, 0f);
	}

	/// <summary>Recomputes the selected player's dealt/taken/deaths stats once per frame for both layout and draw.</summary>
	private void RefreshStats()
	{
		if (_selected < 0)
		{
			_statsWidth = 180f;
			return;
		}

		TeamTotals(out int teamDealt, out int teamTaken);
		_dealtPct = FightStatsSystem.Pct(FightStatsSystem.GetDealt(_selected), teamDealt);
		_takenPct = FightStatsSystem.Pct(FightStatsSystem.GetTaken(_selected), teamTaken);
		_deaths = FightStatsSystem.GetDeaths(_selected);
		_deathText = _deaths == 1 ? "1 death" : $"{_deaths} deaths";
		string s = $"{_dealtPct}% dealt · {_takenPct}% taken · {_deathText}";
		_statsWidth = FontAssets.MouseText.Value.MeasureString(s).X * TextScale + 4f;
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
		PlayerHeadRenderer.DrawSelectableBackground(sb, rect, selected);

		if (selected)
			SpectateHeadButton.DrawSelectionBorder(sb, rect, Color.LightGreen);

		Color mul = Color.White * opacity;
		float scale = Head / 40f;
		var pos = new Vector2(
			rect.X + (rect.Width - 40 * scale) / 2f,
			rect.Y + (rect.Height - 28 * scale) / 2f);
		PlayerHeadRenderer.Draw(sb, player, pos, mul, scale, grayscale: player.dead);

		if (player.dead)
			DrawDeathMark(sb, rect, opacity);

		if (rect.Contains(Main.MouseScreen.ToPoint()))
			SetHover(whoAmI, player);
	}

	private static void DrawDeathMark(SpriteBatch sb, Rectangle headRect, float opacity)
	{
		Texture2D tex = TextureAssets.MapDeath.Value;
		float fit = Math.Min(headRect.Width, headRect.Height) * 0.85f;
		float scale = fit / Math.Max(tex.Width, tex.Height);
		var origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
		var center = new Vector2(headRect.X + headRect.Width * 0.5f, headRect.Y + headRect.Height * 0.5f);
		sb.Draw(tex, center, null, Color.Red * opacity, 0f, origin, scale, SpriteEffects.None, 0f);
	}

	/// <summary>
	/// Screen-space HP / respawn bar using vanilla Hb2 (track) + Hb1 (fill).
	/// <see cref="Main.DrawHealthBar"/> expects world coords and is unusable in UI layers.
	/// </summary>
	private static void DrawHpBar(SpriteBatch sb, int whoAmI, Rectangle rect, float alpha, int respawnMax)
	{
		Player player = Main.player[whoAmI];
		Texture2D track = TextureAssets.Hb2.Value;
		Texture2D fill = TextureAssets.Hb1.Value;
		sb.Draw(track, rect, Color.White * alpha);

		float frac = 0f;
		Color fillColor = new Color(60, 200, 70) * alpha;

		if (player.dead)
		{
			if (RespawnGate.GetLockReason(player) == RespawnGate.LockReason.None
			    && respawnMax > 0 && player.respawnTimer > 0)
			{
				frac = MathHelper.Clamp(player.respawnTimer / (float)respawnMax, 0f, 1f);
				fillColor = Color.White * alpha;
			}
		}
		else if (TrySharedPool(player, out int sharedCur, out int sharedMax))
		{
			frac = MathHelper.Clamp(sharedCur / (float)sharedMax, 0f, 1f);
		}
		else if (player.statLifeMax2 > 0)
		{
			frac = MathHelper.Clamp(player.statLife / (float)player.statLifeMax2, 0f, 1f);
		}

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
			? $"{player.name} ({Language.GetTextValue("Mods.DefinitiveMultiplayer.UI.Spectate.You")})"
			: player.name;

		if (player.dead)
		{
			if (RespawnGate.GetLockReason(player) != RespawnGate.LockReason.None)
				label += " (locked)";
			else if (player.respawnTimer > 0)
			{
				int seconds = (player.respawnTimer + 59) / 60;
				label += $" ({seconds}s)";
			}
			else
				label += " (dead)";
		}
		else if (TrySharedPool(player, out int sharedCur, out int sharedMax))
		{
			label += $" ({sharedCur}/{sharedMax})";
		}
		else if (player.statLifeMax2 > 0)
		{
			label += $" ({player.statLife}/{player.statLifeMax2})";
		}

		Main.hoverItemName = label;
	}

	private static bool TrySharedPool(Player player, out int current, out int max) =>
		SharedHealthSystem.TryGetPoolForPlayer(player, out current, out max); // predicted for local

	/// <summary>Alpha-only scale — keeps RGB (unlike Color * float).</summary>
	private static Color WithAlpha(Color c, float a) =>
		new(c.R, c.G, c.B, (int)(c.A * MathHelper.Clamp(a, 0f, 1f)));
}

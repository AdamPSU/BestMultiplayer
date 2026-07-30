using System;
using System.Collections.Generic;
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
/// Teammate + boss head grid while dead. Above bottom respawn-timer band.
/// </summary>
public sealed class SpectateGridState : UIState
{
	private const int Cell = 52;
	private const int Gap = 6;
	private const int Cols = 4;
	private const float GapAboveRespawn = 20f;

	private readonly UIPanel _panel = new();
	private readonly UIElement _grid = new();
	private readonly List<int> _bossScratch = new(8);
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

		float top = DeathScreenSystem.RespawnTextY - GapAboveRespawn - _panel.Height.Pixels;
		_panel.Top.Set(Math.Max(40f, top), 0f);

		if (_panel.ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;
	}

	private int BuildSignature()
	{
		unchecked
		{
			int h = (int)SpectatePlayer.Kind;
			h = h * 397 ^ (SpectatePlayer.Target ?? -1);
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
			}

			SpectatePlayer.CollectBossIndices(_bossScratch);
			h = h * 397 ^ _bossScratch.Count;
			for (int i = 0; i < _bossScratch.Count; i++)
			{
				int bi = _bossScratch[i];
				h = h * 397 ^ bi;
				h = h * 397 ^ Main.npc[bi].type;
			}

			return h;
		}
	}

	// Called only from Update() right after BuildSignature(), which already refreshed _bossScratch.
	private void Rebuild()
	{
		_grid.RemoveAllChildren();

		var entries = new List<(bool boss, int id)>(16) { (false, Main.myPlayer) };
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (i == Main.myPlayer)
				continue;
			Player p = Main.player[i];
			if (p.active && p.team == Main.LocalPlayer.team)
				entries.Add((false, i));
		}

		for (int i = 0; i < _bossScratch.Count; i++)
			entries.Add((true, _bossScratch[i]));

		int count = entries.Count;
		int cols = Math.Min(Cols, Math.Max(1, count));
		int rows = (count + cols - 1) / cols;
		float width = cols * Cell + (cols - 1) * Gap;
		float height = rows * Cell + (rows - 1) * Gap;
		_panel.Width.Set(width + 16f, 0f);
		_panel.Height.Set(height + 16f, 0f);

		for (int n = 0; n < count; n++)
		{
			(bool boss, int id) = entries[n];
			UIElement btn = boss ? new SpectateBossButton(id) : new SpectateHeadButton(id);
			btn.Left.Set(n % cols * (Cell + Gap), 0f);
			btn.Top.Set(n / cols * (Cell + Gap), 0f);
			btn.Width.Set(Cell, 0f);
			btn.Height.Set(Cell, 0f);
			_grid.Append(btn);
		}
	}
}

public sealed class SpectateHeadButton : UIElement
{
	private static readonly Rectangle HeadBounds = new(0, 0, 40, 56);
	private readonly int _whoAmI;

	public SpectateHeadButton(int whoAmI)
	{
		_whoAmI = whoAmI;
		OnLeftClick += (_, _) =>
		{
			bool isSelf = _whoAmI == Main.myPlayer;
			bool isCurrent = SpectatePlayer.Kind == SpectateKind.Player && SpectatePlayer.Target == _whoAmI;
			if (isSelf || isCurrent)
				SpectatePlayer.StopFollowing();
			else if (SpectatePlayer.IsValidPlayer(_whoAmI))
				SpectatePlayer.SelectPlayer(_whoAmI);
		};
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle d = GetDimensions();
		Player player = Main.player[_whoAmI];
		bool usable = SpectatePlayer.IsValidPlayer(_whoAmI) || _whoAmI == Main.myPlayer;
		bool selected = SpectatePlayer.Kind == SpectateKind.Player && SpectatePlayer.Target == _whoAmI
			|| (SpectatePlayer.Kind == SpectateKind.None && _whoAmI == Main.myPlayer);

		Rectangle rect = d.ToRectangle();
		Utils.DrawInvBG(spriteBatch, rect, selected
			? new Color(80, 160, 80, 180)
			: new Color(40, 50, 80, 160));

		if (selected)
			DrawSelectionBorder(spriteBatch, rect, Color.LightGreen);

		Color mul = usable ? Color.White : Color.Gray;
		Vector2 pos = new(d.X + d.Width / 2f - 20f, d.Y + d.Height / 2f - 16f);
		DrawLayer(spriteBatch, TextureAssets.Players[0, 0], pos, player.skinColor.MultiplyRGBA(mul));
		DrawLayer(spriteBatch, TextureAssets.Players[0, 2], pos, player.eyeColor.MultiplyRGBA(mul));
		DrawLayer(spriteBatch, TextureAssets.Players[0, 1], pos, Color.White.MultiplyRGBA(mul));
		DrawLayer(spriteBatch, TextureAssets.PlayerHair[player.hair], pos, player.hairColor.MultiplyRGBA(mul));

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = _whoAmI == Main.myPlayer
				? $"{player.name} ({Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.You")})"
				: player.dead
					? Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.Dead", player.name)
					: player.name;
		}
	}

	internal static void DrawSelectionBorder(SpriteBatch spriteBatch, Rectangle rect, Color c)
	{
		Texture2D px = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), c);
		spriteBatch.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), c);
		spriteBatch.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), c);
		spriteBatch.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), c);
	}

	private static void DrawLayer(SpriteBatch sb, Asset<Texture2D> asset, Vector2 pos, Color color)
	{
		if (asset.IsLoaded)
			sb.Draw(asset.Value, pos, HeadBounds, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
	}
}

public sealed class SpectateBossButton : UIElement
{
	private readonly int _npcIndex;

	public SpectateBossButton(int npcIndex)
	{
		_npcIndex = npcIndex;
		OnLeftClick += (_, _) =>
		{
			bool isCurrent = SpectatePlayer.Kind == SpectateKind.Boss && SpectatePlayer.Target == _npcIndex;
			if (isCurrent)
				SpectatePlayer.StopFollowing();
			else if (SpectatePlayer.IsValidBoss(_npcIndex))
				SpectatePlayer.SelectBoss(_npcIndex);
		};
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle d = GetDimensions();
		NPC npc = Main.npc[_npcIndex];
		bool usable = SpectatePlayer.IsValidBoss(_npcIndex);
		bool selected = SpectatePlayer.Kind == SpectateKind.Boss && SpectatePlayer.Target == _npcIndex;

		Rectangle rect = d.ToRectangle();
		Utils.DrawInvBG(spriteBatch, rect, selected
			? new Color(160, 100, 80, 180)
			: new Color(50, 40, 40, 160));

		if (selected)
			SpectateHeadButton.DrawSelectionBorder(spriteBatch, rect, Color.Coral);

		int head = usable ? npc.GetBossHeadTextureIndex() : -1;
		if (head >= 0 && head < TextureAssets.NpcHeadBoss.Length)
		{
			Asset<Texture2D> asset = TextureAssets.NpcHeadBoss[head];
			if (asset.IsLoaded)
			{
				Texture2D tex = asset.Value;
				Vector2 pos = new(
					d.X + (d.Width - tex.Width) / 2f,
					d.Y + (d.Height - tex.Height) / 2f);
				spriteBatch.Draw(tex, pos, Color.White);
			}
		}

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.hoverItemName = Language.GetTextValue("Mods.BestMultiplayer.UI.Spectate.Boss", npc.FullName);
		}
	}
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;

namespace DefinitiveMultiplayer.Common.UI;

/// <summary>Shared skin/eyes/body/hair portrait draw, used by the fight-stats feed and the spectate grid.</summary>
internal static class PlayerHeadRenderer
{
	private static readonly Rectangle Src = new(0, 0, 40, 56);

	internal static void Draw(SpriteBatch sb, Player player, Vector2 pos, Color tint, float scale = 1f, bool grayscale = false)
	{
		Color skin = player.skinColor.MultiplyRGBA(tint);
		Color eyes = player.eyeColor.MultiplyRGBA(tint);
		Color body = Color.White.MultiplyRGBA(tint);
		Color hair = player.hairColor.MultiplyRGBA(tint);
		if (grayscale)
		{
			skin = ToGray(skin);
			eyes = ToGray(eyes);
			body = ToGray(body);
			hair = ToGray(hair);
		}

		DrawLayer(sb, TextureAssets.Players[0, 0], pos, skin, scale);
		DrawLayer(sb, TextureAssets.Players[0, 2], pos, eyes, scale);
		DrawLayer(sb, TextureAssets.Players[0, 1], pos, body, scale);
		DrawLayer(sb, TextureAssets.PlayerHair[player.hair], pos, hair, scale);
	}

	private static Color ToGray(Color c)
	{
		int g = (c.R * 30 + c.G * 59 + c.B * 11) / 100;
		return new Color(g, g, g, c.A);
	}

	private static void DrawLayer(SpriteBatch sb, Asset<Texture2D> asset, Vector2 pos, Color color, float scale)
	{
		if (asset.IsLoaded)
			sb.Draw(asset.Value, pos, Src, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
	}
}

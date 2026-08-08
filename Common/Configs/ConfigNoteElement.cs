using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>Read-only footnote row under a config section (not a setting).</summary>
public sealed class ConfigNoteElement : ConfigElement
{
	private const float PadX = 10f;
	private const float PadY = 6f;
	private const float Scale = 0.78f;

	public override void OnBind()
	{
		base.OnBind();
		DrawLabel = false;
		IgnoresMouseInteraction = true;
		Height.Set(MeasureHeight(420f), 0f);
	}

	public override void Recalculate()
	{
		float width = GetDimensions().Width;
		if (width < 1f && Parent != null)
			width = Parent.GetDimensions().Width;
		if (width < 1f)
			width = 420f;

		Height.Set(MeasureHeight(width), 0f);
		base.Recalculate();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		// Background only (DrawLabel=false); then wrap footnote text.
		base.DrawSelf(spriteBatch);

		CalculatedStyle dims = GetDimensions();
		string text = Label ?? string.Empty;
		if (string.IsNullOrEmpty(text))
			return;

		float maxWidth = Math.Max(1f, dims.Width - PadX * 2f);
		var color = new Color(220, 200, 240);
		ChatManager.DrawColorCodedStringWithShadow(
			spriteBatch,
			FontAssets.MouseText.Value,
			text,
			new Vector2(dims.X + PadX, dims.Y + PadY),
			color,
			0f,
			Vector2.Zero,
			new Vector2(Scale),
			maxWidth);
	}

	private float MeasureHeight(float width)
	{
		string text = Label ?? string.Empty;
		if (string.IsNullOrEmpty(text))
			return ConfigVisibility.RowHeight + 8f;

		float maxWidth = Math.Max(1f, width - PadX * 2f);
		Vector2 size = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, new Vector2(Scale), maxWidth);
		return Math.Max(ConfigVisibility.RowHeight + 8f, size.Y + PadY * 2f);
	}
}

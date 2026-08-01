using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Stock bool toggle UI that collapses when <see cref="ConfigGateAttribute"/> is not satisfied.
/// (tML's BooleanElement is internal, so this reimplements the toggle.)
/// </summary>
public sealed class GatedBooleanElement : ConfigElement<bool>
{
	private Asset<Texture2D> _toggleTexture;
	private bool? _lastShown;

	public override void OnBind()
	{
		base.OnBind();
		_toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
		OnLeftClick += (_, _) =>
		{
			if (IgnoresMouseInteraction)
				return;
			Value = !Value;
		};
		_lastShown = null;
		ConfigVisibility.Refresh(this, Item, MemberInfo.MemberInfo, ref _lastShown);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		ConfigVisibility.Refresh(this, Item, MemberInfo.MemberInfo, ref _lastShown);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (ConfigVisibility.IsCollapsed(this))
			return;

		base.DrawSelf(spriteBatch);
		CalculatedStyle dimensions = GetDimensions();
		ChatManager.DrawColorCodedStringWithShadow(
			spriteBatch,
			FontAssets.ItemStack.Value,
			Value ? Lang.menu[126].Value : Lang.menu[124].Value,
			new Vector2(dimensions.X + dimensions.Width - 60f, dimensions.Y + 8f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(0.8f));

		int halfW = (_toggleTexture.Width() - 2) / 2;
		var source = new Rectangle(Value ? halfW + 2 : 0, 0, halfW, _toggleTexture.Height());
		var drawPos = new Vector2(dimensions.X + dimensions.Width - source.Width - 10f, dimensions.Y + 8f);
		spriteBatch.Draw(_toggleTexture.Value, drawPos, source, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.Config.UI;

namespace BestMultiplayer.Common.Configs;

/// <summary>Float slider that collapses when <see cref="ConfigGateAttribute"/> is not satisfied.</summary>
public sealed class GatedFloatElement : FloatElement
{
	private bool? _lastShown;

	public override void OnBind()
	{
		base.OnBind();
		// Force apply even if already "false" so container height is corrected after WrapIt snapshots it.
		_lastShown = null;
		ConfigVisibility.Apply(this, ConfigVisibility.IsVisible(Item, MemberInfo.MemberInfo), ref _lastShown);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		ConfigVisibility.Apply(this, ConfigVisibility.IsVisible(Item, MemberInfo.MemberInfo), ref _lastShown);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (IgnoresMouseInteraction || GetDimensions().Height < 1f)
			return;

		base.DrawSelf(spriteBatch);
	}
}

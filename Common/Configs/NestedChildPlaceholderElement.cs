using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.Config.UI;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Zero-height main-list stub. Real UI for this field is owned by a parent
/// <see cref="ExpandableToggleElement"/> nested list.
/// </summary>
public sealed class NestedChildPlaceholderElement : ConfigElement
{
	public override void OnBind()
	{
		base.OnBind();
		ConfigVisibility.ForceCollapse(this);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		ConfigVisibility.ForceCollapse(this);
	}

	public override void Recalculate()
	{
		ConfigVisibility.ForceCollapse(this);
		base.Recalculate();
		ConfigVisibility.ForceCollapse(this);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
	}
}

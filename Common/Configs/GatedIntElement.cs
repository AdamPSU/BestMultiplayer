using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.Config.UI;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Int slider (tML's IntRangeElement is internal) that collapses when
/// <see cref="ConfigGateAttribute"/> is not satisfied.
/// </summary>
public class GatedIntElement : PrimitiveRangeElement<int>
{
	private bool? _lastShown;

	public override int NumberTicks => ((Max - Min) / Increment) + 1;

	public override float TickIncrement => Increment / (float)(Max - Min);

	protected override float Proportion
	{
		get => (GetValue() - Min) / (float)(Max - Min);
		set => SetValue((int)Math.Round((value * (Max - Min) + Min) * (1f / Increment)) * Increment);
	}

	public GatedIntElement()
	{
		Min = 0;
		Max = 100;
		Increment = 1;
	}

	public override void OnBind()
	{
		base.OnBind();
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
	}

	/// <summary>Current bound int value (for subclasses that customize the label).</summary>
	protected int CurrentValue => GetValue();
}

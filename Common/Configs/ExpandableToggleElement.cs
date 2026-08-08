using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Bool toggle that expands indented child config rows when On (Timer Add-ons style nesting).
/// Children are listed via <see cref="ExpandableChildrenAttribute"/>.
/// </summary>
public sealed class ExpandableToggleElement : ConfigElement<bool>
{
	private Asset<Texture2D> _toggleTexture;
	private UIList _dataList;
	private bool _pending = true;
	private bool _listBuilt;

	public override void OnBind()
	{
		base.OnBind();
		_toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");

		float header = ConfigVisibility.RowHeight;

		_dataList = new UIList();
		_dataList.Width.Set(-14f, 1f);
		_dataList.Left.Set(14f, 0f);
		_dataList.Height.Set(-header, 1f);
		_dataList.Top.Set(header, 0f);
		_dataList.ListPadding = 5f;

		OnLeftClick += (_, _) =>
		{
			// Only toggle from the header band — not when clicking indented children.
			if (Main.mouseY > GetDimensions().Y + ConfigVisibility.RowHeight)
				return;

			Value = !Value;
			_pending = true;
		};

		_pending = true;
		Recalculate();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!_pending)
		{
			// Keep outer height in sync while expanded (child sliders can change list height).
			if (Value && _dataList.Parent != null)
				SyncHeight();
			return;
		}

		_pending = false;
		RemoveChild(_dataList);

		if (Value)
		{
			if (!_listBuilt)
			{
				SetupList();
				_listBuilt = true;
			}

			Append(_dataList);
		}

		SyncHeight();
		ConfigVisibility.ReflowContainingList(this);
	}

	private void SetupList()
	{
		_dataList.Clear();

		ExpandableChildrenAttribute kids =
			ConfigManager.GetCustomAttributeFromMemberThenMemberType<ExpandableChildrenAttribute>(
				MemberInfo, Item, List);
		if (kids?.MemberNames is not { Length: > 0 })
			return;

		Type itemType = Item.GetType();

		foreach (string name in kids.MemberNames)
		{
			MemberInfo member = ConfigVisibility.ResolveMember(itemType, name);
			// tML exposes FieldInfo / PropertyInfo overloads, not MemberInfo.
			PropertyFieldWrapper wrapper = member switch
			{
				FieldInfo field => new PropertyFieldWrapper(field),
				PropertyInfo prop => new PropertyFieldWrapper(prop),
				_ => null,
			};
			if (wrapper is null)
				continue;

			UIElement el = CreateChildElement(wrapper);
			if (el is null)
				continue;

			if (el is ConfigElement configElement)
			{
				configElement.Bind(wrapper, Item, null, -1);
				configElement.OnBind();
			}

			el.Recalculate();
			float elementHeight = el.GetOuterDimensions().Height;
			if (elementHeight < 1f)
				elementHeight = ConfigVisibility.RowHeight;

			UIElement container = new UIElement();
			container.Width.Set(0f, 1f);
			container.Height.Set(elementHeight, 0f);
			container.Height.Pixels = elementHeight;
			el.Width.Set(0f, 1f);
			el.Height.Set(elementHeight, 0f);
			container.Append(el);
			_dataList.Add(container);
		}
	}

	private static UIElement CreateChildElement(PropertyFieldWrapper wrapper)
	{
		Type type = wrapper.Type;
		if (type == typeof(bool))
			return new GatedBooleanElement();
		if (type == typeof(int))
			return new GatedIntElement();
		if (type == typeof(float))
			return new GatedFloatElement();
		return null;
	}

	public override void Recalculate()
	{
		base.Recalculate();
		SyncHeight();
	}

	private void SyncHeight()
	{
		float h = ConfigVisibility.RowHeight;
		if (_dataList is not null && _dataList.Parent != null)
		{
			_dataList.Recalculate();
			h += ConfigVisibility.GetListContentHeight(_dataList) + _dataList.ListPadding;
		}

		Height.Set(h, 0f);
		Height.Pixels = h;
		if (Parent is UIElement parent)
		{
			parent.Height.Set(h, 0f);
			parent.Height.Pixels = h;
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		var headerDims = new CalculatedStyle(dimensions.X, dimensions.Y, dimensions.Width, ConfigVisibility.RowHeight);

		base.DrawSelf(spriteBatch);

		ChatManager.DrawColorCodedStringWithShadow(
			spriteBatch,
			FontAssets.ItemStack.Value,
			Value ? Lang.menu[126].Value : Lang.menu[124].Value,
			new Vector2(headerDims.X + headerDims.Width - 60f, headerDims.Y + 8f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(0.8f));

		int halfW = (_toggleTexture.Width() - 2) / 2;
		var source = new Rectangle(Value ? halfW + 2 : 0, 0, halfW, _toggleTexture.Height());
		var drawPos = new Vector2(headerDims.X + headerDims.Width - source.Width - 10f, headerDims.Y + 8f);
		spriteBatch.Draw(_toggleTexture.Value, drawPos, source, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}
}

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
	private static readonly MethodInfo GetTotalHeightMethod = typeof(UIList).GetMethod(
		"GetTotalHeight",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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
		ReflowParentList();
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
		int order = 0;
		int top = 0;

		foreach (string name in kids.MemberNames)
		{
			FieldInfo field = itemType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			PropertyInfo prop = field is null
				? itemType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				: null;
			if (field is null && prop is null)
				continue;

			// tML exposes FieldInfo / PropertyInfo overloads, not MemberInfo.
			var wrapper = field is not null
				? new PropertyFieldWrapper(field)
				: new PropertyFieldWrapper(prop);
			// Build real controls (ignore NestedChildPlaceholder on the member).
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
			top += (int)elementHeight + (int)_dataList.ListPadding;
			order++;
		}

		_ = order;
		_ = top;
	}

	private static UIElement CreateChildElement(PropertyFieldWrapper wrapper)
	{
		Type type = wrapper.Type;

		if (type == typeof(bool))
			return new InlineBooleanElement();

		if (type == typeof(int))
		{
			// Prefer slider when Range is present (our gated ints always have Range).
			if (ConfigManager.GetCustomAttributeFromMemberThenMemberType<RangeAttribute>(wrapper, null, null) != null)
				return new GatedIntElement();
			return new GatedIntElement();
		}

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
			h += GetListContentHeight(_dataList) + _dataList.ListPadding;
		}

		Height.Set(h, 0f);
		Height.Pixels = h;
		if (Parent is UIElement parent)
		{
			parent.Height.Set(h, 0f);
			parent.Height.Pixels = h;
		}
	}

	private void ReflowParentList()
	{
		for (UIElement walk = Parent; walk is not null; walk = walk.Parent)
		{
			if (walk is UIList list)
			{
				list.Recalculate();
				break;
			}
		}
	}

	private static float GetListContentHeight(UIList list)
	{
		if (GetTotalHeightMethod != null)
			return Convert.ToSingle(GetTotalHeightMethod.Invoke(list, null));

		float total = 0f;
		foreach (UIElement el in list)
			total += el.GetOuterDimensions().Height + list.ListPadding;
		return Math.Max(0f, total - list.ListPadding);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		// Draw only the header band (not the full expanded height panel).
		CalculatedStyle dimensions = GetDimensions();
		float headerH = ConfigVisibility.RowHeight;
		var headerDims = new CalculatedStyle(dimensions.X, dimensions.Y, dimensions.Width, headerH);

		// Mirror ConfigElement panel draw for the header strip only.
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

	/// <summary>Bool toggle used inside expandable child lists (no gate).</summary>
	private sealed class InlineBooleanElement : ConfigElement<bool>
	{
		private Asset<Texture2D> _toggleTexture;

		public override void OnBind()
		{
			base.OnBind();
			_toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
			OnLeftClick += (_, _) => Value = !Value;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
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
}

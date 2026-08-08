using System;
using System.Collections.Generic;
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
/// Supports <see cref="ExclusiveToggleGroupAttribute"/>: siblings gray out and cannot toggle while one is on.
/// </summary>
public sealed class ExpandableToggleElement : ConfigElement<bool>
{
	private Asset<Texture2D> _toggleTexture;
	private UIList _dataList;
	private bool _pending = true;
	private bool _listBuilt;
	private bool _wasValue;
	private MemberInfo[] _exclusiveSiblings = Array.Empty<MemberInfo>();

	public override void OnBind()
	{
		base.OnBind();
		_toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
		_wasValue = Value;
		_exclusiveSiblings = ResolveExclusiveSiblings();

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

			if (IsLockedOut())
				return;

			if (!Value)
				ClearExclusiveSiblings();

			Value = !Value;
			_wasValue = Value;
			_pending = true;
		};

		_pending = true;
		Recalculate();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		// Sibling exclusive clear / load enforce can flip Value without our click handler.
		if (Value != _wasValue)
		{
			_wasValue = Value;
			_pending = true;
		}

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

	/// <summary>True when another exclusive-group sibling is on (this row stays Off and unclickable).</summary>
	private bool IsLockedOut()
	{
		if (Value || Item is null || _exclusiveSiblings.Length == 0)
			return false;

		for (int i = 0; i < _exclusiveSiblings.Length; i++)
		{
			if (ConfigVisibility.TryReadBool(Item, _exclusiveSiblings[i], out bool on) && on)
				return true;
		}

		return false;
	}

	private void ClearExclusiveSiblings()
	{
		if (Item is null)
			return;

		for (int i = 0; i < _exclusiveSiblings.Length; i++)
			ConfigVisibility.WriteBool(Item, _exclusiveSiblings[i], false);
	}

	private MemberInfo[] ResolveExclusiveSiblings()
	{
		if (Item is null || MemberInfo?.MemberInfo is null)
			return Array.Empty<MemberInfo>();

		ExclusiveToggleGroupAttribute mine =
			MemberInfo.MemberInfo.GetCustomAttribute<ExclusiveToggleGroupAttribute>(inherit: true);
		if (mine is null)
			return Array.Empty<MemberInfo>();

		var list = new List<MemberInfo>(4);
		foreach (MemberInfo sibling in Item.GetType().GetMembers(
			         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			if (sibling is not (FieldInfo or PropertyInfo))
				continue;
			if (sibling == MemberInfo.MemberInfo || sibling.Name == MemberInfo.Name)
				continue;

			ExclusiveToggleGroupAttribute theirs =
				sibling.GetCustomAttribute<ExclusiveToggleGroupAttribute>(inherit: true);
			if (theirs is null || theirs.GroupId != mine.GroupId)
				continue;

			list.Add(sibling);
		}

		return list.Count == 0 ? Array.Empty<MemberInfo>() : list.ToArray();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		bool locked = IsLockedOut();
		CalculatedStyle dimensions = GetDimensions();
		var headerDims = new CalculatedStyle(dimensions.X, dimensions.Y, dimensions.Width, ConfigVisibility.RowHeight);

		base.DrawSelf(spriteBatch);

		if (locked)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(
				px,
				new Rectangle((int)headerDims.X, (int)headerDims.Y, (int)headerDims.Width, (int)headerDims.Height),
				Color.Black * 0.45f);
		}

		Color ui = locked ? Color.Gray * 0.75f : Color.White;

		ChatManager.DrawColorCodedStringWithShadow(
			spriteBatch,
			FontAssets.ItemStack.Value,
			Value ? Lang.menu[126].Value : Lang.menu[124].Value,
			new Vector2(headerDims.X + headerDims.Width - 60f, headerDims.Y + 8f),
			ui,
			0f,
			Vector2.Zero,
			new Vector2(0.8f));

		int halfW = (_toggleTexture.Width() - 2) / 2;
		var source = new Rectangle(Value ? halfW + 2 : 0, 0, halfW, _toggleTexture.Height());
		var drawPos = new Vector2(headerDims.X + headerDims.Width - source.Width - 10f, headerDims.Y + 8f);
		spriteBatch.Draw(_toggleTexture.Value, drawPos, source, ui, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
	}
}

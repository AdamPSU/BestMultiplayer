using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Object-style expandable config row with the expand chevron vertically centered
/// (stock ObjectElement pins it with Top=4, which sits high relative to the label).
/// </summary>
public sealed class ExpandableObjectElement : ConfigElement<object>
{
	private static readonly MethodInfo GetTotalHeightMethod = typeof(UIList).GetMethod(
		"GetTotalHeight",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private bool _expanded = true;
	private bool _pending = true;
	private UIList _dataList;
	private UIImage _expandButton;

	public override void OnBind()
	{
		base.OnBind();

		ExpandAttribute expand = ConfigManager.GetCustomAttributeFromMemberThenMemberType<ExpandAttribute>(
			MemberInfo, Item, List);
		if (expand != null)
			_expanded = expand.Expand;

		float header = ConfigVisibility.RowHeight;

		_dataList = new UIList();
		_dataList.Width.Set(-14f, 1f);
		_dataList.Left.Set(14f, 0f);
		_dataList.Height.Set(-header, 1f);
		_dataList.Top.Set(header, 0f);
		_dataList.ListPadding = 5f;

		_expandButton = new UIImage(CollapsedTexture);
		_expandButton.Width.Set(22f, 0f);
		_expandButton.Height.Set(22f, 0f);
		_expandButton.VAlign = 0f;
		_expandButton.HAlign = 1f;
		_expandButton.Top.Set((header - 22f) * 0.5f, 0f);
		// Between stock ObjectElement (-52) and flush-right (-8); aligns with spinner/On cluster.
		_expandButton.Left.Set(-18f, 0f);
		_expandButton.OnLeftClick += (_, _) =>
		{
			_expanded = !_expanded;
			_pending = true;
			SoundEngine.PlaySound(SoundID.MenuTick);
		};

		SetupList();
		_pending = true;
		Recalculate();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!_pending)
			return;

		_pending = false;
		RemoveChild(_expandButton);
		RemoveChild(_dataList);

		if (Value is null)
			return;

		Append(_expandButton);
		if (_expanded)
		{
			Append(_dataList);
			_expandButton.SetImage(ExpandedTexture);
		}
		else
		{
			_expandButton.SetImage(CollapsedTexture);
		}
	}

	private void SetupList()
	{
		_dataList.Clear();
		object data = Value;
		if (data is null)
			return;

		int order = 0;
		int top = 0;
		foreach (PropertyFieldWrapper variable in ConfigManager.GetFieldsAndProperties(data))
		{
			if (Attribute.IsDefined(variable.MemberInfo, typeof(Newtonsoft.Json.JsonIgnoreAttribute))
			    && !Attribute.IsDefined(variable.MemberInfo, typeof(ShowDespiteJsonIgnoreAttribute)))
				continue;

			ConfigManager.WrapIt(_dataList, ref top, variable, data, order++);
		}
	}

	public override void Recalculate()
	{
		base.Recalculate();

		float h = ConfigVisibility.RowHeight;
		if (_dataList.Parent != null)
			h += GetListContentHeight(_dataList) + _dataList.ListPadding;

		Height.Set(h, 0f);
		if (Parent is UIElement parent)
			parent.Height.Set(h, 0f);
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
}

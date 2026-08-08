using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Hide this config row unless a sibling member matches.
/// Bool gate (default): sibling must be true.
/// Int/enum gate: sibling must equal <see cref="ExpectedValue"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ConfigGateAttribute : Attribute
{
	public string MemberName { get; }

	/// <summary>When set, compare sibling as int/enum ordinal. When null, require bool true.</summary>
	public int? ExpectedValue { get; }

	public ConfigGateAttribute(string memberName)
	{
		MemberName = memberName;
		ExpectedValue = null;
	}

	public ConfigGateAttribute(string memberName, int expectedValue)
	{
		MemberName = memberName;
		ExpectedValue = expectedValue;
	}
}

internal static class ConfigVisibility
{
	public const float RowHeight = 30f;

	private static readonly Dictionary<MemberInfo, ConfigGateAttribute> GateCache = new();
	private static readonly Dictionary<(Type, string), MemberInfo> MemberCache = new();

	private static readonly MethodInfo GetTotalHeightMethod = typeof(UIList).GetMethod(
		"GetTotalHeight",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	/// <summary>Prevents container.Recalculate → child.Recalculate → container.Recalculate loops.</summary>
	private static int _syncDepth;

	public static bool IsVisible(object item, MemberInfo gatedMember)
	{
		if (!GateCache.TryGetValue(gatedMember, out ConfigGateAttribute gate))
		{
			gate = gatedMember.GetCustomAttribute<ConfigGateAttribute>(inherit: true);
			GateCache[gatedMember] = gate;
		}

		if (gate is null || item is null)
			return gate is null;

		object value = ReadMember(item, gate.MemberName);
		if (gate.ExpectedValue is int expected)
			return value is not null && Convert.ToInt32(value) == expected;

		return value is true;
	}

	/// <summary>Resolves gate visibility and applies height (reflows UIList when show state changes).</summary>
	public static void Refresh(UIElement element, object item, MemberInfo gatedMember, ref bool? lastShown)
	{
		bool show = IsVisible(item, gatedMember);
		bool changed = lastShown != show;
		lastShown = show;

		ApplyHeights(element, show, syncContainerDimensions: true);

		if (changed)
			ReflowContainingList(element);
	}

	/// <summary>
	/// Pin gate heights around <c>base.Recalculate</c> (call before and after) so layout
	/// sees the collapsed/expanded size instead of stock row height.
	/// </summary>
	public static void SyncRecalculateHeights(UIElement element, object item, MemberInfo gatedMember)
	{
		if (item is null || gatedMember is null)
			return;

		ApplyHeights(element, IsVisible(item, gatedMember), syncContainerDimensions: _syncDepth == 0);
	}

	/// <summary>True when the element is currently collapsed (hidden by a gate) and shouldn't draw.</summary>
	public static bool IsCollapsed(UIElement element) =>
		element.IgnoresMouseInteraction || element.Height.Pixels < 1f;

	/// <summary>Always collapse a main-list placeholder row (and its WrapIt container).</summary>
	public static void ForceCollapse(UIElement element) =>
		ApplyHeights(element, show: false, syncContainerDimensions: _syncDepth == 0);

	public static void ReflowContainingList(UIElement element)
	{
		for (UIElement walk = element.Parent; walk is not null; walk = walk.Parent)
		{
			if (walk is UIList list)
			{
				list.Recalculate();
				return;
			}
		}
	}

	public static float GetListContentHeight(UIList list)
	{
		if (GetTotalHeightMethod != null)
			return Convert.ToSingle(GetTotalHeightMethod.Invoke(list, null));

		float total = 0f;
		foreach (UIElement el in list)
			total += el.GetOuterDimensions().Height + list.ListPadding;
		return Math.Max(0f, total - list.ListPadding);
	}

	public static MemberInfo ResolveMember(Type type, string name)
	{
		(Type, string) key = (type, name);
		if (!MemberCache.TryGetValue(key, out MemberInfo member))
		{
			member = (MemberInfo)type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				?? type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MemberCache[key] = member;
		}

		return member;
	}

	internal static object ReadMemberValue(object item, MemberInfo member) =>
		member switch
		{
			FieldInfo field => field.GetValue(item),
			PropertyInfo prop => prop.GetValue(item),
			_ => null,
		};

	internal static bool TryReadBool(object item, MemberInfo member, out bool value)
	{
		object raw = ReadMemberValue(item, member);
		if (raw is bool b)
		{
			value = b;
			return true;
		}

		value = false;
		return false;
	}

	internal static void WriteBool(object item, MemberInfo member, bool value)
	{
		switch (member)
		{
			case FieldInfo f when f.FieldType == typeof(bool):
				f.SetValue(item, value);
				break;
			case PropertyInfo p when p.PropertyType == typeof(bool) && p.CanWrite:
				p.SetValue(item, value);
				break;
		}
	}

	private static void ApplyHeights(UIElement element, bool show, bool syncContainerDimensions)
	{
		float h = show ? RowHeight : 0f;
		// Cancel UIList.ListPadding so collapsed rows contribute 0 total spacing.
		float marginBottom = show ? 0f : -FindListPadding(element);

		SetHeight(element, h, marginBottom);
		element.IgnoresMouseInteraction = !show;

		// UISortableElement container from UIModConfig.WrapIt
		if (element.Parent is not UIElement container)
			return;

		SetHeight(container, h, marginBottom);
		container.IgnoresMouseInteraction = !show;

		if (!syncContainerDimensions || _syncDepth > 0)
			return;

		// Container already ran Recalculate with the old height; refresh dims now that Height is correct.
		_syncDepth++;
		try
		{
			container.Recalculate();
		}
		finally
		{
			_syncDepth--;
		}
	}

	private static float FindListPadding(UIElement element)
	{
		for (UIElement walk = element; walk is not null; walk = walk.Parent)
		{
			if (walk is UIList list)
				return list.ListPadding;
		}

		return 5f; // UIList default
	}

	private static void SetHeight(UIElement el, float h, float marginBottom)
	{
		el.MinHeight.Set(0f, 0f);
		el.MaxHeight.Set(h <= 0f ? 0f : float.MaxValue, 0f);
		el.Height.Set(h, 0f);
		// WrapIt assigns Height.Pixels directly — match that so UIList layout sees the change
		el.Height.Pixels = h;
		el.MarginTop = 0f;
		el.MarginBottom = marginBottom;
		if (h <= 0f)
		{
			el.PaddingTop = el.PaddingBottom = 0f;
		}
	}

	private static object ReadMember(object item, string name)
	{
		MemberInfo member = ResolveMember(item.GetType(), name);
		return member is null ? null : ReadMemberValue(item, member);
	}
}

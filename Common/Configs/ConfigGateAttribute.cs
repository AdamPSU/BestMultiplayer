using System;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BestMultiplayer.Common.Configs;

/// <summary>
/// Hide this config row unless a sibling member on the same config object equals <see cref="VisibleWhen"/>.
/// Default <see cref="VisibleWhen"/> is <c>true</c> (for bool masters).
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ConfigGateAttribute : Attribute
{
	public string MemberName { get; }

	public object VisibleWhen { get; }

	public ConfigGateAttribute(string memberName)
	{
		MemberName = memberName;
		VisibleWhen = true;
	}

	public ConfigGateAttribute(string memberName, object visibleWhen)
	{
		MemberName = memberName;
		VisibleWhen = visibleWhen;
	}
}

internal static class ConfigVisibility
{
	public const float RowHeight = 30f;

	public static bool IsVisible(object item, MemberInfo gatedMember)
	{
		var gate = gatedMember.GetCustomAttribute<ConfigGateAttribute>(inherit: true);
		if (gate is null || item is null)
			return true;

		object hostValue = ReadMember(item, gate.MemberName);
		if (hostValue is null)
			return false;

		object expected = gate.VisibleWhen;
		if (expected is null)
			return hostValue is true;

		if (hostValue is Enum && expected is not Enum)
		{
			try
			{
				expected = Enum.ToObject(hostValue.GetType(), expected);
			}
			catch
			{
				return false;
			}
		}

		return Equals(hostValue, expected);
	}

	/// <summary>
	/// tML wraps each config row in a UISortableElement. WrapIt snapshots container height AFTER
	/// OnBind (when Parent is still null). Sync the container every frame once attached, and force
	/// UIList to reflow when height changes.
	/// </summary>
	public static void Apply(UIElement element, bool show, ref bool? lastShown)
	{
		float h = show ? RowHeight : 0f;
		SetHeight(element, h);
		element.IgnoresMouseInteraction = !show;

		bool needReflow = lastShown != show;
		lastShown = show;

		// UISortableElement container — null during OnBind; WrapIt may also overwrite height after OnBind
		if (element.Parent is UIElement container)
		{
			if (Math.Abs(container.Height.Pixels - h) > 0.5f)
				needReflow = true;

			SetHeight(container, h);
			container.IgnoresMouseInteraction = !show;
		}

		if (!needReflow)
			return;

		element.Recalculate();
		element.Parent?.Recalculate();

		for (UIElement walk = element.Parent; walk is not null; walk = walk.Parent)
		{
			if (walk is UIList list)
			{
				list.Recalculate();
				break;
			}
		}
	}

	private static void SetHeight(UIElement el, float h)
	{
		el.MinHeight.Set(0f, 0f);
		el.MaxHeight.Set(h <= 0f ? 0f : float.MaxValue, 0f);
		el.Height.Set(h, 0f);
		// WrapIt assigns Height.Pixels directly — match that so UIList layout sees the change
		el.Height.Pixels = h;
	}

	private static object ReadMember(object item, string name)
	{
		Type type = item.GetType();
		FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field is not null)
			return field.GetValue(item);

		PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		return prop?.GetValue(item);
	}
}

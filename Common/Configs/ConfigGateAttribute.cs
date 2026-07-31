using System;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>Hide this config row unless a sibling bool member on the same config object is true.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ConfigGateAttribute : Attribute
{
	public string MemberName { get; }

	public ConfigGateAttribute(string memberName) => MemberName = memberName;
}

internal static class ConfigVisibility
{
	public const float RowHeight = 30f;

	public static bool IsVisible(object item, MemberInfo gatedMember)
	{
		var gate = gatedMember.GetCustomAttribute<ConfigGateAttribute>(inherit: true);
		return gate is null || (item is not null && ReadMember(item, gate.MemberName) is true);
	}

	/// <summary>True when the element is currently collapsed (hidden by a gate) and shouldn't draw.</summary>
	public static bool IsCollapsed(UIElement element) =>
		element.IgnoresMouseInteraction || element.GetDimensions().Height < 1f;

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

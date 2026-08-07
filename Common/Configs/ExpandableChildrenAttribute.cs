using System;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// On a parent bool config field: when On, expand these sibling members as indented child rows
/// (same visual idea as Timer Add-ons). Children should use <see cref="NestedChildPlaceholderElement"/>
/// so they do not also appear as top-level rows.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ExpandableChildrenAttribute : Attribute
{
	public string[] MemberNames { get; }

	public ExpandableChildrenAttribute(params string[] memberNames) =>
		MemberNames = memberNames ?? Array.Empty<string>();
}

using System;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Bool toggles sharing the same <see cref="GroupId"/> are mutually exclusive in the config UI
/// and enforced on load/change. Enabling one clears the others; the others gray out until it is off.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ExclusiveToggleGroupAttribute : Attribute
{
	public string GroupId { get; }

	public ExclusiveToggleGroupAttribute(string groupId = "default")
	{
		GroupId = groupId;
	}
}

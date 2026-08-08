using DefinitiveMultiplayer.Common.Drawing;
using DefinitiveMultiplayer.Common.Systems;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>Red/white dual-ring silhouette for the marked living player.</summary>
public sealed class MarkedDrawPlayer : ModPlayer
{
	public override void TransformDrawData(ref PlayerDrawSet drawInfo)
	{
		if (!MarkedSystem.IsMarked(Player))
			return;

		PlayerStatusOutline.Apply(
			ref drawInfo,
			MarkedSystem.Accent,
			MarkedSystem.DisplaySeconds,
			TransferPopup.IsFlashing);
	}
}

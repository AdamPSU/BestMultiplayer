using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Drawing;
using DefinitiveMultiplayer.Common.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>Speed bonus and gold/white dual-ring silhouette for the hot-potato holder.</summary>
public sealed class HotPotatoPlayer : ModPlayer
{
	public override void PostUpdateRunSpeeds()
	{
		if (!HotPotatoSystem.IsHolder(Player))
			return;

		int bonus = Utils.Clamp(ServerConfig.Instance.HotPotatoSpeedBonusPercent, 0, 100);
		if (bonus <= 0)
			return;

		float mult = 1f + bonus / 100f;
		Player.moveSpeed *= mult;
		Player.maxRunSpeed *= mult;
		Player.accRunSpeed *= mult;
	}

	public override void TransformDrawData(ref PlayerDrawSet drawInfo)
	{
		if (!HotPotatoSystem.IsHolder(Player))
			return;

		PlayerStatusOutline.Apply(
			ref drawInfo,
			HotPotatoSystem.Accent,
			HotPotatoSystem.DisplaySeconds,
			TransferPopup.IsFlashing);
	}
}

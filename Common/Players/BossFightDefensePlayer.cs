using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>
/// During boss fights, scales how much player defense applies to incoming non-PvP damage.
/// </summary>
public sealed class BossFightDefensePlayer : ModPlayer
{
	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (modifiers.PvP)
			return;

		if (!BossFightSystem.IsBossFightActive())
			return;

		float mult = Utils.Clamp(ServerConfig.Instance.BossFightDefenseMultiplier, 0.1f, 1f);
		if (mult >= 1f)
			return;

		modifiers.ScalingArmorPenetration += 1f - mult;
	}
}

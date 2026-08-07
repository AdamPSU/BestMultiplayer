using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>
/// During boss fights, scales non-PvP damage taken and all damage dealt by host-configured multipliers.
/// </summary>
public sealed class BossFightDamagePlayer : ModPlayer
{
	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (modifiers.PvP)
			return;

		if (!BossFightSystem.IsBossFightActive())
			return;

		float mult = Utils.Clamp(ServerConfig.Instance.BossFightDamageMultiplier, 0.5f, 3f);
		if (mult == 1f)
			return;

		modifiers.FinalDamage *= mult;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!BossFightSystem.IsBossFightActive())
			return;

		float mult = Utils.Clamp(ServerConfig.Instance.BossFightDamageDealtMultiplier, 0.5f, 3f);
		if (mult == 1f)
			return;

		modifiers.FinalDamage *= mult;
	}
}

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

		if (!TryBossMult(ServerConfig.Instance.BossFightDamageMultiplier, out float mult))
			return;

		modifiers.FinalDamage *= mult;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!TryBossMult(ServerConfig.Instance.BossFightDamageDealtMultiplier, out float mult))
			return;

		modifiers.FinalDamage *= mult;
	}

	private static bool TryBossMult(float raw, out float mult)
	{
		// Default 1×: skip boss scan entirely.
		if (raw == 1f)
		{
			mult = 1f;
			return false;
		}

		if (!BossFightSystem.IsBossFightActive())
		{
			mult = 1f;
			return false;
		}

		mult = Utils.Clamp(raw, 0.5f, 3f);
		return mult != 1f;
	}
}

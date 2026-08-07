using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>
/// During boss fights, scales non-PvP damage taken and damage dealt by Boss Balance,
/// then stacks Marked mults on the marked living player.
/// </summary>
public sealed class BossFightDamagePlayer : ModPlayer
{
	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (modifiers.PvP)
			return;

		if (!TryComposeMult(taken: true, out float mult))
			return;

		modifiers.FinalDamage *= mult;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!TryComposeMult(taken: false, out float mult))
			return;

		modifiers.FinalDamage *= mult;
	}

	private bool TryComposeMult(bool taken, out float mult)
	{
		mult = 1f;
		if (!BossFightSystem.IsBossFightActive())
			return false;

		ServerConfig cfg = ServerConfig.Instance;
		mult = taken ? cfg.ClampedDamageTakenMult() : cfg.ClampedDamageDealtMult();

		if (MarkedSystem.IsMarked(Player))
			mult *= taken ? cfg.ClampedMarkedTakenMult() : cfg.ClampedMarkedDealtMult();

		return mult != 1f;
	}
}

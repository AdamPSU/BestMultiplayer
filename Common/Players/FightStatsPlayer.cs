using BestMultiplayer.Common.Systems;
using Terraria;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Players;

public sealed class FightStatsPlayer : ModPlayer
{
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
		FightStatsSystem.NotifyDealt(Player, target, damageDone);

	public override void OnHurt(Player.HurtInfo info) =>
		FightStatsSystem.NotifyTaken(Player, info.Damage);
}

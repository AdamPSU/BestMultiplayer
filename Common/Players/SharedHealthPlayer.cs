using BestMultiplayer.Common.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Players;

public sealed class SharedHealthPlayer : ModPlayer
{
	/// <summary>Natural gear max captured while linked; used to restore after fight.</summary>
	internal int RealLifeMax;
	internal bool HasSnapshot;

	public override void PostUpdateMiscEffects()
	{
		if (!SharedHealthSystem.IsLinked(Player))
			return;

		// Gear already set statLifeMax2 this tick — snapshot before override.
		int gearMax = Player.statLifeMax2;
		if (gearMax > 0)
		{
			RealLifeMax = gearMax;
			HasSnapshot = true;
		}

		if (!SharedHealthSystem.TryGetPool(Player.team, out int cur, out int max) || max <= 0)
			return;

		Player.statLifeMax2 = max;

		// Clients: force current for the bar. Server/SP leave current for reconcile
		// so damage this tick is still visible as a life drop.
		if (Main.netMode == NetmodeID.MultiplayerClient)
			Player.statLife = Utils.Clamp(cur, 0, max);
	}

	public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
		ref PlayerDeathReason damageSource)
	{
		// Any member death while the team pool is live → DeathLink wipe.
		// Clients: server/host applies wipe; allow local death animation.
		SharedHealthSystem.NotifyMemberDeath(Player);
		return true;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		// Backup if PreKill was skipped or IsLinked-style checks would fail once dead.
		SharedHealthSystem.NotifyMemberDeath(Player);
	}

	internal void ClearSnapshot()
	{
		HasSnapshot = false;
		RealLifeMax = 0;
	}
}

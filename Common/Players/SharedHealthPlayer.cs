using System;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

public sealed class SharedHealthPlayer : ModPlayer
{
	/// <summary>Natural gear max (pre-paint). Snapshot in PostUpdateEquips every tick while teamed.</summary>
	internal int NaturalMax;
	internal bool HasNatural;

	/// <summary>
	/// Permanent crystal max stashed while we paint fake max for heart UI.
	/// tML heart count = LifeMax / (20 + (max2-max)/(max/20)); both must match visual.
	/// </summary>
	private int _stashedLifeMax;
	private bool _hasStashedLifeMax;

	/// <summary>Restore real crystal max before gear/natural snapshot each tick.</summary>
	public override void PreUpdate() => RestoreStashedLifeMax();

	/// <summary>
	/// Runs after gear applies to statLifeMax2 and before shared-health paint.
	/// Always keep a true personal max so pool math is (Σ natural) × mult.
	/// </summary>
	public override void PostUpdateEquips()
	{
		if (!SharedHealthSystem.IsEnabled() || Player.dead || !Teams.IsReal(Player.team))
			return;

		NaturalMax = Math.Max(1, Player.statLifeMax2);
		HasNatural = true;
	}

	public override void PostUpdateMiscEffects()
	{
		if (!SharedHealthSystem.IsLinked(Player))
			return;

		Player.lifeRegen = 0;
		Player.lifeRegenCount = 0;

		if (!SharedHealthSystem.TryGetPool(Player.team, out _, out int max) || max <= 0)
			return;

		// Local UI only (MP client). Listen-host/SP painted in SharedHealthSystem.
		if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
		{
			int cur = SharedHealthSystem.GetDisplayCurrent(Player.team);
			SharedHealthSystem.GetVisualLife(cur, max, out int vCur, out int vMax);
			ApplyVisualLifePaint(vCur, vMax);
		}
	}

	/// <summary>
	/// Paint heart UI: set both statLifeMax and statLifeMax2 so vanilla draws the right heart count.
	/// Call only for the local player (or SP). Remote server copies keep real pool values.
	/// </summary>
	internal void ApplyVisualLifePaint(int visualCurrent, int visualMax)
	{
		if (!_hasStashedLifeMax)
		{
			_stashedLifeMax = Math.Max(1, Player.statLifeMax);
			_hasStashedLifeMax = true;
		}

		int vMax = Math.Max(1, visualMax);
		int vCur = Utils.Clamp(visualCurrent, 0, vMax);
		Player.statLifeMax = vMax;
		Player.statLifeMax2 = vMax;
		Player.statLife = vCur;
	}

	internal void RestoreStashedLifeMax()
	{
		if (!_hasStashedLifeMax)
			return;

		Player.statLifeMax = Math.Max(1, _stashedLifeMax);
		_hasStashedLifeMax = false;
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (info.Damage <= 0)
			return;
		SharedHealthSystem.NotifyDamage(Player, info.Damage);
	}

	public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
		ref PlayerDeathReason damageSource)
	{
		if (!SharedHealthSystem.IsArmed() || !Teams.IsReal(Player.team))
			return true;

		// Wipe / DeathLink: never cancel — meta is sent before KillMe so clients know.
		if (SharedHealthSystem.IsTeamWiped(Player.team))
		{
			SharedHealthSystem.NotifyMemberDeath(Player);
			return true;
		}

		int cur = SharedHealthSystem.GetDisplayCurrent(Player.team);
		if (cur > 0)
		{
			// Organism still alive — this body does not die alone.
			if (SharedHealthSystem.TryGetPool(Player.team, out _, out int poolMax) && poolMax > 0)
			{
				SharedHealthSystem.GetVisualLife(cur, poolMax, out int vCur, out _);
				Player.statLife = vCur;
			}
			else
				Player.statLife = cur;
			return false;
		}

		// Pool empty (or unknown on client before meta): allow death; server wipes team.
		SharedHealthSystem.NotifyMemberDeath(Player);
		return true;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		SharedHealthSystem.NotifyMemberDeath(Player);
	}

	public override void OnRespawn()
	{
		// Rejoin the same organism bar; drop stale client prediction.
		SharedHealthSystem.NotifyLocalRespawn();
	}

	internal void ClearNatural()
	{
		RestoreStashedLifeMax();
		HasNatural = false;
		NaturalMax = 0;
	}
}

/// <summary>Potions and life pickups fold into the shared pool (hearts do not team-sick).</summary>
public sealed class SharedHealthHealItem : GlobalItem
{
	public override void OnConsumeItem(Item item, Player player)
	{
		if (item.healLife <= 0 || !SharedHealthSystem.IsLinked(player))
			return;
		if (!ShouldNotifyHeal(player))
			return;

		SharedHealCause cause = item.potion || IsHealingPotionType(item.type)
			? SharedHealCause.Potion
			: SharedHealCause.Other;
		SharedHealthSystem.NotifyHeal(player, item.healLife, cause);
	}

	public override bool OnPickup(Item item, Player player)
	{
		if (!SharedHealthSystem.IsLinked(player))
			return base.OnPickup(item, player);

		int heal = LifePickupHeal(item.type);
		if (heal <= 0 || !ShouldNotifyHeal(player))
			return base.OnPickup(item, player);

		SharedHealthSystem.NotifyHeal(player, heal, SharedHealCause.Heart);
		return base.OnPickup(item, player);
	}

	/// <summary>
	/// SP + local client always. Listen host (server + myPlayer) applies locally.
	/// Dedicated/remote server copies skip — the owning client sends HealDelta.
	/// </summary>
	private static bool ShouldNotifyHeal(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return player.whoAmI == Main.myPlayer;
		if (Main.netMode == NetmodeID.Server)
			return player.whoAmI == Main.myPlayer;
		return true;
	}

	private static int LifePickupHeal(int type) => type switch
	{
		ItemID.Heart or ItemID.CandyApple or ItemID.CandyCane => 20,
		_ => 0,
	};

	private static bool IsHealingPotionType(int type) =>
		type is ItemID.LesserHealingPotion or ItemID.HealingPotion or ItemID.GreaterHealingPotion
			or ItemID.SuperHealingPotion or ItemID.RestorationPotion or ItemID.BottledHoney
			or ItemID.Honeyfin or ItemID.Eggnog or ItemID.StrangeBrew;
}

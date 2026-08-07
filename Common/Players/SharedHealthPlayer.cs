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
	/// <summary>Natural gear max (pre-paint). Snapshot in PostUpdateEquips while teamed.</summary>
	internal int NaturalMax;

	/// <summary>Vanilla lifeRegen after buffs this tick (before we zero personal regen).</summary>
	internal int CapturedLifeRegen;

	/// <summary>Crystal max stashed while pool paints over statLifeMax. 0 = none.</summary>
	private int _stashedLifeMax;

	public override void PreUpdate() => RestoreStashedLifeMax();

	public override void PostUpdateEquips()
	{
		if (!SharedHealthSystem.IsEnabled() || Player.dead || !Teams.IsReal(Player.team))
			return;

		NaturalMax = Math.Max(1, Player.statLifeMax2);
	}

	public override void PostUpdateMiscEffects()
	{
		// MP client local UI only. SP / listen-host painted in SharedHealthSystem.
		// Must run before UpdateLifeRegen so vanilla heart UI uses pool max this frame.
		if (SharedHealthSystem.IsLinked(Player)
		    && Main.netMode == NetmodeID.MultiplayerClient
		    && Player.whoAmI == Main.myPlayer)
			SharedHealthSystem.PaintLocalBar(Player, Player.team);
	}

	// UpdateLifeRegen runs after PostUpdateMiscEffects and sets the final lifeRegen
	// (natural + gear + DoT). Capture here so server TickTeamRegen sees real rates.
	// Personal statLife still moves; pool is authority and paint overwrites the bar.
	public override void PostUpdate()
	{
		if (!SharedHealthSystem.IsLinked(Player))
		{
			CapturedLifeRegen = 0;
			return;
		}

		CapturedLifeRegen = Player.lifeRegen;
	}

	/// <summary>
	/// Pool → vanilla bar. max=min(400,pool) / max2=pool so fruit-split hearts deplete above 400.
	/// </summary>
	internal void ApplyPoolLifePaint(int poolCurrent, int poolMax)
	{
		if (_stashedLifeMax <= 0)
			_stashedLifeMax = Math.Max(1, Player.statLifeMax);

		int max = Math.Max(1, poolMax);
		Player.statLifeMax = Math.Min(400, max);
		Player.statLifeMax2 = max;
		Player.statLife = Utils.Clamp(poolCurrent, 0, max);
	}

	internal void RestoreStashedLifeMax()
	{
		if (_stashedLifeMax <= 0)
			return;

		Player.statLifeMax = Math.Max(1, _stashedLifeMax);
		_stashedLifeMax = 0;
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (info.Damage > 0)
			SharedHealthSystem.NotifyDamage(Player, info.Damage);
	}

	public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
		ref PlayerDeathReason damageSource)
	{
		if (!SharedHealthSystem.IsArmed() || !Teams.IsReal(Player.team))
			return true;

		if (SharedHealthSystem.IsTeamWiped(Player.team))
		{
			SharedHealthSystem.NotifyMemberDeath(Player);
			return true;
		}

		int cur = SharedHealthSystem.GetDisplayCurrent(Player.team);
		if (cur > 0)
		{
			// Cancel personal death; clamp to painted bar max so vanilla does not re-kill.
			int cap = Player.statLifeMax2 > 0 ? Player.statLifeMax2 : cur;
			Player.statLife = Utils.Clamp(cur, 1, cap);
			return false;
		}

		SharedHealthSystem.NotifyMemberDeath(Player);
		return true;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) =>
		SharedHealthSystem.NotifyMemberDeath(Player);

	public override void OnRespawn() => SharedHealthSystem.NotifyLocalRespawn();

	internal void ClearNatural()
	{
		RestoreStashedLifeMax();
		NaturalMax = 0;
		CapturedLifeRegen = 0;
	}
}

/// <summary>Potions and life pickups fold into the shared pool (hearts do not team-sick).</summary>
public sealed class SharedHealthHealItem : GlobalItem
{
	public override void OnConsumeItem(Item item, Player player)
	{
		if (item.healLife <= 0 || !SharedHealthSystem.IsLinked(player))
			return;

		SharedHealCause cause = item.potion ? SharedHealCause.Potion : SharedHealCause.Other;
		SharedHealthSystem.NotifyHeal(player, item.healLife, cause);
	}

	public override bool OnPickup(Item item, Player player)
	{
		if (!SharedHealthSystem.IsLinked(player))
			return base.OnPickup(item, player);

		int heal = LifePickupHeal(item.type);
		if (heal <= 0)
			return base.OnPickup(item, player);

		SharedHealthSystem.NotifyHeal(player, heal);
		return base.OnPickup(item, player);
	}

	private static int LifePickupHeal(int type) => type switch
	{
		ItemID.Heart or ItemID.CandyApple or ItemID.CandyCane => 20,
		_ => 0,
	};
}

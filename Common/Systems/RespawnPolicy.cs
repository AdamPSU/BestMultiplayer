using System;
using DefinitiveMultiplayer.Common.Configs;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>
/// Host-configured respawn wait. Outside boss: base only. Boss: base + per player + escalate, × boss mult. Event mult always.
/// </summary>
internal static class RespawnPolicy
{
	internal static int ComputeTimerTicks(int bossDeathsBeforeThisDeath)
	{
		ServerConfig cfg = ServerConfig.Instance;
		RespawnTimerAdjustments timer = cfg.RespawnTimer ??= new();
		timer.Sanitize();
		int priorDeaths = Math.Max(0, bossDeathsBeforeThisDeath);
		bool boss = BossFightSystem.IsBossFightActive();

		// Base always. Party-size and death-escalate only during bosses (co-op boss pacing).
		double seconds = cfg.RespawnBaseSeconds;
		if (boss)
		{
			seconds += timer.ExtraSecondsPerPlayer * Math.Max(0, CountActivePlayers() - 1);
			seconds += timer.ExtraSecondsPerBossDeath * priorDeaths;
			seconds *= timer.BossMultiplier;
		}

		if (IsEventActive())
			seconds *= timer.EventMultiplier;

		if (seconds <= 0.0)
			return 0;

		// Display/UX: whole seconds, rounded up to the next multiple of 5 (e.g. 12 → 15).
		int wholeSeconds = Math.Max(1, (int)Math.Ceiling(seconds));
		int roundedUp = ((wholeSeconds + 4) / 5) * 5;
		return roundedUp * 60;
	}

	internal static void ApplyVitalsOnRespawn(Player player)
	{
		ServerConfig cfg = ServerConfig.Instance;

		int lifeMax = Math.Max(1, player.statLifeMax2);
		int life = (int)Math.Round(lifeMax * (cfg.RespawnHealthPercent / 100.0));
		player.statLife = Utils.Clamp(life, 1, lifeMax);

		int manaMax = Math.Max(0, player.statManaMax2);
		if (manaMax <= 0)
		{
			player.statMana = 0;
			return;
		}

		int mana = (int)Math.Round(manaMax * (cfg.RespawnManaPercent / 100.0));
		player.statMana = Utils.Clamp(mana, 0, manaMax);
	}

	internal static int CountActivePlayers()
	{
		int n = 0;
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (Main.player[i].active)
				n++;
		}

		return n;
	}

	internal static bool IsEventActive()
	{
		if (Main.bloodMoon || Main.eclipse || Main.pumpkinMoon || Main.snowMoon)
			return true;

		if (Main.invasionType > InvasionID.None && Main.invasionSize > 0)
			return true;

		if (DD2Event.Ongoing)
			return true;

		if (NPC.LunarApocalypseIsUp)
			return true;

		return false;
	}
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Session/world helpers for boss-fight detection used by wormhole and respawn rules.
/// </summary>
public sealed class BossFightSystem : ModSystem
{
	public static bool IsBossFightActive()
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || !npc.HasValidTarget)
				continue;

			if (npc.boss)
				return true;

			if (npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
				return true;
		}

		return false;
	}
}

using System;
using DefinitiveMultiplayer.Common.Configs;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.NPCs;

/// <summary>
/// Scales boss-segment max HP at spawn by the host-configured boss health multiplier.
/// </summary>
public sealed class BossFightBossHealthGlobalNPC : GlobalNPC
{
	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if (!BossNpc.IsAnySegment(npc))
			return;

		float mult = ServerConfig.Instance.ClampedBossHealthMult();
		if (mult == 1f)
			return;

		npc.lifeMax = Math.Max(1, (int)(npc.lifeMax * mult));
		npc.life = npc.lifeMax;
	}
}

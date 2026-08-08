using System;
using DefinitiveMultiplayer.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.NPCs;

/// <summary>
/// Scales boss-segment max HP by the host-configured boss health multiplier.
/// Runs in SetDefaults (all sides) so client lifeMax matches server; vanilla
/// SyncNPC never transmits lifeMax, so OnSpawn-only scaling desyncs the boss bar.
/// </summary>
public sealed class BossFightBossHealthGlobalNPC : GlobalNPC
{
	public override void SetDefaults(NPC npc)
	{
		if (npc.IsABestiaryIconDummy)
			return;

		if (!BossNpc.IsAnySegment(npc))
			return;

		float mult = ServerConfig.Instance.ClampedBossHealthMult();
		if (mult == 1f)
			return;

		// Before ScaleStats: expert/MP/journey multipliers apply on top (commutative).
		// Do not set life here — SetDefaults ends with life = lifeMax after ScaleStats.
		npc.lifeMax = Math.Max(1, (int)(npc.lifeMax * mult));
	}
}

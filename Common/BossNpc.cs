using Terraria;
using Terraria.ID;

namespace DefinitiveMultiplayer.Common;

/// <summary>
/// Single source of truth for "which NPC type IDs are boss segments", shared by
/// BossFightSystem (fight-active detection) and SpectatePlayer (spectate targeting)
/// so the two lists can't silently drift apart when a new special-cased boss is added.
/// </summary>
internal static class BossNpc
{
	/// <summary>True for any segment of a boss, including multi-part bosses like Eater of Worlds.</summary>
	internal static bool IsAnySegment(NPC npc) =>
		npc.boss || npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

	/// <summary>True only for the single representative segment of a boss (e.g. EoW's head, not its body/tail).</summary>
	internal static bool IsCanonicalSegment(NPC npc) =>
		npc.boss || npc.type is NPCID.EaterofWorldsHead;
}

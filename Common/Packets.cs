using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common;

internal static class Packets
{
	internal const byte Section = 0;
	internal const byte PreferredRespawn = 1;
	/// <summary>Server → clients: shared-health meta (seq / armed / wiped / max / current / acks).</summary>
	internal const byte SharedHealthMeta = 2;
	/// <summary>Server → clients: boss-fight per-player stats snapshot.</summary>
	internal const byte FightStatsSnapshot = 3;
	/// <summary>Client → server: dealt/taken delta (OnHitNPC/OnHurt are client-local only).</summary>
	internal const byte FightStatsDelta = 4;
	/// <summary>Client → server: potion used (team sick scheduled server-side with heal).</summary>
	internal const byte SharedHealthPotionSick = 5;
	/// <summary>Server → clients: apply potion sickness to living team members.</summary>
	internal const byte SharedHealthTeamSick = 6;
	/// <summary>Client → server: damage event against shared pool.</summary>
	internal const byte SharedHealthDamage = 7;
	/// <summary>Client → server: heal event into shared pool.</summary>
	internal const byte SharedHealthHeal = 8;

	internal static ModPacket Begin(byte type)
	{
		ModPacket packet = ModContent.GetInstance<DefinitiveMultiplayer>().GetPacket();
		packet.Write(type);
		return packet;
	}
}

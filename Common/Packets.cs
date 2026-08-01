using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common;

internal static class Packets
{
	internal const byte Section = 0;
	internal const byte PreferredRespawn = 1;
	/// <summary>Server → clients: team shared HP pool snapshot.</summary>
	internal const byte SharedHealthPool = 2;
	/// <summary>Server → clients: boss-fight per-player stats snapshot.</summary>
	internal const byte FightStatsSnapshot = 3;
	/// <summary>Client → server: dealt/taken delta (OnHitNPC/OnHurt are client-local only).</summary>
	internal const byte FightStatsDelta = 4;

	/// <summary>Gets a new packet from the mod instance and writes the header byte.</summary>
	internal static ModPacket Begin(byte type)
	{
		ModPacket packet = ModContent.GetInstance<DefinitiveMultiplayer>().GetPacket();
		packet.Write(type);
		return packet;
	}
}

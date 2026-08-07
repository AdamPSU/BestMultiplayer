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
	// 5 reserved (removed unused SharedHealthPotionSick)
	/// <summary>Server → clients: apply potion sickness to living team members.</summary>
	internal const byte SharedHealthTeamSick = 6;
	/// <summary>Client → server: damage event against shared pool.</summary>
	internal const byte SharedHealthDamage = 7;
	/// <summary>Client → server: heal event into shared pool.</summary>
	internal const byte SharedHealthHeal = 8;
	/// <summary>Server → clients: player-swap countdown seconds (−1 clears the chat line).</summary>
	internal const byte PlayerSwapCountdown = 9;
	/// <summary>Server → clients: marked player whoAmI (−1 clears).</summary>
	internal const byte MarkedState = 10;
	/// <summary>Server → clients: mark-rotation countdown seconds (−1 clears the chat line).</summary>
	internal const byte MarkedCountdown = 11;
	/// <summary>Server → clients: hot-potato holder whoAmI (−1 clears).</summary>
	internal const byte HotPotatoState = 12;
	/// <summary>Server → clients: potato fuse countdown seconds (−1 clears the chat line).</summary>
	internal const byte HotPotatoCountdown = 13;

	internal static ModPacket Begin(byte type)
	{
		ModPacket packet = ModContent.GetInstance<DefinitiveMultiplayer>().GetPacket();
		packet.Write(type);
		return packet;
	}
}

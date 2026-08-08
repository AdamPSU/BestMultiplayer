using System.IO;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Drawing;
using DefinitiveMultiplayer.Common.Players;
using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer;

/// <summary>Thin Mod entry: lifecycle and custom packets only.</summary>
public sealed class DefinitiveMultiplayer : Mod
{
	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		switch (reader.ReadByte())
		{
			case Packets.Section:
				Vector2 position = reader.ReadVector2();
				if (Main.netMode == NetmodeID.Server)
					RemoteClient.CheckSection(whoAmI, position);
				break;

			case Packets.PreferredRespawn:
				byte raw = reader.ReadByte();
				if (Main.netMode == NetmodeID.Server)
					DefinitiveMultiplayerPlayer.HandlePreferredRespawnPacket(whoAmI, raw == 255 ? -1 : raw);
				break;

			case Packets.FightStatsSnapshot:
				FightStatsSystem.HandleSnapshotPacket(reader);
				break;

			case Packets.FightStatsDelta:
				FightStatsSystem.HandleDeltaPacket(reader, whoAmI);
				break;

			case Packets.SharedHealthMeta:
				SharedHealthSystem.HandleMetaPacket(reader);
				break;

			case Packets.SharedHealthTeamSick:
				SharedHealthSystem.HandleTeamSickPacket(reader);
				break;

			case Packets.SharedHealthDamage:
				SharedHealthSystem.HandleDamagePacket(reader, whoAmI);
				break;

			case Packets.SharedHealthHeal:
				SharedHealthSystem.HandleHealPacket(reader, whoAmI);
				break;

			case Packets.PlayerSwapCountdown:
				PlayerSwapSystem.HandleCountdownPacket(reader);
				break;

			case Packets.MarkedState:
				MarkedSystem.HandleStatePacket(reader);
				break;

			case Packets.MarkedCountdown:
				MarkedSystem.HandleCountdownPacket(reader);
				break;

			case Packets.HotPotatoState:
				HotPotatoSystem.HandleStatePacket(reader);
				break;

			case Packets.HotPotatoCountdown:
				HotPotatoSystem.HandleCountdownPacket(reader);
				break;
		}
	}

	public override void Unload()
	{
		SpectatePlayer.Clear();
		PlayerStatusOutline.Unload();
	}
}

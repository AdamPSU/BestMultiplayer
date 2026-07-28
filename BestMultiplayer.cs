using System.IO;
using BestMultiplayer.Common;
using BestMultiplayer.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer;

/// <summary>Thin Mod entry: lifecycle and custom packets only.</summary>
public sealed class BestMultiplayer : Mod
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
					BestMultiplayerPlayer.HandlePreferredRespawnPacket(whoAmI, raw == 255 ? -1 : raw);
				break;
		}
	}

	public override void Unload()
	{
		SpectatePlayer.Clear();
	}
}

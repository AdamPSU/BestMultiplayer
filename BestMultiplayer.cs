using System.IO;
using BestMultiplayer.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer;

/// <summary>
/// Mod entry. Keep thin: lifecycle and custom packets only.
/// Features live under Common/ (configs, players, systems, globals) and Content/.
/// </summary>
public sealed class BestMultiplayer : Mod
{
	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		byte id = reader.ReadByte();
		if (id == SpectatePlayer.PacketSection)
		{
			Vector2 position = reader.ReadVector2();
			if (Main.netMode == NetmodeID.Server)
				RemoteClient.CheckSection(whoAmI, position);
		}
	}

	public override void Unload()
	{
		SpectatePlayer.Clear();
	}
}

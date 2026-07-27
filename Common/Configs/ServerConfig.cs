using System.ComponentModel;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BestMultiplayer.Common.Configs;

/// <summary>
/// Shared multiplayer policy. Synced from the server; only the host may change it in-session.
/// </summary>
public sealed class ServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

	[Header("Teams")]
	[DrawTicks]
	[OptionStrings(new string[] { "None", "Red", "Green", "Blue", "Yellow", "Pink" })]
	[DefaultValue("Red")]
	public string TeamToJoin;

	[Header("BossFights")]
	[DefaultValue(true)]
	public bool NoBossFightRespawn;

	[Header("Wormholes")]
	[DefaultValue(true)]
	public bool FreeTeamWormhole;

	[DefaultValue(false)]
	public bool BlockFreeWormholeDuringBoss;

	public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
	{
		if (!NetMessage.DoesPlayerSlotCountAsAHost(whoAmI))
		{
			message = NetworkText.FromKey("tModLoader.ModConfigRejectChangesNotHost");
			return false;
		}

		return true;
	}
}

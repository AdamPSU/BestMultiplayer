using System.ComponentModel;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BestMultiplayer.Common.Configs;

public enum TeamToJoinOption
{
	None,
	Red,
	Green,
	Blue,
	Yellow,
	Pink,
}

public enum BossFightLivesMode
{
	Off,
	PerPlayer,
	PerTeam,
}

/// <summary>
/// Shared multiplayer policy. Synced from the server; only the host may change it in-session.
/// </summary>
public sealed class ServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

	[Header("Teams")]
	[Dropdown]
	[DefaultValue(TeamToJoinOption.Red)]
	public TeamToJoinOption TeamToJoin;

	[Header("BossFights")]
	[Dropdown]
	[DefaultValue(BossFightLivesMode.PerPlayer)]
	public BossFightLivesMode BossFightLivesMode;

	[CustomModConfigItem(typeof(BossFightRespawnsElement))]
	[Range(0, 99)]
	[DefaultValue(1)]
	public int BossFightRespawns;

	[DefaultValue(true)]
	public bool RespawnAtTeammateDuringBoss;

	[Header("Wormholes")]
	[DefaultValue(true)]
	public bool UnlimitedTeamTeleport;

	[DefaultValue(false)]
	public bool BlockUnlimitedTeleportDuringBoss;

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

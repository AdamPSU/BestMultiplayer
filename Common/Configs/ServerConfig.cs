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
[BackgroundColor(ConfigUiStyle.PanelR, ConfigUiStyle.PanelG, ConfigUiStyle.PanelB, ConfigUiStyle.PanelA)]
public sealed class ServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

	[Header("Teams")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[Dropdown]
	[DefaultValue(TeamToJoinOption.Red)]
	public TeamToJoinOption TeamToJoin;

	[Header("BossFights")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[Dropdown]
	[DefaultValue(BossFightLivesMode.PerPlayer)]
	public BossFightLivesMode BossFightLivesMode;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(BossFightRespawnsElement))]
	[Range(0, 99)]
	[DefaultValue(1)]
	public int BossFightRespawns;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool RespawnAtTeammateDuringBoss;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool BossFightStatsEnabled;

	[Header("SharedHealth")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(false)]
	public bool SharedHealthEnabled;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(SharedHealthEnabled))]
	[CustomModConfigItem(typeof(GatedBooleanElement))]
	[DefaultValue(false)]
	public bool SharedHealthBossesOnly;

	/// <summary>Pool max = sum(living max HP) × this. 0.5–1.5, default 1.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(SharedHealthEnabled))]
	[CustomModConfigItem(typeof(GatedFloatElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 1.5f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(0.5f)]
	public float SharedHealthMultiplier;

	[Header("Wormholes")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool UnlimitedTeamTeleport;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(UnlimitedTeamTeleport))]
	[CustomModConfigItem(typeof(GatedBooleanElement))]
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

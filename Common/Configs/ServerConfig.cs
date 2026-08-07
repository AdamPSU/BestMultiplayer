using System.ComponentModel;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DefinitiveMultiplayer.Common.Configs;

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
/// Shared multiplayer policy. Synced from the server; any client may save changes in-session.
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

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool UnlimitedTeamTeleport;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(UnlimitedTeamTeleport))]
	[CustomModConfigItem(typeof(GatedBooleanElement))]
	[DefaultValue(false)]
	public bool BlockUnlimitedTeleportDuringBoss;

	[Header("BossFights")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[Dropdown]
	[DefaultValue(BossFightLivesMode.PerPlayer)]
	public BossFightLivesMode BossFightLivesMode;

	/// <summary>Per-player lives during a boss fight. 1 = no respawns; internal respawn budget is lives − 1.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(BossFightLivesMode), (int)BossFightLivesMode.PerPlayer)]
	[CustomModConfigItem(typeof(GatedIntElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1, 5)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(2)]
	public int BossFightLives;

	/// <summary>Per-team shared pool. 0 = team size at fight start; 1–20 = fixed pool size.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(BossFightLivesMode), (int)BossFightLivesMode.PerTeam)]
	[CustomModConfigItem(typeof(BossFightTeamLivesElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 20)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(0)]
	public int BossFightTeamLives;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool RespawnAtTeammateDuringBoss;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool BossFightStatsEnabled;

	/// <summary>Fraction of player defense that applies during boss fights. 1 = full; 0.1 = 10%.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.1f, 1f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(1f)]
	public float BossFightDefenseMultiplier;

	[Header("SharedHealth")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(false)]
	public bool SharedHealthEnabled;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(SharedHealthEnabled))]
	[CustomModConfigItem(typeof(GatedBooleanElement))]
	[DefaultValue(false)]
	public bool SharedHealthBossesOnly;

	/// <summary>Pool max = sum(living max HP) × this. 1–3, default 1.5.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(SharedHealthEnabled))]
	[CustomModConfigItem(typeof(GatedFloatElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1f, 3f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(1.5f)]
	public float SharedHealthMultiplier;

	// Cloud / dedicated hosts often have no in-game "host" player; allow any client to save.
	public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) =>
		true;
}

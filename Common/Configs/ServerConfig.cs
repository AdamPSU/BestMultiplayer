using System.ComponentModel;
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

/// <summary>
/// Shared multiplayer policy. Synced from the server; any client may save changes in-session.
/// </summary>
[BackgroundColor(ConfigUiStyle.PanelR, ConfigUiStyle.PanelG, ConfigUiStyle.PanelB, ConfigUiStyle.PanelA)]
public sealed class ServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

	// --- Teams (join, teleport, shared health) ---

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

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(false)]
	public bool SharedHealthEnabled;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(SharedHealthEnabled))]
	[CustomModConfigItem(typeof(GatedBooleanElement))]
	[DefaultValue(false)]
	public bool SharedHealthBossesOnly;

	/// <summary>Pool max = sum(living max HP) × this when 2+ living. Solo ignores mult. 0.5–2, default 0.75.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(SharedHealthEnabled))]
	[CustomModConfigItem(typeof(GatedFloatElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 2f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(0.75f)]
	public float SharedHealthMultiplier;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool BossFightStatsEnabled;

	// --- Boss Lives (player and team limits can both be on; both must allow a respawn) ---

	[Header("BossLives")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool BossFightPlayerLivesEnabled;

	/// <summary>Per-player lives during a boss fight. 1 = no respawns; internal respawn budget is lives − 1.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(BossFightPlayerLivesEnabled))]
	[CustomModConfigItem(typeof(GatedIntElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1, 5)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(3)]
	public int BossFightLives;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(false)]
	public bool BossFightTeamLivesEnabled;

	/// <summary>Per-team shared pool. 0 = team size at fight start; 1–20 = fixed pool size.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ConfigGate(nameof(BossFightTeamLivesEnabled))]
	[CustomModConfigItem(typeof(BossFightTeamLivesElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 20)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(0)]
	public int BossFightTeamLives;

	// --- Boss Balance ---

	[Header("BossBalance")]
	/// <summary>Damage-taken multiplier during boss fights. 1 = vanilla; 0.5 = half; 2 = double.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(1f)]
	public float BossFightDamageMultiplier;

	/// <summary>Damage-dealt multiplier during boss fights. 1 = vanilla; 0.5 = half; 2 = double.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(1f)]
	public float BossFightDamageDealtMultiplier;

	/// <summary>Boss segment lifeMax multiplier at spawn. 1 = vanilla; 0.5 = half HP; 2 = double HP.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(1f)]
	public float BossFightBossHealthMultiplier;

	// --- Respawn (last) ---

	[Header("Respawn")]
	/// <summary>Base respawn wait in seconds. 0 = instant when respawn is allowed.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 120)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(5)]
	public int RespawnBaseSeconds;

	/// <summary>Optional extras on top of base wait (party size, boss/event multipliers, escalate).</summary>
	[Expand(false)]
	[CustomModConfigItem(typeof(ExpandableObjectElement))]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	public RespawnTimerAdjustments RespawnTimer = new();

	/// <summary>Percent of max life on respawn (1–100).</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1, 100)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(100)]
	public int RespawnHealthPercent;

	/// <summary>Percent of max mana on respawn (0–100).</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 100)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(100)]
	public int RespawnManaPercent;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool RespawnAtTeammateDuringBoss;

	// Cloud / dedicated hosts often have no in-game "host" player; allow any client to save.
	public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) =>
		true;

	public override void OnLoaded()
	{
		RespawnTimer ??= new();
		RespawnTimer.Sanitize();
	}

	public override void OnChanged()
	{
		RespawnTimer ??= new();
		RespawnTimer.Sanitize();
	}
}

/// <summary>Advanced respawn-timer knobs (collapsed under Respawn).</summary>
/// <remarks>
/// Field initializers matter: nested objects are constructed with <c>new()</c>, and
/// <see cref="DefaultValueAttribute"/> alone does not populate those C# defaults.
/// </remarks>
[BackgroundColor(ConfigUiStyle.PanelR, ConfigUiStyle.PanelG, ConfigUiStyle.PanelB, ConfigUiStyle.PanelA)]
public sealed class RespawnTimerAdjustments
{
	/// <summary>During boss fights only: added seconds per active player beyond the first.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 30)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(3)]
	public int ExtraSecondsPerPlayer = 3;

	/// <summary>Multiplier while a boss fight is active.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(1.5f)]
	public float BossMultiplier = 1.5f;

	/// <summary>Multiplier during invasions, moons, blood moon, eclipse, DD2, lunar events.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(1f)]
	public float EventMultiplier = 1f;

	/// <summary>Extra seconds × prior deaths this boss fight (0 = off). Resets when the fight ends.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 60)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(3)]
	public int ExtraSecondsPerBossDeath = 3;

	/// <summary>
	/// Repair values from older saves where nested fields were left at C# zero
	/// (<see cref="DefaultValueAttribute"/> does not run on <c>new()</c> nested objects).
	/// </summary>
	public void Sanitize()
	{
		// Whole nested blob stuck at C# defaults → apply intended ship defaults once.
		if (BossMultiplier < 0.5f && EventMultiplier < 0.5f
		    && ExtraSecondsPerPlayer == 0 && ExtraSecondsPerBossDeath == 0)
		{
			ExtraSecondsPerPlayer = 3;
			BossMultiplier = 1.5f;
			EventMultiplier = 1f;
			ExtraSecondsPerBossDeath = 3;
			return;
		}

		if (BossMultiplier < 0.5f)
			BossMultiplier = 1.5f;
		if (EventMultiplier < 0.5f)
			EventMultiplier = 1f;
	}
}

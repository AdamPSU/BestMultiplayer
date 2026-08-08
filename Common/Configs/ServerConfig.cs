using System.ComponentModel;
using DefinitiveMultiplayer.Common.Systems;
using Newtonsoft.Json;
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

/// <summary>
/// Shared multiplayer policy. Synced from the server; any client may save changes in-session.
/// </summary>
[BackgroundColor(ConfigUiStyle.PanelR, ConfigUiStyle.PanelG, ConfigUiStyle.PanelB, ConfigUiStyle.PanelA)]
public sealed class ServerConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ServerSide;

	public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

	// --- Teams (join, teleport, stats) ---

	[Header("Teams")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[Dropdown]
	[DefaultValue(TeamToJoinOption.Red)]
	public TeamToJoinOption TeamToJoin;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ExpandableChildren(nameof(BlockUnlimitedTeleportDuringBoss))]
	[CustomModConfigItem(typeof(ExpandableToggleElement))]
	[DefaultValue(true)]
	public bool UnlimitedTeamTeleport;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[DefaultValue(false)]
	public bool BlockUnlimitedTeleportDuringBoss;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool BossFightStatsEnabled;

	// --- Modes ---

	[Header("Modes")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ExpandableChildren(nameof(SharedHealthBossesOnly), nameof(SharedHealthMultiplier))]
	[CustomModConfigItem(typeof(ExpandableToggleElement))]
	[DefaultValue(false)]
	public bool SharedHealthEnabled;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[DefaultValue(false)]
	public bool SharedHealthBossesOnly;

	/// <summary>Pool max = sum(living max HP) × this when 2+ living. Solo ignores mult. 0.5–2, default 0.75.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 2f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(0.75f)]
	public float SharedHealthMultiplier;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ExclusiveToggleGroup("Modes")]
	[ExpandableChildren(nameof(PlayerSwapIntervalMinutes))]
	[CustomModConfigItem(typeof(ExpandableToggleElement))]
	[DefaultValue(false)]
	public bool PlayerSwapEnabled;

	/// <summary>Minutes between teammate position shuffles. 1–30, default 5.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1, 30)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(5)]
	public int PlayerSwapIntervalMinutes;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ExclusiveToggleGroup("Modes")]
	[ExpandableChildren(nameof(MarkedIntervalSeconds), nameof(MarkedDamageTakenMult), nameof(MarkedDamageDealtMult))]
	[CustomModConfigItem(typeof(ExpandableToggleElement))]
	[DefaultValue(false)]
	public bool MarkedEnabled;

	/// <summary>Seconds between mark rotations during boss fights. 15–120, step 5, default 60.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(15, 120)]
	[Increment(5)]
	[DrawTicks]
	[DefaultValue(60)]
	public int MarkedIntervalSeconds;

	/// <summary>Extra damage-taken mult for the marked player on top of Boss Balance. Default 1.5.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1f, 3f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(1.5f)]
	public float MarkedDamageTakenMult;

	/// <summary>Extra damage-dealt mult for the marked player on top of Boss Balance. Default 1.15.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1f, 3f)]
	[Increment(0.05f)]
	[DrawTicks]
	[DefaultValue(1.15f)]
	public float MarkedDamageDealtMult;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[ExclusiveToggleGroup("Modes")]
	[ExpandableChildren(
		nameof(HotPotatoBossesOnly),
		nameof(HotPotatoIntervalSeconds),
		nameof(HotPotatoSpeedBonusPercent),
		nameof(HotPotatoTeamOnly))]
	[CustomModConfigItem(typeof(ExpandableToggleElement))]
	[DefaultValue(false)]
	public bool HotPotatoEnabled;

	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[DefaultValue(false)]
	public bool HotPotatoBossesOnly;

	/// <summary>Seconds until potato explodes. 30–300, step 15, default 90.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(30, 300)]
	[Increment(15)]
	[DrawTicks]
	[DefaultValue(90)]
	public int HotPotatoIntervalSeconds;

	/// <summary>Move-speed bonus % for the holder. 0–100, default 25.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 100)]
	[Increment(5)]
	[DrawTicks]
	[DefaultValue(25)]
	public int HotPotatoSpeedBonusPercent;

	/// <summary>When true, only same-team living players are in the pool / pass targets.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(NestedChildPlaceholderElement))]
	[DefaultValue(true)]
	public bool HotPotatoTeamOnly;

	/// <summary>UI-only footnote under Modes (not a setting).</summary>
	[JsonIgnore]
	[ShowDespiteJsonIgnore]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(ConfigNoteElement))]
	public bool ModesExclusiveNote;

	// --- Boss Lives (player and team limits can both be on; both must allow a respawn) ---
	// Slider max = Vanilla (off). Lower values = fewer lives.

	/// <summary>Rightmost slider step: unlimited vanilla respawns (player life limit off).</summary>
	public const int PlayerLivesVanilla = 6;

	/// <summary>Rightmost slider step: team life limit off.</summary>
	public const int TeamLivesVanilla = 21;

	[Header("BossLives")]
	/// <summary>Per-player lives (1–5). <see cref="PlayerLivesVanilla"/> = off. Default 3.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(BossFightPlayerLivesElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(1, PlayerLivesVanilla)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(3)]
	public int BossFightLives;

	/// <summary>Team pool: 0 = team size at fight start; 1–20 = fixed; <see cref="TeamLivesVanilla"/> = off.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[CustomModConfigItem(typeof(BossFightTeamLivesElement))]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, TeamLivesVanilla)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(TeamLivesVanilla)]
	public int BossFightTeamLives;

	/// <summary>True when the player life slider is not Vanilla.</summary>
	internal bool BossFightPlayerLivesEnabled => BossFightLives < PlayerLivesVanilla;

	/// <summary>True when the team life slider is not Vanilla.</summary>
	internal bool BossFightTeamLivesEnabled => BossFightTeamLives < TeamLivesVanilla;

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
	[Increment(0.25f)]
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

	// Prior mode flags: exclusive last-wins + chat announce diffs.
	private static bool _prevSharedHealthEnabled;
	private static bool _prevPlayerSwapEnabled;
	private static bool _prevMarkedEnabled;
	private static bool _prevHotPotatoEnabled;

	public override void OnLoaded()
	{
		EnsureRespawnTimer();
		ClampLivesSliders();
		EnforceExclusiveModes();
		SnapshotModeFlags();
	}

	public override void OnChanged()
	{
		bool prevShared = _prevSharedHealthEnabled;
		bool prevSwap = _prevPlayerSwapEnabled;
		bool prevMarked = _prevMarkedEnabled;
		bool prevPotato = _prevHotPotatoEnabled;

		EnsureRespawnTimer();
		ClampLivesSliders();
		EnforceExclusiveModes();
		ConfigModeChatSystem.AnnounceModeDiffs(this, prevShared, prevSwap, prevMarked, prevPotato);
		SnapshotModeFlags();
	}

	public override void HandleAcceptClientChangesReply(bool success, int player, NetworkText message)
	{
		if (success)
			ConfigModeChatSystem.ArmSharedConfigStrip();
	}

	private void SnapshotModeFlags()
	{
		_prevSharedHealthEnabled = SharedHealthEnabled;
		_prevPlayerSwapEnabled = PlayerSwapEnabled;
		_prevMarkedEnabled = MarkedEnabled;
		_prevHotPotatoEnabled = HotPotatoEnabled;
	}

	/// <summary>Player Swap is on and exclusive siblings are off.</summary>
	internal bool PlayerSwapModeActive =>
		PlayerSwapEnabled && !MarkedEnabled && !HotPotatoEnabled;

	/// <summary>Marked is on and exclusive siblings are off.</summary>
	internal bool MarkedModeActive =>
		MarkedEnabled && !PlayerSwapEnabled && !HotPotatoEnabled;

	/// <summary>Hot Potato is on and exclusive siblings are off.</summary>
	internal bool HotPotatoModeActive =>
		HotPotatoEnabled && !MarkedEnabled && !PlayerSwapEnabled;

	/// <summary>
	/// Player Swap, Marked, and Hot Potato are mutually exclusive.
	/// Last-enabled wins when the user toggles; load/migrate with multiple on keeps Player Swap, else Marked, else Hot Potato.
	/// </summary>
	internal void EnforceExclusiveModes()
	{
		int on = (PlayerSwapEnabled ? 1 : 0) + (MarkedEnabled ? 1 : 0) + (HotPotatoEnabled ? 1 : 0);
		if (on <= 1)
			return;

		bool swapJust = PlayerSwapEnabled && !_prevPlayerSwapEnabled;
		bool markJust = MarkedEnabled && !_prevMarkedEnabled;
		bool potJust = HotPotatoEnabled && !_prevHotPotatoEnabled;
		int just = (swapJust ? 1 : 0) + (markJust ? 1 : 0) + (potJust ? 1 : 0);

		if (just == 1)
		{
			PlayerSwapEnabled = swapJust;
			MarkedEnabled = markJust;
			HotPotatoEnabled = potJust;
			return;
		}

		// Load / multi-flip: priority Player Swap > Marked > Hot Potato.
		if (PlayerSwapEnabled)
		{
			MarkedEnabled = false;
			HotPotatoEnabled = false;
		}
		else if (MarkedEnabled)
		{
			HotPotatoEnabled = false;
		}
	}

	internal void EnsureRespawnTimer()
	{
		RespawnTimer ??= new();
		RespawnTimer.Sanitize();
	}

	internal void ClampLivesSliders()
	{
		BossFightLives = Utils.Clamp(BossFightLives, 1, PlayerLivesVanilla);
		BossFightTeamLives = Utils.Clamp(BossFightTeamLives, 0, TeamLivesVanilla);
	}

	internal float ClampedDamageTakenMult() =>
		Utils.Clamp(BossFightDamageMultiplier, 0.5f, 3f);

	internal float ClampedDamageDealtMult() =>
		Utils.Clamp(BossFightDamageDealtMultiplier, 0.5f, 3f);

	internal float ClampedBossHealthMult() =>
		Utils.Clamp(BossFightBossHealthMultiplier, 0.5f, 3f);

	internal float ClampedMarkedTakenMult() =>
		Utils.Clamp(MarkedDamageTakenMult, 1f, 3f);

	internal float ClampedMarkedDealtMult() =>
		Utils.Clamp(MarkedDamageDealtMult, 1f, 3f);
}

/// <summary>Advanced respawn-timer knobs (collapsed under Respawn).</summary>
/// <remarks>
/// Field initializers matter: nested objects are constructed with <c>new()</c>, and
/// <see cref="DefaultValueAttribute"/> alone does not populate those C# defaults.
/// </remarks>
[BackgroundColor(ConfigUiStyle.PanelR, ConfigUiStyle.PanelG, ConfigUiStyle.PanelB, ConfigUiStyle.PanelA)]
public sealed class RespawnTimerAdjustments
{
	public const int DefaultExtraSecondsPerPlayer = 3;
	public const float DefaultBossMultiplier = 1.5f;
	public const float DefaultEventMultiplier = 1f;
	public const int DefaultExtraSecondsPerBossDeath = 3;

	/// <summary>During boss fights only: added seconds per active player beyond the first.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 30)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(DefaultExtraSecondsPerPlayer)]
	public int ExtraSecondsPerPlayer = DefaultExtraSecondsPerPlayer;

	/// <summary>Multiplier while a boss fight is active.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(DefaultBossMultiplier)]
	public float BossMultiplier = DefaultBossMultiplier;

	/// <summary>Multiplier during invasions, moons, blood moon, eclipse, DD2, lunar events.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0.5f, 3f)]
	[Increment(0.1f)]
	[DrawTicks]
	[DefaultValue(DefaultEventMultiplier)]
	public float EventMultiplier = DefaultEventMultiplier;

	/// <summary>Extra seconds × prior deaths this boss fight (0 = off). Resets when the fight ends.</summary>
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
	[Range(0, 60)]
	[Increment(1)]
	[DrawTicks]
	[DefaultValue(DefaultExtraSecondsPerBossDeath)]
	public int ExtraSecondsPerBossDeath = DefaultExtraSecondsPerBossDeath;

	/// <summary>
	/// Repair values from older saves where nested fields were left at C# zero
	/// (<see cref="DefaultValueAttribute"/> does not run on <c>new()</c> nested objects).
	/// </summary>
	public void Sanitize()
	{
		// Whole nested blob stuck at C# zeros → ship defaults once.
		if (BossMultiplier < 0.5f && EventMultiplier < 0.5f
		    && ExtraSecondsPerPlayer == 0 && ExtraSecondsPerBossDeath == 0)
		{
			ExtraSecondsPerPlayer = DefaultExtraSecondsPerPlayer;
			BossMultiplier = DefaultBossMultiplier;
			EventMultiplier = DefaultEventMultiplier;
			ExtraSecondsPerBossDeath = DefaultExtraSecondsPerBossDeath;
			return;
		}

		if (BossMultiplier < 0.5f)
			BossMultiplier = DefaultBossMultiplier;
		if (EventMultiplier < 0.5f)
			EventMultiplier = DefaultEventMultiplier;
	}
}

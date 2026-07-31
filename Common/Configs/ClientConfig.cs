using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>Local presentation preferences. Not synced.</summary>
[BackgroundColor(ConfigUiStyle.PanelR, ConfigUiStyle.PanelG, ConfigUiStyle.PanelB, ConfigUiStyle.PanelA)]
public sealed class ClientConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	public static ClientConfig Instance => ModContent.GetInstance<ClientConfig>();

	[Header("Spectate")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool SpectateOnDeath;

	[Header("FightStats")]
	[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
	[DefaultValue(true)]
	public bool ShowBossFightStats;
}

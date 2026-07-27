using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace BestMultiplayer.Common.Configs;

/// <summary>
/// Local presentation preferences. Not synced; each client keeps their own values.
/// </summary>
public sealed class ClientConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;

	public static ClientConfig Instance => ModContent.GetInstance<ClientConfig>();

	[Header("Spectate")]
	[DefaultValue(true)]
	public bool SpectateOnDeath;

	[DefaultValue(true)]
	public bool StopSpectateOnRespawn;
}

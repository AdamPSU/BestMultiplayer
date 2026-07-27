using BestMultiplayer.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Systems;

/// <summary>
/// Pretends the player has a wormhole potion so vanilla map team-teleport works without inventory stock.
/// </summary>
public sealed class WormholeSystem : ModSystem
{
	public override void Load()
	{
		On_Player.HasUnityPotion += HasUnityPotion;
		On_Player.TakeUnityPotion += TakeUnityPotion;
	}

	public override void Unload()
	{
		On_Player.HasUnityPotion -= HasUnityPotion;
		On_Player.TakeUnityPotion -= TakeUnityPotion;
	}

	private static bool HasUnityPotion(On_Player.orig_HasUnityPotion orig, Player player)
	{
		if (ShouldFakeWormhole(player))
			return true;

		return orig(player);
	}

	private static void TakeUnityPotion(On_Player.orig_TakeUnityPotion orig, Player player)
	{
		if (ShouldFakeWormhole(player))
			return;

		orig(player);
	}

	internal static bool ShouldFakeWormhole(Player player)
	{
		if (player.dead)
			return false;

		ServerConfig config = ServerConfig.Instance;
		if (config is null || !config.UnlimitedTeamTeleport)
			return false;

		if (config.BlockUnlimitedTeleportDuringBoss && BossFightSystem.IsBossFightActive())
			return false;

		return true;
	}
}

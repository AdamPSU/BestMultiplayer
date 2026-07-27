using BestMultiplayer.Common.Configs;
using BestMultiplayer.Common.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Players;

public sealed class BestMultiplayerPlayer : ModPlayer
{
	internal int RespawnsRemaining;
	internal bool RespawnAllowedThisDeath;
	internal bool LivesInitialized;

	public override void OnEnterWorld()
	{
		TeamToJoinOption choice = ServerConfig.Instance.TeamToJoin;
		if (choice == TeamToJoinOption.None)
			return;

		// Enum order matches vanilla team ids (None=0 … Pink=5).
		Player.team = (int)choice;
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.PlayerTeam, -1, -1, null, Player.whoAmI);
	}

	public override void OnRespawn()
	{
		RespawnAllowedThisDeath = false;
	}

	public override void UpdateDead()
	{
		if (BossFightSystem.MayRespawnThisDeath(Player))
			return;

		int t = 1200;
		if (Main.expertMode)
			t = 1800;
		if (Main.getGoodWorld)
			t = 3600;
		Player.respawnTimer = t;
	}
}

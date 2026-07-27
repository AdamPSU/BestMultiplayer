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
		if (!TryTeamId(ServerConfig.Instance.TeamToJoin, out int team))
			return;

		Player.team = team;
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

	private static bool TryTeamId(string name, out int team) => (team = name switch
	{
		"Red" => 1,
		"Green" => 2,
		"Blue" => 3,
		"Yellow" => 4,
		"Pink" => 5,
		_ => 0,
	}) != 0;
}

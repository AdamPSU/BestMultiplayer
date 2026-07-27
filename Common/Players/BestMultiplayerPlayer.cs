using BestMultiplayer.Common.Configs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Players;

public sealed class BestMultiplayerPlayer : ModPlayer
{
	public override void OnEnterWorld()
	{
		if (!TryTeamId(ServerConfig.Instance.TeamToJoin, out int team))
			return;

		Player.team = team;
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.PlayerTeam, -1, -1, null, Player.whoAmI);
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

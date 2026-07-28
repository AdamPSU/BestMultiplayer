using BestMultiplayer.Common;
using BestMultiplayer.Common.Configs;
using BestMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Players;

public sealed class BestMultiplayerPlayer : ModPlayer
{
	internal int RespawnsRemaining;
	internal bool RespawnAllowedThisDeath;
	internal bool LivesInitialized;
	internal bool DiedDuringBossFight;
	internal int PreferredRespawnWhoAmI = -1;

	public override void OnEnterWorld()
	{
		TeamToJoinOption choice = ServerConfig.Instance.TeamToJoin;
		if (choice == TeamToJoinOption.None)
			return;

		Player.team = (int)choice;
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.PlayerTeam, -1, -1, null, Player.whoAmI);
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		DiedDuringBossFight = BossFightSystem.IsBossFightActive();
		if (Player.whoAmI == Main.myPlayer)
			SetPreferredRespawnTarget(-1);
	}

	public override void OnRespawn()
	{
		RespawnAllowedThisDeath = false;

		bool wantTeammate = DiedDuringBossFight
			&& (ServerConfig.Instance?.RespawnAtTeammateDuringBoss ?? true);
		DiedDuringBossFight = false;

		int preferred = PreferredRespawnWhoAmI;
		PreferredRespawnWhoAmI = -1;

		if (!wantTeammate || Main.netMode == NetmodeID.MultiplayerClient)
			return;

		if (preferred < 0 || !IsLivingTeammate(Player, preferred))
			return;

		Player mate = Main.player[preferred];
		float offsetX = mate.direction != 0 ? -mate.direction * 32f : 32f;
		Player.Teleport(mate.position + new Vector2(offsetX, 0f), TeleportationStyleID.TeleportationPotion);
		Player.fallStart = (int)(Player.position.Y / 16f);
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

	internal static bool IsLivingTeammate(Player self, int whoAmI)
	{
		if (whoAmI < 0 || whoAmI >= Main.maxPlayers || whoAmI == self.whoAmI)
			return false;

		Player p = Main.player[whoAmI];
		return p.active && !p.dead && p.team == self.team;
	}

	internal static void SetPreferredRespawnTarget(int whoAmI)
	{
		Player local = Main.LocalPlayer;
		if (!local.active)
			return;

		BestMultiplayerPlayer mp = local.GetModPlayer<BestMultiplayerPlayer>();
		int value = whoAmI >= 0 && IsLivingTeammate(local, whoAmI) ? whoAmI : -1;
		if (mp.PreferredRespawnWhoAmI == value)
			return;

		mp.PreferredRespawnWhoAmI = value;
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		ModPacket packet = mp.Mod.GetPacket();
		packet.Write(Packets.PreferredRespawn);
		packet.Write((byte)(value < 0 ? 255 : value));
		packet.Send();
	}

	internal static void HandlePreferredRespawnPacket(int fromWho, int whoAmI)
	{
		if (fromWho < 0 || fromWho >= Main.maxPlayers)
			return;

		Player player = Main.player[fromWho];
		if (!player.active)
			return;

		player.GetModPlayer<BestMultiplayerPlayer>().PreferredRespawnWhoAmI =
			whoAmI >= 0 && IsLivingTeammate(player, whoAmI) ? whoAmI : -1;
	}
}

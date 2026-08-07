using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

public sealed class DefinitiveMultiplayerPlayer : ModPlayer
{
	internal int RespawnsRemaining;
	internal bool RespawnAllowedThisDeath;
	internal bool LivesInitialized;
	internal bool DiedDuringBossFight;
	internal int PreferredRespawnWhoAmI = -1;

	/// <summary>Boss-fight deaths so far this fight (escalating respawn). Cleared when the fight ends.</summary>
	internal int BossDeathsThisFight;

	/// <summary>Prior deaths used for this death's timer (set in Kill; lives path reapplies with it).</summary>
	internal int PriorBossDeathsForTimer;

	/// <summary>Set in OnRespawn; applied in PostUpdate after Spawn_SetPosition.</summary>
	private int _pendingTeammate = -1;

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
		bool boss = BossFightSystem.IsBossFightActive();
		DiedDuringBossFight = boss;
		_pendingTeammate = -1;
		if (Player.whoAmI == Main.myPlayer)
			SetPreferredRespawnTarget(-1);

		PriorBossDeathsForTimer = boss ? BossDeathsThisFight : 0;
		if (boss)
			BossDeathsThisFight++;

		// Lives mode applies the policy timer after spend (hard-lock may overwrite until then).
		if (!(boss && BossFightSystem.IsLivesModeActive()))
			Player.respawnTimer = RespawnPolicy.ComputeTimerTicks(PriorBossDeathsForTimer);
	}

	public override void OnRespawn()
	{
		RespawnAllowedThisDeath = false;

		bool wantTeammate = DiedDuringBossFight && ServerConfig.Instance.RespawnAtTeammateDuringBoss;
		DiedDuringBossFight = false;

		int preferred = PreferredRespawnWhoAmI;
		PreferredRespawnWhoAmI = -1;

		RespawnPolicy.ApplyVitalsOnRespawn(Player);

		// Spawn_SetPosition runs after OnRespawn — defer teleport to PostUpdate.
		if (wantTeammate && Main.netMode != NetmodeID.MultiplayerClient)
			_pendingTeammate = ResolveTeammate(preferred);
	}

	public override void PostUpdate()
	{
		if (_pendingTeammate < 0 || Player.dead)
			return;

		int mateWho = ResolveTeammate(_pendingTeammate);
		_pendingTeammate = -1;
		if (mateWho < 0)
			return;

		Player mate = Main.player[mateWho];
		float offsetX = mate.direction != 0 ? -mate.direction * 32f : 32f;
		Vector2 pos = mate.position + new Vector2(offsetX, 0f);
		int style = TeleportationStyleID.TeleportationPotion;

		Player.Teleport(pos, style);
		Player.fallStart = (int)(Player.position.Y / 16f);

		if (Main.netMode == NetmodeID.Server)
		{
			RemoteClient.CheckSection(Player.whoAmI, pos);
			NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, pos.X, pos.Y, style);
		}
	}

	public override void UpdateDead()
	{
		if (RespawnGate.MayRespawnThisDeath(Player))
			return;

		Player.respawnTimer = Main.getGoodWorld ? 3600 : Main.expertMode ? 1800 : 1200;
	}

	private int ResolveTeammate(int preferred) =>
		IsLivingTeammate(Player, preferred) ? preferred : FindLivingTeammate(Player);

	private static int FindLivingTeammate(Player self)
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (IsLivingTeammate(self, i))
				return i;
		}

		return -1;
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

		DefinitiveMultiplayerPlayer mp = local.GetModPlayer<DefinitiveMultiplayerPlayer>();
		int value = whoAmI >= 0 && IsLivingTeammate(local, whoAmI) ? whoAmI : -1;
		if (mp.PreferredRespawnWhoAmI == value)
			return;

		mp.PreferredRespawnWhoAmI = value;
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		ModPacket packet = Packets.Begin(Packets.PreferredRespawn);
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

		player.GetModPlayer<DefinitiveMultiplayerPlayer>().PreferredRespawnWhoAmI =
			whoAmI >= 0 && IsLivingTeammate(player, whoAmI) ? whoAmI : -1;
	}
}

using DefinitiveMultiplayer.Common.Players;
using Terraria;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>Single source of truth for whether — and why — a dead player can't respawn yet.</summary>
internal static class RespawnGate
{
	internal enum LockReason { None, OutOfLives, SharedHealthWipe }

	internal static LockReason GetLockReason(Player player)
	{
		if (SharedHealthSystem.IsPlayerHardLocked(player))
			return LockReason.SharedHealthWipe;

		if (BossFightSystem.IsBossFightActive() && BossFightSystem.IsLivesModeActive()
		    && !player.GetModPlayer<DefinitiveMultiplayerPlayer>().RespawnAllowedThisDeath)
			return LockReason.OutOfLives;

		return LockReason.None;
	}

	internal static bool MayRespawnThisDeath(Player player) => GetLockReason(player) == LockReason.None;
}

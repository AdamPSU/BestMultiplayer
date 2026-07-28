using BestMultiplayer.Common;
using BestMultiplayer.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestMultiplayer.Common.Players;

/// <summary>
/// Death-only team spectate: intro, camera, hotkeys, section packets.
/// </summary>
public sealed class SpectatePlayer : ModPlayer
{
	private const int IntroDuration = 180;

	internal static int? Target { get; private set; }
	internal static int IntroTicks { get; private set; }
	internal static bool IsIntro => IntroTicks > 0;
	internal static int IntroSeconds => (IntroTicks + 59) / 60;

	private static bool _holdCorpse;
	private static bool AutoSpectateEnabled => ClientConfig.Instance?.SpectateOnDeath ?? true;

	internal static void Clear()
	{
		SetTarget(null, syncPreferred: false);
		IntroTicks = 0;
		_holdCorpse = false;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		SetTarget(null);
		IntroTicks = IntroDuration;
		_holdCorpse = false;
	}

	public override void UpdateDead()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (Target is int current && !IsValid(current))
			SetTarget(Step(current, +1));

		if (IntroTicks > 0)
		{
			IntroTicks--;
			if (IntroTicks == 0 && AutoSpectateEnabled)
				SetTarget(Step(Main.myPlayer, +1));
		}
		else if (Target is null && !_holdCorpse && AutoSpectateEnabled)
		{
			SetTarget(Step(Main.myPlayer, +1));
		}

		TrySendSection();
	}

	public override void ModifyScreenPosition()
	{
		if (Player.whoAmI != Main.myPlayer || !Player.dead)
			return;

		if (Target is not int t || !IsValid(t))
		{
			if (Target is not null)
				SetTarget(null);
			return;
		}

		Player followed = Main.player[t];
		Main.screenPosition = followed.position - new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (Player.whoAmI != Main.myPlayer || !Player.dead)
			return;

		SpectateKeybinds keys = ModContent.GetInstance<SpectateKeybinds>();
		if (keys.NextPlayer.JustPressed)
			Select(Step(Target ?? Main.myPlayer, +1));
		if (keys.PrevPlayer.JustPressed)
			Select(Step(Target ?? Main.myPlayer, -1));
		if (keys.StopSpectating.JustPressed)
			StopFollowing();
	}

	public override void OnRespawn()
	{
		if (Player.whoAmI == Main.myPlayer)
			Clear();
	}

	public override void OnEnterWorld()
	{
		if (Player.whoAmI == Main.myPlayer)
			Clear();
	}

	internal static void SelectTarget(int whoAmI) => Select(whoAmI);

	internal static void StopFollowing()
	{
		SetTarget(null);
		IntroTicks = 0;
		_holdCorpse = true;
	}

	private static void Select(int? whoAmI)
	{
		IntroTicks = 0;
		_holdCorpse = false;
		SetTarget(whoAmI is int i && IsValid(i) ? i : null);
	}

	private static void SetTarget(int? whoAmI, bool syncPreferred = true)
	{
		Target = whoAmI;
		if (syncPreferred)
			BestMultiplayerPlayer.SetPreferredRespawnTarget(whoAmI ?? -1);
	}

	internal static bool IsValid(int whoAmI) =>
		BestMultiplayerPlayer.IsLivingTeammate(Main.LocalPlayer, whoAmI);

	private static int? Step(int from, int dir)
	{
		for (int n = 1; n <= Main.maxPlayers; n++)
		{
			int i = (from + dir * n + Main.maxPlayers * 4) % Main.maxPlayers;
			if (IsValid(i))
				return i;
		}

		return null;
	}

	private void TrySendSection()
	{
		if (Target is null || Main.netMode != NetmodeID.MultiplayerClient)
			return;
		if (Main.GameUpdateCount % 10 != 0)
			return;

		ModPacket packet = Mod.GetPacket();
		packet.Write(Packets.Section);
		packet.WriteVector2(Main.screenPosition);
		packet.Send();
	}
}

public sealed class SpectateKeybinds : ModSystem
{
	public ModKeybind PrevPlayer { get; private set; } = null!;
	public ModKeybind NextPlayer { get; private set; } = null!;
	public ModKeybind StopSpectating { get; private set; } = null!;

	public override void Load()
	{
		PrevPlayer = KeybindLoader.RegisterKeybind(Mod, "PreviousPlayer", Keys.A);
		NextPlayer = KeybindLoader.RegisterKeybind(Mod, "NextPlayer", Keys.D);
		StopSpectating = KeybindLoader.RegisterKeybind(Mod, "StopSpectating", Keys.None);
	}

	public override void Unload()
	{
		PrevPlayer = null!;
		NextPlayer = null!;
		StopSpectating = null!;
	}
}

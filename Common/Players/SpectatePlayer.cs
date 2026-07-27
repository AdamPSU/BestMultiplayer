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
/// Death-only team spectate: intro countdown, camera follow, hotkeys, section packets.
/// Lifecycle mirrors Team Spectate (Kill / ModifyScreenPosition / OnRespawn); intro ticks in UpdateDead.
/// </summary>
public sealed class SpectatePlayer : ModPlayer
{
	internal const byte PacketSection = 0;
	private const int IntroDuration = 180;

	internal static int? Target { get; private set; }
	internal static int IntroTicks { get; private set; }
	internal static bool IsIntro => IntroTicks > 0;
	internal static int IntroSeconds => (IntroTicks + 59) / 60;

	/// <summary>User pressed Stop; do not auto-reacquire until next death.</summary>
	private static bool _holdCorpse;

	private static bool AutoSpectateEnabled => ClientConfig.Instance?.SpectateOnDeath ?? true;

	internal static void Clear()
	{
		Target = null;
		IntroTicks = 0;
		_holdCorpse = false;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		Target = null;
		IntroTicks = IntroDuration;
		_holdCorpse = false;
	}

	public override void UpdateDead()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (Target is int current && !IsValid(current))
			Target = Step(current, +1);

		if (IntroTicks > 0)
		{
			IntroTicks--;
			if (IntroTicks == 0 && AutoSpectateEnabled)
				Target = Step(Main.myPlayer, +1);
		}
		else if (Target is null && !_holdCorpse && AutoSpectateEnabled)
		{
			// Teammate revived / joined while everyone was dead.
			Target = Step(Main.myPlayer, +1);
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
				Target = null;
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
		Target = null;
		IntroTicks = 0;
		_holdCorpse = true;
	}

	private static void Select(int? whoAmI)
	{
		IntroTicks = 0;
		_holdCorpse = false;
		Target = whoAmI is int i && IsValid(i) ? i : null;
	}

	internal static bool IsValid(int whoAmI)
	{
		if (whoAmI < 0 || whoAmI >= Main.maxPlayers || whoAmI == Main.myPlayer)
			return false;

		Player p = Main.player[whoAmI];
		return p.active && !p.dead && p.team == Main.LocalPlayer.team;
	}

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
		packet.Write(PacketSection);
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
		// Defaults: A/D (left/right). Rebindable in Controls; fine while dead.
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

using System.Collections.Generic;
using DefinitiveMultiplayer.Common;
using DefinitiveMultiplayer.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

internal enum SpectateKind : byte
{
	None = 0,
	Player = 1,
	Boss = 2,
}

/// <summary>
/// Death-only spectate: teammates + bosses, smooth camera, section packets.
/// </summary>
public sealed class SpectatePlayer : ModPlayer
{
	private const int IntroDuration = 180;
	private const int LerpTicksTotal = 18;

	internal static SpectateKind Kind { get; private set; }
	internal static int? Target { get; private set; }
	internal static int IntroTicks { get; private set; }
	internal static bool IsIntro => IntroTicks > 0;
	internal static int IntroSeconds => (IntroTicks + 59) / 60;

	private static bool _holdCorpse;
	private static int _lerpTicks;
	private static Vector2 _lerpFrom;
	private static bool AutoSpectateEnabled => ClientConfig.Instance.SpectateOnDeath;

	internal static void Clear()
	{
		SetTarget(SpectateKind.None, null, syncPreferred: false, startLerp: false);
		IntroTicks = 0;
		_holdCorpse = false;
		_lerpTicks = 0;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		SetTarget(SpectateKind.None, null, startLerp: false);
		IntroTicks = IntroDuration;
		_holdCorpse = false;
		_lerpTicks = 0;
	}

	public override void UpdateDead()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (Kind != SpectateKind.None && !IsCurrentValid())
			ApplyStep(+1);

		if (IntroTicks > 0)
		{
			IntroTicks--;
			if (IntroTicks == 0 && AutoSpectateEnabled)
				ApplyStep(+1);
		}
		else if (Kind == SpectateKind.None && !_holdCorpse && AutoSpectateEnabled)
		{
			ApplyStep(+1);
		}

		TrySendSection();
	}

	public override void ModifyScreenPosition()
	{
		if (Player.whoAmI != Main.myPlayer || !Player.dead)
			return;

		if (!TryGetDesired(out Vector2 desired))
		{
			if (Kind != SpectateKind.None)
				SetTarget(SpectateKind.None, null, startLerp: false);
			_lerpTicks = 0;
			return;
		}

		if (_lerpTicks > 0)
		{
			float t = 1f - _lerpTicks / (float)LerpTicksTotal;
			Main.screenPosition = Vector2.SmoothStep(_lerpFrom, desired, t);
			_lerpTicks--;
			if (_lerpTicks <= 0)
				Main.screenPosition = desired;
		}
		else
		{
			Main.screenPosition = desired;
		}
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (Player.whoAmI != Main.myPlayer || !Player.dead)
			return;

		SpectateKeybinds keys = ModContent.GetInstance<SpectateKeybinds>();
		if (keys.NextPlayer.JustPressed)
			SelectStep(+1);
		if (keys.PrevPlayer.JustPressed)
			SelectStep(-1);
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

	internal static void SelectPlayer(int whoAmI)
	{
		ResetManualSelectState();
		if (IsValidPlayer(whoAmI))
			SetTarget(SpectateKind.Player, whoAmI);
		else
			SetTarget(SpectateKind.None, null, startLerp: false);
	}

	internal static void SelectBoss(int npcIndex)
	{
		ResetManualSelectState();
		if (IsValidBoss(npcIndex))
			SetTarget(SpectateKind.Boss, npcIndex);
		else
			SetTarget(SpectateKind.None, null, startLerp: false);
	}

	internal static void StopFollowing()
	{
		SetTarget(SpectateKind.None, null, startLerp: false);
		IntroTicks = 0;
		_holdCorpse = true;
		_lerpTicks = 0;
	}

	internal static bool IsValidPlayer(int whoAmI) =>
		DefinitiveMultiplayerPlayer.IsLivingTeammate(Main.LocalPlayer, whoAmI);

	internal static bool IsValidBoss(int npcIndex)
	{
		if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
			return false;

		NPC n = Main.npc[npcIndex];
		if (!n.active || n.dontCountMe)
			return false;

		if (n.realLife >= 0 && n.realLife != npcIndex)
			return false;

		if (BossNpc.IsCanonicalSegment(n))
			return true;

		return n.GetBossHeadTextureIndex() >= 0;
	}

	internal static void CollectBossIndices(List<int> into)
	{
		into.Clear();
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			if (IsValidBoss(i))
				into.Add(i);
		}
	}

	private static void SelectStep(int dir)
	{
		ResetManualSelectState();
		ApplyStep(dir);
	}

	private static void ResetManualSelectState()
	{
		IntroTicks = 0;
		_holdCorpse = false;
	}

	private static void ApplyStep(int dir)
	{
		if (TryStep(dir, out SpectateKind k, out int idx))
			SetTarget(k, idx);
		else
			SetTarget(SpectateKind.None, null, startLerp: false);
	}

	/// <summary>
	/// Unified ring: living teammates (whoAmI order), then valid bosses (npc index).
	/// No current target: anchor before first entry so +1 is first teammate (teammate-first).
	/// </summary>
	private static bool TryStep(int dir, out SpectateKind kind, out int index)
	{
		kind = SpectateKind.None;
		index = -1;
		var ring = BuildRing();
		if (ring.Count == 0)
			return false;

		int at = FindRingIndex(ring);
		int next = at < 0
			? (dir > 0 ? 0 : ring.Count - 1)
			: (at + dir + ring.Count) % ring.Count;

		(kind, index) = ring[next];
		return true;
	}

	private static List<(SpectateKind kind, int index)> BuildRing()
	{
		var ring = new List<(SpectateKind, int)>(16);
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			if (IsValidPlayer(i))
				ring.Add((SpectateKind.Player, i));
		}

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			if (IsValidBoss(i))
				ring.Add((SpectateKind.Boss, i));
		}

		return ring;
	}

	private static int FindRingIndex(List<(SpectateKind kind, int index)> ring)
	{
		if (Kind != SpectateKind.None && Target is int t)
		{
			for (int i = 0; i < ring.Count; i++)
			{
				if (ring[i].kind == Kind && ring[i].index == t)
					return i;
			}
		}

		return -1;
	}

	private static void SetTarget(SpectateKind kind, int? index, bool syncPreferred = true, bool startLerp = true)
	{
		bool changed = Kind != kind || Target != index;

		if (kind == SpectateKind.None || index is null)
		{
			Kind = SpectateKind.None;
			Target = null;
		}
		else
		{
			Kind = kind;
			Target = index;
		}

		if (startLerp && changed && Kind != SpectateKind.None)
		{
			_lerpFrom = Main.screenPosition;
			_lerpTicks = LerpTicksTotal;
		}

		if (!syncPreferred)
			return;

		int preferred = Kind == SpectateKind.Player && Target is int p ? p : -1;
		DefinitiveMultiplayerPlayer.SetPreferredRespawnTarget(preferred);
	}

	private static bool IsCurrentValid() => TryGetCenter(out _);

	private static bool TryGetDesired(out Vector2 desired)
	{
		if (!TryGetCenter(out Vector2 center))
		{
			desired = default;
			return false;
		}

		desired = center - HalfScreen();
		return true;
	}

	private static bool TryGetCenter(out Vector2 center)
	{
		if (Kind == SpectateKind.Player && Target is int p && IsValidPlayer(p))
		{
			center = Main.player[p].Center;
			return true;
		}

		if (Kind == SpectateKind.Boss && Target is int b && IsValidBoss(b))
		{
			center = Main.npc[b].Center;
			return true;
		}

		center = default;
		return false;
	}

	private static Vector2 HalfScreen() =>
		new(Main.screenWidth / 2f, Main.screenHeight / 2f);

	private void TrySendSection()
	{
		if (Kind == SpectateKind.None || Main.netMode != NetmodeID.MultiplayerClient)
			return;
		if (Main.GameUpdateCount % 10 != 0)
			return;

		ModPacket packet = Packets.Begin(Packets.Section);
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

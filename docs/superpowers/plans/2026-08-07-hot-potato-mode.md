# Hot Potato Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Hot Potato mode: one living player holds a timed potato; timer expiry kills them and reassigns; touch-pass to another valid player; holder gets configurable move-speed; works always or bosses-only like Shared Health.

**Architecture:** Standalone `HotPotatoSystem` (mirror Marked/Swap patterns). Server/SP owns holder, timer, pass collision, and kill. Clients mirror holder whoAmI + countdown chat via packets. Speed buff and orange FX on ModPlayers. Independent of Marked (both may run).

**Tech Stack:** tModLoader, ModSystem, ModPlayer, ModPacket, KillMe/NetMessage.SendPlayerDeath, hitbox intersection

## Global Constraints

- Pool: **active, non-dead** players only. Optional **same-team only** via config.
- Activation: `HotPotatoEnabled && (!HotPotatoBossesOnly || BossFightSystem.IsBossFightActive())`.
- Timer: **30–300** seconds, step **15**, default **90**. Snap like Marked.
- On timer **0**: holder **dies** (normal death path), then **immediately** pick new living holder (prefer ≠ previous), **restart** timer.
- Mid-timer holder death (any cause): re-pick + **restart** timer (no second kill).
- Pass: holder **hitbox intersects** another valid living player → transfer; **2s** cannot pass back to the player you just received it from (fixed).
- Speed: holder `Player.moveSpeed` mult = `1f + clamp(HotPotatoSpeedBonusPercent, 0, 100) / 100f` (default **25** → **1.25×**).
- Config labels omit the word "Mode". Unique icons (not reusing Marked/Swap icons).
- Potato death uses same kill pattern as SharedHealth wipe: `statLife = 0`, `KillMe`, server `NetMessage.SendPlayerDeath`.
- Lives / Shared HP / respawn rules apply unchanged (potato death is a normal death).

---

### Task 1: ServerConfig + localization + descriptions

**Files:**
- Modify: `Common/Configs/ServerConfig.cs` (Modes section, after Marked block, before Boss Lives)
- Modify: `Localization/*_Mods.DefinitiveMultiplayer.hjson` (all 9)
- Modify: `description.txt`, `description_workshop.txt`, `changelog.txt`

**Fields:**

```csharp
[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
[DefaultValue(false)]
public bool HotPotatoEnabled;

[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
[ConfigGate(nameof(HotPotatoEnabled))]
[CustomModConfigItem(typeof(GatedBooleanElement))]
[DefaultValue(false)]
public bool HotPotatoBossesOnly;

/// <summary>Seconds until potato explodes. 30–300, step 15, default 90.</summary>
[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
[ConfigGate(nameof(HotPotatoEnabled))]
[CustomModConfigItem(typeof(GatedIntElement))]
[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
[Range(30, 300)]
[Increment(15)]
[DrawTicks]
[DefaultValue(90)]
public int HotPotatoIntervalSeconds;

/// <summary>Move-speed bonus % for the holder. 0–100, default 25.</summary>
[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
[ConfigGate(nameof(HotPotatoEnabled))]
[CustomModConfigItem(typeof(GatedIntElement))]
[SliderColor(ConfigUiStyle.SliderR, ConfigUiStyle.SliderG, ConfigUiStyle.SliderB, ConfigUiStyle.SliderA)]
[Range(0, 100)]
[Increment(5)]
[DrawTicks]
[DefaultValue(25)]
public int HotPotatoSpeedBonusPercent;

/// <summary>When true, only same-team living players are in the pool / pass targets.</summary>
[BackgroundColor(ConfigUiStyle.RowR, ConfigUiStyle.RowG, ConfigUiStyle.RowB, ConfigUiStyle.RowA)]
[ConfigGate(nameof(HotPotatoEnabled))]
[CustomModConfigItem(typeof(GatedBooleanElement))]
[DefaultValue(true)]
public bool HotPotatoTeamOnly;
```

**EN labels (unique icons):**
- `HotPotatoEnabled`: `[i:ExplosiveBunny] Hot Potato` — pass the potato or explode
- `HotPotatoBossesOnly`: `[i:SuspiciousLookingEye] Only During Boss Fights` (same icon as SharedHealthBossesOnly is OK for parallel meaning; if icon collision in UI is an issue use `[i:SlimeCrown]`)
- `HotPotatoIntervalSeconds`: `[i:Timer] Potato Fuse (Seconds)`
- `HotPotatoSpeedBonusPercent`: `[i:HermesBoots] Potato Speed Bonus %`
- `HotPotatoTeamOnly`: `[i:TeamBlockRed] Same Team Only`

**UI strings:**
```hjson
UI: {
	HotPotato: {
		Countdown: Potato explodes in {0}
		Assigned: "{0} has the potato!"
		// Optional death reason for KillMe custom text — use if easy:
		// Exploded: "{0} couldn't pass the potato..."
	}
}
```

**Descriptions:**
- `description.txt`: `• Hot Potato (always or bosses; touch-pass; fuse kill; holder speed bonus)`
- Workshop: `[*][b]Hot Potato[/b]: pass by touch or explode when the fuse hits zero; holder is faster; always-on or bosses-only`
- `changelog.txt` bullet matching defaults

- [ ] **Step 1:** Add five config fields after Marked block with BackgroundColor/ConfigGate/sliders matching neighbors
- [ ] **Step 2:** Add EN loc keys under `Configs.ServerConfig` + `UI.HotPotato`; copy structure to other 8 langs (EN text OK for non-EN for now, matching existing pattern)
- [ ] **Step 3:** Update `description.txt`, `description_workshop.txt`, `changelog.txt`
- [ ] **Step 4:** Sanity: gated children listed after parent so ConfigVisibility packs correctly

---

### Task 2: Packets + HandlePacket wiring

**Files:**
- Modify: `Common/Packets.cs`
- Modify: `DefinitiveMultiplayer.cs`

**Interfaces:**
- Produces: `Packets.HotPotatoState = 12`, `Packets.HotPotatoCountdown = 13`
- Consumes (Task 3): `HotPotatoSystem.HandleStatePacket`, `HandleCountdownPacket`

```csharp
/// <summary>Server → clients: hot-potato holder whoAmI (−1 clears).</summary>
internal const byte HotPotatoState = 12;
/// <summary>Server → clients: potato fuse countdown seconds (−1 clears the chat line).</summary>
internal const byte HotPotatoCountdown = 13;
```

```csharp
case Packets.HotPotatoState:
	HotPotatoSystem.HandleStatePacket(reader);
	break;

case Packets.HotPotatoCountdown:
	HotPotatoSystem.HandleCountdownPacket(reader);
	break;
```

- [ ] **Step 1:** Add packet constants 12 and 13
- [ ] **Step 2:** Wire HandlePacket cases (handlers can be stubs until Task 3 compiles — implement Task 3 in same session if needed)

---

### Task 3: HotPotatoSystem — core logic

**Files:**
- Create: `Common/Systems/HotPotatoSystem.cs`

**Interfaces:**
- Produces:
  - `internal static int HolderWhoAmI`
  - `internal static bool IsHolder(Player player)`
  - `internal static bool IsActive()` — config + bosses-only gate
  - `internal static void HandleStatePacket(BinaryReader reader)`
  - `internal static void HandleCountdownPacket(BinaryReader reader)`
- Consumes: `ServerConfig`, `BossFightSystem.IsBossFightActive()`, `Teams.IsReal`, `Packets.HotPotatoState/Countdown`

**Constants:**
```csharp
private const int MinIntervalSeconds = 30;
private const int MaxIntervalSeconds = 300;
private const int IntervalStepSeconds = 15;
private const int TicksPerSecond = 60;
private const int PassBackImmuneTicks = 120; // 2 seconds
private const int ChatLineLife = 600;
private const int CountdownVisibleSeconds = 5;
private const string PotatoColorHex = "FFAA33"; // orange/gold
```

**State:**
```csharp
private static int _holderWhoAmI = -1;
private static int _ticksLeft = -1;
private static bool _wasActive;
private static int _lastIntervalSeconds = -1;
private static int _lastPublishedSeconds = int.MinValue;
private static int _lastSentWhoAmI = int.MinValue;
private static int _passBackBlockedWhoAmI = -1; // cannot pass TO this who while immune
private static int _passBackImmuneTicksLeft;
private static bool _killingHolder; // re-entrancy guard during KillMe
```

**IsActive / IsHolder:**
```csharp
internal static bool IsActive()
{
	ServerConfig cfg = ServerConfig.Instance;
	if (!cfg.HotPotatoEnabled)
		return false;
	if (cfg.HotPotatoBossesOnly && !BossFightSystem.IsBossFightActive())
		return false;
	return true;
}

internal static bool IsHolder(Player player) =>
	player is not null
	&& IsActive()
	&& player.active
	&& !player.dead
	&& player.whoAmI == _holderWhoAmI;
```

**PostUpdateWorld (server/SP only):**
1. If `!IsActive()` → `Deactivate()` if was active; return.
2. Snap interval; if newly active or interval changed → set `_wasActive`, reset timer, `PickHolder(excludeWho: -1)`, publish seconds.
3. If holder invalid (left/dead) and `!_killingHolder` → `PickHolder(excludeWho: previous)`, **restart timer** (do not kill again).
4. `TryPassByTouch()` — if holder hitbox intersects another valid pool player who is not `_passBackBlockedWhoAmI` while immune → `PassTo(target)`.
5. Tick down `_passBackImmuneTicksLeft`; clear block when 0.
6. `_ticksLeft--`. If still > 0 → publish seconds; return.
7. Timer hit 0 with valid holder → `ExplodeHolder()` then `PickHolder(excludeWho: explodedWho)` + full timer restart. If no holder → just restart pick.

**ExplodeHolder:**
```csharp
private static void ExplodeHolder()
{
	int who = _holderWhoAmI;
	if (!IsValidLiving(who))
		return;

	Player p = Main.player[who];
	_killingHolder = true;
	try
	{
		// Prefer custom death reason if NetworkText/Language key is easy; else ByOther.
		var reason = PlayerDeathReason.ByCustomReason(
			Terraria.Localization.NetworkText.FromKey(
				"Mods.DefinitiveMultiplayer.UI.HotPotato.Exploded",
				p.name));
		// Fallback if key missing: PlayerDeathReason.LegacyEmpty() or ByOther(who)
		p.statLife = 0;
		p.KillMe(reason, 9999.0, 0);
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendPlayerDeath(p.whoAmI, reason, 9999, 0, pvp: false);
	}
	finally
	{
		_killingHolder = false;
	}
}
```

If custom reason is awkward, use:
```csharp
PlayerDeathReason reason = PlayerDeathReason.ByOther(0);
// or Language.GetText for plain string overload if available in this TML version
```

**PassTo / SetHolder:**
```csharp
private static void PassTo(int newWho)
{
	int prev = _holderWhoAmI;
	SetHolder(newWho);
	// Receiver cannot immediately pass back to prev
	_passBackBlockedWhoAmI = prev;
	_passBackImmuneTicksLeft = PassBackImmuneTicks;
	// Do NOT restart fuse on pass — only on explode / death re-pick / activation
}

private static void SetHolder(int whoAmI)
{
	if (_holderWhoAmI == whoAmI && _lastSentWhoAmI == whoAmI)
		return;
	int prev = _holderWhoAmI;
	_holderWhoAmI = whoAmI;
	SendState(whoAmI, force: true);
	if (!Main.dedServ && whoAmI >= 0 && whoAmI != prev)
		AnnounceHolder(whoAmI);
}
```

**TryPassByTouch:**
```csharp
private static void TryPassByTouch()
{
	if (!IsValidLiving(_holderWhoAmI))
		return;

	Player holder = Main.player[_holderWhoAmI];
	Rectangle hit = holder.Hitbox;

	for (int i = 0; i < Main.maxPlayers; i++)
	{
		if (i == _holderWhoAmI)
			continue;
		if (_passBackImmuneTicksLeft > 0 && i == _passBackBlockedWhoAmI)
			continue;
		if (!IsInPool(Main.player[i]))
			continue;
		if (!hit.Intersects(Main.player[i].Hitbox))
			continue;

		PassTo(i);
		return; // one pass per tick max
	}
}
```

**Pool:**
```csharp
private static bool IsInPool(Player p)
{
	if (p?.active != true || p.dead)
		return false;
	if (!ServerConfig.Instance.HotPotatoTeamOnly)
		return true;
	// Team-only: need real team match with current holder if holder valid; else any real-team living
	if (!IsValidLiving(_holderWhoAmI))
		return Teams.IsReal(p.team);
	return Teams.IsReal(p.team) && p.team == Main.player[_holderWhoAmI].team;
}

private static void CollectPool(List<int> into)
{
	into.Clear();
	for (int i = 0; i < Main.maxPlayers; i++)
	{
		if (IsInPool(Main.player[i]))
			into.Add(i);
	}
}
```

When team-only and holder invalid, `IsInPool` allows any real-team player — `PickHolder` should pick randomly among pool. If multiple teams exist under team-only with no holder, prefer largest team or first non-empty team of living players:

```csharp
private static void PickHolder(int excludeWho)
{
	CollectPool(Living);
	// If team-only and no holder, restrict to one team: pick team of first living real-team player, filter
	if (ServerConfig.Instance.HotPotatoTeamOnly && Living.Count > 0)
	{
		int team = Main.player[Living[0]].team;
		Living.RemoveAll(i => Main.player[i].team != team);
	}

	if (Living.Count == 0)
	{
		SetHolder(-1);
		return;
	}

	if (excludeWho >= 0)
	{
		int idx = Living.IndexOf(excludeWho);
		if (idx >= 0 && Living.Count > 1)
			Living.RemoveAt(idx);
	}

	SetHolder(Living[Main.rand.Next(Living.Count)]);
}
```

**Chat / packets:** Copy MarkedSystem patterns (`PublishSeconds`, `ApplyCountdownSeconds`, upsert chat line, `AnnounceHolder` with `UI.HotPotato.Assigned`, color `FFAA33`). Packets use `HotPotatoState` / `HotPotatoCountdown`.

**SnapIntervalSeconds:** identical math to Marked with 30/300/15 constants.

**Deactivate / ResetAll / OnWorldLoad / OnWorldUnload:** clear holder, timer, pass immune, countdown chat.

- [ ] **Step 1:** Create `HotPotatoSystem.cs` with full logic above
- [ ] **Step 2:** Confirm compile (fix/build or IDE); fix missing usings (`Microsoft.Xna.Framework` for Rectangle, `Terraria.DataStructures` for PlayerDeathReason)
- [ ] **Step 3:** Manual logic check: pass does not reset fuse; explode kills then re-picks; death mid-fuse re-picks without double kill (`_killingHolder`)

---

### Task 4: Speed buff + orange draw FX

**Files:**
- Create: `Common/Players/HotPotatoPlayer.cs` (speed + draw in one ModPlayer is fine; split if preferred)

**Speed:**
```csharp
public override void PostUpdateRunSpeeds()
{
	if (!HotPotatoSystem.IsHolder(Player))
		return;

	int bonus = Utils.Clamp(ServerConfig.Instance.HotPotatoSpeedBonusPercent, 0, 100);
	if (bonus <= 0)
		return;

	float mult = 1f + bonus / 100f;
	Player.moveSpeed *= mult;
	// Optional consistency with vanilla boot stacking:
	// Player.maxRunSpeed *= mult;
	// Player.accRunSpeed *= mult;
}
```

Apply `maxRunSpeed` and `accRunSpeed` as well so the bonus is actually felt (moveSpeed alone is often weak). Use the same mult on all three.

**Draw (orange, not red):**
- `FrameEffects`: `armorEffectDrawOutlines = true`, `armorEffectDrawShadow = true` when holder
- `DrawEffects`: tint toward orange `(1f, 0.55f, 0.15f)`, `Lighting.AddLight` orange pulse, dust `DustID.Torch` or `DustID.OrangeTorch` with orange Color
- `ModifyDrawInfo`: lerp armor colors toward `new Color(255, 170, 50)`

Mirror structure of `MarkedDrawPlayer.cs` with different colors/dust.

- [ ] **Step 1:** Implement HotPotatoPlayer speed + draw
- [ ] **Step 2:** Verify `IsHolder` false when mode inactive or dead → no buff/FX

---

### Task 5: In-game verification checklist

No automated tests in this repo for gameplay systems. Manual / host checklist:

- [ ] **Step 1:** Enable Hot Potato, Bosses Only **off**, 2+ players same team, Team Only **on**
  - One player announced; orange FX; speed up
  - Touch pass transfers; cannot bounce back for ~2s; fuse **does not** reset on pass
  - At 5s: orange countdown chat; at 0: holder dies; new holder; fuse restarts
- [ ] **Step 2:** Bosses Only **on** — inactive outside boss; activates on boss spawn; deactivates when fight ends (holder cleared)
- [ ] **Step 3:** Team Only **off** — pass works across teams
- [ ] **Step 4:** Solo living player — holds potato, explodes, no holder until someone else living
- [ ] **Step 5:** Marked + Hot Potato both on — independent holders/FX (red vs orange)
- [ ] **Step 6:** Config UI — children collapse under Hot Potato with no layout gap
- [ ] **Step 7:** Speed slider 0% = no boost; 100% = clearly faster
- [ ] **Step 8:** Potato death consumes boss life / shared HP like a normal death

---

### Task 6: Commit (only if user asks)

Do **not** commit unless explicitly requested.

Suggested message when asked:
```
feat: Hot Potato mode (touch-pass, fuse kill, speed bonus)

Always-on or bosses-only; 30–300s fuse; team or free-for-all pool;
holder +25% speed default; orange FX and chat countdown.
```

---

## Self-review

| Spec requirement | Task |
|---|---|
| Enable + Bosses Only like Shared Health | Task 1, 3 `IsActive` |
| Timer max 5 min (300s), 30–300 step 15 default 90 | Task 1, 3 snap |
| Holder +speed default 25%, slider 0–100 | Task 1, 4 |
| Touch pass | Task 3 `TryPassByTouch` |
| 2s no pass-back | Task 3 immune |
| Timer 0 → die → reassign + restart | Task 3 explode path |
| Mid-death reassign + restart | Task 3 invalid holder |
| Team vs everyone config | Task 1, 3 pool |
| Orange FX + chat | Task 3, 4 |
| Packets / MP sync | Task 2, 3 |
| Docs/changelog | Task 1 |
| Independent of Marked | separate system |

No TBD placeholders. Types consistent: `HotPotatoIntervalSeconds`, `HotPotatoSpeedBonusPercent`, `HotPotatoTeamOnly`, packets 12/13.

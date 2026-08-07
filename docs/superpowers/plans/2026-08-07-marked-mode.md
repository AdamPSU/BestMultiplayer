# Marked Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a boss-only Marked mode that rotates the mark among living players on a timer, stacking deal/take multipliers on Boss Balance, with red glow/outline and swap-style chat countdown.

**Architecture:** Server/SP owns mark assignment and rotation timer. Clients receive marked whoAmI + countdown seconds via packets. Damage applies in `BossFightDamagePlayer` as `bossBalance × markedMult` for the marked living player only during boss fights. Presentation is a client draw layer (red glow + outline). Chat reuses the in-place `ChatMessageContainer` pattern from `PlayerSwapSystem`.

**Tech Stack:** tModLoader, ModSystem, ModPlayer, ModPacket, PlayerDrawLayer / ModifyDrawInfo

## Global Constraints

- Mark pool: **active, non-dead players only** (`p.active && !p.dead`). Never mark spectators/corpses.
- Effects **only during boss fights** (`BossFightSystem.IsBossFightActive()`).
- Damage formula (marked player only):  
  `finalTaken = clamp(BossFightDamageMultiplier) × clamp(MarkedDamageTakenMult)`  
  `finalDealt = clamp(BossFightDamageDealtMultiplier) × clamp(MarkedDamageDealtMult)`  
  Unmarked players: Boss Balance only (unchanged).
- Defaults: taken mult **1.5**, dealt mult **1.25**, rotate every **1** minute.
- Config labels do **not** include the word "Mode" (section header is already Modes).
- Icons must not reuse existing config icons (see en-US labels).

---

### Task 1: ServerConfig + localization

**Files:**
- Modify: `Common/Configs/ServerConfig.cs` (Modes section, after Player Swap block)
- Modify: `Localization/*_Mods.DefinitiveMultiplayer.hjson` (all 9)
- Modify: `description.txt`, `description_workshop.txt` (one-line feature)

**Fields (Modes, gated children):**

```csharp
[DefaultValue(false)]
public bool MarkedEnabled;

[ConfigGate(nameof(MarkedEnabled))]
[CustomModConfigItem(typeof(GatedIntElement))]
[Range(1, 30)]
[DefaultValue(1)]
public int MarkedIntervalMinutes;

// Extra mult on top of Boss Balance for the marked player (boss fights only).
[ConfigGate(nameof(MarkedEnabled))]
[CustomModConfigItem(typeof(GatedFloatElement))]
[Range(1f, 3f)]
[Increment(0.05f)]
[DefaultValue(1.5f)]
public float MarkedDamageTakenMult;

[ConfigGate(nameof(MarkedEnabled))]
[CustomModConfigItem(typeof(GatedFloatElement))]
[Range(1f, 3f)]
[Increment(0.05f)]
[DefaultValue(1.25f)]
public float MarkedDamageDealtMult;

internal float ClampedMarkedTakenMult() => Utils.Clamp(MarkedDamageTakenMult, 1f, 3f);
internal float ClampedMarkedDealtMult() => Utils.Clamp(MarkedDamageDealtMult, 1f, 3f);
```

**Labels (EN, unique icons — suggestions):**
- Marked: `[i:BattlePotion] Marked` — rotate a living player during bosses
- Interval: `[i:CopperWatch] Mark Every (Minutes)`
- Taken: `[i:RedPotion] Marked Damage Taken`
- Dealt: `[i:PsychoKnife] Marked Damage Dealt`

Tooltips: plain one-liners; taken/dealt tooltips must say they stack on Boss Balance.

**Workshop bullet:** under Modes list  
`[*][b]Marked[/b]: during bosses, one living player is marked on a timer; they deal/take more (stacks on Boss Balance)`

- [ ] **Step 1:** Add config fields + clamp helpers
- [ ] **Step 2:** Add loc keys all 9 langs + descriptions
- [ ] **Step 3:** Verify gated rows collapse with no gap (existing ConfigVisibility fix)

---

### Task 2: MarkedSystem — assignment, rotation, sync

**Files:**
- Create: `Common/Systems/MarkedSystem.cs`
- Modify: `Common/Packets.cs` — add `MarkedState = 10`, `MarkedCountdown = 11`
- Modify: `DefinitiveMultiplayer.cs` — handle packets

**State (server/SP authoritative):**
- `int MarkedWhoAmI` (−1 = none)
- `int _ticksLeft` rotation timer
- Client mirror of `MarkedWhoAmI` for draw/damage prediction on clients (damage hooks run on both sides as appropriate)

**Lifecycle (server/SP only in `PostUpdateWorld` or hook beside boss system):**

1. If `!MarkedEnabled` or `!IsBossFightActive()`:
   - Clear mark (`-1`), clear timer, publish clear countdown + clear mark packet if was active
2. On boss fight start / mode enable while boss active:
   - Pick random living player → set mark
   - Start timer = `MarkedIntervalMinutes * 60 * 60` ticks
3. Each tick while active:
   - If current mark invalid (left, dead, inactive) → immediately re-pick from living pool (if empty, clear mark)
   - Decrement timer; at 0 → pick **different** living player when possible (Sattolo-style: if pool size ≥ 2, exclude current; if 1, keep them); reset timer
4. Broadcast:
   - `MarkedState`: `int whoAmI` (−1 clear) whenever mark changes
   - `MarkedCountdown`: `int seconds` (−1 clear), same cadence/publish rules as PlayerSwap (only when second changes)

**Pool helper:**

```csharp
static void CollectLiving(List<int> into)
{
  into.Clear();
  for (int i = 0; i < Main.maxPlayers; i++)
  {
    Player p = Main.player[i];
    if (p?.active == true && !p.dead)
      into.Add(i);
  }
}
```

**API:**
- `internal static bool IsMarked(Player p)` → enabled + boss + `p.whoAmI == MarkedWhoAmI` + living
- `internal static void HandleStatePacket` / `HandleCountdownPacket`

**Chat (client/listen host, mirror PlayerSwap):**
- Last **5 seconds** only
- In-place line: `Mark rotates in {0}` with gold `[c/FFD700:…]` time
- Loc: `UI.Marked.Countdown`
- Clear when inactive / not in last 5s

- [ ] **Step 1:** Packets + HandlePacket cases
- [ ] **Step 2:** Implement MarkedSystem timer/pick/sync
- [ ] **Step 3:** Countdown chat (reuse reflection helpers or small shared chat util if clean; otherwise copy minimal pattern from PlayerSwapSystem — prefer extract only if both stay DRY without large refactor)

---

### Task 3: Damage stacking

**Files:**
- Modify: `Common/Players/BossFightDamagePlayer.cs`

**Behavior:**
- Always apply Boss Balance mult when boss active (even at 1.0 if marked needs base — actually base 1.0 can skip; marked mult still applies alone).
- Clean approach:

```csharp
public override void ModifyHurt(ref Player.HurtModifiers modifiers)
{
  if (modifiers.PvP) return;
  if (!BossFightSystem.IsBossFightActive()) return;

  float mult = ServerConfig.Instance.ClampedDamageTakenMult(); // 0.5–3
  if (MarkedSystem.IsMarked(Player))
    mult *= ServerConfig.Instance.ClampedMarkedTakenMult(); // 1–3

  if (mult != 1f)
    modifiers.FinalDamage *= mult;
}

// ModifyHitNPC: same with dealt mults
```

- Remove early-out that skips entire boss scan when raw balance is 1.0 **if** marked can still apply — use `IsBossFightActive` once then compose mults.
- Marked mult only when `IsMarked(Player)` (implies living).

- [ ] **Step 1:** Rewrite TryBossMult path into composed mults
- [ ] **Step 2:** Sanity: unmarked + balance 1.0 → no change; marked + defaults → taken 1.5, dealt 1.25; balance 2.0 × marked 1.5 taken → 3.0

---

### Task 4: Presentation — red glow + outline

**Files:**
- Create: `Common/Players/MarkedDrawPlayer.cs` (or `Common/DrawLayers/MarkedPlayerLayer.cs`)

**Approach (client / all drawing sides):**
1. `ModifyDrawInfo`: if `MarkedSystem.IsMarked(Player)`, boost `drawPlayer` glow (e.g. set `glowColor` / use reddish `colorArmor*` tint lightly) — prefer minimal tint so gear stays readable.
2. Outline: `PlayerDrawLayer` before head/body that, when marked, draws the player silhouette 4–8 times at 1px offsets in **red** (`Color.Red * alpha`) with slightly larger scale or use vanilla-style border via multiple `DrawData` copies of body — keep cheap:
   - Simple v1: pulsing red dust ring (`DustID.Something` red) + `ModifyDrawInfo` `colorEyeWhite` / overall `headGlowMask` if available
   - Preferred: custom `PlayerDrawLayer` parented before `PlayerDrawLayers.Backpacks` that injects outline DrawData

**Concrete preferred implementation:**
- `ModPlayer.DrawEffects`: if marked, `drawInfo.dustColor` / spawn few red dusts at player center each frame (subtle pulse with `Main.GameUpdateCount`)
- `PlayerDrawLayer` (`Multiple` sample positions): for offsets `(±1,0),(0,±1),(±1,±1)` draw player texture layers in flat red with alpha ~0.6 — if full re-draw is too heavy, use `Terraria.Graphics.Shaders` armor shader only if already in project (avoid new dependency)

**Pragmatic v1 (ship this):**
1. `DrawEffects`: red afterimage-style dust + light `Lighting.AddLight(player.Center, 0.6f, 0.1f, 0.1f)`
2. `ModifyDrawInfo`: set `drawPlayer.eyeGlow` / multiply body color slightly toward red
3. If outline via full redraw is too large, red light + dust + nameplate tint is acceptable — but **user asked for outline + glow**, so implement outline via offset DrawData on a single simple layer using `PlayerDrawLayers` sample from tML docs

**Visibility:** all clients see the marked player’s effect (mark whoAmI synced).

- [ ] **Step 1:** DrawEffects glow + light
- [ ] **Step 2:** Outline layer
- [ ] **Step 3:** Verify dead/unmarked players never show effect

---

### Task 5: Wire boss-end cleanup + descriptions polish

**Files:**
- `MarkedSystem`: clear on world unload, boss end, mode off
- `changelog.txt` bullet under current version section if still 0.1.5 WIP

- [ ] **Step 1:** Ensure no stale mark after boss dies
- [ ] **Step 2:** Changelog + final loc pass

---

## Testing (manual in-game)

1. Enable Marked, start boss with 2+ living players → exactly one red-outlined glowing player.
2. Wait last 5s of minute → chat `Mark rotates in N` updates in place; at 0 mark moves to another living player when possible.
3. Kill marked player → mark jumps to another living player immediately (not stuck on corpse).
4. Boss Balance take 2.0 + Marked take 1.5 → marked takes 3.0×; unmarked takes 2.0×.
5. Boss Balance deal 1.0 + Marked deal 1.25 → marked deals 1.25×.
6. Mode off / boss dead → no outline, no chat, no mult.
7. Solo living player → stays marked; mults still apply.
8. Config UI: Marked off → gated rows collapsed with no gap.

## Out of scope

- Team-scoped mark pools (all living players in world)
- Mark transfer on hit (timer + death reassign only)
- UI nameplate text beyond glow/outline
- Extracting shared chat helper unless duplication is painful (optional cleanup)


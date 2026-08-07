# Boss Fight Defense Multiplier Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Host-configurable fraction of player defense that still applies during boss fights (0.1–1.0, default 1.0 = vanilla).

**Architecture:** `ServerConfig.BossFightDefenseMultiplier` (ServerSide float). On each non-PvP hurt while `BossFightSystem.IsBossFightActive()`, a small `ModPlayer` adds `ScalingArmorPenetration += (1f - multiplier)` so e.g. `0.80` keeps 80% defense. No custom packets — tML already runs `ModifyHurt` on the hurt client and config is host-synced.

**Tech Stack:** tModLoader 1.4.4.9 / C# / `ModPlayer.ModifyHurt` / `Player.HurtModifiers.ScalingArmorPenetration` / `ServerConfig`

## Global Constraints

- Slider = **defense retained**, not penetration. `1.0` = full defense; `0.1` = 10% defense.
- Range **0.1–1.0**, increment **0.05**, default **1.0**.
- Active only while `BossFightSystem.IsBossFightActive()` (any boss segment / EoW).
- All non-PvP damage; skip when `modifiers.PvP`.
- All players (not team-gated). Independent of shared HP / lives.
- No separate master toggle (min 0.1 during bosses).
- Prefer Build+Reload; no automated tests — `dotnet build` + in-game smoke.
- Do not commit unless user asks.
- Full loc in all 9 language files (match existing pattern).

## Design lock

| Choice | Value |
|---|---|
| Field | `BossFightDefenseMultiplier` |
| UI label | Boss Fight Defense |
| Math | `ScalingArmorPenetration += 1f - Utils.Clamp(value, 0.1f, 1f)` |
| Gate | Boss fight active + !PvP |
| Default | `1.0f` (no change until host lowers it) |

## File map

| File | Role |
|---|---|
| `Common/Configs/ServerConfig.cs` | Float slider under Boss Fights |
| `Common/Players/BossFightDefensePlayer.cs` | **New.** `ModifyHurt` apply scaling AP |
| `Localization/*_Mods.DefinitiveMultiplayer.hjson` (9) | Label/tooltip |
| `description.txt`, `description_workshop.txt`, `changelog.txt`, `build.txt` | Feature note; version bump to 0.1.3 |
| `wiki/concepts/boss-fight-defense.md` | **New** concept page |
| `wiki/concepts/multiplayer-config-rules.md`, `mod-anatomy.md`, `overview.md`, `log.md` | Index + rules |

---

### Task 1: Config + localization

**Files:**
- Modify: `Common/Configs/ServerConfig.cs`
- Modify: all 9 `Localization/*_Mods.DefinitiveMultiplayer.hjson`

- [x] Add `BossFightDefenseMultiplier` field
- [x] Localize all 9 languages
- [x] Build

### Task 2: Apply defense fraction on hurt

**Files:**
- Create: `Common/Players/BossFightDefensePlayer.cs`

- [x] Create `BossFightDefensePlayer` with `ModifyHurt`
- [x] Build

### Task 3: Packaging + wiki

- [x] version 0.1.3, changelog, descriptions
- [x] wiki concept + index updates
- [x] Build

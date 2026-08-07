# Shared Team Health Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Optional one-organism shared HP per team (1–5): server-owned pool projected onto the local vanilla life bar; hearts/potions heal the pool without revert; potions also team-sick; pool empty = DeathLink (+ boss hard-lock).

**Architecture:** Server mutates pool only. Clients send idempotent damage/heal events; server acks by event id + monotonic `poolSeq` meta. Display = `clamp(serverCurrent − unackedDmg + unackedHeal, 0, Max)`. Paint local player only on clients; server paints living members for sim. No `PlayerLifeMana` projection. Regen zeroed while linked.

**Tech Stack:** tModLoader 1.4.4.9 / C# / ModSystem + ModPlayer / ServerConfig / ModPacket

## Design lock

| Choice | Value |
|---|---|
| Multiplier | 0.5–2, default 0.75 |
| Max | Frozen at arm |
| Heals | Event-only; hearts no team sick; potions team-sick iff applied > 0 |
| Prediction | Event-id unacked dmg/heal; never baseline Δcurrent |
| Death | Server wipe authority; RespawnGate.SharedHealthWipe before lives |

## Status

Implemented 2026-08-06 in-tree (DLL compiles; package may hit TML003 if tML holds `.tmod`).

### Smoke checklist

- [ ] Config off — no behavior change
- [ ] SP armed — bar = mult × max; damage drops; die at 0
- [ ] Potion heal sticks; sick on drinker
- [ ] Heart heal sticks; no team sick
- [ ] 2P listen — both bars match; hit A drops B
- [ ] Lag: potion then delayed meta — no snap down
- [ ] Pool 0 boss — wipe string, hard-lock, lives not spent
- [ ] Boss end — respawn unlocks
- [ ] Feed shows shared cur/max

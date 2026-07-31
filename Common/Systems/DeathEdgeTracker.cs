using System;
using Terraria;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>Detects per-player dead-since-last-tick transitions for a single ModSystem's own bookkeeping.</summary>
internal sealed class DeathEdgeTracker
{
	private readonly bool[] _wasDead = new bool[Main.maxPlayers];

	/// <summary>Invokes onNewDeath for players newly dead since the last call, then updates state for all players.</summary>
	public void ForEachNewDeath(Action<Player> onNewDeath)
	{
		for (int i = 0; i < Main.maxPlayers; i++)
		{
			Player p = Main.player[i];
			bool dead = p.active && p.dead;
			if (dead && !_wasDead[i])
				onNewDeath(p);
			_wasDead[i] = dead;
		}
	}

	/// <summary>Updates state for all players without triggering edge callbacks.</summary>
	public void Snapshot()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
			_wasDead[i] = Main.player[i] is { active: true, dead: true };
	}

	/// <summary>Seeds a single slot, e.g. for a player who just (re)joined mid-fight.</summary>
	public void Seed(int index, bool dead) => _wasDead[index] = dead;

	public void Reset() => Array.Clear(_wasDead);
}

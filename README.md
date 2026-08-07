# The Definitive Multiplayer Mod

![rate and favorite](https://i.imgur.com/W2c5CX6.gif)

Multiplayer QoL for Terraria on tModLoader. Built to replace the legacy [Better Multiplayer](https://steamcommunity.com/sharedfiles/filedetails/?id=2634682993) mod. EVERYTHING is configurable.

## Teams

![Teams config](https://i.imgur.com/vGxGOqX.png)

Auto team join puts people on a host-chosen team upon joining. Free teammate teleport lets you map-click to a teammate without burning Wormhole Potions. You can block that during boss fights if you think it's cheaty.

## Modes

![Modes config](https://i.imgur.com/QlZDj4b.jpeg)

Shared team health gives the party one HP bar. It is the only shared-health option on the workshop that actually works. :)

Player swap shuffles living teammates on a timer (default every 5 minutes). Do with that as you will.

Marked picks a living player during boss fights and rotates on an interval. The marked player deals and takes extra damage on top of whatever you set in Boss Balance, with a red glow so everyone can see who has it.

Hot Potato hands someone a fuse. Pass it by touching another player, or it kills the holder when time runs out. The holder gets a speed bonus. Run it always, or only during bosses.

Marked and Hot Potato cannot both be on (they share the in-place chat line).

## Boss Lives

![Boss Lives config](https://i.imgur.com/IjNmHMe.png)

Player life limit caps how many times each person can die in a fight. Team life limit caps total deaths for the whole team. Once you are out of lives, you stay dead until the boss ends instead of cycling back in.

## Boss Balance

![Boss Balance config](https://i.imgur.com/x4xkwXV.png)

Sliders for damage you take and damage you deal, from 0.5x to 3x (1.0 is normal). Boss max health scales the same way when a boss spawns.

## Spectate and Respawn

![Spectate and Respawn config](https://i.imgur.com/mxQzLjg.png)

When you die, spectate teammates and bosses instead of staring at dirt. The death screen has separate copy for a normal death, running out of lives, and a full team wipe. The boss stats feed shows party heads, HP, damage dealt and taken, and deaths while you wait.

Respawn wait is a host-set number of seconds, not vanilla difficulty timers. Timer add-ons can scale that per extra player, during bosses or events, and escalate with each death in the fight. Health and mana come back as a percent of max. During bosses you can drop in beside the teammate you were watching, and when the boss ends death timers clear so nobody sits out the loot screen.

## Config

Server Config can be edited by any player, which matters on cloud and dedicated hosts where only one person has the real host seat. Mode options nest under each On toggle. Client Config covers spectate and the stats feed.

## Languages

English, German, Italian, French, Spanish, Russian, Simplified Chinese, Brazilian Portuguese, Polish.

## Future Plans

- More modes
- Disable friendly projectiles
- Better stats UI

## Architecture

One mod (`side = Both`). Server policy vs client presentation.

```text
DefinitiveMultiplayer.cs     thin Mod entry (packets)
Common/Configs/              ServerConfig + ClientConfig + expandable UI
Common/Players/              team, lives, shared HP, marked/potato FX, spectate
Common/Systems/              modes, boss pools, fight stats, death UI, wormhole
Common/UI/                   spectate grid, fight stats feed
Localization/                en-US + 8 other languages
```

| Concern | Config | Runtime |
|---|---|---|
| Teams, modes, lives, balance, respawn | `ServerConfig` | Players / Systems |
| Spectate prefs, stats feed | `ClientConfig` | Spectate / death UI |
| Packets | — | team, modes, lives, spectate targets |

## Build

- In-game: `Workshop → Develop Mods → Build + Reload`
- VS Code: `tModLoader: build mod` task
- Repo under `ModSources`; keep mod folder / `build.txt` / type names aligned

Local `.dotnet` is used by the VS Code task (gitignored).

## Links

Bug reports, ideas, and PRs welcome:

- GitHub: https://github.com/0xABAN/DefinitiveMultiplayer
- Steam Workshop: publish from tModLoader using `description_workshop.txt`

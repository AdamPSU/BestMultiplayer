using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace BestMultiplayer.Common.UI;

/// <summary>
/// Dead-only teammate head grid (Team Spectate–inspired, no custom assets).
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class SpectateUISystem : ModSystem
{
	private UserInterface _ui = null!;
	private SpectateGridState _state = null!;
	private GameTime _lastTime = new();

	public override void Load()
	{
		if (Main.dedServ)
			return;

		_state = new SpectateGridState();
		_state.Activate();
		_ui = new UserInterface();
		_ui.SetState(_state);
	}

	public override void Unload()
	{
		_ui = null!;
		_state = null!;
	}

	public override void UpdateUI(GameTime gameTime)
	{
		_lastTime = gameTime;
		if (!ShouldShow())
			return;

		_ui.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (idx == -1)
			return;

		layers.Insert(idx, new LegacyGameInterfaceLayer(
			"BestMultiplayer: SpectateGrid",
			delegate
			{
				if (ShouldShow())
					_ui.Draw(Main.spriteBatch, _lastTime);
				return true;
			},
			InterfaceScaleType.UI));
	}

	private static bool ShouldShow() =>
		!Main.dedServ
		&& Main.netMode != NetmodeID.SinglePlayer
		&& Main.LocalPlayer is { active: true, dead: true };
}

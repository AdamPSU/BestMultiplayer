using System.Collections.Generic;
using BestMultiplayer.Common.Configs;
using BestMultiplayer.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BestMultiplayer.Common.Systems;

/// <summary>Client host for the boss-fight stats feed (layout M).</summary>
[Autoload(Side = ModSide.Client)]
public sealed class FightStatsUISystem : ModSystem
{
	private UserInterface _ui;
	private FightStatsFeedState _state;
	private GameTime _lastTime = new();
	private LegacyGameInterfaceLayer _feedLayer;

	public override void Load()
	{
		if (Main.dedServ)
			return;

		_state = new FightStatsFeedState();
		_state.Activate();
		_ui = new UserInterface();
		_ui.SetState(_state);
		_feedLayer = new LegacyGameInterfaceLayer(
			"BestMultiplayer: FightStatsFeed",
			delegate
			{
				if (ShouldShow())
					_ui?.Draw(Main.spriteBatch, _lastTime);
				return true;
			},
			InterfaceScaleType.UI);
	}

	public override void Unload()
	{
		_ui = null;
		_state = null;
		_feedLayer = null;
	}

	public override void UpdateUI(GameTime gameTime)
	{
		_lastTime = gameTime;
		if (ShouldShow())
			_ui?.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseIdx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (mouseIdx == -1)
			return;

		layers.Insert(mouseIdx, _feedLayer);
	}

	private static bool ShouldShow() =>
		!Main.dedServ
		&& !Main.gameMenu
		&& !Main.playerInventory
		&& ClientConfig.Instance.ShowBossFightStats
		&& FightStatsSystem.ShouldShowFeed
		&& Main.LocalPlayer is { active: true };
}

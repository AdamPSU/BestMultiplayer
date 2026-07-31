using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace BestMultiplayer.Common.Configs;

/// <summary>
/// PerPlayer: editable 0–99 budget (+/−). PerTeam: read-only "team size at fight start". Off: hidden.
/// </summary>
public sealed class BossFightRespawnsElement : ConfigElement<int>
{
	private UIText _minus;
	private UIText _plus;
	private BossFightLivesMode _drawnMode = (BossFightLivesMode)(-1);
	private bool? _lastShown;

	public override void OnBind()
	{
		base.OnBind();
		TextDisplayFunction = LabelText;

		_minus = new UIText("−", 0.9f);
		_minus.Top.Set(6f, 0f);
		_minus.Left.Set(-50f, 1f);
		_minus.OnLeftClick += (_, _) =>
		{
			if (CurrentMode() != BossFightLivesMode.PerPlayer || IgnoresMouseInteraction)
				return;
			Value = Utils.Clamp(Value - 1, 0, 99);
		};
		Append(_minus);

		_plus = new UIText("+", 0.9f);
		_plus.Top.Set(6f, 0f);
		_plus.Left.Set(-28f, 1f);
		_plus.OnLeftClick += (_, _) =>
		{
			if (CurrentMode() != BossFightLivesMode.PerPlayer || IgnoresMouseInteraction)
				return;
			Value = Utils.Clamp(Value + 1, 0, 99);
		};
		Append(_plus);

		ApplyMode(CurrentMode(), force: true);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		BossFightLivesMode mode = CurrentMode();
		if (mode != _drawnMode)
			ApplyMode(mode, force: false);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (IgnoresMouseInteraction || GetDimensions().Height < 1f)
			return;
		base.DrawSelf(spriteBatch);
	}

	private BossFightLivesMode CurrentMode() =>
		Item is ServerConfig cfg ? cfg.BossFightLivesMode : BossFightLivesMode.Off;

	private void ApplyMode(BossFightLivesMode mode, bool force)
	{
		_drawnMode = mode;
		bool edit = mode == BossFightLivesMode.PerPlayer;
		bool show = mode is BossFightLivesMode.PerPlayer or BossFightLivesMode.PerTeam;

		if (force)
			_lastShown = null;

		ConfigVisibility.Apply(this, show, ref _lastShown);

		_minus.IgnoresMouseInteraction = !edit || !show;
		_plus.IgnoresMouseInteraction = !edit || !show;
		_minus.TextColor = edit && show ? Color.White : Color.Transparent;
		_plus.TextColor = edit && show ? Color.White : Color.Transparent;
	}

	private string LabelText()
	{
		string label = Label ?? MemberInfo.Name;
		return CurrentMode() switch
		{
			BossFightLivesMode.PerTeam => $"{label}: team size at fight start",
			BossFightLivesMode.PerPlayer => $"{label}: {Value}",
			_ => label,
		};
	}
}

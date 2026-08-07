using Terraria.Localization;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Player life limit slider. Max = Vanilla (off); lower = fewer lives.
/// </summary>
public sealed class BossFightPlayerLivesElement : GatedIntElement
{
	public override void OnBind()
	{
		base.OnBind();
		TextDisplayFunction = () =>
		{
			string label = Label ?? MemberInfo.Name;
			int n = CurrentValue;
			if (n >= ServerConfig.PlayerLivesVanilla)
				return $"{label}: {Language.GetTextValue("Mods.DefinitiveMultiplayer.Configs.ServerConfig.BossFightLives.Vanilla")}";
			return $"{label}: {n}";
		};
	}
}

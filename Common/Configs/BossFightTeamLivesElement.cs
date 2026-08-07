using Terraria.Localization;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Team life limit slider. Max = Vanilla (off); 0 = team size; 1–20 = fixed pool.
/// </summary>
public sealed class BossFightTeamLivesElement : GatedIntElement
{
	public override void OnBind()
	{
		base.OnBind();
		TextDisplayFunction = () =>
		{
			string label = Label ?? MemberInfo.Name;
			int n = CurrentValue;
			if (n >= ServerConfig.TeamLivesVanilla)
				return $"{label}: {Language.GetTextValue("Mods.DefinitiveMultiplayer.Configs.ServerConfig.BossFightTeamLives.Vanilla")}";
			if (n == 0)
				return $"{label}: {Language.GetTextValue("Mods.DefinitiveMultiplayer.Configs.ServerConfig.BossFightTeamLives.TeamSize")}";
			return $"{label}: {n}";
		};
	}
}

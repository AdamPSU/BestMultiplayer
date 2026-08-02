using Terraria.Localization;

namespace DefinitiveMultiplayer.Common.Configs;

/// <summary>
/// Per-team shared life pool slider. 0 = team size at fight start; 1–20 = fixed pool.
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
			if (n == 0)
				return $"{label}: {Language.GetTextValue("Mods.DefinitiveMultiplayer.Configs.ServerConfig.BossFightTeamLives.TeamSize")}";
			return $"{label}: {n}";
		};
	}
}

namespace DefinitiveMultiplayer.Common;

/// <summary>Single source of truth for the real (non-zero) team ID range, shared by BossFightSystem and SharedHealthSystem.</summary>
internal static class Teams
{
	internal const int Min = 1;
	internal const int Max = 5;

	internal static bool IsReal(int team) => team is >= Min and <= Max;
}

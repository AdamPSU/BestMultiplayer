namespace BestMultiplayer.Common;

internal static class Packets
{
	internal const byte Section = 0;
	internal const byte PreferredRespawn = 1;
	/// <summary>Server → clients: team shared HP pool snapshot.</summary>
	internal const byte SharedHealthPool = 2;
}

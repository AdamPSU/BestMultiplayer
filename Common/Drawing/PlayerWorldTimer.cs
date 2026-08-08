using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Drawing;

/// <summary>Persistent fuse/countdown text above a player (world space).</summary>
internal static class PlayerWorldTimer
{
	private const float OffsetY = -28f;
	private const float Scale = 1.1f;

	private static int _cacheSeconds = int.MinValue;
	private static string _cacheText = "";

	internal static string FormatTime(int totalSeconds)
	{
		if (totalSeconds < 60)
			return totalSeconds.ToString();

		int m = totalSeconds / 60;
		int s = totalSeconds % 60;
		return $"{m}:{s:D2}";
	}

	internal static bool TryResolve(Player player, out string text, out Color color)
	{
		text = null;
		color = default;
		if (player is null || !player.active)
			return false;

		if (HotPotatoSystem.IsHolder(player) && HotPotatoSystem.DisplaySeconds >= 0)
		{
			text = FormatTimeCached(HotPotatoSystem.DisplaySeconds);
			color = HotPotatoSystem.Accent;
			return true;
		}

		if (MarkedSystem.IsMarked(player))
		{
			int s = MarkedSystem.DisplaySeconds;
			if (s >= 0 && s <= MarkedSystem.CountdownVisibleSeconds)
			{
				text = FormatTimeCached(s);
				color = MarkedSystem.Accent;
				return true;
			}
		}

		return false;
	}

	internal static void DrawAbove(PlayerDrawSet drawInfo, string text, Color color)
	{
		if (Main.dedServ || string.IsNullOrEmpty(text) || drawInfo.shadow != 0f)
			return;

		Player player = drawInfo.drawPlayer;
		if (player is null || !player.active)
			return;

		Vector2 world = new(player.Center.X, player.Top.Y + OffsetY);
		Vector2 screen = world - Main.screenPosition;
		screen = new Vector2((int)screen.X, (int)screen.Y);

		var font = FontAssets.MouseText.Value;
		Vector2 size = ChatManager.GetStringSize(font, text, new Vector2(Scale));
		ChatManager.DrawColorCodedStringWithShadow(
			Main.spriteBatch,
			font,
			text,
			screen - new Vector2(size.X * 0.5f, size.Y * 0.5f),
			color,
			0f,
			Vector2.Zero,
			new Vector2(Scale));
	}

	private static string FormatTimeCached(int seconds)
	{
		if (seconds == _cacheSeconds)
			return _cacheText;

		_cacheSeconds = seconds;
		_cacheText = FormatTime(seconds);
		return _cacheText;
	}
}

/// <summary>Draws Hot Potato fuse / Marked last-5s countdown over the target player.</summary>
public sealed class StatusTimerDrawLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
		drawInfo.shadow == 0f && PlayerWorldTimer.TryResolve(drawInfo.drawPlayer, out _, out _);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		if (!PlayerWorldTimer.TryResolve(drawInfo.drawPlayer, out string text, out Color color))
			return;

		PlayerWorldTimer.DrawAbove(drawInfo, text, color);
	}
}

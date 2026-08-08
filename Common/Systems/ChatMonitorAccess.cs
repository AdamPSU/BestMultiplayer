using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.UI.Chat;

namespace DefinitiveMultiplayer.Common.Systems;

/// <summary>Shared reflection access to the live chat message list.</summary>
internal static class ChatMonitorAccess
{
	private static FieldInfo _messagesField;

	internal static bool TryGetMessages(out List<ChatMessageContainer> messages)
	{
		messages = null;
		if (Main.chatMonitor is not RemadeChatMonitor monitor)
			return false;

		_messagesField ??= typeof(RemadeChatMonitor).GetField(
			"_messages",
			BindingFlags.Instance | BindingFlags.NonPublic);
		if (_messagesField?.GetValue(monitor) is not List<ChatMessageContainer> list)
			return false;

		messages = list;
		return true;
	}
}

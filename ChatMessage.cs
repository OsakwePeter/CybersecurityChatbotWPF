// ============================================================
//  ChatMessage.cs
//  Responsibility: Data model for a single chat message.
//                 Used by MessageBubbleFactory to build
//                 correctly styled WPF UI elements.
// ============================================================

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Represents a single message in the conversation.
    /// IsUser = true → displayed as a right-aligned user bubble (blue).
    /// IsUser = false → displayed as a left-aligned bot bubble (dark green).
    /// </summary>
    public class ChatMessage
    {
        public string Text   { get; set; } = "";
        public bool   IsUser { get; set; }
        public string Time   { get; set; } = "";

        public ChatMessage(string text, bool isUser)
        {
            Text   = text;
            IsUser = isUser;
            Time   = System.DateTime.Now.ToString("HH:mm");
        }
    }
}

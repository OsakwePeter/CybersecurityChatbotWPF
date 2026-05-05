// ============================================================
//  MessageBubbleFactory.cs
//  Responsibility: Creates styled WPF Border+TextBlock "bubbles"
//                 for each chat message, using the app's colour
//                 palette defined in App.xaml resources.
//  Pattern      : Factory — separates UI construction from
//                 business logic (Single Responsibility Principle).
// ============================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Factory that builds WPF UIElements representing chat message bubbles.
    /// Keeps all message styling in one place so it is easy to change.
    /// </summary>
    public static class MessageBubbleFactory
    {
        // ── Colour constants (match App.xaml) ────────────────────────────────
        private static readonly SolidColorBrush UserBubbleBrush =
            new SolidColorBrush(Color.FromRgb(0x1F, 0x3A, 0x5F));  // Dark blue
        private static readonly SolidColorBrush BotBubbleBrush  =
            new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1A));  // Dark green
        private static readonly SolidColorBrush UserBorderBrush =
            new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8));  // Cyan
        private static readonly SolidColorBrush BotBorderBrush  =
            new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));  // Green
        private static readonly SolidColorBrush TextBrush =
            new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));  // Light
        private static readonly SolidColorBrush TimeBrush =
            new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));  // Grey

        // ── System / info message colours ─────────────────────────────────────
        private static readonly SolidColorBrush SystemBrush =
            new SolidColorBrush(Color.FromRgb(0x1C, 0x21, 0x28));
        private static readonly SolidColorBrush SystemBorderBrush =
            new SolidColorBrush(Color.FromRgb(0xF0, 0xC0, 0x00));  // Yellow

        /// <summary>
        /// Creates and returns a WPF UIElement (a Border containing a TextBlock)
        /// styled appropriately for the given ChatMessage.
        /// </summary>
        public static UIElement Create(ChatMessage msg)
        {
            // ── Outer container — aligns the bubble left (bot) or right (user) ──
            DockPanel dock = new DockPanel
            {
                Margin = new Thickness(0, 4, 0, 4)
            };

            // ── Avatar / label ────────────────────────────────────────────────
            TextBlock avatar = new TextBlock
            {
                Text              = msg.IsUser ? "👤" : "🤖",
                FontSize          = 18,
                VerticalAlignment = VerticalAlignment.Top,
                Margin            = msg.IsUser
                                    ? new Thickness(8, 4, 0, 0)
                                    : new Thickness(0, 4, 8, 0)
            };

            // ── Message bubble ────────────────────────────────────────────────
            TextBlock messageText = new TextBlock
            {
                Text         = msg.Text,
                Foreground   = TextBrush,
                FontSize     = 13.5,
                TextWrapping = TextWrapping.Wrap,
                LineHeight   = 20,
            };

            TextBlock timeText = new TextBlock
            {
                Text              = msg.Time,
                Foreground        = TimeBrush,
                FontSize          = 10,
                HorizontalAlignment = msg.IsUser
                                    ? HorizontalAlignment.Right
                                    : HorizontalAlignment.Left,
                Margin            = new Thickness(0, 4, 0, 0)
            };

            StackPanel bubbleContent = new StackPanel();
            bubbleContent.Children.Add(messageText);
            bubbleContent.Children.Add(timeText);

            Border bubble = new Border
            {
                Background      = msg.IsUser ? UserBubbleBrush : BotBubbleBrush,
                BorderBrush     = msg.IsUser ? UserBorderBrush : BotBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius    = msg.IsUser
                                  ? new CornerRadius(12, 2, 12, 12)
                                  : new CornerRadius(2, 12, 12, 12),
                Padding         = new Thickness(14, 10, 14, 10),
                MaxWidth        = 520,
                Child           = bubbleContent
            };

            // ── Name label above bubble ───────────────────────────────────────
            TextBlock nameLabel = new TextBlock
            {
                Text       = msg.IsUser ? "You" : "🤖 CyberBot",
                Foreground = msg.IsUser ? UserBorderBrush : BotBorderBrush,
                FontSize   = 11,
                FontWeight = FontWeights.SemiBold,
                Margin     = new Thickness(0, 0, 0, 3)
            };

            StackPanel column = new StackPanel
            {
                HorizontalAlignment = msg.IsUser
                                      ? HorizontalAlignment.Right
                                      : HorizontalAlignment.Left
            };
            column.Children.Add(nameLabel);
            column.Children.Add(bubble);

            // ── Assemble dock panel ───────────────────────────────────────────
            if (msg.IsUser)
            {
                DockPanel.SetDock(avatar, Dock.Right);
                dock.Children.Add(avatar);
                dock.Children.Add(column);
            }
            else
            {
                DockPanel.SetDock(avatar, Dock.Left);
                dock.Children.Add(avatar);
                dock.Children.Add(column);
            }

            return dock;
        }

        /// <summary>
        /// Creates a centred, yellow-bordered system/info message bubble.
        /// Used for: welcome messages, session notes, tips, etc.
        /// </summary>
        public static UIElement CreateSystem(string text)
        {
            Border bubble = new Border
            {
                Background      = SystemBrush,
                BorderBrush     = SystemBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(8),
                Padding         = new Thickness(16, 10, 16, 10),
                Margin          = new Thickness(40, 6, 40, 6),
                Child           = new TextBlock
                {
                    Text              = text,
                    Foreground        = new SolidColorBrush(Color.FromRgb(0xF0, 0xC0, 0x00)),
                    FontSize          = 12,
                    TextWrapping      = TextWrapping.Wrap,
                    TextAlignment     = TextAlignment.Center,
                }
            };
            return bubble;
        }
    }
}

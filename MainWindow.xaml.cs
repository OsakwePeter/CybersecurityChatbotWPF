// ============================================================
//  MainWindow.xaml.cs  (Part 2 — WPF Edition)
//  Module  : PROG6221/w  Programming 2A
//  Purpose : WPF GUI for the Cybersecurity Awareness Chatbot.
//            Implements all Part 2 requirements:
//              1. GUI design (WPF + XAML)
//              2. Voice greeting (preserved from Part 1)
//              3. ASCII art in GUI
//              4. Keyword recognition (via ResponseEngine)
//              5. Random responses (via ResponseEngine)
//              6. Conversation flow / follow-ups
//              7. Memory & recall (name, interest, last topic)
//              8. Sentiment detection (delegate event)
//              9. Error handling & fallback responses
//             10. Code optimisation (classes, dictionaries, delegates)
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Main window code-behind.
    /// Orchestrates the conversation session, wires up the ResponseEngine,
    /// and manages all UI state updates.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ── Fields ────────────────────────────────────────────────────────────
        private readonly ResponseEngine _engine = new ResponseEngine();
        private          string         _userName       = "";
        private          bool           _nameCollected  = false;

        // Memory storage (Part 2 requirement)
        private readonly List<string>   _conversationTopics = new List<string>();

        /// <summary>Tracks total messages sent this session — displayed in status bar.</summary>
        private int _messageCount = 0;

        // ── Constructor ───────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();

            // Wire up the sentiment-changed delegate event
            // When the engine detects a new sentiment it calls this handler
            _engine.OnSentimentChanged += HandleSentimentChanged;

            // Display ASCII art in the sidebar panel
            AsciiArtBlock.Text = BuildAsciiArt();

            // Play voice greeting asynchronously (non-blocking)
            VoiceGreeting.PlayAsync();

            // Show welcome flow in the chat
            ShowWelcomeFlow();

            // Focus the input box immediately
            InputBox.Focus();
        }

        // ── Startup / Welcome ─────────────────────────────────────────────────

        /// <summary>
        /// Displays the initial welcome messages and asks for the user's name.
        /// Uses async/await with delays to create a natural conversational feel.
        /// </summary>
        private async void ShowWelcomeFlow()
        {
            // System greeting
            AddSystemMessage("🛡️  Welcome to the Cybersecurity Awareness Bot  🛡️");
            AddSystemMessage("Empowering South African citizens to stay safe online.");

            await Task.Delay(600);

            // Bot intro
            AddBotMessage(
                "Hello! 👋 I'm your Cybersecurity Awareness Assistant.\n\n" +
                "I'm here to help you navigate the digital world safely. " +
                "I can answer questions about phishing, passwords, malware, " +
                "safe browsing, 2FA, and much more.\n\n" +
                "To get started, please tell me your name!");

            // Prompt for name via system message
            AddSystemMessage("Please type your name below to begin.");
        }

        // ── Message Adding Helpers ────────────────────────────────────────────

        /// <summary>Adds a user bubble to the chat panel and scrolls to bottom.</summary>
        private void AddUserMessage(string text)
        {
            var msg = new ChatMessage(text, isUser: true);
            ChatPanel.Children.Add(MessageBubbleFactory.Create(msg));
            ScrollToBottom();
        }

        /// <summary>
        /// Adds a bot bubble to the chat panel with a simulated typing delay.
        /// async/await keeps the UI responsive while the "typing" pause runs.
        /// </summary>
        private async Task AddBotMessageAsync(string text)
        {
            // Show typing indicator
            var typingIndicator = MessageBubbleFactory.CreateSystem("🤖 CyberBot is typing…");
            ChatPanel.Children.Add(typingIndicator);
            ScrollToBottom();

            // Delay proportional to message length (natural feel, capped at 1.5s)
            int delay = Math.Min(300 + text.Length * 8, 1500);
            await Task.Delay(delay);

            // Remove typing indicator, add real bubble
            ChatPanel.Children.Remove(typingIndicator);
            AddBotMessage(text);
        }

        /// <summary>Adds a bot message synchronously (for startup flow).</summary>
        private void AddBotMessage(string text)
        {
            var msg = new ChatMessage(text, isUser: false);
            ChatPanel.Children.Add(MessageBubbleFactory.Create(msg));
            ScrollToBottom();
        }

        /// <summary>Adds a centred yellow system/info message bubble.</summary>
        private void AddSystemMessage(string text)
        {
            ChatPanel.Children.Add(MessageBubbleFactory.CreateSystem(text));
            ScrollToBottom();
        }

        /// <summary>Scrolls the chat area to the bottom so the latest message is visible.</summary>
        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        // ── Input Handling ────────────────────────────────────────────────────

        /// <summary>
        /// Fires when the user presses a key in the input box.
        /// Enter (without Shift) sends the message.
        /// Shift+Enter inserts a newline (multi-line input).
        /// </summary>
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true; // Prevent the 'ding' sound
                ProcessInput();
            }
        }

        /// <summary>Fires when the Send button is clicked.</summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
            => ProcessInput();

        /// <summary>
        /// Fires when input text changes.
        /// Hides the placeholder hint as soon as the user types anything,
        /// and shows it again when the box is cleared.
        /// </summary>
        private void InputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (InputPlaceholder != null)
                InputPlaceholder.Visibility = string.IsNullOrEmpty(InputBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        /// <summary>
        /// Core input processing pipeline:
        ///   1. Validate the input
        ///   2. If name not yet collected → collect name, personalise session
        ///   3. Otherwise → route to ResponseEngine and display result
        /// </summary>
        private async void ProcessInput()
        {
            string rawInput = InputBox.Text.Trim();
            InputBox.Clear();
            if (InputPlaceholder != null) InputPlaceholder.Visibility = Visibility.Visible;

            // Rule 1: empty input
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                AddSystemMessage("⚠️  Please type a message before pressing Send.");
                return;
            }

            // Rule 2: too long
            if (rawInput.Length > 500)
            {
                AddSystemMessage("⚠️  Message too long. Please keep it under 500 characters.");
                return;
            }

            // Always show what the user typed
            AddUserMessage(rawInput);

            // ── Phase 1: Collect name ─────────────────────────────────────────
            if (!_nameCollected)
            {
                _userName = rawInput.Trim();
                _nameCollected = true;
                _engine.SetUserName(_userName);

                // Update sidebar info panel
                UpdateSidebarInfo();

                // Personalised greeting
                await AddBotMessageAsync(
                    $"Great to meet you, {_userName}! 🤗\n\n" +
                    $"I'll remember your name throughout our conversation. " +
                    $"Feel free to ask me about any cybersecurity topic — " +
                    $"phishing, passwords, malware, 2FA, or anything else.\n\n" +
                    $"Type 'menu' to see all available topics, or 'help' for example questions.");

                AddSystemMessage($"✅ Session started for {_userName}. Happy learning! 🛡️");
                TopBarTitle.Text = $"🛡️ CyberBot — Session: {_userName}";
                return;
            }

            // ── Phase 2: Normal conversation ──────────────────────────────────
            string? response = _engine.GetResponse(rawInput);

            if (response == null)
            {
                // Fallback — no keyword matched
                response = BuildFallbackResponse(rawInput);
            }

            // Track topics for memory recall (Part 2 memory requirement)
            _conversationTopics.Add(rawInput);
            _messageCount++;
            UpdateStatusBar();

            await AddBotMessageAsync(response);

            // Contextual memory callout — fires when topic matches stored interest
            MaybeAddMemoryCallout(rawInput);
        }

        /// <summary>
        /// Builds an informative fallback response for unrecognised input.
        /// Includes the specific unrecognised text and guidance on valid topics.
        /// </summary>
        private string BuildFallbackResponse(string input)
        {
            return $"I didn't quite understand \"{input}\" 🤔\n\n" +
                   $"Could you rephrase? Here are some things you can try:\n" +
                   $"  • Type 'menu' to see all available topics\n" +
                   $"  • Type 'help' for example questions\n" +
                   $"  • Click a topic button in the left sidebar\n" +
                   $"  • Try 'phishing', 'password', 'malware', or '2FA'";
        }

        /// <summary>
        /// Surfaces a personalised memory reminder when the user's current message
        /// is related to their stored interest — contextual rather than arbitrary.
        /// Part 2 requirement: memory and recall feature.
        /// </summary>
        private void MaybeAddMemoryCallout(string currentInput)
        {
            string interest = _engine.GetUserInterest();
            if (string.IsNullOrEmpty(interest)) return;

            // Only fire when the current input actually relates to the stored interest
            if (currentInput.ToLower().Contains(interest.ToLower()))
            {
                AddSystemMessage(
                    $"💭 Memory recall: As someone interested in {interest}, " +
                    $"remember to regularly review your settings and stay updated on {interest}-related threats.");
                UpdateSidebarInfo();
            }
        }

        /// <summary>Updates the bottom status bar with current session stats.</summary>
        private void UpdateStatusBar()
        {
            StatusBarLeft.Text  = $"🛡️ Session: {(_nameCollected ? _userName : "Not started")}";
            StatusBarRight.Text = $"Messages: {_messageCount}";
        }

        // ── Delegate / Event Handlers ─────────────────────────────────────────

        /// <summary>
        /// Called by ResponseEngine via the SentimentChangedHandler delegate
        /// whenever a new sentiment is detected.
        /// Updates the sidebar mood indicator.
        /// </summary>
        private void HandleSentimentChanged(string sentiment, string emoji)
        {
            // Must update UI on the UI thread — Dispatcher.Invoke ensures thread safety
            Dispatcher.Invoke(() =>
            {
                MoodBlock.Text = $"{emoji} Mood detected: {sentiment}";
                UpdateSidebarInfo();
            });
        }

        // ── Sidebar / UI Updates ──────────────────────────────────────────────

        /// <summary>Refreshes all sidebar info blocks with current session data.</summary>
        private void UpdateSidebarInfo()
        {
            UserInfoBlock.Text = string.IsNullOrEmpty(_userName)
                ? "Not yet identified"
                : $"👤 {_userName}";

            string interest = _engine.GetUserInterest();
            InterestBlock.Text = string.IsNullOrEmpty(interest)
                ? ""
                : $"📌 Interest: {interest}";
        }

        // ── Quick Topic Buttons ───────────────────────────────────────────────

        /// <summary>
        /// Handles clicks on the quick-topic sidebar buttons.
        /// Uses the button's Tag property as the input to route through the engine.
        /// </summary>
        private async void QuickTopic_Click(object sender, RoutedEventArgs e)
        {
            if (!_nameCollected)
            {
                AddSystemMessage("⚠️  Please enter your name first!");
                return;
            }

            if (sender is System.Windows.Controls.Button btn && btn.Tag is string topic)
            {
                AddUserMessage($"Tell me about {topic}");

                string? response = _engine.GetResponse(topic);
                await AddBotMessageAsync(response ?? BuildFallbackResponse(topic));
            }
        }

        // ── Clear Chat ────────────────────────────────────────────────────────

        /// <summary>Clears all messages from the chat panel and resets session state.</summary>
        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            _nameCollected = false;
            _userName = "";
            _conversationTopics.Clear();
            _messageCount = 0;
            TopBarTitle.Text = "🛡️ Cybersecurity Awareness Bot";
            UserInfoBlock.Text = "";
            MoodBlock.Text = "";
            InterestBlock.Text = "";
            StatusBarLeft.Text  = "🛡️ CyberBot ready";
            StatusBarRight.Text = "Messages: 0";
            ShowWelcomeFlow();
        }

        // ── ASCII Art (Part 2: translate Part 1 ASCII art into GUI) ──────────

        /// <summary>
        /// Returns the cybersecurity ASCII art adapted for the WPF sidebar panel.
        /// Part 2 requirement: translate Part 1 ASCII art into the GUI effectively.
        /// Uses a smaller, sidebar-friendly shield design.
        /// </summary>
        private static string BuildAsciiArt()
        {
            return
                "  ██████╗██╗   ██╗██████╗ \n" +
                " ██╔════╝╚██╗ ██╔╝██╔══██╗\n" +
                " ██║      ╚████╔╝ ██████╔╝\n" +
                " ██║       ╚██╔╝  ██╔══██╗\n" +
                " ╚██████╗   ██║   ██████╔╝\n" +
                "  ╚═════╝   ╚═╝   ╚═════╝ \n" +
                "                          \n" +
                "  ╔══════════════════╗    \n" +
                "  ║  🛡️  SECURITY   ║    \n" +
                "  ║    AWARENESS    ║    \n" +
                "  ║      BOT 🇿🇦    ║    \n" +
                "  ╚══════════════════╝    ";
        }
    }
}

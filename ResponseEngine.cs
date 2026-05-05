// ============================================================
//  ResponseEngine.cs  (Part 2 — WPF Edition)
//  Responsibility: Complete knowledge base and NLP layer.
//                 Handles keyword recognition, random response
//                 selection, conversation memory, sentiment
//                 detection, and follow-up/conversation flow.
//  New in Part 2 : Random responses, memory, sentiment,
//                  follow-up detection, delegate callbacks.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CybersecurityChatbotWPF
{
    // ── Delegate type for sentiment-change notifications ───────────────────────
    /// <summary>
    /// Fired whenever the engine detects a change in user sentiment.
    /// The subscriber (MainWindow) uses this to update the UI mood indicator.
    /// </summary>
    public delegate void SentimentChangedHandler(string sentiment, string emoji);

    /// <summary>
    /// The core intelligence of the chatbot.
    /// Manages keyword → response mapping, random tip selection,
    /// conversation memory, sentiment detection, and follow-up handling.
    /// </summary>
    /// <remarks>
    /// Processing pipeline order in <see cref="GetResponse"/>:
    ///   1. Sentiment detection  — detects emotional tone, prepends empathetic prefix
    ///   2. Follow-up detection  — if user says "tell me more", continues last topic
    ///   3. Interest memory      — stores any expressed topic interest for later recall
    ///   4. Random-response pool — topics with multiple tips (phishing tip, etc.)
    ///   5. Main dictionary      — single detailed response per keyword
    ///   6. Returns null         — caller (MainWindow) shows the fallback message
    /// </remarks>
    public class ResponseEngine
    {
        // ── Events / Delegates ────────────────────────────────────────────────
        /// <summary>Raised when detected sentiment changes.</summary>
        public event SentimentChangedHandler? OnSentimentChanged;

        // ── Memory (Part 2 requirement) ───────────────────────────────────────
        /// <summary>The user's name, set via <see cref="SetUserName"/>.</summary>
        private string _userName       = "there";
        /// <summary>The cybersecurity topic the user expressed interest in.</summary>
        private string _userInterest   = "";
        /// <summary>The keyword key of the last matched topic — used for follow-up responses.</summary>
        private string _lastTopic      = "";
        /// <summary>The most recently detected sentiment label.</summary>
        private string _currentSentiment = "neutral";

        // ── Knowledge base structures ─────────────────────────────────────────

        /// <summary>
        /// Primary keyword → single response dictionary.
        /// Used for most topics where one detailed response is sufficient.
        /// </summary>
        private readonly Dictionary<string, string> _responses;

        /// <summary>
        /// Keyword → list of responses (random selection).
        /// Part 2 requirement: for topics like "phishing tip" the bot picks
        /// a different tip each time to keep interactions varied.
        /// </summary>
        private readonly Dictionary<string, List<string>> _randomResponses;

        /// <summary>
        /// Sentiment keyword → (sentiment label, emoji, empathetic prefix).
        /// Detects emotional tone and adjusts the bot's reply accordingly.
        /// </summary>
        private readonly Dictionary<string, (string label, string emoji, string prefix)> _sentimentMap;

        /// <summary>
        /// Phrases the user can type to get the bot to continue the previous topic.
        /// Part 2 requirement: conversation flow / follow-up handling.
        /// </summary>
        private static readonly string[] FollowUpPhrases = new[]
        {
            "tell me more", "more", "explain more", "give me another tip",
            "another tip", "continue", "go on", "elaborate", "and",
            "keep going", "more info", "more information", "what else"
        };

        private readonly Random _rng = new Random();

        // ── Constructor ───────────────────────────────────────────────────────
        public ResponseEngine()
        {
            _responses       = BuildResponses();
            _randomResponses = BuildRandomResponses();
            _sentimentMap    = BuildSentimentMap();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Store the user's name for response personalisation.</summary>
        public void SetUserName(string name) => _userName = name;

        /// <summary>Return whatever topic the user has expressed interest in (for UI display).</summary>
        public string GetUserInterest() => _userInterest;

        /// <summary>Return the currently detected sentiment label.</summary>
        public string GetCurrentSentiment() => _currentSentiment;

        /// <summary>
        /// Main entry point: process raw user input and return a bot response.
        /// Processing order:
        ///   1. Detect and handle sentiment
        ///   2. Check for follow-up / continuation phrases
        ///   3. Check random-response topics
        ///   4. Check main keyword dictionary
        ///   5. Return null (caller shows fallback)
        /// </summary>
        public string? GetResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            string lower = input.ToLower().Trim();

            // ── Step 1: Sentiment detection ───────────────────────────────────
            string? sentimentPrefix = DetectSentiment(lower);

            // ── Step 2: Follow-up / conversation flow ─────────────────────────
            if (IsFollowUp(lower) && !string.IsNullOrEmpty(_lastTopic))
            {
                string? followUp = GetFollowUpResponse(_lastTopic);
                if (followUp != null)
                    return Personalise((sentimentPrefix ?? "") + followUp);
            }

            // ── Step 3: Memory — store expressed interest ──────────────────────
            CheckAndRememberInterest(lower);

            // ── Step 4: Random-response topics ────────────────────────────────
            foreach (var kvp in _randomResponses)
            {
                if (ContainsKeyword(lower, kvp.Key))
                {
                    _lastTopic = kvp.Key;
                    string pick = kvp.Value[_rng.Next(kvp.Value.Count)];
                    return Personalise((sentimentPrefix ?? "") + pick);
                }
            }

            // ── Step 5: Main dictionary ────────────────────────────────────────
            foreach (var kvp in _responses)
            {
                if (ContainsKeyword(lower, kvp.Key))
                {
                    _lastTopic = kvp.Key;
                    return Personalise((sentimentPrefix ?? "") + kvp.Value);
                }
            }

            return null; // Caller shows default fallback
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Replace {name} and {interest} placeholders with real values.</summary>
        private string Personalise(string text)
            => text.Replace("{name}", _userName)
                   .Replace("{interest}", string.IsNullOrEmpty(_userInterest)
                                          ? "cybersecurity" : _userInterest);

        /// <summary>
        /// Word-boundary keyword match — the keyword must begin at a non-letter/digit
        /// character (or at the very start of the string).
        ///
        /// Why this matters:
        ///   lower.Contains("hi")  matches "hi" inside "p[hi]shing" → wrong response fires.
        ///   ContainsKeyword checks that the character before the match is NOT a letter,
        ///   so "phishing" will NOT trigger the "hi" greeting keyword.
        ///
        /// Stem keys (e.g. "phish", "brows", "authenticat") are intentional — we only
        /// check the START boundary, not the end, so stems still match their full words.
        /// </summary>
        private static bool ContainsKeyword(string haystack, string needle)
        {
            int idx = haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                // Accept match only when it starts at a word boundary
                bool startOk = idx == 0 || !char.IsLetterOrDigit(haystack[idx - 1]);
                if (startOk) return true;

                idx = haystack.IndexOf(needle, idx + 1, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Checks if the input contains a sentiment keyword.
        /// When detected: updates internal state, fires the delegate event,
        /// and returns an empathetic prefix to prepend to the main response.
        /// Returns null if no sentiment is detected (no prefix needed).
        /// </summary>
        private string? DetectSentiment(string lower)
        {
            foreach (var kvp in _sentimentMap)
            {
                if (ContainsKeyword(lower, kvp.Key))
                {
                    var (label, emoji, prefix) = kvp.Value;

                    if (label != _currentSentiment)
                    {
                        _currentSentiment = label;
                        // Fire the delegate so the UI can update the mood display
                        OnSentimentChanged?.Invoke(label, emoji);
                    }

                    return prefix + "\n\n";
                }
            }
            return null;
        }

        /// <summary>Returns true if the user's message is a follow-up continuation phrase.</summary>
        private static bool IsFollowUp(string lower)
            => FollowUpPhrases.Any(phrase => lower == phrase || lower.StartsWith(phrase));

        /// <summary>
        /// Gets a follow-up response for the last discussed topic.
        /// Pulls from the random-response pool (different tip each time)
        /// or returns a contextual "did you know" fact for main-dictionary topics.
        /// </summary>
        private string? GetFollowUpResponse(string topic)
        {
            // Try random pool first — more variety for follow-ups
            if (_randomResponses.ContainsKey(topic))
            {
                var list = _randomResponses[topic];
                return "Here's another tip on that topic:\n\n"
                     + list[_rng.Next(list.Count)];
            }

            // For main-dictionary topics, build a "did you know" variation
            // rather than repeating the same response
            if (_responses.ContainsKey(topic))
            {
                return $"Here's something else worth knowing about {topic}:\n\n"
                     + $"Did you know that most {topic}-related incidents can be prevented with "
                     + $"basic awareness and good digital habits? Review the key points and "
                     + $"consider sharing them with friends or family to help protect them too.\n\n"
                     + $"Type 'menu' to explore more cybersecurity topics.";
            }

            return null;
        }

        /// <summary>
        /// Checks for interest-declaration phrases and stores the topic in memory.
        /// E.g. "I'm interested in privacy" → _userInterest = "privacy"
        /// </summary>
        private void CheckAndRememberInterest(string lower)
        {
            string[] interestPhrases = { "interested in", "i care about", "i want to learn about",
                                         "tell me about", "i'm worried about" };
            foreach (string phrase in interestPhrases)
            {
                int idx = lower.IndexOf(phrase);
                if (idx >= 0)
                {
                    string after = lower.Substring(idx + phrase.Length).Trim(' ', '.');
                    if (after.Length > 2)
                        _userInterest = after;
                    break;
                }
            }
        }

        // ── Knowledge base builders ───────────────────────────────────────────

        /// <summary>
        /// Builds the primary single-response dictionary.
        /// 20+ cybersecurity topics with comprehensive, educational content.
        /// </summary>
        private static Dictionary<string, string> BuildResponses()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Greetings
                { "hello", "Hi {name}! 👋 Great to have you here. I'm your Cybersecurity Awareness Bot. Ask me anything about staying safe online!" },
                { "hi",    "Hey {name}! 😊 Ready to boost your cybersecurity knowledge? Just ask me a question or pick a topic from the sidebar!" },
                { "how are you", "I'm fully patched and running with zero vulnerabilities! 😄 Thanks for asking, {name}. How can I help keep you safe today?" },
                { "good morning",   "Good morning, {name}! ☀️ Start your day securely — check for software updates before you begin work!" },
                { "good afternoon", "Good afternoon, {name}! 🌤️ A good time to review your recent login activity." },
                { "good evening",   "Good evening, {name}! 🌙 Before you wind down: log out of shared devices and lock your screen." },

                // Bot info
                { "purpose", "My purpose is to educate you on cybersecurity threats and safe online practices. 🛡️ I can help with phishing, passwords, malware, safe browsing, and much more!" },
                { "what can you do",
                    "Great question, {name}! Here's what I cover:\n\n" +
                    "🎣  Phishing awareness\n🔐  Password best practices\n🌐  Safe browsing habits\n" +
                    "🦠  Malware & virus protection\n📱  Social engineering defence\n🔒  Two-factor authentication\n" +
                    "💾  Data backup strategies\n🇿🇦  South African cybercrime resources\n\nJust ask about any topic!" },
                { "help",
                    "Sure, {name}! Try asking:\n\n" +
                    "  🎣  'Tell me about phishing'\n  🔐  'How do I create a strong password?'\n" +
                    "  🌐  'What is safe browsing?'\n  🦠  'What is malware?'\n" +
                    "  📲  'What is 2FA?'\n  📡  'Is public Wi-Fi safe?'\n\n" +
                    "Or click a topic in the sidebar!" },
                { "menu",
                    "📋 Available Topics:\n\n" +
                    "1. Phishing          8. Public Wi-Fi & VPN\n" +
                    "2. Password Safety   9. Data Backup\n" +
                    "3. Safe Browsing    10. Privacy\n" +
                    "4. Malware          11. Scams\n" +
                    "5. Ransomware       12. Software Updates\n" +
                    "6. Social Engineer  13. Identity Theft\n" +
                    "7. Two-Factor Auth  14. SIM Swap Attacks\n" +
                    "                   15. South Africa Resources\n\n" +
                    "Random tips: 'phishing tip', 'password tip', 'malware tip', '2fa tip', 'security tip'\n\n" +
                    "Type any topic name to learn more!" },

                // Core cybersecurity topics
                { "phish",
                    "🎣 PHISHING — What You Need to Know\n\n" +
                    "Phishing is when criminals impersonate trusted organisations to steal your credentials or money.\n\n" +
                    "🔎 How to spot a phishing email:\n" +
                    "  ✔ Check the FULL sender email address — not just the display name\n" +
                    "  ✔ Look for spelling mistakes and urgency tactics\n" +
                    "  ✔ Hover over links before clicking — verify the real URL\n" +
                    "  ✔ Legitimate companies NEVER ask for passwords via email\n" +
                    "  ✔ Be suspicious of unexpected attachments\n\n" +
                    "💡 When in doubt: go directly to the official website instead of clicking any link.\n\n" +
                    "Type 'tell me more' for additional phishing tips." },

                { "password",
                    "🔐 PASSWORD SAFETY — Your First Line of Defence\n\n" +
                    "Strong passwords protect your accounts from being hacked.\n\n" +
                    "✅ Best practices:\n" +
                    "  • Use at least 12 characters\n" +
                    "  • Mix UPPERCASE, lowercase, numbers (0-9), and symbols (!@#)\n" +
                    "  • Never reuse passwords across different sites\n" +
                    "  • Avoid personal info (birthdays, pet names, addresses)\n" +
                    "  • Use a reputable password manager (Bitwarden, 1Password)\n\n" +
                    "💡 Tip for {name}: A passphrase like 'Coffee@Sunrise!42' is both strong and memorable." },

                { "brows",
                    "🌐 SAFE BROWSING — Stay Secure Online\n\n" +
                    "  🔒 Always look for 'https://' and the padlock icon\n" +
                    "  🚫 Avoid clicking pop-up ads or suspicious download buttons\n" +
                    "  🧩 Keep your browser and extensions up to date\n" +
                    "  🛡️ Use an ad blocker and privacy-focused extensions\n" +
                    "  🔍 Verify URLs carefully — scammers use lookalike domains (e.g. paypa1.com)\n" +
                    "  📥 Only download software from official, trusted sources\n" +
                    "  🍪 Clear cookies and cache regularly" },

                { "malware",
                    "🦠 MALWARE — Malicious Software Explained\n\n" +
                    "Malware is any software designed to damage systems or steal data.\n\n" +
                    "Common types:\n" +
                    "  • Viruses      — self-replicating programs that corrupt files\n" +
                    "  • Ransomware   — encrypts your data and demands payment\n" +
                    "  • Spyware      — secretly monitors your activity\n" +
                    "  • Trojans      — disguise themselves as legitimate software\n\n" +
                    "🛡️ Protection:\n" +
                    "  ✔ Install reputable antivirus software\n" +
                    "  ✔ Keep your OS and apps patched\n" +
                    "  ✔ Never download from unknown sources\n" +
                    "  ✔ Scan USB drives before opening files" },

                { "ransomware",
                    "💰 RANSOMWARE — Don't Pay the Ransom!\n\n" +
                    "Ransomware encrypts your files and demands payment for the decryption key.\n\n" +
                    "Prevention:\n" +
                    "  ✔ Back up data regularly — offline AND cloud copies\n" +
                    "  ✔ Keep all software patched and updated\n" +
                    "  ✔ Never pay the ransom — it doesn't guarantee recovery\n" +
                    "  ✔ Report incidents to SAPS Cybercrime Unit: 10111" },

                { "social engineer",
                    "🎭 SOCIAL ENGINEERING — Hacking People, Not Systems\n\n" +
                    "Social engineering manipulates people psychologically to reveal confidential information.\n\n" +
                    "Common tactics:\n" +
                    "  • Pretexting  — creating a fake scenario to extract info\n" +
                    "  • Baiting     — leaving infected USB drives in public\n" +
                    "  • Tailgating  — following someone into a secure area\n" +
                    "  • Vishing     — voice phishing over the telephone\n\n" +
                    "🛡️ Defence: Always verify identities and never give sensitive info under pressure." },

                { "two-factor",
                    "📲 TWO-FACTOR AUTHENTICATION (2FA)\n\n" +
                    "2FA adds a second verification step beyond your password.\n\n" +
                    "How it works:\n" +
                    "  1️⃣  Enter your password\n" +
                    "  2️⃣  Confirm your identity via SMS, authenticator app, or hardware key\n\n" +
                    "Even if your password is stolen, 2FA blocks attackers from logging in.\n" +
                    "✅ Enable 2FA on ALL important accounts — especially email and banking!" },

                { "2fa",
                    "📲 2FA dramatically reduces account hijacking risk.\n\n" +
                    "Use an authenticator app (Google Authenticator, Microsoft Authenticator)\n" +
                    "rather than SMS — SMS can be intercepted via SIM-swapping attacks.\n\n" +
                    "Supported by: Gmail, Facebook, Instagram, banking apps, and most modern services." },

                { "wi-fi",
                    "📡 PUBLIC WI-FI RISKS\n\n" +
                    "  ⚠️  Evil-twin hotspots can mimic legitimate network names\n" +
                    "  ⚠️  Man-in-the-middle attacks can intercept your traffic\n" +
                    "  ⚠️  Unencrypted connections expose your passwords\n\n" +
                    "🛡️ Stay safe:\n" +
                    "  ✔ Always use a VPN on public networks\n" +
                    "  ✔ Avoid banking apps on public Wi-Fi\n" +
                    "  ✔ Use your mobile data instead when possible" },

                { "wifi",
                    "📡 Public Wi-Fi is risky! Always use a VPN when connecting to public networks\n" +
                    "and avoid accessing sensitive accounts like banking or email on shared connections." },

                { "identity theft",
                    "🪪 IDENTITY THEFT — Protecting Who You Are Online\n\n" +
                    "Identity theft occurs when someone steals your personal information\n" +
                    "to impersonate you for financial gain.\n\n" +
                    "🔎 Warning signs:\n" +
                    "  • Unexpected bills or account statements\n" +
                    "  • Unfamiliar accounts on your credit report\n" +
                    "  • Being denied credit unexpectedly\n\n" +
                    "🛡️ Prevention:\n" +
                    "  ✔ Never share your ID number or banking details online\n" +
                    "  ✔ Use strong unique passwords and enable 2FA\n" +
                    "  ✔ Check haveibeenpwned.com regularly\n" +
                    "  ✔ Report identity theft to SAPS and your bank immediately" },

                { "sim swap",
                    "📱 SIM SWAP ATTACKS — A Growing Threat in South Africa\n\n" +
                    "A SIM swap attack is when a fraudster convinces your mobile network\n" +
                    "to transfer your number to a SIM card they control.\n\n" +
                    "Once they have your number, they can:\n" +
                    "  • Intercept your SMS OTPs\n" +
                    "  • Access your banking app\n" +
                    "  • Reset your email and social media passwords\n\n" +
                    "🛡️ Protection:\n" +
                    "  ✔ Use an authenticator app instead of SMS for 2FA\n" +
                    "  ✔ Add a SIM swap PIN/password with your mobile provider\n" +
                    "  ✔ If your phone loses signal unexpectedly, call your provider immediately" },

                { "vpn",
                    "🔐 A VPN (Virtual Private Network) encrypts your internet connection\n" +
                    "and masks your IP address.\n\n" +
                    "Choose a reputable PAID VPN — free VPNs often sell your data.\n" +
                    "Recommended: ProtonVPN, Mullvad, ExpressVPN." },

                { "backup",
                    "💾 DATA BACKUP — The 3-2-1 Rule\n\n" +
                    "Regular backups protect against ransomware, hardware failure, and accidental deletion.\n\n" +
                    "  3️⃣  Keep 3 copies of your data\n" +
                    "  2️⃣  Store on 2 different media types (e.g. hard drive + cloud)\n" +
                    "  1️⃣  Keep 1 copy offsite or in the cloud\n\n" +
                    "⚠️  Test your backups regularly — a backup you can't restore is worthless!" },

                { "privacy",
                    "🔏 PRIVACY — Protecting Your Personal Information\n\n" +
                    "  • Review app permissions regularly — revoke what isn't needed\n" +
                    "  • Use privacy-focused browsers (Firefox, Brave)\n" +
                    "  • Enable private/incognito mode on shared devices\n" +
                    "  • Limit personal info shared on social media\n" +
                    "  • Check haveibeenpwned.com to see if your email has been breached\n\n" +
                    "As someone interested in {interest}, this is especially important for you, {name}." },

                { "scam",
                    "⚠️ COMMON SOUTH AFRICAN ONLINE SCAMS\n\n" +
                    "  • 'You've won!' lottery and prize scams\n" +
                    "  • Fake job offers requiring upfront payment\n" +
                    "  • Romance scams on dating platforms\n" +
                    "  • SARS (tax) impersonation emails and calls\n" +
                    "  • Banking vishing (voice phishing) calls\n" +
                    "  • Fake online stores that take payment but deliver nothing\n\n" +
                    "💡 Rule of thumb: if it sounds too good to be true, it almost certainly is!" },

                { "update",
                    "🔄 SOFTWARE UPDATES — Don't Delay!\n\n" +
                    "Attackers actively exploit unpatched systems within hours of a vulnerability being published.\n\n" +
                    "  ✔ Enable automatic updates for your OS and applications\n" +
                    "  ✔ Keep browsers and plugins updated\n" +
                    "  ✔ Update your home router's firmware regularly" },

                { "firewall",
                    "🔥 A firewall monitors and controls incoming/outgoing network traffic.\n" +
                    "It acts as a barrier between your device and untrusted networks.\n\n" +
                    "Ensure your OS firewall is always ENABLED.\n" +
                    "Consider a hardware firewall for your home network." },

                { "encrypt",
                    "🔐 ENCRYPTION — Making Your Data Unreadable to Attackers\n\n" +
                    "  • Files on your device  — BitLocker (Windows), FileVault (Mac)\n" +
                    "  • Communications        — Signal, WhatsApp end-to-end encryption\n" +
                    "  • Email                 — PGP encryption for sensitive messages" },

                { "south africa",
                    "🇿🇦 SOUTH AFRICA — Cybersecurity Resources\n\n" +
                    "South Africa has one of the highest cybercrime rates on the continent.\n\n" +
                    "Key local resources:\n" +
                    "  • Report cybercrime : www.cybercrime.org.za\n" +
                    "  • SAPS Cybercrime Unit : 10111\n" +
                    "  • SABRIC (Banking Risk) : www.sabric.co.za\n" +
                    "  • POPIA — Protection of Personal Information Act protects your data rights\n" +
                    "  • Report fraud : www.justice.gov.za" },

                { "exit",
                    "Goodbye, {name}! 👋 Stay safe online.\n\n" +
                    "Remember:\n  🔐 Use strong, unique passwords\n  📲 Enable 2FA everywhere\n" +
                    "  🎣 Think before you click\n  💾 Back up your data regularly\n\n" +
                    "Come back anytime! 🛡️" },

                { "thank",
                    "You're welcome, {name}! 😊 Staying informed is your best defence online.\n" +
                    "Is there anything else you'd like to know?" },

                { "virus",
                    "🦠 A computer virus attaches itself to legitimate files and spreads when shared.\n" +
                    "Use up-to-date antivirus software and avoid opening email attachments from unknown senders." },

                { "https",
                    "🔒 HTTPS encrypts data between your browser and a website.\n" +
                    "Always check for 'https://' and a padlock before entering any sensitive information.\n" +
                    "HTTP (without S) = unencrypted = avoid entering passwords there." },

                { "authenticat",
                    "📲 Strong authentication uses multiple factors:\n" +
                    "  🔑 Something you KNOW   — password or PIN\n" +
                    "  📱 Something you HAVE   — phone or security token\n" +
                    "  👁️ Something you ARE    — fingerprint or face scan\n\n" +
                    "Enable MFA (Multi-Factor Authentication) wherever it is available!" },

                { "data",
                    "📊 Your personal data is valuable — protect it:\n" +
                    "  ✔ Only share what is absolutely necessary\n" +
                    "  ✔ Use strong, unique passwords for every account\n" +
                    "  ✔ Check haveibeenpwned.com for breaches\n" +
                    "  ✔ Review connected app permissions regularly" },
            };
        }

        /// <summary>
        /// Builds the random-response dictionary.
        /// Each key maps to a LIST of responses; one is picked randomly per interaction.
        /// Part 2 requirement: variability to keep interactions engaging.
        /// </summary>
        private static Dictionary<string, List<string>> BuildRandomResponses()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "phishing tip", new List<string>
                    {
                        "🎣 Tip: Be cautious of emails creating urgency — 'Your account will be closed in 24 hours!' is a classic phishing tactic.",
                        "🎣 Tip: Always hover over links before clicking. If the URL looks suspicious or doesn't match the sender's domain, don't click it.",
                        "🎣 Tip: Legitimate banks and government agencies will NEVER ask for your password, OTP, or PIN via email or SMS.",
                        "🎣 Tip: Check the sender's full email address — scammers use addresses like 'security@paypa1-support.com' that look legitimate at a glance.",
                        "🎣 Tip: If you receive an unexpected attachment, even from someone you know, confirm with them via a separate channel before opening it.",
                    }
                },
                {
                    "password tip", new List<string>
                    {
                        "🔐 Tip: Use a passphrase — a sequence of random words like 'Purple$Monkey!Dishwasher42' is both strong and memorable.",
                        "🔐 Tip: Enable your password manager's breach monitoring feature — it will alert you when a saved password appears in a known data breach.",
                        "🔐 Tip: Avoid 'password walking' — patterns like 'qwerty', '123456', or keyboard walks are the first things hackers try.",
                        "🔐 Tip: Change your passwords immediately if a site you use announces a data breach — don't wait.",
                        "🔐 Tip: Use a different password for EVERY account. If one is compromised, the damage stays contained.",
                    }
                },
                {
                    "security tip", new List<string>
                    {
                        "💡 Keep your software and operating system up to date — most attacks exploit known vulnerabilities that patches have already fixed.",
                        "💡 Enable automatic screen lock on all your devices — if you walk away, your data stays protected.",
                        "💡 Be careful what you share on social media — cybercriminals use public posts to craft targeted attacks.",
                        "💡 Shred documents containing personal information before discarding them — physical data theft is still common.",
                        "💡 Check your financial statements regularly for unauthorised transactions — early detection limits damage.",
                        "💡 Use Have I Been Pwned (haveibeenpwned.com) to check if your email has appeared in a data breach.",
                    }
                },
                {
                    "safe browsing tip", new List<string>
                    {
                        "🌐 Tip: Install uBlock Origin — it's a free, open-source ad blocker that also blocks many malicious scripts.",
                        "🌐 Tip: Use Firefox or Brave for improved privacy — both block trackers by default.",
                        "🌐 Tip: Check a website's privacy policy before entering personal details — if there isn't one, leave.",
                        "🌐 Tip: Avoid clicking 'Allow' on browser notification pop-ups from unknown sites — this is how malicious notification spam spreads.",
                    }
                },
                {
                    "malware tip", new List<string>
                    {
                        "🦠 Tip: Keep your antivirus software up to date — new malware variants are released daily and signature databases need to be current.",
                        "🦠 Tip: Never plug in a USB drive you found or received unexpectedly — attackers deliberately leave infected drives in public places.",
                        "🦠 Tip: Disable AutoRun/AutoPlay on Windows — this prevents malware from automatically executing when you insert a drive.",
                        "🦠 Tip: Regularly scan your device even if nothing seems wrong — many malware types are designed to run silently in the background.",
                        "🦠 Tip: Be cautious of 'free' software downloads — they often bundle adware or spyware. Use official sources only.",
                    }
                },
                {
                    "2fa tip", new List<string>
                    {
                        "📲 Tip: Use an authenticator app (Google Authenticator, Microsoft Authenticator) rather than SMS — SMS codes can be intercepted via SIM swap attacks.",
                        "📲 Tip: Enable 2FA on your email first — it's the master key to all your other accounts via password reset links.",
                        "📲 Tip: Store your 2FA backup codes somewhere safe and offline — if you lose your phone, these are your only way back in.",
                        "📲 Tip: Hardware security keys (like YubiKey) provide the strongest form of 2FA and are immune to phishing attacks.",
                        "📲 Tip: Even if a site only offers SMS 2FA, enable it — it's still far better than a password alone.",
                    }
                },
            };
        }

        /// <summary>
        /// Maps sentiment keywords to label, emoji, and empathetic prefix strings.
        /// Part 2 requirement: detect and respond to user emotions.
        /// </summary>
        private static Dictionary<string, (string, string, string)> BuildSentimentMap()
        {
            return new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                { "worried",     ("worried",    "😟", "I completely understand — it's normal to feel anxious about online threats. You're already taking a great step by learning about them.") },
                { "scared",      ("worried",    "😟", "It's okay to feel that way. Cybercrime is a real threat, but knowledge is your best protection.") },
                { "anxious",     ("worried",    "😟", "Take a breath — you're in the right place. Let me share some practical steps that will help you feel safer online.") },
                { "stressed",    ("stressed",   "😤", "I hear you. Security can feel overwhelming, but we'll take it one step at a time.") },
                { "frustrated",  ("frustrated", "😤", "Totally understandable — security tools can be complex. Let me break it down simply for you.") },
                { "confused",    ("confused",   "🤔", "No worries — cybersecurity jargon is notoriously confusing! Let me explain in plain language.") },
                { "curious",     ("curious",    "🤓", "Love the curiosity, {name}! Curiosity is the foundation of great security habits.") },
                { "interested",  ("curious",    "🤓", "Great that you're interested! Let me share what I know.") },
                { "happy",       ("happy",      "😊", "Love the positive energy, {name}! Let's channel that into building great security habits.") },
                { "excited",     ("happy",      "😊", "That enthusiasm is wonderful! Let's put it to good use.") },
                { "unsure",      ("confused",   "🤔", "That's completely fine — cybersecurity is a big topic. Let's start with the basics.") },
            };
        }
    }
}

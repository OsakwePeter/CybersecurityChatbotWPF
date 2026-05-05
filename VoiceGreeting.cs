// ============================================================
//  VoiceGreeting.cs  (Part 2 — WPF Edition)
//  Responsibility: Plays the recorded WAV greeting on startup.
//                 Identical logic to Part 1 — the voice feature
//                 is preserved as required by the Part 2 spec.
//  Note         : System.Media.SoundPlayer is Windows-only.
//                 Audio failure is caught and suppressed so
//                 the chatbot never crashes due to audio issues.
// ============================================================

using System;
using System.IO;
using System.Media;

namespace CybersecurityChatbotWPF
{
    /// <summary>
    /// Handles WAV voice greeting playback on application startup.
    /// Static class — no instantiation required.
    /// </summary>
    public static class VoiceGreeting
    {
        private const string WavFileName = "greeting.wav";

        /// <summary>
        /// Plays the greeting.wav file asynchronously.
        /// Fails silently if the file is missing or audio is unavailable.
        /// </summary>
        public static void PlayAsync()
        {
            string wavPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, WavFileName);

            if (!File.Exists(wavPath)) return;

            try
            {
                SoundPlayer player = new SoundPlayer(wavPath);
                player.Play(); // Non-blocking — GUI remains responsive
            }
            catch
            {
                // Audio failure must never crash the chatbot
            }
        }
    }
}

# 🛡️ Cybersecurity Awareness Chatbot — Part 2 (WPF GUI)

**Module:** PROG6221/w — Programming 2A  
**Part:** 2 of 3  
**Framework:** WPF (.NET 8 / Windows)

---

## 📋 Overview

This is the Part 2 upgrade of the Cybersecurity Awareness Chatbot. The console application from Part 1 has been redesigned as a full **WPF (Windows Presentation Foundation) GUI** application with advanced features including keyword recognition, random responses, conversation memory, and sentiment detection.

---

## ✨ Features (Part 2 Requirements)

| Requirement | Implementation |
|---|---|
| ✅ GUI Design (WPF) | Dark-theme, two-panel layout with sidebar and chat area |
| ✅ Voice Greeting | `VoiceGreeting.PlayAsync()` plays `greeting.wav` on startup |
| ✅ ASCII Art in GUI | Displayed in the sidebar `TextBlock` with `Courier New` font |
| ✅ Keyword Recognition | `ResponseEngine` dictionary covers 20+ cybersecurity topics |
| ✅ Random Responses | `_randomResponses` dictionary returns varied tips each call |
| ✅ Conversation Flow | Follow-up phrases ("tell me more", "another tip") continue topic |
| ✅ Memory & Recall | Engine stores user name and expressed interest; recalls them later |
| ✅ Sentiment Detection | `SentimentChangedHandler` delegate updates mood in sidebar |
| ✅ Error Handling | Input validation + graceful fallback for unknown queries |
| ✅ Code Optimisation | Separated into: `ResponseEngine`, `MessageBubbleFactory`, `VoiceGreeting`, `ChatMessage` |

---

## 🚀 How to Run

### Prerequisites
- **Visual Studio 2022** (or later) with the **.NET desktop development** workload
- **.NET 8 SDK** installed
- **Windows OS** (required for WPF and `System.Media.SoundPlayer`)

### Steps
1. Clone or unzip the repository
2. Open `CybersecurityChatbotWPF.csproj` in Visual Studio
3. Press **F5** to build and run
4. (Optional) Place your `greeting.wav` file in the project root — it will be copied to the output directory automatically

### Voice Greeting
Copy your `greeting.wav` from Part 1 into the `CybersecurityChatbotWPF/` folder.  
The project file already includes: `<None Update="greeting.wav"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`

---

## 🗂️ Project Structure

```
CybersecurityChatbotWPF/
├── App.xaml                  # Application resources (colours, styles)
├── App.xaml.cs               # Application entry point
├── MainWindow.xaml           # Main UI layout (XAML)
├── MainWindow.xaml.cs        # UI code-behind (event handlers, session logic)
├── ResponseEngine.cs         # Knowledge base, keyword matching, NLP simulation
├── MessageBubbleFactory.cs   # Factory: builds styled WPF message bubbles
├── ChatMessage.cs            # Data model for a single chat message
├── VoiceGreeting.cs          # WAV playback on startup
├── greeting.wav              # Voice greeting audio file (copy from Part 1)
└── README.md                 # This file
```

---

## 💬 How to Use

1. **Launch** the application — the voice greeting plays and the welcome message appears
2. **Type your name** to personalise the session
3. **Ask questions** like:
   - `"Tell me about phishing"`
   - `"What is a strong password?"`
   - `"I'm worried about online scams"`
   - `"Give me a phishing tip"` (random response each time)
   - `"Tell me more"` (follow-up on last topic)
4. **Click sidebar buttons** for quick access to any topic
5. Type `menu` to see all available topics

---

## 🧠 Sentiment Detection Examples

| What you type | Detected mood | Bot response style |
|---|---|---|
| "I'm worried about phishing" | 😟 Worried | Empathetic, reassuring |
| "I'm confused about 2FA" | 🤔 Confused | Simple, plain-language explanation |
| "I'm curious about malware" | 🤓 Curious | Enthusiastic, detailed |
| "This is frustrating" | 😤 Frustrated | Patient, step-by-step |

---

## 🔄 GitHub & CI

- Minimum **6 meaningful commits** with descriptive messages
- **GitHub Actions** CI workflow runs a `.NET build` check on every push
- At least **2 GitHub Releases** with version tags (`v2.0`, `v2.1`)

---

## 📸 CI Workflow Screenshot

> 
---

## 🎥 Video Presentation

> YouTube unlisted link: https://youtu.be/I0d5oePawgs---

## 📚 References

- Pieterse, H. 2021. *The Cyber Threat Landscape in South Africa: A 10-Year Review*. The African Journal of Information and Communication, 28(28). doi: https://doi.org/10.23962/10539/32213
- Microsoft. 2024. *WPF Overview*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/ [Accessed 2026]
- Microsoft. 2024. *Data binding overview*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/ [Accessed 2026]

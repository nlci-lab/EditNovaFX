# EditNovaFX
### Professional Desktop Video Editor — Built with C# · WPF · .NET 8

> A full-featured video editing desktop app for Windows ,
> YouTube publishing, multi-track timeline editing, and rich subtitle support.
> Developed by **Jacob's Studio**.

---

## ✨ Key Features

| Feature | Details |
|---|---|
| 🎬 **Multi-track Timeline** | Video, audio & subtitle tracks with drag, resize, trim and playhead scrubbing |
| 🤖 **AI Transcription (Local)** | Whisper.cpp speech-to-text → auto-generates SRT subtitle files |
| ☁️ **AI Transcription (Cloud)** | OpenAI Whisper API for word-level accuracy |
| 📝 **Subtitle Editor** | Import SRT files, font/color/size/bold/italic styling, live preview |
| 🖼️ **Logo & Watermark** | Add image overlays with position and size controls |
| 📤 **Export** | FFmpeg-powered 720p / 1080p / 1440p / 4K MP4 export with progress |
| 🚀 **YouTube Publisher** | OAuth Google sign-in → upload video + custom thumbnail directly |
| ❓ **Rich Help System** | Structured step-by-step guides, tips, keyboard shortcuts, FAQ per topic |

---

## 🗂️ Project Structure

```
EditNovaFX/
├── Models/               # Data models (Project, Clip, Track, Subtitle …)
├── ViewModels/           # MVVM business logic (CommunityToolkit.Mvvm)
├── Views/                # WPF XAML windows and dialogs
├── Services/             # FFmpeg, Whisper, YouTube, AI content, Undo/Redo
├── Controls/             # Custom WPF controls (TimelineControl)
├── Converters/           # IValueConverter implementations
├── Utilities/            # Timecode helpers
├── Assets/               # App icon / logo
├── App.xaml              # Application-wide resources and styles
└── VideoEditor.csproj    # .NET 8 WPF project file
```

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 8 · WPF (Windows Presentation Foundation) |
| MVVM | CommunityToolkit.Mvvm 8.2.2 |
| Video engine | FFmpeg (bundled) |
| AI (local) | Whisper.cpp — `whisper-cli.exe` + `ggml-*.bin` models |
| AI (cloud) | OpenAI Whisper API |
| YouTube API | Google.Apis.YouTube.v3 1.73.0 + Google.Apis.Auth |
| Serialization | Newtonsoft.Json |

---

## 🚀 Getting Started (for Developers)

### Prerequisites

- **Windows 10 / 11** (64-bit)
- **.NET 8 SDK** → https://dotnet.microsoft.com/download
- **FFmpeg** — place `ffmpeg.exe` + `ffprobe.exe` in a `ffmpeg\` subfolder next to the exe
- **Whisper.cpp** *(optional, for AI subtitles)* — place `whisper.exe` in a `whisper-bin-x64\` subfolder next to the exe
- **Subtitle Parser** *(optional, for Scripture subtitles)* — download from [EditNovaFX-SRT-Generator](https://github.com/nlci-lab/EditNovaFX-SRT-Generator/releases/tag/v0.1.1-alpha), place in a `subtitle-parser\` subfolder

> **Tip:** All tool paths can be configured in **File → Settings** if you prefer a different location.

### Build & Run

```bash
# Clone the repo
git clone https://github.com/nlci-lab/EditNovaFX.git
cd EditNovaFX

# Restore packages
dotnet restore

# Build (Debug)
dotnet build

# Run
dotnet run
```

### Publish (self-contained, win-x64)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

---

## 🤖 AI Transcription Setup

The **Audio → Subtitle** feature needs a Whisper model file (not included — too large for GitHub).

1. Download one `.bin` file from [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp/tree/main):

   | Model | Size | Best For |
   |---|---|---|
   | ggml-tiny.bin | 75 MB | Quick drafts |
   | ggml-base.bin | 142 MB | General English ✅ Recommended |
   | ggml-small.bin | 466 MB | Non-English languages |
   | ggml-medium.bin | 1.5 GB | Hindi, Tamil, multi-language ✅ |
   | ggml-large-v3.bin | 3.1 GB | Maximum accuracy |

2. Place the file in:  `<app folder>\models\`

3. Restart the app — it auto-detects the model.

> **Note:** The app shows a download banner with direct links when no model is found.

---

## 📤 YouTube Publishing Setup

YouTube uploading requires your own Google API credentials (none are bundled with the app).

1. Go to [Google Cloud Console](https://console.cloud.google.com/) → create a project
2. Enable **YouTube Data API v3** (APIs & Services → Library)
3. Create **OAuth 2.0 Client ID** (APIs & Services → Credentials → Desktop app)
4. In EditNovaFX: **Tools → Setup YouTube API** → paste your Client ID and Secret → Save
5. Click **Login to YouTube** in the AI Publisher — a browser opens for Google OAuth
6. Your credentials are stored in `%AppData%\EditNovaFX\youtube_secrets.json`

---

## 📦 NuGet Dependencies

```xml
<PackageReference Include="Google.Apis.Auth"         Version="1.73.0" />
<PackageReference Include="Google.Apis.YouTube.v3"   Version="1.73.0.4029" />
<PackageReference Include="Newtonsoft.Json"           Version="13.0.4" />
<PackageReference Include="CommunityToolkit.Mvvm"    Version="8.2.2" />
```

---

## 📋 Keyboard Shortcuts

| Key | Action |
|---|---|
| `Space` | Play / Pause |
| `F11` | Fullscreen preview toggle |
| `Ctrl + S` | Save project |
| `Ctrl + Z` | Undo |
| `Ctrl + Y` | Redo |
| `Delete` | Remove selected clip |

---

## 📁 Excluded from Repo

The following are **not** committed (see `.gitignore`):

- `bin/` · `obj/` · `publish/` — build outputs
- `ffmpeg/` · `whisper-bin-x64/` — large binary tools
- `models/*.bin` — AI model files (75 MB – 3 GB)
- `InstallPackage/` · `*.zip` — distribution packages
- `**/token.json` · `**/youtube_secrets.json` — OAuth tokens

---

## 🪪 License

Source code provided for review and educational purposes.  
© 2026 Jacob's Studio. All rights reserved.

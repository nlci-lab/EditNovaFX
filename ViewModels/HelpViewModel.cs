using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoEditor.Models;

namespace VideoEditor.ViewModels
{
    public partial class HelpViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<HelpTopic> _topics;

        [ObservableProperty]
        private HelpTopic? _selectedTopic;

        public HelpViewModel()
        {
            _topics = new ObservableCollection<HelpTopic>();
            InitializeContent();
            SelectedTopic = _topics.FirstOrDefault();
        }

        private void InitializeContent()
        {
            BuildGettingStarted();
            BuildTimeline();
            BuildAudioSubtitles();
            BuildAiFeatures();
            BuildExportPublish();
            BuildScriptureSubtitles();
            BuildExternalTools();
            BuildSupport();
            BuildKeyboardShortcuts();
            BuildTroubleshooting();
            BuildAbout();
        }

        // ────────────────────────────────────────────────────────────────────
        // 1. GETTING STARTED
        // ────────────────────────────────────────────────────────────────────
        private void BuildGettingStarted()
        {
            var topic = new HelpTopic
            {
                Title = "Getting Started",
                Icon = "🚀",
                Summary = "Welcome to EditNovaFX! This guide walks you through making your very first video from scratch. No experience needed — just follow the steps below.",
                ScreenZones = new()
                {
                    new() { Label = "① Media Library", Description = "Top-left panel. Add and browse your videos, audio, and images here.", Color = "#1F6B8E" },
                    new() { Label = "② Preview", Description = "Center panel. Watch your video as you edit it. Press Space to play.", Color = "#1E6B3A" },
                    new() { Label = "③ Properties", Description = "Right panel. Change settings, subtitle styles, and export the video.", Color = "#6B3A1E" },
                    new() { Label = "④ Timeline", Description = "Bottom panel. Arrange your clips in the order you want them to appear.", Color = "#4A1E6B" },
                }
            };

            topic.Steps.AddRange(new[]
            {
                new HelpStep { Number = 1, Emoji = "📁", Title = "Create or Open a Project", Detail = "Click File → New Project to start fresh. Give your project a name. You can also click File → Open Project to continue an existing one." },
                new HelpStep { Number = 2, Emoji = "📥", Title = "Import Your Media Files", Detail = "Click the 📁+ button in the Media Library (top-left), or drag files directly from Windows Explorer into the library. Supported: MP4, MOV, AVI, MP3, WAV, PNG, JPG and more." },
                new HelpStep { Number = 3, Emoji = "🎞️", Title = "Add Media to the Timeline", Detail = "Select a file in the Media Library and click 'Add to Timeline' at the bottom left — or simply drag it onto a timeline track. Videos go on Video tracks, audio on Audio tracks." },
                new HelpStep { Number = 4, Emoji = "✂️", Title = "Trim and Arrange Your Clips", Detail = "Drag clips left/right on the timeline to reorder them. To trim: drag the left or right edge of a clip inward. To split a clip at the playhead, position the red line and click ✂ Split." },
                new HelpStep { Number = 5, Emoji = "▶️", Title = "Preview Your Work", Detail = "Press Spacebar or click the ▶ Play button (bottom of the preview panel) to play through your video. The red playhead shows your current position." },
                new HelpStep { Number = 6, Emoji = "🎬", Title = "Export Your Video", Detail = "When you're happy with the result, click the big 'Export Video' button on the right panel. Choose your output folder and click Export. EditNovaFX will render the final file." },
            });

            topic.Tips.Add("You can drag media files directly from Windows File Explorer into the Media Library — no need to use the import button.");
            topic.Tips.Add("Press Ctrl + S often to save your project progress.");
            topic.Warnings.Add("Do not close the app while exporting. Wait until the progress bar completes.");

            // Quick Start subtopic
            var quickStart = new HelpTopic
            {
                Title = "Quick Start Guide",
                Icon = "⚡",
                Summary = "Need to create a simple video fast? Here's the shortest possible path from zero to a finished video.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "📂", Title = "Import a video file", Detail = "Click 📁+ in the Media Library and select your video file." },
                    new() { Number = 2, Emoji = "🖱️", Title = "Drag it to the Timeline", Detail = "Drag the video from the library onto 'Video/Image 1' track at the bottom." },
                    new() { Number = 3, Emoji = "💾", Title = "Export immediately", Detail = "Click 'Export Video' on the right panel. If you need no edits, this is all you need!" },
                },
                Tips = new() { "This 3-step workflow works perfectly for simple cuts, resizing, or format conversions." }
            };
            topic.SubTopics.Add(quickStart);

            // Importing Media subtopic
            var importMedia = new HelpTopic
            {
                Title = "Importing Media",
                Icon = "📥",
                Summary = "EditNovaFX supports a wide range of file formats. Here's how to get your files into the project.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "📁", Title = "Click the Import button", Detail = "In the Media Library (top-left), click the 📁+ icon, OR use the top menu: Import → Import Media." },
                    new() { Number = 2, Emoji = "📂", Title = "Browse for your file", Detail = "A file picker window opens. Navigate to your file and click Open. You can select multiple files at once by holding Ctrl." },
                    new() { Number = 3, Emoji = "✅", Title = "File appears in the library", Detail = "Your file will appear under the Videos, Audio, or Images tab automatically based on its type." },
                },
                Tips = new()
                {
                    "Supported video formats: MP4, MOV, AVI, MKV, WMV.",
                    "Supported audio formats: MP3, WAV, AAC, OGG, FLAC.",
                    "Supported image formats: PNG, JPG, JPEG, BMP, GIF.",
                    "Drag & drop directly from Windows Explorer works too!"
                },
                Warnings = new() { "Very large files (over 10 GB) may cause slow performance during import and preview." }
            };
            topic.SubTopics.Add(importMedia);

            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. TIMELINE GUIDE
        // ────────────────────────────────────────────────────────────────────
        private void BuildTimeline()
        {
            var topic = new HelpTopic
            {
                Title = "Timeline Guide",
                Icon = "🎞️",
                Summary = "The Timeline is the heart of EditNovaFX. It's where you arrange, trim, and layer your media clips to build your final video.",
                ScreenZones = new()
                {
                    new() { Label = "🔴 Playhead", Description = "The red vertical line. Drag it to jump to any point in time.", Color = "#8B1A1A" },
                    new() { Label = "🟦 Clip", Description = "A block representing a media file on the timeline. Drag to move it.", Color = "#1A4A8B" },
                    new() { Label = "🟩 Track", Description = "A horizontal row. Video tracks hold visuals; Audio tracks hold sound.", Color = "#1A6B3A" },
                    new() { Label = "🔍 Zoom Slider", Description = "Top-right of the timeline. Slide right to zoom in for precise editing.", Color = "#5A5A5A" },
                }
            };

            topic.Tips.Add("Use the Zoom slider (top of timeline) to zoom in for frame-accurate trimming.");
            topic.Tips.Add("Right-click a clip for quick options: Delete, Split, Properties.");
            topic.Warnings.Add("Each track accepts only one media type: Video tracks for video/images, Audio tracks for audio. Dropping the wrong type will be ignored.");

            var adding = new HelpTopic
            {
                Title = "Adding Clips",
                Icon = "➕",
                Summary = "There are two easy ways to add media clips to your timeline.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🖱️", Title = "Method 1 – Drag and Drop", Detail = "Click and hold a file in the Media Library, then drag it to a track on the timeline. Release the mouse to place it." },
                    new() { Number = 2, Emoji = "🖱️", Title = "Method 2 – Right-Click Menu", Detail = "Select a file in the Media Library, then right-click it and choose 'Add to Timeline'. It will be placed at the end of the current track." },
                    new() { Number = 3, Emoji = "🔄", Title = "Move Clips Around", Detail = "Once on the timeline, click and drag any clip left or right to reposition it. The clip snaps when close to another clip." },
                },
                Tips = new() { "Hold Shift while dragging to lock the clip to its current track (prevents accidental track changes)." }
            };

            var splitting = new HelpTopic
            {
                Title = "Splitting Clips",
                Icon = "✂️",
                Summary = "Splitting lets you cut a clip into two pieces at any point — perfect for removing unwanted sections.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🔴", Title = "Position the Playhead", Detail = "Click on the timeline ruler (the time bar at the top) to move the red playhead to the exact moment where you want to cut." },
                    new() { Number = 2, Emoji = "✂️", Title = "Click Split", Detail = "Click the '✂ Split' button in the timeline toolbar, or press Ctrl+Shift+S. The clip under the playhead is cut into two separate clips." },
                    new() { Number = 3, Emoji = "🗑️", Title = "Delete the Unwanted Part", Detail = "Click the unwanted piece to select it, then press the Delete key to remove it." },
                },
                Tips = new() { "Zoom in first before splitting to get a more precise cut." },
                Warnings = new() { "Make sure the playhead is exactly over the clip you want to split, not over an empty gap." }
            };

            var ripple = new HelpTopic
            {
                Title = "Ripple Editing",
                Icon = "🌊",
                Summary = "Ripple mode controls what happens to other clips when you delete or move one.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🟢", Title = "Ripple ON (default)", Detail = "When you delete a clip, all the clips to its right automatically slide left to fill the gap. This keeps your video gap-free and in sync." },
                    new() { Number = 2, Emoji = "⚪", Title = "Ripple OFF", Detail = "Clips stay where they are when you delete. A gap is left behind. Use this for multi-track editing when you need precise alignment." },
                    new() { Number = 3, Emoji = "🔘", Title = "Toggle the mode", Detail = "Click the 'Ripple' toggle button in the timeline toolbar (top-right of the timeline area)." },
                },
                Tips = new() { "For beginners, keep Ripple ON. It prevents accidental gaps in your video." }
            };

            topic.SubTopics.Add(adding);
            topic.SubTopics.Add(splitting);
            topic.SubTopics.Add(ripple);
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 3. AUDIO & SUBTITLES
        // ────────────────────────────────────────────────────────────────────
        private void BuildAudioSubtitles()
        {
            var topic = new HelpTopic
            {
                Title = "Audio & Subtitles",
                Icon = "🎵",
                Summary = "Learn how to add background music, adjust audio levels, add subtitle text, and use the AI to automatically generate subtitles from speech.",
            };
            topic.Tips.Add("Use the Audio tab in the Media Library to find your imported music files.");

            var aiSub = new HelpTopic
            {
                Title = "AI Subtitle Generator",
                Icon = "🤖",
                Summary = "EditNovaFX uses a built-in AI (Whisper) to listen to your video's audio and automatically create subtitles — no internet needed!",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🎵", Title = "Make sure your video has audio", Detail = "The AI reads the spoken words from your video or audio clip on the timeline. Ensure the audio clip is present and enabled." },
                    new() { Number = 2, Emoji = "🔧", Title = "Open the tool", Detail = "Click the top menu: Tools → Audio to Subtitle. A dialog window will open." },
                    new() { Number = 3, Emoji = "🌐", Title = "Choose your language", Detail = "Select the language spoken in your video from the dropdown list (e.g., English, Hindi, Spanish)." },
                    new() { Number = 4, Emoji = "▶️", Title = "Click Generate", Detail = "Press the Generate button. The AI will process the audio. This may take 1–3 minutes for a 10-minute video." },
                    new() { Number = 5, Emoji = "✅", Title = "Subtitles appear automatically", Detail = "When finished, a new subtitle track is added to your project. You will see the text overlaid on your video in the preview." },
                },
                Tips = new()
                {
                    "The AI works best with clear speech and minimal background noise.",
                    "Use the 'small' model for speed, or 'medium' model for better accuracy in the settings.",
                },
                Warnings = new()
                {
                    "The first run downloads the AI model file. This requires an internet connection only once.",
                    "If the AI seems to produce no output, check that the Whisper model file exists in the correct folder (see Troubleshooting)."
                }
            };

            var styling = new HelpTopic
            {
                Title = "Subtitle Styling",
                Icon = "🎨",
                Summary = "Once subtitles are generated or imported, you can fully customize their appearance from the Properties panel on the right.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🖱️", Title = "Select a subtitle track", Detail = "In the Properties panel (right side), scroll down to 'Subtitle Tracks' and click on a track name to select it." },
                    new() { Number = 2, Emoji = "🔤", Title = "Change font & size", Detail = "Use the Font and Size dropdowns to pick any installed font and a text size from 12 to 96." },
                    new() { Number = 3, Emoji = "🎨", Title = "Change color", Detail = "Choose a text color from the Color dropdown. Common choices: White for dark backgrounds, Yellow for better visibility." },
                    new() { Number = 4, Emoji = "📐", Title = "Drag to reposition", Detail = "In the Preview area, click and drag a subtitle text box to move it anywhere on the screen." },
                    new() { Number = 5, Emoji = "↔️", Title = "Resize the text box", Detail = "Drag the corner handles (white squares) around the selected subtitle to make the text box wider or narrower." },
                },
                Tips = new()
                {
                    "White text with a black shadow (Outline Width > 0) is the most readable on any background.",
                    "Bold text is easier to read on small screens or mobile."
                }
            };

            var importSub = new HelpTopic
            {
                Title = "Import Subtitles (SRT)",
                Icon = "📄",
                Summary = "Already have a subtitle file? You can import it directly.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "📂", Title = "Open Import dialog", Detail = "Click the top menu: Import → Import Subtitle, or click the 'Import Subtitle' button in the Properties panel." },
                    new() { Number = 2, Emoji = "📄", Title = "Select your file", Detail = "Browse for your SRT file and click Open." },
                    new() { Number = 3, Emoji = "✅", Title = "Track is added", Detail = "The subtitle track appears in the Subtitle Tracks list on the right panel and is visible in the preview." },
                },
                Tips = new() { "SRT is the most common format. ASS files also support advanced styling." }
            };

            topic.SubTopics.Add(aiSub);
            topic.SubTopics.Add(styling);
            topic.SubTopics.Add(importSub);
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. AI FEATURES
        // ────────────────────────────────────────────────────────────────────
        private void BuildAiFeatures()
        {
            var topic = new HelpTopic
            {
                Title = "AI Features",
                Icon = "🧠",
                Summary = "EditNovaFX includes powerful AI tools to help you save time — from auto-generating subtitles to writing YouTube descriptions.",
            };

            var publisher = new HelpTopic
            {
                Title = "AI Publisher",
                Icon = "📢",
                Summary = "The AI Publisher is a one-stop tool that helps you write a title, description, and tags for your video — then upload it to YouTube.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🔧", Title = "Open AI Publisher", Detail = "Click the '✨ AI Publisher' button on the right panel, or go to Tools → AI Publisher & Share." },
                    new() { Number = 2, Emoji = "✍️", Title = "Enter a brief topic", Detail = "Type a short description of your video (e.g., 'Cooking tutorial for beginners'). The AI will generate a professional title, description, and tags." },
                    new() { Number = 3, Emoji = "✅", Title = "Review and edit", Detail = "Look over the generated text. You can edit any part before publishing." },
                    new() { Number = 4, Emoji = "📹", Title = "Connect your YouTube account", Detail = "Click 'Login with YouTube'. A browser window opens — sign in to your Google account and grant permission." },
                    new() { Number = 5, Emoji = "🚀", Title = "Upload", Detail = "Click 'Publish to YouTube'. EditNovaFX will export your video and upload it directly. A progress bar shows the status." },
                },
                Tips = new()
                {
                    "You only need to log in to YouTube once. Your credentials are saved securely.",
                    "Set the video visibility to 'Private' first so you can review it on YouTube before making it public."
                },
                Warnings = new() { "YouTube upload requires an active internet connection." }
            };

            topic.SubTopics.Add(publisher);
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 5. EXPORT & PUBLISH
        // ────────────────────────────────────────────────────────────────────
        private void BuildExportPublish()
        {
            var topic = new HelpTopic
            {
                Title = "Export & Publish",
                Icon = "📤",
                Summary = "When your edit is complete, Export lets you save your final video as a file on your computer. Publish lets you upload directly to YouTube.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "⚙️", Title = "Set Output Resolution", Detail = "In the Properties panel (right), select your desired resolution: 720p, 1080p, 1440p, or 4K. 1080p is recommended for most uses." },
                    new() { Number = 2, Emoji = "🎞️", Title = "Set Frame Rate", Detail = "Choose 24fps for a cinematic look, 30fps for standard video, or 60fps for very smooth motion." },
                    new() { Number = 3, Emoji = "📤", Title = "Click Export Video", Detail = "Click the 'Export Video' button at the bottom of the Properties panel. A save dialog will appear." },
                    new() { Number = 4, Emoji = "📂", Title = "Choose output location", Detail = "Browse to the folder where you want to save the file. Give it a name and click Save." },
                    new() { Number = 5, Emoji = "⏳", Title = "Wait for rendering", Detail = "A progress overlay appears showing the export progress. Do not close the app until it completes." },
                    new() { Number = 6, Emoji = "✅", Title = "Done!", Detail = "The exported MP4 file is saved to your chosen folder. You can now share or upload it anywhere." },
                },
                Tips = new()
                {
                    "Export creates an MP4 file which works on all platforms: YouTube, Instagram, WhatsApp, etc.",
                    "Make sure you have at least 2× your project's expected file size available as free disk space for rendering.",
                },
                Warnings = new()
                {
                    "Do not turn off your computer or put it to sleep during export.",
                    "If FFmpeg is not set up, export will fail. Go to File → Settings to configure it first."
                }
            };

            var youtube = new HelpTopic
            {
                Title = "YouTube Upload",
                Icon = "📹",
                Summary = "EditNovaFX can export and upload your video directly to your YouTube channel. You'll need your own Google API credentials.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🌐", Title = "Create a Google Cloud Project", Detail = "Go to https://console.cloud.google.com/ and create a new project (or use an existing one)." },
                    new() { Number = 2, Emoji = "📺", Title = "Enable YouTube Data API v3", Detail = "In the Google Cloud Console, go to APIs & Services → Library. Search for 'YouTube Data API v3' and click Enable." },
                    new() { Number = 3, Emoji = "🔑", Title = "Create OAuth 2.0 Credentials", Detail = "Go to APIs & Services → Credentials → Create Credentials → OAuth Client ID. Choose 'Desktop app' as the application type. Copy the Client ID and Client Secret." },
                    new() { Number = 4, Emoji = "⚙️", Title = "Enter Credentials in EditNovaFX", Detail = "In EditNovaFX, go to Tools → Setup YouTube API. Paste your Client ID and Client Secret, then click Save." },
                    new() { Number = 5, Emoji = "🔧", Title = "Open AI Publisher", Detail = "Click the '✨ AI Publisher' button. This is where you manage YouTube uploads." },
                    new() { Number = 6, Emoji = "🔐", Title = "Login to Google", Detail = "Click 'Login with YouTube'. A browser opens. Sign in to your Google account and click Allow." },
                    new() { Number = 7, Emoji = "✍️", Title = "Fill in video details", Detail = "Add a title, description, tags, and set the visibility (Public, Private, Unlisted)." },
                    new() { Number = 8, Emoji = "🚀", Title = "Click Publish", Detail = "EditNovaFX exports and uploads the video automatically. A progress bar shows the upload status." },
                },
                Tips = new()
                {
                    "Start with 'Private' so you can review the video on YouTube before publishing publicly.",
                    "Your credentials are stored securely in %AppData%/EditNovaFX/youtube_secrets.json. You only need to set this up once."
                },
                Warnings = new()
                {
                    "Uploading large videos (over 1 GB) may take many minutes depending on your internet speed.",
                    "You must create your own Google Cloud credentials. No default credentials are included with the app."
                }
            };

            topic.SubTopics.Add(youtube);
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 6. SCRIPTURE SUBTITLES
        // ────────────────────────────────────────────────────────────────────
        private void BuildScriptureSubtitles()
        {
            var topic = new HelpTopic
            {
                Title = "Scripture Subtitles",
                Icon = "📖",
                Summary = "EditNovaFX includes specialized tools for creating Bible video subtitles using USFM files and timing data.",
            };

            var generator = new HelpTopic
            {
                Title = "Scripture Subtitle Generator",
                Icon = "⚙️",
                Summary = "Automatically generate synchronized Bible subtitles from USFM and Timing files.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "📑", Title = "Upload USFM File", Detail = "Select your .SFM or .USFM file containing the Bible text." },
                    new() { Number = 2, Emoji = "⏱️", Title = "Upload Timing File", Detail = "Upload the corresponding timing file (e.g., from HearThis or aeneas)." },
                    new() { Number = 3, Emoji = "🔍", Title = "Select Book & Chapter", Detail = "Ensure the dropdown selections match the uploaded files. The app validates this for accuracy." },
                    new() { Number = 4, Emoji = "⚡", Title = "Choose Generation Mode", Detail = "Select Verse (full text), Segment (word split), or Phrase (natural split)." },
                    new() { Number = 5, Emoji = "▶️", Title = "Preview Subtitles", Detail = "Click 'Preview Subtitles' to generate and see the results instantly in the preview window." },
                },
                Tips = new()
                {
                    "Verse Mode is best for simple full-screen text.",
                    "Phrase Mode uses an external AI tool for natural language splitting.",
                    "Segment Mode is great for fast-paced videos with word-by-word highlights."
                }
            };

            var converter = new HelpTopic
            {
                Title = "Subtitle Converter",
                Icon = "🔄",
                Summary = "Access the standalone USFM-to-SRT converter tool directly from the app.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🛠️", Title = "Open Converter", Detail = "Go to Tools → Subtitle Converter to launch the external tool." },
                    new() { Number = 2, Emoji = "💻", Title = "Advanced Processing", Detail = "Use this for batch processing or advanced phrase splitting via the command-line interface." }
                }
            };

            var parsingProcess = new HelpTopic
            {
                Title = "Subtitle Parsing Process",
                Icon = "⚙️",
                Summary = "Understand the technical workflow behind transforming Bible scripture into synchronized subtitles.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "📖", Title = "USFM Analysis", Detail = "The parser scans the USFM file for markers like \\v (verse), \\c (chapter), and \\p (paragraph) to extract the raw text structure." },
                    new() { Number = 2, Emoji = "⏱️", Title = "Timecode Matching", Detail = "It then reads the timing file (text/label format) and aligns each verse text with its corresponding start and end timestamps." },
                    new() { Number = 3, Emoji = "✂️", Title = "Text Segmentation", Detail = "Depending on the 'Mode' selected, the text is either kept as full verses or split into smaller phrases or individual words for better readability." },
                    new() { Number = 4, Emoji = "📤", Title = "SRT/timeline Construction", Detail = "The synced data is converted into the industry-standard SubRip (SRT) format and injected as a new track on the EditNovaFX timeline." },
                },
                Tips = new()
                {
                    "Ensure your USFM file and Timing file correspond to the exact same chapter for accurate alignment.",
                    "The Subtitle Parser tool runs as an external high-performance process for maximum reliability."
                }
            };

            topic.SubTopics.Add(generator);
            topic.SubTopics.Add(converter);
            topic.SubTopics.Add(parsingProcess);
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 7. SUPPORT
        // ────────────────────────────────────────────────────────────────────
        private void BuildSupport()
        {
            var topic = new HelpTopic
            {
                Title = "Support",
                Icon = "📧",
                Summary = "Need help or want to report a bug? Contact the NLCI Lab team directly.",
                Content = "For technical support, feature requests, or bug reports, please contact:\n\n" +
                          "• Jacob Thomas: jacob_thomas@nlife.in\n" +
                          "• Benjamin Varghese: benjamin_varghese@nlife.in\n\n" +
                          "Please include your project details and a description of the issue."
            };
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 8. KEYBOARD SHORTCUTS
        // ────────────────────────────────────────────────────────────────────
        private void BuildKeyboardShortcuts()
        {
            var topic = new HelpTopic
            {
                Title = "Keyboard Shortcuts",
                Icon = "⌨️",
                Summary = "Learn these shortcuts to edit twice as fast. You don't need to memorise all of them — start with Space, Ctrl+Z, and Ctrl+S.",
                Shortcuts = new()
                {
                    new() { Keys = "Space", Action = "Play / Pause preview" },
                    new() { Keys = "Ctrl + N", Action = "New Project" },
                    new() { Keys = "Ctrl + O", Action = "Open Project" },
                    new() { Keys = "Ctrl + S", Action = "Save Project" },
                    new() { Keys = "Ctrl + I", Action = "Import Media" },
                    new() { Keys = "Ctrl + Z", Action = "Undo last action" },
                    new() { Keys = "Ctrl + Y", Action = "Redo last undone action" },
                    new() { Keys = "Ctrl + E", Action = "Export Video" },
                    new() { Keys = "Delete", Action = "Delete selected clip" },
                    new() { Keys = "F11", Action = "Toggle Full Screen preview" },
                    new() { Keys = "Escape", Action = "Exit Full Screen" },
                    new() { Keys = "Ctrl + Shift + S", Action = "Split clip at playhead" },
                },
                Tips = new() { "You can hover over any button in the app to see a tooltip describing what it does." }
            };

            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 7. SETTING UP EXTERNAL TOOLS
        // ────────────────────────────────────────────────────────────────────
        private void BuildExternalTools()
        {
            var topic = new HelpTopic
            {
                Title = "Setting Up External Tools",
                Icon = "⚙️",
                Summary = "EditNovaFX relies on three external tools to process media. You must download these and link them in the Settings window.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "🎬", Title = "FFmpeg (Video Export)", Detail = "Required for exporting videos and processing audio. Download FFmpeg and extract it. You need 'ffmpeg.exe' and 'ffprobe.exe'. Place them in a folder named 'ffmpeg' next to the EditNovaFX executable." },
                    new() { Number = 2, Emoji = "🤖", Title = "Whisper AI (Auto Subtitles)", Detail = "Required for generating subtitles from audio. Download 'whisper.exe'. Place it in a folder named 'whisper-bin-x64'." },
                    new() { Number = 3, Emoji = "📝", Title = "Subtitle Parser (Scripture Subtitles)", Detail = "Required for processing USFM Bible files into SRT. Download 'usfm-to-srt-v0.1.1-alpha.exe' from https://github.com/nlci-lab/EditNovaFX-SRT-Generator/releases/tag/v0.1.1-alpha. Place it in a folder named 'subtitle-parser'." },
                    new() { Number = 4, Emoji = "🔗", Title = "Link in Settings", Detail = "Go to File → Settings in EditNovaFX. Click 'Browse' next to each tool and select the executable file you downloaded. Click Save." }
                },
                Tips = new()
                {
                    "If you place the tools in their default folders (e.g., 'ffmpeg/' next to the app), EditNovaFX will find them automatically.",
                    "You only need to set these up once."
                }
            };
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 8. TROUBLESHOOTING
        // ────────────────────────────────────────────────────────────────────
        private void BuildTroubleshooting()
        {
            var topic = new HelpTopic
            {
                Title = "Troubleshooting",
                Icon = "🛠️",
                Summary = "Something not working as expected? Check the common problems below. Most issues have a simple fix.",
                Faqs = new()
                {
                    new() { Question = "Export failed — what should I do?", Answer = "1. Check that FFmpeg is set up: go to File → Settings and set the correct FFmpeg path.\n2. Ensure your hard drive has enough free space (at least 2 GB).\n3. Make sure you have at least one clip on the timeline before exporting." },
                    new() { Question = "The AI subtitle generator produced no text.", Answer = "This usually means the Whisper AI model file is missing or corrupt.\n1. Open the Audio to Subtitle tool and check the model status message.\n2. Try selecting the 'small' model instead of 'medium'.\n3. Make sure there is actual speech in your audio (not just music)." },
                    new() { Question = "My video preview is blank / black screen.", Answer = "1. Make sure you clicked on a clip in the timeline or Media Library to load it into the preview.\n2. Try resizing the preview panel.\n3. Check that your video file isn't corrupt by playing it in another media player." },
                    new() { Question = "YouTube login fails or shows an error.", Answer = "1. Check your internet connection.\n2. Ensure your Google Client ID and Secret are correctly entered (Tools → Setup YouTube API).\n3. Try clicking 'Logout' then 'Login with YouTube' again." },
                    new() { Question = "The app is very slow or uses high CPU.", Answer = "1. Close other heavy applications (browsers, games) while editing.\n2. Avoid using multiple 4K clips at once during preview.\n3. If using AI subtitle generation, it requires significant CPU — this is normal for a few minutes." },
                    new() { Question = "A clip is missing from the timeline after I reopen my project.", Answer = "This can happen if the original file was moved, renamed, or deleted. Move the file back to its original location and reopen the project." },
                }
            };

            var exportFailed = new HelpTopic
            {
                Title = "Export Failed",
                Icon = "❌",
                Summary = "Export failures are almost always caused by one of three things: missing FFmpeg, not enough disk space, or an empty timeline.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "⚙️", Title = "Set up FFmpeg", Detail = "Go to File → Settings. Point the path to ffmpeg.exe on your computer. FFmpeg is required for all exports." },
                    new() { Number = 2, Emoji = "💾", Title = "Check disk space", Detail = "Make sure your output drive has at least 2 GB free for a standard 10-minute video." },
                    new() { Number = 3, Emoji = "🎞️", Title = "Check the timeline", Detail = "You must have at least one video clip on the timeline before exporting." },
                    new() { Number = 4, Emoji = "🔁", Title = "Try again", Detail = "After fixing the above, click Export Video again." },
                },
                Warnings = new() { "If you see 'Access Denied' during export, run EditNovaFX as Administrator." }
            };

            var aiSlow = new HelpTopic
            {
                Title = "AI Subtitle is Slow",
                Icon = "🐢",
                Summary = "The AI subtitle generator does heavy processing locally on your PC. Slow speed is often normal, but here's how to speed it up.",
                Steps = new()
                {
                    new() { Number = 1, Emoji = "📦", Title = "Use the smaller model", Detail = "In the Audio to Subtitle tool, select the 'small' model instead of 'medium' or 'large'. Small is 5× faster with slightly less accuracy." },
                    new() { Number = 2, Emoji = "🔕", Title = "Close other apps", Detail = "Close web browsers, games, and other memory-heavy programs to free up CPU and RAM." },
                    new() { Number = 3, Emoji = "⏱️", Title = "Be patient", Detail = "For a 10-minute video, allow 2–5 minutes for the small model, or 5–10 minutes for the medium model on an average PC." },
                }
            };

            topic.SubTopics.Add(exportFailed);
            topic.SubTopics.Add(aiSlow);
            Topics.Add(topic);
        }

        // ────────────────────────────────────────────────────────────────────
        // 9. ABOUT
        // ────────────────────────────────────────────────────────────────────
        private void BuildAbout()
        {
            var topic = new HelpTopic
            {
                Title = "About EditNovaFX",
                Icon = "ℹ️",
                Summary = "EditNovaFX is a powerful and user-friendly desktop video editing application designed for creators, educators, and media professionals.",
                Content = "The goal of EditNovaFX is to bring video editing, subtitle creation, and publishing tools into one unified platform, allowing users to create professional-quality videos faster and with less effort.\n\n" +
                          "EditNovaFX v1.1.0\n\n" +
                          "Created by NLCI Lab",
                Tips = new()
                {
                    "🤖 AI Features: Auto-generate professional subtitles and metadata.",
                    "🔒 Privacy First: All processing happens locally. Your media is never uploaded.",
                    "⚡ Modern Core: Built with .NET 8 and FFmpeg for maximum performance.",
                    "Keep your application updated for the latest feature releases and security fixes."
                }
            };
            Topics.Add(topic);
        }

        public void SelectTopicByTitle(string title)
        {
            foreach (var topic in Topics)
            {
                if (topic.Title.Contains(title, System.StringComparison.OrdinalIgnoreCase))
                {
                    SelectedTopic = topic;
                    return;
                }
                foreach (var sub in topic.SubTopics)
                {
                    if (sub.Title.Contains(title, System.StringComparison.OrdinalIgnoreCase))
                    {
                        SelectedTopic = sub;
                        return;
                    }
                }
            }
        }
    }
}

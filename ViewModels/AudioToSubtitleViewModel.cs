using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VideoEditor.Models;
using VideoEditor.Services;

namespace VideoEditor.ViewModels
{
    public partial class AudioToSubtitleViewModel : ObservableObject
    {
        private readonly FFmpegService _ffmpegService;
        private readonly WhisperService _whisperService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFile))]
        [NotifyPropertyChangedFor(nameof(FileSizeFriendly))]
        private string _audioFilePath = string.Empty;

        [ObservableProperty]
        private string _fileDuration = "--:--";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FileSizeFriendly))]
        private long _fileSize;

        [ObservableProperty]
        private string _selectedLanguage = "Hindi";

        [ObservableProperty]
        private string _selectedFormat = "SubRip (.srt)";

        [ObservableProperty]
        private int _progressValue;

        [ObservableProperty]
        private string _statusMessage = "Ready to start";

        [ObservableProperty]
        private bool _isNotBusy = true;

        [ObservableProperty]
        private string _apiKey = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowModelBanner))]
        private bool _useLocalAI = true;

        [ObservableProperty]
        private string _whisperExePath = "whisper.exe";

        [ObservableProperty]
        private string _whisperModelPath = "models/ggml-base.bin";

        // ── Model-missing banner state ──────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowModelBanner))]
        private bool _isModelMissing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowModelBanner))]
        private bool _isWhisperExeMissing;

        /// <summary>True when either the exe or the model is absent and Local AI tab is active.</summary>
        public bool ShowModelBanner => (IsModelMissing || IsWhisperExeMissing) && UseLocalAI;

        [ObservableProperty]
        private string _modelInstallFolder = string.Empty;

        /// <summary>All known ggml models the user can choose to download.</summary>
        public List<WhisperModelInfo> AvailableModels { get; } = WhisperService.AvailableModels;

        public bool HasFile => !string.IsNullOrEmpty(AudioFilePath);

        public string ProgressText => IsNotBusy ? "" : $"{ProgressValue}%";

        public ObservableCollection<string> Languages { get; } = new ObservableCollection<string>
        {
            "Hindi", "Bengali", "Telugu", "Marathi", "Tamil", "Urdu", "Gujarati", 
            "Kannada", "Malayalam", "Odia", "Punjabi", "Assamese", "Maithili", 
            "Santali", "Kashmiri", "Nepali", "Konkani", "Sindhi", "Dogri", 
            "Manipuri", "Sanskrit", "English"
        };

        public ObservableCollection<string> Formats { get; } = new ObservableCollection<string>
        {
            "SubRip (.srt)"
        };

        public string FileSizeFriendly
        {
            get
            {
                if (FileSize == 0) return "0 MB";
                double mb = FileSize / (1024.0 * 1024.0);
                return $"{mb:F1} MB";
            }
        }

        public AudioToSubtitleViewModel()
        {
            _ffmpegService = new FFmpegService();
            _whisperService = new WhisperService();

            // Check what is installed and populate banner state
            var status = _whisperService.GetStatus();
            IsWhisperExeMissing = !status.WhisperExeFound;
            IsModelMissing = !status.ModelFound;
            ModelInstallFolder = status.ExpectedModelFolder;

            if (status.WhisperExeFound)  WhisperExePath  = status.WhisperExePath;
            if (status.ModelFound)       WhisperModelPath = status.ModelPath;
        }

        [RelayCommand]
        private void Browse()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav;*.aac;*.m4a)|*.mp3;*.wav;*.aac;*.m4a|All Files (*.*)|*.*",
                Title = "Select Audio File"
            };

            if (dialog.ShowDialog() == true)
            {
                AudioFilePath = dialog.FileName;
                var fileInfo = new FileInfo(AudioFilePath);
                FileSize = fileInfo.Length;
                UpdateDuration();
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var window = new VideoEditor.Views.SettingsWindow();
            // Try to set owner if possible
            if (System.Windows.Application.Current.MainWindow != null)
            {
                window.Owner = System.Windows.Application.Current.MainWindow;
            }
            window.ShowDialog();
            
            // Re-check status after settings are closed
            var status = _whisperService.GetStatus();
            IsWhisperExeMissing = !status.WhisperExeFound;
            IsModelMissing = !status.ModelFound;
        }

        private async void UpdateDuration()
        {
            try
            {
                var info = await _ffmpegService.GetMediaInfo(AudioFilePath);
                if (info != null)
                {
                    FileDuration = info.Duration.ToString(@"mm\:ss");
                }
                else
                {
                    FileDuration = "Unknown";
                }
            }
            catch
            {
                FileDuration = "Unknown";
            }
        }

        [RelayCommand]
        private async Task Convert()
        {
            if (string.IsNullOrEmpty(AudioFilePath))
            {
                StatusMessage = "Please select an audio file first.";
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "SRT Subtitle (*.srt)|*.srt",
                FileName = Path.GetFileNameWithoutExtension(AudioFilePath),
                Title = "Save Subtitle File"
            };

            if (saveDialog.ShowDialog() != true) return;

            IsNotBusy = false;
            ProgressValue = 0;
            StatusMessage = "Processing audio...";

            try
            {
                string content = string.Empty;

                if (UseLocalAI)
                {
                    StatusMessage = "Running whisper.cpp local AI...";
                    ProgressValue = 10;
                    _whisperService.WhisperPath = WhisperExePath;
                    _whisperService.ModelPath = WhisperModelPath;
                    
                    // Wire up real-time feedback
                    Action<string> logHandler = (log) => { StatusMessage = log.Trim(); };
                    Action<int> progressHandler = (p) => { ProgressValue = Math.Max(ProgressValue, p); };
                    
                    _whisperService.LogReceived += logHandler;
                    _whisperService.ProgressChanged += progressHandler;

                    try
                    {
                        string langCode = GetLanguageCode(SelectedLanguage);
                        content = await _whisperService.TranscribeLocal(AudioFilePath, langCode, false);
                    }
                    finally
                    {
                        _whisperService.LogReceived -= logHandler;
                        _whisperService.ProgressChanged -= progressHandler;
                    }
                }
                else if (!string.IsNullOrEmpty(ApiKey))
                {
                    StatusMessage = "Connecting to OpenAI Cloud AI...";
                    ProgressValue = 10;
                    
                    string langCode = GetLanguageCode(SelectedLanguage);
                    // OpenAI supports 'srt' or 'vtt'. We'll stick to 'srt' as it's more standard.
                    content = await TranscribeWithOpenAI(AudioFilePath, langCode, "srt");
                }
                else
                {
                    StatusMessage = "Please provide an API Key or ensure Local AI is properly configured.";
                    System.Windows.MessageBox.Show("Transcription requires either a valid OpenAI API Key or a configured whisper.cpp environment.", "Configuration Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                var encoding = new System.Text.UTF8Encoding(true);
                await File.WriteAllTextAsync(saveDialog.FileName, content, encoding);

                StatusMessage = "Saved successfully";
                string msg = UseLocalAI ? "Local whisper.cpp transcription successful!" : "Cloud Transcription Successful!";
                
                System.Windows.MessageBox.Show(msg, "Conversion Finished", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "Conversion failed";
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Conversion Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsNotBusy = true;
                ProgressValue = 100;
            }
        }

        private string FormatSrtTime(TimeSpan ts) => ts.ToString(@"hh\:mm\:ss\,fff");

        private async Task<string> TranscribeWithOpenAI(string filePath, string language, string format)
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
            using var form = new System.Net.Http.MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new System.Net.Http.ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("audio/mpeg");
            form.Add(fileContent, "file", Path.GetFileName(filePath));
            form.Add(new System.Net.Http.StringContent("whisper-1"), "model");
            form.Add(new System.Net.Http.StringContent(format), "response_format");
            form.Add(new System.Net.Http.StringContent(language.ToLower()), "language");
            
            var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
            if (!response.IsSuccessStatusCode) 
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI Error ({response.StatusCode}): {error}");
            }
            return await response.Content.ReadAsStringAsync();
        }

        private string GetLanguageCode(string languageName)
        {
            var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Hindi", "hi" },
                { "Bengali", "bn" },
                { "Telugu", "te" },
                { "Marathi", "mr" },
                { "Tamil", "ta" },
                { "Urdu", "ur" },
                { "Gujarati", "gu" },
                { "Kannada", "kn" },
                { "Malayalam", "ml" },
                { "Odia", "or" },
                { "Punjabi", "pa" },
                { "Assamese", "as" },
                { "Maithili", "mai" },
                { "Nepali", "ne" },
                { "Sanskrit", "sa" },
                { "English", "en" },
                { "Konkani", "kok" },
                { "Sindhi", "sd" },
                { "Dogri", "doi" },
                { "Manipuri", "mni" },
                { "Kashmiri", "ks" },
                { "Santali", "sat" }
            };

            return map.TryGetValue(languageName, out string? code) ? code : "hi"; // Default to Hindi if not found
        }
    }
}

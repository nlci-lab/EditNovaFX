using System;
using System.IO;
using System.Text.Json;

namespace VideoEditor.Services
{
    public class AppSettings
    {
        public string FFmpegPath { get; set; } = string.Empty;
        public string FFprobePath { get; set; } = string.Empty;
        public string WhisperExePath { get; set; } = string.Empty;
        public string SubtitleParserPath { get; set; } = string.Empty;

        public void SetDefaultsIfEmpty()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (string.IsNullOrWhiteSpace(FFmpegPath))
                FFmpegPath = Path.Combine(baseDir, "ffmpeg", "ffmpeg.exe");

            if (string.IsNullOrWhiteSpace(FFprobePath))
                FFprobePath = Path.Combine(baseDir, "ffmpeg", "ffprobe.exe");

            if (string.IsNullOrWhiteSpace(WhisperExePath))
                WhisperExePath = Path.Combine(baseDir, "whisper-bin-x64", "whisper.exe");

            if (string.IsNullOrWhiteSpace(SubtitleParserPath))
                SubtitleParserPath = Path.Combine(baseDir, "subtitle-parser", "usfm-to-srt-v0.1.1-alpha.exe");
        }
    }

    public class SettingsService
    {
        private static readonly Lazy<SettingsService> _instance = new Lazy<SettingsService>(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;

        private readonly string _settingsFilePath;

        public AppSettings CurrentSettings { get; private set; }

        private SettingsService()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _settingsFilePath = Path.Combine(baseDir, "settings.json");
            CurrentSettings = LoadPlatformSettings();
        }

        private AppSettings LoadPlatformSettings()
        {
            AppSettings settings;
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    settings = new AppSettings();
                }
            }
            else
            {
                settings = new AppSettings();
            }

            // Ensure defaults are populated if fields are empty
            settings.SetDefaultsIfEmpty();
            return settings;
        }

        public void SaveSettings()
        {
            try
            {
                CurrentSettings.SetDefaultsIfEmpty();
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}

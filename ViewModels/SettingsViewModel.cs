using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using VideoEditor.Services;

namespace VideoEditor.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _ffmpegPath = string.Empty;

        [ObservableProperty]
        private string _ffprobePath = string.Empty;

        [ObservableProperty]
        private string _whisperExePath = string.Empty;

        [ObservableProperty]
        private string _subtitleParserPath = string.Empty;

        public SettingsViewModel()
        {
            var settings = SettingsService.Instance.CurrentSettings;
            FfmpegPath = settings.FFmpegPath;
            FfprobePath = settings.FFprobePath;
            WhisperExePath = settings.WhisperExePath;
            SubtitleParserPath = settings.SubtitleParserPath;
        }

        [RelayCommand]
        private void BrowseFFmpeg()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ffmpeg.exe|ffmpeg.exe|All Executables (*.exe)|*.exe",
                Title = "Select ffmpeg.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                FfmpegPath = dialog.FileName;
                
                // Try to auto-resolve ffprobe
                string? dir = Path.GetDirectoryName(FfmpegPath);
                if (dir != null)
                {
                    string possibleProbe = Path.Combine(dir, "ffprobe.exe");
                    if (File.Exists(possibleProbe) && string.IsNullOrEmpty(FfprobePath))
                    {
                        FfprobePath = possibleProbe;
                    }
                }
            }
        }

        [RelayCommand]
        private void BrowseFFprobe()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ffprobe.exe|ffprobe.exe|All Executables (*.exe)|*.exe",
                Title = "Select ffprobe.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                FfprobePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseWhisper()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Whisper Executable (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select whisper.cpp Executable"
            };
            if (dialog.ShowDialog() == true) WhisperExePath = dialog.FileName;
        }

        [RelayCommand]
        private void BrowseSubtitleParser()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Subtitle Parser (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Subtitle Parser Executable"
            };
            if (dialog.ShowDialog() == true) SubtitleParserPath = dialog.FileName;
        }

        [RelayCommand]
        private void SaveSettings(System.Windows.Window window)
        {
            var settings = SettingsService.Instance.CurrentSettings;
            settings.FFmpegPath = FfmpegPath;
            settings.FFprobePath = FfprobePath;
            settings.WhisperExePath = WhisperExePath;
            settings.SubtitleParserPath = SubtitleParserPath;
            
            SettingsService.Instance.SaveSettings();
            
            window.DialogResult = true;
            window.Close();
        }

        [RelayCommand]
        private void Cancel(System.Windows.Window window)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}

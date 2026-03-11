using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VideoEditor.Models;

namespace VideoEditor.ViewModels
{
    public partial class ExportViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _targetFileName = "ExportedVideo.mp4";

        [ObservableProperty]
        private string _targetDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        [ObservableProperty]
        private ObservableCollection<ExportPreset> _presets;

        [ObservableProperty]
        private ExportPreset _selectedPreset;

        [ObservableProperty]
        private int _customWidth = 1920;

        [ObservableProperty]
        private int _customHeight = 1080;

        [ObservableProperty]
        private bool _isCustomSize;

        public string FullOutputPath => Path.Combine(TargetDirectory, TargetFileName);

        public ExportViewModel(Project currentProject)
        {
            Presets = new ObservableCollection<ExportPreset>(ExportPreset.GetDefaultPresets());
            SelectedPreset = Presets.First();
            
            TargetFileName = currentProject.Name + ".mp4";
            CustomWidth = currentProject.OutputWidth;
            CustomHeight = currentProject.OutputHeight;
        }

        [RelayCommand]
        private void BrowseLocation()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "MP4 Video (*.mp4)|*.mp4|All Files (*.*)|*.*",
                InitialDirectory = TargetDirectory,
                FileName = TargetFileName
            };

            if (dialog.ShowDialog() == true)
            {
                TargetDirectory = Path.GetDirectoryName(dialog.FileName) ?? TargetDirectory;
                TargetFileName = Path.GetFileName(dialog.FileName);
            }
        }

        public ExportSettings GetSettings()
        {
            return new ExportSettings
            {
                OutputPath = FullOutputPath,
                Width = IsCustomSize ? CustomWidth : SelectedPreset.Width,
                Height = IsCustomSize ? CustomHeight : SelectedPreset.Height,
                FrameRate = SelectedPreset.FrameRate,
                VideoBitrate = SelectedPreset.VideoBitrate,
                AudioBitrate = SelectedPreset.AudioBitrate,
                Format = "mp4"
            };
        }
    }
}

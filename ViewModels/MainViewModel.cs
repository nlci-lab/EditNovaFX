using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoEditor.Models;
using VideoEditor.Services;
using Microsoft.Win32;
using System.Linq;
using System.Threading.Tasks;
using VideoEditor.Views;

namespace VideoEditor.ViewModels
{
    public partial class SubtitlePreviewItem : ObservableObject
    {
        [ObservableProperty] private string _text = string.Empty;
        [ObservableProperty] private System.Windows.Thickness _margin;
        [ObservableProperty] private string _color = "#FFFFFF";
        [ObservableProperty] private string _font = "Nirmala UI";
        [ObservableProperty] private int _fontSize = 28;
        [ObservableProperty] private System.Windows.TextAlignment _textAlignment;
        [ObservableProperty] private System.Windows.VerticalAlignment _verticalAlignment;
        [ObservableProperty] private double _width;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isBold;
        [ObservableProperty] private bool _isItalic;
        [ObservableProperty] private double _outlineWidth;
        [ObservableProperty] private double _shadowWidth;
        [ObservableProperty] private string _shadowColor = "#000000";
        [ObservableProperty] private bool _hasBackgroundBox;
        [ObservableProperty] private double _backgroundBoxOpacity;

        partial void OnHasBackgroundBoxChanged(bool value)
        {
            OnPropertyChanged(nameof(BackgroundBrush));
            OnPropertyChanged(nameof(BackgroundPadding));
        }

        partial void OnBackgroundBoxOpacityChanged(double value)
        {
            OnPropertyChanged(nameof(BackgroundBrush));
        }

        public System.Windows.Media.Brush BackgroundBrush =>
            HasBackgroundBox
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        (byte)(BackgroundBoxOpacity * 255), 0, 0, 0))
                : System.Windows.Media.Brushes.Transparent;

        public System.Windows.Thickness BackgroundPadding =>
            HasBackgroundBox ? new System.Windows.Thickness(8, 4, 8, 4) : new System.Windows.Thickness(0);

        public SubtitleTrack? Track { get; set; }
    }

    /// <summary>
    /// Main ViewModel for the application
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly ProjectService _projectService;
        private readonly FFmpegService _ffmpegService;
        private readonly UndoRedoService _undoRedoService;

        [ObservableProperty]
        private Project _currentProject;

        [ObservableProperty]
        private ObservableCollection<MediaItem> _mediaLibrary;

        public IEnumerable<MediaItem> VideoItems => MediaLibrary.Where(m => m.Type == MediaType.Video);
        public IEnumerable<MediaItem> AudioItems => MediaLibrary.Where(m => m.Type == MediaType.Audio);
        public IEnumerable<MediaItem> ImageItems => MediaLibrary.Where(m => m.Type == MediaType.Image);

        private void NotifyMediaChanged()
        {
            OnPropertyChanged(nameof(VideoItems));
            OnPropertyChanged(nameof(AudioItems));
            OnPropertyChanged(nameof(ImageItems));
        }

        partial void OnMediaLibraryChanged(ObservableCollection<MediaItem> value)
        {
            if (value != null)
            {
                value.CollectionChanged += (s, e) => NotifyMediaChanged();
                NotifyMediaChanged();
            }
        }

        [ObservableProperty]
        private ObservableCollection<TimelineTrack> _timelineTracks;

        [ObservableProperty]
        private ObservableCollection<SubtitleTrack> _subtitleTracks;

        [ObservableProperty]
        private TimeSpan _playheadPosition;

        partial void OnPlayheadPositionChanged(TimeSpan value) => UpdateSubtitles();

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private int _renderingProgress;

        [ObservableProperty]
        private bool _isRendering;

        [ObservableProperty]
        private MediaItem? _selectedMediaItem;

        public ObservableCollection<string> Fonts { get; } = new ObservableCollection<string> { "Nirmala UI", "Mangal", "Arial", "Verdana", "Times New Roman", "Courier New", "Georgia", "Impact", "Segoe UI" };
        public ObservableCollection<int> FontSizes { get; } = new ObservableCollection<int> { 12, 14, 16, 18, 24, 28, 32, 36, 48, 60, 72, 84, 96, 110, 128, 144, 160, 180, 200 };
        public ObservableCollection<string> FontColors { get; } = new ObservableCollection<string> { "#FFFFFF", "#FFFF00", "#FF0000", "#00FF00", "#0000FF", "#FF00FF", "#00FFFF", "#000000" };
        public ObservableCollection<string> Alignments { get; } = new ObservableCollection<string> { "Top Left", "Top Center", "Top Right", "Middle Left", "Middle Center", "Middle Right", "Bottom Left", "Bottom Center", "Bottom Right" };

        [ObservableProperty]
        private ObservableCollection<SubtitlePreviewItem> _activeSubtitlePreviews = new ObservableCollection<SubtitlePreviewItem>();

        [ObservableProperty]
        private SubtitleTrack? _selectedSubtitleTrack;

        [ObservableProperty]
        private bool _isFontBold = true;

        [ObservableProperty]
        private bool _isFontItalic = false;

        [ObservableProperty]
        private double _zoomLevel = 10.0; // Pixels per second

        partial void OnZoomLevelChanged(double value) => RefreshTimeline();

        [ObservableProperty]
        private double _outlineWidth = 2.0;

        [ObservableProperty]
        private double _shadowWidth = 1.0;

        [ObservableProperty]
        private string _selectedShadowColor = "#000000";

        [ObservableProperty]
        private bool _hasBackgroundBox = false;

        [ObservableProperty]
        private double _backgroundBoxOpacity = 0.5;

        public MainViewModel()
        {
            _projectService = new ProjectService();
            _ffmpegService = new FFmpegService();
            _undoRedoService = new UndoRedoService();
            
            CurrentProject = new Project();
            MediaLibrary = new ObservableCollection<MediaItem>(CurrentProject.MediaItems);
            TimelineTracks = new ObservableCollection<TimelineTrack>(CurrentProject.Tracks);
            SubtitleTracks = new ObservableCollection<SubtitleTrack>(CurrentProject.SubtitleTracks);
            
            LogoMargin = new System.Windows.Thickness(CurrentProject.LogoX, CurrentProject.LogoY, 0, 0);
            StatusMessage = "Ready";

            _undoRedoService.SaveState(_currentProject);
        }

        [RelayCommand]
        private void NewProject()
        {
            CurrentProject = new Project();
            MediaLibrary = new ObservableCollection<MediaItem>(CurrentProject.MediaItems);
            TimelineTracks = new ObservableCollection<TimelineTrack>(CurrentProject.Tracks);
            SubtitleTracks = new ObservableCollection<SubtitleTrack>(CurrentProject.SubtitleTracks);
            
            // Clear volatile UI state
            SelectedMediaItem = null;
            SelectedClip = null;
            SelectedSubtitleTrack = null;
            ActiveSubtitlePreviews.Clear();
            PlayheadPosition = TimeSpan.Zero;
            IsPlaying = false;
            
            LogoPath = null;
            LogoScale = 1.0;
            LogoMargin = new System.Windows.Thickness(0);
            
            StatusMessage = "New project created";

            _undoRedoService.Clear();
            _undoRedoService.SaveState(CurrentProject);
            
            // Force UI refreshes
            UpdateSubtitles();
            RefreshTimeline();
            
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void SaveState()
        {
            _undoRedoService.SaveState(CurrentProject);
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        public bool CanUndo => _undoRedoService.CanUndo;
        public bool CanRedo => _undoRedoService.CanRedo;

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            var project = _undoRedoService.Undo(CurrentProject);
            if (project != null)
            {
                ApplyProjectState(project);
                StatusMessage = "Undo successful";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            var project = _undoRedoService.Redo();
            if (project != null)
            {
                ApplyProjectState(project);
                StatusMessage = "Redo successful";
            }
        }

        private void ApplyProjectState(Project project)
        {
            // Use current projekt's filepath if loading from history
            if (string.IsNullOrEmpty(project.FilePath))
                project.FilePath = CurrentProject.FilePath;

            CurrentProject = project;
            MediaLibrary = new ObservableCollection<MediaItem>(CurrentProject.MediaItems);
            TimelineTracks = new ObservableCollection<TimelineTrack>(CurrentProject.Tracks);
            SubtitleTracks = new ObservableCollection<SubtitleTrack>(CurrentProject.SubtitleTracks);
            
            LogoPath = CurrentProject.LogoPath;
            LogoScale = CurrentProject.LogoScale;
            LogoMargin = new System.Windows.Thickness(CurrentProject.LogoX, CurrentProject.LogoY, 0, 0);

            UpdateSubtitles();
            RefreshTimeline();
            
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        [RelayCommand]
        public void CommitChange() => SaveState();

        [RelayCommand]
        private async Task OpenProject()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "EditNova Project (*.veproj)|*.veproj|All Files (*.*)|*.*",
                Title = "Open Project"
            };

            if (dialog.ShowDialog() == true)
            {
                var project = await _projectService.LoadProject(dialog.FileName);
                if (project != null)
                {
                    CurrentProject = project;
                    MediaLibrary = new ObservableCollection<MediaItem>(CurrentProject.MediaItems);
                    TimelineTracks = new ObservableCollection<TimelineTrack>(CurrentProject.Tracks);
                    SubtitleTracks = new ObservableCollection<SubtitleTrack>(CurrentProject.SubtitleTracks);
                    if (SubtitleTracks.Count > 0) SelectedSubtitleTrack = SubtitleTracks[0];
                    LogoPath = CurrentProject.LogoPath;
                    LogoScale = CurrentProject.LogoScale;
                    LogoMargin = new System.Windows.Thickness(CurrentProject.LogoX, CurrentProject.LogoY, 0, 0);
                    StatusMessage = $"Opened: {project.Name}";
                }
            }
        }

        [RelayCommand]
        private async Task SaveProject()
        {
            if (string.IsNullOrEmpty(CurrentProject.FilePath))
            {
                await SaveProjectAs();
                return;
            }

            await _projectService.SaveProject(CurrentProject, CurrentProject.FilePath);
            StatusMessage = "Project saved";
        }

        [RelayCommand]
        private async Task SaveProjectAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "EditNova Project (*.veproj)|*.veproj|All Files (*.*)|*.*",
                Title = "Save Project As",
                FileName = CurrentProject.Name
            };

            if (dialog.ShowDialog() == true)
            {
                await _projectService.SaveProject(CurrentProject, dialog.FileName);
                StatusMessage = $"Saved: {dialog.FileName}";
            }
        }

        [RelayCommand]
        private async Task ImportMedia()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Media Files|*.mp4;*.avi;*.mkv;*.mov;*.mp3;*.wav;*.aac;*.jpg;*.png;*.bmp|" +
                         "Video Files|*.mp4;*.avi;*.mkv;*.mov|" +
                         "Audio Files|*.mp3;*.wav;*.aac|" +
                         "Image Files|*.jpg;*.png;*.bmp|" +
                         "All Files|*.*",
                Title = "Import Media",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    var mediaItem = await _ffmpegService.GetMediaInfo(file);
                    if (mediaItem != null)
                    {
                        CurrentProject.MediaItems.Add(mediaItem);
                        MediaLibrary.Add(mediaItem);
                    }
                }
                StatusMessage = $"Imported {dialog.FileNames.Length} file(s)";
                SaveState();
            }
        }

        [RelayCommand]
        private void ImportSubtitle()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Subtitle Files (*.srt)|*.srt|All Files (*.*)|*.*",
                Title = "Import Subtitle",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    try
                    {
                        var subtitleTrack = SubtitleParser.ParseSubtitleFile(file);
                        CurrentProject.SubtitleTracks.Add(subtitleTrack);
                        SubtitleTracks.Add(subtitleTrack);
                        SelectedSubtitleTrack = subtitleTrack;
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error loading subtitle: {ex.Message}";
                    }
                }
                StatusMessage = $"Imported {dialog.FileNames.Length} subtitle(s)";
                UpdateSubtitles(); // Refresh subtitle display
                SaveState();
            }
        }

        [RelayCommand]
        private void RemoveSubtitleTrack(SubtitleTrack? track)
        {
            if (track == null) track = SelectedSubtitleTrack;
            if (track == null) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to remove the subtitle track '{track.Name}'?", "Confirm Removal", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                CurrentProject.SubtitleTracks.Remove(track);
                SubtitleTracks.Remove(track);
                if (SelectedSubtitleTrack == track) SelectedSubtitleTrack = SubtitleTracks.FirstOrDefault();
                UpdateSubtitles();
                StatusMessage = $"Removed subtitle track: {track.Name}";
                SaveState();
            }
        }

        [RelayCommand]
        private void OpenAudioToSubtitle()
        {
            var window = new AudioToSubtitleWindow();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.Show();
        }

        [RelayCommand]
        private void OpenScriptureSubtitleGenerator()
        {
            var window = new ScriptureSubtitleWindow();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.Show();
        }

        [RelayCommand]
        private void OpenPublisher()
        {
            var window = new PublishingWindow(CurrentProject);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.Show();
        }

        [RelayCommand]
        private void OpenYouTubeSetup()
        {
            var window = new YouTubeSetupWindow();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        [RelayCommand]
        private void OpenSubtitleConverter()
        {
            try
            {
                // Make path relative to the application base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // If running from bin/Debug/net6.0-windows, we need to go up to the project root
                // In a deployed scenario, the 'Subtitle Parser' folder should be at the same level or reachable.
                // For development, we'll try to find it relative to the solution root.
                
                string exePath = System.IO.Path.Combine(baseDir, "Subtitle Parser", "usfm-to-srt-v0.1.1-alpha.exe");
                
                // Fallback for development (checking typical bin structure)
                if (!System.IO.File.Exists(exePath))
                {
                    string devPath = System.IO.Path.Combine(baseDir, "..", "..", "..", "Subtitle Parser", "usfm-to-srt-v0.1.1-alpha.exe");
                    if (System.IO.File.Exists(devPath)) exePath = devPath;
                }

                if (System.IO.File.Exists(exePath))
                {
                    string fullPath = System.IO.Path.GetFullPath(exePath);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(fullPath),
                        UseShellExecute = true
                    });
                    StatusMessage = "Opening Subtitle Converter...";
                }
                else
                {
                    StatusMessage = "Subtitle Converter executable not found.";
                    System.Windows.MessageBox.Show($"Could not find the executable. Checked:\n{exePath}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error opening Subtitle Converter";
                System.Windows.MessageBox.Show($"Failed to open Subtitle Converter:\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ExportVideo()
        {
            try
            {
                if (IsRendering) return;

                // Check FFmpeg availability
                if (!_ffmpegService.IsFFmpegAvailable())
                {
                    StatusMessage = "FFmpeg not found! Please use File -> Settings to configure paths.";
                    
                    var setupResult = System.Windows.MessageBox.Show(
                        "FFmpeg binaries (ffmpeg.exe and ffprobe.exe) were not found. Would you like to open Settings to set them up now?", 
                        "FFmpeg Not Found", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (setupResult == System.Windows.MessageBoxResult.Yes)
                    {
                        OpenSettings();
                    }
                    return;
                }

                // Validate project has content
                if (CurrentProject == null)
                {
                    System.Windows.MessageBox.Show("No project loaded!", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var enabledTracks = CurrentProject.Tracks.Where(t => t.IsEnabled && t.GetOrderedClips().Any(c => c.IsEnabled && c.MediaItem != null)).ToList();
                if (enabledTracks.Count == 0)
                {
                    System.Windows.MessageBox.Show("No enabled tracks with media clips found! Please add media to your timeline before exporting.", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[EXPORT] Starting export with {enabledTracks.Count} enabled tracks");
                System.Diagnostics.Debug.WriteLine($"[EXPORT] Project Duration: {CurrentProject.Duration}");

                var exportWin = new ExportWindow(CurrentProject);
                exportWin.Owner = System.Windows.Application.Current.MainWindow;

                if (exportWin.ShowDialog() == true)
                {
                    var settings = exportWin.ResultSettings;
                    if (settings == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[EXPORT] Export cancelled - no settings returned");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[EXPORT] Export Settings:");
                    System.Diagnostics.Debug.WriteLine($"  Output: {settings.OutputPath}");
                    System.Diagnostics.Debug.WriteLine($"  Resolution: {settings.Width}x{settings.Height}");
                    System.Diagnostics.Debug.WriteLine($"  Frame Rate: {settings.FrameRate}");
                    System.Diagnostics.Debug.WriteLine($"  Video Bitrate: {settings.VideoBitrate}k");
                    System.Diagnostics.Debug.WriteLine($"  Audio Bitrate: {settings.AudioBitrate}k");

                    StatusMessage = "Rendering video...";
                    RenderingProgress = 0;
                    IsRendering = true;
                    
                    var progress = new Progress<int>(percent =>
                    {
                        RenderingProgress = percent;
                        StatusMessage = $"Rendering: {percent}%";
                    });

                    System.Diagnostics.Debug.WriteLine("[EXPORT] Starting FFmpeg render...");
                    var result = await _ffmpegService.RenderVideo(CurrentProject, settings, progress);
                    
                    IsRendering = false;
                    
                    System.Diagnostics.Debug.WriteLine($"[EXPORT] Render completed. Success: {result.success}");
                    if (!result.success)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EXPORT] Error Message: {result.message}");
                    }
                    
                    StatusMessage = result.success ? "Export completed!" : $"Export failed: {result.message}";
                    
                    if (!result.success)
                    {
                        System.Windows.MessageBox.Show($"Export failed:\n\n{result.message}\n\nPlease check the Debug output for more details.", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    else
                    {
                        CurrentProject.LastExportedFilePath = settings.OutputPath; // Save for publishing
                        
                        var fileInfo = new System.IO.FileInfo(settings.OutputPath);
                        var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                        System.Windows.MessageBox.Show($"Export completed successfully!\n\nFile: {settings.OutputPath}\nSize: {fileSizeMB:F2} MB", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[EXPORT] Export cancelled by user");
                }
            }
            catch (Exception ex)
            {
                IsRendering = false;
                StatusMessage = "Export crashed";
                System.Diagnostics.Debug.WriteLine($"[EXPORT] CRITICAL ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[EXPORT] Stack Trace: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"Critical Error during Export:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void AddToTimeline(MediaItem? mediaItem)
        {
            if (mediaItem == null)
                mediaItem = SelectedMediaItem;

            if (mediaItem == null)
            {
                StatusMessage = "Please select a media item first";
                return;
            }

            // Find appropriate track
            TimelineTrack? targetTrack = null;
            
            // Intelligence: If it has dimensions, it's a visual item (Video/Image)
            bool isVisual = mediaItem.Type == MediaType.Video || 
                            mediaItem.Type == MediaType.Image || 
                            (mediaItem.Width > 0 && mediaItem.Height > 0);

            if (isVisual)
            {
                // Prefer an existing video track or a universal one
                targetTrack = TimelineTracks.FirstOrDefault(t => t.TrackType == MediaType.Video);
                if (targetTrack == null)
                    targetTrack = TimelineTracks.FirstOrDefault(t => t.TrackType == MediaType.Universal);
            }
            else if (mediaItem.Type == MediaType.Audio)
            {
                // Try to find an audio track, prefer empty ones
                targetTrack = TimelineTracks.FirstOrDefault(t => t.TrackType == MediaType.Audio && t.Clips.Count == 0);
                if (targetTrack == null)
                    targetTrack = TimelineTracks.FirstOrDefault(t => t.TrackType == MediaType.Audio);
                if (targetTrack == null)
                    targetTrack = TimelineTracks.FirstOrDefault(t => t.TrackType == MediaType.Universal);
            }

            if (targetTrack != null)
            {
                var clip = new TimelineClip
                {
                    MediaItemId = mediaItem.Id,
                    MediaItem = mediaItem,
                    Duration = mediaItem.Duration
                };

                // Assign type to universal track
                if (targetTrack.TrackType == MediaType.Universal)
                {
                    targetTrack.TrackType = isVisual ? MediaType.Video : MediaType.Audio;
                    if (targetTrack.Name.StartsWith("Track"))
                    {
                        string typePrefix = isVisual ? "Video/Image" : "Audio";
                        targetTrack.Name = $"{typePrefix} {targetTrack.Name.Substring(6).Trim()}";
                    }
                }

                targetTrack.AddClipSequentially(clip);
                RefreshTimeline();
                
                StatusMessage = $"Added {mediaItem.Name} to {targetTrack.Name} at {clip.StartTime.ToString(@"mm\:ss")}";
                SaveState();
            }
            else
            {
                // Auto-create a track if none found
                AddTrack(isVisual ? "Video" : "Audio");
                // Recurse once with the new track available
                AddToTimeline(mediaItem);
            }
        }

        [RelayCommand]
        private void Play()
        {
            IsPlaying = !IsPlaying;
            StatusMessage = IsPlaying ? "Playing" : "Paused";
        }


        [RelayCommand]
        private void Stop()
        {
            IsPlaying = false;
            PlayheadPosition = TimeSpan.Zero;
            StatusMessage = "Stopped";
        }

        [RelayCommand]
        private void AddTrack(string? trackType)
        {
            // Default to Video if not specified or invalid
            MediaType type = MediaType.Video;
            if (!string.IsNullOrEmpty(trackType) && Enum.TryParse<MediaType>(trackType, out var parsedType))
            {
                // Only allow Video or Audio, not Universal
                if (parsedType == MediaType.Video || parsedType == MediaType.Audio)
                {
                    type = parsedType;
                }
            }

            // Count existing tracks of the same type for better naming
            var trackCount = TimelineTracks.Count(t => t.TrackType == type) + 1;
            string typePrefix = type == MediaType.Video ? "Video/Image" : type.ToString();
            string name = $"{typePrefix} {trackCount}";
            var track = new TimelineTrack($"track_{Guid.NewGuid().ToString().Substring(0, 8)}", name, type);
            
            CurrentProject.Tracks.Add(track);
            TimelineTracks.Add(track);
            StatusMessage = $"Added {name}";
            SaveState();
        }

        [RelayCommand]
        private void RemoveTrack(TimelineTrack? track)
        {
            if (track == null)
            {
                StatusMessage = "No track selected to remove";
                return;
            }

            if (track.Clips.Count > 0)
            {
                var result = System.Windows.MessageBox.Show(
                    $"This track contains {track.Clips.Count} clip(s). Are you sure you want to delete it?",
                    "Confirm Track Removal",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            CurrentProject.Tracks.Remove(track);
            TimelineTracks.Remove(track);
            RefreshTimeline();
            StatusMessage = $"Removed track: {track.Name}";
            SaveState();
        }

        [RelayCommand]
        private void RemoveMediaItem(MediaItem? mediaItem)
        {
            if (mediaItem == null)
                mediaItem = SelectedMediaItem;

            if (mediaItem == null)
            {
                StatusMessage = "Please select a media item to remove";
                return;
            }

            // Remove from project (Media Library)
            CurrentProject.MediaItems.Remove(mediaItem);
            MediaLibrary.Remove(mediaItem);
            
            // Remove from Timeline Tracks
            bool clipsRemoved = false;
            foreach (var track in TimelineTracks)
            {
                // Find all clips referencing this media item
                var clipsToRemove = track.Clips.Where(c => c.MediaItemId == mediaItem.Id).ToList();
                
                if (clipsToRemove.Count > 0)
                {
                    foreach (var clip in clipsToRemove)
                    {
                        track.RemoveClip(clip, RippleEditingEnabled);
                    }
                    clipsRemoved = true;
                }
            }

            // Refresh Timeline UI if clips were removed
            if (clipsRemoved)
            {
                RefreshTimeline();
            }
            
            // Clear selection if this was the selected item
            if (SelectedMediaItem == mediaItem)
            {
                SelectedMediaItem = null;
            }

            StatusMessage = $"Removed {mediaItem.Name} from library" + (clipsRemoved ? " and timeline" : "");
            SaveState();
        }

        [RelayCommand]
        private void RemoveClip(TimelineClip? clip)
        {
            if (clip == null)
                clip = SelectedClip;

            if (clip == null)
            {
                StatusMessage = "Please select a clip to remove";
                return;
            }

            // Find the track containing this clip
            foreach (var track in TimelineTracks)
            {
                if (track.Clips.Contains(clip))
                {
                    track.RemoveClip(clip, RippleEditingEnabled);
                    RefreshTimeline(); // Force UI update
                    StatusMessage = $"Removed clip from {track.Name}";
                    SelectedClip = null;
                    SaveState();
                    return;
                }
            }
            StatusMessage = "Clip not found in any track";
        }



        // Ripple editing toggle
        [ObservableProperty]
        private bool _rippleEditingEnabled = true;

        [RelayCommand]
        private void ToggleRippleEditing()
        {
            RippleEditingEnabled = !RippleEditingEnabled;
            StatusMessage = $"Ripple editing: {(RippleEditingEnabled ? "ON" : "OFF")}";
        }

        [RelayCommand]
        private void MoveClipLeft(TimelineClip? clip)
        {
            if (clip == null)
            {
                StatusMessage = "Please select a clip to move";
                return;
            }

            var track = TimelineTracks.FirstOrDefault(t => t.Clips.Contains(clip));
            if (track != null && clip.Order > 0)
            {
                track.MoveClip(clip, clip.Order - 1, RippleEditingEnabled);
                RefreshTimeline();
                StatusMessage = $"Moved clip left to position {clip.Order}";
            }
        }

        [RelayCommand]
        private void MoveClipRight(TimelineClip? clip)
        {
            if (clip == null)
            {
                StatusMessage = "Please select a clip to move";
                return;
            }

            var track = TimelineTracks.FirstOrDefault(t => t.Clips.Contains(clip));
            if (track != null && clip.Order < track.Clips.Count - 1)
            {
                track.MoveClip(clip, clip.Order + 1, RippleEditingEnabled);
                RefreshTimeline();
                StatusMessage = $"Moved clip right to position {clip.Order}";
            }
        }

        [RelayCommand]
        private void SplitClip()
        {
            // 1. Identify track and clip to split
            TimelineClip? targetClip = null;
            TimelineTrack? targetTrack = null;

            // Priority: Split the selected clip if it's under the playhead
            if (SelectedClip != null && 
                PlayheadPosition >= SelectedClip.StartTime && 
                PlayheadPosition < SelectedClip.EndTime)
            {
                targetClip = SelectedClip;
                targetTrack = TimelineTracks.FirstOrDefault(t => t.Clips.Contains(targetClip));
            }

            // Fallback: Split the first clip found at playhead position
            if (targetClip == null)
            {
                foreach (var track in TimelineTracks)
                {
                    targetClip = track.Clips.FirstOrDefault(c => 
                        PlayheadPosition >= c.StartTime && 
                        PlayheadPosition < c.EndTime);
                    
                    if (targetClip != null)
                    {
                        targetTrack = track;
                        break;
                    }
                }
            }

            if (targetClip == null || targetTrack == null)
            {
                StatusMessage = "No clip found at playhead to split";
                return;
            }

            // 2. Calculate offsets and split points
            TimeSpan splitOffset = PlayheadPosition - targetClip.StartTime;

            // Validate split point (must not be too close to edges)
            const double minSegmentLength = 0.2; // 200ms
            if (splitOffset.TotalSeconds < minSegmentLength || (targetClip.Duration - splitOffset).TotalSeconds < minSegmentLength)
            {
                StatusMessage = "Playhead too close to clip edge";
                return;
            }

            // 3. Create the second half
            var secondHalf = new TimelineClip
            {
                MediaItemId = targetClip.MediaItemId,
                MediaItem = targetClip.MediaItem,
                TrackId = targetClip.TrackId,
                IsMuted = targetClip.IsMuted,
                Volume = targetClip.Volume,
                IsEnabled = targetClip.IsEnabled,
                
                // Keep same trim as original for the end, but shift start
                TrimStart = targetClip.TrimStart + splitOffset,
                TrimEnd = targetClip.TrimEnd,
                Duration = targetClip.Duration - splitOffset,
                
                // Position start right after the first half
                StartTime = targetClip.StartTime + splitOffset,
                Order = targetClip.Order + 1 
            };

            // 4. Update the first half (original clip)
            targetClip.TrimEnd = targetClip.TrimEnd + secondHalf.Duration;
            targetClip.Duration = splitOffset;

            // 5. Commit changes to timeline
            targetTrack.Clips.Add(secondHalf);
            
            // Re-order and handle snapping/ripple
            if (RippleEditingEnabled)
            {
                // Snap everything together (current behavior)
                targetTrack.RecalculateStartTimes();
            }
            else
            {
                // Just sort to ensure Order indexes are correct, but keep gaps
                targetTrack.SortClips();
            }
            
            // 6. UI Update and Selection
            RefreshTimeline();
            SelectedClip = secondHalf; // Select the new part for better flow
            StatusMessage = $"Split {targetClip.MediaItem?.Name ?? "clip"}";
            SaveState();
        }

        [RelayCommand]
        public void ExtendClipToEndOfAudio(TimelineClip? clip)
        {
            if (clip == null) clip = SelectedClip;
            if (clip == null) return;

            // Find the end of the longest audio track
            TimeSpan maxAudioEnd = TimeSpan.Zero;
            foreach (var track in TimelineTracks)
            {
                if (track.TrackType == MediaType.Audio)
                {
                    var end = track.GetEndTime();
                    if (end > maxAudioEnd) maxAudioEnd = end;
                }
            }

            if (maxAudioEnd > clip.StartTime)
            {
                clip.Duration = maxAudioEnd - clip.StartTime;
                RefreshTimeline();
                SaveState();
                StatusMessage = $"Extended {clip.MediaItem?.Name} to end of audio ({maxAudioEnd:mm\\:ss})";
            }
            else
            {
                StatusMessage = "No audio tracks found or clip starts after audio ends.";
            }
        }

        [RelayCommand]
        private void RecalculateTimeline()
        {
            foreach (var track in TimelineTracks)
            {
                track.RecalculateStartTimes();
            }
            RefreshTimeline();
            StatusMessage = "Timeline recalculated";
        }

        [ObservableProperty]
        private double _timelineWidth = 2000; // Default width

        private void UpdateTimelineWidth()
        {
            // Calculate total duration in seconds based on tracks
            TimeSpan maxDuration = TimeSpan.Zero;
            foreach (var track in TimelineTracks)
            {
                var end = track.GetEndTime();
                if (end > maxDuration) maxDuration = end;
            }

            CurrentProject.Duration = maxDuration;

            // Convert to pixels based on ZoomLevel (PixelsPerSecond) + some padding
            double width = maxDuration.TotalSeconds * ZoomLevel + 1000;
            
            // Minimum width to fill screen
            TimelineWidth = Math.Max(2000, width);
        }

        public void RefreshTimeline()
        {
            // Update width
            UpdateTimelineWidth();

            // Force UI update
            var tracks = TimelineTracks.ToList();
            TimelineTracks.Clear();
            foreach (var t in tracks)
            {
                TimelineTracks.Add(t);
            }
        }

        [ObservableProperty]
        private TimelineClip? _selectedClip;

        [ObservableProperty]
        private string _currentSubtitleText = "";

        [ObservableProperty]
        private System.Windows.Thickness _subtitleMargin = new System.Windows.Thickness(10, 0, 0, 10);

        public void UpdateSubtitles()
        {
            if (SubtitleTracks == null) return;
            
            var pos = PlayheadPosition;
            var currentEntries = new List<(SubtitleTrack Track, SubtitleEntry Entry)>();

            // 1. Identify what should be visible
            foreach (var track in SubtitleTracks)
            {
                if (!track.IsEnabled) continue;
                var entry = track.Entries.FirstOrDefault(e => pos >= e.StartTime + track.TimeOffset && pos <= e.EndTime + track.TimeOffset);
                if (entry != null)
                {
                    currentEntries.Add((track, entry));
                }
            }

            // 2. Remove those that are no longer active
            var toRemove = ActiveSubtitlePreviews.Where(p => !currentEntries.Any(e => e.Track == p.Track)).ToList();
            foreach (var r in toRemove) ActiveSubtitlePreviews.Remove(r);

            // 3. Update existing or Add new
            foreach (var item in currentEntries)
            {
                var existing = ActiveSubtitlePreviews.FirstOrDefault(p => p.Track == item.Track);
                
                System.Windows.Thickness margin;
                string alignName = GetAlignmentName(item.Track.Alignment);

                if (alignName.StartsWith("Top"))
                    margin = new System.Windows.Thickness(item.Track.GlobalMarginL, item.Track.GlobalMarginV, 10, 0);
                else if (alignName.StartsWith("Middle"))
                    margin = new System.Windows.Thickness(item.Track.GlobalMarginL, item.Track.GlobalMarginV, 10, 0);
                else
                    margin = new System.Windows.Thickness(item.Track.GlobalMarginL, 0, 10, item.Track.GlobalMarginV);

                if (existing != null)
                {
                    // Update properties (notifications triggered by [ObservableProperty])
                    existing.Text = item.Entry.Text;
                    existing.Margin = margin;
                    existing.Color = item.Track.FontColor;
                    existing.Font = item.Track.FontName;
                    existing.FontSize = item.Track.FontSize;
                    existing.TextAlignment = GetTextAlignment(alignName);
                    existing.VerticalAlignment = GetVerticalAlignment(alignName);
                    existing.Width = item.Track.TextRegionWidth;
                    existing.IsSelected = (item.Track == SelectedSubtitleTrack);
                    existing.IsBold = item.Track.IsBold;
                    existing.IsItalic = item.Track.IsItalic;
                    existing.OutlineWidth = item.Track.OutlineWidth;
                    existing.ShadowWidth = item.Track.ShadowWidth;
                    existing.ShadowColor = item.Track.ShadowColor;
                    existing.HasBackgroundBox = item.Track.HasBackgroundBox;
                    existing.BackgroundBoxOpacity = item.Track.BackgroundBoxOpacity;
                }
                else
                {
                    // Add new
                    ActiveSubtitlePreviews.Add(new SubtitlePreviewItem
                    {
                        Text = item.Entry.Text,
                        Margin = margin,
                        Color = item.Track.FontColor,
                        Font = item.Track.FontName,
                        FontSize = item.Track.FontSize,
                        TextAlignment = GetTextAlignment(alignName),
                        VerticalAlignment = GetVerticalAlignment(alignName),
                        Width = item.Track.TextRegionWidth,
                        IsSelected = (item.Track == SelectedSubtitleTrack),
                        IsBold = item.Track.IsBold,
                        IsItalic = item.Track.IsItalic,
                        OutlineWidth = item.Track.OutlineWidth,
                        ShadowWidth = item.Track.ShadowWidth,
                        ShadowColor = item.Track.ShadowColor,
                        HasBackgroundBox = item.Track.HasBackgroundBox,
                        BackgroundBoxOpacity = item.Track.BackgroundBoxOpacity,
                        Track = item.Track
                    });
                }
            }
        }

        private System.Windows.TextAlignment GetTextAlignment(string align)
        {
            if (align.Contains("Left")) return System.Windows.TextAlignment.Left;
            if (align.Contains("Right")) return System.Windows.TextAlignment.Right;
            return System.Windows.TextAlignment.Center;
        }

        private System.Windows.VerticalAlignment GetVerticalAlignment(string align)
        {
            if (align.StartsWith("Top")) return System.Windows.VerticalAlignment.Top;
            if (align.StartsWith("Middle")) return System.Windows.VerticalAlignment.Center;
            return System.Windows.VerticalAlignment.Bottom;
        }

        public void SetSubtitlePosition(double left, double vPos)
        {
            if (SelectedSubtitleTrack == null) return;

            // Clamp to reasonable bounds (Project Dimensions)
            double maxWidth = CurrentProject?.OutputWidth ?? 1920;
            double maxHeight = CurrentProject?.OutputHeight ?? 1080;

            // Clamp based on alignment
            string alignName = GetAlignmentName(SelectedSubtitleTrack.Alignment);
            if (alignName.StartsWith("Middle"))
            {
                // Allow negative for center offset
                vPos = Math.Max(-maxHeight, Math.Min(vPos, maxHeight));
            }
            else
            {
                vPos = Math.Max(0, Math.Min(vPos, maxHeight));
            }

            SelectedSubtitleTrack.GlobalMarginL = (int)left;
            SelectedSubtitleTrack.GlobalMarginV = (int)vPos;
            
            UpdateSubtitles();
        }

        public void SetSubtitleWidth(double width)
        {
            if (SelectedSubtitleTrack == null) return;
            SelectedSubtitleTrack.TextRegionWidth = Math.Max(50, width);
            UpdateSubtitles();
        }

        public void ScaleSubtitle(double width, int fontSize)
        {
            if (SelectedSubtitleTrack == null) return;
            SelectedSubtitleTrack.TextRegionWidth = Math.Max(50, width);
            // Updating the ViewModel property triggers OnSelectedFontSizeChanged -> UpdateSubtitleSettings -> UpdateSubtitles
            SelectedFontSize = Math.Max(8, fontSize);
        }

        public System.Windows.TextAlignment SubtitleTextAlignment
        {
            get
            {
                if (SelectedAlignment.Contains("Left")) return System.Windows.TextAlignment.Left;
                if (SelectedAlignment.Contains("Right")) return System.Windows.TextAlignment.Right;
                return System.Windows.TextAlignment.Center;
            }
        }

        public System.Windows.VerticalAlignment SubtitleVerticalAlignment
        {
            get
            {
                if (SelectedAlignment.StartsWith("Top")) return System.Windows.VerticalAlignment.Top;
                if (SelectedAlignment.StartsWith("Middle")) return System.Windows.VerticalAlignment.Center;
                return System.Windows.VerticalAlignment.Bottom;
            }
        }

        [ObservableProperty]
        private string? _selectedFont = "Nirmala UI";

        [ObservableProperty]
        private int _selectedFontSize = 24;

        [ObservableProperty]
        private string _selectedFontColor = "#FFFFFF";

        [ObservableProperty]
        private System.Windows.Thickness _logoMargin;

        [ObservableProperty]
        private string? _logoPath;

        [ObservableProperty]
        private double _logoScale = 1.0;

        partial void OnLogoScaleChanged(double value)
        {
            if (CurrentProject != null)
                CurrentProject.LogoScale = value;
        }

        [RelayCommand]
        private void SelectLogo()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                LogoPath = dialog.FileName;
                CurrentProject.LogoPath = dialog.FileName;
                StatusMessage = "Logo added to project";
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var window = new SettingsWindow();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        public void SetLogoPosition(double left, double top)
        {
            CurrentProject.LogoX = (int)left;
            CurrentProject.LogoY = (int)top;
            LogoMargin = new System.Windows.Thickness(left, top, 0, 0);
        }

        [ObservableProperty]
        private string _selectedAlignment = "Bottom Center";

        private void UpdateSubtitleSettings()
        {
            if (SelectedSubtitleTrack == null) return;
            
            SelectedSubtitleTrack.FontName = SelectedFont ?? "Mangal";
            SelectedSubtitleTrack.FontSize = SelectedFontSize;
            SelectedSubtitleTrack.FontColor = SelectedFontColor;
            SelectedSubtitleTrack.Alignment = GetAlignmentValue(SelectedAlignment ?? "Bottom Center");
            SelectedSubtitleTrack.IsBold = IsFontBold;
            SelectedSubtitleTrack.IsItalic = IsFontItalic;
            SelectedSubtitleTrack.OutlineWidth = OutlineWidth;
            SelectedSubtitleTrack.ShadowWidth = ShadowWidth;
            SelectedSubtitleTrack.ShadowColor = SelectedShadowColor;
            SelectedSubtitleTrack.HasBackgroundBox = HasBackgroundBox;
            SelectedSubtitleTrack.BackgroundBoxOpacity = BackgroundBoxOpacity;

            UpdateSubtitles();
        }

        partial void OnSelectedSubtitleTrackChanged(SubtitleTrack? value)
        {
            if (value != null)
            {
                SelectedFont = value.FontName;
                SelectedFontSize = value.FontSize;
                SelectedFontColor = value.FontColor;
                SelectedAlignment = GetAlignmentName(value.Alignment);
                IsFontBold = value.IsBold;
                IsFontItalic = value.IsItalic;
                OutlineWidth = value.OutlineWidth;
                ShadowWidth = value.ShadowWidth;
                SelectedShadowColor = value.ShadowColor;
                HasBackgroundBox = value.HasBackgroundBox;
                BackgroundBoxOpacity = value.BackgroundBoxOpacity;
            }
        }

        private string GetAlignmentName(int val)
        {
            return val switch
            {
                1 => "Bottom Left",
                2 => "Bottom Center",
                3 => "Bottom Right",
                4 => "Middle Left",
                5 => "Middle Center",
                6 => "Middle Right",
                7 => "Top Left",
                8 => "Top Center",
                9 => "Top Right",
                _ => "Bottom Center"
            };
        }

        private int GetAlignmentValue(string alignment)
        {
            return alignment switch
            {
                "Bottom Left" => 1,
                "Bottom Center" => 2,
                "Bottom Right" => 3,
                "Middle Left" => 4,
                "Middle Center" => 5,
                "Middle Right" => 6,
                "Top Left" => 7,
                "Top Center" => 8,
                "Top Right" => 9,
                _ => 2
            };
        }

        partial void OnSelectedFontChanged(string? value) => UpdateSubtitleSettings();
        partial void OnSelectedFontSizeChanged(int value) => UpdateSubtitleSettings();
        partial void OnSelectedFontColorChanged(string value) => UpdateSubtitleSettings();
        
        partial void OnSelectedAlignmentChanged(string value)
        {
            UpdateSubtitleSettings();
            OnPropertyChanged(nameof(SubtitleTextAlignment));
            OnPropertyChanged(nameof(SubtitleVerticalAlignment));
        }

        partial void OnIsFontBoldChanged(bool value) => UpdateSubtitleSettings();
        partial void OnIsFontItalicChanged(bool value) => UpdateSubtitleSettings();
        partial void OnOutlineWidthChanged(double value) => UpdateSubtitleSettings();
        partial void OnShadowWidthChanged(double value) => UpdateSubtitleSettings();
        partial void OnSelectedShadowColorChanged(string value) => UpdateSubtitleSettings();
        partial void OnHasBackgroundBoxChanged(bool value) => UpdateSubtitleSettings();
        partial void OnBackgroundBoxOpacityChanged(double value) => UpdateSubtitleSettings();
    }
}

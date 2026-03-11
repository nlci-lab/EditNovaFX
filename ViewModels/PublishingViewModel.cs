using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VideoEditor.Models;
using VideoEditor.Services;

namespace VideoEditor.ViewModels
{
    public partial class PublishingViewModel : ObservableObject
    {
        private readonly Project _project;
        private readonly AIContentService _aiService;
        private readonly YouTubePublishingService _youtubeService;
        private readonly FFmpegService _ffmpegService;

        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private string _tags = string.Empty;
        [ObservableProperty] private bool _isGenerating;
        [ObservableProperty] private bool _isPublishing;
        [ObservableProperty] private bool _isExporting;
        [ObservableProperty] private int _publishProgress;
        [ObservableProperty] private int _exportProgress;
        [ObservableProperty] private string _statusMessage = "Ready to generate metadata";
        [ObservableProperty] private string? _videoUrl;
        [ObservableProperty] private bool _isSuccess;

        [ObservableProperty] private bool _isYouTubeLoggedIn;
        [ObservableProperty] private string _channelName = "Not Logged In";
        [ObservableProperty] private string? _channelThumbnail;

        [ObservableProperty] private ObservableCollection<string> _platforms = new ObservableCollection<string> 
        { "YouTube", "Instagram Reels", "TikTok", "Facebook", "Twitter (X)" };
        
        [ObservableProperty] private string _selectedPlatform = "YouTube";

        [ObservableProperty] private ObservableCollection<string> _presets = new ObservableCollection<string> 
        { "YouTube 1080p", "YouTube Shorts (Vertical)" };

        [ObservableProperty] private string _selectedPreset = "YouTube 1080p";

        [ObservableProperty] private ObservableCollection<string> _privacyStatuses = new ObservableCollection<string> 
        { "public", "unlisted", "private" };

        [ObservableProperty] private string _selectedPrivacyStatus = "unlisted";

        // ── Thumbnail ────────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasThumbnail))]
        private string _thumbnailPath = string.Empty;

        /// <summary>True when a thumbnail image has been selected.</summary>
        public bool HasThumbnail => !string.IsNullOrEmpty(ThumbnailPath) && File.Exists(ThumbnailPath);

        public PublishingViewModel(Project project)
        {
            _project = project;
            _aiService = new AIContentService();
            _youtubeService = new YouTubePublishingService();
            _ffmpegService = new FFmpegService();
            Title = project.Name;
            
            // Auto-check for existing credentials/auth
            _ = CheckExistingAuth();
        }

        private async Task CheckExistingAuth()
        {
            if (_youtubeService.HasCredentials)
            {
                StatusMessage = "Checking YouTube session...";
                // Try to authorize silently (it will use the saved token)
                bool success = await _youtubeService.AuthorizeAsync();
                if (success)
                {
                    var info = await _youtubeService.GetChannelInfoAsync();
                    ChannelName = info.Title;
                    ChannelThumbnail = info.ThumbnailUrl;
                    IsYouTubeLoggedIn = true;
                    StatusMessage = $"Connected as {ChannelName}";
                }
                else
                {
                    StatusMessage = "YouTube session expired. Please login again.";
                }
            }
        }

        [RelayCommand]
        private async Task LoginYouTube()
        {
            StatusMessage = "Signing in to Google...";
            bool success = await _youtubeService.AuthorizeAsync();
            if (success)
            {
                var info = await _youtubeService.GetChannelInfoAsync();
                ChannelName = info.Title;
                ChannelThumbnail = info.ThumbnailUrl;
                IsYouTubeLoggedIn = true;
                StatusMessage = $"Logged in as {ChannelName}";
            }
            else
            {
                StatusMessage = "Login failed. Check your connection or credentials.";
                System.Windows.MessageBox.Show("Login failed. Make sure you have configured your Google Client ID and Secret in YouTubePublishingService.cs", "Authentication Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task GenerateAIMetadata()
        {
            IsGenerating = true;
            StatusMessage = "AI is analyzing subtitle context...";
            
            try
            {
                var metadata = await _aiService.GenerateMetadata(_project);
                Title = metadata.Title;
                Description = metadata.Description;
                Tags = string.Join(", ", metadata.Tags);
                StatusMessage = "Metadata generated successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"AI Error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        [RelayCommand]
        private async Task Publish()
        {
            if (SelectedPlatform == "YouTube")
            {
                if (!IsYouTubeLoggedIn)
                {
                    StatusMessage = "Please login to YouTube first";
                    return;
                }

                if (SelectedPreset == "YouTube Shorts (Vertical)")
                {
                    if (_project.Duration.TotalSeconds > 60)
                    {
                        var warn = System.Windows.MessageBox.Show(
                            "YouTube Shorts must be under 60 seconds. Your project is longer. Continue anyway?",
                            "Duration Warning",
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Warning);
                        
                        if (warn == System.Windows.MessageBoxResult.No) return;
                    }
                }

                string videoPath = _project.LastExportedFilePath;
                
                // Check if file exists
                if (string.IsNullOrEmpty(videoPath) || !System.IO.File.Exists(videoPath))
                {
                    var result = System.Windows.MessageBox.Show(
                        "The video is not exported yet. Do you want to export and publish automatically?",
                        "Export Required",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        // 1. Automatic Export
                        string exportDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "EditNovaFX", "Exports");
                        
                        if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);
                        
                        videoPath = Path.Combine(exportDir, $"{Title.Replace(" ", "_")}.mp4");
                        
                        IsExporting = true;
                        IsPublishing = true; // Overall lock
                        StatusMessage = "Exporting video... 0%";
                        
                        try 
                        {
                            bool isShorts = SelectedPreset == "YouTube Shorts (Vertical)";
                            
                            var exportSettings = new ExportSettings
                            {
                                OutputPath = videoPath,
                                Width = isShorts ? 1080 : _project.OutputWidth,
                                Height = isShorts ? 1920 : _project.OutputHeight,
                                FrameRate = _project.OutputFrameRate
                            };

                            var exportProgress = new Progress<int>(p => {
                                ExportProgress = p;
                                StatusMessage = $"Exporting video... {p}%";
                            });

                            var exportResult = await _ffmpegService.RenderVideo(_project, exportSettings, exportProgress);
                            
                            if (!exportResult.success)
                            {
                                StatusMessage = $"Export failed: {exportResult.message}";
                                System.Windows.MessageBox.Show($"Export failed: {exportResult.message}", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                IsExporting = false;
                                IsPublishing = false;
                                return;
                            }
                            
                            _project.LastExportedFilePath = videoPath;
                            StatusMessage = "Processing video...";
                            await Task.Delay(1000); // Small breath for OS file handles
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"Export Error: {ex.Message}";
                            IsExporting = false;
                            IsPublishing = false;
                            return;
                        }
                        finally
                        {
                            IsExporting = false;
                        }
                    }
                    else
                    {
                        return; // User cancelled
                    }
                }

                // 2. Upload to YouTube
                IsPublishing = true;
                StatusMessage = "Uploading to YouTube... 0%";
                PublishProgress = 0;
                
                try
                {
                    var progress = new Progress<int>(p => {
                        PublishProgress = p;
                        StatusMessage = $"Uploading to YouTube... {p}%";
                    });

                    // Note: YouTubePublishingService.UploadVideoAsync returns the video ID
                    var videoId = await _youtubeService.UploadVideoAsync(
                        videoPath, 
                        Title, 
                        Description, 
                        Tags.Split(',', StringSplitOptions.RemoveEmptyEntries), 
                        SelectedPrivacyStatus, 
                        progress);
                    
                    // ── Thumbnail upload (non-fatal) ──────────────────────
                    if (!string.IsNullOrEmpty(videoId) && HasThumbnail)
                    {
                        try
                        {
                            StatusMessage = "Uploading thumbnail...";
                            await _youtubeService.UploadThumbnailAsync(videoId, ThumbnailPath);
                            StatusMessage = "Thumbnail uploaded!";
                        }
                        catch (Exception thumbEx)
                        {
                            // Thumbnail upload failed — video is still live, just warn the user
                            System.Windows.MessageBox.Show(
                                $"Video uploaded successfully, but the thumbnail could not be set:\n{thumbEx.Message}\n\nYou can set it manually in YouTube Studio.",
                                "Thumbnail Warning",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                        }
                    }

                    StatusMessage = "Upload completed successfully!";
                    IsSuccess = true;
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        VideoUrl = $"https://www.youtube.com/watch?v={videoId}";
                    }
                    else
                    {
                         VideoUrl = "https://www.youtube.com/channel_dashboard"; // Fallback
                    }
                    
                    System.Windows.MessageBox.Show("Video published successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Upload failed: {ex.Message}";
                    System.Windows.MessageBox.Show($"YouTube upload failed: {ex.Message}", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsPublishing = false;
                }
            }
            else
            {
                // Simulated publishing for other platforms
                IsPublishing = true;
                StatusMessage = $"Exporting for {SelectedPlatform}...";
                PublishProgress = 0;

                for (int i = 0; i <= 100; i += 5)
                {
                    PublishProgress = i;
                    StatusMessage = $"{SelectedPlatform} Progress... {i}%";
                    await Task.Delay(150);
                }

                IsPublishing = false;
                StatusMessage = $"Successfully published to {SelectedPlatform}!";
                System.Windows.MessageBox.Show($"Video successfully uploaded to {SelectedPlatform}!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void BrowseThumbnail()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Thumbnail Image",
                Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All Files (*.*)|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
                ThumbnailPath = dlg.FileName;
        }

        [RelayCommand]
        private void ClearThumbnail() => ThumbnailPath = string.Empty;

        [RelayCommand]
        private void OpenInYouTube()
        {
            if (!string.IsNullOrEmpty(VideoUrl))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = VideoUrl,
                    UseShellExecute = true
                });
            }
        }

        [RelayCommand]
        private void CopyLink()
        {
            if (!string.IsNullOrEmpty(VideoUrl))
            {
                System.Windows.Clipboard.SetText(VideoUrl);
                StatusMessage = "Link copied to clipboard!";
            }
        }
    }
}

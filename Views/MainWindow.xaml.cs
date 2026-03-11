using System.Windows;
using System.Windows.Input;
using VideoEditor.ViewModels;
using System.Windows.Threading;
using System;
using VideoEditor.Services;
using VideoEditor.Models;
using System.Windows.Media;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;

namespace VideoEditor.Views
{
    public partial class MainWindow : Window
    {
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private bool _isFullScreen = false;
        private bool _isPlaying = false;
        private DispatcherTimer _timer;
        private TimelinePreviewEngine _previewEngine;

        public MainWindow()
        {
            InitializeComponent();
            _previousWindowState = WindowState.Maximized;
            _previousWindowStyle = WindowStyle.SingleBorderWindow;

            // Setup preview engine
            _previewEngine = new TimelinePreviewEngine(PreviewMediaElement, PreviewImage, PreviewStatus);

            // Setup timer for updating position
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50); // Faster update for smoothness
            _timer.Tick += Timer_Tick;

            // Subscribe to SelectedMediaItem changes
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedMediaItem))
            {
                LoadSelectedMedia();
            }
            else if (e.PropertyName == nameof(MainViewModel.PlayheadPosition))
            {
                // Handle Scrubbing while Paused
                if (!_isPlaying && DataContext is MainViewModel vm)
                {
                    // Ensure engine knows current project state before update
                    _previewEngine.SetProject(vm.CurrentProject);
                    _previewEngine.Update(vm.PlayheadPosition, false);
                    TxtPosition.Text = vm.PlayheadPosition.ToString(@"hh\:mm\:ss");
                    UpdateDurationDisplay(vm);
                }
            }
        }

        private void LoadSelectedMedia()
        {
            // DISABLED: We now strictly preview the Timeline, not individual sources,
            // as per "Combined Timeline Preview" requirement.
            // If Source Monitor is needed later, we can add a mode toggle.
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Ensure engine has latest project
                _previewEngine.SetProject(vm.CurrentProject);

                if (_isPlaying)
                {
                    // Calculate Total Duration
                    TimeSpan totalDuration = TimeSpan.Zero;
                    if (vm.CurrentProject != null)
                    {
                        foreach (var track in vm.CurrentProject.Tracks)
                        {
                            var end = track.GetEndTime();
                            if (end > totalDuration) totalDuration = end;
                        }
                    }

                    // Check boundaries
                    if (vm.PlayheadPosition >= totalDuration && totalDuration > TimeSpan.Zero)
                    {
                         vm.PlayheadPosition = totalDuration;
                         
                         // Auto-Pause at end
                         _previewEngine.Pause();
                         _timer.Stop();
                         BtnPlayPause.Content = "▶";
                         _isPlaying = false;
                    }
                    else
                    {
                        // Advance Playhead
                        vm.PlayheadPosition = vm.PlayheadPosition.Add(_timer.Interval);
                        
                        // Update Engine
                        _previewEngine.Update(vm.PlayheadPosition, true);
                        vm.UpdateSubtitles();
                    }
                }
                
                TxtPosition.Text = vm.PlayheadPosition.ToString(@"hh\:mm\:ss");
                UpdateDurationDisplay(vm);
            }
        }

        private void UpdateDurationDisplay(MainViewModel vm)
        {
            if (vm.CurrentProject == null) return;
            
            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (var track in vm.CurrentProject.Tracks)
            {
                var end = track.GetEndTime();
                if (end > totalDuration) totalDuration = end;
            }
            
            TxtDuration.Text = totalDuration.ToString(@"hh\:mm\:ss");
        }

        private void PreviewMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Managed by Engine
        }

        private void PreviewMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Managed by Engine
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                _previewEngine.Pause();
                _timer.Stop();
                BtnPlayPause.Content = "▶";
                _isPlaying = false;
            }
            else
            {
                _timer.Start();
                BtnPlayPause.Content = "⏸";
                _isPlaying = true;

                if (DataContext is MainViewModel vm)
                {
                     _previewEngine.Update(vm.PlayheadPosition, true);
                }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _previewEngine.Stop();
            _timer.Stop();
            BtnPlayPause.Content = "▶";
            _isPlaying = false;
            
            if (DataContext is MainViewModel vm)
            {
                vm.PlayheadPosition = TimeSpan.Zero;
                TxtPosition.Text = "00:00:00";
            }
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PlayheadPosition = TimeSpan.Zero;
                TxtPosition.Text = "00:00:00";
                _previewEngine.Update(TimeSpan.Zero);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            
            if (sender is MenuItem menuItem && menuItem.Tag is string topicTitle)
            {
                helpWindow.SelectTopic(topicTitle);
            }
            
            helpWindow.Show();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Toggle fullscreen with F11
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
            }
            // Exit fullscreen with Escape
            else if (e.Key == Key.Escape && _isFullScreen)
            {
                ToggleFullScreen();
            }
            // Space bar for play/pause
            else if (e.Key == Key.Space && PreviewMediaElement.Source != null)
            {
                BtnPlayPause_Click(sender, e);
                e.Handled = true;
            }
            // Delete Key to remove clip (if listbox not focused)
            else if (e.Key == Key.Delete)
            {
                if (!MediaLibraryTabs.IsKeyboardFocusWithin && DataContext is MainViewModel vm)
                {
                    vm.RemoveClipCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private Point _dragStartPoint;

        private void MediaLibraryListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void MediaLibraryListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is not UIElement element) return;
                var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (listBoxItem == null) return;

                if (sender is not ListBox listBox) return;
                
                var mediaItem = listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem) as MediaItem;
                if (mediaItem == null) return;

                DataObject dragData = new DataObject("MediaItem", mediaItem);
                DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Copy);
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void TimelineControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("MediaItem"))
            {
                var mediaItem = e.Data.GetData("MediaItem") as MediaItem;
                if (mediaItem != null && DataContext is MainViewModel vm)
                {
                    var timeline = sender as VideoEditor.Controls.TimelineControl;
                    var pos = e.GetPosition(timeline);

                    // Determine Track based on Y Position
                    // Constants from TimelineControl
                    double headerHeight = 30.0;
                    double trackHeight = 70.0;
                    double gap = 5.0;

                    if (pos.Y > headerHeight)
                    {
                        double relativeY = pos.Y - headerHeight;
                        int trackIndex = (int)(relativeY / (trackHeight + gap));

                        if (trackIndex >= 0 && trackIndex < vm.TimelineTracks.Count)
                        {
                            var track = vm.TimelineTracks[trackIndex];
                            
                            // Check compatibility
                            bool isCompatible = false;
                            bool isVideoItem = mediaItem.Type == MediaType.Video || mediaItem.Type == MediaType.Image;
                            bool isAudioItem = mediaItem.Type == MediaType.Audio;

                            // Strict check: Only Video on Video, Audio on Audio
                            if (isVideoItem && track.TrackType == MediaType.Video) isCompatible = true;
                            else if (isAudioItem && track.TrackType == MediaType.Audio) isCompatible = true;
                            // Also allow Universal tracks, though we prefer strict typing now
                            else if (track.TrackType == MediaType.Universal) isCompatible = true;

                            if (isCompatible)
                            {
                                var clip = new TimelineClip
                                {
                                    MediaItemId = mediaItem.Id,
                                    MediaItem = mediaItem,
                                    Duration = mediaItem.Duration
                                };

                                track.AddClipSequentially(clip);
                                vm.RefreshTimeline();
                                vm.StatusMessage = $"Added {mediaItem.Name} to {track.Name}";
                                vm.CommitChangeCommand.Execute(null);
                                return;
                            }
                            else
                            {
                                vm.StatusMessage = $"Cannot add {mediaItem.Type} to {track.TrackType} track.";
                                return; // Stop here, don't fallback to random track
                            }
                        }
                    }

                    // Fallback to auto-add only if we weren't targeting a specific valid track region
                    // But for drag-and-drop to specific area, we might want to just stop if missed
                    vm.AddToTimeline(mediaItem);
                }
            }
        }

        private void ToggleFullScreen()
        {
            if (_isFullScreen)
            {
                // Exit fullscreen
                WindowStyle = _previousWindowStyle;
                WindowState = _previousWindowState;
                ResizeMode = ResizeMode.CanResize;
                _isFullScreen = false;

                // Show all UI panels
                SetUIVisibility(Visibility.Visible);
            }
            else
            {
                // Enter fullscreen
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;
                
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                _isFullScreen = true;

                // Hide all UI panels except Preview
                SetUIVisibility(Visibility.Collapsed);
            }
        }

        private void SetUIVisibility(Visibility visibility)
        {
            // Toggle side, bottom, and menu panels
            TopBar.Visibility = visibility;
            StatusBar.Visibility = visibility;
            MediaLibraryPanel.Visibility = visibility;
            MediaLibSplitter.Visibility = visibility;
            TimelinePanel.Visibility = visibility;
            TimelineSplitter.Visibility = visibility;
            PropertiesPanel.Visibility = visibility;
            PropertiesSplitter.Visibility = visibility;

            // Internal Preview Panel elements
            PreviewTitle.Visibility = visibility;
            PreviewControlsBar.Visibility = visibility;

            // Adjust Grid definitions to allow growing
            if (visibility == Visibility.Collapsed)
            {
                // Entering Fullscreen
                MediaLibCol.Width = new GridLength(0);
                MediaLibSplitterCol.Width = new GridLength(0);
                PropertiesCol.Width = new GridLength(0);
                PropertiesSplitterCol.Width = new GridLength(0);
                TimelineRow.Height = new GridLength(0);
                TimelineSplitterRow.Height = new GridLength(0);
                
                // Maximize Preview area
                PreviewRow.Height = new GridLength(1, GridUnitType.Star);

                // Internal Rows
                PreviewHeaderRow.Height = new GridLength(0);
                PreviewFooterRow.Height = new GridLength(0);

                // Remove margins to bleed to edges
                PreviewPanel.Margin = new Thickness(0);
                PreviewPanel.BorderThickness = new Thickness(0);
                VideoBorder.Margin = new Thickness(0);
            }
            else
            {
                // Exiting Fullscreen - Restore defaults
                MediaLibCol.Width = new GridLength(250);
                MediaLibSplitterCol.Width = new GridLength(5);
                PropertiesCol.Width = new GridLength(300);
                PropertiesSplitterCol.Width = new GridLength(5);
                TimelineRow.Height = new GridLength(1, GridUnitType.Star);
                TimelineSplitterRow.Height = new GridLength(5);
                PreviewRow.Height = new GridLength(2, GridUnitType.Star);
                
                // Internal Rows
                PreviewHeaderRow.Height = GridLength.Auto;
                PreviewFooterRow.Height = GridLength.Auto;

                PreviewPanel.Margin = new Thickness(5);
                PreviewPanel.BorderThickness = new Thickness(1);
                VideoBorder.Margin = new Thickness(10);
            }
        }

        public void ToggleFullScreenFromMenu(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void MediaLibraryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the ViewModel and execute AddToTimeline command
            if (DataContext is MainViewModel viewModel && viewModel.SelectedMediaItem != null)
            {
                viewModel.AddToTimelineCommand.Execute(null);
            }
        }

        private void MediaLibraryListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.RemoveMediaItemCommand.Execute(null);
                }
            }
        }

        private void TimelineClip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is TimelineClip clip)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectedClip = clip;
                    vm.StatusMessage = $"Selected clip: {clip.MediaItem?.Name}";
                    e.Handled = true; // Prevent bubbling
                }
            }
        }

        private bool _isDraggingSubtitle;
        private Point _subtitleDragStart;
        private Thickness _initialSubtitleMargin;
        private bool _isResizingSubtitle;
        private Point _subtitleResizeStart;
        private double _initialSubtitleWidth;
        private int _initialSubtitleFontSize;
        private bool _isProportionalResize;

        private Grid? FindPreviewCanvas()
        {
            return PreviewCanvas;
        }

        private void Subtitle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                textBlock.Cursor = Cursors.SizeAll;
            }
        }

        private void Subtitle_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSubtitle && !_isResizingSubtitle)
            {
                if (sender is TextBlock textBlock)
                {
                    textBlock.Cursor = Cursors.Arrow;
                }
            }
        }

        private void Subtitle_ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement handle && DataContext is MainViewModel vm)
            {
                var canvas = FindPreviewCanvas();
                if (canvas == null) return;

                _isResizingSubtitle = true;
                _subtitleResizeStart = e.GetPosition(canvas);
                
                string handleName = handle.Name ?? string.Empty;
                _isProportionalResize = handleName.Contains("Corner");

                if (handle.Tag is SubtitleTrack track)
                {
                    vm.SelectedSubtitleTrack = track;
                    
                    // Find the TextBlock to get initial dimensions
                    var container = FindAncestor<Grid>(handle);
                    if (container != null)
                    {
                        var textBlock = FindVisualChild<TextBlock>(container, "SubtitleText");
                        if (textBlock != null)
                        {
                            _initialSubtitleWidth = textBlock.ActualWidth > 0 ? textBlock.ActualWidth : 100;
                            _initialSubtitleFontSize = vm.SelectedSubtitleTrack?.FontSize ?? 28;
                        }
                    }
                }

                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Subtitle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && DataContext is MainViewModel vm)
            {
                var canvas = FindPreviewCanvas();
                if (canvas == null) return;

                // Only start dragging if we're not clicking on a resize handle
                if (!_isResizingSubtitle)
                {
                    _isDraggingSubtitle = true;
                    _subtitleDragStart = e.GetPosition(canvas);
                    
                    // Find the container Grid to get its margin
                    var container = element as Grid ?? FindAncestor<Grid>(element);
                    if (container != null)
                    {
                        _initialSubtitleMargin = container.Margin;
                    }
                    
                    // Select the track being dragged
                    if (element.Tag is SubtitleTrack track)
                    {
                        vm.SelectedSubtitleTrack = track;
                    }

                    element.CaptureMouse();
                }
                
                e.Handled = true;
            }
        }

        private void Subtitle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var canvas = FindPreviewCanvas();
            if (canvas == null) return;

            if (_isResizingSubtitle && DataContext is MainViewModel vmRes)
            {
                var currentPos = e.GetPosition(canvas);
                var delta = currentPos - _subtitleResizeStart;
                
                double newWidth = _initialSubtitleWidth + delta.X;
                
                if (_isProportionalResize)
                {
                    double scale = newWidth / _initialSubtitleWidth;
                    int newFontSize = (int)(_initialSubtitleFontSize * scale);
                    newFontSize = Math.Max(8, Math.Min(newFontSize, 200)); // Clamp font size
                    vmRes.ScaleSubtitle(newWidth, newFontSize);
                }
                else
                {
                    newWidth = Math.Max(50, newWidth); // Minimum width
                    vmRes.SetSubtitleWidth(newWidth);
                }
                e.Handled = true;
            }
            else if (_isDraggingSubtitle && sender is FrameworkElement element && DataContext is MainViewModel vm)
            {
                var currentPos = e.GetPosition(canvas);
                var delta = currentPos - _subtitleDragStart;

                double newLeft = _initialSubtitleMargin.Left + delta.X;
                double newVPos;
                
                // Find the container to get its VerticalAlignment
                var container = element as Grid ?? FindAncestor<Grid>(element);
                if (container == null) return;

                if (container.VerticalAlignment == System.Windows.VerticalAlignment.Top)
                    newVPos = _initialSubtitleMargin.Top + delta.Y;
                else if (container.VerticalAlignment == System.Windows.VerticalAlignment.Bottom)
                    newVPos = _initialSubtitleMargin.Bottom - delta.Y;
                else // Center
                    newVPos = _initialSubtitleMargin.Top + delta.Y;

                // Use canvas dimensions for constraints (video space)
                double maxWidth = canvas.ActualWidth;
                double maxHeight = canvas.ActualHeight;

                newLeft = Math.Max(0, Math.Min(newLeft, maxWidth));

                if (container.VerticalAlignment == VerticalAlignment.Center)
                {
                     // Allow negative offset from center
                     newVPos = Math.Max(-maxHeight/2, Math.Min(newVPos, maxHeight/2));
                }
                else
                {
                     newVPos = Math.Max(0, Math.Min(newVPos, maxHeight));
                }

                vm.SetSubtitlePosition(newLeft, newVPos);
                e.Handled = true;
            }
        }

        private void Subtitle_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSubtitle || _isResizingSubtitle)
            {
                _isDraggingSubtitle = false;
                _isResizingSubtitle = false;
                if (sender is FrameworkElement element)
                {
                    element.ReleaseMouseCapture();
                }
                if (DataContext is MainViewModel vm) vm.CommitChangeCommand.Execute(null);
                e.Handled = true;
            }
        }

        // Helper method to find a named visual child
        private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                {
                    return element;
                }
                var result = FindVisualChild<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private bool _isDraggingLogo;
        private Point _logoDragStart;
        private Thickness _initialLogoMargin;

        private void Logo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var canvas = FindPreviewCanvas();
                if (canvas == null) return;

                _isDraggingLogo = true;
                _logoDragStart = e.GetPosition(canvas);
                _initialLogoMargin = vm.LogoMargin;
                
                if (sender is FrameworkElement element)
                {
                    element.CaptureMouse();
                }
                e.Handled = true;
            }
        }

        private void Logo_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingLogo && DataContext is MainViewModel vm)
            {
                var canvas = FindPreviewCanvas();
                if (canvas == null) return;

                var currentPos = e.GetPosition(canvas);
                var delta = currentPos - _logoDragStart;

                double newLeft = _initialLogoMargin.Left + delta.X;
                double newTop = _initialLogoMargin.Top + delta.Y;

                // Constraint logic: Keep inside canvas (video space)
                double maxWidth = canvas.ActualWidth;
                double maxHeight = canvas.ActualHeight;

                newLeft = Math.Max(0, Math.Min(newLeft, maxWidth));
                newTop = Math.Max(0, Math.Min(newTop, maxHeight));

                vm.SetLogoPosition(newLeft, newTop);
            }
        }

        private void Logo_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingLogo)
            {
                _isDraggingLogo = false;
                if (sender is FrameworkElement element)
                {
                    element.ReleaseMouseCapture();
                }
                if (DataContext is MainViewModel vm) vm.CommitChangeCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}

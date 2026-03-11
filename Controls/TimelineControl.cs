using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VideoEditor.Models;
using VideoEditor.ViewModels;

namespace VideoEditor.Controls
{
    /// <summary>
    /// A data-driven custom timeline control that renders tracks and clips
    /// using direct drawing instructions (OnRender) for high performance and precision.
    /// </summary>
    public class TimelineControl : FrameworkElement
    {
        #region Constants & Metrics
        private const double HeaderHeight = 30.0;
        private const double TrackHeight = 70.0;
        private const double ClipHeight = 50.0;
        private const double TrackHeaderWidth = 100.0; // Left side label area
        private const double TrackGap = 5.0;
        private const double ClipPaddingY = 10.0;
        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty TracksProperty =
            DependencyProperty.Register("Tracks", typeof(ObservableCollection<TimelineTrack>), typeof(TimelineControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnTracksChanged));

        private static void OnTracksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineControl tc)
            {
                if (e.OldValue is ObservableCollection<TimelineTrack> oldList)
                {
                    oldList.CollectionChanged -= tc.OnTracksCollectionChanged;
                }
                if (e.NewValue is ObservableCollection<TimelineTrack> newList)
                {
                    newList.CollectionChanged += tc.OnTracksCollectionChanged;
                }
            }
        }

        private void OnTracksCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }

        public ObservableCollection<TimelineTrack> Tracks
        {
            get { return (ObservableCollection<TimelineTrack>)GetValue(TracksProperty); }
            set { SetValue(TracksProperty, value); }
        }

        public static readonly DependencyProperty PixelsPerSecondProperty =
            DependencyProperty.Register("PixelsPerSecond", typeof(double), typeof(TimelineControl),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double PixelsPerSecond
        {
            get { return (double)GetValue(PixelsPerSecondProperty); }
            set { SetValue(PixelsPerSecondProperty, value); }
        }

        public static readonly DependencyProperty PlayheadPositionProperty =
            DependencyProperty.Register("PlayheadPosition", typeof(TimeSpan), typeof(TimelineControl),
                new FrameworkPropertyMetadata(TimeSpan.Zero, 
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPlayheadPositionChanged));

        private static void OnPlayheadPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineControl tc)
            {
                tc.InvalidateVisual();
            }
        }

        public TimeSpan PlayheadPosition
        {
            get { return (TimeSpan)GetValue(PlayheadPositionProperty); }
            set { SetValue(PlayheadPositionProperty, value); }
        }

        public static readonly DependencyProperty SelectedClipProperty =
            DependencyProperty.Register("SelectedClip", typeof(TimelineClip), typeof(TimelineControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public TimelineClip? SelectedClip
        {
            get { return (TimelineClip)GetValue(SelectedClipProperty); }
            set { SetValue(SelectedClipProperty, value); }
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register("VerticalOffset", typeof(double), typeof(TimelineControl),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        // Routed Event for track context menu
        public static readonly RoutedEvent TrackContextMenuRequestedEvent =
            EventManager.RegisterRoutedEvent("TrackContextMenuRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TimelineControl));

        public event RoutedEventHandler TrackContextMenuRequested
        {
            add { AddHandler(TrackContextMenuRequestedEvent, value); }
            remove { RemoveHandler(TrackContextMenuRequestedEvent, value); }
        }

        #endregion

        #region Internal State
        
        private TimelineClip? _draggedClip;
        private Point _dragStartPoint;
        private bool _isDragging;
        private TimelineTrack? _draggedClipTrack; // Source Track
        private TimelineTrack? _hoverTrack;       // Target Track (Visual Feedback)
        private double _dragOffsetX;
        private TimelineTrack? _draggingVolumeTrack;
        private bool _isDraggingPlayhead;
        private const double PlayheadHitTolerance = 8.0;
        private const double ResizeEdgeTolerance = 10.0;
        private enum DragMode { Move, ResizeLeft, ResizeRight }
        private DragMode _currentDragMode = DragMode.Move;
        private TimeSpan _origDuration;
        private TimeSpan _origStartTime;

        // Pens and Brushes (cached for performance)
        private static readonly Pen RulerPen = new Pen(Brushes.Gray, 1.0);
        private static readonly Pen PlayheadPen = new Pen(Brushes.Red, 1.0);
        private static readonly Pen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(63, 63, 70)), 1.0);
        private static readonly Brush TrackBackgroundBrush = new SolidColorBrush(Color.FromRgb(37, 37, 38)); // #252526
        private static readonly Brush HeaderBackgroundBrush = new SolidColorBrush(Color.FromRgb(45, 45, 48)); // #2D2D30
        private static readonly Brush TextBrush = Brushes.White;
        private static readonly Brush ClipNormalBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // #007ACC
        private static readonly Brush ClipSelectedBrush = new SolidColorBrush(Color.FromRgb(28, 151, 234)); // Highlight
        private static readonly Typeface DefaultTypeface = new Typeface("Segoe UI");

        #endregion

        public TimelineControl()
        {
            // Enable mouse input
            this.Focusable = true;
            this.ClipToBounds = true;
        }

        #region Layout & Rendering

        protected override Size MeasureOverride(Size availableSize)
        {
            // Calculate desired size based on tracks and duration
            double height = HeaderHeight;
            if (Tracks != null)
            {
                height += Tracks.Count * (TrackHeight + TrackGap);
            }
            
            // Width is indeterminate in scroll viewer, typically bound
            // but we request at least 2000 or calculated width
            double width = 2000; 
            
            // If inside scrollviewer, we want to grow with content
            // We use standard size for now, ViewModel sets container size
            
            return new Size(width, height);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // Draw Background
            dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (Tracks != null) 
            {
                double currentY = HeaderHeight;

                // Draw Tracks
                foreach (var track in Tracks)
                {
                    DrawTrack(dc, track, currentY);
                    currentY += TrackHeight + TrackGap;
                }
            }

            // Draw Time Ruler (Sticky at VerticalOffset)
            DrawRuler(dc);

            // Draw Playhead Line (on top of tracks)
            DrawPlayheadLine(dc);

            // Draw Playhead Handle (Sticky at VerticalOffset)
            DrawPlayheadHandle(dc);
        }

        private void DrawRuler(DrawingContext dc)
        {
            var headerRect = new Rect(0, VerticalOffset, ActualWidth, HeaderHeight);
            dc.DrawRectangle(HeaderBackgroundBrush, null, headerRect);

            // Time marks
            double stepSeconds = 1.0; 
            if (PixelsPerSecond < 5) stepSeconds = 5.0;
            if (PixelsPerSecond < 1) stepSeconds = 10.0;

            for (double t = 0; t * PixelsPerSecond < ActualWidth; t += stepSeconds)
            {
                double x = TrackHeaderWidth + (t * PixelsPerSecond);
                dc.DrawLine(RulerPen, new Point(x, VerticalOffset + 20), new Point(x, VerticalOffset + HeaderHeight));

                if (t % 5 == 0) // Major tick every 5 secs
                {
                    dc.DrawLine(RulerPen, new Point(x, VerticalOffset + 10), new Point(x, VerticalOffset + HeaderHeight));
                    var text = new FormattedText(TimeSpan.FromSeconds(t).ToString(@"mm\:ss"),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, DefaultTypeface, 10, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    dc.DrawText(text, new Point(x + 2, VerticalOffset + 5));
                }
            }
        }

        private void DrawTrack(DrawingContext dc, TimelineTrack track, double y)
        {
            // Draw Track Header (Left)
            var headerRect = new Rect(0, y, TrackHeaderWidth, TrackHeight);
            dc.DrawRectangle(HeaderBackgroundBrush, BorderPen, headerRect);
            
            // Draw Icon based on type
            // Video: 🎬 (Film strip), Audio: 🎵 (Note)
            string icon = track.TrackType == MediaType.Video ? "🎬" : (track.TrackType == MediaType.Audio ? "🎵" : "📄");
            // Or better, draw simple geometry
            if (track.TrackType == MediaType.Video)
            {
                // Draw Film Strip Icon
                dc.DrawRectangle(Brushes.LightSkyBlue, null, new Rect(10, y + 15, 16, 12));
                dc.DrawRectangle(Brushes.Black, null, new Rect(10, y + 17, 16, 8)); // cutout
                dc.DrawGeometry(Brushes.White, null, Geometry.Parse($"M 18,{y+19} L 22,{y+22} L 18,{y+25} Z")); // Play triangle
            }
            else if (track.TrackType == MediaType.Audio)
            {
                // Draw Note Icon
                dc.DrawGeometry(Brushes.LightGreen, null, Geometry.Parse($"M 15,{y+25} Q 15,{y+25} 15,{y+15} L 15,{y+12} L 25,{y+12} L 25,{y+22} M 15,{y+25} A 3,3 0 1 1 18,{y+25}"));
                var noteText = new FormattedText("🎵", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, DefaultTypeface, 14, Brushes.LightGreen, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(noteText, new Point(10, y + 12));
            }

            // Draw track name with improved label
            // "Video Track 1", "Audio Track 2"
            string typeLabel = track.TrackType == MediaType.Video ? "Video Track" : "Audio Track";
            string displayName = string.IsNullOrEmpty(track.Name) || track.Name.StartsWith("track_") ? typeLabel : track.Name;
            
            var nameText = new FormattedText(displayName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, DefaultTypeface, 11, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            nameText.MaxTextWidth = TrackHeaderWidth - 35; // Allow space for icon
            nameText.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(nameText, new Point(35, y + 5));

            // Draw Mute Button
            double muteX = 75;
            double muteY = y + 35;
            string muteIcon = track.IsMuted ? "🔇" : "🔊";
            var muteText = new FormattedText(muteIcon,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, DefaultTypeface, 12, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(muteText, new Point(muteX, muteY));

            // Draw Volume Bar
            double barX = 10;
            double barY = y + 42;
            double barWidth = 60;
            double barHeight = 4;
            dc.DrawRectangle(Brushes.DimGray, null, new Rect(barX, barY, barWidth, barHeight));
            dc.DrawRectangle(track.TrackType == MediaType.Audio ? Brushes.LightGreen : Brushes.DodgerBlue, 
                             null, new Rect(barX, barY, barWidth * track.Volume, barHeight));
            dc.DrawEllipse(Brushes.White, null, new Point(barX + barWidth * track.Volume, barY + (barHeight / 2)), 3, 3);

            // Draw Track Lane
            var trackRect = new Rect(TrackHeaderWidth, y, Math.Max(0, ActualWidth - TrackHeaderWidth), TrackHeight);
            
            // Highlight if dragging over
            if (_hoverTrack == track && _isDragging)
            {
                // Check compatibility for highlight color
                bool compatible = true;
                if (_draggedClip != null && _draggedClip.MediaItem != null)
                {
                    if (_draggedClip.MediaItem.Type == MediaType.Video && track.TrackType == MediaType.Audio) compatible = false;
                    if (_draggedClip.MediaItem.Type == MediaType.Audio && track.TrackType == MediaType.Video) compatible = false;
                }
                
                dc.DrawRectangle(compatible ? new SolidColorBrush(Color.FromArgb(30, 0, 255, 0)) : new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)), 
                                 BorderPen, trackRect);
            }
            else
            {
                dc.DrawRectangle(TrackBackgroundBrush, BorderPen, trackRect);
            }

            // Draw Clips
            if (track.Clips != null)
            {
                foreach (var clip in track.Clips)
                {
                    DrawClip(dc, clip, y);
                }
            }
        }

        private void DrawClip(DrawingContext dc, TimelineClip clip, double trackY)
        {
            double x = TrackHeaderWidth + (clip.StartTime.TotalSeconds * PixelsPerSecond);
            double w = clip.Duration.TotalSeconds * PixelsPerSecond;
            double y = trackY + ClipPaddingY;
            double h = ClipHeight;

            var clipRect = new Rect(x, y, w, h);
            
            // Check visibility optimization
            if (x > ActualWidth || x + w < 0) return;

            var brush = (clip == SelectedClip) ? ClipSelectedBrush : ClipNormalBrush;
            
            // Clip Body
            // FIXED: Using DrawRoundedRectangle
            dc.DrawRoundedRectangle(brush, new Pen(Brushes.White, 0.5), clipRect, 2, 2);

            // Clip Text
            if (clip.MediaItem != null && w > 20)
            {
                var text = new FormattedText(clip.MediaItem.Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, DefaultTypeface, 10, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                
                text.MaxTextWidth = w - 4;
                text.MaxTextHeight = h - 4;
                
                // Clip text
                dc.PushClip(new RectangleGeometry(clipRect));
                dc.DrawText(text, new Point(x + 4, y + 4));
                dc.Pop();
            }
            
            // Selection Highlight Logic handled by coloring above
        }

        private void DrawPlayheadLine(DrawingContext dc)
        {
            double x = TrackHeaderWidth + (PlayheadPosition.TotalSeconds * PixelsPerSecond);
            if (x >= TrackHeaderWidth && x < ActualWidth)
            {
                dc.DrawLine(PlayheadPen, new Point(x, 0), new Point(x, ActualHeight));
            }
        }

        private void DrawPlayheadHandle(DrawingContext dc)
        {
            double x = TrackHeaderWidth + (PlayheadPosition.TotalSeconds * PixelsPerSecond);
            if (x >= TrackHeaderWidth && x < ActualWidth)
            {
                // Playhead Handle (Sticky at VerticalOffset)
                dc.DrawGeometry(Brushes.Red, null, Geometry.Parse($"M {x},{VerticalOffset} L {x-5},{VerticalOffset} L {x-5},{VerticalOffset+15} L {x},{VerticalOffset+25} L {x+5},{VerticalOffset+15} L {x+5},{VerticalOffset} Z"));
            }
        }

        #endregion

        #region Interaction Logic

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            
            Point p = e.GetPosition(this);
            _dragStartPoint = p;
            _dragOffsetX = 0; // Initialize
            
            // 0. Check Playhead Hit (Top Priority if grabbing the line)
            // Calculate current playhead X
            double playheadX = TrackHeaderWidth + (PlayheadPosition.TotalSeconds * PixelsPerSecond);
            
            // If clicking near playhead line (and not on a clip resize handle - simpler logic for now)
            if (Math.Abs(p.X - playheadX) <= PlayheadHitTolerance)
            {
                _isDraggingPlayhead = true;
                Mouse.Capture(this);
                return;
            }

            // 1. Check if clicking on timeline Ruler (Seeking)
            if (p.Y >= VerticalOffset && p.Y < VerticalOffset + HeaderHeight && p.X > TrackHeaderWidth)
            {
                double seconds = (p.X - TrackHeaderWidth) / PixelsPerSecond;
                PlayheadPosition = TimeSpan.FromSeconds(Math.Max(0, seconds));
                _isDraggingPlayhead = true; // Allow dragging from ruler too
                Mouse.Capture(this);
                return;
            }

            // 1.5 Check if clicking on Track Header (Left Side)
            double currentY = HeaderHeight;
            if (Tracks != null)
            {
                foreach (var track in Tracks)
                {
                    if (p.Y >= currentY && p.Y <= currentY + TrackHeight && p.X < TrackHeaderWidth)
                    {
                        // Inside track header - Check Mute Button (Updated coordinates)
                        if (p.X >= 75 && p.X <= 95 && p.Y >= currentY + 35 && p.Y <= currentY + 55)
                        {
                            track.IsMuted = !track.IsMuted;
                            InvalidateVisual();
                            return;
                        }

                        // Check Volume Slider (Updated coordinates)
                        if (p.X >= 10 && p.X <= 70 && p.Y >= currentY + 35 && p.Y <= currentY + 50)
                        {
                            _draggingVolumeTrack = track;
                            UpdateVolumeFromMouse(p.X);
                            Mouse.Capture(this);
                            return;
                        }

                        // If not clicking controls, Start Track Drag (Reorder)
                        _draggedClipTrack = track; // Reusing field to store track being dragged
                        _isDragging = false; // Will trigger on move
                        _dragStartPoint = p;
                        Mouse.Capture(this);
                        return;
                    }
                    currentY += TrackHeight + TrackGap;
                }
            }

            // 2. Check Hit Test for Clips
            TimelineClip? hitClip = null;
            TimelineTrack? hitTrack = null;
            
            currentY = HeaderHeight;
            if (Tracks != null)
            {
                foreach (var track in Tracks)
                {
                    if (p.Y >= currentY && p.Y <= currentY + TrackHeight)
                    {
                        // Inside this track
                        double trackRelativeX = p.X - TrackHeaderWidth;
                        
                        foreach(var clip in track.Clips)
                        {
                            double clipX = clip.StartTime.TotalSeconds * PixelsPerSecond;
                            double clipW = clip.Duration.TotalSeconds * PixelsPerSecond;
                            
                            // Hit test
                            if (trackRelativeX >= clipX && trackRelativeX <= clipX + clipW)
                            {
                                hitClip = clip;
                                hitTrack = track;
                                break;
                            }
                        }
                        break;
                    }
                    currentY += TrackHeight + TrackGap;
                }
            }

            if (hitClip != null)
            {
                SelectedClip = hitClip;
                _draggedClip = hitClip;
                _draggedClipTrack = hitTrack;
                _isDragging = false;
                _dragOffsetX = p.X - (TrackHeaderWidth + (hitClip.StartTime.TotalSeconds * PixelsPerSecond));
                
                // Determine if we are resizing
                double clipX = TrackHeaderWidth + (hitClip.StartTime.TotalSeconds * PixelsPerSecond);
                double clipW = hitClip.Duration.TotalSeconds * PixelsPerSecond;
                
                if (p.X <= clipX + ResizeEdgeTolerance)
                {
                    _currentDragMode = DragMode.ResizeLeft;
                }
                else if (p.X >= clipX + clipW - ResizeEdgeTolerance)
                {
                    _currentDragMode = DragMode.ResizeRight;
                }
                else
                {
                    _currentDragMode = DragMode.Move;
                }
                
                _origDuration = hitClip.Duration;
                _origStartTime = hitClip.StartTime;

                InvalidateVisual(); // Redraw selection
                Mouse.Capture(this);
                return;
            }
            else
            {
                SelectedClip = null; 
                
                // Clicking on empty space moves playhead
                if (p.X > TrackHeaderWidth && p.Y > VerticalOffset + HeaderHeight)
                {
                    double seconds = (p.X - TrackHeaderWidth) / PixelsPerSecond;
                    PlayheadPosition = TimeSpan.FromSeconds(Math.Max(0, seconds));
                    _isDraggingPlayhead = true; 
                }

                InvalidateVisual();
                Mouse.Capture(this);
                return;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Zooming
                double delta = e.Delta > 0 ? 1.2 : 0.8;
                PixelsPerSecond = Math.Max(1.0, Math.Min(200.0, PixelsPerSecond * delta));
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (Mouse.Captured == this)
            {
                Point p = e.GetPosition(this);
                
                // If dragging volume
                if (_draggingVolumeTrack != null)
                {
                    UpdateVolumeFromMouse(p.X);
                    return;
                }

                // If dragging playhead (from ruler OR anywhere if initiated)
                if (_isDraggingPlayhead)
                {
                    if (p.X > TrackHeaderWidth)
                    {
                         double seconds = (p.X - TrackHeaderWidth) / PixelsPerSecond;
                         PlayheadPosition = TimeSpan.FromSeconds(Math.Max(0, seconds));
                    }
                    return; // Playhead drag overrides other drags
                }

                // Track Reordering Logic
                if (_draggedClipTrack != null && _draggedClip == null && Mouse.Captured == this)
                {
                    // dragging a TRACK, not a clip
                     if (Math.Abs(p.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                     {
                         // Find new index based on Y
                         double y = HeaderHeight;
                         int newIndex = -1;
                         for(int i=0; i<Tracks.Count; i++) 
                         {
                             if (p.Y >= y && p.Y <= y + TrackHeight + TrackGap)
                             {
                                 newIndex = i;
                                 break;
                             }
                             y += TrackHeight + TrackGap;
                         }

                         if (newIndex != -1 && newIndex != Tracks.IndexOf(_draggedClipTrack))
                         {
                             int oldIndex = Tracks.IndexOf(_draggedClipTrack);
                             Tracks.Move(oldIndex, newIndex);
                             InvalidateVisual();
                         }
                     }
                     return;
                }

                // Dragging Clip Logic
                if (_draggedClip != null)
                {
                    if (!_isDragging && (Math.Abs(p.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance))
                    {
                        _isDragging = true;
                    }

                    if (_isDragging)
                    {
                        double deltaX = p.X - _dragStartPoint.X;
                        double deltaSecs = deltaX / PixelsPerSecond;

                        if (_currentDragMode == DragMode.Move)
                        {
                            // Move logic is currently handled by the UI/ViewModel reordering or 
                            // we can implement simple time-shifting here if supported.
                            // For now, let's keep the reorder hover logic:
                            double currentYOffset = HeaderHeight;
                            foreach (var track in Tracks)
                            {
                                if (p.Y >= currentYOffset && p.Y <= currentYOffset + TrackHeight)
                                {
                                    if (_hoverTrack != track)
                                    {
                                        _hoverTrack = track;
                                        InvalidateVisual();
                                    }
                                    break;
                                }
                                currentYOffset += TrackHeight + TrackGap;
                            }
                        }
                        else if (_currentDragMode == DragMode.ResizeRight)
                        {
                            double newDur = Math.Max(0.1, _origDuration.TotalSeconds + deltaSecs);
                            _draggedClip.Duration = TimeSpan.FromSeconds(newDur);
                            InvalidateVisual();
                        }
                        else if (_currentDragMode == DragMode.ResizeLeft)
                        {
                            double rightEdgeTime = _origStartTime.TotalSeconds + _origDuration.TotalSeconds;
                            double newStart = _origStartTime.TotalSeconds + deltaSecs;
                            
                            // Clamp start to 0 or right edge (min duration)
                            if (newStart < 0) newStart = 0;
                            if (newStart > rightEdgeTime - 0.1) newStart = rightEdgeTime - 0.1;
                            
                            double newDur = rightEdgeTime - newStart;
                            
                            _draggedClip.StartTime = TimeSpan.FromSeconds(newStart);
                            _draggedClip.Duration = TimeSpan.FromSeconds(newDur);
                            InvalidateVisual();
                        }
                    }
                }
                else
                {
                    // Update Cursor based on hover
                    HitTestAndSetCursor(p);
                }
            }
            else
            {
                // Not captured, still update cursor on hover
                HitTestAndSetCursor(e.GetPosition(this));
            }
        }

        private void HitTestAndSetCursor(Point p)
        {
            if (Tracks == null) return;

            double currentY = HeaderHeight;
            foreach (var track in Tracks)
            {
                if (p.Y >= currentY && p.Y <= currentY + TrackHeight)
                {
                    double trackRelativeX = p.X - TrackHeaderWidth;
                    foreach (var clip in track.Clips)
                    {
                        double clipX = clip.StartTime.TotalSeconds * PixelsPerSecond;
                        double clipW = clip.Duration.TotalSeconds * PixelsPerSecond;

                        if (trackRelativeX >= clipX && trackRelativeX <= clipX + clipW)
                        {
                            if (trackRelativeX <= clipX + ResizeEdgeTolerance || trackRelativeX >= clipX + clipW - ResizeEdgeTolerance)
                            {
                                this.Cursor = Cursors.SizeWE;
                                return;
                            }
                            this.Cursor = Cursors.Hand;
                            return;
                        }
                    }
                }
                currentY += TrackHeight + TrackGap;
            }
            
            // Playhead or Ruler
            double playheadX = TrackHeaderWidth + (PlayheadPosition.TotalSeconds * PixelsPerSecond);
            if (Math.Abs(p.X - playheadX) <= PlayheadHitTolerance || (p.Y >= VerticalOffset && p.Y < VerticalOffset + HeaderHeight && p.X > TrackHeaderWidth))
            {
                this.Cursor = Cursors.SizeWE;
                return;
            }

            this.Cursor = Cursors.Arrow;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            this.ReleaseMouseCapture();
            
            _isDraggingPlayhead = false; // Reset playhead drag state

            if (_isDragging && _draggedClip != null && _draggedClipTrack != null && _currentDragMode == DragMode.Move)
            {
                Point p = e.GetPosition(this);
                
                // Determine Target Track based on drop Y
                TimelineTrack? targetTrack = null;
                double currentY = HeaderHeight;
                if (Tracks != null)
                {
                    foreach (var track in Tracks)
                    {
                        if (p.Y >= currentY && p.Y <= currentY + TrackHeight)
                        {
                            targetTrack = track;
                            break;
                        }
                        currentY += TrackHeight + TrackGap;
                    }
                }

                double dropX = p.X - TrackHeaderWidth;
                double dropTime = Math.Max(0, dropX / PixelsPerSecond);

                // If no target track found (dropped outside), stay on current track
                if (targetTrack == null) targetTrack = _draggedClipTrack;

                HandleClipDrop(_draggedClip, _draggedClipTrack, targetTrack, dropTime);
            }

            _isDragging = false;
            _draggedClip = null;
            _draggedClipTrack = null;
            _hoverTrack = null;
            _draggingVolumeTrack = null;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            
            Point p = e.GetPosition(this);
            
            // Check if right-clicking on track header
            double currentY = HeaderHeight;
            if (Tracks != null)
            {
                foreach (var track in Tracks)
                {
                    if (p.Y >= currentY && p.Y <= currentY + TrackHeight && p.X < TrackHeaderWidth)
                    {
                        // Right-clicked on track header - show context menu
                        var contextMenu = new ContextMenu();
                        
                        var deleteItem = new MenuItem { Header = "Delete Track" };
                        deleteItem.Click += (s, args) =>
                        {
                            // Raise event or command - we'll handle via the DataContext binding
                            RaiseEvent(new RoutedEventArgs(TrackContextMenuRequestedEvent, track));
                        };
                        contextMenu.Items.Add(deleteItem);
                        
                        contextMenu.IsOpen = true;
                        e.Handled = true;
                        return;
                    }
                    currentY += TrackHeight + TrackGap;
                }
            }

            // Check if right-clicking on a clip
            TimelineClip? hitClip = null;
            double clipY = HeaderHeight;
            if (Tracks != null)
            {
                foreach (var track in Tracks)
                {
                    if (p.Y >= clipY && p.Y <= clipY + TrackHeight)
                    {
                        double trackRelativeX = p.X - TrackHeaderWidth;
                        foreach (var clip in track.Clips)
                        {
                            double clipX = clip.StartTime.TotalSeconds * PixelsPerSecond;
                            double clipW = clip.Duration.TotalSeconds * PixelsPerSecond;
                            if (trackRelativeX >= clipX && trackRelativeX <= clipX + clipW)
                            {
                                hitClip = clip;
                                break;
                            }
                        }
                        break;
                    }
                    clipY += TrackHeight + TrackGap;
                }
            }

            if (hitClip != null)
            {
                var contextMenu = new ContextMenu();
                
                // Only show extend option for visual items
                if (hitClip.MediaItem != null && (hitClip.MediaItem.Type == MediaType.Video || hitClip.MediaItem.Type == MediaType.Image))
                {
                    var extendItem = new MenuItem { Header = "Extend to End of Audio" };
                    extendItem.Click += (s, args) =>
                    {
                        if (DataContext is MainViewModel vm)
                        {
                            vm.ExtendClipToEndOfAudioCommand.Execute(hitClip);
                        }
                    };
                    contextMenu.Items.Add(extendItem);
                }

                var deleteItem = new MenuItem { Header = "Delete Clip" };
                deleteItem.Click += (s, args) =>
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.RemoveClipCommand.Execute(hitClip);
                    }
                };
                contextMenu.Items.Add(deleteItem);

                contextMenu.IsOpen = true;
                e.Handled = true;
                return;
            }
        }

        private void UpdateVolumeFromMouse(double mouseX)
        {
            if (_draggingVolumeTrack == null) return;
            double barX = 10;
            double barWidth = 60;
            double relativeX = mouseX - barX;
            double volume = Math.Max(0, Math.Min(1, relativeX / barWidth));
            _draggingVolumeTrack.Volume = volume;
            InvalidateVisual();
        }

        private void HandleClipDrop(TimelineClip? clip, TimelineTrack? sourceTrack, TimelineTrack? targetTrack, double dropTime)
        {
            if (clip == null || sourceTrack == null || targetTrack == null) return;

            // 1. Validate Track Compatibility
            if (clip.MediaItem != null)
            {
                bool isVisual = clip.MediaItem.Type == MediaType.Video || clip.MediaItem.Type == MediaType.Image;
                bool isAudio = clip.MediaItem.Type == MediaType.Audio;

                if (targetTrack.TrackType == MediaType.Audio && isVisual) 
                {
                    System.Diagnostics.Debug.WriteLine("Cannot drop video on audio track");
                    return; 
                }
                if (targetTrack.TrackType == MediaType.Video && isAudio)
                {
                     System.Diagnostics.Debug.WriteLine("Cannot drop audio on video track");
                    return;
                }
            }

            // 2. Free Placement Logic
            TimeSpan newStartTime = TimeSpan.FromSeconds(dropTime);
            
            // Optional: Snap to nearby clips (magnetic timeline) if needed
            // For now, raw free placement as requested.

            // 3. Move Logic
            if (sourceTrack == targetTrack)
            {
                // Just moving in time on same track
                clip.StartTime = newStartTime;
                targetTrack.SortClips(); 
            }
            else
            {
                // Move track to track
                
                // Remove from old (Ripple = false to leave gap? Or true to close it? 
                // For "Free placed manually", usually we don't auto-close gaps unexpectedly, 
                // but default NLE behavior varies. Let's use Ripple=false for true free editing).
                sourceTrack.RemoveClip(clip, false); 

                // Add to new at specific time
                targetTrack.AddClipAt(clip, newStartTime);
            }

            InvalidateVisual();
        }

        #endregion
    }
}

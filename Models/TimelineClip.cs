using System;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents a clip placed on the timeline
    /// </summary>
    public class TimelineClip
    {
        public Guid Id { get; set; }
        public Guid MediaItemId { get; set; }
        public string TrackId { get; set; }
        
        // Timeline positioning
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int Order { get; set; } // Order within the track (0-based)
        
        // Trimming (which part of the source media to use)
        public TimeSpan TrimStart { get; set; }
        public TimeSpan TrimEnd { get; set; }
        
        // Audio controls
        public bool IsMuted { get; set; }
        public double Volume { get; set; } // 0.0 to 1.0
        
        // Enable/Disable
        public bool IsEnabled { get; set; }
        
        // Reference to the actual media
        public MediaItem? MediaItem { get; set; }

        public TimelineClip()
        {
            Id = Guid.NewGuid();
            TrackId = string.Empty;
            Volume = 1.0;
            IsEnabled = true;
            Order = 0;
        }

        // Computed properties
        public TimeSpan EndTime => StartTime + Duration;
        
        // Compute the duration that should be rendered
        public TimeSpan EffectiveDuration => Duration;
    }
}

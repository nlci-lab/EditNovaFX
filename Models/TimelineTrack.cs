using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents a track on the timeline (video, audio, or subtitle)
    /// </summary>
    public class TimelineTrack
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public MediaType TrackType { get; set; }
        public int Order { get; set; } // Track order (0 = top, higher = bottom)
        public bool IsLocked { get; set; }
        public bool IsMuted { get; set; }
        public bool IsEnabled { get; set; }
        public double Volume { get; set; } = 1.0;
        
        public List<TimelineClip> Clips { get; set; }

        public TimelineTrack(string id, string name, MediaType trackType)
        {
            Id = id;
            Name = name;
            TrackType = trackType;
            Clips = new List<TimelineClip>();
            IsEnabled = true;
            Order = 0;
        }

        /// <summary>
        /// Get clips sorted by order
        /// </summary>
        public List<TimelineClip> GetOrderedClips()
        {
            return Clips.OrderBy(c => c.Order).ToList();
        }

        /// <summary>
        /// Recalculate start times based on sequential placement
        /// </summary>
        public void RecalculateStartTimes()
        {
            var orderedClips = GetOrderedClips();
            TimeSpan currentTime = TimeSpan.Zero;

            for (int i = 0; i < orderedClips.Count; i++)
            {
                orderedClips[i].StartTime = currentTime;
                orderedClips[i].Order = i;
                currentTime += orderedClips[i].Duration;
            }
        }

        /// <summary>
        /// Get the end time of the last clip
        /// </summary>
        public TimeSpan GetEndTime()
        {
            if (Clips.Count == 0) return TimeSpan.Zero;
            var orderedClips = GetOrderedClips();
            var lastClip = orderedClips.LastOrDefault();
            return lastClip?.EndTime ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Add clip at a specific time (Free Placement)
        /// </summary>
        public void AddClipAt(TimelineClip clip, TimeSpan startTime)
        {
             // Validate compatibility
            if (clip.MediaItem != null && TrackType != MediaType.Universal)
            {
                 bool isVisual = clip.MediaItem.Type == MediaType.Video || clip.MediaItem.Type == MediaType.Image;
                 bool isAudio = clip.MediaItem.Type == MediaType.Audio;
                 
                 if (TrackType == MediaType.Audio && isVisual) return;
                 if (TrackType == MediaType.Video && isAudio) return;
            }

            clip.StartTime = startTime;
            clip.TrackId = Id;
            
            Clips.Add(clip);
            SortClips();
        }

        public void SortClips()
        {
            Clips.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            
            // Re-assign Order index based on time
            for(int i=0; i<Clips.Count; i++)
            {
                Clips[i].Order = i;
            }
        }

        /// <summary>
        /// Add clip sequentially (after the last clip)
        /// </summary>
        public void AddClipSequentially(TimelineClip clip)
        {
            // Validate compatibility
            if (clip.MediaItem != null && TrackType != MediaType.Universal)
            {
                 bool isVisual = clip.MediaItem.Type == MediaType.Video || clip.MediaItem.Type == MediaType.Image;
                 bool isAudio = clip.MediaItem.Type == MediaType.Audio;
                 
                 if (TrackType == MediaType.Audio && isVisual) return;
                 if (TrackType == MediaType.Video && isAudio) return;
            }

            // Append to end
            clip.StartTime = GetEndTime();
            clip.TrackId = Id;
            
            Clips.Add(clip);
            SortClips();
        }

        /// <summary>
        /// Remove clip and optionally ripple (shift subsequent clips)
        /// </summary>
        public void RemoveClip(TimelineClip clip, bool ripple = true)
        {
            Clips.Remove(clip);
            if (ripple)
            {
                RecalculateStartTimes();
            }
        }

        /// <summary>
        /// Move clip to new order position (Legacy/Sequential mode)
        /// </summary>
        public void MoveClip(TimelineClip clip, int newOrder, bool ripple = true)
        {
            // For free placement, we don't use Order index to move. 
            // We use StartTime. This method is kept for legacy ripple behavior.
            
            if (newOrder < 0 || newOrder >= Clips.Count) return;
            
            var oldOrder = clip.Order;
            clip.Order = newOrder;

            // Adjust other clips' orders
            foreach (var c in Clips.Where(c => c.Id != clip.Id))
            {
                if (oldOrder < newOrder)
                {
                    // Moving forward
                    if (c.Order > oldOrder && c.Order <= newOrder)
                        c.Order--;
                }
                else
                {
                    // Moving backward
                    if (c.Order >= newOrder && c.Order < oldOrder)
                        c.Order++;
                }
            }

            SortClipsByOrder();

            if (ripple)
            {
                RecalculateStartTimes();
            }
        }

        private void SortClipsByOrder()
        {
             Clips.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}

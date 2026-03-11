using System;
using System.Collections.Generic;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents the entire video editing project
    /// </summary>
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string LastExportedFilePath { get; set; } // Track the last rendered video file
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        
        // Project settings
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public double OutputFrameRate { get; set; }
        public string OutputFormat { get; set; }
        
        // Logo Setting
        public string? LogoPath { get; set; }
        public int LogoX { get; set; } = 10;
        public int LogoY { get; set; } = 10;
        public double LogoScale { get; set; } = 1.0;
        
        // Media library
        public List<MediaItem> MediaItems { get; set; }
        
        // Timeline
        public List<TimelineTrack> Tracks { get; set; }
        
        // Subtitles
        public List<SubtitleTrack> SubtitleTracks { get; set; }
        
        // Timeline duration
        public TimeSpan Duration { get; set; }

        public Project()
        {
            Id = Guid.NewGuid();
            Name = "Untitled Project";
            FilePath = string.Empty;
            LastExportedFilePath = string.Empty;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            
            OutputWidth = 1920;
            OutputHeight = 1080;
            OutputFrameRate = 30.0;
            OutputFormat = "mp4";
            
            MediaItems = new List<MediaItem>();
            Tracks = new List<TimelineTrack>();
            SubtitleTracks = new List<SubtitleTrack>();
            
            InitializeDefaultTracks();
        }

        private void InitializeDefaultTracks()
        {
            // 2 Video tracks
            Tracks.Add(new TimelineTrack("video_1", "Video/Image", MediaType.Video));
            Tracks.Add(new TimelineTrack("video_2", "Video/Image", MediaType.Video));
            
            // 2 Audio tracks
            Tracks.Add(new TimelineTrack("audio_1", "Voice Over", MediaType.Audio));
            Tracks.Add(new TimelineTrack("audio_2", "Bg Audio", MediaType.Audio));
        }
    }
}

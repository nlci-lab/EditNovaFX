using System;
using System.Collections.Generic;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents a subtitle track with multiple subtitle entries
    /// </summary>
    public class SubtitleTrack
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Format { get; set; } // "SRT"
        
        public bool IsEnabled { get; set; }
        public TimeSpan TimeOffset { get; set; } // For adjusting subtitle timing
        
        public List<SubtitleEntry> Entries { get; set; }
        
        // Positioning
        public int GlobalMarginL { get; set; } = 10;
        public int GlobalMarginV { get; set; } = 10;
        public int Alignment { get; set; } = 2; // Alignment: 2 = Bottom Center
        public string FontName { get; set; } = "Nirmala UI";
        public int FontSize { get; set; } = 28;
        public string FontColor { get; set; } = "#FFFFFF";
        public double TextRegionWidth { get; set; } = 0; // 0 means auto/full
        
        // Advanced Styling
        public bool IsBold { get; set; } = true;
        public bool IsItalic { get; set; } = false;
        public string OutlineColor { get; set; } = "#000000";
        public double OutlineWidth { get; set; } = 2.0;
        public double ShadowWidth { get; set; } = 1.0;
        public string ShadowColor { get; set; } = "#000000";

        // Background Box
        public bool HasBackgroundBox { get; set; } = false;
        public string BackgroundBoxColor { get; set; } = "#000000";
        public double BackgroundBoxOpacity { get; set; } = 0.5;


        public SubtitleTrack()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            FilePath = string.Empty;
            Format = "SRT";
            IsEnabled = true;
            Entries = new List<SubtitleEntry>();
        }
    }
}

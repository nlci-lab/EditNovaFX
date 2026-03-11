using System;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents a single subtitle entry
    /// </summary>
    public class SubtitleEntry
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; }
        
        // For ASS format - preserve styling
        public string? Style { get; set; }
        public string? Actor { get; set; }
        public int MarginL { get; set; }
        public int MarginR { get; set; }
        public int MarginV { get; set; }
        public string? Effect { get; set; }

        public SubtitleEntry()
        {
            Text = string.Empty;
        }

        public TimeSpan Duration => EndTime - StartTime;
    }
}

using System;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents a Bible book with USFM code and chapter count.
    /// </summary>
    public class BibleBook
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // USFM ID (e.g., GEN, MRK)
        public int ChapterCount { get; set; }

        public override string ToString() => Name;
    }
}

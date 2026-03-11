using System;

namespace VideoEditor.Models
{
    /// <summary>
    /// Represents a media file (video, audio, image, or subtitle)
    /// </summary>
    public class MediaItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public MediaType Type { get; set; }
        public TimeSpan Duration { get; set; }
        
        // Video/Image specific
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        
        // Audio specific
        public bool HasAudio { get; set; }
        public int AudioChannels { get; set; }
        public int SampleRate { get; set; }

        public MediaItem()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
            FilePath = string.Empty;
        }

        public MediaItem(string filePath, MediaType type)
        {
            Id = Guid.NewGuid();
            FilePath = filePath;
            Type = type;
            Name = System.IO.Path.GetFileName(filePath);
        }
    }
}

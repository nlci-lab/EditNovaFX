namespace VideoEditor.Models
{
    /// <summary>
    /// Settings for video export/rendering
    /// </summary>
    public class ExportSettings
    {
        public string OutputPath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public string Format { get; set; }
        public int VideoBitrate { get; set; } // kbps
        public int AudioBitrate { get; set; } // kbps
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }

        public ExportSettings()
        {
            OutputPath = string.Empty;
            Width = 1920;
            Height = 1080;
            FrameRate = 30.0;
            Format = "mp4";
            VideoBitrate = 5000;
            AudioBitrate = 192;
            VideoCodec = "libx264";
            AudioCodec = "aac";
        }
    }
}

using System;
using System.Collections.Generic;

namespace VideoEditor.Models
{
    public class ExportPreset
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public int VideoBitrate { get; set; } // kbps
        public int AudioBitrate { get; set; } // kbps
        public string Icon { get; set; } = "🎬"; // For UI

        public string ResolutionLabel => $"{Width}x{Height}";

        public string AspectRatio
        {
            get
            {
                int gcd = GreatestCommonDivisor(Width, Height);
                int rw = gcd == 0 ? Width : Width / gcd;
                int rh = gcd == 0 ? Height : Height / gcd;
                return $"{rw}:{rh}";
            }
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }

        public static List<ExportPreset> GetDefaultPresets()
        {
            return new List<ExportPreset>
            {
                new ExportPreset
                {
                    Name = "YouTube Full HD",
                    Description = "Standard 16:9 High Quality (1080p)",
                    Width = 1920,
                    Height = 1080,
                    FrameRate = 30,
                    VideoBitrate = 8000,
                    AudioBitrate = 192,
                    Icon = "📺"
                },
                new ExportPreset
                {
                    Name = "YouTube 4K",
                    Description = "Ultra High Definition (2160p)",
                    Width = 3840,
                    Height = 2160,
                    FrameRate = 60,
                    VideoBitrate = 35000,
                    AudioBitrate = 320,
                    Icon = "💎"
                },
                new ExportPreset
                {
                    Name = "Instagram Reels / Shorts",
                    Description = "Vertical 9:16 optimized for mobile",
                    Width = 1080,
                    Height = 1920,
                    FrameRate = 30,
                    VideoBitrate = 5000,
                    AudioBitrate = 128,
                    Icon = "📱"
                },
                new ExportPreset
                {
                    Name = "Instagram Portrait",
                    Description = "Vertical 4:5 for social feed posts",
                    Width = 1080,
                    Height = 1350,
                    FrameRate = 30,
                    VideoBitrate = 4500,
                    AudioBitrate = 128,
                    Icon = "🖼️"
                },
                new ExportPreset
                {
                    Name = "WhatsApp / Telegram",
                    Description = "Small size, optimized for messaging",
                    Width = 1280,
                    Height = 720,
                    FrameRate = 24,
                    VideoBitrate = 2000,
                    AudioBitrate = 96,
                    Icon = "💬"
                },
                new ExportPreset
                {
                    Name = "Twitter (X) / Facebook",
                    Description = "Balanced for social newsfeeds",
                    Width = 1280,
                    Height = 720,
                    FrameRate = 30,
                    VideoBitrate = 3500,
                    AudioBitrate = 128,
                    Icon = "🌐"
                }
            };
        }
    }
}

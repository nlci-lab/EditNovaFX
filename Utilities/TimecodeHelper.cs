using System;

namespace VideoEditor.Utilities
{
    /// <summary>
    /// Helper class for timecode formatting and parsing
    /// </summary>
    public static class TimecodeHelper
    {
        /// <summary>
        /// Format TimeSpan as timecode string (HH:MM:SS:FF)
        /// </summary>
        public static string FormatTimecode(TimeSpan time, double frameRate = 30.0)
        {
            int hours = (int)time.TotalHours;
            int minutes = time.Minutes;
            int seconds = time.Seconds;
            int frames = (int)(time.Milliseconds / 1000.0 * frameRate);

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frames:D2}";
        }

        /// <summary>
        /// Parse timecode string to TimeSpan
        /// </summary>
        public static TimeSpan ParseTimecode(string timecode, double frameRate = 30.0)
        {
            var parts = timecode.Split(':');
            if (parts.Length != 4)
                return TimeSpan.Zero;

            if (!int.TryParse(parts[0], out int hours)) return TimeSpan.Zero;
            if (!int.TryParse(parts[1], out int minutes)) return TimeSpan.Zero;
            if (!int.TryParse(parts[2], out int seconds)) return TimeSpan.Zero;
            if (!int.TryParse(parts[3], out int frames)) return TimeSpan.Zero;

            int milliseconds = (int)(frames / frameRate * 1000);

            return new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }

        /// <summary>
        /// Convert TimeSpan to frame number
        /// </summary>
        public static long TimeToFrames(TimeSpan time, double frameRate)
        {
            return (long)(time.TotalSeconds * frameRate);
        }

        /// <summary>
        /// Convert frame number to TimeSpan
        /// </summary>
        public static TimeSpan FramesToTime(long frames, double frameRate)
        {
            return TimeSpan.FromSeconds(frames / frameRate);
        }

        /// <summary>
        /// Snap time to nearest frame
        /// </summary>
        public static TimeSpan SnapToFrame(TimeSpan time, double frameRate)
        {
            long frames = TimeToFrames(time, frameRate);
            return FramesToTime(frames, frameRate);
        }
    }
}

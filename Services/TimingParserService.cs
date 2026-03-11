using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VideoEditor.Services
{
    public class TimingEntry
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string SegmentId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service for parsing Timing files (.txt).
    /// </summary>
    public class TimingParserService
    {
        public string Separators { get; private set; } = string.Empty;
        /// <summary>
        /// Parses a timing file and returns entries.
        /// </summary>
        /// <param name="filePath">Path to the .txt timing file.</param>
        /// <returns>List of TimingEntry objects.</returns>
        public List<TimingEntry> ParseTiming(string filePath)
        {
            var entries = new List<TimingEntry>();
            if (!File.Exists(filePath)) return entries;

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("\\separators"))
                {
                    Separators = trimmed.Replace("\\separators", "").Trim();
                    continue;
                }

                if (trimmed.StartsWith("\\")) continue;

                // Typical line: 10.700   15.540    1
                // Split by tabs or multiple spaces
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length >= 3)
                {
                    string segmentId = parts[2];

                    // Rule 3: Ignore timing entries starting with s (section markers)
                    if (segmentId.StartsWith("s", StringComparison.OrdinalIgnoreCase) && 
                        segmentId.Length > 1 && char.IsDigit(segmentId[1]))
                    {
                        continue;
                    }

                    if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double start) &&
                        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double end))
                    {
                        entries.Add(new TimingEntry
                        {
                            StartTime = start,
                            EndTime = end,
                            SegmentId = segmentId
                        });
                    }
                }
            }

            return entries;
        }
    }
}

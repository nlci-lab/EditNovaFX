using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    /// <summary>
    /// Parses SRT subtitle files
    /// </summary>
    public class SubtitleParser
    {
        /// <summary>
        /// Parse a subtitle file and return a SubtitleTrack
        /// </summary>
        public static SubtitleTrack ParseSubtitleFile(string filePath)
        {
            var track = new SubtitleTrack
            {
                FilePath = filePath,
                Name = Path.GetFileName(filePath)
            };

            string extension = Path.GetExtension(filePath).ToLower();
            
            if (extension == ".srt")
            {
                track.Format = "SRT";
                track.Entries = ParseSRT(filePath);
            }
            else
            {
                throw new NotSupportedException($"Subtitle format {extension} is not supported. Only SRT is supported.");
            }

            return track;
        }

        /// <summary>
        /// Parse SRT subtitle format
        /// </summary>
        private static List<SubtitleEntry> ParseSRT(string filePath)
        {
            var entries = new List<SubtitleEntry>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            
            int i = 0;
            while (i < lines.Length)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    i++;
                    continue;
                }

                // Parse index
                if (!int.TryParse(lines[i].Trim(), out int index))
                {
                    i++;
                    continue;
                }

                i++;
                if (i >= lines.Length) break;

                // Parse timecode line: 00:00:20,000 --> 00:00:24,400
                var timeMatch = Regex.Match(lines[i], @"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})");
                if (!timeMatch.Success)
                {
                    i++;
                    continue;
                }

                var startTime = new TimeSpan(0,
                    int.Parse(timeMatch.Groups[1].Value),
                    int.Parse(timeMatch.Groups[2].Value),
                    int.Parse(timeMatch.Groups[3].Value),
                    int.Parse(timeMatch.Groups[4].Value));

                var endTime = new TimeSpan(0,
                    int.Parse(timeMatch.Groups[5].Value),
                    int.Parse(timeMatch.Groups[6].Value),
                    int.Parse(timeMatch.Groups[7].Value),
                    int.Parse(timeMatch.Groups[8].Value));

                i++;

                // Parse text (can be multiple lines)
                var textBuilder = new StringBuilder();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    textBuilder.AppendLine(lines[i]);
                    i++;
                }

                var entry = new SubtitleEntry
                {
                    Index = index,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = textBuilder.ToString().Trim()
                };

                entries.Add(entry);
            }

            return entries;
        }
    }
}

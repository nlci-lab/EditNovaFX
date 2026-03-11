using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoEditor.Services
{
    public class SrtWriterService
    {
        public enum GenerationMode
        {
            Verse,
            Segment,
            Phrase
        }

        public string GenerateSrtContent(List<TimingEntry> timingEntries, Dictionary<string, string> verseMap, GenerationMode mode = GenerationMode.Phrase, string separators = "")
        {
            if (mode == GenerationMode.Segment || mode == GenerationMode.Phrase)
            {
                return GenerateSegmentedSrtContent(timingEntries, verseMap, mode, separators);
            }

            StringBuilder sb = new StringBuilder();
            int index = 1;

            foreach (var entry in timingEntries)
            {
                // Skip section markers
                if (entry.SegmentId.StartsWith("s", StringComparison.OrdinalIgnoreCase)) continue;

                string? text = null;
                
                // For Verse mode, we always try to get the full verse
                var baseVerseMatch = Regex.Match(entry.SegmentId, @"^(\d+)");
                if (baseVerseMatch.Success)
                {
                    string baseVerse = baseVerseMatch.Groups[1].Value;
                    verseMap.TryGetValue(baseVerse, out text);
                }
                else
                {
                    verseMap.TryGetValue(entry.SegmentId, out text);
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(index.ToString());
                    sb.AppendLine($"{FormatTime(entry.StartTime)} --> {FormatTime(entry.EndTime)}");
                    sb.AppendLine(text);
                    sb.AppendLine();
                    index++;
                }
            }

            return sb.ToString();
        }

        private string GenerateSegmentedSrtContent(List<TimingEntry> timingEntries, Dictionary<string, string> verseMap, GenerationMode mode, string separators)
        {
            var splitter = new VerseSegmentSplitterService();
            var groups = splitter.GroupSegmentsByVerse(timingEntries);
            StringBuilder sb = new StringBuilder();
            int index = 1;

            var processedSegments = new HashSet<TimingEntry>();

            foreach (var entry in timingEntries)
            {
                if (processedSegments.Contains(entry)) continue;

                // Skip section markers
                if (entry.SegmentId.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    processedSegments.Add(entry);
                    continue;
                }

                var match = Regex.Match(entry.SegmentId, @"^(\d+)");
                if (match.Success)
                {
                    string verseNum = match.Groups[1].Value;
                    if (verseMap.TryGetValue(verseNum, out string? verseText))
                    {
                        var verseSegments = groups[verseNum];
                        List<VerseSegment> splitResults;

                        if (mode == GenerationMode.Phrase)
                        {
                            splitResults = splitter.SplitByPhrase(verseText, verseSegments);
                        }
                        else
                        {
                            splitResults = splitter.SplitVerse(verseText, verseSegments, separators);
                        }

                        foreach (var split in splitResults)
                        {
                            sb.AppendLine(index.ToString());
                            sb.AppendLine($"{FormatTime(split.StartTime)} --> {FormatTime(split.EndTime)}");
                            sb.AppendLine(split.Text);
                            sb.AppendLine();
                            index++;
                        }

                        foreach (var s in verseSegments) processedSegments.Add(s);
                        continue;
                    }
                }

                // Fallback for standalone segments or missing verses
                if (verseMap.TryGetValue(entry.SegmentId, out string? standaloneText))
                {
                    sb.AppendLine(index.ToString());
                    sb.AppendLine($"{FormatTime(entry.StartTime)} --> {FormatTime(entry.EndTime)}");
                    sb.AppendLine(standaloneText);
                    sb.AppendLine();
                    index++;
                }
                processedSegments.Add(entry);
            }

            return sb.ToString();
        }

        private string FormatTime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", 
                t.Hours + (t.Days * 24), t.Minutes, t.Seconds, t.Milliseconds);
        }
    }
}

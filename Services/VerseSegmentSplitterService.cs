using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoEditor.Services
{
    public partial class VerseSegment : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string SegmentId { get; set; } = string.Empty;
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private string _text = string.Empty;
        
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private double _startTime;
        
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private double _endTime;
        
        public double Duration => EndTime - StartTime;
    }

    public class VerseSegmentSplitterService
    {
        /// <summary>
        /// Splits verse text proportionally across multiple timing segments.
        /// </summary>
        public List<VerseSegment> SplitVerse(string verseText, List<TimingEntry> segments, string separators = "")
        {
            var result = new List<VerseSegment>();
            if (string.IsNullOrWhiteSpace(verseText) || !segments.Any())
                return result;

            // Remove section markers just in case (though they should be filtered out by caller)
            segments = segments.Where(s => !s.SegmentId.StartsWith("s", StringComparison.OrdinalIgnoreCase)).ToList();
            if (!segments.Any()) return result;

            if (segments.Count == 1)
            {
                result.Add(new VerseSegment
                {
                    SegmentId = segments[0].SegmentId,
                    Text = verseText.Trim(),
                    StartTime = segments[0].StartTime,
                    EndTime = segments[0].EndTime
                });
                return result;
            }

            // Split text into words
            string[] words = verseText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return result;

            // Advanced splitting attempt: use separators (phrase-based)
            if (!string.IsNullOrEmpty(separators))
            {
                // Simple attempt to split by the first found separator type if counts match
                // In practice, a full phrase-based alignment is complex, 
                // but we can try to find split points that align with punctuation.
            }

            double totalDuration = segments.Sum(s => s.EndTime - s.StartTime);
            if (totalDuration <= 0) totalDuration = segments.Count;

            int wordsDistributed = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                double duration = segment.EndTime - segment.StartTime;
                double ratio = duration / totalDuration;

                int wordCount;
                if (i == segments.Count - 1)
                {
                    wordCount = words.Length - wordsDistributed;
                }
                else
                {
                    wordCount = (int)Math.Round(words.Length * ratio);
                    if (wordCount == 0 && wordsDistributed < words.Length) wordCount = 1;
                    wordCount = Math.Min(wordCount, words.Length - wordsDistributed);
                    
                    // Optimization: try to move the split point if a nearby word ends with a common separator
                    if (wordCount > 0 && wordCount < (words.Length - wordsDistributed) && !string.IsNullOrEmpty(separators))
                    {
                        // Check if current split word or adjacent words have separators
                        // This is a simple heuristic enhancement
                        char[] sepChars = separators.Replace(" ", "").ToCharArray();
                        int searchRange = 2; // Look 2 words ahead/behind
                        for (int j = 0; j <= searchRange; j++)
                        {
                            int idxAhead = wordsDistributed + wordCount + j - 1;
                            int idxBehind = wordsDistributed + wordCount - j - 1;

                            if (idxAhead >= wordsDistributed && idxAhead < words.Length - 1 && words[idxAhead].Trim().Any(c => sepChars.Contains(c)))
                            {
                                wordCount = idxAhead - wordsDistributed + 1;
                                break;
                            }
                            if (idxBehind >= wordsDistributed && idxBehind < words.Length - 1 && words[idxBehind].Trim().Any(c => sepChars.Contains(c)))
                            {
                                wordCount = idxBehind - wordsDistributed + 1;
                                break;
                            }
                        }
                    }
                }

                string segmentText = string.Join(" ", words.Skip(wordsDistributed).Take(wordCount));

                result.Add(new VerseSegment
                {
                    SegmentId = segment.SegmentId,
                    Text = segmentText,
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime
                });

                wordsDistributed += wordCount;
            }

            return result;
        }

        /// <summary>
        /// Splits verse text naturally across timing segments using punctuation and connectors.
        /// </summary>
        public List<VerseSegment> SplitByPhrase(string verseText, List<TimingEntry> segments)
        {
            var result = new List<VerseSegment>();
            if (string.IsNullOrWhiteSpace(verseText) || !segments.Any())
                return result;

            // Remove section markers
            segments = segments.Where(s => !s.SegmentId.StartsWith("s", StringComparison.OrdinalIgnoreCase)).ToList();
            if (!segments.Any()) return result;

            // Step 1: Split into atomic phrases using punctuation and connectors
            // Connectors: and, but, that, which, who
            string[] connectors = { " and ", " but ", " that ", " which ", " who " };
            char[] punctuation = { '.', '?', '!', ':', ';', ',' };

            var atomicPhrases = new List<string>();
            
            // Pattern to split by punctuation (keeping it) or connectors (keeping them)
            string pattern = @"(?<=[.?!:;,])|(?=\b(and|but|that|which|who)\b)";
            string[] parts = Regex.Split(verseText, pattern, RegexOptions.IgnoreCase);

            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                    atomicPhrases.Add(part.Trim());
            }

            // Step 2: Group atomic phrases to fit within segment counts based on duration ratios
            double totalDuration = segments.Sum(s => s.EndTime - s.StartTime);
            if (totalDuration <= 0) totalDuration = segments.Count;

            int phrasesUsed = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                double ratio = (segment.EndTime - segment.StartTime) / totalDuration;

                int phraseCount;
                if (i == segments.Count - 1)
                {
                    phraseCount = atomicPhrases.Count - phrasesUsed;
                }
                else
                {
                    phraseCount = (int)Math.Round(atomicPhrases.Count * ratio);
                    if (phraseCount == 0 && phrasesUsed < atomicPhrases.Count) phraseCount = 1;
                    phraseCount = Math.Min(phraseCount, atomicPhrases.Count - phrasesUsed);
                }

                string segmentText = string.Join(" ", atomicPhrases.Skip(phrasesUsed).Take(phraseCount));
                
                // Final Polish: Ensure within 42 chars per line, max 2 lines
                segmentText = FormatForSubtitle(segmentText, 42);

                result.Add(new VerseSegment
                {
                    SegmentId = segment.SegmentId,
                    Text = segmentText,
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime
                });

                phrasesUsed += phraseCount;
            }

            return result;
        }

        private string FormatForSubtitle(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxCharsPerLine)
                return text;

            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            string currentLine = "";
            int lineCount = 0;

            foreach (var word in words)
            {
                if ((currentLine + " " + word).Trim().Length <= maxCharsPerLine)
                {
                    currentLine = (currentLine + " " + word).Trim();
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        sb.AppendLine(currentLine);
                        lineCount++;
                    }
                    currentLine = word;
                    if (lineCount >= 1) break; // Limit to 2 lines (current line will be the 2nd)
                }
            }
            
            if (!string.IsNullOrEmpty(currentLine))
            {
                sb.Append(currentLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Groups timing entries by their numeric verse ID.
        /// e.g. 2a, 2b -> 2
        /// </summary>
        public Dictionary<string, List<TimingEntry>> GroupSegmentsByVerse(List<TimingEntry> entries)
        {
            var groups = new Dictionary<string, List<TimingEntry>>();
            foreach (var entry in entries)
            {
                // Extract numeric part
                var match = Regex.Match(entry.SegmentId, @"^(\d+)");
                if (match.Success)
                {
                    string verseNum = match.Groups[1].Value;
                    if (!groups.ContainsKey(verseNum))
                    {
                        groups[verseNum] = new List<TimingEntry>();
                    }
                    groups[verseNum].Add(entry);
                }
                else
                {
                    // For non-numeric IDs that aren't section markers, 
                    // we'll treat them as standalone (if they didn't start with 's')
                    if (!groups.ContainsKey(entry.SegmentId))
                    {
                        groups[entry.SegmentId] = new List<TimingEntry>();
                    }
                    groups[entry.SegmentId].Add(entry);
                }
            }
            return groups;
        }
    }
}

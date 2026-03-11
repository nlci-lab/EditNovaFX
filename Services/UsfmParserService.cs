using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoEditor.Services
{
    public class UsfmParserService
    {
        private static readonly Regex VerseRegex = new Regex(@"^\\v\s+(\d+[a-z]?)\s+(.*)", RegexOptions.Compiled);
        private static readonly Regex IgnoreMarkersRegex = new Regex(@"^\\(s\d?|ms|mt\d?|d|rem|cl|id|c|r|p|q\d?|m|f|x|v|b)", RegexOptions.Compiled);

        /// <summary>
        /// Parses lines from a specific chapter to extract verse text.
        /// Only processes \v and continuation text.
        /// </summary>
        public Dictionary<string, string> ParseVerses(List<string> lines)
        {
            var verseMap = new Dictionary<string, string>();
            string currentVerseId = string.Empty;
            StringBuilder currentVerseText = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var verseMatch = VerseRegex.Match(trimmed);
                if (verseMatch.Success)
                {
                    // Save previous verse
                    if (!string.IsNullOrEmpty(currentVerseId))
                        verseMap[currentVerseId] = CleanText(currentVerseText.ToString());

                    currentVerseId = verseMatch.Groups[1].Value;
                    currentVerseText = new StringBuilder(verseMatch.Groups[2].Value);
                }
                else if (trimmed.StartsWith("\\"))
                {
                    // If it's another marker, check if it's one we should ignore or if it contains text
                    // But requirements say "Only process \v"
                    // However, we should check if it's a structural ignore marker that stops verse accumulation
                    if (IsMajorStructuralMarker(trimmed))
                    {
                        if (!string.IsNullOrEmpty(currentVerseId))
                        {
                            verseMap[currentVerseId] = CleanText(currentVerseText.ToString());
                            currentVerseId = string.Empty;
                            currentVerseText.Clear();
                        }
                    }
                }
                else
                {
                    // Continuation text
                    if (!string.IsNullOrEmpty(currentVerseId))
                    {
                        if (currentVerseText.Length > 0) currentVerseText.Append(" ");
                        currentVerseText.Append(trimmed);
                    }
                }
            }

            // Save last verse
            if (!string.IsNullOrEmpty(currentVerseId))
                verseMap[currentVerseId] = CleanText(currentVerseText.ToString());

            return verseMap;
        }

        private bool IsMajorStructuralMarker(string line) => 
            line.StartsWith("\\s") || line.StartsWith("\\ms") || line.StartsWith("\\mt") || 
            line.StartsWith("\\c") || line.StartsWith("\\d") || line.StartsWith("\\rem") || line.StartsWith("\\cl");

        private string CleanText(string text)
        {
            // Remove USFM footnotes \f ... \f* and cross-references \x ... \x*
            // Use a more robust lazy match to prevent over-matching across verses
            string cleaned = Regex.Replace(text, @"\\f\s+.*?\\f\*", "", RegexOptions.Singleline);
            cleaned = Regex.Replace(cleaned, @"\\x\s+.*?\\x\*", "", RegexOptions.Singleline);
            
            // Remove other USFM character markers like \bk ... \bk*, \qs ... \qs* etc.
            cleaned = Regex.Replace(cleaned, @"\\[a-z1-9]+\*?", "");
            
            // Clean up extra whitespace
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }
    }
}

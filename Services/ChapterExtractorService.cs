using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VideoEditor.Services
{
    public class ChapterExtractorService
    {
        /// <summary>
        /// Extracts the content of a specific chapter from a full USFM file.
        /// Extracts text between \c X and the next \c X+1 or end of file.
        /// </summary>
        public List<string> ExtractChapter(string[] usfmLines, int chapterNumber)
        {
            var chapterLines = new List<string>();
            bool inTargetChapter = false;
            
            // Regex for \c X
            Regex chapterRegex = new Regex($@"^\\c\s+{chapterNumber}(\s|$)", RegexOptions.Compiled);
            // Regex for any \c marker
            Regex anyChapterRegex = new Regex(@"^\\c\s+(\d+)", RegexOptions.Compiled);

            foreach (var line in usfmLines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (chapterRegex.IsMatch(trimmed))
                {
                    inTargetChapter = true;
                    chapterLines.Add(trimmed);
                    continue;
                }

                if (inTargetChapter)
                {
                    // If we hit another \c marker, we've reached the end of the chapter
                    if (anyChapterRegex.IsMatch(trimmed))
                    {
                        break;
                    }
                    chapterLines.Add(trimmed);
                }
            }

            return chapterLines;
        }
    }
}

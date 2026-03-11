using System.Collections.Generic;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    public static class BibleDataService
    {
        public static List<BibleBook> GetProtestantBooks()
        {
            return new List<BibleBook>
            {
                // Old Testament
                new BibleBook { Name = "Genesis", Code = "GEN", ChapterCount = 50 },
                new BibleBook { Name = "Exodus", Code = "EXO", ChapterCount = 40 },
                new BibleBook { Name = "Leviticus", Code = "LEV", ChapterCount = 27 },
                new BibleBook { Name = "Numbers", Code = "NUM", ChapterCount = 36 },
                new BibleBook { Name = "Deuteronomy", Code = "DEU", ChapterCount = 34 },
                new BibleBook { Name = "Joshua", Code = "JOS", ChapterCount = 24 },
                new BibleBook { Name = "Judges", Code = "JDG", ChapterCount = 21 },
                new BibleBook { Name = "Ruth", Code = "RUT", ChapterCount = 4 },
                new BibleBook { Name = "1 Samuel", Code = "1SAM", ChapterCount = 31 },
                new BibleBook { Name = "2 Samuel", Code = "2SAM", ChapterCount = 24 },
                new BibleBook { Name = "1 Kings", Code = "1KGS", ChapterCount = 22 },
                new BibleBook { Name = "2 Kings", Code = "2KGS", ChapterCount = 25 },
                new BibleBook { Name = "1 Chronicles", Code = "1CHR", ChapterCount = 29 },
                new BibleBook { Name = "2 Chronicles", Code = "2CHR", ChapterCount = 36 },
                new BibleBook { Name = "Ezra", Code = "EZR", ChapterCount = 10 },
                new BibleBook { Name = "Nehemiah", Code = "NEH", ChapterCount = 13 },
                new BibleBook { Name = "Esther", Code = "EST", ChapterCount = 10 },
                new BibleBook { Name = "Job", Code = "JOB", ChapterCount = 42 },
                new BibleBook { Name = "Psalms", Code = "PSA", ChapterCount = 150 },
                new BibleBook { Name = "Proverbs", Code = "PRO", ChapterCount = 31 },
                new BibleBook { Name = "Ecclesiastes", Code = "ECC", ChapterCount = 12 },
                new BibleBook { Name = "Song of Solomon", Code = "SNG", ChapterCount = 8 },
                new BibleBook { Name = "Isaiah", Code = "ISA", ChapterCount = 66 },
                new BibleBook { Name = "Jeremiah", Code = "JER", ChapterCount = 52 },
                new BibleBook { Name = "Lamentations", Code = "LAM", ChapterCount = 5 },
                new BibleBook { Name = "Ezekiel", Code = "EZK", ChapterCount = 48 },
                new BibleBook { Name = "Daniel", Code = "DAN", ChapterCount = 12 },
                new BibleBook { Name = "Hosea", Code = "HOS", ChapterCount = 14 },
                new BibleBook { Name = "Joel", Code = "JOL", ChapterCount = 3 },
                new BibleBook { Name = "Amos", Code = "AMO", ChapterCount = 9 },
                new BibleBook { Name = "Obadiah", Code = "OBA", ChapterCount = 1 },
                new BibleBook { Name = "Jonah", Code = "JON", ChapterCount = 4 },
                new BibleBook { Name = "Micah", Code = "MIC", ChapterCount = 7 },
                new BibleBook { Name = "Nahum", Code = "NAM", ChapterCount = 3 },
                new BibleBook { Name = "Habakkuk", Code = "HAB", ChapterCount = 3 },
                new BibleBook { Name = "Zephaniah", Code = "ZEP", ChapterCount = 3 },
                new BibleBook { Name = "Haggai", Code = "HAG", ChapterCount = 2 },
                new BibleBook { Name = "Zechariah", Code = "ZEC", ChapterCount = 14 },
                new BibleBook { Name = "Malachi", Code = "MAL", ChapterCount = 4 },

                // New Testament
                new BibleBook { Name = "Matthew", Code = "MAT", ChapterCount = 28 },
                new BibleBook { Name = "Mark", Code = "MRK", ChapterCount = 16 },
                new BibleBook { Name = "Luke", Code = "LUK", ChapterCount = 24 },
                new BibleBook { Name = "John", Code = "JHN", ChapterCount = 21 },
                new BibleBook { Name = "Acts", Code = "ACT", ChapterCount = 28 },
                new BibleBook { Name = "Romans", Code = "ROM", ChapterCount = 16 },
                new BibleBook { Name = "1 Corinthians", Code = "1COR", ChapterCount = 16 },
                new BibleBook { Name = "2 Corinthians", Code = "2COR", ChapterCount = 13 },
                new BibleBook { Name = "Galatians", Code = "GAL", ChapterCount = 6 },
                new BibleBook { Name = "Ephesians", Code = "EPH", ChapterCount = 6 },
                new BibleBook { Name = "Philippians", Code = "PHP", ChapterCount = 4 },
                new BibleBook { Name = "Colossians", Code = "COL", ChapterCount = 4 },
                new BibleBook { Name = "1 Thessalonians", Code = "1TH", ChapterCount = 5 },
                new BibleBook { Name = "2 Thessalonians", Code = "2TH", ChapterCount = 3 },
                new BibleBook { Name = "1 Timothy", Code = "1TI", ChapterCount = 6 },
                new BibleBook { Name = "2 Timothy", Code = "2TI", ChapterCount = 4 },
                new BibleBook { Name = "Titus", Code = "TIT", ChapterCount = 3 },
                new BibleBook { Name = "Philemon", Code = "PHM", ChapterCount = 1 },
                new BibleBook { Name = "Hebrews", Code = "HEB", ChapterCount = 13 },
                new BibleBook { Name = "James", Code = "JAS", ChapterCount = 5 },
                new BibleBook { Name = "1 Peter", Code = "1PE", ChapterCount = 5 },
                new BibleBook { Name = "2 Peter", Code = "2PE", ChapterCount = 3 },
                new BibleBook { Name = "1 John", Code = "1JN", ChapterCount = 5 },
                new BibleBook { Name = "2 John", Code = "2JN", ChapterCount = 1 },
                new BibleBook { Name = "3 John", Code = "3JN", ChapterCount = 1 },
                new BibleBook { Name = "Jude", Code = "JUD", ChapterCount = 1 },
                new BibleBook { Name = "Revelation", Code = "REV", ChapterCount = 22 }
            };
        }
    }
}

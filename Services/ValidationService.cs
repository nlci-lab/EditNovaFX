using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VideoEditor.Services
{
    public class ValidationService
    {
        public struct ValidationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Validates that the USFM file matches the selected book code.
        /// </summary>
        public ValidationResult ValidateBook(string filePath, string expectedBookCode)
        {
            if (!File.Exists(filePath))
                return new ValidationResult { Success = false, Message = "File not found." };

            try
            {
                // Check the first few lines for \id
                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    int lineCount = 0;
                    while ((line = reader.ReadLine()) != null && lineCount < 20)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("\\id "))
                        {
                            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string actualCode = parts[1].ToUpper();
                                if (actualCode == expectedBookCode.ToUpper())
                                {
                                    return new ValidationResult { Success = true };
                                }
                                else
                                {
                                    return new ValidationResult 
                                    { 
                                        Success = false, 
                                        Message = $"Selected book ({expectedBookCode}) does not match uploaded USFM file ({actualCode})." 
                                    };
                                }
                            }
                        }
                        lineCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult { Success = false, Message = $"Error reading USFM: {ex.Message}" };
            }

            return new ValidationResult { Success = false, Message = "Could not find \\id marker in USFM file." };
        }

        /// <summary>
        /// Validates that the chapter exists in the USFM file.
        /// </summary>
        public ValidationResult ValidateChapter(string[] usfmLines, int chapterNumber)
        {
            Regex chapterRegex = new Regex($@"^\\c\s+{chapterNumber}(\s|$)", RegexOptions.Compiled);
            bool found = usfmLines.Any(l => chapterRegex.IsMatch(l.Trim()));
            
            if (found)
                return new ValidationResult { Success = true };
            else
                return new ValidationResult { Success = false, Message = $"Chapter {chapterNumber} not found in USFM file." };
        }
    }
}

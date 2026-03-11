using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    public class AIContentService
    {
        public class GeneratedMetadata
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<string> Tags { get; set; } = new List<string>();
            public List<string> Hashtags { get; set; } = new List<string>();
            public List<string> Chapters { get; set; } = new List<string>();
        }

        public async Task<GeneratedMetadata> GenerateMetadata(Project project)
        {
            // Simulate AI processing delay
            await Task.Delay(1500);

            var metadata = new GeneratedMetadata();
            
            // Extract text from all enabled subtitle tracks
            string allText = string.Join(" ", project.SubtitleTracks
                .Where(t => t.IsEnabled)
                .SelectMany(t => t.Entries)
                .Select(e => e.Text));

            if (string.IsNullOrWhiteSpace(allText))
            {
                metadata.Title = project.Name + " - Final Cut";
                metadata.Description = "Created with EditNovaFX. Subscribe for more content!";
                metadata.Tags = new List<string> { "video", "editor", "content creator" };
                return metadata;
            }

            // Simple Keyword Extraction for Title
            var words = Regex.Matches(allText.ToLower(), @"\w+")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(w => w.Length > 4)
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => char.ToUpper(g.Key[0]) + g.Key.Substring(1))
                .ToList();

            if (words.Count >= 2)
                metadata.Title = $"{words[0]} & {words[1]}: A Deep Dive";
            else
                metadata.Title = project.Name + " - High Quality Edit";

            // Generate Description
            metadata.Description = $"In this video, we explore {string.Join(", ", words.Take(3))}.\n\n" +
                                   $"This content was automatically generated and edited for the best viewer experience.\n\n" +
                                   $"TIMESTAMPS:\n00:00 - Introduction\n" +
                                   $"01:30 - Key Highlights\n" +
                                   $"05:00 - Conclusion\n\n" +
                                   $"#EditNovaFX #CreatorReady";

            // Tags and Hashtags
            metadata.Tags = words.Take(10).ToList();
            metadata.Hashtags = words.Take(3).Select(w => "#" + w).ToList();
            metadata.Hashtags.Add("#EditNovaFX");

            return metadata;
        }
    }
}

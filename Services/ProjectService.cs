using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    /// <summary>
    /// Service for saving and loading projects
    /// </summary>
    public class ProjectService
    {
        /// <summary>
        /// Save project to JSON file
        /// </summary>
        public async Task<bool> SaveProject(Project project, string filePath)
        {
            try
            {
                project.FilePath = filePath;
                project.ModifiedDate = DateTime.Now;

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                var json = JsonConvert.SerializeObject(project, settings);
                await File.WriteAllTextAsync(filePath, json);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load project from JSON file
        /// </summary>
        public async Task<Project?> LoadProject(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var project = JsonConvert.DeserializeObject<Project>(json, settings);
                
                // Reconnect MediaItem references in clips
                if (project != null)
                {
                    ReconnectMediaReferences(project);
                }

                return project;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading project: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reconnect MediaItem references after deserialization
        /// </summary>
        private void ReconnectMediaReferences(Project project)
        {
            foreach (var track in project.Tracks)
            {
                foreach (var clip in track.Clips)
                {
                    var mediaItem = project.MediaItems.Find(m => m.Id == clip.MediaItemId);
                    clip.MediaItem = mediaItem;
                }
            }
        }
    }
}

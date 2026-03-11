using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    public class UndoRedoService
    {
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private const int MaxHistory = 50;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public void SaveState(Project project)
        {
            var json = JsonConvert.SerializeObject(project, SerializerSettings);
            
            // Only save if different from last state
            if (_undoStack.Count > 0 && _undoStack.Peek() == json)
                return;

            _undoStack.Push(json);
            _redoStack.Clear(); // Clear redo on new action

            if (_undoStack.Count > MaxHistory)
            {
                // Remove oldest (simplified)
            }
        }

        public Project? Undo(Project currentProject)
        {
            if (_undoStack.Count <= 1) return null;

            // Current state is topmost, move it to redo
            var currentState = _undoStack.Pop();
            _redoStack.Push(currentState);

            // Peek the previous state
            var previousJson = _undoStack.Peek();
            var project = JsonConvert.DeserializeObject<Project>(previousJson, SerializerSettings);
            
            if (project != null)
                ReconnectMediaReferences(project);
            
            return project;
        }

        public Project? Redo()
        {
            if (_redoStack.Count == 0) return null;

            var redoJson = _redoStack.Pop();
            _undoStack.Push(redoJson);

            var project = JsonConvert.DeserializeObject<Project>(redoJson, SerializerSettings);
            
            if (project != null)
                ReconnectMediaReferences(project);

            return project;
        }

        public bool CanUndo => _undoStack.Count > 1;
        public bool CanRedo => _redoStack.Count > 0;

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

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}

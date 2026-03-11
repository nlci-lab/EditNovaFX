using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VideoEditor.Models
{
    /// <summary>A single numbered step in a how-to guide.</summary>
    public class HelpStep
    {
        public int Number { get; set; }
        public string Emoji { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    /// <summary>A keyboard shortcut row.</summary>
    public class ShortcutItem
    {
        public string Keys { get; set; } = "";
        public string Action { get; set; } = "";
    }

    /// <summary>A FAQ entry.</summary>
    public class FaqItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    /// <summary>A labelled screen zone used in screen-layout diagrams.</summary>
    public class ScreenZone
    {
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public string Color { get; set; } = "#2C5F8A";
    }

    public class HelpTopic
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = "ℹ️";

        /// <summary>Short introductory paragraph shown at the top.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Legacy plain-text body (kept for backward compat).</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Numbered step-by-step instructions.</summary>
        public List<HelpStep> Steps { get; set; } = new();

        /// <summary>Tip callouts (green).</summary>
        public List<string> Tips { get; set; } = new();

        /// <summary>Warning callouts (orange).</summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>Keyboard shortcuts table.</summary>
        public List<ShortcutItem> Shortcuts { get; set; } = new();

        /// <summary>FAQ items.</summary>
        public List<FaqItem> Faqs { get; set; } = new();

        /// <summary>Screen-layout zones for the annotated diagram.</summary>
        public List<ScreenZone> ScreenZones { get; set; } = new();

        /// <summary>Whether to show the annotated screen diagram.</summary>
        public bool HasScreenDiagram => ScreenZones.Count > 0;

        /// <summary>Whether any structured content exists.</summary>
        public bool HasRichContent =>
            Steps.Count > 0 || Tips.Count > 0 || Warnings.Count > 0 ||
            Shortcuts.Count > 0 || Faqs.Count > 0 || ScreenZones.Count > 0;

        public ObservableCollection<HelpTopic> SubTopics { get; set; } = new();

        public HelpTopic() { }

        public HelpTopic(string title, string content, string icon = "ℹ️")
        {
            Title = title;
            Content = content;
            Icon = icon;
            Summary = content;
        }
    }
}

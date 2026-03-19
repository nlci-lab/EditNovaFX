using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VideoEditor.Models;
using VideoEditor.Services;

namespace VideoEditor.ViewModels
{
    public partial class ScriptureSubtitleViewModel : ObservableObject
    {
        private readonly ValidationService _validationService;
        private readonly ChapterExtractorService _extractorService;
        private readonly UsfmParserService _usfmParser;
        private readonly TimingParserService _timingParser;
        private readonly SrtWriterService _srtWriter;

        [ObservableProperty] private ObservableCollection<BibleBook> _books;
        [ObservableProperty] private BibleBook? _selectedBook;
        [ObservableProperty] private ObservableCollection<int> _chapters = new ObservableCollection<int>();
        [ObservableProperty] private int _selectedChapter = 1;

        [ObservableProperty] private string _usfmPath = string.Empty;
        [ObservableProperty] private string _timingPath = string.Empty;
        
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private bool _isProcessing;
        [ObservableProperty] private double _progress;

        [ObservableProperty] private bool _isVerseMode = false;
        [ObservableProperty] private bool _isSegmentMode = false;
        [ObservableProperty] private bool _isPhraseMode = true;

        [ObservableProperty] private ObservableCollection<VerseSegment> _previewSegments = new ObservableCollection<VerseSegment>();

        public ScriptureSubtitleViewModel()
        {
            _validationService = new ValidationService();
            _extractorService = new ChapterExtractorService();
            _usfmParser = new UsfmParserService();
            _timingParser = new TimingParserService();
            _srtWriter = new SrtWriterService();

            Books = new ObservableCollection<BibleBook>(BibleDataService.GetProtestantBooks());
            SelectedBook = Books.FirstOrDefault(b => b.Code == "MAT");
        }

        partial void OnSelectedBookChanged(BibleBook? value)
        {
            if (value != null)
            {
                var newChapters = Enumerable.Range(1, value.ChapterCount).ToList();
                Chapters = new ObservableCollection<int>(newChapters);
                SelectedChapter = 1;
            }
        }

        [RelayCommand]
        private void UploadUsfm()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "USFM Files (*.sfm;*.usfm)|*.sfm;*.usfm|All Files (*.*)|*.*",
                Title = "Select USFM File"
            };

            if (dialog.ShowDialog() == true)
            {
                UsfmPath = dialog.FileName;
                StatusMessage = $"USFM selected: {Path.GetFileName(UsfmPath)}";
            }
        }

        [RelayCommand]
        private void UploadTiming()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Timing Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Select Timing File"
            };

            if (dialog.ShowDialog() == true)
            {
                TimingPath = dialog.FileName;
                StatusMessage = $"Timing selected: {Path.GetFileName(TimingPath)}";
            }
        }

        [RelayCommand]
        private void AdjustStartTime(object? parameter)
        {
            if (parameter is not object[] values || values.Length < 2) return;
            if (values[0] is not VerseSegment segment || values[1] is not string deltaStr) return;
            if (!double.TryParse(deltaStr, out double delta)) return;

            int index = PreviewSegments.IndexOf(segment);
            double newStart = segment.StartTime + delta;

            // Don't allow start to exceed end
            if (newStart >= segment.EndTime) return;
            
            // Don't allow start to go below 0
            if (newStart < 0) newStart = 0;

            segment.StartTime = newStart;

            // Ripple to previous segment's end time
            if (index > 0)
            {
                PreviewSegments[index - 1].EndTime = segment.StartTime;
            }
        }

        [RelayCommand]
        private void AdjustEndTime(object? parameter)
        {
            if (parameter is not object[] values || values.Length < 2) return;
            if (values[0] is not VerseSegment segment || values[1] is not string deltaStr) return;
            if (!double.TryParse(deltaStr, out double delta)) return;

            int index = PreviewSegments.IndexOf(segment);
            double newEnd = segment.EndTime + delta;

            // Don't allow end to be before start
            if (newEnd <= segment.StartTime) return;

            segment.EndTime = newEnd;

            // Ripple to next segment's start time
            if (index < PreviewSegments.Count - 1)
            {
                PreviewSegments[index + 1].StartTime = segment.EndTime;
            }
        }

        [RelayCommand]
        private async Task PreviewSrt()
        {
            if (string.IsNullOrEmpty(UsfmPath) || !File.Exists(UsfmPath))
            {
                MessageBox.Show("Please select a valid USFM file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TimingPath) || !File.Exists(TimingPath))
            {
                MessageBox.Show("Please select a valid Timing file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsProcessing = true;
                Progress = 5;
                StatusMessage = "Validating selection...";

                if (SelectedBook == null)
                {
                    MessageBox.Show("Please select a Bible book.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsProcessing = false;
                    return;
                }

                // Step 1: Validate Book matches USFM
                var bookCheck = _validationService.ValidateBook(UsfmPath, SelectedBook.Code);
                if (!bookCheck.Success)
                {
                    MessageBox.Show(bookCheck.Message, "Book Mismatch", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsProcessing = false;
                    return;
                }

                Progress = 15;
                StatusMessage = "Checking chapter...";

                // Step 2: Validate Chapter exists in USFM
                string[] usfmLines = await Task.Run(() => File.ReadAllLines(UsfmPath, Encoding.UTF8));
                var chapterCheck = _validationService.ValidateChapter(usfmLines, SelectedChapter);
                if (!chapterCheck.Success)
                {
                    MessageBox.Show(chapterCheck.Message, "Chapter Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsProcessing = false;
                    return;
                }

                if (IsPhraseMode)
                {
                    Progress = 20;
                    StatusMessage = "Preparing External Converter...";

                    string exePath = SettingsService.Instance.CurrentSettings.SubtitleParserPath;

                    if (!File.Exists(exePath))
                    {
                        MessageBox.Show($"Could not find the converter tool at:\n{exePath}\nPlease configure the path in File -> Settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        IsProcessing = false;
                        return;
                    }

                    StatusMessage = "Running External Converter...";
                    Progress = 30;

                    string fullPath = Path.GetFullPath(exePath);
                    string arguments = $"\"{UsfmPath}\" \"{TimingPath}\"";
                    
                    // Run the process and wait for it to exit
                    await Task.Run(() =>
                    {
                        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = fullPath,
                            Arguments = arguments,
                            WorkingDirectory = Path.GetDirectoryName(fullPath),
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
                        });
                        process?.WaitForExit();
                    });

                    Progress = 70;
                    StatusMessage = "Loading generated subtitles...";

                    // The tool generates an SRT with the same name as USFM in the same directory
                    string generatedSrtPath = Path.ChangeExtension(UsfmPath, ".srt");

                    if (File.Exists(generatedSrtPath))
                    {
                        // Use the existing SubtitleParser to read the SRT
                        var track = SubtitleParser.ParseSubtitleFile(generatedSrtPath);
                        
                        var segments = track.Entries.Select(e => new VerseSegment
                        {
                            SegmentId = e.Index.ToString(),
                            Text = e.Text,
                            StartTime = e.StartTime.TotalSeconds,
                            EndTime = e.EndTime.TotalSeconds
                        }).ToList();

                        PreviewSegments = new ObservableCollection<VerseSegment>(segments);
                        StatusMessage = $"Loaded {segments.Count} subtitles from external tool (Phrase Mode).";
                    }
                    else
                    {
                        MessageBox.Show("The external tool did not generate the output file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusMessage = "External tool failed.";
                    }
                }
                else
                {
                    Progress = 60;
                    StatusMessage = "Parsing Timing...";

                    var timingEntries = _timingParser.ParseTiming(TimingPath);

                    Progress = 80;
                    StatusMessage = "Applying Splitting Strategy (Internal)...";

                    var chapterLines = _extractorService.ExtractChapter(usfmLines, SelectedChapter);
                    var verseMap = _usfmParser.ParseVerses(chapterLines);

                    var mode = IsVerseMode ? SrtWriterService.GenerationMode.Verse : SrtWriterService.GenerationMode.Segment;

                    // For Preview/Generation, we need to get the segments via internal logic
                    var segments = await Task.Run(() => GenerateSegments(timingEntries, verseMap, mode));
                    PreviewSegments = new ObservableCollection<VerseSegment>(segments);

                    if (!PreviewSegments.Any())
                    {
                        MessageBox.Show("No matching verses and timing segments found.", "Generation Result", MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsProcessing = false;
                        return;
                    }

                    StatusMessage = $"Preview loaded ({(IsVerseMode ? "Verse" : "Segment")} Mode). Ready for export.";
                }

                Progress = 100;
            }
            catch (Exception ex)
            {
                StatusMessage = "Error occurred.";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task ExportSrt()
        {
            if (SelectedBook == null)
            {
                MessageBox.Show("Please select a book.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!PreviewSegments.Any())
            {
                MessageBox.Show("Please generate/preview the subtitles first.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string srtContent = BuildSrtFromSegments(PreviewSegments.ToList());
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "SRT Files (*.srt)|*.srt|All Files (*.*)|*.*",
                    Title = "Save Generated SRT",
                    FileName = IsVerseMode ? $"{SelectedBook?.Code}_{SelectedChapter:D2}.srt" :
                               IsPhraseMode ? $"{SelectedBook?.Code}_{SelectedChapter:D2}_Phrased.srt" :
                               $"{SelectedBook?.Code}_{SelectedChapter:D2}_Segmented.srt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await Task.Run(() => File.WriteAllText(saveDialog.FileName, srtContent, Encoding.UTF8));
                    StatusMessage = $"Successfully exported to {Path.GetFileName(saveDialog.FileName)}";
                    
                    var result = MessageBox.Show($"SRT exported successfully to:\n{saveDialog.FileName}\n\nOpen folder?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{saveDialog.FileName}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<VerseSegment> GenerateSegments(List<TimingEntry> timingEntries, Dictionary<string, string> verseMap, SrtWriterService.GenerationMode mode)
        {
            var results = new List<VerseSegment>();
            var splitter = new VerseSegmentSplitterService();
            var groups = splitter.GroupSegmentsByVerse(timingEntries);
            var processedSegments = new HashSet<TimingEntry>();

            foreach (var entry in timingEntries)
            {
                if (processedSegments.Contains(entry)) continue;
                if (entry.SegmentId.StartsWith("s", StringComparison.OrdinalIgnoreCase)) continue;

                var match = Regex.Match(entry.SegmentId, @"^(\d+)");
                if (match.Success)
                {
                    string verseNum = match.Groups[1].Value;
                    if (verseMap.TryGetValue(verseNum, out string? verseText))
                    {
                        var verseSegments = groups[verseNum];
                        List<VerseSegment> split;

                        if (mode == SrtWriterService.GenerationMode.Phrase)
                            split = splitter.SplitByPhrase(verseText, verseSegments);
                        else if (mode == SrtWriterService.GenerationMode.Segment)
                            split = splitter.SplitVerse(verseText, verseSegments, _timingParser.Separators);
                        else // Verse Mode
                        {
                            split = verseSegments.Select(s => new VerseSegment 
                            { 
                                SegmentId = s.SegmentId, 
                                Text = verseText, 
                                StartTime = s.StartTime, 
                                EndTime = s.EndTime 
                            }).ToList();
                        }

                        results.AddRange(split);
                        foreach (var s in verseSegments) processedSegments.Add(s);
                        continue;
                    }
                }

                if (verseMap.TryGetValue(entry.SegmentId, out string? standalone))
                {
                    results.Add(new VerseSegment { SegmentId = entry.SegmentId, Text = standalone, StartTime = entry.StartTime, EndTime = entry.EndTime });
                }
                processedSegments.Add(entry);
            }
            return results;
        }

        private string BuildSrtFromSegments(IEnumerable<VerseSegment> segments)
        {
            StringBuilder sb = new StringBuilder();
            int index = 1;
            foreach (var seg in segments)
            {
                sb.AppendLine(index.ToString());
                sb.AppendLine($"{FormatTime(seg.StartTime)} --> {FormatTime(seg.EndTime)}");
                sb.AppendLine(seg.Text);
                sb.AppendLine();
                index++;
            }
            return sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n"); // Normalize to Windows CRLF for SRT
        }

        private string FormatTime(double seconds)
        {
            // Use Ticks for maximum precision and round to nearest millisecond to avoid 0.99999 precision errors
            long ticks = (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
            if (ticks < 0) ticks = 0;
            TimeSpan t = TimeSpan.FromTicks(ticks);
            
            return string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", 
                t.Hours + (t.Days * 24), 
                t.Minutes, 
                t.Seconds, 
                t.Milliseconds);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using VideoEditor.Models;
using System.Windows.Media;
using System.Windows;

namespace VideoEditor.Services
{
    /// <summary>
    /// Service for interacting with FFmpeg
    /// </summary>
    public class FFmpegService
    {
        private string _ffmpegPath;
        private string _ffprobePath;

        public string FFmpegPath => _ffmpegPath;
        public string FFprobePath => _ffprobePath;

        public event EventHandler<string>? OutputReceived;

        public FFmpegService()
        {
            var settings = SettingsService.Instance.CurrentSettings;
            _ffmpegPath = settings.FFmpegPath;
            _ffprobePath = settings.FFprobePath;
            
            if (!IsFFmpegAvailable())
            {
                AutoDetectPaths();
                
                // If auto-detect found valid ones, update settings
                if (IsFFmpegAvailable())
                {
                    settings.FFmpegPath = _ffmpegPath;
                    settings.FFprobePath = _ffprobePath;
                    SettingsService.Instance.SaveSettings();
                }
            }
        }

        private void AutoDetectPaths()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] commonLocations = {
                Path.Combine(baseDir, "ffmpeg"),
                Path.Combine(baseDir, "ffmpeg", "bin"),
                baseDir,
                @"C:\ffmpeg\bin",
                @"C:\Program Files\ffmpeg\bin",
                @"C:\Program Files (x86)\ffmpeg\bin",
                @"C:\Program Files\ShareX"
            };

            foreach (var loc in commonLocations)
            {
                if (!Directory.Exists(loc)) continue;
                
                string ffmpeg = Path.Combine(loc, "ffmpeg.exe");
                string ffprobe = Path.Combine(loc, "ffprobe.exe");

                if (File.Exists(ffmpeg))
                {
                    _ffmpegPath = ffmpeg;
                    if (File.Exists(ffprobe))
                    {
                        _ffprobePath = ffprobe;
                        break;
                    }
                }
            }
        }

        public bool SetPaths(string ffmpegPath, string ffprobePath)
        {
            if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
            {
                _ffmpegPath = ffmpegPath;
                _ffprobePath = ffprobePath;
                
                var settings = SettingsService.Instance.CurrentSettings;
                settings.FFmpegPath = ffmpegPath;
                settings.FFprobePath = ffprobePath;
                SettingsService.Instance.SaveSettings();

                return true;
            }
            return false;
        }

        public bool IsFFmpegAvailable()
        {
            try
            {
                // Check if paths exist explicitly
                if (File.Exists(_ffmpegPath) && File.Exists(_ffprobePath))
                    return true;

                // Check if they are in PATH
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get media information using ffprobe
        /// </summary>
        public async Task<MediaItem?> GetMediaInfo(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var mediaType = DetermineMediaType(filePath);
            var mediaItem = new MediaItem(filePath, mediaType);

            bool infoFound = false;

            try
            {
                // Try to get info from FFprobe first
                var args = $"-v error -show_entries format=duration:stream=width,height,r_frame_rate,channels,sample_rate,codec_type -of default=noprint_wrappers=1 \"{filePath}\"";
                
                var output = await RunFFprobeAsync(args);
                if (!string.IsNullOrWhiteSpace(output))
                {
                    ParseMediaInfo(output, mediaItem);
                    if (mediaItem.Duration > TimeSpan.Zero)
                        infoFound = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FFmpeg probe failed: {ex.Message}");
            }

            // If FFprobe failed, use accurate WPF fallback
            if (!infoFound || mediaItem.Duration == TimeSpan.Zero)
            {
                await GetInfoFromWpfPlayer(mediaItem);
            }

            return mediaItem;
        }

        private async Task GetInfoFromWpfPlayer(MediaItem mediaItem)
        {
            if (mediaItem.Type != MediaType.Video && mediaItem.Type != MediaType.Audio)
            {
                UseBasicFileInfo(mediaItem); // Use basic for Images/Others
                return;
            }

            try 
            {
                var tcs = new TaskCompletionSource<bool>();
                
                // Must run on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    var player = new MediaPlayer();
                    player.Open(new Uri(mediaItem.FilePath));
                    
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    
                    void Cleanup()
                    {
                        timer.Stop();
                        player.Close();
                    }

                    timer.Tick += (s, e) => {
                         Cleanup();
                         tcs.TrySetResult(false);
                    };

                    player.MediaOpened += (s, e) => {
                         if (player.NaturalDuration.HasTimeSpan)
                         {
                             mediaItem.Duration = player.NaturalDuration.TimeSpan;
                             if (mediaItem.Type == MediaType.Video)
                             {
                                 mediaItem.Width = player.NaturalVideoWidth;
                                 mediaItem.Height = player.NaturalVideoHeight;
                             }
                             mediaItem.HasAudio = player.HasAudio;
                         }
                         Cleanup();
                         tcs.TrySetResult(true);
                    };

                    player.MediaFailed += (s, e) => {
                         Cleanup();
                         tcs.TrySetResult(false);
                    };
                    
                    timer.Start();
                });

                bool success = await tcs.Task;
                if (!success || mediaItem.Duration == TimeSpan.Zero)
                {
                     UseBasicFileInfo(mediaItem);
                }
            }
            catch
            {
                UseBasicFileInfo(mediaItem);
            }
        }

        /// <summary>
        /// Last resort fallback using file size (Inaccurate)
        /// </summary>
        private void UseBasicFileInfo(MediaItem mediaItem)
        {
            try
            {
                var fileInfo = new FileInfo(mediaItem.FilePath);
                
                // For images, set a default duration of 5 seconds
                if (mediaItem.Type == MediaType.Image)
                {
                    mediaItem.Duration = TimeSpan.FromSeconds(5);
                    mediaItem.Width = 1920; // Default
                    mediaItem.Height = 1080; // Default
                }
                // For video/audio, estimate based on file size (very rough estimate)
                else
                {
                    // This fallback is extremely rough, but better than 0
                    var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                    // Assume ~1MB/s bitrate (8Mbps) as a safer default than 1MB/10s
                    var estimatedSeconds = fileSizeMB * 1.0; 
                    
                    mediaItem.Duration = TimeSpan.FromSeconds(Math.Max(estimatedSeconds, 5));
                    
                    if (mediaItem.Type == MediaType.Video)
                    {
                        mediaItem.Width = 1920;
                        mediaItem.Height = 1080;
                    }
                    mediaItem.HasAudio = true;
                }
                
                Debug.WriteLine($"Using fallback info for {mediaItem.Name}: Duration = {mediaItem.Duration}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in fallback method: {ex.Message}");
                mediaItem.Duration = TimeSpan.FromSeconds(10);
            }
        }

        /// <summary>
        /// Render/export the final video
        /// </summary>
        public async Task<(bool success, string message)> RenderVideo(Project project, ExportSettings settings, IProgress<int>? progress = null)
        {
            try
            {
                Debug.WriteLine("=== FFmpeg Render Starting ===");
                Debug.WriteLine($"Project: {project.Name}");
                Debug.WriteLine($"Duration: {project.Duration}");
                Debug.WriteLine($"Output: {settings.OutputPath}");
                
                var command = BuildFFmpegCommand(project, settings);
                Debug.WriteLine($"\n=== FFmpeg Command ===");
                Debug.WriteLine(command);
                Debug.WriteLine("======================\n");
                
                string result = await RunFFmpegAsync(command, progress, project.Duration);
                
                // Always report 100% when FFmpeg finishes so the popup closes
                progress?.Report(100);
                
                Debug.WriteLine("\n=== FFmpeg Output ===");
                Debug.WriteLine(result);
                Debug.WriteLine("====================\n");
                
                // Basic check if output file was created
                if (File.Exists(settings.OutputPath))
                {
                    var fileInfo = new FileInfo(settings.OutputPath);
                    Debug.WriteLine($"✓ Output file created: {settings.OutputPath}");
                    Debug.WriteLine($"✓ File size: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                    return (true, "Export successful");
                }
                else
                {
                    Debug.WriteLine($"✗ Output file NOT created: {settings.OutputPath}");
                    return (false, "FFmpeg failed to create output file.\n\nFFmpeg Output:\n" + result);
                }
            }
            catch (Exception ex)
            {
                progress?.Report(100); // Ensure popup closes even on error
                Debug.WriteLine($"✗ Exception in RenderVideo: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                return (false, $"Exception: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Build the complete FFmpeg command for rendering
        /// </summary>
        private string BuildFFmpegCommand(Project project, ExportSettings settings)
        {
            var inputMap = new Dictionary<string, int>(); // Path -> Input Index
            var inputs = new StringBuilder();
            var filterComplex = new StringBuilder();
            int clipCounter = 0;

            // Helper: register an input file and return its index
            int GetInputIndex(MediaItem item)
            {
                if (inputMap.TryGetValue(item.FilePath, out int idx)) return idx;
                int newIdx = inputMap.Count;
                inputMap[item.FilePath] = newIdx;
                inputs.Append(item.Type == MediaType.Image
                    ? $"-loop 1 -i \"{item.FilePath}\" "
                    : $"-stream_loop -1 -i \"{item.FilePath}\" ");
                return newIdx;
            }

            // Total project duration
            double totalSec = project.Duration.TotalSeconds;
            if (totalSec <= 0) totalSec = 5;
            string totalDurStr = totalSec.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var audioStreamLabels = new List<string>();

            // ── 1. VIDEO TRACK ────────────────────────────────────────────────────────
            var videoTrack = project.Tracks.Find(t => t.TrackType == MediaType.Video && t.IsEnabled);
            bool hasVideo = false;
            string videoContentLabel = "[v_content]";
            string audioMainLabel    = "[a_main_out]";

            if (videoTrack != null)
            {
                var clips = videoTrack.GetOrderedClips()
                                      .Where(c => c.IsEnabled && c.MediaItem != null).ToList();
                if (clips.Count > 0)
                {
                    hasVideo = true;
                    var concatSegs = new List<string>();

                    foreach (var clip in clips)
                    {
                        int ii = GetInputIndex(clip.MediaItem!);
                        double dur      = clip.EffectiveDuration.TotalSeconds;
                        double trimSt   = clip.TrimStart.TotalSeconds;
                        double trimEnd  = trimSt + dur;
                        string durS     = dur.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string stS      = trimSt.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string endS     = trimEnd.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string vL = $"[vc{clipCounter}]";
                        string aL = $"[ac{clipCounter}]";
                        clipCounter++;

                        // Video segment
                        if (clip.MediaItem!.Type == MediaType.Image)
                            filterComplex.Append($"[{ii}:v]trim=0:{durS},setpts=PTS-STARTPTS," +
                                $"scale={settings.Width}:{settings.Height}:force_original_aspect_ratio=decrease," +
                                $"pad={settings.Width}:{settings.Height}:(ow-iw)/2:(oh-ih)/2,format=yuv420p{vL};");
                        else
                            filterComplex.Append($"[{ii}:v]trim={stS}:{endS},setpts=PTS-STARTPTS," +
                                $"scale={settings.Width}:{settings.Height}:force_original_aspect_ratio=decrease," +
                                $"pad={settings.Width}:{settings.Height}:(ow-iw)/2:(oh-ih)/2,format=yuv420p{vL};");

                        // Audio segment for this video clip
                        if (clip.MediaItem.HasAudio && clip.MediaItem.Type != MediaType.Image && !videoTrack.IsMuted)
                        {
                            string vol = videoTrack.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            filterComplex.Append($"[{ii}:a]atrim={stS}:{endS},asetpts=PTS-STARTPTS,volume={vol},aresample=48000{aL};");
                        }
                        else
                        {
                            filterComplex.Append($"anullsrc=channel_layout=stereo:sample_rate=48000:d={durS},aresample=48000{aL};");
                        }

                        concatSegs.Add($"{vL}{aL}");
                    }

                    foreach (var s in concatSegs) filterComplex.Append(s);
                    filterComplex.Append($" concat=n={concatSegs.Count}:v=1:a=1{videoContentLabel}{audioMainLabel};");
                    audioStreamLabels.Add(audioMainLabel);
                }
            }

            // ── 2. BLACK BASE VIDEO (full project duration) ───────────────────────────
            string baseVideoLabel = "[v_base]";
            // NOTE: space before label is required by FFmpeg filter_complex syntax
            filterComplex.Append($"color=c=black:s={settings.Width}x{settings.Height}:r={settings.FrameRate}:d={totalDurStr} {baseVideoLabel};");

            string finalVideoLabel;
            if (hasVideo)
            {
                finalVideoLabel = "[v_master]";
                filterComplex.Append($"{baseVideoLabel}{videoContentLabel} overlay=0:0:eof_action=pass{finalVideoLabel};");
            }
            else
            {
                finalVideoLabel = baseVideoLabel;
            }

            // ── 3. AUDIO TRACKS ──────────────────────────────────────────────────────
            // Use adelay to place each clip at its TIMELINE position so gaps become
            // silence, then apad to extend each track stream to total duration.
            var audioTracks = project.Tracks
                                     .Where(t => t.TrackType == MediaType.Audio && t.IsEnabled).ToList();
            int trackCount = 0;

            foreach (var track in audioTracks)
            {
                var clips = track.GetOrderedClips()
                                 .Where(c => c.IsEnabled && c.MediaItem != null).ToList();
                if (clips.Count == 0) continue;

                var delayedSegs = new List<string>();

                foreach (var clip in clips)
                {
                    int ii = GetInputIndex(clip.MediaItem!);

                    double trimSt   = clip.TrimStart.TotalSeconds;
                    double clipDur  = clip.EffectiveDuration.TotalSeconds;
                    double trimEnd  = trimSt + clipDur;
                    double tlStart  = clip.StartTime.TotalSeconds; // position on timeline

                    string stS  = trimSt.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string endS = trimEnd.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string vol  = (track.IsMuted ? 0 : track.Volume)
                                    .ToString(System.Globalization.CultureInfo.InvariantCulture);

                    // adelay expects milliseconds; use same value for all channels (L|R)
                    int delayMs = (int)(tlStart * 1000);

                    string segLabel = $"[as{trackCount}_{clipCounter}]";
                    clipCounter++;

                    filterComplex.Append(
                        $"[{ii}:a]atrim={stS}:{endS},asetpts=PTS-STARTPTS," +
                        $"volume={vol},aresample=48000,adelay={delayMs}|{delayMs}{segLabel};");
                    delayedSegs.Add(segLabel);
                }

                string trackOutLabel = $"[a_track{trackCount}]";
                if (delayedSegs.Count == 1)
                {
                    filterComplex.Append($"{delayedSegs[0]}apad=whole_dur={totalDurStr}{trackOutLabel};");
                }
                else
                {
                    string segsStr = string.Join("", delayedSegs);
                    filterComplex.Append(
                        $"{segsStr} amix=inputs={delayedSegs.Count}:duration=longest," +
                        $"apad=whole_dur={totalDurStr}{trackOutLabel};");
                }

                audioStreamLabels.Add(trackOutLabel);
                trackCount++;
            }

            // ── 4. MIX ALL AUDIO STREAMS ─────────────────────────────────────────────
            string finalAudioLabel = "[a_final_out]";
            if (audioStreamLabels.Count == 0)
            {
                filterComplex.Append($"anullsrc=channel_layout=stereo:sample_rate=48000:d={totalDurStr} {finalAudioLabel};");
            }
            else if (audioStreamLabels.Count == 1)
            {
                filterComplex.Append($"{audioStreamLabels[0]}apad=whole_dur={totalDurStr}{finalAudioLabel};");
            }
            else
            {
                string allStreams = string.Join("", audioStreamLabels);
                filterComplex.Append(
                    $"{allStreams} amix=inputs={audioStreamLabels.Count}:duration=longest," +
                    $"apad=whole_dur={totalDurStr}{finalAudioLabel};");
            }

            // ── 5. SUBTITLES ─────────────────────────────────────────────────────────
            foreach (var track in project.SubtitleTracks.Where(t => t.IsEnabled))
            {
                try
                {
                    var tempFile = Path.Combine(Path.GetTempPath(),
                        $"subs_{Guid.NewGuid().ToString("n")}_{track.Id}.ass");
                    
                    // Use UTF8 without BOM for better compatibility with FFmpeg/libass
                    using (var writer = new StreamWriter(tempFile, false, new UTF8Encoding(false)))
                        WriteASS(writer, track, project.OutputWidth, project.OutputHeight);

                    // FFmpeg filter path escaping: forward slashes, escape colon for Windows, escape single quotes.
                    string esc  = tempFile.Replace("\\", "/").Replace(":", "\\:").Replace("'", "\\'");
                    
                    string next = $"[vsub{Guid.NewGuid().ToString("n").Substring(0, 8)}]";
                    filterComplex.Append($"{finalVideoLabel} ass='{esc}'{next};");
                    finalVideoLabel = next;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Subtitle generation failed: " + ex.Message);
                }
            }

            // ── 6. LOGO OVERLAY ──────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(project.LogoPath) && File.Exists(project.LogoPath))
            {
                var logoItem = new MediaItem(project.LogoPath, MediaType.Image);
                int logoIdx  = GetInputIndex(logoItem);
                double sf    = (double)settings.Width / project.OutputWidth;
                int tw       = (int)(100.0 * project.LogoScale * sf);
                int lx       = (int)(project.LogoX * sf);
                int ly       = (int)(project.LogoY * sf);

                filterComplex.Append($"[{logoIdx}:v]scale={tw}:-1[logo_sc];");
                string logoOut = "[v_logo_out]";
                filterComplex.Append($"{finalVideoLabel}[logo_sc] overlay={lx}:{ly}{logoOut};");
                finalVideoLabel = logoOut;
            }

            // ── 7. ASSEMBLE COMMAND ──────────────────────────────────────────────────
            // Strip trailing semicolons – FFmpeg rejects them
            string filterStr = filterComplex.ToString().TrimEnd(';', ' ');

            var cmd = new StringBuilder();
            cmd.Append(inputs);
            cmd.Append($"-filter_complex \"{filterStr}\" ");
            cmd.Append($"-map \"{finalVideoLabel}\" ");
            cmd.Append($"-map \"{finalAudioLabel}\" ");
            cmd.Append($"-c:v {settings.VideoCodec} -b:v {settings.VideoBitrate}k -pix_fmt yuv420p ");
            cmd.Append($"-c:a {settings.AudioCodec} -b:a {settings.AudioBitrate}k -ar 48000 ");
            cmd.Append($"-r {settings.FrameRate} ");
            cmd.Append($"-t {totalDurStr} ");   // Hard-limit output to project duration
            cmd.Append($"-y \"{settings.OutputPath}\"");

            return cmd.ToString();
        }

        /// <summary>
        /// Generates a single master SRT file from valid clips on the subtitle track.
        /// Handles offsetting and trimming.
        /// </summary>
        private string GenerateMasterSubtitleFile(TimelineTrack subTrack, int width, int height)
        {
            var masterEntries = new List<SubtitleEntry>();
            int indexCounter = 1;

            var orderedClips = subTrack.GetOrderedClips();

            foreach (var clip in orderedClips)
            {
                if (!clip.IsEnabled || clip.MediaItem == null) continue;

                try
                {
                    var parsedTrack = SubtitleParser.ParseSubtitleFile(clip.MediaItem.FilePath);
                    
                    if (parsedTrack != null && parsedTrack.Entries != null)
                    {
                        foreach (var entry in parsedTrack.Entries)
                        {
                            var entryStart = entry.StartTime - clip.TrimStart;
                            var entryEnd = entry.EndTime - clip.TrimStart;

                            if (entryEnd <= TimeSpan.Zero || entryStart >= clip.EffectiveDuration)
                                continue;

                            if (entryStart < TimeSpan.Zero) entryStart = TimeSpan.Zero;
                            if (entryEnd > clip.EffectiveDuration) entryEnd = clip.EffectiveDuration;

                            var finalStart = entryStart + clip.StartTime;
                            var finalEnd = entryEnd + clip.StartTime;

                            masterEntries.Add(new SubtitleEntry
                            {
                                Index = indexCounter++,
                                StartTime = finalStart,
                                EndTime = finalEnd,
                                Text = entry.Text,
                                Style = entry.Style
                            });
                        }
                    }
                }
                catch (Exception) { }
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"master_subs_{Guid.NewGuid()}.ass");
            
            var wrapperTrack = new SubtitleTrack
            {
                Entries = masterEntries.OrderBy(e => e.StartTime).ToList(),
                GlobalMarginL = 10,
                GlobalMarginV = 10,
                Alignment = 2,
                FontName = "Mangal",
                FontSize = 24,
                FontColor = "#FFFFFF",
                TimeOffset = TimeSpan.Zero
            };

            using (var writer = new StreamWriter(tempFile, false, Encoding.UTF8))
            {
                WriteASS(writer, wrapperTrack, width, height);
            }

            return tempFile;
        }

        /// <summary>
        /// Apply time offset to subtitles and return temp file path
        /// </summary>
        private string ApplySubtitleOffset(SubtitleTrack track)
        {
            if (track.TimeOffset == TimeSpan.Zero)
                return track.FilePath;

            var tempFile = Path.Combine(Path.GetTempPath(), $"sub_{track.Id}.{track.Format.ToLower()}");
            
            // Write adjusted subtitles
            using (var writer = new StreamWriter(tempFile, false, Encoding.UTF8))
            {
                if (track.Format == "SRT")
                {
                    WriteSRT(writer, track);
                }
                else if (track.Format == "ASS")
                {
                    WriteASS(writer, track, 1920, 1080);
                }
            }

            return tempFile;
        }

        private void WriteSRT(StreamWriter writer, SubtitleTrack track)
        {
            int index = 1;
            foreach (var entry in track.Entries)
            {
                var start = entry.StartTime + track.TimeOffset;
                var end = entry.EndTime + track.TimeOffset;

                writer.WriteLine(index++);
                writer.WriteLine($"{FormatSRTTime(start)} --> {FormatSRTTime(end)}");
                writer.WriteLine(entry.Text);
                writer.WriteLine();
            }
        }

        private void WriteASS(StreamWriter writer, SubtitleTrack track, int width, int height)
        {
            writer.WriteLine("[Script Info]");
            writer.WriteLine("ScriptType: v4.00+");
            writer.WriteLine($"PlayResX: {width}");
            writer.WriteLine($"PlayResY: {height}");
            writer.WriteLine("Timer: 100.0000");
            writer.WriteLine("UTF8: Yes");
            writer.WriteLine();
            
            writer.WriteLine("[V4+ Styles]");
            writer.WriteLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            string assColor = ColorToAss(track.FontColor);
            string outlineColor = ColorToAss(track.OutlineColor);
            string fontName = string.IsNullOrEmpty(track.FontName) || track.FontName == "Arial" ? "Nirmala UI" : track.FontName;
            
            int bold = track.IsBold ? -1 : 0;
            int italic = track.IsItalic ? -1 : 0;

            // WPF FontSize and libass (FFmpeg) FontSize differ in interpretation. 
            // 72 units in WPF (96 DPI) looks larger than 72 units in ASS.
            // A scaling factor of ~1.3 helps match the visual "heaviness" of the subtitles.
            double visualScale = 1.3;
            double scaledFontSize = track.FontSize * visualScale;
            double scaledOutline = track.OutlineWidth * visualScale;
            double scaledShadow = track.ShadowWidth * visualScale;

            string fontSize = scaledFontSize.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            string outW = scaledOutline.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            string shadW = scaledShadow.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

            // Calculate margins to match WPF's layout
            int marginL = track.GlobalMarginL;
            int marginV = track.GlobalMarginV;
            int marginR = 10; // Default right margin

            if (track.TextRegionWidth > 0)
            {
                // If user set a specific width, constrain the ASS text region
                marginR = (int)(width - marginL - track.TextRegionWidth);
                if (marginR < 0) marginR = 0;
            }

            writer.WriteLine($"Style: Default,{fontName},{fontSize},{assColor},&H000000FF,{outlineColor},{outlineColor},{bold},{italic},0,0,100,100,0,0,1,{outW},{shadW},{track.Alignment},{marginL},{marginR},{marginV},1");
            writer.WriteLine();

            writer.WriteLine("[Events]");
            writer.WriteLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
            
            foreach (var entry in track.Entries)
            {
                var start = entry.StartTime + track.TimeOffset;
                var end = entry.EndTime + track.TimeOffset;
                
                if (end <= TimeSpan.Zero) continue;
                if (start < TimeSpan.Zero) start = TimeSpan.Zero;

                string text = entry.Text.Replace("\n", "\\N").Replace("\r", "");

                // Use the calculated margins in Dialogue line too for consistency
                writer.WriteLine($"Dialogue: 0,{FormatASSTime(start)},{FormatASSTime(end)},Default,," +
                    $"{marginL},{marginR},{marginV},,{text}");
            }
        }

        private string FormatSRTTime(TimeSpan time)
        {
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
        }

        private string FormatASSTime(TimeSpan time)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
        }

        private string ColorToAss(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#") || hexColor.Length < 7) 
                return "&H00FFFFFF";
            
            try 
            {
                string r = hexColor.Substring(1, 2);
                string g = hexColor.Substring(3, 2);
                string b = hexColor.Substring(5, 2);
                // ASS is BGR, alpha is 00 for opaque
                return $"&H00{b}{g}{r}";
            } 
            catch { return "&H00FFFFFF"; }
        }

        private async Task<string> RunFFmpegAsync(string arguments, IProgress<int>? progress = null, TimeSpan totalDuration = default)
        {
            return await RunProcessAsync(_ffmpegPath, arguments, progress, totalDuration);
        }

        private async Task<string> RunFFprobeAsync(string arguments)
        {
            return await RunProcessAsync(_ffprobePath, arguments, null);
        }

        private async Task<string> RunProcessAsync(string fileName, string arguments, IProgress<int>? progress, TimeSpan totalDuration = default)
        {
            var output = new StringBuilder();
            
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    OutputReceived?.Invoke(this, e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        OutputReceived?.Invoke(this, e.Data);
                        
                        // Parse progress from FFmpeg output (time=HH:MM:SS.ss)
                        if (progress != null && totalDuration.TotalSeconds > 0 && e.Data.Contains("time="))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(
                                e.Data, @"time=(\d{2}):(\d{2}):(\d{2}\.?\d*)");
                            if (match.Success)
                            {
                                double hours   = double.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                                double minutes = double.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                                double seconds = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                                double currentSeconds = hours * 3600 + minutes * 60 + seconds;
                                
                                int percentage = (int)((currentSeconds / totalDuration.TotalSeconds) * 100);
                                // Cap at 99 here – 100 is reported explicitly after process exits
                                progress.Report(Math.Min(99, Math.Max(0, percentage)));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in FFmpeg Error Handler: {ex.Message}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            
            // Report 100% now that FFmpeg has fully exited and flushed the output file
            progress?.Report(100);

            return output.ToString();
        }

        private MediaType DetermineMediaType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            
            return ext switch
            {
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" or ".ts" or ".m4v" or ".3gp" => MediaType.Video,
                ".mp3" or ".wav" or ".aac" or ".flac" or ".ogg" or ".m4a" or ".wma" => MediaType.Audio,
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".webp" => MediaType.Image,
                ".srt" or ".ass" or ".ssa" or ".vtt" => MediaType.Subtitle,
                _ => MediaType.Video
            };
        }

        private void ParseMediaInfo(string output, MediaItem mediaItem)
        {
            var lines = output.Split('\n');
            
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "duration":
                        if (double.TryParse(value, out double duration))
                        {
                            mediaItem.Duration = TimeSpan.FromSeconds(duration);
                        }
                        break;
                    case "width":
                        int.TryParse(value, out int width);
                        mediaItem.Width = width;
                        break;
                    case "height":
                        int.TryParse(value, out int height);
                        mediaItem.Height = height;
                        break;
                    case "r_frame_rate":
                        var fps = value.Split('/');
                        if (fps.Length == 2 && double.TryParse(fps[0], out double num) && double.TryParse(fps[1], out double den) && den != 0)
                        {
                            mediaItem.FrameRate = num / den;
                        }
                        break;
                    case "channels":
                        int.TryParse(value, out int channels);
                        mediaItem.AudioChannels = channels;
                        mediaItem.HasAudio = channels > 0;
                        break;
                    case "sample_rate":
                        int.TryParse(value, out int sampleRate);
                        mediaItem.SampleRate = sampleRate;
                        break;
                }
            }
        }
    }
}

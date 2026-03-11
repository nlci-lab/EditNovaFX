using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using VideoEditor.Models;

namespace VideoEditor.Services
{
    /// <summary>
    /// Manages timeline preview playback.
    /// 
    /// Design principles:
    ///  - Video is handled by a WPF MediaElement (supports hardware-accelerated decode).
    ///  - Audio-only tracks use MediaPlayer instances (one per track).
    ///  - We NEVER drift-correct audio during active playback. We let MediaPlayer run on
    ///    its own hardware clock. Drift correction only happens on:
    ///      (a) pause/scrub — snap to expected position
    ///      (b) clip switch — re-open and seek
    ///      (c) gross drift (>1.5s) — only after file is confirmed open
    ///  - Open() is async. We suppress all position/play operations until MediaOpened fires.
    ///  - We track a per-player "ready" flag (_isPlayerReady) to know when it's safe to act.
    /// </summary>
    public class TimelinePreviewEngine
    {
        private Project? _project;
        private MediaElement _videoPlayer;
        private Image _imagePlayer;
        private TextBlock? _statusLabel;
        private TimelineClip? _currentVideoClip;
        private TimeSpan? _pendingVideoSeek = null;

        // Audio management — one player per audio track (keyed by Track ID)
        private Dictionary<string, MediaPlayer>    _audioPlayers    = new();
        private Dictionary<string, TimelineClip?>  _currentClip     = new();
        private Dictionary<string, bool>           _isPlayerReady   = new(); // true after MediaOpened fires
        private Dictionary<string, bool>           _isPlaying       = new(); // our desired state
        private Dictionary<string, TimeSpan>       _pendingSeek     = new(); // deferred seek for after MediaOpened
        private Dictionary<string, bool>           _hasClipEnded    = new(); // true when MediaEnded fires naturally

        private bool _videoIsPlaying = false;
        private bool _targetPlayingState = false; // Intended state (playing vs scrubbing)
        private TimeSpan _videoNaturalDuration = TimeSpan.Zero; // file duration, stored on MediaOpened

        public TimelinePreviewEngine(MediaElement videoPlayer, Image imagePlayer, TextBlock? statusLabel = null)
        {
            _videoPlayer = videoPlayer;
            _imagePlayer = imagePlayer;
            _statusLabel = statusLabel;
            _videoPlayer.LoadedBehavior  = MediaState.Manual;
            _videoPlayer.UnloadedBehavior = MediaState.Manual;
            _videoPlayer.MediaOpened += OnVideoMediaOpened;
            // No MediaEnded handler needed — looping is handled proactively via modulo in UpdateVideo
        }

        // ────────────────────────────────────────────────
        //  Public API
        // ────────────────────────────────────────────────

        public void SetProject(Project project) => _project = project;

        public void Update(TimeSpan timelineTime, bool playing = true)
        {
            if (_project == null) return;

            UpdateVideo(timelineTime, playing);
            UpdateAudio(timelineTime, playing);
        }

        public void Pause()
        {
            _videoPlayer.Pause();
            _videoIsPlaying = false;

            foreach (var entry in _audioPlayers)
            {
                entry.Value.Pause();
                _isPlaying[entry.Key] = false;
            }
        }

        public void Stop()
        {
            _videoPlayer.Stop();
            _videoPlayer.Source = null;
            _currentVideoClip = null;
            _pendingVideoSeek = null;
            _videoIsPlaying = false;

            CleanupAllAudioPlayers();
        }

        // ────────────────────────────────────────────────
        //  Video logic
        // ────────────────────────────────────────────────

        private void UpdateVideo(TimeSpan timelineTime, bool playing)
        {
            // Find the top-most visual clip (video or image) at this time
            TimelineClip? activeClip = null;
            foreach (var track in _project!.Tracks.Where(t => (t.TrackType == MediaType.Video || t.TrackType == MediaType.Universal) && t.IsEnabled))
            {
                var c = track.Clips.FirstOrDefault(c =>
                    timelineTime >= c.StartTime && timelineTime < c.EndTime && c.IsEnabled && 
                    (c.MediaItem?.Type == MediaType.Video || c.MediaItem?.Type == MediaType.Image));
                if (c != null) activeClip = c;
            }

            if (activeClip != _currentVideoClip)
            {
                SwitchVideoClip(activeClip, timelineTime, playing);
                return;
            }

            if (activeClip == null)
            {
                if (_videoPlayer.Source != null || _imagePlayer.Source != null)
                {
                    _videoPlayer.Source = null;
                    _imagePlayer.Source = null;
                    _videoPlayer.Visibility = System.Windows.Visibility.Collapsed;
                    _imagePlayer.Visibility = System.Windows.Visibility.Collapsed;
                    _currentVideoClip = null;
                }
                if (_statusLabel != null) _statusLabel.Text = "No visual clip at this time";
                return;
            }

            if (_statusLabel != null) _statusLabel.Text = $"Clip: {activeClip.MediaItem?.Name} ({activeClip.MediaItem?.Type})";

            // Same clip — maintain volume/mute
            var track2 = _project.Tracks.FirstOrDefault(t => t.Id == activeClip.TrackId);
            if (track2 != null)
            {
                bool mute = activeClip.IsMuted || track2.IsMuted;
                if (_videoPlayer.IsMuted != mute) _videoPlayer.IsMuted = mute;
                double vol = activeClip.Volume * track2.Volume;
                if (Math.Abs(_videoPlayer.Volume - vol) > 0.01) _videoPlayer.Volume = vol;
            }

            if (_pendingVideoSeek == null)
            {
                // Compute where in the file we should be, looping if the clip extends past the file.
                var rawOffset  = activeClip.TrimStart + (timelineTime - activeClip.StartTime);
                var loopTarget = ComputeLoopedPosition(rawOffset, activeClip.TrimStart);

                double drift = Math.Abs((_videoPlayer.Position - loopTarget).TotalSeconds);

                if (!playing)
                {
                    // Scrubbing — always snap to exact looped position
                    if (drift > 0.05) _videoPlayer.Position = loopTarget;
                }
                else
                {
                    // During playback: only correct on gross drift (stall/loop boundary)
                    if (drift > 1.0) _videoPlayer.Position = loopTarget;
                }
            }

            // Play/Pause state
            _targetPlayingState = playing;
            if (playing && !_videoIsPlaying)
            {
                _videoPlayer.Play();
                _videoIsPlaying = true;
            }
            else if (!playing && _videoIsPlaying)
            {
                _videoPlayer.Pause();
                _videoIsPlaying = false;
            }
        }

        private void SwitchVideoClip(TimelineClip? newClip, TimeSpan timelineTime, bool playing)
        {
            _currentVideoClip = newClip;
            _videoIsPlaying = false;

            if (newClip == null || newClip.MediaItem == null)
            {
                _videoPlayer.Source = null;
                _imagePlayer.Source = null;
                _videoPlayer.Visibility = System.Windows.Visibility.Collapsed;
                _imagePlayer.Visibility = System.Windows.Visibility.Collapsed;
                _pendingVideoSeek = null;
                return;
            }

            try
            {
                var uri = new Uri(newClip.MediaItem.FilePath);
                _targetPlayingState = playing;

                if (newClip.MediaItem.Type == MediaType.Image)
                {
                    if (_statusLabel != null) _statusLabel.Text = $"Loading Image: {newClip.MediaItem.Name}...";
                    // Image Logic
                    _videoPlayer.Source = null;
                    _videoPlayer.Visibility = System.Windows.Visibility.Collapsed;
                    
                    if (_imagePlayer.Source == null || _imagePlayer.Source.ToString() != uri.ToString())
                    {
                        try
                        {
                            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = uri;
                            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile;
                            bitmap.EndInit();
                            if (bitmap.CanFreeze) bitmap.Freeze();
                            _imagePlayer.Source = bitmap;
                        }
                        catch (Exception ex)
                        {
                            if (_statusLabel != null) _statusLabel.Text = $"Image Load Error: {ex.Message}";
                        }
                    }
                    _imagePlayer.Visibility = System.Windows.Visibility.Visible;
                    _pendingVideoSeek = null;
                }
                else
                {
                    // Video Logic
                    _imagePlayer.Source = null;
                    _imagePlayer.Visibility = System.Windows.Visibility.Collapsed;
                    _videoPlayer.Visibility = System.Windows.Visibility.Visible;

                    var target = newClip.TrimStart + (timelineTime - newClip.StartTime);
                    if (_videoPlayer.Source?.OriginalString != uri.OriginalString)
                    {
                        if (_statusLabel != null) _statusLabel.Text = $"Loading Video: {newClip.MediaItem.Name}...";
                        _pendingVideoSeek = target;
                        _videoPlayer.Source = uri;
                        // MediaOpened will seek + apply _targetPlayingState
                    }
                    else
                    {
                        if (Math.Abs((_videoPlayer.Position - target).TotalSeconds) > 0.1)
                        {
                            _videoPlayer.Position = target;
                        }
                        
                        if (playing) { _videoPlayer.Play(); _videoIsPlaying = true; }
                        else         
                        { 
                            _videoPlayer.Pause(); 
                            _videoIsPlaying = false;
                            
                            // Nudge to ensure frame update if paused
                            _videoPlayer.Play();
                            _videoPlayer.Pause();
                        }
                    }
                }
            }
            catch { _videoPlayer.Stop(); }
        }

        private void OnVideoMediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            // Capture the file's natural duration so we can loop by modulo later
            var nd = _videoPlayer.NaturalDuration;
            _videoNaturalDuration = nd.HasTimeSpan ? nd.TimeSpan : TimeSpan.Zero;

            if (_pendingVideoSeek.HasValue)
            {
                var target = _pendingVideoSeek.Value;
                // Apply loop even for the initial seek
                if (_currentVideoClip != null)
                    target = ComputeLoopedPosition(target, _currentVideoClip.TrimStart);
                
                _videoPlayer.Position = target;
                _pendingVideoSeek = null;
                
                if (_targetPlayingState)
                {
                    _videoPlayer.Play();
                    _videoIsPlaying = true;
                    if (_statusLabel != null) _statusLabel.Text += " [Playing]";
                }
                else
                {
                    // Force a Play/Pause cycle to ensure WPF renders at least one frame
                    _videoPlayer.Play(); 
                    _videoPlayer.Pause();
                    
                    // Nudge position slightly to force a render refresh if it's still black
                    _videoPlayer.Position = target; 
                    
                    _videoIsPlaying = false;
                    if (_statusLabel != null) _statusLabel.Text += " [Ready]";
                }
            }
            else if (!_targetPlayingState)
            {
                // No pending seek, but ensure it's "warmed up"
                _videoPlayer.Play();
                _videoPlayer.Pause();
                _videoIsPlaying = false;
                if (_statusLabel != null) _statusLabel.Text += " [Ready]";
            }
        }

        /// <summary>
        /// Given a raw file offset (which may exceed the file's natural end),
        /// wraps it back to TrimStart using modulo so the video loops seamlessly.
        /// Falls back to rawOffset if NaturalDuration is not known yet.
        /// </summary>
        private TimeSpan ComputeLoopedPosition(TimeSpan rawOffset, TimeSpan trimStart)
        {
            if (_videoNaturalDuration <= TimeSpan.Zero || rawOffset < _videoNaturalDuration)
                return rawOffset < TimeSpan.Zero ? TimeSpan.Zero : rawOffset;

            // Playable segment length: from TrimStart to end of file
            var segmentLength = _videoNaturalDuration - trimStart;
            if (segmentLength <= TimeSpan.Zero) return trimStart;

            // How far past TrimStart are we in total?
            var offsetPastTrim = rawOffset - trimStart;
            // Wrap within the segment
            var looped = TimeSpan.FromTicks(offsetPastTrim.Ticks % segmentLength.Ticks);
            return trimStart + looped;
        }

        // ────────────────────────────────────────────────
        //  Audio logic
        // ────────────────────────────────────────────────

        private void UpdateAudio(TimeSpan timelineTime, bool playing)
        {
            var audioTracks = _project!.Tracks
                .Where(t => t.TrackType == MediaType.Audio && t.IsEnabled)
                .ToList();

            var activeIds = new HashSet<string>();

            foreach (var track in audioTracks)
            {
                activeIds.Add(track.Id);
                EnsurePlayerExists(track.Id);

                var player      = _audioPlayers[track.Id];
                var activeClip  = track.Clips.FirstOrDefault(c =>
                    timelineTime >= c.StartTime && timelineTime < c.EndTime && c.IsEnabled);
                var currentClip = _currentClip[track.Id];

                // ── 1. Clip changed (or clip ended) ──────────────────────
                if (activeClip != currentClip)
                {
                    _currentClip[track.Id]   = activeClip;
                    _isPlayerReady[track.Id] = false;
                    _isPlaying[track.Id]     = false;
                    _hasClipEnded[track.Id]  = false; // reset — new clip

                    if (activeClip?.MediaItem != null)
                    {
                        var seek = activeClip.TrimStart + (timelineTime - activeClip.StartTime);
                        // Clamp to valid range
                        if (seek < TimeSpan.Zero) seek = TimeSpan.Zero;

                        _pendingSeek[track.Id] = seek;

                        ApplyVolumeMute(player, activeClip, track);

                        try
                        {
                            player.Close();
                            player.Open(new Uri(activeClip.MediaItem.FilePath));
                            // MediaOpened handler will seek + play
                            _isPlaying[track.Id] = playing; // store desired state
                        }
                        catch
                        {
                            player.Close();
                            _pendingSeek.Remove(track.Id);
                        }
                    }
                    else
                    {
                        // No active clip — stop
                        player.Stop();
                        player.Close();
                        _pendingSeek.Remove(track.Id);
                    }

                    continue; // skip sync this tick — wait for MediaOpened
                }

                // ── 2. No active clip ────────────────────────────────────
                if (activeClip == null) continue;

                // ── 3. Player not ready yet (Open still in progress) ─────
                if (!_isPlayerReady[track.Id]) continue;

                // ── 4. Ongoing playback on same clip ─────────────────────

                // If audio file has naturally finished, skip all playback logic
                // to avoid seeking/restarting the ended player.
                if (_hasClipEnded.TryGetValue(track.Id, out var clipEnded) && clipEnded)
                    continue;

                ApplyVolumeMute(player, activeClip, track);

                if (playing)
                {
                    if (!_isPlaying[track.Id])
                    {
                        // Resuming from pause — restart playback
                        player.Play();
                        _isPlaying[track.Id] = true;
                    }
                    // During active play: we do NOT drift-correct. MediaPlayer runs freely.
                    // Only gross drift (>1.5s) warrants intervention.
                    else
                    {
                        var expected = activeClip.TrimStart + (timelineTime - activeClip.StartTime);
                        double drift = Math.Abs((player.Position - expected).TotalSeconds);
                        if (drift > 1.5)
                        {
                            // Player stalled / very far off — re-seek
                            player.Position = expected < TimeSpan.Zero ? TimeSpan.Zero : expected;
                        }
                    }
                }
                else
                {
                    // Paused — snap to exact scrub position
                    if (_isPlaying[track.Id])
                    {
                        player.Pause();
                        _isPlaying[track.Id] = false;
                    }

                    var expected = activeClip.TrimStart + (timelineTime - activeClip.StartTime);
                    if (expected < TimeSpan.Zero) expected = TimeSpan.Zero;
                    double drift = Math.Abs((player.Position - expected).TotalSeconds);
                    if (drift > 0.0) player.Position = expected;
                }
            }

            // ── Cleanup removed / inactive tracks ────────────────────────
            foreach (var key in _audioPlayers.Keys.Where(k => !activeIds.Contains(k)).ToList())
            {
                _audioPlayers[key].Stop();
                _audioPlayers[key].Close();
                _audioPlayers.Remove(key);
                _currentClip.Remove(key);
                _isPlayerReady.Remove(key);
                _isPlaying.Remove(key);
                _pendingSeek.Remove(key);
                _hasClipEnded.Remove(key);
            }
        }

        private void EnsurePlayerExists(string trackId)
        {
            if (_audioPlayers.ContainsKey(trackId)) return;

            var player = new MediaPlayer();
            _audioPlayers[trackId]   = player;
            _currentClip[trackId]    = null;
            _isPlayerReady[trackId]  = false;
            _isPlaying[trackId]      = false;
            _hasClipEnded[trackId]   = false;

            // Capture for closure
            player.MediaOpened += (_, _) => OnAudioMediaOpened(trackId);
            player.MediaEnded  += (_, _) => OnAudioMediaEnded(trackId);
        }

        private void OnAudioMediaOpened(string trackId)
        {
            if (!_audioPlayers.TryGetValue(trackId, out var player)) return;

            _isPlayerReady[trackId] = true;

            // Apply deferred seek
            if (_pendingSeek.TryGetValue(trackId, out var seek))
            {
                player.Position = seek;
                _pendingSeek.Remove(trackId);
            }

            // Start playback if that's what was requested
            bool shouldPlay = _isPlaying.TryGetValue(trackId, out var sp) && sp;
            if (shouldPlay)
                player.Play();
            else
                player.Pause();
        }

        private void OnAudioMediaEnded(string trackId)
        {
            // Audio file finished naturally — mark as ended so the update loop
            // does NOT call Play() again.  _currentClip stays unchanged so the
            // engine still recognises this as the "same clip" (no re-open loop).
            if (_isPlaying.ContainsKey(trackId))
                _isPlaying[trackId] = false;
            if (_hasClipEnded.ContainsKey(trackId))
                _hasClipEnded[trackId] = true;
        }

        private static void ApplyVolumeMute(MediaPlayer player, TimelineClip clip, TimelineTrack track)
        {
            bool mute = clip.IsMuted || track.IsMuted;
            if (player.IsMuted != mute) player.IsMuted = mute;

            double vol = clip.Volume * track.Volume;
            if (Math.Abs(player.Volume - vol) > 0.01) player.Volume = vol;
        }

        private void CleanupAllAudioPlayers()
        {
            foreach (var p in _audioPlayers.Values) { p.Stop(); p.Close(); }
            _audioPlayers.Clear();
            _currentClip.Clear();
            _isPlayerReady.Clear();
            _isPlaying.Clear();
            _pendingSeek.Clear();
            _hasClipEnded.Clear();
        }
    }
}

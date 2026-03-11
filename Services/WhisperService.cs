using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VideoEditor.Services
{
    /// <summary>Describes a downloadable ggml model variant.</summary>
    public class WhisperModelInfo
    {
        public string Name { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Size { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Description { get; set; } = "";
    }

    /// <summary>Result of checking whether Whisper components exist on disk.</summary>
    public class WhisperStatus
    {
        public bool WhisperExeFound { get; set; }
        public bool ModelFound { get; set; }
        public string WhisperExePath { get; set; } = "";
        public string ModelPath { get; set; } = "";
        /// <summary>Folder where the user should save downloaded model files.</summary>
        public string ExpectedModelFolder { get; set; } = "";
        public bool IsReady => WhisperExeFound && ModelFound;
    }

    public class WhisperService
    {
        private string _whisperPath = "whisper.exe";
        private string _modelPath = "models/ggml-base.bin";

        public WhisperService()
        {
            var settings = SettingsService.Instance.CurrentSettings;
            _whisperPath = settings.WhisperExePath;
            
            // Model path is generally per-session or standard default
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _modelPath = Path.Combine(baseDir, "models", "ggml-base.bin");
        }

        public string WhisperPath 
        { 
            get => _whisperPath; 
            set => _whisperPath = value; 
        }

        public string ModelPath 
        { 
            get => _modelPath; 
            set => _modelPath = value; 
        }

        public bool IsAvailable()
        {
            if (CheckAndSetPaths(_whisperPath, _modelPath)) return true;
            
            // Try auto-detection
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));

            // Potential EXE names and locations - prioritize whisper-cli.exe to avoid deprecation warnings
            string[] exeNames = { "whisper-cli.exe", "whisper.exe", "main.exe" };
            string[] exePaths = {
                baseDir,
                Path.Combine(baseDir, "whisper"),
                Path.Combine(projectDir, "whisper-bin-x64", "Release"),
                projectDir
            };

            // Potential Model names and locations
            string[] modelNames = { "ggml-medium.bin", "ggml-base.bin", "ggml-small.bin", "ggml-large.bin" };
            string[] modelPaths = {
                baseDir,
                Path.Combine(baseDir, "models"),
                projectDir,
                Path.Combine(projectDir, "models")
            };

            string? foundExe = null;
            string? foundModel = null;

            foreach (var p in exePaths)
            {
                if (!Directory.Exists(p)) continue;
                foreach (var name in exeNames)
                {
                    string fullPath = Path.Combine(p, name);
                    if (File.Exists(fullPath))
                    {
                        foundExe = fullPath;
                        break;
                    }
                }
                if (foundExe != null) break;
            }

            foreach (var p in modelPaths)
            {
                if (!Directory.Exists(p)) continue;
                foreach (var name in modelNames)
                {
                    string fullPath = Path.Combine(p, name);
                    if (File.Exists(fullPath))
                    {
                        foundModel = fullPath;
                        break;
                    }
                }
                if (foundModel != null) break;
            }

            if (foundExe != null) 
            {
                _whisperPath = foundExe;
                
                // Update settings if auto-detection finds it
                var settings = SettingsService.Instance.CurrentSettings;
                settings.WhisperExePath = foundExe;
                SettingsService.Instance.SaveSettings();
            }
            if (foundModel != null) _modelPath = foundModel;

            return foundExe != null && foundModel != null;
        }

        /// <summary>
        /// Returns a detailed status object indicating which components were found and which are missing,
        /// including the expected folder path where model files should be placed.
        /// </summary>
        public WhisperStatus GetStatus()
        {
            var status = new WhisperStatus();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));

            // Determine the expected models folder (prefer baseDir/models)
            string expectedModelFolder = Path.Combine(baseDir, "models");
            status.ExpectedModelFolder = expectedModelFolder;

            // Check EXE
            string[] exeNames = { "whisper-cli.exe", "whisper.exe", "main.exe" };
            string[] exePaths = {
                baseDir,
                Path.Combine(baseDir, "whisper"),
                Path.Combine(projectDir, "whisper-bin-x64", "Release"),
                projectDir
            };

            foreach (var p in exePaths)
            {
                if (!Directory.Exists(p)) continue;
                foreach (var name in exeNames)
                {
                    string fullPath = Path.Combine(p, name);
                    if (File.Exists(fullPath))
                    {
                        status.WhisperExeFound = true;
                        status.WhisperExePath = fullPath;
                        break;
                    }
                }
                if (status.WhisperExeFound) break;
            }

            // Check Model
            string[] modelNames = { "ggml-medium.bin", "ggml-base.bin", "ggml-small.bin", "ggml-large.bin", "ggml-tiny.bin" };
            string[] modelFolders = {
                baseDir,
                Path.Combine(baseDir, "models"),
                projectDir,
                Path.Combine(projectDir, "models")
            };

            foreach (var folder in modelFolders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var name in modelNames)
                {
                    string fullPath = Path.Combine(folder, name);
                    if (File.Exists(fullPath))
                    {
                        status.ModelFound = true;
                        status.ModelPath = fullPath;
                        break;
                    }
                }
                if (status.ModelFound) break;
            }

            return status;
        }

        /// <summary>All known ggml model variants with Hugging Face download links.</summary>
        public static List<WhisperModelInfo> AvailableModels => new()
        {
            new() {
                Name = "Tiny",
                FileName = "ggml-tiny.bin",
                Size = "75 MB",
                Description = "Fastest, basic accuracy. Good for quick drafts.",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin"
            },
            new() {
                Name = "Base",
                FileName = "ggml-base.bin",
                Size = "142 MB",
                Description = "Good balance of speed and accuracy for most use cases.",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin"
            },
            new() {
                Name = "Small",
                FileName = "ggml-small.bin",
                Size = "466 MB",
                Description = "Better accuracy. Recommended for non-English languages.",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
            },
            new() {
                Name = "Medium",
                FileName = "ggml-medium.bin",
                Size = "1.5 GB",
                Description = "High accuracy. Best for Hindi, Tamil, and multi-language content.",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin"
            },
            new() {
                Name = "Large v3",
                FileName = "ggml-large-v3.bin",
                Size = "3.1 GB",
                Description = "Maximum accuracy. Requires a powerful PC with 8+ GB RAM.",
                DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"
            },
        };

        private bool CheckAndSetPaths(string exe, string model)
        {
            return File.Exists(exe) && File.Exists(model);
        }

        public event Action<string>? LogReceived;
        public event Action<int>? ProgressChanged;

        public async Task<string> TranscribeLocal(string audioPath, string language, bool isAss = false)
        {
            if (!IsAvailable())
            {
                string exeExists = File.Exists(_whisperPath) ? "✅ Found" : "❌ Missing";
                string modelExists = File.Exists(_modelPath) ? "✅ Found" : "❌ Missing";
                
                throw new Exception($"Local AI components not found:\n\n" +
                    $"1. Whisper Engine: {exeExists} (expected: {_whisperPath})\n" +
                    $"2. AI Model: {modelExists} (expected: {_modelPath})\n\n" +
                    $"Please ensure you have placed the files in the app folder or run 'Installation/CreateInstaller.ps1'.");
            }

            LogReceived?.Invoke("Preparing audio for AI...");
            
            // Get total duration for progress calculation
            var ffmpegInfo = new FFmpegService();
            var mediaInfo = await ffmpegInfo.GetMediaInfo(audioPath);
            double totalSeconds = mediaInfo?.Duration.TotalSeconds ?? 1.0;

            string outputBase = Path.Combine(Path.GetTempPath(), "whisper_temp_" + Guid.NewGuid().ToString("n"));
            string formatExt = isAss ? ".ass" : ".srt";
            string outputFilePath = outputBase + formatExt;
            string outputFlag = isAss ? "-oass" : "-osrt";

            // Whisper.cpp requires 16kHz WAV.
            string wavPath = await ConvertToWhisperWav(audioPath);

            try
            {
                LogReceived?.Invoke("Loading AI model (this may take a minute for 'medium')...");
                
                int threads = Environment.ProcessorCount;
                var args = $"-m \"{_modelPath}\" -f \"{wavPath}\" -l {language.ToLower()} -t {threads} -bs 1 {outputFlag} -of \"{outputBase}\"";
                
                var psi = new ProcessStartInfo
                {
                    FileName = _whisperPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                var combinedOutput = new StringBuilder();
                
                // Regex for timestamp: [00:00:10.000 --> 00:00:20.000]
                var timestampRegex = new System.Text.RegularExpressions.Regex(@"-->\s*(\d{2}:\d{2}:\d{2}\.\d{3})");
                // Regex for progress: progress = 25%
                var progressRegex = new System.Text.RegularExpressions.Regex(@"progress\s*=\s*(\d+)%");

                void HandleData(string data)
                {
                    if (string.IsNullOrEmpty(data)) return;
                    combinedOutput.AppendLine(data);
                    
                    // Filter out progress messages from the main log but use them for calculation
                    if (data.Contains("progress ="))
                    {
                        var pMatch = progressRegex.Match(data);
                        if (pMatch.Success && int.TryParse(pMatch.Groups[1].Value, out int pVal))
                        {
                            ProgressChanged?.Invoke(pVal);
                        }
                    }
                    else
                    {
                        LogReceived?.Invoke(data);
                        
                        // Fallback: Parse from timestamps if progress reporting is not available
                        var tMatch = timestampRegex.Match(data);
                        if (tMatch.Success && totalSeconds > 0)
                        {
                            if (TimeSpan.TryParse(tMatch.Groups[1].Value, out var currentTs))
                            {
                                int progress = (int)((currentTs.TotalSeconds / totalSeconds) * 100);
                                ProgressChanged?.Invoke(Math.Clamp(progress, 0, 100));
                            }
                        }
                    }
                }

                process.OutputDataReceived += (s, e) => { if (e.Data != null) HandleData(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) HandleData(e.Data); };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (File.Exists(outputFilePath))
                {
                    ProgressChanged?.Invoke(100);
                    return await File.ReadAllTextAsync(outputFilePath, Encoding.UTF8);
                }

                string err = combinedOutput.ToString();
                int exitCode = process.ExitCode;

                if (err.Contains("failed to load model"))
                    throw new Exception("Local AI Error: Failed to load the model file. It may be corrupted or in an unsupported format.");

                throw new Exception($"Local AI failed (Exit Code: {exitCode}) to generate {formatExt}.\n\nConsole Output:\n{err}");
            }
            finally
            {
                if (File.Exists(wavPath)) try { File.Delete(wavPath); } catch { }
                if (File.Exists(outputFilePath)) try { File.Delete(outputFilePath); } catch { }
            }
        }

        private async Task<string> ConvertToWhisperWav(string audioPath)
        {
            string wavPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            
            // FFmpeg command to convert to 16kHz mono WAV (required by whisper.cpp)
            var ffmpeg = new FFmpegService();
            var args = $"-i \"{audioPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{wavPath}\"";
            
            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg.FFmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            await process.WaitForExitAsync();

            return wavPath;
        }
    }
}

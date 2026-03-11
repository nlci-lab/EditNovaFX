using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Reflection;
using Newtonsoft.Json;

namespace VideoEditor.Services
{
    public class YouTubePublishingService
    {
        private YouTubeService? _youtubeService;
        private UserCredential? _credential;

        private string _clientId = string.Empty;
        private string _clientSecret = string.Empty;

        // To use YouTube publishing, create your own Google API credentials:
        // 1. Go to https://console.cloud.google.com/
        // 2. Create a project, enable YouTube Data API v3
        // 3. Create OAuth 2.0 credentials (Desktop application)
        // 4. Enter your Client ID and Secret via Tools → Setup YouTube API in the app
        //    (they are stored in %AppData%/EditNovaFX/youtube_secrets.json)
        private const string DEFAULT_CLIENT_ID = "";
        private const string DEFAULT_CLIENT_SECRET = "";

        private readonly string _configPath;

        public YouTubePublishingService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _configPath = Path.Combine(appData, "EditNovaFX", "youtube_secrets.json");
            LoadCredentials();
        }

        public string ClientId => _clientId;
        public bool HasCredentials => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);
        public bool IsAuthenticated => _credential != null;

        public void LoadCredentials()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    var data = JsonConvert.DeserializeObject<dynamic>(json);
                    _clientId = data?.client_id ?? string.Empty;
                    _clientSecret = data?.client_secret ?? string.Empty;
                }
                catch { }
            }

            // Fallback to built-in defaults (empty unless you compile your own)
            if (string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(DEFAULT_CLIENT_ID))
            {
                _clientId = DEFAULT_CLIENT_ID;
                _clientSecret = DEFAULT_CLIENT_SECRET;
            }
        }

        public void SaveCredentials(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;

            string directory = Path.GetDirectoryName(_configPath)!;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            string json = JsonConvert.SerializeObject(new { client_id = clientId, client_secret = clientSecret });
            File.WriteAllText(_configPath, json);
        }

        public async Task<bool> AuthorizeAsync()
        {
            if (!HasCredentials) return false;

            try
            {
                string[] scopes = { YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeReadonly };

                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($@"{{""installed"":{{""client_id"":""{_clientId}"",""client_secret"":""{_clientSecret}""}}}}")))
                {
                    // The file token.json stores the user's access and refresh tokens, and is
                    // created automatically when the authorization flow completes for the first time.
                    string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EditNovaFX", "token.json");
                    
                    _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                _youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "EditNovaFX"
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<(string Title, string ThumbnailUrl)> GetChannelInfoAsync()
        {
            if (_youtubeService == null) return ("Not Logged In", "");

            var channelsListRequest = _youtubeService.Channels.List("snippet");
            channelsListRequest.Mine = true;

            var channelsListResponse = await channelsListRequest.ExecuteAsync();

            if (channelsListResponse.Items.Count > 0)
            {
                var channel = channelsListResponse.Items[0];
                return (channel.Snippet.Title, channel.Snippet.Thumbnails.Default__.Url);
            }

            return ("Unknown Channel", "");
        }

        public async Task<string> UploadVideoAsync(string filePath, string title, string description, string[] tags, string privacyStatus, IProgress<int> progress)
        {
            if (_youtubeService == null) throw new InvalidOperationException("Not authenticated");

            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = title;
            video.Snippet.Description = description;
            video.Snippet.Tags = tags;
            video.Snippet.CategoryId = "22"; // See https://developers.google.com/youtube/v3/docs/videoCategories/list
            video.Status = new VideoStatus { PrivacyStatus = privacyStatus };

            string uploadedVideoId = string.Empty;

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var videosInsertRequest = _youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += (p) =>
                {
                    switch (p.Status)
                    {
                        case Google.Apis.Upload.UploadStatus.Uploading:
                            progress.Report((int)((double)p.BytesSent / fileStream.Length * 100));
                            break;
                        case Google.Apis.Upload.UploadStatus.Completed:
                            progress.Report(100);
                            break;
                        case Google.Apis.Upload.UploadStatus.Failed:
                            throw p.Exception;
                    }
                };

                var response = await videosInsertRequest.UploadAsync();
                uploadedVideoId = videosInsertRequest.ResponseBody?.Id ?? string.Empty;
            }

            return uploadedVideoId;
        }

        /// <summary>
        /// Uploads a custom thumbnail for an already-uploaded video.
        /// Supported formats: JPG, PNG. Max size: 2 MB. Recommended: 1280×720 px.
        /// Note: The YouTube channel must have custom thumbnails enabled (verified account).
        /// </summary>
        /// <param name="videoId">The ID of the uploaded video.</param>
        /// <param name="thumbnailPath">Absolute path to the thumbnail image file.</param>
        public async Task UploadThumbnailAsync(string videoId, string thumbnailPath)
        {
            if (_youtubeService == null) throw new InvalidOperationException("Not authenticated");
            if (string.IsNullOrEmpty(videoId)) throw new ArgumentException("Video ID is required.");
            if (!File.Exists(thumbnailPath)) throw new FileNotFoundException("Thumbnail file not found.", thumbnailPath);

            // Determine MIME type from extension
            string ext = Path.GetExtension(thumbnailPath).ToLowerInvariant();
            string mimeType = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "image/jpeg"
            };

            using var fileStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read);
            var thumbnailRequest = _youtubeService.Thumbnails.Set(videoId, fileStream, mimeType);
            await thumbnailRequest.UploadAsync();
        }
    }
}

using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using MusicJournal.Models;

namespace MusicJournal.Services
{
    public class SpotifyService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private string? _cachedToken;
        private DateTime _tokenExpiration;

        public SpotifyService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        private async Task<string> GetAccessToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _cachedToken;
            }

            var clientId = _config["Spotify:ClientId"];
            var clientSecret = _config["Spotify:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Spotify ClientId or ClientSecret is missing from appsettings.");
            }

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to authenticate with Spotify ({response.StatusCode}): {errBody}");
            }

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            _cachedToken = json["access_token"]?.ToString() ?? throw new InvalidOperationException("Failed to retrieve access token.");
            
            var expiresInSeconds = json["expires_in"]?.Value<int>() ?? 3600;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresInSeconds - 300);

            return _cachedToken;
        }

        public async Task<List<GlobalTrack>> GetTop50Playlist(int year)
        {
            var token = await GetAccessToken();
            
            var url = $"https://api.spotify.com/v1/search?q=year:{year}&type=track&limit=50&market=US";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Spotify API error: {response.StatusCode} - {responseBody}");
            }

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var items = json["tracks"]?["items"];

            var globalTracks = new List<GlobalTrack>();

            if (items != null)
            {
                int currentRank = 1;
                foreach (var item in items)
                {
                    globalTracks.Add(new GlobalTrack
                    {
                        Title = item["name"]?.ToString() ?? "Unknown Title",
                        Artist = item["artists"]?[0]?["name"]?.ToString() ?? "Unknown Artist",
                        Album = item["album"]?["name"]?.ToString(),
                        Rank = currentRank++,
                        SpotifyId = item["id"]?.ToString(),
                        SpotifyUrl = item["external_urls"]?["spotify"]?.ToString(),
                        AlbumArtUrl = item["album"]?["images"]?[0]?["url"]?.ToString()
                    });
                }
            }

            return globalTracks;
        }

        public async Task<List<FavoriteTrack>> GetSearchedFavorites(string query, int page = 1, int limit = 10, string? market = "US")
        {
            var token = await GetAccessToken();
            var offset = (page - 1) * limit;
            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit={limit}&offset={offset}&market=US";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<FavoriteTrack>();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var items = json["tracks"]?["items"];

            var results = new List<FavoriteTrack>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    results.Add(new FavoriteTrack
                    {
                        Title = item["name"]?.ToString() ?? "",
                        Artist = item["artists"]?[0]?["name"]?.ToString() ?? "",
                        Album = item["album"]?["name"]?.ToString() ?? "",
                        SpotifyUrl = item["external_urls"]?["spotify"]?.ToString() ?? "",
                        AlbumArtUrl = item["album"]?["images"]?[0]?["url"]?.ToString() ?? "",
                        SpotifyId = item["id"]?.ToString() ?? ""
                    });
                }
            }

            return results;
        }

        // Overload to support GetTrackInfo(title, artist) used in RecommendationsController
        public async Task<RecTrack?> GetTrackInfo(string title, string artist)
        {
            var token = await GetAccessToken();
            var query = Uri.EscapeDataString($"track:{title} artist:{artist}");
            var url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit=1&market=US";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var item = json["tracks"]?["items"]?.FirstOrDefault();

            if (item == null) return null;

            return new RecTrack
            {
                Title = item["name"]?.ToString() ?? title,
                Artist = item["artists"]?[0]?["name"]?.ToString() ?? artist,
                Album = item["album"]?["name"]?.ToString(),
                SpotifyUrl = item["external_urls"]?["spotify"]?.ToString(),
                AlbumArtUrl = item["album"]?["images"]?[0]?["url"]?.ToString(),
                SpotifyId = item["id"]?.ToString()
            };
        }

        public async Task<List<RecTrack>> GetSpotifyRecommendations(List<string> favoriteTracks, int count = 5)
        {
            var recs = new List<RecTrack>();
            var token = await GetAccessToken();

            var random = new Random();
            var seedTracks = favoriteTracks.OrderBy(_ => random.Next()).Take(3).ToList();

            if (!seedTracks.Any())
            {
                seedTracks.Add("pop hits");
            }

            foreach (var seed in seedTracks)
            {
                var query = Uri.EscapeDataString(seed);
                var url = $"https://api.spotify.com/v1/search?q={query}&type=track&limit={count}&market=US";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) continue;

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                var items = json["tracks"]?["items"];

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        recs.Add(new RecTrack
                        {
                            Title = item["name"]?.ToString(),
                            Artist = item["artists"]?[0]?["name"]?.ToString(),
                            Album = item["album"]?["name"]?.ToString(),
                            SpotifyUrl = item["external_urls"]?["spotify"]?.ToString(),
                            AlbumArtUrl = item["album"]?["images"]?[0]?["url"]?.ToString(),
                            SpotifyId = item["id"]?.ToString()
                        });
                    }
                }
            }

            return recs.DistinctBy(r => $"{r.Title}|{r.Artist}".ToLower())
                       .OrderBy(_ => random.Next())
                       .Take(count)
                       .ToList();
        }
    }
}
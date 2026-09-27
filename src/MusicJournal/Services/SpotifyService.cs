using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using MusicJournal.Models;
using MusicJournal.Models.ViewModels;

namespace MusicJournal.Services
{
    public class SpotifyService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        private string _accessToken = null!; // limit on access token, cache it
        private DateTime _tokenExpiration;

        public SpotifyService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        private async Task<string> GetAccessToken()
        {
            // if token exists and is not expired, reuse it
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return _accessToken;
            }

            var clientId = _config["Spotify:ClientId"];
            var clientSecret = _config["Spotify:ClientSecret"];
            var auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"grant_type", "client_credentials" }
        });

            var response = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get access token: {response.StatusCode} - {responseBody}");
            }

            var json = JObject.Parse(responseBody);
            _accessToken = json["access_token"]!.ToString();

            // expires_in is in seconds
            var expiresIn = json["expires_in"]?.ToObject<int>() ?? 3600;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60); // subtract 60 sec buffer

            return _accessToken;
        }

        public async Task<List<GlobalTrack>> GetTop50Playlist(int year)
        {
            var token = await GetAccessToken();

            // Get popular tracks by the year
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q=year:{year}&type=track&limit=50&market=US");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {response.StatusCode} - {responseBody}");
            }

            var json = JObject.Parse(responseBody);
            var tracks = json["tracks"]["items"];

            var result = new List<GlobalTrack>();
            int rank = 1;

            foreach (var track in tracks)
            {
                if (track == null) continue;

                result.Add(new GlobalTrack
                {
                    Rank = rank++,
                    Title = track["name"]?.ToString(),
                    Artist = track["artists"][0]["name"]?.ToString(),
                    Album = track["album"]["name"]?.ToString(),
                    SpotifyUrl = track["external_urls"]["spotify"]?.ToString(),
                    AlbumArtUrl = track["album"]["images"][0]["url"]?.ToString(),
                    //Genre = "N/a"

                    SpotifyId = track["id"]?.ToString()
                });
            }

            return result;
        }

        public async Task<(List<TrackResult> Results, bool HasMoreResults)> GetSearchedFavorites(string search_query, int searchPages, int searchSize, string search_country)
        {
            if (string.IsNullOrWhiteSpace(search_query))
            {
                return (new List<TrackResult>(), false);
            }

            // Authorization for Spotify API
            var token = await GetAccessToken();

            // Offset used for getting more results if needed
            //var total_searches = searchPages * searchSize;
            var search_offset = (searchPages - 1) * searchSize;

            // Get tracks based on search query
            var search_request = $"https://api.spotify.com/v1/search?q=${Uri.EscapeDataString(search_query)}&type=track&market={search_country}&limit={searchSize+1}&offset={search_offset}";
            var request = new HttpRequestMessage(HttpMethod.Get, search_request);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API error: {response.StatusCode} - {responseBody}");
            }

            var json = JObject.Parse(responseBody);
            var tracks = json["tracks"]["items"];

            var result = new List<TrackResult>();

            foreach (var track in tracks)
            {
                if (track == null) continue;

                result.Add(new TrackResult
                {
                    Title = track["name"]?.ToString(),
                    Artist = track["artists"][0]["name"]?.ToString(),
                    Album = track["album"]["name"]?.ToString(),
                    SpotifyUrl = track["external_urls"]["spotify"]?.ToString(),
                    AlbumArtUrl = track["album"]["images"][0]["url"]?.ToString(),

                    SpotifyId = track["id"]?.ToString()
                });
            }

            bool HasMoreResults = result.Count > searchSize;

            if (HasMoreResults)
            {
                result.RemoveAt(result.Count - 1);
            }

            return (result, HasMoreResults);
        }

        public async Task<RecTrack> GetTrackInfo(string title, string artist)
        {
            var token = await GetAccessToken();
            string query = $"{title} {artist}";
            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1&market=US";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var item = json["tracks"]["items"]?.FirstOrDefault();

            if (item == null) return null;

            return new RecTrack
            {
                Title = item["name"]?.ToString(),
                Artist = item["artists"][0]["name"]?.ToString(),
                Album = item["album"]["name"]?.ToString(),
                SpotifyUrl = item["external_urls"]["spotify"]?.ToString(),
                AlbumArtUrl = item["album"]["images"]?[0]?["url"]?.ToString(),

                SpotifyId = item["id"]?.ToString()
            };
        }

        public async Task<List<RecTrack>> GetSpotifyRecommendations(List<string> favoriteTracks, int count = 5)
        {
            var recs = new List<RecTrack>();
            var token = await GetAccessToken();

            // Pick up to 3 random favorite track strings to use as search inspiration
            var random = new Random();
            var seedTracks = favoriteTracks.OrderBy(_ => random.Next()).Take(3).ToList();

            foreach (var seed in seedTracks)
            {
                // Extract title/artist keyword
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

            // Shuffle and take requested count
            return recs.DistinctBy(r => $"{r.Title}|{r.Artist}".ToLower())
                    .OrderBy(_ => random.Next())
                    .Take(count)
                    .ToList();
        }
    }
}
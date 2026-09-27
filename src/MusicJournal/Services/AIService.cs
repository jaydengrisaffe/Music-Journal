using MusicJournal.Models;

namespace MusicJournal.Services
{
    public class AIService : IAIService
    {
        private readonly SpotifyService _spotify;

        public AIService(SpotifyService spotify)
        {
            _spotify = spotify;
        }

        public async Task<List<string>> GenerateMusicRecommendations(List<string> favoriteTracks, int count = 5)
        {
            var recs = await _spotify.GetSpotifyRecommendations(favoriteTracks, count);
            return recs.Select(r => $"{r.Title} by {r.Artist}").ToList();
        }

        public async Task<List<RecTrack>> GenerateRecTracks(List<string> favoriteTracks, int count = 5)
        {
            return await _spotify.GetSpotifyRecommendations(favoriteTracks, count);
        }
    }
}
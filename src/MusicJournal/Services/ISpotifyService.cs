using MusicJournal.Models;

namespace MusicJournal.Services
{
    public interface ISpotifyService
    {
        Task<List<GlobalTrack>> GetTop50Playlist(int year);
        Task<List<FavoriteTrack>> GetSearchedFavorites(string query, int page = 1, int limit = 10, string? market = "US");
        Task<FavoriteTrack?> GetTrackInfo(string trackId, string? market = "US");
        Task<RecTrack?> GetTrackInfo(string title, string artist);
        Task<List<RecTrack>> GetSpotifyRecommendations(List<string> favoriteTracks, int count = 5);
    }
}
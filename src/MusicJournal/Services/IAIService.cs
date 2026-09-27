using MusicJournal.Models;

namespace MusicJournal.Services
{
    public interface IAIService
    {
        Task<List<string>> GenerateMusicRecommendations(List<string> favoriteTracks, int count = 5);
        Task<List<RecTrack>> GenerateRecTracks(List<string> favoriteTracks, int count = 5);
    }
}
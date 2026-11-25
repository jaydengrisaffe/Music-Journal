namespace MusicJournal.Models.ViewModels
{
    public class CreateFavoriteViewModel
    {
        public string SearchQuery { get; set; } = string.Empty;
        public int SearchPages { get; set; }
        public int SearchSize { get; set; }
        public bool HasMoreResults { get; set; }

        public List<TrackResult> SearchResults { get; set; }

        public CreateFavoriteViewModel()
        {
            SearchResults = new List<TrackResult>();
            SearchPages = 1;
            SearchSize = 15;
            HasMoreResults = false;
        }
    }

    public class TrackResult
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string SpotifyUrl { get; set; } = string.Empty;
        public string AlbumArtUrl { get; set; } = string.Empty;
        public string SpotifyId { get; set; } = string.Empty;
    }
}
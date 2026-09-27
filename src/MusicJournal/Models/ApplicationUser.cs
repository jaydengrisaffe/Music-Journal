using Microsoft.AspNetCore.Identity;

namespace MusicJournal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public byte[]? ProfilePic { get; set; }
        public virtual ICollection<FavoriteTrack> FavoriteTracks { get; set; } = new List<FavoriteTrack>();
    }
}
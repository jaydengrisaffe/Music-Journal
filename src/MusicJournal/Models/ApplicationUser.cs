using Microsoft.AspNetCore.Identity;

namespace MusicJournal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<FavoriteTrack> FavoriteTracks { get; set; } = new List<FavoriteTrack>();
    }
}
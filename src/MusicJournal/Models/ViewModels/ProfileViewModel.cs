using System.ComponentModel.DataAnnotations;
using MusicJournal.Models;

namespace MusicJournal.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<FavoriteTrack> FavoriteTracks { get; set; } = new();
    }
}
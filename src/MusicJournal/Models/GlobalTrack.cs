using System.ComponentModel.DataAnnotations;

namespace MusicJournal.Models
{
    public class GlobalTrack
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(200)]
        public string Artist { get; set; }

        [StringLength(200)]
        public string? Album { get; set; }

        public int? Rank { get; set; }

        [StringLength(500)]
        public string? SpotifyUrl { get; set; }

        [StringLength(500)]
        public string? SpotifyId { get; set; }



        [StringLength(500)]
        public string? AlbumArtUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
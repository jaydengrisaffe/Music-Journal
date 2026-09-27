using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicJournal.Models
{
    public class FavoriteTrack
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(200)]
        public string Artist { get; set; }

        [StringLength(200)]
        public string? Album { get; set; }

        [StringLength(500)]
        public string? SpotifyUrl { get; set; }

        [StringLength(50)]
        public string? SpotifyId { get; set; }

        [ForeignKey("GlobalTrack")]
        public int GlobalTrackId { get; set; }

        public GlobalTrack GlobalTrack { get; set; }

        [StringLength(500)]
        public string? AlbumArtUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser? User { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace MusicJournal.Models
{
	public class RecTrack
	{
		[Required]
		[StringLength(200)]
		public string Title { get; set; } = "";

		[Required]
		[StringLength(200)]
		public string Artist { get; set; } = "";

		[StringLength(200)]
		public string Album { get; set; } = "";

		[StringLength(500)]
		public string SpotifyUrl { get; set; } = "";

        [StringLength(500)]
        public string SpotifyId { get; set; } = "";

        [StringLength(500)]
		public string AlbumArtUrl { get; set; } = "";

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
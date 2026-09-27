using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Services;
using Microsoft.AspNetCore.Identity;

namespace MusicJournal.Controllers
{
    [Authorize]
    public class RecommendationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly SpotifyService _spotify;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecommendationsController(
            ApplicationDbContext context,
            IAIService aiService,
            SpotifyService spotify,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _aiService = aiService;
            _spotify = spotify;
            _userManager = userManager;
        }

        // GET: Recommendations
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var favoriteTracks = await _context.FavoriteTracks
                .Where(f => f.UserId == user.Id)
                .ToListAsync();

            if (!favoriteTracks.Any())
            {
                ViewBag.Message = "Please add some favorite tracks to get recommendations.";
                ViewBag.AllFavorites = new List<FavoriteTrack>();
                return View(new List<RecTrack>());
            }

            // Convert favorites to string descriptions for AI input
            var trackDescriptions = favoriteTracks
                .Select(f => $"{f.Title} by {f.Artist}")
                .ToList();

            // Generate recommendations using AI
            var aiRecommendations = await _aiService.GenerateRecTracks(trackDescriptions, 10);

            var fullRecs = new List<RecTrack>();
            foreach (var rec in aiRecommendations)
            {
                var fullInfo = await _spotify.GetTrackInfo(rec.Title, rec.Artist);
                if (fullInfo != null)
                    fullRecs.Add(fullInfo);
            }

            // Filter out tracks that user already favorited
            var filteredRecs = RemoveAlreadyFavorited(fullRecs, favoriteTracks);

            ViewBag.AllFavorites = favoriteTracks;

            return View(filteredRecs);
        }

        // POST: Add to favorites
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(
            string title, string artist, string album, string spotifyUrl, string albumArtUrl, string SpotifyId)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(artist))// || string.IsNullOrEmpty(SpotifyId))
                return BadRequest();

            var user = await _userManager.GetUserAsync(User);

            var exists = await _context.FavoriteTracks
                .AnyAsync(f => f.UserId == user.Id && f.Title == title && f.Artist == artist);

            if (!exists)
            {
                var globalTrack = await _context.GlobalTracks
                                        .FirstOrDefaultAsync(t => t.SpotifyId == SpotifyId);

                if (globalTrack == null)
                {
                    globalTrack = new GlobalTrack
                    {
                        //UserId = user.Id,
                        Title = title,
                        Artist = artist,
                        Album = album,
                        SpotifyUrl = spotifyUrl,
                        AlbumArtUrl = albumArtUrl,
                        SpotifyId = SpotifyId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.GlobalTracks.Add(globalTrack);
                    await _context.SaveChangesAsync();
                }

                var favoriteTrack = new FavoriteTrack
                {
                    UserId = user.Id,
                    GlobalTrackId = globalTrack.Id,
                    Title = title,
                    Artist = artist,
                    Album = album,
                    SpotifyUrl = spotifyUrl,
                    AlbumArtUrl = albumArtUrl,
                    SpotifyId = SpotifyId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FavoriteTracks.Add(favoriteTrack);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Regenerate recommendations
        public async Task<IActionResult> GenerateNew()
        {
            return RedirectToAction("Index");
        }

        // POST: Generate recommendations based on selected favorites
        [HttpPost]
        public async Task<IActionResult> GenerateFromSelected(List<string> selected)
        {
            if (selected == null || !selected.Any())
                return RedirectToAction("Index");

            // Generate AI recs from selected favorites
            var aiRecommendations = await _aiService.GenerateRecTracks(selected, 10);

            var fullRecs = new List<RecTrack>();
            foreach (var rec in aiRecommendations)
            {
                var fullInfo = await _spotify.GetTrackInfo(rec.Title, rec.Artist);
                if (fullInfo != null)
                    fullRecs.Add(fullInfo);
            }

            var user = await _userManager.GetUserAsync(User);
            var favorites = await _context.FavoriteTracks
                .Where(f => f.UserId == user.Id)
                .ToListAsync();

            ViewBag.AllFavorites = favorites;

            // Filter duplicates
            var filteredRecs = RemoveAlreadyFavorited(fullRecs, favorites);

            return View("Index", filteredRecs);
        }

        private List<RecTrack> RemoveAlreadyFavorited(List<RecTrack> recs, List<FavoriteTrack> favorites)
        {
            var favKeys = new HashSet<string>(
                favorites.Select(f => $"{f.Title}|{f.Artist}".ToLower())
            );

            return recs
                .Where(r => !favKeys.Contains($"{r.Title}|{r.Artist}".ToLower()))
                .ToList();
        }
    }
}
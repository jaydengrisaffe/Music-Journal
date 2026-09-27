using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MusicJournal.Data;
using MusicJournal.Models;
using Microsoft.AspNetCore.Identity;
using MusicJournal.Services;
using MusicJournal.Models.ViewModels;

namespace MusicJournal.Controllers
{
    public class FavoriteTracksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SpotifyService _spotify;
        public FavoriteTracksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SpotifyService spotify)
        {
            _context = context;
            _userManager = userManager;
            _spotify = spotify;
        }
        // GET: FavoriteTracks
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var favorites = await _context.FavoriteTracks
                .Where(f => f.UserId == user.Id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(favorites);
        }
        // GET: FavoriteTracks/Create
        public async Task<IActionResult> Create(string searchQuery, int searchPages = 1)
        {
            const int SEARCH_LIMIT = 15;
            var viewModel = new CreateFavoriteViewModel
            {
                SearchQuery = searchQuery,
                SearchPages = searchPages,
                SearchSize = SEARCH_LIMIT
            };

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var user = await _userManager.GetUserAsync(User);
                var searchLocation = "US";
                if (user.Location != null)
                {
                    searchLocation = user.Location;
                }
                var results = await _spotify.GetSearchedFavorites(searchQuery, searchPages, SEARCH_LIMIT, searchLocation);
                viewModel.SearchResults = results.Results;
                viewModel.HasMoreResults = results.HasMoreResults;
            }
            return View(viewModel);
        }

        // POST: FavoriteTracks/AddToFavorites
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(FavoriteTrack favoriteTrack)
        {
            var user = await _userManager.GetUserAsync(User);

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("GlobalTrackId");
            ModelState.Remove("GlobalTrack");

            // Check if chosen song is already in favorites
            var existingFavorite = await _context.FavoriteTracks.FirstOrDefaultAsync(
                f => f.UserId == user.Id && f.Title == favoriteTrack.Title && f.Artist == favoriteTrack.Artist);



            if (existingFavorite != null)
            {
                TempData["Message"] = "This song is already in your favorites!";
                TempData["MessageType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                var globalTrack = await _context.GlobalTracks
                                        .FirstOrDefaultAsync(t => t.SpotifyId == favoriteTrack.SpotifyId);

                if (globalTrack == null)
                {
                    // Create the global record so the Admin Charts can see it
                    globalTrack = new GlobalTrack
                    {
                        SpotifyId = favoriteTrack.SpotifyId,
                        Title = favoriteTrack.Title,
                        Artist = favoriteTrack.Artist,
                        Album = favoriteTrack.Album,
                        SpotifyUrl = favoriteTrack.SpotifyUrl,
                        AlbumArtUrl = favoriteTrack.AlbumArtUrl,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.GlobalTracks.Add(globalTrack);
                    await _context.SaveChangesAsync();
                }

                favoriteTrack.UserId = user.Id;
                favoriteTrack.CreatedAt = DateTime.Now;

                favoriteTrack.GlobalTrackId = globalTrack.Id;

                _context.Add(favoriteTrack);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"{favoriteTrack.Title} has been added to your favorites!";
                TempData["MessageType"] = "success";

                return RedirectToAction(nameof(Index));
            }

            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            TempData["Message"] = $"Error adding favorite: {string.Join(" | ", errorMessages)}";
            TempData["MessageType"] = "danger";
            return RedirectToAction(nameof(Create));
        }

        // GET: FavoriteTracks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var favoriteTrack = await _context.FavoriteTracks
                .Where(f => f.UserId == user.Id) // Their own tracks
                .FirstOrDefaultAsync(m => m.Id == id);

            if (favoriteTrack == null)
            {
                return NotFound();
            }

            return View(favoriteTrack);
        }

        // POST: FavoriteTracks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var favoriteTrack = await _context.FavoriteTracks
                .Where(f => f.UserId == user.Id)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (favoriteTrack != null)
            {
                _context.FavoriteTracks.Remove(favoriteTrack);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: FavoriteTracks/AddFromTrending
        [HttpPost]
        public async Task<IActionResult> AddFromTrending([FromBody] FavoriteTrack favoriteTrack)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "You must be logged in." });
            }

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("CreatedAt");

            // Check if already in favorites
            var existingFavorite = await _context.FavoriteTracks.FirstOrDefaultAsync(
                f => f.UserId == user.Id &&
                     f.Title == favoriteTrack.Title &&
                     f.Artist == favoriteTrack.Artist);

            if (existingFavorite != null)
            {
                return Json(new { success = false, message = "Already in favorites." });
            }

            var globalTrack = await _context.GlobalTracks
                                    .FirstOrDefaultAsync(t => t.SpotifyId == favoriteTrack.SpotifyId);

            if (globalTrack == null)
            {
                globalTrack = new GlobalTrack
                {
                    SpotifyId = favoriteTrack.SpotifyId,
                    Title = favoriteTrack.Title,
                    Artist = favoriteTrack.Artist,
                    Album = favoriteTrack.Album,
                    SpotifyUrl = favoriteTrack.SpotifyUrl,
                    AlbumArtUrl = favoriteTrack.AlbumArtUrl,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GlobalTracks.Add(globalTrack);
                await _context.SaveChangesAsync();
            }

            favoriteTrack.UserId = user.Id;
            favoriteTrack.CreatedAt = DateTime.Now;

            favoriteTrack.GlobalTrackId = globalTrack.Id;

            _context.Add(favoriteTrack);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MusicJournal.Controllers
{
    public class GlobalTracksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SpotifyService _spotify;

        public GlobalTracksController(ApplicationDbContext context, SpotifyService spotify)
        {
            _context = context;
            _spotify = spotify;
        }

        // GET: GlobalTracks
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Fetch trending from Spotify directly
            var year = DateTime.Now.Year;
            var tracks = await _spotify.GetTop50Playlist(year);

            List<string> favoritedTracks = new List<string>();

            if (User.Identity!.IsAuthenticated)
            {
                // get favorited tracks
                favoritedTracks = await _context.FavoriteTracks
                    .Where(f => f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
                    .Select(f => $"{f.Title}|{f.Artist}")
                    .ToListAsync();
            }

            ViewBag.FavoritedTracks = favoritedTracks;

            return View(tracks);
        }

        // GET: GlobalTracks/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.GlobalTracks == null)
            {
                return NotFound();
            }

            var globalTrack = await _context.GlobalTracks
                .FirstOrDefaultAsync(m => m.Id == id);

            if (globalTrack == null)
            {
                return NotFound();
            }

            return View(globalTrack);
        }

        // GET: GlobalTracks/GetByYear?year={year}
        [AllowAnonymous]
        public async Task<IActionResult> GetByYear(int year)
        {
            var tracks = await _spotify.GetTop50Playlist(year);

            List<string> favoritedTracks = new List<string>();
            if (User.Identity!.IsAuthenticated)
            {
                favoritedTracks = await _context.FavoriteTracks
                    .Where(f => f.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
                    .Select(f => $"{f.Title}|{f.Artist}")
                    .ToListAsync();
            }

            ViewBag.FavoritedTracks = favoritedTracks;

            // return partial view for ajax
            return PartialView("_TracksPartial", tracks);
        }

    }
}
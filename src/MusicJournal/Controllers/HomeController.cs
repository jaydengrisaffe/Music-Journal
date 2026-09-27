using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Models.ViewModels;
using MusicJournal.Services;

namespace MusicJournal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAIService _aiService;
        private readonly SpotifyService _spotifyService;

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IAIService aiService,
            SpotifyService spotifyService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _aiService = aiService;
            _spotifyService = spotifyService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    // Fetch user's favorite tracks to generate a recommendation
                    var favorites = await _context.FavoriteTracks
                        .Where(f => f.UserId == user.Id)
                        .ToListAsync();

                    if (favorites.Any())
                    {
                        try
                        {
                            var trackDescriptions = favorites
                                .Take(5)
                                .Select(f => $"{f.Title} by {f.Artist}")
                                .ToList();

                            // Uses updated Spotify-backed recommendation engine
                            var recommendations = await _aiService.GenerateMusicRecommendations(trackDescriptions, 1);

                            if (recommendations.Any())
                            {
                                ViewBag.TopRecommendation = recommendations.First();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error getting top recommendation for home page.");
                        }
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
using System.Diagnostics;
using MusicJournal.Models;
using Microsoft.AspNetCore.Mvc;
using MusicJournal.Data;
using MusicJournal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MusicJournal.Models.ViewModels;

namespace MusicJournal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAIService _aiservice;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAIService aiservice)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _aiservice = aiservice;
        }

        public async Task<IActionResult> Index()
        {
            if(User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                // get the favs to generate a rec
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

                        var recommendations = await _aiservice.GenerateMusicRecommendations(trackDescriptions, 1);

                        if (recommendations.Any())
                        {
                            ViewBag.TopRecommendation = recommendations.First();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting recommendation");
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
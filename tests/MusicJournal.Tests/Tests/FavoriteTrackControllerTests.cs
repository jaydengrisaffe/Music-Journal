using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicJournal.Controllers;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Models.ViewModels;
using MusicJournal.Services;
using Xunit;

namespace MusicJournal.Tests;

public class FavoriteTracksControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private UserManager<ApplicationUser> GetMockUserManager(ApplicationUser? testUser)
    {
        var store = new FakeUserStore();
        if (testUser != null)
        {
            store.Users.Add(testUser);
        }

        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[0],
            new IPasswordValidator<ApplicationUser>[0],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new Logger<UserManager<ApplicationUser>>(new LoggerFactory())
        );
    }

    private SpotifyService CreateStubSpotifyService()
    {
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Spotify:ClientId", "mock-client-id" },
                { "Spotify:ClientSecret", "mock-client-secret" }
            })
            .Build();

        return new SpotifyService(httpClient, config);
    }

    private void SetControllerUserContext(Controller controller, ApplicationUser? user)
    {
        var claims = new List<Claim>();
        if (user != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Name, user.UserName ?? "testuser"));
        }

        var identity = user != null ? new ClaimsIdentity(claims, "TestAuth") : new ClaimsIdentity();
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new DictionaryTempDataProvider());
    }

    [Fact]
    public async Task Index_ReturnsViewWithOnlyCurrentUserFavorites()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var otherUser = new ApplicationUser { Id = "user-2", UserName = "listener2" };

            context.FavoriteTracks.AddRange(
                new FavoriteTrack { Id = 1, UserId = user.Id, Title = "Song A", Artist = "Artist A" },
                new FavoriteTrack { Id = 2, UserId = user.Id, Title = "Song B", Artist = "Artist B" },
                new FavoriteTrack { Id = 3, UserId = otherUser.Id, Title = "Other Song", Artist = "Other Artist" }
            );
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<FavoriteTrack>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.All(model, track => Assert.Equal(user.Id, track.UserId));
        }
    }

    [Fact]
    public async Task Create_EmptyQuery_ReturnsViewWithEmptyResults()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Create(searchQuery: "");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateFavoriteViewModel>(viewResult.Model);
            Assert.Empty(model.SearchResults);
        }
    }

    [Fact]
    public async Task Create_WithSearchQueryAndCustomLocation_PopulatesResults()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1", Location = "CA" };
            var userManager = GetMockUserManager(user);
            var spotifyService = CreateStubSpotifyService();
            var controller = new FavoriteTracksController(context, userManager, spotifyService);
            SetControllerUserContext(controller, user);

            var result = await controller.Create(searchQuery: "Bohemian Rhapsody", searchPages: 1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateFavoriteViewModel>(viewResult.Model);
            Assert.NotEmpty(model.SearchResults);
        }
    }

    [Fact]
    public async Task AddToFavorites_DuplicateTrack_RedirectsWithWarningAndDoesNotAdd()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.FavoriteTracks.Add(new FavoriteTrack
            {
                Id = 10,
                UserId = user.Id,
                Title = "Duplicate Song",
                Artist = "Duplicate Artist",
                SpotifyId = "dup-123"
            });
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var duplicateAttempt = new FavoriteTrack
            {
                Title = "Duplicate Song",
                Artist = "Duplicate Artist",
                SpotifyId = "dup-123"
            };

            var result = await controller.AddToFavorites(duplicateAttempt);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(FavoriteTracksController.Index), redirectResult.ActionName);
            Assert.Equal("warning", controller.TempData["MessageType"]);
            Assert.Equal(1, await context.FavoriteTracks.CountAsync());
        }
    }

    [Fact]
    public async Task AddToFavorites_ValidNewTrack_CreatesGlobalTrackAndRedirects()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var newTrack = new FavoriteTrack
            {
                Title = "Brand New Song",
                Artist = "Fresh Artist",
                SpotifyId = "new-spotify-1",
                Album = "Fresh Album"
            };

            var result = await controller.AddToFavorites(newTrack);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(FavoriteTracksController.Index), redirectResult.ActionName);
            Assert.Equal("success", controller.TempData["MessageType"]);
            Assert.Equal(1, await context.FavoriteTracks.CountAsync());
            Assert.Equal(1, await context.GlobalTracks.CountAsync());
        }
    }

    [Fact]
    public async Task AddToFavorites_ValidTrackWithExistingGlobalTrack_LinksGlobalTrack()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.GlobalTracks.Add(new GlobalTrack
            {
                Id = 88,
                SpotifyId = "existing-spot-id",
                Title = "Existing Hit",
                Artist = "Star"
            });
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var newTrack = new FavoriteTrack
            {
                Title = "Existing Hit",
                Artist = "Star",
                SpotifyId = "existing-spot-id"
            };

            var result = await controller.AddToFavorites(newTrack);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(FavoriteTracksController.Index), redirect.ActionName);

            var added = await context.FavoriteTracks.FirstAsync();
            Assert.Equal(88, added.GlobalTrackId);
        }
    }

    [Fact]
    public async Task AddToFavorites_InvalidModelState_RedirectsToCreateWithError()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            controller.ModelState.AddModelError("Title", "Title is required");

            var invalidTrack = new FavoriteTrack { Artist = "Only Artist" };
            var result = await controller.AddToFavorites(invalidTrack);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(FavoriteTracksController.Create), redirectResult.ActionName);
            Assert.Equal("danger", controller.TempData["MessageType"]);
        }
    }

    [Fact]
    public async Task Delete_NullId_ReturnsNotFound()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Delete(null);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task Delete_NonExistentId_ReturnsNotFound()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task Delete_ValidIdOwnTrack_ReturnsViewWithTrack()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var track = new FavoriteTrack { Id = 42, UserId = user.Id, Title = "Delete Me", Artist = "Band" };
            context.FavoriteTracks.Add(track);
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Delete(42);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FavoriteTrack>(viewResult.Model);
            Assert.Equal(42, model.Id);
        }
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesTrackFromDatabase()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.FavoriteTracks.Add(new FavoriteTrack { Id = 55, UserId = user.Id, Title = "Drop Me", Artist = "Band" });
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.DeleteConfirmed(55);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(FavoriteTracksController.Index), redirect.ActionName);
            Assert.Empty(await context.FavoriteTracks.ToListAsync());
        }
    }

    [Fact]
    public async Task AddFromTrending_UnauthenticatedUser_ReturnsUnauthorizedJson()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var userManager = GetMockUserManager(null);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, null);

            var result = await controller.AddFromTrending(new FavoriteTrack { Title = "Song", Artist = "Artist" });

            var jsonResult = Assert.IsType<JsonResult>(result);
            var json = JsonSerializer.Serialize(jsonResult.Value);
            Assert.Contains("\"success\":false", json);
            Assert.Contains("You must be logged in", json);
        }
    }

    [Fact]
    public async Task AddFromTrending_DuplicateSong_ReturnsAlreadyInFavoritesJson()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.FavoriteTracks.Add(new FavoriteTrack
            {
                Id = 1,
                UserId = user.Id,
                Title = "Already Added",
                Artist = "Artist"
            });
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var duplicate = new FavoriteTrack { Title = "Already Added", Artist = "Artist" };
            var result = await controller.AddFromTrending(duplicate);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var json = JsonSerializer.Serialize(jsonResult.Value);
            Assert.Contains("\"success\":false", json);
            Assert.Contains("Already in favorites", json);
        }
    }

    [Fact]
    public async Task AddFromTrending_ValidNewTrack_CreatesGlobalTrackAndReturnsSuccess()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new FavoriteTracksController(context, userManager, null!);
            SetControllerUserContext(controller, user);

            var newTrack = new FavoriteTrack
            {
                SpotifyId = "trending-spotify-1",
                Title = "Viral Track",
                Artist = "Trending Star"
            };

            var result = await controller.AddFromTrending(newTrack);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var json = JsonSerializer.Serialize(jsonResult.Value);
            Assert.Contains("\"success\":true", json);
            Assert.Equal(1, await context.FavoriteTracks.CountAsync());
            Assert.Equal(1, await context.GlobalTracks.CountAsync());
        }
    }
}

// Minimal in-memory TempDataProvider to avoid missing reference errors
public class DictionaryTempDataProvider : ITempDataProvider
{
    private IDictionary<string, object> _values = new Dictionary<string, object>();
    public IDictionary<string, object> LoadTempData(HttpContext context) => _values;
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _values = values ?? new Dictionary<string, object>();
}

// Minimal in-memory user store for UserManager
public class FakeUserStore : IUserStore<ApplicationUser>
{
    public List<ApplicationUser> Users = new();

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) { Users.Add(user); return Task.FromResult(IdentityResult.Success); }
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) { Users.Remove(user); return Task.FromResult(IdentityResult.Success); }
    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct) => Task.FromResult(Users.Find(u => u.Id == userId));
    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct) => Task.FromResult(Users.Find(u => u.UserName == normalizedUserName));
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.UserName?.ToUpperInvariant());
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Id);
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.UserName);
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct) => Task.CompletedTask;
    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(IdentityResult.Success);
    public void Dispose() { }
}
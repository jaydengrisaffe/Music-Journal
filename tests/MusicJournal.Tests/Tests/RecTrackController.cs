using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MusicJournal.Controllers;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Services;
using Xunit;

namespace MusicJournal.Tests;

public class RecommendationsControllerTests
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

    private UserManager<ApplicationUser> GetMockUserManager(ApplicationUser testUser)
    {
        var store = new FakeUserStore();
        store.Users.Add(testUser);

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

    private void SetControllerUserContext(Controller controller, ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private SpotifyService CreateStubSpotifyService()
    {
        var fakeHandler = new RecFakeHttpMessageHandler();
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

    [Fact]
    public async Task Index_NoFavoriteTracks_ReturnsEmptyViewWithNotice()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var mockAi = new Mock<IAIService>();
            var spotify = CreateStubSpotifyService();

            var controller = new RecommendationsController(context, mockAi.Object, spotify, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<RecTrack>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("Please add some favorite tracks to get recommendations.", controller.ViewBag.Message);
        }
    }

    [Fact]
    public async Task Index_WithFavorites_GeneratesAndFiltersRecommendations()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.FavoriteTracks.AddRange(
                new FavoriteTrack { Id = 1, UserId = user.Id, Title = "Existing Song", Artist = "Known Artist" }
            );
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var mockAi = new Mock<IAIService>();
            mockAi.Setup(a => a.GenerateRecTracks(It.IsAny<List<string>>(), 10))
                  .ReturnsAsync(new List<RecTrack>
                  {
                      new RecTrack { Title = "Existing Song", Artist = "Known Artist" }, // Duplicate - should be filtered out
                      new RecTrack { Title = "Fresh Song", Artist = "New Artist" }       // New - should be kept
                  });

            var spotify = CreateStubSpotifyService();
            var controller = new RecommendationsController(context, mockAi.Object, spotify, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<RecTrack>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Fresh Song", model[0].Title);
            Assert.NotNull(controller.ViewBag.AllFavorites);
        }
    }

    [Theory]
    [InlineData("", "Artist")]
    [InlineData("Title", "")]
    [InlineData(null, "Artist")]
    [InlineData("Title", null)]
    public async Task AddToFavorites_MissingTitleOrArtist_ReturnsBadRequest(string? title, string? artist)
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new RecommendationsController(context, null!, null!, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.AddToFavorites(title!, artist!, "Album", "http://spotify", "http://art", "spot-id");

            Assert.IsType<BadRequestResult>(result);
        }
    }

    [Fact]
    public async Task AddToFavorites_NewTrack_CreatesGlobalAndFavoriteTrack()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new RecommendationsController(context, null!, null!, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.AddToFavorites(
                "Fresh Rec", "Cool Artist", "Album X", "http://spotify/1", "http://art/1", "rec-spot-1");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(RecommendationsController.Index), redirect.ActionName);

            var globalTrack = await context.GlobalTracks.FirstOrDefaultAsync(g => g.SpotifyId == "rec-spot-1");
            Assert.NotNull(globalTrack);
            Assert.Equal("Fresh Rec", globalTrack.Title);

            var fav = await context.FavoriteTracks.FirstOrDefaultAsync(f => f.SpotifyId == "rec-spot-1");
            Assert.NotNull(fav);
            Assert.Equal(globalTrack.Id, fav.GlobalTrackId);
            Assert.Equal(user.Id, fav.UserId);
        }
    }

    [Fact]
    public async Task AddToFavorites_ExistingGlobalTrack_LinksExistingGlobalId()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var existingGlobal = new GlobalTrack
            {
                Id = 99,
                SpotifyId = "shared-spot-99",
                Title = "Shared Song",
                Artist = "Shared Artist"
            };
            context.GlobalTracks.Add(existingGlobal);
            await context.SaveChangesAsync();

            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            var userManager = GetMockUserManager(user);
            var controller = new RecommendationsController(context, null!, null!, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.AddToFavorites(
                "Shared Song", "Shared Artist", "Album", "http://url", "http://art", "shared-spot-99");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(RecommendationsController.Index), redirect.ActionName);

            var fav = await context.FavoriteTracks.FirstOrDefaultAsync(f => f.SpotifyId == "shared-spot-99");
            Assert.NotNull(fav);
            Assert.Equal(99, fav.GlobalTrackId);
            Assert.Equal(1, await context.GlobalTracks.CountAsync()); // No second global track created
        }
    }

    [Fact]
    public async Task AddToFavorites_AlreadyInFavorites_DoesNotDuplicate()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.FavoriteTracks.Add(new FavoriteTrack
            {
                Id = 1,
                UserId = user.Id,
                Title = "Existing Hit",
                Artist = "Star",
                SpotifyId = "dup-spot-1",
                GlobalTrackId = 1
            });
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var controller = new RecommendationsController(context, null!, null!, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.AddToFavorites(
                "Existing Hit", "Star", "Album", "http://url", "http://art", "dup-spot-1");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(RecommendationsController.Index), redirect.ActionName);
            Assert.Equal(1, await context.FavoriteTracks.CountAsync());
        }
    }

    [Fact]
    public async Task GenerateNew_RedirectsToIndex()
    {
        var controller = new RecommendationsController(null!, null!, null!, null!);
        var result = await controller.GenerateNew();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task GenerateFromSelected_NullOrEmptyList_RedirectsToIndex()
    {
        var controller = new RecommendationsController(null!, null!, null!, null!);

        var nullResult = await controller.GenerateFromSelected(null!);
        var emptyResult = await controller.GenerateFromSelected(new List<string>());

        var redirect1 = Assert.IsType<RedirectToActionResult>(nullResult);
        var redirect2 = Assert.IsType<RedirectToActionResult>(emptyResult);
        Assert.Equal("Index", redirect1.ActionName);
        Assert.Equal("Index", redirect2.ActionName);
    }

    [Fact]
    public async Task GenerateFromSelected_ValidList_ReturnsViewWithFilteredRecs()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };
            context.FavoriteTracks.Add(new FavoriteTrack
            {
                Id = 1,
                UserId = user.Id,
                Title = "Track Already Saved",
                Artist = "Artist"
            });
            await context.SaveChangesAsync();

            var userManager = GetMockUserManager(user);
            var mockAi = new Mock<IAIService>();
            mockAi.Setup(a => a.GenerateRecTracks(It.IsAny<List<string>>(), 10))
                  .ReturnsAsync(new List<RecTrack>
                  {
                      new RecTrack { Title = "Track Already Saved", Artist = "Artist" },
                      new RecTrack { Title = "Fresh Pick", Artist = "Fresh Artist" }
                  });

            var spotify = CreateStubSpotifyService();
            var controller = new RecommendationsController(context, mockAi.Object, spotify, userManager);
            SetControllerUserContext(controller, user);

            var result = await controller.GenerateFromSelected(new List<string> { "Track Already Saved by Artist" });

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            var model = Assert.IsAssignableFrom<List<RecTrack>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Fresh Song", model[0].Title);
        }
    }
}

public class RecFakeHttpMessageHandler : HttpMessageHandler
{
    private int _searchCallCount = 0;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string rawUri = request.RequestUri?.ToString() ?? "";

        if (rawUri.Contains("api/token"))
        {
            var tokenJson = "{\"access_token\":\"mock-token-xyz\",\"token_type\":\"Bearer\",\"expires_in\":3600}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenJson, Encoding.UTF8, "application/json")
            });
        }

        string decodedUri = Uri.UnescapeDataString(rawUri);

        string songTitle = "Fresh Song";
        string artistName = "New Artist";

        // Check decoded URI for either duplicate track
        if (decodedUri.Contains("Existing Song", StringComparison.OrdinalIgnoreCase) ||
            decodedUri.Contains("Track Already Saved", StringComparison.OrdinalIgnoreCase) ||
            _searchCallCount == 0)
        {
            // First call corresponds to the already-saved favorite
            if (decodedUri.Contains("Track Already Saved", StringComparison.OrdinalIgnoreCase))
            {
                songTitle = "Track Already Saved";
                artistName = "Artist";
            }
            else
            {
                songTitle = "Existing Song";
                artistName = "Known Artist";
            }
        }
        else
        {
            songTitle = "Fresh Song";
            artistName = "New Artist";
        }

        _searchCallCount++;

        var searchPayload = $@"{{
            ""tracks"": {{
                ""items"": [{{
                    ""id"": ""rec-spot-{_searchCallCount}"",
                    ""name"": ""{songTitle}"",
                    ""artists"": [{{ ""name"": ""{artistName}"" }}],
                    ""album"": {{
                        ""name"": ""Sample Album"",
                        ""images"": [{{ ""url"": ""https://example.com/art.jpg"" }}]
                    }},
                    ""external_urls"": {{ ""spotify"": ""https://open.spotify.com/track/rec-spot-{_searchCallCount}"" }}
                }}]
            }}
        }}";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(searchPayload, Encoding.UTF8, "application/json")
        });
    }
}
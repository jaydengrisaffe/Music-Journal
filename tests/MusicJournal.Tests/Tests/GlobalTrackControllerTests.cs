using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MusicJournal.Controllers;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Services;
using Xunit;

namespace MusicJournal.Tests;

public class GlobalTracksControllerTests
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

    private void SetControllerUserContext(Controller controller, string? userId = null)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.Add(new Claim(ClaimTypes.Name, "testuser"));
        }

        var identity = string.IsNullOrEmpty(userId)
            ? new ClaimsIdentity()
            : new ClaimsIdentity(claims, "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // Creates a SpotifyService backed by a mock HTTP response
    private SpotifyService CreateStubSpotifyService()
    {
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler);

        // Dummy Spotify API credentials in memory
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
    public async Task Index_UnauthenticatedUser_ReturnsViewWithTracksAndEmptyFavorites()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var spotifyService = CreateStubSpotifyService();
            var controller = new GlobalTracksController(context, spotifyService);
            SetControllerUserContext(controller, userId: null);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<GlobalTrack>>(viewResult.Model);
            Assert.NotEmpty(model);

            var favoritedTracks = (List<string>)controller.ViewBag.FavoritedTracks;
            Assert.Empty(favoritedTracks);
        }
    }

    [Fact]
    public async Task Index_AuthenticatedUser_PopulatesFavoritedTracksInViewBag()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            string userId = "user-123";
            context.FavoriteTracks.AddRange(
                new FavoriteTrack { Id = 1, UserId = userId, Title = "Test Song", Artist = "Test Artist" },
                new FavoriteTrack { Id = 2, UserId = "other-user", Title = "Other Song", Artist = "Other Artist" }
            );
            await context.SaveChangesAsync();

            var spotifyService = CreateStubSpotifyService();
            var controller = new GlobalTracksController(context, spotifyService);
            SetControllerUserContext(controller, userId: userId);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var favoritedTracks = (List<string>)controller.ViewBag.FavoritedTracks;
            Assert.Single(favoritedTracks);
            Assert.Contains("Test Song|Test Artist", favoritedTracks);
        }
    }

    [Fact]
    public async Task GetByYear_UnauthenticatedUser_ReturnsPartialViewWithTracks()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var spotifyService = CreateStubSpotifyService();
            var controller = new GlobalTracksController(context, spotifyService);
            SetControllerUserContext(controller, userId: null);

            var result = await controller.GetByYear(2023);

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TracksPartial", partialResult.ViewName);
            var model = Assert.IsAssignableFrom<List<GlobalTrack>>(partialResult.Model);
            Assert.NotEmpty(model);
        }
    }

    [Fact]
    public async Task GetByYear_AuthenticatedUser_MapsUserFavoritesInViewBag()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            string userId = "user-999";
            context.FavoriteTracks.Add(new FavoriteTrack
            {
                Id = 1,
                UserId = userId,
                Title = "Test Song",
                Artist = "Test Artist"
            });
            await context.SaveChangesAsync();

            var spotifyService = CreateStubSpotifyService();
            var controller = new GlobalTracksController(context, spotifyService);
            SetControllerUserContext(controller, userId: userId);

            var result = await controller.GetByYear(1999);

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_TracksPartial", partialResult.ViewName);
            var favorited = (List<string>)controller.ViewBag.FavoritedTracks;
            Assert.Single(favorited);
            Assert.Contains("Test Song|Test Artist", favorited);
        }
    }

    [Fact]
    public async Task Details_NullId_ReturnsNotFound()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var controller = new GlobalTracksController(context, null!);
            SetControllerUserContext(controller);

            var result = await controller.Details(null);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task Details_NonExistentId_ReturnsNotFound()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var controller = new GlobalTracksController(context, null!);
            SetControllerUserContext(controller);

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }

    [Fact]
    public async Task Details_ExistingId_ReturnsViewWithCorrectTrack()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var track = new GlobalTrack
            {
                Id = 15,
                Title = "Stairway to Heaven",
                Artist = "Led Zeppelin",
                Album = "Led Zeppelin IV",
                SpotifyId = "5CQ30WqJwcep0pYcV4AMNc"
            };
            context.GlobalTracks.Add(track);
            await context.SaveChangesAsync();

            var controller = new GlobalTracksController(context, null!);
            SetControllerUserContext(controller);

            var result = await controller.Details(15);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<GlobalTrack>(viewResult.Model);
            Assert.Equal(15, model.Id);
            Assert.Equal("Stairway to Heaven", model.Title);
            Assert.Equal("Led Zeppelin", model.Artist);
        }
    }
}

// Intercepts all outgoing HTTP calls from SpotifyService and returns canned responses
public class FakeHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string uri = request.RequestUri?.ToString() ?? "";

        // Spotify OAuth token endpoint
        if (uri.Contains("api/token"))
        {
            var tokenJson = "{\"access_token\":\"mock-token-xyz\",\"token_type\":\"Bearer\",\"expires_in\":3600}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenJson, Encoding.UTF8, "application/json")
            });
        }

        // Mock search/playlist payload containing a sample track
        var tracksJson = @"{
            ""playlists"": {
                ""items"": [{
                    ""id"": ""mock-playlist-id""
                }]
            },
            ""tracks"": {
                ""items"": [{
                    ""track"": {
                        ""id"": ""mock-track-id"",
                        ""name"": ""Test Song"",
                        ""artists"": [{ ""name"": ""Test Artist"" }],
                        ""album"": {
                            ""name"": ""Test Album"",
                            ""images"": [{ ""url"": ""https://example.com/art.jpg"" }]
                        },
                        ""external_urls"": { ""spotify"": ""https://open.spotify.com/track/mock-track-id"" }
                    }
                }]
            },
            ""items"": [{
                ""track"": {
                    ""id"": ""mock-track-id"",
                    ""name"": ""Test Song"",
                    ""artists"": [{ ""name"": ""Test Artist"" }],
                    ""album"": {
                        ""name"": ""Test Album"",
                        ""images"": [{ ""url"": ""https://example.com/art.jpg"" }]
                    },
                    ""external_urls"": { ""spotify"": ""https://open.spotify.com/track/mock-track-id"" }
                }
            }]
        }";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tracksJson, Encoding.UTF8, "application/json")
        });
    }
}
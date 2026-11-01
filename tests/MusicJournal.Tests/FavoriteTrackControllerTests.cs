using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicJournal.Controllers;
using MusicJournal.Data;
using MusicJournal.Models;
using Xunit;

namespace MusicJournal.Tests;

public class FavoriteTracksControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // Disable SQLite foreign key checks so test records can be inserted freely
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
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

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
    public async Task Delete_ValidIdOwnTrack_ReturnsViewWithTrack()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "listener1" };

            var track = new FavoriteTrack
            {
                Id = 42,
                UserId = user.Id,
                Title = "Track to Delete",
                Artist = "Artist"
            };
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

            var track = new FavoriteTrack
            {
                Id = 55,
                UserId = user.Id,
                Title = "Song To Remove",
                Artist = "Artist"
            };
            context.FavoriteTracks.Add(track);
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
}

public class DictionaryTempDataProvider : ITempDataProvider
{
    private IDictionary<string, object> _values = new Dictionary<string, object>();
    public IDictionary<string, object> LoadTempData(HttpContext context) => _values;
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _values = values ?? new Dictionary<string, object>();
}

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
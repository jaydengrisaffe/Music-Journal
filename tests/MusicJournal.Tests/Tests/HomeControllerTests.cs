using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MusicJournal.Controllers;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Models.ViewModels;
using MusicJournal.Services;
using Xunit;

namespace MusicJournal.Tests;

public class HomeControllerTests
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

    private void SetControllerUserContext(Controller controller, ApplicationUser? user)
    {
        var claims = new List<Claim>();
        if (user != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Name, user.UserName ?? "testuser"));
        }

        var identity = user != null ? new ClaimsIdentity(claims, "TestAuth") : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Index_UnauthenticatedUser_ReturnsViewWithoutRecommendation()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var logger = new Mock<ILogger<HomeController>>();
            var userManager = GetMockUserManager(null);
            var mockAi = new Mock<IAIService>();

            var controller = new HomeController(logger.Object, context, userManager, mockAi.Object, null!);
            SetControllerUserContext(controller, null);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(controller.ViewBag.TopRecommendation);
        }
    }

    [Fact]
    public async Task Index_AuthenticatedUserWithNoFavorites_ReturnsViewWithoutRecommendation()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-home-1", UserName = "listener" };
            var logger = new Mock<ILogger<HomeController>>();
            var userManager = GetMockUserManager(user);
            var mockAi = new Mock<IAIService>();

            var controller = new HomeController(logger.Object, context, userManager, mockAi.Object, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(controller.ViewBag.TopRecommendation);
            mockAi.Verify(a => a.GenerateMusicRecommendations(It.IsAny<List<string>>(), It.IsAny<int>()), Times.Never);
        }
    }

    [Fact]
    public async Task Index_AuthenticatedUserWithFavorites_PopulatesTopRecommendationInViewBag()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-home-2", UserName = "listener2" };
            context.FavoriteTracks.AddRange(
                new FavoriteTrack { Id = 1, UserId = user.Id, Title = "Bohemian Rhapsody", Artist = "Queen" },
                new FavoriteTrack { Id = 2, UserId = user.Id, Title = "Don't Stop Me Now", Artist = "Queen" }
            );
            await context.SaveChangesAsync();

            var logger = new Mock<ILogger<HomeController>>();
            var userManager = GetMockUserManager(user);
            var mockAi = new Mock<IAIService>();
            mockAi.Setup(a => a.GenerateMusicRecommendations(It.IsAny<List<string>>(), 1))
                  .ReturnsAsync(new List<string> { "Radio Ga Ga by Queen" });

            var controller = new HomeController(logger.Object, context, userManager, mockAi.Object, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Radio Ga Ga by Queen", controller.ViewBag.TopRecommendation);
        }
    }

    [Fact]
    public async Task Index_AiServiceThrows_CatchesExceptionAndLogsError()
    {
        using var context = GetInMemoryDbContext(out var connection);
        using (connection)
        {
            var user = new ApplicationUser { Id = "user-home-3", UserName = "listener3" };
            context.FavoriteTracks.Add(new FavoriteTrack { Id = 1, UserId = user.Id, Title = "Track", Artist = "Band" });
            await context.SaveChangesAsync();

            var logger = new Mock<ILogger<HomeController>>();
            var userManager = GetMockUserManager(user);
            var mockAi = new Mock<IAIService>();
            mockAi.Setup(a => a.GenerateMusicRecommendations(It.IsAny<List<string>>(), 1))
                  .ThrowsAsync(new Exception("AI service unavailable"));

            var controller = new HomeController(logger.Object, context, userManager, mockAi.Object, null!);
            SetControllerUserContext(controller, user);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(controller.ViewBag.TopRecommendation);
            logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var controller = new HomeController(null!, null!, null!, null!, null!);
        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_WithTraceIdentifier_ReturnsViewWithErrorViewModel()
    {
        var controller = new HomeController(null!, null!, null!, null!, null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-test-id-123" }
        };

        var result = controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.Equal("trace-test-id-123", model.RequestId);
    }

    [Fact]
    public void Error_WithActiveActivity_UsesActivityId()
    {
        var activity = new Activity("TestActivity");
        activity.Start();

        try
        {
            var controller = new HomeController(null!, null!, null!, null!, null!);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-fallback" }
            };

            var result = controller.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal(activity.Id, model.RequestId);
        }
        finally
        {
            activity.Stop();
        }
    }
}
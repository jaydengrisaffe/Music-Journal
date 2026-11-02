using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MusicJournal.Data;
using MusicJournal.Models;
using MusicJournal.Services;

namespace MusicJournal.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
        
        builder.ConfigureServices(services =>
        {
            // 1. Remove production DbContext
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor != null)
            {
                services.Remove(dbDescriptor);
            }

            // 2. Open persistent SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                cmd.ExecuteNonQuery();
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // 3. Mock AI service so integration tests don't call real APIs
            var aiDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAIService));
            if (aiDescriptor != null)
            {
                services.Remove(aiDescriptor);
            }

            var mockAi = new Mock<IAIService>();
            mockAi.Setup(m => m.GenerateMusicRecommendations(It.IsAny<List<string>>(), It.IsAny<int>()))
                  .ReturnsAsync(new List<string> { "Test Track by Test Artist" });
            mockAi.Setup(m => m.GenerateRecTracks(It.IsAny<List<string>>(), It.IsAny<int>()))
                  .ReturnsAsync(new List<RecTrack>
                  {
                      new RecTrack { Title = "Test Track", Artist = "Test Artist", SpotifyId = "spotify-1" }
                  });

            services.AddSingleton(mockAi.Object);

            // 4. Initialize schema and seed test user
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any(u => u.Id == "test-user-id"))
            {
                var testUser = new ApplicationUser
                {
                    Id = "test-user-id",
                    UserName = "testuser@example.com",
                    NormalizedUserName = "TESTUSER@EXAMPLE.COM",
                    Email = "testuser@example.com",
                    NormalizedEmail = "TESTUSER@EXAMPLE.COM",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                db.Users.Add(testUser);
                db.SaveChanges();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Close();
        _connection?.Dispose();
    }
}
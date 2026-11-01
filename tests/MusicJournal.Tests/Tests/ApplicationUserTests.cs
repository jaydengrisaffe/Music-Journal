using System.Collections.Generic;
using MusicJournal.Models;
using Xunit;

namespace MusicJournal.Tests;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_PropertyGettersAndSetters_WorkAsExpected()
    {
        var sampleBytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        var customFavorites = new List<FavoriteTrack>
        {
            new FavoriteTrack { Id = 1, Title = "Track A", Artist = "Artist A" }
        };

        var user = new ApplicationUser
        {
            Id = "user-abc-123",
            UserName = "johndoe",
            Email = "john@example.com",
            DisplayName = "John Doe",
            Bio = "Music enthusiast and developer",
            Location = "US",
            ProfilePic = sampleBytes,
            FavoriteTracks = customFavorites
        };

        Assert.Equal("user-abc-123", user.Id);
        Assert.Equal("johndoe", user.UserName);
        Assert.Equal("john@example.com", user.Email);
        Assert.Equal("John Doe", user.DisplayName);
        Assert.Equal("Music enthusiast and developer", user.Bio);
        Assert.Equal("US", user.Location);
        Assert.Equal(sampleBytes, user.ProfilePic);
        Assert.Same(customFavorites, user.FavoriteTracks);
        Assert.Single(user.FavoriteTracks);
    }

    [Fact]
    public void ApplicationUser_DefaultInitialization_SetsDefaultCollectionAndNullableProperties()
    {
        var user = new ApplicationUser();

        Assert.NotNull(user.FavoriteTracks);
        Assert.Empty(user.FavoriteTracks);
        Assert.Null(user.DisplayName);
        Assert.Null(user.Bio);
        Assert.Null(user.Location);
        Assert.Null(user.ProfilePic);
    }
}
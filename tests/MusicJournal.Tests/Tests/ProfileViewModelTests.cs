using System.Collections.Generic;
using MusicJournal.Models;
using MusicJournal.Models.ViewModels;
using Xunit;

namespace MusicJournal.Tests;

public class ProfileViewModelTests
{
    [Fact]
    public void ProfileViewModel_PropertyGettersAndSetters_WorkAsExpected()
    {
        var tracks = new List<FavoriteTrack>
        {
            new FavoriteTrack { Id = 1, Title = "Heroes", Artist = "David Bowie" },
            new FavoriteTrack { Id = 2, Title = "Life on Mars?", Artist = "David Bowie" }
        };

        var model = new ProfileViewModel
        {
            Id = 42,
            UserId = "user-guid-12345",
            UserName = "starman",
            FavoriteTracks = tracks
        };

        Assert.Equal(42, model.Id);
        Assert.Equal("user-guid-12345", model.UserId);
        Assert.Equal("starman", model.UserName);
        Assert.Same(tracks, model.FavoriteTracks);
        Assert.Equal(2, model.FavoriteTracks.Count);
    }

    [Fact]
    public void ProfileViewModel_DefaultInitialization_SetsDefaultValues()
    {
        var model = new ProfileViewModel();

        Assert.Equal(0, model.Id);
        Assert.Equal(string.Empty, model.UserId);
        Assert.Equal(string.Empty, model.UserName);
        Assert.NotNull(model.FavoriteTracks);
        Assert.Empty(model.FavoriteTracks);
    }
}
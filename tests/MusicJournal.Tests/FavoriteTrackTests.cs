using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MusicJournal.Models;
using Xunit;

namespace MusicJournal.Tests;

public class FavoriteTrackTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void FavoriteTrack_ValidModel_PassesDataAnnotationValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Album = "A Night at the Opera",
            SpotifyId = "4u7EnebtmKWzUH433cf5Qv",
            SpotifyUrl = "https://open.spotify.com/track/4u7EnebtmKWzUH433cf5Qv",
            AlbumArtUrl = "https://i.scdn.co/image/example",
            GlobalTrackId = 1
        };

        var errors = ValidateModel(track);

        Assert.Empty(errors);
        Assert.True(track.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "Queen")]       // Missing Title
    [InlineData("Bohemian Rhapsody", null)] // Missing Artist
    public void FavoriteTrack_MissingRequiredFields_FailsValidation(string? title, string? artist)
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = title!,
            Artist = artist!
        };

        var errors = ValidateModel(track);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void FavoriteTrack_TitleExceedsMaxLength_FailsValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = new string('A', 201), // Exceeds [StringLength(200)]
            Artist = "Queen"
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FavoriteTrack.Title)));
    }

    [Fact]
    public void DuplicateCheckLogic_MatchesControllerCriteria_IdentifiesDuplicate()
    {
        // Replicates the condition in FavoriteTracksController:
        // f.UserId == user.Id && f.Title == favoriteTrack.Title && f.Artist == favoriteTrack.Artist
        var existingTrack = new FavoriteTrack
        {
            UserId = "user-1",
            Title = "Hotel California",
            Artist = "Eagles"
        };

        var incomingTrack = new FavoriteTrack
        {
            UserId = "user-1",
            Title = "Hotel California",
            Artist = "Eagles"
        };

        bool isDuplicate = existingTrack.UserId == incomingTrack.UserId
                        && existingTrack.Title == incomingTrack.Title
                        && existingTrack.Artist == incomingTrack.Artist;

        Assert.True(isDuplicate);
    }

    [Fact]
    public void DuplicateCheckLogic_DifferentUserSameSong_DoesNotFlagDuplicate()
    {
        var existingTrack = new FavoriteTrack
        {
            UserId = "user-1",
            Title = "Hotel California",
            Artist = "Eagles"
        };

        var incomingTrack = new FavoriteTrack
        {
            UserId = "user-2",
            Title = "Hotel California",
            Artist = "Eagles"
        };

        bool isDuplicate = existingTrack.UserId == incomingTrack.UserId
                        && existingTrack.Title == incomingTrack.Title
                        && existingTrack.Artist == incomingTrack.Artist;

        Assert.False(isDuplicate);
    }

    [Fact]
    public void GlobalTrack_Association_AssignsCorrectly()
    {
        var globalTrack = new GlobalTrack
        {
            Id = 10,
            SpotifyId = "spot-999",
            Title = "Dreams",
            Artist = "Fleetwood Mac"
        };

        var favoriteTrack = new FavoriteTrack
        {
            UserId = "user-1",
            Title = "Dreams",
            Artist = "Fleetwood Mac",
            GlobalTrackId = globalTrack.Id,
            GlobalTrack = globalTrack
        };

        Assert.Equal(10, favoriteTrack.GlobalTrackId);
        Assert.Equal("spot-999", favoriteTrack.GlobalTrack.SpotifyId);
    }
}
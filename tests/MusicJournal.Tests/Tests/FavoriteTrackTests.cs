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
        var user = new ApplicationUser { Id = "user-test-123", UserName = "musicfan" };
        var globalTrack = new GlobalTrack { Id = 1, Title = "Bohemian Rhapsody", Artist = "Queen" };

        var track = new FavoriteTrack
        {
            Id = 5,
            UserId = user.Id,
            User = user,
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            Album = "A Night at the Opera",
            SpotifyId = "4u7EnebtmKWzUH433cf5Qv",
            SpotifyUrl = "https://open.spotify.com/track/4u7EnebtmKWzUH433cf5Qv",
            AlbumArtUrl = "https://i.scdn.co/image/example",
            GlobalTrackId = globalTrack.Id,
            GlobalTrack = globalTrack,
            CreatedAt = DateTime.UtcNow
        };

        var errors = ValidateModel(track);

        Assert.Empty(errors);
        Assert.Equal(5, track.Id);
        Assert.Equal("user-test-123", track.UserId);
        Assert.NotNull(track.User);
        Assert.Equal("musicfan", track.User.UserName);
        Assert.Equal("Bohemian Rhapsody", track.Title);
        Assert.Equal("Queen", track.Artist);
        Assert.Equal("A Night at the Opera", track.Album);
        Assert.Equal("4u7EnebtmKWzUH433cf5Qv", track.SpotifyId);
        Assert.Equal("https://open.spotify.com/track/4u7EnebtmKWzUH433cf5Qv", track.SpotifyUrl);
        Assert.Equal("https://i.scdn.co/image/example", track.AlbumArtUrl);
        Assert.Equal(1, track.GlobalTrackId);
        Assert.NotNull(track.GlobalTrack);
        Assert.True(track.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "Queen")]             // Missing Title
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
    public void FavoriteTrack_ArtistExceedsMaxLength_FailsValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Valid Title",
            Artist = new string('B', 201) // Exceeds [StringLength(200)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FavoriteTrack.Artist)));
    }

    [Fact]
    public void FavoriteTrack_AlbumExceedsMaxLength_FailsValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Valid Title",
            Artist = "Valid Artist",
            Album = new string('C', 201) // Exceeds [StringLength(200)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FavoriteTrack.Album)));
    }

    [Fact]
    public void FavoriteTrack_SpotifyUrlExceedsMaxLength_FailsValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Valid Title",
            Artist = "Valid Artist",
            SpotifyUrl = new string('D', 501) // Exceeds [StringLength(500)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FavoriteTrack.SpotifyUrl)));
    }

    [Fact]
    public void FavoriteTrack_SpotifyIdExceedsMaxLength_FailsValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Valid Title",
            Artist = "Valid Artist",
            SpotifyId = new string('E', 51) // Exceeds [StringLength(50)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FavoriteTrack.SpotifyId)));
    }

    [Fact]
    public void FavoriteTrack_AlbumArtUrlExceedsMaxLength_FailsValidation()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Valid Title",
            Artist = "Valid Artist",
            AlbumArtUrl = new string('F', 501) // Exceeds [StringLength(500)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(FavoriteTrack.AlbumArtUrl)));
    }

    [Fact]
    public void FavoriteTrack_AllowsNullablePropertiesToBeNull()
    {
        var track = new FavoriteTrack
        {
            UserId = "user-test-123",
            Title = "Title",
            Artist = "Artist",
            Album = null,
            SpotifyUrl = null,
            SpotifyId = null,
            AlbumArtUrl = null,
            User = null
        };

        var errors = ValidateModel(track);

        Assert.Empty(errors);
        Assert.Null(track.Album);
        Assert.Null(track.SpotifyUrl);
        Assert.Null(track.SpotifyId);
        Assert.Null(track.AlbumArtUrl);
        Assert.Null(track.User);
    }
}
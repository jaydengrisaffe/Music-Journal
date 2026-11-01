using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MusicJournal.Models;
using Xunit;

namespace MusicJournal.Tests;

public class RecTrackTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void RecTrack_ValidModel_PassesValidationAndSetsProperties()
    {
        var now = DateTime.UtcNow;
        var track = new RecTrack
        {
            Title = "Starboy",
            Artist = "The Weeknd",
            Album = "Starboy",
            SpotifyUrl = "https://open.spotify.com/track/7MXVkk9YM5IZvg0v45ENrP",
            SpotifyId = "7MXVkk9YM5IZvg0v45ENrP",
            AlbumArtUrl = "https://i.scdn.co/image/ab67616d0000b2734718e2b124f79258be7bc452",
            CreatedAt = now
        };

        var errors = ValidateModel(track);

        Assert.Empty(errors);
        Assert.Equal("Starboy", track.Title);
        Assert.Equal("The Weeknd", track.Artist);
        Assert.Equal("Starboy", track.Album);
        Assert.Equal("https://open.spotify.com/track/7MXVkk9YM5IZvg0v45ENrP", track.SpotifyUrl);
        Assert.Equal("7MXVkk9YM5IZvg0v45ENrP", track.SpotifyId);
        Assert.Equal("https://i.scdn.co/image/ab67616d0000b2734718e2b124f79258be7bc452", track.AlbumArtUrl);
        Assert.Equal(now, track.CreatedAt);
    }

    [Fact]
    public void RecTrack_DefaultValues_InitializedCorrectly()
    {
        var track = new RecTrack
        {
            Title = "Default Test",
            Artist = "Default Artist"
        };

        Assert.Equal("", track.Album);
        Assert.Equal("", track.SpotifyUrl);
        Assert.Equal("", track.SpotifyId);
        Assert.Equal("", track.AlbumArtUrl);
        Assert.True(track.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "The Weeknd")]      // Missing Title
    [InlineData("Starboy", null)]        // Missing Artist
    public void RecTrack_MissingRequiredFields_FailsValidation(string? title, string? artist)
    {
        var track = new RecTrack
        {
            Title = title!,
            Artist = artist!
        };

        var errors = ValidateModel(track);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void RecTrack_TitleExceedsMaxLength_FailsValidation()
    {
        var track = new RecTrack
        {
            Title = new string('A', 201), // Exceeds [StringLength(200)]
            Artist = "The Weeknd"
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RecTrack.Title)));
    }

    [Fact]
    public void RecTrack_ArtistExceedsMaxLength_FailsValidation()
    {
        var track = new RecTrack
        {
            Title = "Starboy",
            Artist = new string('B', 201) // Exceeds [StringLength(200)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RecTrack.Artist)));
    }

    [Fact]
    public void RecTrack_AlbumExceedsMaxLength_FailsValidation()
    {
        var track = new RecTrack
        {
            Title = "Starboy",
            Artist = "The Weeknd",
            Album = new string('C', 201) // Exceeds [StringLength(200)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RecTrack.Album)));
    }

    [Fact]
    public void RecTrack_SpotifyUrlExceedsMaxLength_FailsValidation()
    {
        var track = new RecTrack
        {
            Title = "Starboy",
            Artist = "The Weeknd",
            SpotifyUrl = new string('D', 501) // Exceeds [StringLength(500)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RecTrack.SpotifyUrl)));
    }

    [Fact]
    public void RecTrack_SpotifyIdExceedsMaxLength_FailsValidation()
    {
        var track = new RecTrack
        {
            Title = "Starboy",
            Artist = "The Weeknd",
            SpotifyId = new string('E', 501) // Exceeds [StringLength(500)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RecTrack.SpotifyId)));
    }

    [Fact]
    public void RecTrack_AlbumArtUrlExceedsMaxLength_FailsValidation()
    {
        var track = new RecTrack
        {
            Title = "Starboy",
            Artist = "The Weeknd",
            AlbumArtUrl = new string('F', 501) // Exceeds [StringLength(500)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RecTrack.AlbumArtUrl)));
    }
}
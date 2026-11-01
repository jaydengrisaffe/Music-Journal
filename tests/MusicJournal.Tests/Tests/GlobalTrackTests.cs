using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MusicJournal.Models;
using Xunit;

namespace MusicJournal.Tests;

public class GlobalTrackTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void GlobalTrack_ValidModel_PassesValidation()
    {
        var track = new GlobalTrack
        {
            Id = 1,
            Title = "Billie Jean",
            Artist = "Michael Jackson",
            Album = "Thriller",
            Rank = 1,
            SpotifyId = "5ChkMS8Otdzsq2gXFaR94e",
            SpotifyUrl = "https://open.spotify.com/track/5ChkMS8Otdzsq2gXFaR94e",
            AlbumArtUrl = "https://i.scdn.co/image/ab67616d0000b2734121faee21f2c50364e57130"
        };

        var errors = ValidateModel(track);

        Assert.Empty(errors);
        Assert.Equal(1, track.Id);
        Assert.Equal("Billie Jean", track.Title);
        Assert.Equal("Michael Jackson", track.Artist);
        Assert.Equal("Thriller", track.Album);
        Assert.Equal(1, track.Rank);
        Assert.Equal("5ChkMS8Otdzsq2gXFaR94e", track.SpotifyId);
        Assert.Equal("https://open.spotify.com/track/5ChkMS8Otdzsq2gXFaR94e", track.SpotifyUrl);
        Assert.Equal("https://i.scdn.co/image/ab67616d0000b2734121faee21f2c50364e57130", track.AlbumArtUrl);
        Assert.True(track.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "Queen")]            // Missing Title
    [InlineData("Bohemian Rhapsody", null)] // Missing Artist
    public void GlobalTrack_MissingRequiredFields_FailsValidation(string? title, string? artist)
    {
        var track = new GlobalTrack
        {
            Title = title!,
            Artist = artist!
        };

        var errors = ValidateModel(track);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void GlobalTrack_TitleExceedsMaxLength_FailsValidation()
    {
        var track = new GlobalTrack
        {
            Title = new string('A', 201), // Exceeds [StringLength(200)]
            Artist = "Valid Artist"
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(GlobalTrack.Title)));
    }

    [Fact]
    public void GlobalTrack_ArtistExceedsMaxLength_FailsValidation()
    {
        var track = new GlobalTrack
        {
            Title = "Valid Title",
            Artist = new string('B', 201) // Exceeds [StringLength(200)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(GlobalTrack.Artist)));
    }

    [Fact]
    public void GlobalTrack_AlbumExceedsMaxLength_FailsValidation()
    {
        var track = new GlobalTrack
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            Album = new string('C', 201) // Exceeds [StringLength(200)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(GlobalTrack.Album)));
    }

    [Fact]
    public void GlobalTrack_SpotifyUrlExceedsMaxLength_FailsValidation()
    {
        var track = new GlobalTrack
        {
            Title = "Valid Title",
            Artist = "Valid Artist",
            SpotifyUrl = new string('D', 501) // Exceeds [StringLength(500)]
        };

        var errors = ValidateModel(track);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(GlobalTrack.SpotifyUrl)));
    }

    [Fact]
    public void GlobalTrack_AllowsNullableFieldsToBeNull()
    {
        var track = new GlobalTrack
        {
            Title = "Minimum Song",
            Artist = "Solo Artist",
            Album = null,
            Rank = null,
            SpotifyUrl = null,
            SpotifyId = null,
            AlbumArtUrl = null
        };

        var errors = ValidateModel(track);

        Assert.Empty(errors);
        Assert.Null(track.Album);
        Assert.Null(track.Rank);
        Assert.Null(track.SpotifyUrl);
        Assert.Null(track.SpotifyId);
        Assert.Null(track.AlbumArtUrl);
    }
}
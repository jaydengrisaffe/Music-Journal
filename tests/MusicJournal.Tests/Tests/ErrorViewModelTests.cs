using MusicJournal.Models;
using Xunit;

namespace MusicJournal.Tests;

public class ErrorViewModelTests
{
    [Fact]
    public void ShowRequestId_ReturnsTrue_WhenRequestIdHasValue()
    {
        var model = new ErrorViewModel { RequestId = "req-12345" };

        Assert.Equal("req-12345", model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShowRequestId_ReturnsFalse_WhenRequestIdIsNullOrEmpty(string? requestId)
    {
        var model = new ErrorViewModel { RequestId = requestId };

        Assert.Equal(requestId, model.RequestId);
        Assert.False(model.ShowRequestId);
    }
}
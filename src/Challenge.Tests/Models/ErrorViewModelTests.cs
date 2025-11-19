using Challenge.Models;

namespace Challenge.Tests.Models;

public class ErrorViewModelTests
{
    [Fact]
    public void ShowRequestId_WithRequestId_ReturnsTrue()
    {
        // Arrange
        var model = new ErrorViewModel
        {
            RequestId = "test-request-id"
        };

        // Act
        var result = model.ShowRequestId;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShowRequestId_WithoutRequestId_ReturnsFalse()
    {
        // Arrange
        var model = new ErrorViewModel
        {
            RequestId = null
        };

        // Act
        var result = model.ShowRequestId;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShowRequestId_WithEmptyRequestId_ReturnsFalse()
    {
        // Arrange
        var model = new ErrorViewModel
        {
            RequestId = ""
        };

        // Act
        var result = model.ShowRequestId;

        // Assert
        Assert.False(result);
    }
}


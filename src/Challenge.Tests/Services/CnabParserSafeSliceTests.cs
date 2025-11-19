using Challenge.Services;
using System.Reflection;

namespace Challenge.Tests.Services;

public class CnabParserSafeSliceTests
{
    [Fact]
    public void SafeSlice_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var method = typeof(CnabParser).GetMethod("SafeSlice", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method!.Invoke(null, new object[] { "", 0, 5 });

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void SafeSlice_StartIndexOutOfRange_ReturnsEmpty()
    {
        // Arrange
        var method = typeof(CnabParser).GetMethod("SafeSlice", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method!.Invoke(null, new object[] { "test", 10, 15 });

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void SafeSlice_EndIndexBeforeStart_ReturnsEmpty()
    {
        // Arrange
        var method = typeof(CnabParser).GetMethod("SafeSlice", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method!.Invoke(null, new object[] { "test", 5, 2 });

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void SafeSlice_ValidRange_ReturnsSubstring()
    {
        // Arrange
        var method = typeof(CnabParser).GetMethod("SafeSlice", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method!.Invoke(null, new object[] { "1234567890", 2, 5 });

        // Assert
        Assert.Equal("3456", result);
    }

    [Fact]
    public void SafeSlice_EndIndexBeyondString_ReturnsToEnd()
    {
        // Arrange
        var method = typeof(CnabParser).GetMethod("SafeSlice", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method!.Invoke(null, new object[] { "test", 1, 10 });

        // Assert
        Assert.Equal("est", result);
    }

    [Fact]
    public void SafeSlice_NegativeStartIndex_ReturnsEmpty()
    {
        // Arrange
        var method = typeof(CnabParser).GetMethod("SafeSlice", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act
        var result = method!.Invoke(null, new object[] { "test", -1, 2 });

        // Assert
        Assert.Equal("", result);
    }
}


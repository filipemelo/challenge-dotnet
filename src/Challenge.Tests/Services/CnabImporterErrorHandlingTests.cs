using Challenge.Data;
using Challenge.Models;
using Challenge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Challenge.Tests.Services;

public class CnabImporterErrorHandlingTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private ILogger<CnabImporter> CreateLogger()
    {
        return new LoggerFactory().CreateLogger<CnabImporter>();
    }

    [Fact]
    public async Task ImportAsync_WithParseErrors_ReturnsErrors()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var invalidLine = "0201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.StoresCount);
    }

    [Fact]
    public async Task ImportAsync_EmptyFile_ReturnsSuccessWithZeroCounts()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(""));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.StoresCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportAsync_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var invalidLines = @"0201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
3201903XX0000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.Errors.Count >= 2);
    }
}


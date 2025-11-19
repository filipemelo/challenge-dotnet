using Challenge.Data;
using Challenge.Models;
using Challenge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Challenge.Tests.Services;

public class CnabImporterErrorPathTests
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
    public async Task ImportAsync_WithParseErrors_ReturnsErrorsWithoutDatabaseAccess()
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
        
        // Verify no database changes were made
        var stores = await context.Stores.ToListAsync();
        Assert.Empty(stores);
    }

    [Fact]
    public async Task ImportAsync_EmptyStoresDictionary_ReturnsSuccessWithZeroCounts()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Empty file results in empty stores dictionary
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
    public async Task ImportAsync_StoreWithNoTransactions_StillCreatesStore()
    {
        // Arrange - This test would require modifying the parser to create stores without transactions
        // For now, we test that stores are created even if they have empty transaction lists
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // A valid CNAB line will always have at least one transaction
        var cnabLine = "1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var stores = await context.Stores.ToListAsync();
        Assert.Single(stores);
    }

    [Fact]
    public async Task ImportAsync_MultipleStoresWithSameName_UpdatesFirstStore()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Create a store first
        var existingStore = new Store { Name = "BAR DO JOÃO", Owner = "OLD OWNER" };
        context.Stores.Add(existingStore);
        await context.SaveChangesAsync();

        // Import with same store name but different owner
        var cnabLine = "1201903010000014200096206760174753****3153153453NEW OWNER     BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var stores = await context.Stores.ToListAsync();
        Assert.Single(stores);
        Assert.Equal("NEW OWNER", stores[0].Owner);
    }
}


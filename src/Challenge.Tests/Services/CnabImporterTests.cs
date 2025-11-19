using Challenge.Data;
using Challenge.Models;
using Challenge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Challenge.Tests.Services;

public class CnabImporterTests
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
    public async Task ImportAsync_ValidCnabFile_ImportsSuccessfully()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.StoresCount);
        Assert.Empty(result.Errors);

        var stores = await context.Stores.Include(s => s.Transactions).ToListAsync();
        Assert.Single(stores);
        Assert.Equal("BAR DO JOÃO", stores[0].Name);
        Assert.Equal("JOÃO MACEDO", stores[0].Owner);
        Assert.Single(stores[0].Transactions);
    }

    [Fact]
    public async Task ImportAsync_ExistingStore_UpdatesOwner()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Create existing store
        var existingStore = new Store { Name = "BAR DO JOÃO", Owner = "OLD OWNER" };
        context.Stores.Add(existingStore);
        await context.SaveChangesAsync();

        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var store = await context.Stores.FirstAsync(s => s.Name == "BAR DO JOÃO");
        Assert.Equal("JOÃO MACEDO", store.Owner);
    }

    [Fact]
    public async Task ImportAsync_InvalidCnabFile_ReturnsErrors()
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
    }

    [Fact]
    public async Task ImportAsync_MultipleTransactions_SameStore_ImportsAll()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLines = @"3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
1201903010000015200096206760171234****7890233000JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.StoresCount);

        var store = await context.Stores.Include(s => s.Transactions).FirstAsync();
        Assert.Equal(2, store.Transactions.Count);
    }
}


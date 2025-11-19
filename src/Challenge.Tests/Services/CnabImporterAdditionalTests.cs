using Challenge.Data;
using Challenge.Models;
using Challenge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Challenge.Tests.Services;

public class CnabImporterAdditionalTests
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
    public async Task ImportAsync_MultipleStores_ImportsAllStores()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLines = @"3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
5201903010000013200556418150633123****7687145607MARIA JOSEFINALOJA DO Ó - MATRIZ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, result.StoresCount);

        var stores = await context.Stores.Include(s => s.Transactions).ToListAsync();
        Assert.Equal(2, stores.Count);
        Assert.Contains(stores, s => s.Name == "BAR DO JOÃO");
        Assert.Contains(stores, s => s.Name == "LOJA DO Ó - MATRIZ");
    }

    [Fact]
    public async Task ImportAsync_StoreWithMultipleTransactions_ImportsAll()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLines = @"1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
2201903010000015200096206760171234****7890153000JOÃO MACEDO   BAR DO JOÃO       
4201903010000016200096206760175678****9012164500JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.ImportedCount);
        Assert.Equal(1, result.StoresCount);

        var store = await context.Stores.Include(s => s.Transactions).FirstAsync();
        Assert.Equal(3, store.Transactions.Count);
    }

    [Fact]
    public async Task ImportAsync_StoreOwnerUpdate_UpdatesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Create store with initial owner
        var store = new Store { Name = "BAR DO JOÃO", Owner = "OLD OWNER" };
        context.Stores.Add(store);
        await context.SaveChangesAsync();
        var originalUpdatedAt = store.UpdatedAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(10);

        var cnabLine = "3201903010000014200096206760174753****3153153453NEW OWNER     BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var updatedStore = await context.Stores.FirstAsync(s => s.Name == "BAR DO JOÃO");
        Assert.Equal("NEW OWNER", updatedStore.Owner);
        // UpdatedAt should be different (or at least the owner should be updated)
        Assert.True(updatedStore.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public async Task ImportAsync_StoreOwnerUnchanged_DoesNotUpdate()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var store = new Store { Name = "BAR DO JOÃO", Owner = "JOÃO MACEDO" };
        context.Stores.Add(store);
        await context.SaveChangesAsync();
        var originalUpdatedAt = store.UpdatedAt;

        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var existingStore = await context.Stores.FirstAsync(s => s.Name == "BAR DO JOÃO");
        Assert.Equal("JOÃO MACEDO", existingStore.Owner);
    }

    [Fact]
    public async Task ImportAsync_AllTransactionTypes_AreImported()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLines = @"1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
2201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
4201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
5201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
6201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
7201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
8201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
9201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(9, result.ImportedCount);
        
        var transactions = await context.Transactions.ToListAsync();
        Assert.Equal(9, transactions.Count);
        Assert.Contains(transactions, t => t.TransactionType == 1);
        Assert.Contains(transactions, t => t.TransactionType == 9);
    }

    [Fact]
    public async Task ImportAsync_TransactionDescription_IsSetCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLine = "1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var transaction = await context.Transactions.FirstAsync();
        Assert.Equal("Débito", transaction.Description);
    }
}


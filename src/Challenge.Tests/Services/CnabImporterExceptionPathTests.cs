using Challenge.Data;
using Challenge.Models;
using Challenge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Challenge.Tests.Services;

public class CnabImporterExceptionPathTests
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
    public async Task ImportAsync_TransactionDescription_IsSetFromTransactionTypes()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Test all transaction types
        var transactionTypes = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var cnabLines = string.Join("\n", transactionTypes.Select(type => 
            $"{type}201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       "));
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var transactions = await context.Transactions.ToListAsync();
        Assert.Equal(9, transactions.Count);
        foreach (var type in transactionTypes)
        {
            var transaction = transactions.First(t => t.TransactionType == type);
            Assert.Equal(Transaction.TransactionTypes[type].Description, transaction.Description);
        }
    }

    [Fact]
    public async Task ImportAsync_ImportResultProperties_AreSetCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        var cnabLines = @"1201903010000014200096206760174753****3153153453OWNER ONE     STORE ONE        
2201903010000014200096206760171234****7890153453OWNER TWO     STORE TWO        
3201903010000014200096206760175678****9012153453OWNER ONE     STORE ONE        ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.ImportedCount);
        Assert.Equal(2, result.StoresCount); // Two unique stores
        Assert.Empty(result.Errors);
        Assert.True(result.Success);
    }
}


using Challenge.Data;
using Challenge.Models;
using Challenge.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Challenge.Tests.Services;

public class CnabImporterDatabaseErrorTests
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
    public async Task ImportAsync_StoreNameWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Store name with special characters
        var cnabLine = "1201903010000014200096206760174753****3153153453OWNER NAME    STORE & CO.        ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var stores = await context.Stores.ToListAsync();
        Assert.Single(stores);
        Assert.Contains("STORE & CO.", stores[0].Name);
    }

    [Fact]
    public async Task ImportAsync_OwnerNameWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Owner name with special characters
        var cnabLine = "1201903010000014200096206760174753****3153153453JOSÉ DA SILVA BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var stores = await context.Stores.ToListAsync();
        Assert.Single(stores);
        Assert.Contains("JOSÉ DA SILVA", stores[0].Owner);
    }


    [Fact]
    public async Task ImportAsync_LargeAmount_HandlesCorrectly()
    {
        // Arrange
        var context = CreateContext();
        var logger = CreateLogger();
        var importer = new CnabImporter(context, logger);
        
        // Large amount: 9999999999 = 99999999.99 (10 digits, positions 9-18)
        // Format: Type(1) + Date(8) + Amount(10) + CPF(11) + Card(12) + Time(6) + Owner(15) + Store(18) = 81
        // Use valid time format (HHMMSS): 153453 = 15:34:53
        // Positions: 0=type, 1-8=date, 9-18=amount, 19-29=cpf, 30-41=card, 42-47=time, 48-62=owner, 63-80=store
        var cnabLine = "12019030199999999990096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        // Ensure exactly 81 characters with valid time (positions 42-47)
        if (cnabLine.Length < 81)
        {
            cnabLine = cnabLine.PadRight(81);
        }
        // Replace time field (positions 42-47) with valid time: 153453 (15:34:53)
        cnabLine = cnabLine.Substring(0, 42) + "153453" + cnabLine.Substring(48);
        Assert.Equal(81, cnabLine.Length);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = await importer.ImportAsync(stream);

        // Assert
        Assert.True(result.Success);
        var transaction = await context.Transactions.FirstAsync();
        // Amount is divided by 100, so 9999999999 / 100 = 99999999.99
        Assert.Equal(99999999.99m, transaction.Amount);
    }

}


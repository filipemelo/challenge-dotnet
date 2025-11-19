using Challenge.Data;
using Challenge.Models;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Tests.Data;

public class ApplicationDbContextTests
{
    [Fact]
    public void ApplicationDbContext_CanBeCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        using var context = new ApplicationDbContext(options);
        Assert.NotNull(context);
    }

    [Fact]
    public async Task ApplicationDbContext_CanAddStore()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Act
        var store = new Store { Name = "Test Store", Owner = "Test Owner" };
        context.Stores.Add(store);
        await context.SaveChangesAsync();

        // Assert
        var savedStore = await context.Stores.FirstAsync();
        Assert.Equal("Test Store", savedStore.Name);
    }

    [Fact]
    public async Task ApplicationDbContext_CanAddTransaction()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var store = new Store { Name = "Test Store", Owner = "Test Owner" };
        context.Stores.Add(store);
        await context.SaveChangesAsync();

        // Act
        var transaction = new Transaction
        {
            StoreId = store.Id,
            TransactionType = 1,
            Date = DateOnly.FromDateTime(DateTime.Now),
            Amount = 100.00m,
            Cpf = "12345678901",
            Card = "1234****5678",
            Time = TimeOnly.FromDateTime(DateTime.Now),
            Nature = "Entrada",
            Description = "Débito"
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Assert
        var savedTransaction = await context.Transactions.FirstAsync();
        Assert.Equal(1, savedTransaction.TransactionType);
        Assert.Equal(store.Id, savedTransaction.StoreId);
    }

    [Fact]
    public async Task ApplicationDbContext_StoreHasTransactions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var store = new Store { Name = "Test Store", Owner = "Test Owner" };
        context.Stores.Add(store);
        await context.SaveChangesAsync();

        var transaction = new Transaction
        {
            StoreId = store.Id,
            TransactionType = 1,
            Date = DateOnly.FromDateTime(DateTime.Now),
            Amount = 100.00m,
            Cpf = "12345678901",
            Card = "1234****5678",
            Time = TimeOnly.FromDateTime(DateTime.Now),
            Nature = "Entrada",
            Description = "Débito"
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        // Act
        var storeWithTransactions = await context.Stores
            .Include(s => s.Transactions)
            .FirstAsync();

        // Assert
        Assert.Single(storeWithTransactions.Transactions);
        Assert.Equal(transaction.Id, storeWithTransactions.Transactions.First().Id);
    }
}


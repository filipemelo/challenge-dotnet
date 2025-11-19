using Challenge.Controllers;
using Challenge.Data;
using Challenge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Tests.Controllers;

public class StoresControllerTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Index_NoStores_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext();
        var controller = new StoresController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Store>>(viewResult.Model);
        Assert.Empty(model);
    }

    [Fact]
    public async Task Index_WithStores_ReturnsStoresOrderedByName()
    {
        // Arrange
        var context = CreateContext();
        var store1 = new Store { Name = "Z Store", Owner = "Owner 1" };
        var store2 = new Store { Name = "A Store", Owner = "Owner 2" };
        context.Stores.Add(store1);
        context.Stores.Add(store2);
        await context.SaveChangesAsync();
        var controller = new StoresController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Store>>(viewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.Equal("A Store", model[0].Name);
        Assert.Equal("Z Store", model[1].Name);
    }

    [Fact]
    public async Task Index_WithStoresAndTransactions_IncludesTransactions()
    {
        // Arrange
        var context = CreateContext();
        var store = new Store { Name = "Test Store", Owner = "Owner" };
        store.Transactions.Add(new Transaction
        {
            TransactionType = 1,
            Date = DateOnly.FromDateTime(DateTime.Now),
            Amount = 100.00m,
            Cpf = "12345678901",
            Card = "1234****5678",
            Time = TimeOnly.FromDateTime(DateTime.Now),
            Nature = "Entrada",
            Description = "Débito"
        });
        context.Stores.Add(store);
        await context.SaveChangesAsync();
        var controller = new StoresController(context);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Store>>(viewResult.Model);
        Assert.Single(model);
        Assert.Single(model[0].Transactions);
    }
}


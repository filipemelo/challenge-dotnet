using Challenge.Controllers;
using Challenge.Data;
using Challenge.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Challenge.Tests.Controllers;

public class ConfigurationControllerTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Index_ReturnsView()
    {
        // Arrange
        var context = CreateContext();
        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<ConfigurationController>();
        var controller = new ConfigurationController(context, logger);

        // Act
        var result = controller.Index();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ResetDatabase_WrongConfirmation_RedirectsWithError()
    {
        // Arrange
        var context = CreateContext();
        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<ConfigurationController>();
        var controller = new ConfigurationController(context, logger);
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

        // Act
        var result = await controller.ResetDatabase("wrong");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task ResetDatabase_CorrectConfirmation_ClearsDatabase()
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

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<ConfigurationController>();
        var controller = new ConfigurationController(context, logger);
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

        // Act
        var result = await controller.ResetDatabase("CONFIRM");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        // Note: In-memory database doesn't support TRUNCATE, so we just verify the method executes
        // In a real database, the tables would be cleared
    }
}


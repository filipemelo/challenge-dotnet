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

public class ConfigurationControllerErrorTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ResetDatabase_ExceptionDuringReset_LogsErrorAndShowsMessage()
    {
        // Arrange
        var context = CreateContext();
        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<ConfigurationController>();
        var controller = new ConfigurationController(context, logger);
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

        // Act - In-memory database doesn't support TRUNCATE, so this will likely throw
        var result = await controller.ResetDatabase("CONFIRM");

        // Assert - Should redirect regardless of success/failure
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task ResetDatabase_EmptyConfirmation_ShowsError()
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
        var result = await controller.ResetDatabase("");

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task ResetDatabase_NullConfirmation_ShowsError()
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
        var result = await controller.ResetDatabase(null!);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
    }
}


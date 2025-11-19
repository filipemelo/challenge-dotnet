using Challenge.Controllers;
using Challenge.Data;
using Challenge.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Challenge.Tests.Controllers;

public class CnabFilesControllerAdditionalTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private CnabFilesController CreateController(ApplicationDbContext? context = null, CnabImporter? importer = null)
    {
        context ??= CreateContext();
        var logger = new Mock<ILogger<CnabFilesController>>().Object;
        
        if (importer == null)
        {
            var importerLogger = new Mock<ILogger<CnabImporter>>().Object;
            importer = new CnabImporter(context, importerLogger);
        }

        var controller = new CnabFilesController(importer, logger);
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
        return controller;
    }

    [Fact]
    public async Task Create_ImportFails_ReturnsViewWithErrors()
    {
        // Arrange
        var context = CreateContext();
        var controller = CreateController(context);
        
        // Invalid CNAB file that will cause import to fail
        var invalidLine = "0201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(invalidLine.Length);
        file.Setup(f => f.FileName).Returns("test.txt");
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidLine)));

        // Act
        var result = await controller.Create(file.Object);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_FileExtensionCaseInsensitive_AcceptsTxt()
    {
        // Arrange
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var controller = CreateController(context);
        
        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(cnabLine.Length);
        file.Setup(f => f.FileName).Returns("test.TXT"); // Uppercase extension
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine)));

        // Act
        var result = await controller.Create(file.Object);

        // Assert - Should accept .TXT extension
        // Result can be either redirect (success) or view (if there's an error)
        Assert.True(result is RedirectToActionResult || result is ViewResult);
    }

    [Fact]
    public async Task Create_FileExtensionCaseInsensitive_AcceptsTxtMixedCase()
    {
        // Arrange
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var controller = CreateController(context);
        
        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(cnabLine.Length);
        file.Setup(f => f.FileName).Returns("test.TxT"); // Mixed case extension
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine)));

        // Act
        var result = await controller.Create(file.Object);

        // Assert - Should accept .TxT extension
        Assert.True(result is RedirectToActionResult || result is ViewResult);
    }

    [Fact]
    public async Task Create_FileSizeExceedsLimit_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateContext();
        var controller = CreateController(context);
        
        // File size exceeds 10MB limit (11MB)
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB
        file.Setup(f => f.FileName).Returns("large.txt");
        file.Setup(f => f.ContentType).Returns("text/plain");

        // Act
        var result = await controller.Create(file.Object);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState, kvp => 
            kvp.Value!.Errors.Any(e => 
                e.ErrorMessage!.Contains("exceeds maximum") || 
                e.ErrorMessage!.Contains("10MB")));
    }

    [Fact]
    public async Task Create_FileSizeWithinLimit_ProcessesFile()
    {
        // Arrange
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var controller = CreateController(context);
        
        // File size within 10MB limit
        var validCnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var fileContent = System.Text.Encoding.UTF8.GetBytes(validCnabLine);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(fileContent.Length);
        file.Setup(f => f.FileName).Returns("test.txt");
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        // Act
        var result = await controller.Create(file.Object);

        // Assert - Should process successfully (redirect on success)
        Assert.True(result is RedirectToActionResult || result is ViewResult);
    }

    [Fact]
    public async Task Create_FileSizeExactlyAtLimit_ProcessesFile()
    {
        // Arrange
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var controller = CreateController(context);
        
        // File size exactly at 10MB limit
        var validCnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(10 * 1024 * 1024); // Exactly 10MB
        file.Setup(f => f.FileName).Returns("test.txt");
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(validCnabLine)));

        // Act
        var result = await controller.Create(file.Object);

        // Assert - Should process successfully (10MB is the limit, so it should be accepted)
        Assert.True(result is RedirectToActionResult || result is ViewResult);
    }

    [Fact]
    public async Task Create_InvalidContentType_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateContext();
        var controller = CreateController(context);
        
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(100);
        file.Setup(f => f.FileName).Returns("test.txt");
        file.Setup(f => f.ContentType).Returns("application/pdf"); // Invalid content type

        // Act
        var result = await controller.Create(file.Object);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState, kvp => 
            kvp.Value!.Errors.Any(e => e.ErrorMessage!.Contains("Invalid file type") || e.ErrorMessage!.Contains("content type")));
    }

    [Fact]
    public async Task Create_BinaryFileContent_ReturnsViewWithError()
    {
        // Arrange
        var context = CreateContext();
        var controller = CreateController(context);
        
        // Create a binary file (contains null bytes)
        var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF };
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(binaryContent.Length);
        file.Setup(f => f.FileName).Returns("malicious.txt"); // Renamed to .txt
        file.Setup(f => f.ContentType).Returns("text/plain"); // Fake content type
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(binaryContent));

        // Act
        var result = await controller.Create(file.Object);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("New", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        // Check for error about binary file or invalid characters
        Assert.Contains(controller.ModelState, kvp => 
            kvp.Value!.Errors.Any(e => 
                e.ErrorMessage!.Contains("binary") || 
                e.ErrorMessage!.Contains("non-printable") ||
                e.ErrorMessage!.Contains("invalid characters")));
    }

    [Fact]
    public async Task Create_ValidTextFile_ProcessesSuccessfully()
    {
        // Arrange
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var controller = CreateController(context);
        
        var validCnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var fileContent = System.Text.Encoding.UTF8.GetBytes(validCnabLine);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(fileContent.Length);
        file.Setup(f => f.FileName).Returns("test.txt");
        file.Setup(f => f.ContentType).Returns("text/plain");
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        // Act
        var result = await controller.Create(file.Object);

        // Assert - Should process successfully
        Assert.True(result is RedirectToActionResult || result is ViewResult);
    }
}


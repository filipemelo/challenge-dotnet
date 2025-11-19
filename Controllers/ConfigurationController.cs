using Challenge.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Controllers;

public class ConfigurationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(ApplicationDbContext context, ILogger<ConfigurationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDatabase(string confirmationText)
    {
        if (confirmationText != "CONFIRM")
        {
            TempData["ErrorMessage"] = "Incorrect confirmation text.";
            return RedirectToAction("Index");
        }

        try
        {
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Transactions\" RESTART IDENTITY CASCADE;");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Stores\" RESTART IDENTITY CASCADE;");
            
            TempData["SuccessMessage"] = "Database successfully cleared. All stores and transactions have been removed and indexes restarted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database");
            TempData["ErrorMessage"] = $"Error clearing database: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}


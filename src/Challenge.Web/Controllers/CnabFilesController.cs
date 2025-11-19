using Challenge.Services;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.Controllers;

public class CnabFilesController : Controller
{
    private readonly CnabImporter _importer;
    private readonly ILogger<CnabFilesController> _logger;

    public CnabFilesController(CnabImporter importer, ILogger<CnabFilesController> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    public IActionResult New()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file to upload.");
            return View("New");
        }

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("file", "Only .txt files are allowed.");
            return View("New");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importer.ImportAsync(stream);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"CNAB file processed successfully! {result.ImportedCount} transactions imported from {result.StoresCount} store(s).";
                return RedirectToAction("Index", "Stores");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View("New");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CNAB file");
            ModelState.AddModelError("", $"An error occurred while processing the file: {ex.Message}");
            return View("New");
        }
    }
}


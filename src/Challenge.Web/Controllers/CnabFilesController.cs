using Challenge.Services;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.Controllers;

/// <summary>
/// Controller for handling CNAB file uploads and processing.
/// </summary>
public class CnabFilesController : Controller
{
    private readonly CnabImporter _importer;
    private readonly ILogger<CnabFilesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CnabFilesController"/> class.
    /// </summary>
    /// <param name="importer">The CNAB importer service for processing uploaded files.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    public CnabFilesController(CnabImporter importer, ILogger<CnabFilesController> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    /// <summary>
    /// Displays the file upload form.
    /// </summary>
    /// <returns>The file upload view.</returns>
    public IActionResult New()
    {
        return View();
    }

    /// <summary>
    /// Processes an uploaded CNAB file.
    /// Validates the file (size, extension, content type, and content) before importing.
    /// </summary>
    /// <param name="file">The uploaded CNAB file to process.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Redirects to the stores index on success, or returns the upload view with errors on failure.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Please select a file to upload.");
            return View("New");
        }

        // Validate file size to prevent DoS attacks
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            ModelState.AddModelError("file", $"File size exceeds maximum allowed size ({maxFileSize / (1024 * 1024)}MB).");
            return View("New");
        }

        // Validate file extension
        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("file", "Only .txt files are allowed.");
            return View("New");
        }

        // Validate content type to prevent malicious file uploads
        // Accept text/plain and application/octet-stream (some browsers send this for .txt files)
        var allowedContentTypes = new[] { "text/plain", "application/octet-stream", "text/plain; charset=utf-8" };
        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("file", "Invalid file type. Only plain text files are allowed.");
            return View("New");
        }

        // Validate file content is actually text (check first few bytes for binary content)
        try
        {
            using var contentStream = file.OpenReadStream();
            var buffer = new byte[Math.Min(512, file.Length)];
            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            
            // Check for null bytes or excessive non-printable characters (signs of binary files)
            int nullBytes = 0;
            int nonPrintableChars = 0;
            
            for (int i = 0; i < bytesRead; i++)
            {
                var b = buffer[i];
                
                // Count null bytes (definite sign of binary file)
                if (b == 0)
                {
                    nullBytes++;
                }
                
                // Count non-printable characters (except common whitespace: tab, newline, carriage return)
                if (b < 9 || (b > 13 && b < 32) || b == 127)
                {
                    // Allow UTF-8 multi-byte sequences
                    if (!(b >= 0xC0 && b <= 0xF4) && !(b >= 0x80 && b <= 0xBF))
                    {
                        nonPrintableChars++;
                    }
                }
            }
            
            // Reject if file contains null bytes (binary file indicator)
            if (nullBytes > 0)
            {
                ModelState.AddModelError("file", "File appears to be binary. Only plain text files are allowed.");
                return View("New");
            }
            
            // Reject if more than 30% of characters are non-printable (likely binary)
            if (bytesRead > 0 && (nonPrintableChars * 100.0 / bytesRead) > 30)
            {
                ModelState.AddModelError("file", "File contains too many non-printable characters. Only plain text files are allowed.");
                return View("New");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating file content");
            // Don't fail on validation errors, but log them
            // Continue with upload as content type check should be sufficient
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importer.ImportAsync(stream, cancellationToken);

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
            _logger.LogError(ex, "Error processing CNAB file: {ErrorMessage}", ex.Message);
            ModelState.AddModelError("", "An error occurred while processing the file. Please try again or contact support if the problem persists.");
            return View("New");
        }
    }
}


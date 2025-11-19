using Challenge.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Controllers;

/// <summary>
/// Controller for application configuration and administrative operations.
/// </summary>
public class ConfigurationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigurationController> _logger;
    
    // Constant for database reset confirmation text to avoid magic strings
    private const string ResetConfirmationText = "CONFIRM";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="context">The database context for performing database operations.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    public ConfigurationController(ApplicationDbContext context, ILogger<ConfigurationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Displays the configuration page.
    /// </summary>
    /// <returns>The configuration index view.</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Resets the database by removing all stores and transactions, and resetting identity sequences.
    /// Requires confirmation text "CONFIRM" to prevent accidental data loss.
    /// </summary>
    /// <param name="confirmationText">The confirmation text that must match "CONFIRM" to proceed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Redirects to the configuration index with a success or error message.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDatabase(string confirmationText, CancellationToken cancellationToken = default)
    {
        if (confirmationText != ResetConfirmationText)
        {
            TempData["ErrorMessage"] = "Incorrect confirmation text.";
            return RedirectToAction("Index");
        }

        try
        {
            // Use EF Core methods instead of raw SQL to prevent SQL injection
            // Remove all transactions first (due to foreign key constraint)
            var transactions = await _context.Transactions.ToListAsync(cancellationToken);
            _context.Transactions.RemoveRange(transactions);
            
            // Remove all stores
            var stores = await _context.Stores.ToListAsync(cancellationToken);
            _context.Stores.RemoveRange(stores);
            
            // Save changes in a single transaction
            await _context.SaveChangesAsync(cancellationToken);
            
            // Reset identity sequences by querying the database for actual sequence names
            // This prevents SQL injection and works regardless of table naming conventions
            // Table and column names are constants, not user input, so safe to use in queries
            const string storesTable = "Stores";
            const string transactionsTable = "Transactions";
            const string idColumn = "Id";
            
            // Query PostgreSQL to get the actual sequence names (safe - uses parameterized queries)
            // pg_get_serial_sequence returns the sequence name for an identity column
            var storeSequenceName = await _context.Database
                .SqlQueryRaw<string>($"SELECT pg_get_serial_sequence({{0}}, {{1}})", storesTable, idColumn)
                .FirstOrDefaultAsync(cancellationToken);
            
            var transactionSequenceName = await _context.Database
                .SqlQueryRaw<string>($"SELECT pg_get_serial_sequence({{0}}, {{1}})", transactionsTable, idColumn)
                .FirstOrDefaultAsync(cancellationToken);
            
            // Reset sequences if they exist
            // Sequence names come from database schema query, validated before use
            if (!string.IsNullOrEmpty(storeSequenceName) && IsValidSequenceName(storeSequenceName))
            {
                // Sequence name is validated and comes from DB schema, not user input - safe
                // Using ExecuteSqlRawAsync is acceptable here because:
                // 1. Sequence name comes from pg_get_serial_sequence (database schema)
                // 2. We validate the format before use
                // 3. No user input is involved
#pragma warning disable EF1002 // SQL injection warning - safe because sequence name is from DB schema and validated
                await _context.Database.ExecuteSqlRawAsync(
                    $"ALTER SEQUENCE IF EXISTS {storeSequenceName} RESTART WITH 1", cancellationToken);
#pragma warning restore EF1002
            }
            
            if (!string.IsNullOrEmpty(transactionSequenceName) && IsValidSequenceName(transactionSequenceName))
            {
#pragma warning disable EF1002 // SQL injection warning - safe because sequence name is from DB schema and validated
                await _context.Database.ExecuteSqlRawAsync(
                    $"ALTER SEQUENCE IF EXISTS {transactionSequenceName} RESTART WITH 1", cancellationToken);
#pragma warning restore EF1002
            }
            
            TempData["SuccessMessage"] = "Database successfully cleared. All stores and transactions have been removed and indexes restarted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "An error occurred while clearing the database. Please try again or contact support if the problem persists.";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Validates that a sequence name follows PostgreSQL naming conventions.
    /// This prevents potential SQL injection by ensuring only valid sequence names are used.
    /// </summary>
    /// <param name="sequenceName">The sequence name to validate.</param>
    /// <returns><c>true</c> if the sequence name is valid; otherwise, <c>false</c>.</returns>
    private static bool IsValidSequenceName(string sequenceName)
    {
        if (string.IsNullOrWhiteSpace(sequenceName))
            return false;

        // PostgreSQL sequence names must follow identifier rules:
        // - Can contain letters, digits, underscores
        // - Can be quoted (schema.sequence format)
        // - Should match pattern: [schema.]sequence_name
        // We validate it contains only safe characters
        var validPattern = new System.Text.RegularExpressions.Regex(@"^[\w\.""]+$");
        return validPattern.IsMatch(sequenceName) && 
               sequenceName.Length <= 63 && // PostgreSQL identifier max length
               !sequenceName.Contains("--") && // No SQL comments
               !sequenceName.Contains(";"); // No statement terminators
    }
}


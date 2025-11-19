using Challenge.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Controllers;

/// <summary>
/// Controller for displaying stores and their associated transactions.
/// </summary>
public class StoresController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoresController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoresController"/> class.
    /// </summary>
    /// <param name="context">The database context for retrieving store data.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    public StoresController(ApplicationDbContext context, ILogger<StoresController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Displays a list of all stores with their transactions, ordered by store name.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The stores index view with a list of stores and their transactions.</returns>
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        try
        {
            var stores = await _context.Stores
                .Include(s => s.Transactions)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            return View(stores);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            _logger.LogError(ex, "Error retrieving stores from database");
            // If database doesn't exist yet or there's an error, return empty list
            return View(new List<Models.Store>());
        }
    }
}


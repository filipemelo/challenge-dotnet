using Challenge.Data;
using Challenge.Models;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Services;

/// <summary>
/// Service for importing CNAB file data into the database.
/// Handles parsing, validation, and persistence of transaction data.
/// </summary>
public class CnabImporter
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CnabImporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CnabImporter"/> class.
    /// </summary>
    /// <param name="context">The database context for persisting data.</param>
    /// <param name="logger">The logger for recording import operations and errors.</param>
    public CnabImporter(ApplicationDbContext context, ILogger<CnabImporter> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Imports CNAB file data from a stream into the database.
    /// Parses the file, creates or updates stores, and adds transactions.
    /// All operations are performed within a database transaction for atomicity.
    /// </summary>
    /// <param name="fileStream">The stream containing the CNAB file data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="ImportResult"/> indicating success or failure, with details about imported records and any errors.</returns>
    public async Task<ImportResult> ImportAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var parsedData = CnabParser.Parse(fileStream);

        if (parsedData.Errors.Any())
        {
            return new ImportResult
            {
                Success = false,
                Errors = parsedData.Errors
            };
        }

        var importedCount = 0;

        using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Load all existing stores in a single query to avoid N+1 problem
            var storeNames = parsedData.Stores.Values.Select(s => s.Name).ToList();
            var existingStores = await _context.Stores
                .Where(s => storeNames.Contains(s.Name))
                .ToDictionaryAsync(s => s.Name, cancellationToken);

            foreach (var (storeKey, storeData) in parsedData.Stores)
            {
                // Check if store exists in the dictionary (loaded in single query)
                existingStores.TryGetValue(storeData.Name, out var store);

                if (store == null)
                {
                    store = new Store
                    {
                        Name = storeData.Name,
                        Owner = storeData.Owner
                    };
                    _context.Stores.Add(store);
                    // Don't save yet - batch all changes
                    // EF Core will assign the Id when SaveChangesAsync is called
                }
                else if (store.Owner != storeData.Owner)
                {
                    store.Owner = storeData.Owner;
                    store.UpdatedAt = DateTime.UtcNow;
                    // Don't save yet - batch all changes
                }

                // Add all transactions for this store
                // Use navigation property so EF Core handles the relationship correctly
                // This works even for new stores (Id = 0) because EF Core will set it during SaveChangesAsync
                foreach (var transactionData in storeData.Transactions)
                {
                    var transaction = new Transaction
                    {
                        Store = store, // Use navigation property instead of StoreId
                        TransactionType = transactionData.TransactionType,
                        Date = transactionData.Date,
                        Amount = transactionData.Amount,
                        Cpf = transactionData.Cpf,
                        Card = transactionData.Card,
                        Time = transactionData.Time,
                        Nature = transactionData.Nature,
                        Description = Transaction.TransactionTypes[transactionData.TransactionType].Description
                    };

                    _context.Transactions.Add(transaction);
                    importedCount++;
                }
            }

            // Single SaveChangesAsync call for all changes (batched)
            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return new ImportResult
            {
                Success = true,
                ImportedCount = importedCount,
                StoresCount = parsedData.Stores.Count
            };
        }
        catch (DbUpdateException ex)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Database error during import: {ErrorMessage}", ex.Message);
            return new ImportResult
            {
                Success = false,
                Errors = new List<string> { "An error occurred while saving the data to the database. The file may contain invalid data or there may be a database issue." }
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Unexpected error during import: {ErrorMessage}", ex.Message);
            return new ImportResult
            {
                Success = false,
                Errors = new List<string> { "An unexpected error occurred while processing the file. Please try again or contact support if the problem persists." }
            };
        }
    }
}

/// <summary>
/// Contains the result of a CNAB file import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the import operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions successfully imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of unique stores processed during the import.
    /// </summary>
    public int StoresCount { get; set; }

    /// <summary>
    /// Gets or sets the list of error messages encountered during the import operation.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}


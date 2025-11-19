using Challenge.Data;
using Challenge.Models;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Services;

public class CnabImporter
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CnabImporter> _logger;

    public CnabImporter(ApplicationDbContext context, ILogger<CnabImporter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(Stream fileStream)
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

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var (storeKey, storeData) in parsedData.Stores)
            {
                var store = await _context.Stores
                    .FirstOrDefaultAsync(s => s.Name == storeData.Name);

                if (store == null)
                {
                    store = new Store
                    {
                        Name = storeData.Name,
                        Owner = storeData.Owner
                    };
                    _context.Stores.Add(store);
                    await _context.SaveChangesAsync();
                }
                else if (store.Owner != storeData.Owner)
                {
                    store.Owner = storeData.Owner;
                    store.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                foreach (var transactionData in storeData.Transactions)
                {
                    var transaction = new Transaction
                    {
                        StoreId = store.Id,
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

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new ImportResult
            {
                Success = true,
                ImportedCount = importedCount,
                StoresCount = parsedData.Stores.Count
            };
        }
        catch (DbUpdateException ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Database error during import");
            return new ImportResult
            {
                Success = false,
                Errors = new List<string> { $"Error saving to database: {ex.Message}" }
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Unexpected error during import");
            return new ImportResult
            {
                Success = false,
                Errors = new List<string> { $"Unexpected error: {ex.Message}" }
            };
        }
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int StoresCount { get; set; }
    public List<string> Errors { get; set; } = new();
}


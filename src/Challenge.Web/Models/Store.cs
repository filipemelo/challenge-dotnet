using System.ComponentModel.DataAnnotations;

namespace Challenge.Models;

/// <summary>
/// Represents a store entity that contains transactions.
/// </summary>
public class Store
{
    /// <summary>
    /// Gets or sets the unique identifier for the store.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the store. Required, maximum length 255 characters.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner name of the store. Required, maximum length 255 characters.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the store was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the store was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the collection of transactions associated with this store.
    /// </summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>
    /// Gets the calculated balance of the store based on all transactions.
    /// Positive amounts for "Entrada" transactions, negative for "Saída" transactions.
    /// </summary>
    public decimal Balance
    {
        get
        {
            return Transactions.Sum(t => t.IsEntrada ? t.Amount : -t.Amount);
        }
    }
}


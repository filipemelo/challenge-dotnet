using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge.Models;

/// <summary>
/// Represents a financial transaction from a CNAB file.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the transaction type code (1-9). Required.
    /// </summary>
    [Required]
    public int TransactionType { get; set; }

    /// <summary>
    /// Gets or sets the date of the transaction. Required.
    /// </summary>
    [Required]
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount. Required, stored as decimal(10,2).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the CPF (Brazilian tax ID) associated with the transaction. Required, maximum length 11 characters.
    /// </summary>
    [Required]
    [MaxLength(11)]
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the card number (masked format). Required, maximum length 12 characters.
    /// </summary>
    [Required]
    [MaxLength(12)]
    public string Card { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time of the transaction. Required.
    /// </summary>
    [Required]
    public TimeOnly Time { get; set; }

    /// <summary>
    /// Gets or sets the nature of the transaction ("Entrada" or "Saída"). Required, maximum length 50 characters.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Nature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the transaction type. Required, maximum length 255 characters.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the foreign key to the associated store. Required.
    /// </summary>
    [Required]
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated store.
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the transaction was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the dictionary of valid transaction types with their descriptions, nature, and signal.
    /// </summary>
    public static readonly Dictionary<int, TransactionTypeInfo> TransactionTypes = new()
    {
        { 1, new TransactionTypeInfo { Description = "Débito", Nature = "Entrada", Signal = "+" } },
        { 2, new TransactionTypeInfo { Description = "Boleto", Nature = "Saída", Signal = "-" } },
        { 3, new TransactionTypeInfo { Description = "Financiamento", Nature = "Saída", Signal = "-" } },
        { 4, new TransactionTypeInfo { Description = "Crédito", Nature = "Entrada", Signal = "+" } },
        { 5, new TransactionTypeInfo { Description = "Recebimento Empréstimo", Nature = "Entrada", Signal = "+" } },
        { 6, new TransactionTypeInfo { Description = "Vendas", Nature = "Entrada", Signal = "+" } },
        { 7, new TransactionTypeInfo { Description = "Recebimento TED", Nature = "Entrada", Signal = "+" } },
        { 8, new TransactionTypeInfo { Description = "Recebimento DOC", Nature = "Entrada", Signal = "+" } },
        { 9, new TransactionTypeInfo { Description = "Aluguel", Nature = "Saída", Signal = "-" } }
    };

    /// <summary>
    /// Gets a value indicating whether this transaction is an "Entrada" (income) transaction.
    /// </summary>
    public bool IsEntrada => TransactionTypes.ContainsKey(TransactionType) && TransactionTypes[TransactionType].Nature == "Entrada";

    /// <summary>
    /// Gets a value indicating whether this transaction is a "Saída" (expense) transaction.
    /// </summary>
    public bool IsSaida => !IsEntrada;

    /// <summary>
    /// Gets the description of the transaction type, or "Unknown" if the type is invalid.
    /// </summary>
    public string TypeDescription => TransactionTypes.ContainsKey(TransactionType) ? TransactionTypes[TransactionType].Description : "Unknown";

    /// <summary>
    /// Gets the signal ("+" or "-") for the transaction type, or empty string if the type is invalid.
    /// </summary>
    public string Signal => TransactionTypes.ContainsKey(TransactionType) ? TransactionTypes[TransactionType].Signal : "";
}

/// <summary>
/// Contains information about a transaction type, including description, nature, and signal.
/// </summary>
public class TransactionTypeInfo
{
    /// <summary>
    /// Gets or sets the description of the transaction type.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nature of the transaction ("Entrada" or "Saída").
    /// </summary>
    public string Nature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signal ("+" for income, "-" for expense).
    /// </summary>
    public string Signal { get; set; } = string.Empty;
}


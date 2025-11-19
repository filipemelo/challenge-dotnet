using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge.Models;

public class Transaction
{
    public int Id { get; set; }

    [Required]
    public int TransactionType { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(11)]
    public string Cpf { get; set; } = string.Empty;

    [Required]
    [MaxLength(12)]
    public string Card { get; set; } = string.Empty;

    [Required]
    public TimeOnly Time { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nature { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

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

    public bool IsEntrada => TransactionTypes.ContainsKey(TransactionType) && TransactionTypes[TransactionType].Nature == "Entrada";
    public bool IsSaida => !IsEntrada;
    public string TypeDescription => TransactionTypes.ContainsKey(TransactionType) ? TransactionTypes[TransactionType].Description : "Unknown";
    public string Signal => TransactionTypes.ContainsKey(TransactionType) ? TransactionTypes[TransactionType].Signal : "";
}

public class TransactionTypeInfo
{
    public string Description { get; set; } = string.Empty;
    public string Nature { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
}


using System.ComponentModel.DataAnnotations;

namespace Challenge.Models;

public class Store
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Owner { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public decimal Balance
    {
        get
        {
            return Transactions.Sum(t => t.IsEntrada ? t.Amount : -t.Amount);
        }
    }
}


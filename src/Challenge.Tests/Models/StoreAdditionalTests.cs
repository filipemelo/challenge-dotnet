using Challenge.Models;

namespace Challenge.Tests.Models;

public class StoreAdditionalTests
{
    [Fact]
    public void Balance_WithAllTransactionTypes_CalculatesCorrectly()
    {
        // Arrange
        var store = new Store
        {
            Name = "Test Store",
            Owner = "Test Owner",
            Transactions = new List<Transaction>
            {
                new Transaction { TransactionType = 1, Amount = 100.00m }, // Débito (Entrada)
                new Transaction { TransactionType = 2, Amount = 50.00m },  // Boleto (Saída)
                new Transaction { TransactionType = 3, Amount = 25.00m },  // Financiamento (Saída)
                new Transaction { TransactionType = 4, Amount = 75.00m },  // Crédito (Entrada)
                new Transaction { TransactionType = 5, Amount = 200.00m }, // Recebimento Empréstimo (Entrada)
                new Transaction { TransactionType = 6, Amount = 150.00m }, // Vendas (Entrada)
                new Transaction { TransactionType = 7, Amount = 80.00m },   // Recebimento TED (Entrada)
                new Transaction { TransactionType = 8, Amount = 90.00m },  // Recebimento DOC (Entrada)
                new Transaction { TransactionType = 9, Amount = 30.00m }   // Aluguel (Saída)
            }
        };

        // Act
        var balance = store.Balance;

        // Assert
        // Entradas: 100 + 75 + 200 + 150 + 80 + 90 = 695
        // Saídas: 50 + 25 + 30 = 105
        // Balance: 695 - 105 = 590
        Assert.Equal(590.00m, balance);
    }

    [Fact]
    public void Balance_WithEmptyTransactions_ReturnsZero()
    {
        // Arrange
        var store = new Store
        {
            Name = "Test Store",
            Owner = "Test Owner",
            Transactions = new List<Transaction>()
        };

        // Act
        var balance = store.Balance;

        // Assert
        Assert.Equal(0m, balance);
    }

    [Fact]
    public void Store_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var store = new Store
        {
            Id = 1,
            Name = "Test Store",
            Owner = "Test Owner",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, store.Id);
        Assert.Equal("Test Store", store.Name);
        Assert.Equal("Test Owner", store.Owner);
        Assert.NotNull(store.Transactions);
    }
}


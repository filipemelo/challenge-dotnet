using Challenge.Models;

namespace Challenge.Tests.Models;

public class StoreTests
{
    [Fact]
    public void Balance_WithEntradaTransactions_ReturnsPositiveBalance()
    {
        // Arrange
        var store = new Store
        {
            Name = "Test Store",
            Owner = "Test Owner",
            Transactions = new List<Transaction>
            {
                new Transaction { TransactionType = 1, Amount = 100.00m }, // Débito (Entrada)
                new Transaction { TransactionType = 4, Amount = 50.00m }  // Crédito (Entrada)
            }
        };

        // Act
        var balance = store.Balance;

        // Assert
        Assert.Equal(150.00m, balance);
    }

    [Fact]
    public void Balance_WithSaidaTransactions_ReturnsNegativeBalance()
    {
        // Arrange
        var store = new Store
        {
            Name = "Test Store",
            Owner = "Test Owner",
            Transactions = new List<Transaction>
            {
                new Transaction { TransactionType = 2, Amount = 100.00m }, // Boleto (Saída)
                new Transaction { TransactionType = 3, Amount = 50.00m }  // Financiamento (Saída)
            }
        };

        // Act
        var balance = store.Balance;

        // Assert
        Assert.Equal(-150.00m, balance);
    }

    [Fact]
    public void Balance_WithMixedTransactions_ReturnsCorrectBalance()
    {
        // Arrange
        var store = new Store
        {
            Name = "Test Store",
            Owner = "Test Owner",
            Transactions = new List<Transaction>
            {
                new Transaction { TransactionType = 1, Amount = 200.00m }, // Débito (Entrada)
                new Transaction { TransactionType = 2, Amount = 50.00m },  // Boleto (Saída)
                new Transaction { TransactionType = 4, Amount = 100.00m }  // Crédito (Entrada)
            }
        };

        // Act
        var balance = store.Balance;

        // Assert
        Assert.Equal(250.00m, balance);
    }

    [Fact]
    public void Balance_NoTransactions_ReturnsZero()
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
}


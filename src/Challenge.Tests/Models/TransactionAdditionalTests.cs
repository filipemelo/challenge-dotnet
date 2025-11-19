using Challenge.Models;

namespace Challenge.Tests.Models;

public class TransactionAdditionalTests
{
    [Fact]
    public void Transaction_UnknownTransactionType_ReturnsUnknownDescription()
    {
        // Arrange
        var transaction = new Transaction { TransactionType = 99 };

        // Act & Assert
        Assert.Equal("Unknown", transaction.TypeDescription);
        Assert.Equal("", transaction.Signal);
    }

    [Fact]
    public void Transaction_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Id = 1,
            TransactionType = 1,
            Date = new DateOnly(2023, 1, 15),
            Amount = 100.50m,
            Cpf = "12345678901",
            Card = "1234****5678",
            Time = new TimeOnly(14, 30, 0),
            Nature = "Entrada",
            Description = "Débito",
            StoreId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, transaction.Id);
        Assert.Equal(1, transaction.TransactionType);
        Assert.Equal(new DateOnly(2023, 1, 15), transaction.Date);
        Assert.Equal(100.50m, transaction.Amount);
        Assert.Equal("12345678901", transaction.Cpf);
        Assert.Equal("1234****5678", transaction.Card);
        Assert.Equal(new TimeOnly(14, 30, 0), transaction.Time);
        Assert.Equal("Entrada", transaction.Nature);
        Assert.Equal("Débito", transaction.Description);
        Assert.Equal(1, transaction.StoreId);
    }

    [Fact]
    public void Transaction_IsEntrada_ForAllEntradaTypes()
    {
        // Test all entrada types (1, 4, 5, 6, 7, 8)
        var entradaTypes = new[] { 1, 4, 5, 6, 7, 8 };
        
        foreach (var type in entradaTypes)
        {
            var transaction = new Transaction { TransactionType = type };
            Assert.True(transaction.IsEntrada, $"Transaction type {type} should be entrada");
            Assert.False(transaction.IsSaida, $"Transaction type {type} should not be saida");
        }
    }

    [Fact]
    public void Transaction_IsSaida_ForAllSaidaTypes()
    {
        // Test all saida types (2, 3, 9)
        var saidaTypes = new[] { 2, 3, 9 };
        
        foreach (var type in saidaTypes)
        {
            var transaction = new Transaction { TransactionType = type };
            Assert.False(transaction.IsEntrada, $"Transaction type {type} should not be entrada");
            Assert.True(transaction.IsSaida, $"Transaction type {type} should be saida");
        }
    }

    [Fact]
    public void Transaction_TransactionTypesDictionary_ContainsAllTypes()
    {
        // Assert
        Assert.Equal(9, Transaction.TransactionTypes.Count);
        Assert.True(Transaction.TransactionTypes.ContainsKey(1));
        Assert.True(Transaction.TransactionTypes.ContainsKey(9));
    }
}


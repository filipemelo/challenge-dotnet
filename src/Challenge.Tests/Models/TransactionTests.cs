using Challenge.Models;

namespace Challenge.Tests.Models;

public class TransactionTests
{
    [Theory]
    [InlineData(1, true, "Débito", "+")]      // Débito
    [InlineData(2, false, "Boleto", "-")]     // Boleto
    [InlineData(3, false, "Financiamento", "-")] // Financiamento
    [InlineData(4, true, "Crédito", "+")]     // Crédito
    [InlineData(5, true, "Recebimento Empréstimo", "+")] // Recebimento Empréstimo
    [InlineData(6, true, "Vendas", "+")]      // Vendas
    [InlineData(7, true, "Recebimento TED", "+")] // Recebimento TED
    [InlineData(8, true, "Recebimento DOC", "+")] // Recebimento DOC
    [InlineData(9, false, "Aluguel", "-")]    // Aluguel
    public void TransactionType_Properties_AreCorrect(int transactionType, bool isEntrada, string description, string signal)
    {
        // Arrange
        var transaction = new Transaction
        {
            TransactionType = transactionType,
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.Now),
            Time = TimeOnly.FromDateTime(DateTime.Now),
            Cpf = "12345678901",
            Card = "1234****5678",
            Nature = Transaction.TransactionTypes[transactionType].Nature,
            Description = Transaction.TransactionTypes[transactionType].Description
        };

        // Act & Assert
        Assert.Equal(isEntrada, transaction.IsEntrada);
        Assert.Equal(!isEntrada, transaction.IsSaida);
        Assert.Equal(description, transaction.TypeDescription);
        Assert.Equal(signal, transaction.Signal);
    }

    [Fact]
    public void IsEntrada_EntradaTransaction_ReturnsTrue()
    {
        // Arrange
        var transaction = new Transaction { TransactionType = 1 }; // Débito (Entrada)

        // Act & Assert
        Assert.True(transaction.IsEntrada);
        Assert.False(transaction.IsSaida);
    }

    [Fact]
    public void IsSaida_SaidaTransaction_ReturnsTrue()
    {
        // Arrange
        var transaction = new Transaction { TransactionType = 2 }; // Boleto (Saída)

        // Act & Assert
        Assert.False(transaction.IsEntrada);
        Assert.True(transaction.IsSaida);
    }
}


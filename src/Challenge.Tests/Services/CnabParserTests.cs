using Challenge.Services;

namespace Challenge.Tests.Services;

public class CnabParserTests
{
    [Fact]
    public void Parse_ValidCnabLine_ReturnsCorrectData()
    {
        // Arrange
        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.Stores);
        Assert.True(result.Stores.ContainsKey("BAR DO JOÃO"));
        
        var store = result.Stores["BAR DO JOÃO"];
        Assert.Equal("BAR DO JOÃO", store.Name);
        Assert.Equal("JOÃO MACEDO", store.Owner);
        Assert.Single(store.Transactions);
        
        var transaction = store.Transactions[0];
        Assert.Equal(3, transaction.TransactionType);
        Assert.Equal(new DateOnly(2019, 3, 1), transaction.Date);
        Assert.Equal(142.00m, transaction.Amount);
        Assert.Equal("09620676017", transaction.Cpf);
        Assert.Equal("4753****3153", transaction.Card);
        Assert.Equal(new TimeOnly(15, 34, 53), transaction.Time);
        Assert.Equal("Saída", transaction.Nature);
    }

    [Fact]
    public void Parse_InvalidTransactionType_ReturnsError()
    {
        // Arrange
        var cnabLine = "0201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid transaction type", result.Errors[0]);
    }

    [Fact]
    public void Parse_ShortLine_ReturnsError()
    {
        // Arrange
        var cnabLine = "3201903010000014200096206760174753";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("too short", result.Errors[0]);
    }

    [Fact]
    public void Parse_MultipleStores_ReturnsAllStores()
    {
        // Arrange
        var cnabLines = @"3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
5201903010000013200556418150633123****7687145607MARIA JOSEFINALOJA DO Ó - MATRIZ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Stores.Count);
        Assert.True(result.Stores.ContainsKey("BAR DO JOÃO"));
        Assert.True(result.Stores.ContainsKey("LOJA DO Ó - MATRIZ"));
    }

    [Fact]
    public void Parse_EmptyLine_IsIgnored()
    {
        // Arrange
        var cnabLines = @"3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       

5201903010000013200556418150633123****7687145607MARIA JOSEFINALOJA DO Ó - MATRIZ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Stores.Count);
    }
}


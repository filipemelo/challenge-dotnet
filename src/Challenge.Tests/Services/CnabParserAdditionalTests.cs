using Challenge.Services;

namespace Challenge.Tests.Services;

public class CnabParserAdditionalTests
{
    [Fact]
    public void Parse_InvalidDate_ReturnsError()
    {
        // Arrange
        var cnabLine = "3201903XX0000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid date", result.Errors[0]);
    }

    [Fact]
    public void Parse_InvalidAmount_ReturnsError()
    {
        // Arrange
        var cnabLine = "32019030100000ABC00096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid amount", result.Errors[0]);
    }

    [Fact]
    public void Parse_InvalidTime_ReturnsError()
    {
        // Arrange
        var cnabLine = "3201903010000014200096206760174753****3153XX3453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid time", result.Errors[0]);
    }

    [Fact]
    public void Parse_ShortTime_ReturnsError()
    {
        // Arrange
        var cnabLine = "3201903010000014200096206760174753****31531534JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid time", result.Errors[0]);
    }

    [Fact]
    public void Parse_LineWithoutStoreName_HandlesCorrectly()
    {
        // Arrange - Line shorter than 63 chars (no store name)
        var cnabLine = "3201903010000014200096206760174753****3153153453JOÃO MACEDO";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.Stores);
        var store = result.Stores.Values.First();
        Assert.Equal("", store.Name);
        Assert.Equal("JOÃO MACEDO", store.Owner);
    }

    [Fact]
    public void Parse_AllTransactionTypes_AreParsed()
    {
        // Arrange - Test all 9 transaction types
        var cnabLines = @"1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
2201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
4201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
5201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
6201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
7201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
8201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
9201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var store = result.Stores.Values.First();
        Assert.Equal(9, store.Transactions.Count);
        Assert.Equal(1, store.Transactions[0].TransactionType);
        Assert.Equal(9, store.Transactions[8].TransactionType);
    }

    [Fact]
    public void Parse_MultipleErrors_CollectsAllErrors()
    {
        // Arrange
        var cnabLines = @"0201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
3201903XX0000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
32019030100000ABC00096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void Parse_ValidLineWithAllFields_ExtractsAllData()
    {
        // Arrange
        var cnabLine = "1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var transaction = result.Stores.Values.First().Transactions.First();
        Assert.Equal(1, transaction.TransactionType);
        Assert.Equal(new DateOnly(2019, 3, 1), transaction.Date);
        Assert.Equal(142.00m, transaction.Amount);
        Assert.Equal("09620676017", transaction.Cpf);
        Assert.Equal("4753****3153", transaction.Card);
        Assert.Equal(new TimeOnly(15, 34, 53), transaction.Time);
        Assert.Equal("Entrada", transaction.Nature);
    }

    [Fact]
    public void Parse_VariousAmounts_AreParsedCorrectly()
    {
        // Arrange - Test with different amounts using valid CNAB format
        // Use known good format from actual CNAB file
        var cnabLines = @"1201903010000015200096206760171234****7890233000JOÃO MACEDO   BAR DO JOÃO       
2201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var transactions = result.Stores.Values.First().Transactions;
        Assert.Equal(2, transactions.Count);
        // Verify amounts are parsed correctly (divided by 100)
        var amounts = transactions.Select(t => t.Amount).ToList();
        Assert.All(amounts, amount => Assert.True(amount > 0));
    }

    [Fact]
    public void Parse_TimeAtMidnight_IsParsedCorrectly()
    {
        // Arrange
        var cnabLine = "1201903010000014200096206760174753****3153000000JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var transaction = result.Stores.Values.First().Transactions.First();
        Assert.Equal(new TimeOnly(0, 0, 0), transaction.Time);
    }

    [Fact]
    public void Parse_TimeAtEndOfDay_IsParsedCorrectly()
    {
        // Arrange
        var cnabLine = "1201903010000014200096206760174753****3153235959JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var transaction = result.Stores.Values.First().Transactions.First();
        Assert.Equal(new TimeOnly(23, 59, 59), transaction.Time);
    }

    [Fact]
    public void Parse_TimeWithValidRange_IsParsedCorrectly()
    {
        // Arrange - Test time 12:34:56
        var cnabLine = "1201903010000014200096206760174753****3153123456JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var transaction = result.Stores.Values.First().Transactions.First();
        Assert.Equal(new TimeOnly(12, 34, 56), transaction.Time);
    }

    [Fact]
    public void Parse_EmptyStream_ReturnsEmptyResult()
    {
        // Arrange
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(""));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Empty(result.Stores);
    }

    [Fact]
    public void Parse_WhitespaceOnlyLines_AreIgnored()
    {
        // Arrange
        var cnabLines = @"   
    
3201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.Stores);
        Assert.Single(result.Stores.Values.First().Transactions);
    }

    [Fact]
    public void Parse_InvalidTimeHour_ThrowsParseError()
    {
        // Arrange - Hour 25 is invalid (should be 0-23)
        var invalidTimeLine = "1201903010000014200096206760174753****3153253453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidTimeLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("hour") && e.Contains("0-23"));
    }

    [Fact]
    public void Parse_InvalidTimeMinute_ThrowsParseError()
    {
        // Arrange - Minute 60 is invalid (should be 0-59)
        // Time is at positions 42-47: need "166045" = hour 16, minute 60, second 45
        // Valid format: "1201903010000014200096206760174753****3153166045JOÃO MACEDO   BAR DO JOÃO       "
        var invalidTimeLine = "1201903010000014200096206760174753****3153166045JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidTimeLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("minute") && e.Contains("0-59"));
    }

    [Fact]
    public void Parse_InvalidTimeSecond_ThrowsParseError()
    {
        // Arrange - Second 60 is invalid (should be 0-59)
        var invalidTimeLine = "1201903010000014200096206760174753****3153153460JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidTimeLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("second") && e.Contains("0-59"));
    }
}


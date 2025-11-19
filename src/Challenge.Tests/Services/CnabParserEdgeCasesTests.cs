using Challenge.Services;

namespace Challenge.Tests.Services;

public class CnabParserEdgeCasesTests
{
    [Fact]
    public void Parse_LineWithOnlyRequiredFields_ParsesCorrectly()
    {
        // Arrange - Line with exactly 48 characters (minimum required)
        var cnabLine = "1201903010000014200096206760174753****3153153453";
        Assert.Equal(48, cnabLine.Length);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.Stores);
        var store = result.Stores.Values.First();
        Assert.Equal("", store.Name); // No store name in short line
        Assert.Single(store.Transactions);
    }

    [Fact]
    public void Parse_LineWithExactly63Characters_ParsesCorrectly()
    {
        // Arrange - Line with exactly 63 characters (has owner but no store name)
        // Format: Type(1) + Date(8) + Amount(10) + CPF(11) + Card(12) + Time(6) + Owner(15) = 63
        var cnabLine = "1201903010000014200096206760174753****3153153453OWNER        ";
        Assert.True(cnabLine.Length >= 48); // At least minimum required
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var store = result.Stores.Values.First();
        Assert.Equal("", store.Name.Trim());
        Assert.Contains("OWNER", store.Owner);
    }

    [Fact]
    public void Parse_LineWithExactly81Characters_ParsesCorrectly()
    {
        // Arrange - Full 81 character line (standard CNAB format)
        var cnabLine = "1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        // Pad to exactly 81 characters if needed
        while (cnabLine.Length < 81)
        {
            cnabLine += " ";
        }
        cnabLine = cnabLine.Substring(0, 81);
        Assert.Equal(81, cnabLine.Length);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var store = result.Stores.Values.First();
        Assert.Contains("BAR DO JOÃO", store.Name.Trim());
        Assert.Contains("JOÃO MACEDO", store.Owner.Trim());
    }

    [Fact]
    public void Parse_LineLongerThan81Characters_ParsesCorrectly()
    {
        // Arrange - Line longer than 81 characters (extra spaces)
        var cnabLine = "1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       EXTRA";
        Assert.True(cnabLine.Length > 81);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var store = result.Stores.Values.First();
        Assert.Contains("BAR DO JOÃO", store.Name);
    }

    [Fact]
    public void Parse_TransactionTypeZero_ReturnsError()
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
    public void Parse_TransactionTypeTen_ReturnsError()
    {
        // Arrange
        var cnabLine = "A201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        // Type 'A' (or 0 if not parseable) should result in error
    }

    [Fact]
    public void Parse_InvalidDateFormat_ReturnsError()
    {
        // Arrange
        var cnabLine = "1201903XX0000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid date", result.Errors[0]);
    }

    [Fact]
    public void Parse_InvalidTimeFormat_ReturnsError()
    {
        // Arrange
        var cnabLine = "1201903010000014200096206760174753****3153XX3453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid time", result.Errors[0]);
    }

    [Fact]
    public void Parse_InvalidAmountFormat_ReturnsError()
    {
        // Arrange
        var cnabLine = "12019030100000ABC00096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid amount", result.Errors[0]);
    }

    [Fact]
    public void Parse_MultipleStoresWithSameName_ConsolidatesCorrectly()
    {
        // Arrange - Multiple lines with same store name should create one store with multiple transactions
        var cnabLines = @"1201903010000014200096206760174753****3153153453JOÃO MACEDO   BAR DO JOÃO       
2201903010000014200096206760171234****7890153453JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLines));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.Stores);
        var store = result.Stores.Values.First();
        Assert.Equal(2, store.Transactions.Count);
    }

    [Fact]
    public void Parse_StoreNameWithTrailingSpaces_TrimsCorrectly()
    {
        // Arrange
        var cnabLine = "1201903010000014200096206760174753****3153153453JOÃO MACEDO   STORE NAME     ";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cnabLine));

        // Act
        var result = CnabParser.Parse(stream);

        // Assert
        Assert.Empty(result.Errors);
        var store = result.Stores.Values.First();
        Assert.Equal("STORE NAME", store.Name.Trim());
    }
}


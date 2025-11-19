using Challenge.Models;

namespace Challenge.Services;

/// <summary>
/// Parser for CNAB (Centro Nacional de Automação Bancária) format files.
/// Parses transaction data from fixed-width text files according to CNAB specifications.
/// </summary>
public class CnabParser
{
    // CNAB field positions (0-based indices)
    private const int TransactionTypeIndex = 0;
    private const int DateStartIndex = 1;
    private const int DateEndIndex = 8;
    private const int AmountStartIndex = 9;
    private const int AmountEndIndex = 18;
    private const int CpfStartIndex = 19;
    private const int CpfEndIndex = 29;
    private const int CardStartIndex = 30;
    private const int CardEndIndex = 41;
    private const int TimeStartIndex = 42;
    private const int TimeEndIndex = 47;
    private const int OwnerStartIndex = 48;
    private const int OwnerEndIndex = 61;
    private const int StoreStartIndex = 62;

    // CNAB field lengths and validation constants
    private const int MinLineLength = 48;
    private const int MinLineLengthForStoreName = 63;
    private const int TimeStringLength = 6;
    private const int TimeHourSubstringStart = 0;
    private const int TimeHourSubstringLength = 2;
    private const int TimeMinuteSubstringStart = 2;
    private const int TimeMinuteSubstringLength = 2;
    private const int TimeSecondSubstringStart = 4;
    private const int TimeSecondSubstringLength = 2;

    // Time validation ranges
    private const int HourMin = 0;
    private const int HourMax = 23;
    private const int MinuteMin = 0;
    private const int MinuteMax = 59;
    private const int SecondMin = 0;
    private const int SecondMax = 59;

    // Amount conversion constant (CNAB stores amounts in cents)
    private const int AmountDivisor = 100;

    /// <summary>
    /// Exception thrown when a parsing error occurs during CNAB file processing.
    /// </summary>
    public class ParseError : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseError"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ParseError(string message) : base(message) { }
    }

    /// <summary>
    /// Parses a CNAB file stream and returns the parsed transaction data.
    /// </summary>
    /// <param name="fileStream">The stream containing the CNAB file data.</param>
    /// <returns>A <see cref="ParseResult"/> containing parsed stores and transactions, or any parsing errors.</returns>
    public static ParseResult Parse(Stream fileStream)
    {
        var parser = new CnabParser();
        return parser.ParseFile(fileStream);
    }

    private ParseResult ParseFile(Stream fileStream)
    {
        var storesData = new Dictionary<string, StoreData>();
        var errors = new List<string>();

        using var reader = new StreamReader(fileStream);
        string? line;
        int lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            line = line.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var transactionData = ParseLine(line);
                var storeKey = transactionData.StoreName;

                if (!storesData.ContainsKey(storeKey))
                {
                    storesData[storeKey] = new StoreData
                    {
                        Name = transactionData.StoreName,
                        Owner = transactionData.StoreOwner,
                        Transactions = new List<TransactionData>()
                    };
                }

                storesData[storeKey].Transactions.Add(new TransactionData
                {
                    TransactionType = transactionData.TransactionType,
                    Date = transactionData.Date,
                    Amount = transactionData.Amount,
                    Cpf = transactionData.Cpf,
                    Card = transactionData.Card,
                    Time = transactionData.Time,
                    Nature = transactionData.Nature
                });
            }
            catch (ParseError ex)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
            }
        }

        return new ParseResult
        {
            Stores = storesData,
            Errors = errors
        };
    }

    private TransactionLineData ParseLine(string line)
    {
        line = line.TrimEnd();

        if (line.Length < MinLineLength)
        {
            throw new ParseError($"Line too short: must have at least {MinLineLength} characters (found: {line.Length})");
        }

        // Extract fields according to CNAB documentation (1-based positions, 0-based indices)
        var transactionType = int.TryParse(line[TransactionTypeIndex].ToString(), out var type) ? type : 0;

        var dateStr = SafeSlice(line, DateStartIndex, DateEndIndex);      // Positions 2-9 (indices 1-8): Date
        var amountStr = SafeSlice(line, AmountStartIndex, AmountEndIndex);  // Positions 10-19 (indices 9-18): Amount
        var cpf = SafeSlice(line, CpfStartIndex, CpfEndIndex)?.Trim() ?? "";  // Positions 20-30 (indices 19-29): CPF
        var card = SafeSlice(line, CardStartIndex, CardEndIndex)?.Trim() ?? ""; // Positions 31-42 (indices 30-41): Card
        var timeStr = SafeSlice(line, TimeStartIndex, TimeEndIndex);   // Positions 43-48 (indices 42-47): Time

        // Optional fields (may not exist completely)
        var ownerEnd = Math.Min(OwnerEndIndex, line.Length - 1);
        var storeEnd = line.Length - 1;

        string owner;
        string storeName;

        if (line.Length < MinLineLengthForStoreName)
        {
            owner = SafeSlice(line, OwnerStartIndex, line.Length - 1)?.Trim() ?? "";
            storeName = "";
        }
        else
        {
            owner = SafeSlice(line, OwnerStartIndex, ownerEnd)?.Trim() ?? "";
            storeName = SafeSlice(line, StoreStartIndex, storeEnd)?.Trim() ?? "";
        }

        if (!Transaction.TransactionTypes.ContainsKey(transactionType))
        {
            throw new ParseError($"Invalid transaction type: {transactionType}");
        }

        // Parse date
        if (!DateOnly.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            throw new ParseError($"Invalid date: {dateStr}");
        }

        // Parse amount (divide by 100 as per documentation - CNAB stores amounts in cents)
        if (!decimal.TryParse(amountStr, out var amount))
        {
            throw new ParseError($"Invalid amount: {amountStr}");
        }
        amount = amount / AmountDivisor;

        // Parse time
        if (timeStr == null || timeStr.Length < TimeStringLength)
        {
            throw new ParseError($"Invalid time: {timeStr}");
        }

        if (!int.TryParse(timeStr.Substring(TimeHourSubstringStart, TimeHourSubstringLength), out var hour) ||
            !int.TryParse(timeStr.Substring(TimeMinuteSubstringStart, TimeMinuteSubstringLength), out var minute) ||
            !int.TryParse(timeStr.Substring(TimeSecondSubstringStart, TimeSecondSubstringLength), out var second))
        {
            throw new ParseError($"Invalid time: {timeStr}");
        }

        // Validate time values before creating TimeOnly to prevent ArgumentOutOfRangeException
        if (hour < HourMin || hour > HourMax)
        {
            throw new ParseError($"Invalid time: {timeStr} (hour must be between {HourMin}-{HourMax}, found: {hour})");
        }
        if (minute < MinuteMin || minute > MinuteMax)
        {
            throw new ParseError($"Invalid time: {timeStr} (minute must be between {MinuteMin}-{MinuteMax}, found: {minute})");
        }
        if (second < SecondMin || second > SecondMax)
        {
            throw new ParseError($"Invalid time: {timeStr} (second must be between {SecondMin}-{SecondMax}, found: {second})");
        }

        var time = new TimeOnly(hour, minute, second);

        var nature = Transaction.TransactionTypes[transactionType].Nature;

        return new TransactionLineData
        {
            TransactionType = transactionType,
            Date = date,
            Amount = amount,
            Cpf = cpf,
            Card = card,
            Time = time,
            Nature = nature,
            StoreName = storeName,
            StoreOwner = owner
        };
    }

    private static string? SafeSlice(string str, int startIdx, int endIdx)
    {
        if (string.IsNullOrEmpty(str))
            return "";
        if (startIdx < 0 || startIdx >= str.Length)
            return "";
        if (endIdx < startIdx)
            return "";

        endIdx = Math.Min(endIdx, str.Length - 1);
        var length = endIdx - startIdx + 1;
        if (length <= 0)
            return "";
        return str.Substring(startIdx, length);
    }
}

/// <summary>
/// Contains the result of parsing a CNAB file, including stores, transactions, and any parsing errors.
/// </summary>
public class ParseResult
{
    /// <summary>
    /// Gets or sets a dictionary of parsed stores, keyed by store name.
    /// </summary>
    public Dictionary<string, StoreData> Stores { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of parsing errors encountered during file processing.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Represents store data parsed from a CNAB file.
/// </summary>
public class StoreData
{
    /// <summary>
    /// Gets or sets the name of the store.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner name of the store.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of transactions associated with this store.
    /// </summary>
    public List<TransactionData> Transactions { get; set; } = new();
}

/// <summary>
/// Represents transaction data parsed from a CNAB file line.
/// </summary>
public class TransactionData
{
    /// <summary>
    /// Gets or sets the transaction type code (1-9).
    /// </summary>
    public int TransactionType { get; set; }

    /// <summary>
    /// Gets or sets the date of the transaction.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the CPF (Brazilian tax ID) associated with the transaction.
    /// </summary>
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the card number (masked format).
    /// </summary>
    public string Card { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time of the transaction.
    /// </summary>
    public TimeOnly Time { get; set; }

    /// <summary>
    /// Gets or sets the nature of the transaction ("Entrada" or "Saída").
    /// </summary>
    public string Nature { get; set; } = string.Empty;
}

/// <summary>
/// Represents a complete transaction line data including store information.
/// Extends <see cref="TransactionData"/> with store name and owner.
/// </summary>
public class TransactionLineData : TransactionData
{
    /// <summary>
    /// Gets or sets the name of the store where the transaction occurred.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner name of the store where the transaction occurred.
    /// </summary>
    public string StoreOwner { get; set; } = string.Empty;
}


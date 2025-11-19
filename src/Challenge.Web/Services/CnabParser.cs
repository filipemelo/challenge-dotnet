using Challenge.Models;

namespace Challenge.Services;

public class CnabParser
{
    public class ParseError : Exception
    {
        public ParseError(string message) : base(message) { }
    }

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
            line = line.TrimEnd('\r', '\n').TrimEnd();

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

        if (line.Length < 48)
        {
            throw new ParseError($"Line too short: must have at least 48 characters (found: {line.Length})");
        }

        // Extract fields according to CNAB documentation (1-based positions, 0-based indices)
        var transactionType = int.TryParse(line[0].ToString(), out var type) ? type : 0;

        var dateStr = SafeSlice(line, 1, 8);      // Positions 2-9 (indices 1-8): Date
        var amountStr = SafeSlice(line, 9, 18);  // Positions 10-19 (indices 9-18): Amount
        var cpf = SafeSlice(line, 19, 29)?.Trim() ?? "";  // Positions 20-30 (indices 19-29): CPF
        var card = SafeSlice(line, 30, 41)?.Trim() ?? ""; // Positions 31-42 (indices 30-41): Card
        var timeStr = SafeSlice(line, 42, 47);   // Positions 43-48 (indices 42-47): Time

        // Optional fields (may not exist completely)
        var ownerStart = 48;
        var ownerEnd = Math.Min(61, line.Length - 1);

        var storeStart = 62;
        var storeEnd = line.Length - 1;

        string owner;
        string storeName;

        if (line.Length < 63)
        {
            owner = SafeSlice(line, ownerStart, line.Length - 1)?.Trim() ?? "";
            storeName = "";
        }
        else
        {
            owner = SafeSlice(line, ownerStart, ownerEnd)?.Trim() ?? "";
            storeName = SafeSlice(line, storeStart, storeEnd)?.Trim() ?? "";
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

        // Parse amount (divide by 100 as per documentation)
        if (!decimal.TryParse(amountStr, out var amount))
        {
            throw new ParseError($"Invalid amount: {amountStr}");
        }
        amount = amount / 100;

        // Parse time
        if (timeStr == null || timeStr.Length < 6)
        {
            throw new ParseError($"Invalid time: {timeStr}");
        }

        if (!int.TryParse(timeStr.Substring(0, 2), out var hour) ||
            !int.TryParse(timeStr.Substring(2, 2), out var minute) ||
            !int.TryParse(timeStr.Substring(4, 2), out var second))
        {
            throw new ParseError($"Invalid time: {timeStr}");
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

public class ParseResult
{
    public Dictionary<string, StoreData> Stores { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class StoreData
{
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<TransactionData> Transactions { get; set; } = new();
}

public class TransactionData
{
    public int TransactionType { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Card { get; set; } = string.Empty;
    public TimeOnly Time { get; set; }
    public string Nature { get; set; } = string.Empty;
}

public class TransactionLineData : TransactionData
{
    public string StoreName { get; set; } = string.Empty;
    public string StoreOwner { get; set; } = string.Empty;
}


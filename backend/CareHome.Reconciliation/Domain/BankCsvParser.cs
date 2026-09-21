using System.Globalization;
using System.Text;

namespace CareHome.Api.Reconciliation.Domain;

public sealed class BankCsvParseRow
{
    public int RowNumber { get; init; }

    public string? Date { get; set; }

    public string? ValueDate { get; set; }

    public string? Amount { get; set; }

    public string? Debit { get; set; }

    public string? Credit { get; set; }

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public string? Counterparty { get; set; }

    public string? ExternalId { get; set; }

    public string? Currency { get; set; }

    public Dictionary<string, string> Raw { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class BankCsvValidatedRow
{
    public int RowNumber { get; init; }

    public DateOnly TransactionDate { get; init; }

    public DateOnly? ValueDate { get; init; }

    public decimal Amount { get; init; }

    public string Direction { get; init; } = BankTransactionDirections.In;

    public string Currency { get; init; } = "GBP";

    public string? Reference { get; init; }

    public string? Description { get; init; }

    public string? Counterparty { get; init; }

    public string? ExternalTransactionReference { get; init; }

    public string? Error { get; init; }

    public bool IsValid => string.IsNullOrEmpty(Error);
}

public sealed class BankCsvColumnMapping
{
    public string? Date { get; set; }

    public string? ValueDate { get; set; }

    public string? Amount { get; set; }

    public string? Debit { get; set; }

    public string? Credit { get; set; }

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public string? Counterparty { get; set; }

    public string? ExternalId { get; set; }

    public string? Currency { get; set; }
}

public static class BankCsvParser
{
    public static List<string> ReadHeaders(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return [];
        }

        return SplitCsvLine(headerLine).Select(h => h.Trim()).Where(h => h.Length > 0).ToList();
    }

    public static List<BankCsvParseRow> ParseRows(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return [];
        }

        var headers = SplitCsvLine(headerLine).Select(h => h.Trim()).ToList();
        var rows = new List<BankCsvParseRow>();
        var rowNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = SplitCsvLine(line);
            var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count && i < cells.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(headers[i]))
                {
                    raw[headers[i]] = cells[i].Trim();
                }
            }

            rows.Add(new BankCsvParseRow
            {
                RowNumber = rowNumber,
                Raw = raw
            });
        }

        return rows;
    }

    public static void ApplyMapping(BankCsvParseRow row, BankCsvColumnMapping mapping)
    {
        static string? Get(Dictionary<string, string> raw, string? key) =>
            key is not null && raw.TryGetValue(key, out var v) ? v : null;

        row.Date = Get(row.Raw, mapping.Date);
        row.ValueDate = Get(row.Raw, mapping.ValueDate);
        row.Amount = Get(row.Raw, mapping.Amount);
        row.Debit = Get(row.Raw, mapping.Debit);
        row.Credit = Get(row.Raw, mapping.Credit);
        row.Reference = Get(row.Raw, mapping.Reference);
        row.Description = Get(row.Raw, mapping.Description);
        row.Counterparty = Get(row.Raw, mapping.Counterparty);
        row.ExternalId = Get(row.Raw, mapping.ExternalId);
        row.Currency = Get(row.Raw, mapping.Currency);
    }

    public static BankCsvValidatedRow ValidateRow(BankCsvParseRow row, string defaultCurrency)
    {
        if (!TryParseDate(row.Date, out var txnDate))
        {
            return Invalid(row.RowNumber, "Invalid or missing transaction date.");
        }

        DateOnly? valueDate = null;
        if (!string.IsNullOrWhiteSpace(row.ValueDate))
        {
            if (!TryParseDate(row.ValueDate, out var vd))
            {
                return Invalid(row.RowNumber, "Invalid value date.");
            }

            valueDate = vd;
        }

        decimal amount;
        string direction;
        if (!string.IsNullOrWhiteSpace(row.Amount))
        {
            if (!TryParseAmount(row.Amount, out amount))
            {
                return Invalid(row.RowNumber, "Invalid amount.");
            }

            direction = amount < 0 ? BankTransactionDirections.Out : BankTransactionDirections.In;
            amount = Math.Abs(amount);
        }
        else if (!string.IsNullOrWhiteSpace(row.Credit) || !string.IsNullOrWhiteSpace(row.Debit))
        {
            var credit = 0m;
            var debit = 0m;
            if (!string.IsNullOrWhiteSpace(row.Credit) && !TryParseAmount(row.Credit, out credit))
            {
                return Invalid(row.RowNumber, "Invalid credit amount.");
            }

            if (!string.IsNullOrWhiteSpace(row.Debit) && !TryParseAmount(row.Debit, out debit))
            {
                return Invalid(row.RowNumber, "Invalid debit amount.");
            }

            if (credit > 0 && debit > 0)
            {
                return Invalid(row.RowNumber, "Row has both debit and credit.");
            }

            if (credit <= 0 && debit <= 0)
            {
                return Invalid(row.RowNumber, "Missing debit/credit amount.");
            }

            amount = credit > 0 ? credit : debit;
            direction = credit > 0 ? BankTransactionDirections.In : BankTransactionDirections.Out;
        }
        else
        {
            return Invalid(row.RowNumber, "Missing amount.");
        }

        if (amount <= 0)
        {
            return Invalid(row.RowNumber, "Amount must be greater than zero.");
        }

        var currency = string.IsNullOrWhiteSpace(row.Currency)
            ? defaultCurrency
            : row.Currency.Trim().ToUpperInvariant();

        if (currency.Length != 3)
        {
            return Invalid(row.RowNumber, "Unsupported currency.");
        }

        return new BankCsvValidatedRow
        {
            RowNumber = row.RowNumber,
            TransactionDate = txnDate,
            ValueDate = valueDate,
            Amount = Money.Round(amount),
            Direction = direction,
            Currency = currency,
            Reference = row.Reference?.Trim(),
            Description = row.Description?.Trim(),
            Counterparty = row.Counterparty?.Trim(),
            ExternalTransactionReference = row.ExternalId?.Trim()
        };
    }

    private static BankCsvValidatedRow Invalid(int rowNumber, string error) =>
        new() { RowNumber = rowNumber, Error = error };

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = default;
            return false;
        }

        var formats = new[]
        {
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "dd-MM-yyyy",
            "dd/MM/yy",
            "d/M/yyyy"
        };

        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(value.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        return DateOnly.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        var cleaned = value.Trim().Replace("£", string.Empty).Replace(",", string.Empty);
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
               || decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        result.Add(current.ToString());
        return result;
    }
}

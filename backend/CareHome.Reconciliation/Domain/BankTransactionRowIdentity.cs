using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CareHome.Api.Reconciliation.Domain;

public static class BankTransactionRowIdentity
{
    public static string ComputeRowHash(
        int bankAccountId,
        DateOnly transactionDate,
        decimal amount,
        string direction,
        string? externalId,
        string? normalizedReference,
        string? counterparty)
    {
        var payload = externalId is { Length: > 0 }
            ? $"ext|{bankAccountId}|{externalId.Trim()}"
            : string.Join(
                '|',
                bankAccountId,
                transactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Money.Round(amount).ToString("0.00", CultureInfo.InvariantCulture),
                direction.Trim(),
                Normalize(normalizedReference),
                Normalize(counterparty));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    public static string NormalizeReference(string? reference, string? description)
    {
        var combined = $"{reference} {description}".Trim();
        if (combined.Length == 0)
        {
            return string.Empty;
        }

        var upper = combined.ToUpperInvariant();
        var chars = upper.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}

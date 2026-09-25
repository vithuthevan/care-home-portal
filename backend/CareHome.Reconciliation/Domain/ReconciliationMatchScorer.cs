using System.Text.Json;
using System.Text.RegularExpressions;

namespace CareHome.Api.Reconciliation.Domain;

public sealed record MatchScoreFactor(string Label, string Detail, int Points);

public sealed record ReconciliationMatchCandidate(
    IReadOnlyList<MatchAllocationLine> Lines,
    int TotalScore,
    IReadOnlyList<MatchScoreFactor> Factors);

public sealed record MatchAllocationLine(Guid InvoicePublicId, string InvoiceNumber, decimal Amount);

public static partial class ReconciliationMatchScorer
{
    public const int ExactAmountPoints = 50;
    public const int InvoiceReferencePoints = 30;
    public const int FunderMatchPoints = 10;
    public const int DateProximityPoints = 5;
    public const int HistoricalPayerPoints = 5;

    public static ReconciliationMatchCandidate? ScoreSingleInvoice(
        decimal bankAmount,
        DateOnly bankDate,
        string normalizedReference,
        string? counterparty,
        InvoiceMatchTarget invoice,
        int? historicalPayerMatchCount = null)
    {
        if (invoice.Outstanding <= 0)
        {
            return null;
        }

        var factors = new List<MatchScoreFactor>();
        var score = 0;

        var allocAmount = Math.Min(bankAmount, invoice.Outstanding);
        if (Money.Round(allocAmount) == Money.Round(invoice.Outstanding)
            || Money.Round(allocAmount) == Money.Round(bankAmount))
        {
            if (Money.Round(bankAmount) == Money.Round(invoice.Outstanding))
            {
                score += ExactAmountPoints;
                factors.Add(new MatchScoreFactor("Amount", "Exact", ExactAmountPoints));
            }
        }

        if (ContainsInvoiceReference(normalizedReference, invoice.InvoiceNumber))
        {
            score += InvoiceReferencePoints;
            factors.Add(new MatchScoreFactor("Invoice reference", "Found", InvoiceReferencePoints));
        }

        if (!string.IsNullOrWhiteSpace(counterparty)
            && !string.IsNullOrWhiteSpace(invoice.FunderName)
            && normalizedReference.Contains(invoice.FunderName, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(counterparty)
                && counterparty.Contains(invoice.FunderName, StringComparison.OrdinalIgnoreCase)))
        {
            score += FunderMatchPoints;
            factors.Add(new MatchScoreFactor("Payer", "Match", FunderMatchPoints));
        }

        var days = Math.Abs(bankDate.DayNumber - invoice.InvoiceDate.DayNumber);
        if (days <= 7)
        {
            score += DateProximityPoints;
            factors.Add(new MatchScoreFactor("Date", $"{days} days", DateProximityPoints));
        }

        if (historicalPayerMatchCount is > 0)
        {
            score += HistoricalPayerPoints;
            factors.Add(new MatchScoreFactor("Historical payer pattern", "Match", HistoricalPayerPoints));
        }

        if (score < 50)
        {
            return null;
        }

        return new ReconciliationMatchCandidate(
            [new MatchAllocationLine(invoice.PublicId, invoice.InvoiceNumber, allocAmount)],
            score,
            factors);
    }

    public static ReconciliationMatchCandidate? ScoreMultiInvoiceExactSum(
        decimal bankAmount,
        string normalizedReference,
        IReadOnlyList<InvoiceMatchTarget> invoices)
    {
        var open = invoices.Where(i => i.Outstanding > 0).ToList();
        if (open.Count < 2)
        {
            return null;
        }

        var sum = Money.Round(open.Sum(i => i.Outstanding));
        if (sum != Money.Round(bankAmount))
        {
            return null;
        }

        var factors = new List<MatchScoreFactor>
        {
            new("Amount", "Exact split", ExactAmountPoints)
        };

        var refHits = open.Count(i => ContainsInvoiceReference(normalizedReference, i.InvoiceNumber));
        if (refHits > 0)
        {
            factors.Add(new MatchScoreFactor("Invoice reference", $"{refHits} found", InvoiceReferencePoints));
        }

        var score = ExactAmountPoints + (refHits > 0 ? InvoiceReferencePoints : 0);
        var lines = open.Select(i => new MatchAllocationLine(i.PublicId, i.InvoiceNumber, i.Outstanding)).ToList();
        return new ReconciliationMatchCandidate(lines, score, factors);
    }

    public static string SerializeFactors(IReadOnlyList<MatchScoreFactor> factors) =>
        JsonSerializer.Serialize(factors);

    public static IReadOnlyList<MatchScoreFactor> DeserializeFactors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<MatchScoreFactor>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static bool ContainsInvoiceReference(string normalizedReference, string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(normalizedReference) || string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return false;
        }

        var token = InvoiceNumberTokenRegex().Match(invoiceNumber);
        var needle = token.Success ? token.Value : invoiceNumber;
        return normalizedReference.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || normalizedReference.Replace(" ", string.Empty).Contains(
                   needle.Replace("-", string.Empty),
                   StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"INV[-\s]?\d+", RegexOptions.IgnoreCase)]
    private static partial Regex InvoiceNumberTokenRegex();
}

public sealed record InvoiceMatchTarget(
    int InvoiceId,
    Guid PublicId,
    string InvoiceNumber,
    decimal Outstanding,
    DateOnly InvoiceDate,
    string FunderName,
    int FundingAuthorityId);

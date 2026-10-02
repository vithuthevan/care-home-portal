using System.Globalization;
using CareHome.Api.Common;
using CareHome.Api.Models;

namespace CareHome.Api.Funding;

/// <summary>
/// A funding authority may be used on only one active contract per resident and
/// invoice category. The same authority may be used again when the invoice category
/// is different. Nominal code is not part of that identity.
/// </summary>
public static class FundingContractOverlap
{
    public const string ConflictCode = "OVERLAPPING_FUNDING_CONTRACT";

    public const string BillingCode = "OVERLAPPING_FUNDING_CONTRACTS";

    public const string ConflictMessage =
        "This resident already has an active contract for this funding authority in the same invoice category. The same funding authority can be used again with a different invoice category.";

    public static bool PeriodsOverlap(
        DateOnly startA,
        DateOnly? endA,
        DateOnly startB,
        DateOnly? endB)
        => DateRanges.Overlaps(startA, endA, startB, endB);

    public static List<(ClientFundingContract Left, ClientFundingContract Right)> FindOverlappingPairs(
        IReadOnlyList<ClientFundingContract> contracts)
    {
        var pairs = new List<(ClientFundingContract, ClientFundingContract)>();
        for (var i = 0; i < contracts.Count; i++)
        {
            for (var j = i + 1; j < contracts.Count; j++)
            {
                if (PeriodsOverlap(
                        contracts[i].ContractStartDate,
                        contracts[i].ContractEndDate,
                        contracts[j].ContractStartDate,
                        contracts[j].ContractEndDate))
                {
                    pairs.Add((contracts[i], contracts[j]));
                }
            }
        }

        return pairs;
    }

    public static string FormatOpenEnded(DateOnly? end)
        => end is null || end.Value == DateRanges.OpenEnded
            ? "open"
            : end.Value.ToString("yyyy-MM-dd");

    public static string BillingUserMessage(
        string residentName,
        string authorityName,
        string categoryName,
        DateOnly? overlapStart,
        DateOnly? overlapEnd)
    {
        var dates = overlapStart is null
            ? "this billing period"
            : $"{FormatBusinessDate(overlapStart)} to {FormatBusinessDate(overlapEnd)}";

        return
            $"Overlapping funding arrangements found for {residentName} " +
            $"({authorityName} / {categoryName}). Overlapping dates: {dates}. " +
            "Billing is blocked until the overlap is resolved.";
    }

    public static string FormatBusinessDate(DateOnly? date)
        => date is null || date.Value == DateRanges.OpenEnded
            ? "open-ended"
            : date.Value.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
}

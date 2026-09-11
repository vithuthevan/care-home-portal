using CareHome.Api.Billing;
using CareHome.Api.Models;
using CareHome.Api.Services;
using Xunit;

namespace CareHome.Api.Tests;

public class InvoiceVoidRulesTests
{
    [Fact]
    public void ValidateCanVoid_rejects_already_void()
    {
        var error = InvoiceVoidRules.ValidateCanVoid("Void", "NotPaid", hasCreditNotes: false);
        Assert.Equal("Invoice is already void.", error);
    }

    [Fact]
    public void ValidateCanVoid_rejects_paid()
    {
        var error = InvoiceVoidRules.ValidateCanVoid("Sent", "Paid", hasCreditNotes: false);
        Assert.Contains("paid", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCanVoid_rejects_when_credit_notes_exist()
    {
        var error = InvoiceVoidRules.ValidateCanVoid("Sent", "NotPaid", hasCreditNotes: true);
        Assert.Contains("credit notes", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCanVoid_allows_unpaid_without_credits()
    {
        Assert.Null(InvoiceVoidRules.ValidateCanVoid("Generated", "NotPaid", hasCreditNotes: false));
    }
}

public class MiscChargeResolveRowTests
{
    [Fact]
    public void ResolveRow_ignores_forged_client_id_and_uses_reference()
    {
        var clients = new List<Client>
        {
            new()
            {
                Id = 10,
                TenantId = 1,
                CareHomeId = 1,
                SageId = "S1",
                ReferenceNumber = "REF-A",
                FirstName = "Ann",
                LastName = "Lee",
                CareType = "Nursing",
                Status = "Current",
                AdmissionDate = new DateOnly(2024, 1, 1)
            }
        };
        var nominals = new List<NominalCode>();
        var existing = new HashSet<string>(StringComparer.Ordinal);

        var raw = new RawMiscRow
        {
            RowNumber = 1,
            ClientReference = "REF-A",
            UsedDate = "2024-06-01",
            Description = "Taxi",
            Amount = "12.50"
        };

        var resolved = MiscChargeImportService.ResolveRow(raw, clients, nominals, existing);

        Assert.True(resolved.IsValid);
        Assert.Equal(10, resolved.ClientId);
        Assert.Equal(12.50m, resolved.Amount);
        Assert.Equal("Taxi", resolved.Description);
    }

    [Fact]
    public void ResolveRow_rejects_unknown_client_reference()
    {
        var raw = new RawMiscRow
        {
            RowNumber = 1,
            ClientReference = "MISSING",
            UsedDate = "2024-06-01",
            Description = "Taxi",
            Amount = "12.50"
        };

        var resolved = MiscChargeImportService.ResolveRow(
            raw,
            clients: [],
            nominals: [],
            existingDuplicateKeys: new HashSet<string>(StringComparer.Ordinal));

        Assert.False(resolved.IsValid);
        Assert.Contains("Unknown client", resolved.Error);
    }

    [Fact]
    public void ResolveRow_rejects_duplicate_key()
    {
        var clients = new List<Client>
        {
            new()
            {
                Id = 10,
                TenantId = 1,
                CareHomeId = 1,
                SageId = "S1",
                ReferenceNumber = "REF-A",
                FirstName = "Ann",
                LastName = "Lee",
                CareType = "Nursing",
                Status = "Current",
                AdmissionDate = new DateOnly(2024, 1, 1)
            }
        };
        var existing = new HashSet<string>(StringComparer.Ordinal)
        {
            "10|2024-06-01|Taxi|12.50"
        };

        var raw = new RawMiscRow
        {
            RowNumber = 1,
            ClientReference = "REF-A",
            UsedDate = "2024-06-01",
            Description = "Taxi",
            Amount = "12.50"
        };

        var resolved = MiscChargeImportService.ResolveRow(raw, clients, [], existing);
        Assert.False(resolved.IsValid);
        Assert.Contains("Duplicate", resolved.Error);
    }
}

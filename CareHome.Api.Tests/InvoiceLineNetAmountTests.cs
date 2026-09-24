using CareHome.Api.Common;
using CareHome.Api.Models;
using Xunit;

namespace CareHome.Api.Tests;

public class InvoiceLineNetAmountTests
{
    [Fact]
    public void FromParts_applies_credit_line_sum_and_rounds()
    {
        var net = InvoiceLineNetAmount.FromParts(100m, -25.555m);
        Assert.Equal(74.45m, net);
    }

    [Fact]
    public void FromLine_ignores_void_credit_notes()
    {
        var line = new InvoiceLine
        {
            LineAmount = 200m,
            CreditNoteLines =
            [
                new CreditNoteLine
                {
                    Amount = -50m,
                    CreditNote = new CreditNote { Status = CreditNoteStatuses.Void }
                },
                new CreditNoteLine
                {
                    Amount = -30m,
                    CreditNote = new CreditNote { Status = CreditNoteStatuses.Generated }
                }
            ]
        };

        Assert.Equal(170m, InvoiceLineNetAmount.FromLine(line));
    }

    [Fact]
    public void IsNonZeroExportable_matches_sage_skip_rule()
    {
        Assert.False(InvoiceLineNetAmount.IsNonZeroExportable(0m));
        Assert.True(InvoiceLineNetAmount.IsNonZeroExportable(0.01m));
    }
}

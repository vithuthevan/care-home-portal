using CareHome.Api.Reconciliation.Domain;
using Xunit;

namespace CareHome.Api.Tests;

public class BankCsvParserTests
{
    [Fact]
    public void Parses_amount_column()
    {
        const string csv = "Date,Amount,Reference\n2026-09-21,4200.00,INV-1042\n";
        var rows = BankCsvParser.ParseRows(csv);
        var mapping = new BankCsvColumnMapping { Date = "Date", Amount = "Amount", Reference = "Reference" };
        BankCsvParser.ApplyMapping(rows[0], mapping);
        var validated = BankCsvParser.ValidateRow(rows[0], "GBP");
        Assert.True(validated.IsValid);
        Assert.Equal(4200m, validated.Amount);
    }

    [Fact]
    public void Duplicate_row_hash_is_stable()
    {
        var h1 = BankTransactionRowIdentity.ComputeRowHash(1, new DateOnly(2026, 9, 21), 4200m, "In", null, "INV 1042", "NHS");
        var h2 = BankTransactionRowIdentity.ComputeRowHash(1, new DateOnly(2026, 9, 21), 4200m, "In", null, "INV 1042", "NHS");
        Assert.Equal(h1, h2);
    }
}

public class ReconciliationMatchScorerTests
{
    [Fact]
    public void Exact_invoice_match_scores_high()
    {
        var candidate = ReconciliationMatchScorer.ScoreSingleInvoice(
            4200m,
            new DateOnly(2026, 9, 21),
            "INV 1042 SMITH",
            "NHS NORTH",
            new InvoiceMatchTarget(1, Guid.NewGuid(), "INV-1042", 4200m, new DateOnly(2026, 9, 19), "NHS North", 1));

        Assert.NotNull(candidate);
        Assert.True(candidate!.TotalScore >= 80);
    }

    [Fact]
    public void Multi_invoice_exact_sum_match()
    {
        var invoices = new[]
        {
            new InvoiceMatchTarget(1, Guid.NewGuid(), "INV-1001", 4000m, new DateOnly(2026, 8, 1), "NHS", 1),
            new InvoiceMatchTarget(2, Guid.NewGuid(), "INV-1002", 5500m, new DateOnly(2026, 8, 1), "NHS", 1),
            new InvoiceMatchTarget(3, Guid.NewGuid(), "INV-1003", 5000m, new DateOnly(2026, 8, 1), "NHS", 1),
        };

        var match = ReconciliationMatchScorer.ScoreMultiInvoiceExactSum(14500m, "BATCH PAYMENT", invoices);
        Assert.NotNull(match);
        Assert.Equal(3, match!.Lines.Count);
        Assert.Equal(14500m, match.Lines.Sum(l => l.Amount));
    }
}

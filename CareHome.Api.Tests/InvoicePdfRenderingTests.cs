using System.Text;
using CareHome.Api.Documents;
using Xunit;
using CareHome.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Tests;

public class InvoicePdfRenderingTests
{
    static InvoicePdfRenderingTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Invoice_pdf_embeds_snapshot_values_unchanged()
    {
        var invoice = new Invoice
        {
            Id = 1,
            InvoiceNumber = "INV-PDF-QA-001",
            InvoiceDate = new DateOnly(2026, 9, 22),
            DueDate = new DateOnly(2026, 10, 22),
            PeriodStart = new DateOnly(2026, 9, 22),
            PeriodEnd = new DateOnly(2026, 9, 30),
            TotalAmount = 85.71m,
            SnapshotTenantName = "Demo Organisation",
            SnapshotCompanyName = "Demo Company Ltd",
            SnapshotCareHomeName = "Demo Care Home",
            SnapshotFundingAuthorityName = "Demo Authority",
            SnapshotFundingAuthorityCode = "DA-01",
            SnapshotInvoiceCategoryName = "Residential",
            SnapshotBankAccountName = "Demo Bank Account",
            SnapshotSortCode = "00-00-00",
            SnapshotAccountNumber = "00000000",
            Lines =
            [
                new InvoiceLine
                {
                    SnapshotClientName = "Test Resident",
                    SnapshotClientReferenceNumber = "RES-REF-1",
                    SnapshotSageId = "SAGE-1",
                    ServicePeriodStart = new DateOnly(2026, 9, 24),
                    ServicePeriodEnd = new DateOnly(2026, 9, 24),
                    Description = "Residential care (weekly rate)",
                    EligibleDays = 1,
                    RateAmount = 600m,
                    RateFrequency = "Weekly",
                    SnapshotNominalCode = "4000",
                    LineAmount = 85.71m,
                    AmountBasis = "Pro-rated weekly rate for 1 eligible day (billing snapshot)."
                }
            ]
        };

        var store = new InMemoryDocumentStore();
        var service = new InvoicePdfService(store, NullLogger<InvoicePdfService>.Instance);
        var bytes = await service.GetOrCreateInvoicePdfAsync(invoice, Guid.NewGuid());

        Assert.True(bytes.Length > 2_000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes.AsSpan(0, 4)));
        Assert.False(string.IsNullOrWhiteSpace(invoice.PdfPath));
    }

    private sealed class InMemoryDocumentStore : IDocumentStore
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public Task<string> SaveAsync(
            string relativeFolder,
            string fileName,
            byte[] content,
            CancellationToken cancellationToken = default)
        {
            var path = $"{relativeFolder}/{fileName}";
            _files[path] = content;
            return Task.FromResult(path);
        }

        public Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            _files.TryGetValue(relativePath, out var bytes);
            return Task.FromResult(bytes);
        }

        public string GetFullPath(string relativePath) => relativePath;
    }
}

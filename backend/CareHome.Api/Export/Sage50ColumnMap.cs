using System.Text;
using CareHome.Api.Common;
using CareHome.Api.Models;

namespace CareHome.Api.Export
{
    /// <summary>
    /// PROVISIONAL Sage50 CSV column map. Final import specification requires stakeholder confirmation.
    /// </summary>
    public class Sage50ColumnMap
    {
        public static readonly string[] Headers =
        [
            "AccountRef",
            "NominalCode",
            "InvoiceNumber",
            "InvoiceDate",
            "Details",
            "NetAmount",
            "TaxCode",
            "Department"
        ];

        public string BuildCsv(IEnumerable<Invoice> invoices)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", Headers));

            foreach (var invoice in invoices)
            {
                foreach (var line in invoice.Lines)
                {
                    var netAmount = InvoiceLineNetAmount.FromLine(line);
                    if (!InvoiceLineNetAmount.IsNonZeroExportable(netAmount))
                    {
                        continue;
                    }

                    var values = new[]
                    {
                        Csv(line.SnapshotSageId, neutralizeFormula: false),
                        Csv(line.SnapshotNominalCode, neutralizeFormula: false),
                        Csv(invoice.InvoiceNumber, neutralizeFormula: false),
                        Csv(invoice.InvoiceDate.ToString("yyyy-MM-dd"), neutralizeFormula: false),
                        Csv(line.Description, neutralizeFormula: true),
                        Csv(netAmount.ToString("0.00"), neutralizeFormula: false),
                        Csv("T0", neutralizeFormula: false),
                        Csv(invoice.SnapshotCareHomeCode, neutralizeFormula: false)
                    };
                    builder.AppendLine(string.Join(",", values));
                }
            }

            return builder.ToString();
        }

        public int CountExportableLines(IEnumerable<Invoice> invoices)
        {
            var count = 0;
            foreach (var invoice in invoices)
            {
                foreach (var line in invoice.Lines)
                {
                    if (InvoiceLineNetAmount.IsNonZeroExportable(InvoiceLineNetAmount.FromLine(line)))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static string Csv(string? value, bool neutralizeFormula)
        {
            return CsvFormulaSanitizer.CsvField(value, neutralizeFormula);
        }
    }
}


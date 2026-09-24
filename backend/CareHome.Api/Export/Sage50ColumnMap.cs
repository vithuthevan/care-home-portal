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
                    var netAmount = NetLineAmount(line);
                    if (netAmount == 0m)
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

        public static decimal NetLineAmount(InvoiceLine line)
        {
            var credited = line.CreditNoteLines
                .Where(c => c.CreditNote.Status != CreditNoteStatuses.Void)
                .Sum(c => c.Amount);

            return Money.Round(line.LineAmount + credited);
        }

        private static string Csv(string? value, bool neutralizeFormula)
        {
            return CsvFormulaSanitizer.CsvField(value, neutralizeFormula);
        }
    }
}


using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.MiscCharges;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services
{
    public class MiscChargeImportService(
        CareHomeDbContext dbContext,
        AuditService audit,
        UserAccessService userAccess)
    {
        public async Task<MiscChargePreviewResponse> PreviewAsync(
            int tenantId,
            string fileName,
            Stream csvStream,
            CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(csvStream);
            var content = await reader.ReadToEndAsync(cancellationToken);
            var rawRows = Parse(content);
            return await BuildPreviewAsync(tenantId, fileName, rawRows, cancellationToken);
        }

        public async Task<(MiscChargeImportBatch? Batch, string? Error)> CommitAsync(
            int tenantId,
            MiscChargePreviewResponse preview,
            string? userId,
            CancellationToken cancellationToken = default)
        {
            if (preview.Rows.Count == 0)
            {
                return (null, "There are no rows to import.");
            }

            // Never trust client-supplied ClientId/Amount/IsValid — re-resolve from Raw only.
            var rawRows = preview.Rows.Select(r => r.Raw ?? new RawMiscRow { RowNumber = r.RowNumber, Error = "Missing raw row data." }).ToList();
            var resolved = await BuildPreviewAsync(tenantId, preview.FileName, rawRows, cancellationToken);

            if (resolved.Rows.Any(x => !x.IsValid))
            {
                return (null, "Invalid rows must be corrected before import. Nothing was saved.");
            }

            var valid = resolved.Rows.Where(x => x.IsValid).ToList();
            if (valid.Count == 0)
            {
                return (null, "There are no valid rows to import.");
            }

            // Detect duplicates within the same commit payload.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in valid)
            {
                var key = $"{row.ClientId}|{row.UsedDate:yyyy-MM-dd}|{row.Description}|{row.Amount:0.00}";
                if (!seen.Add(key))
                {
                    return (null, $"Duplicate charge in import: {row.ClientName ?? row.Raw.ClientReference}, {row.UsedDate}, '{row.Description}', {row.Amount:0.00}.");
                }
            }

            var batch = new MiscChargeImportBatch
            {
                TenantId = tenantId,
                FileName = string.IsNullOrWhiteSpace(preview.FileName) ? resolved.FileName : preview.FileName,
                ImportedAt = DateTimeOffset.UtcNow,
                ImportedByUserId = userId,
                TotalRows = resolved.Rows.Count,
                AcceptedRows = valid.Count,
                RejectedRows = 0,
                Status = "Committed"
            };

            foreach (var row in valid)
            {
                batch.Charges.Add(new MiscCharge
                {
                    TenantId = tenantId,
                    ClientId = row.ClientId!.Value,
                    ClientReference = row.Raw.ClientReference,
                    UsedDate = row.UsedDate!.Value,
                    Description = row.Description!,
                    Amount = row.Amount!.Value,
                    NominalCodeId = row.NominalCodeId,
                    NominalCodeValue = row.NominalCode,
                    SourceRowNumber = row.RowNumber,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            dbContext.MiscChargeImportBatches.Add(batch);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return (null, "One or more charges already exist (same resident, date, description and amount). Nothing was saved.");
            }

            await audit.LogAsync(
                "MiscChargeImport",
                batch.Id.ToString(),
                "Import",
                null,
                new { batch.FileName, batch.AcceptedRows },
                $"Imported {batch.AcceptedRows} miscellaneous charges.",
                cancellationToken);

            return (batch, null);
        }

        private async Task<MiscChargePreviewResponse> BuildPreviewAsync(
            int tenantId,
            string fileName,
            List<RawMiscRow> rows,
            CancellationToken cancellationToken)
        {
            var response = new MiscChargePreviewResponse { FileName = fileName };
            var allowedHomes = await userAccess.GetAllowedCareHomeIdsAsync(cancellationToken);

            var clientsQuery = dbContext.Clients.AsNoTracking()
                .Where(x => x.TenantId == tenantId);
            if (allowedHomes is not null)
            {
                clientsQuery = clientsQuery.Where(x => allowedHomes.Contains(x.CareHomeId));
            }

            var clients = await clientsQuery.ToListAsync(cancellationToken);
            var nominals = await dbContext.NominalCodes.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            // Batch-load existing charges for duplicate checks (avoid N+1).
            var existingKeys = await dbContext.MiscCharges.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .Select(x => new { x.ClientId, x.UsedDate, x.Description, x.Amount })
                .ToListAsync(cancellationToken);
            var existingSet = existingKeys
                .Select(x => $"{x.ClientId}|{x.UsedDate:yyyy-MM-dd}|{x.Description}|{x.Amount:0.00}")
                .ToHashSet(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                response.Rows.Add(ResolveRow(row, clients, nominals, existingSet));
            }

            response.ValidCount = response.Rows.Count(x => x.IsValid);
            response.InvalidCount = response.Rows.Count(x => !x.IsValid);
            return response;
        }

        internal static MiscChargePreviewRowDto ResolveRow(
            RawMiscRow row,
            List<Client> clients,
            List<NominalCode> nominals,
            HashSet<string> existingDuplicateKeys)
        {
            var result = new MiscChargePreviewRowDto { RowNumber = row.RowNumber, Raw = row };
            if (row.Error is not null)
            {
                result.IsValid = false;
                result.Error = row.Error;
                return result;
            }

            if (string.IsNullOrWhiteSpace(row.ClientReference))
            {
                result.IsValid = false;
                result.Error = "Resident reference is required.";
                return result;
            }

            var client = clients.FirstOrDefault(c =>
                c.ReferenceNumber.Equals(row.ClientReference, StringComparison.OrdinalIgnoreCase));

            if (client is null)
            {
                result.IsValid = false;
                result.Error = $"Unknown resident reference '{row.ClientReference}'.";
                return result;
            }

            if (!DateOnly.TryParse(row.UsedDate, out var usedDate))
            {
                result.IsValid = false;
                result.Error = "UsedDate must be yyyy-MM-dd.";
                return result;
            }

            if (!decimal.TryParse(row.Amount, out var amount))
            {
                result.IsValid = false;
                result.Error = "Amount must be a decimal number.";
                return result;
            }

            NominalCode? nominal = null;
            if (!string.IsNullOrWhiteSpace(row.NominalCode))
            {
                nominal = nominals.FirstOrDefault(n =>
                    n.Code.Equals(row.NominalCode, StringComparison.OrdinalIgnoreCase));
                if (nominal is null)
                {
                    result.IsValid = false;
                    result.Error = $"Unknown nominal code '{row.NominalCode}'.";
                    return result;
                }
            }

            var description = row.Description.Trim();
            var rounded = Money.Round(amount);
            var dupKey = $"{client.Id}|{usedDate:yyyy-MM-dd}|{description}|{rounded:0.00}";
            if (existingDuplicateKeys.Contains(dupKey))
            {
                result.IsValid = false;
                result.Error = "Duplicate charge: same resident, date, description and amount already imported.";
                return result;
            }

            result.IsValid = true;
            result.ClientId = client.Id;
            result.ClientName = $"{client.FirstName} {client.LastName}";
            result.UsedDate = usedDate;
            result.Description = description;
            result.Amount = rounded;
            result.NominalCodeId = nominal?.Id;
            result.NominalCode = nominal?.Code ?? row.NominalCode;
            return result;
        }

        private static List<RawMiscRow> Parse(string content)
        {
            var lines = content.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var rows = new List<RawMiscRow>();
            if (lines.Length == 0)
            {
                return rows;
            }

            var start = 0;
            if (lines[0].Contains("ClientReference", StringComparison.OrdinalIgnoreCase))
            {
                start = 1;
            }

            for (var i = start; i < lines.Length; i++)
            {
                var parts = SplitCsv(lines[i]);
                var rowNumber = i + 1;
                if (parts.Length < 4)
                {
                    rows.Add(new RawMiscRow { RowNumber = rowNumber, Error = "Expected columns: ClientReference, UsedDate, Description, Amount, NominalCode." });
                    continue;
                }

                rows.Add(new RawMiscRow
                {
                    RowNumber = rowNumber,
                    ClientReference = parts[0].Trim(),
                    UsedDate = parts[1].Trim(),
                    Description = parts[2].Trim(),
                    Amount = parts[3].Trim(),
                    NominalCode = parts.Length > 4 ? parts[4].Trim() : null
                });
            }

            return rows;
        }

        private static string[] SplitCsv(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            var quoted = false;
            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    quoted = !quoted;
                    continue;
                }

                if (ch == ',' && !quoted)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }

    public class RawMiscRow
    {
        public int RowNumber { get; set; }
        public string ClientReference { get; set; } = string.Empty;
        public string UsedDate { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string? NominalCode { get; set; }
        public string? Error { get; set; }
    }
}

using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services
{
    public class ClientIdentifierService(CareHomeDbContext dbContext)
    {
        public async Task<(string ReferenceNumber, string SageId)> ResolveCreateIdentifiersAsync(
            int tenantId,
            int careHomeId,
            string? referenceNumber,
            string? sageId,
            CancellationToken cancellationToken = default)
        {
            var reference = referenceNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(reference))
            {
                reference = await GenerateReferenceNumberAsync(tenantId, careHomeId, cancellationToken);
            }

            var sage = sageId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sage))
            {
                sage = reference.Length <= 20 ? reference : reference[..20];
            }

            return (reference, sage);
        }

        private async Task<string> GenerateReferenceNumberAsync(
            int tenantId,
            int careHomeId,
            CancellationToken cancellationToken)
        {
            var code = await dbContext.CareHomes
                .AsNoTracking()
                .Where(x => x.Id == careHomeId && x.TenantId == tenantId)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(code))
            {
                code = "RES";
            }

            code = code.Trim().ToUpperInvariant();
            if (code.Length > 12)
            {
                code = code[..12];
            }

            for (var attempt = 0; attempt < 200; attempt++)
            {
                var residentCount = await dbContext.Clients.CountAsync(
                    x => x.TenantId == tenantId && x.CareHomeId == careHomeId,
                    cancellationToken);
                var candidate = $"{code}-{(residentCount + 1 + attempt):D3}";
                if (candidate.Length > 20)
                {
                    candidate = candidate[..20];
                }

                var exists = await dbContext.Clients.AnyAsync(
                    x => x.TenantId == tenantId && x.ReferenceNumber == candidate,
                    cancellationToken);
                if (!exists)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique resident reference.");
        }
    }
}

using CareHome.Api.Data;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Billing
{
    public class InvoiceTemplateResolver(CareHomeDbContext dbContext)
    {
        public async Task<InvoiceTemplate?> ResolveAsync(
            int tenantId,
            int invoiceCategoryId,
            int fundingAuthorityId,
            int careHomeId,
            int companyId,
            CancellationToken cancellationToken = default)
        {
            var templates = await dbContext.InvoiceTemplates
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.IsActive && x.InvoiceCategoryId == invoiceCategoryId)
                .ToListAsync(cancellationToken);

            InvoiceTemplate? Match(
                Func<InvoiceTemplate, bool> predicate)
            {
                return templates.FirstOrDefault(predicate);
            }

            return
                Match(x =>
                    x.CareHomeId == careHomeId &&
                    x.FundingAuthorityId == fundingAuthorityId) ??
                Match(x =>
                    x.CareHomeId == null &&
                    x.FundingAuthorityId == fundingAuthorityId) ??
                Match(x =>
                    x.CareHomeId == careHomeId &&
                    x.FundingAuthorityId == null) ??
                Match(x =>
                    x.CompanyId == companyId &&
                    x.CareHomeId == null &&
                    x.FundingAuthorityId == null) ??
                Match(x =>
                    x.CareHomeId == null &&
                    x.FundingAuthorityId == null &&
                    x.CompanyId == null);
        }
    }
}


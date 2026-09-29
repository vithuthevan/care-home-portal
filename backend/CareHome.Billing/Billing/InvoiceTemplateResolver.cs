using CareHome.Api.Data;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Billing
{
    public class InvoiceTemplateResolver(CareHomeDbContext dbContext)
    {
        public async Task<InvoiceTemplate?> ResolveForBillingAsync(
            int tenantId,
            int invoiceCategoryId,
            int fundingAuthorityId,
            int careHomeId,
            int? companyId,
            int? pinnedTemplateId,
            int? runOverrideTemplateId,
            CancellationToken cancellationToken = default)
        {
            if (runOverrideTemplateId is int overrideId)
            {
                var overrideTemplate = await LoadPinnedAsync(
                    tenantId,
                    overrideId,
                    invoiceCategoryId,
                    cancellationToken);
                if (overrideTemplate is not null)
                {
                    return overrideTemplate;
                }
            }

            if (pinnedTemplateId is int pinId)
            {
                var pinned = await LoadPinnedAsync(
                    tenantId,
                    pinId,
                    invoiceCategoryId,
                    cancellationToken);
                if (pinned is not null)
                {
                    return pinned;
                }
            }

            return await ResolveAsync(
                tenantId,
                invoiceCategoryId,
                fundingAuthorityId,
                careHomeId,
                companyId,
                cancellationToken);
        }

        public async Task<InvoiceTemplate?> LoadPinnedAsync(
            int tenantId,
            int templateId,
            int expectedInvoiceCategoryId,
            CancellationToken cancellationToken = default)
        {
            var template = await dbContext.InvoiceTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == templateId
                         && x.TenantId == tenantId
                         && x.IsActive
                         && x.InvoiceCategoryId == expectedInvoiceCategoryId,
                    cancellationToken);

            return template;
        }

        public async Task<InvoiceTemplate?> ResolveAsync(
            int tenantId,
            int invoiceCategoryId,
            int fundingAuthorityId,
            int careHomeId,
            int? companyId,
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

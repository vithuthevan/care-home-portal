using CareHome.Api.Data;
using CareHome.Api.Dtos.Collections;
using CareHome.Api.Models;
using CareHome.Api.Receivables.Contracts;
using CareHome.Api.Receivables.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class CollectionsWorkflowService(
    CareHomeDbContext dbContext,
    IReceivablesService receivables,
    TimeProvider timeProvider)
{
    public async Task<CollectionsDashboardDto> GetDashboardAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.CollectionPolicies.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            policy = new CollectionPolicy
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                Name = "Default",
                IsDefault = true,
                CreatedAt = timeProvider.GetUtcNow(),
                UpdatedAt = timeProvider.GetUtcNow()
            };
            dbContext.CollectionPolicies.Add(policy);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var summary = await receivables.GetTenantSummaryAsync(
            tenantId,
            new ReceivableInvoiceQuery(),
            cancellationToken);

        return new CollectionsDashboardDto
        {
            DueToday = summary.DueThisWeek,
            Overdue = summary.TotalOverdue,
            Overdue30 = summary.Ageing.Days31To60 + summary.Ageing.Days61To90 + summary.Ageing.Days90Plus,
            Overdue60 = summary.Ageing.Days61To90 + summary.Ageing.Days90Plus,
            Overdue90 = summary.Ageing.Days90Plus,
            Policy = MapPolicy(policy)
        };
    }

    public async Task<CollectionPolicyDto> UpdatePolicyAsync(
        int tenantId,
        UpdateCollectionPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.CollectionPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault, cancellationToken);

        if (policy is null)
        {
            policy = new CollectionPolicy
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                Name = "Default",
                IsDefault = true,
                CreatedAt = timeProvider.GetUtcNow()
            };
            dbContext.CollectionPolicies.Add(policy);
        }

        policy.DueReminderDaysBefore = Math.Max(0, request.DueReminderDaysBefore);
        policy.Overdue7Days = request.Overdue7Days;
        policy.Overdue14Days = request.Overdue14Days;
        policy.Overdue30Days = request.Overdue30Days;
        policy.EscalationDays = request.EscalationDays;
        policy.RemindersEnabled = request.RemindersEnabled;
        policy.ReminderEmailSubjectTemplate = string.IsNullOrWhiteSpace(request.ReminderEmailSubjectTemplate)
            ? null
            : request.ReminderEmailSubjectTemplate.Trim();
        policy.ReminderEmailBodyTemplate = string.IsNullOrWhiteSpace(request.ReminderEmailBodyTemplate)
            ? null
            : request.ReminderEmailBodyTemplate.Trim();
        policy.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    private static CollectionPolicyDto MapPolicy(CollectionPolicy policy) => new()
    {
        PublicId = policy.PublicId,
        DueReminderDaysBefore = policy.DueReminderDaysBefore,
        Overdue7Days = policy.Overdue7Days,
        Overdue14Days = policy.Overdue14Days,
        Overdue30Days = policy.Overdue30Days,
        EscalationDays = policy.EscalationDays,
        RemindersEnabled = policy.RemindersEnabled,
        ReminderEmailSubjectTemplate = policy.ReminderEmailSubjectTemplate,
        ReminderEmailBodyTemplate = policy.ReminderEmailBodyTemplate
    };
}

using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CareHome.Api.Services;

public sealed class CollectionsReminderHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectionsReminderOptions> options,
    TimeProvider timeProvider,
    ILogger<CollectionsReminderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ScheduledRemindersEnabled)
        {
            logger.LogInformation("Collections scheduled reminders are disabled (Collections:ScheduledRemindersEnabled=false).");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilNextRunAsync(stoppingToken);
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await RunAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Collections reminder job failed.");
            }
        }
    }

    private async Task RunAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        var reminders = scope.ServiceProvider.GetRequiredService<CollectionReminderService>();

        var tenantIds = await db.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            var result = await reminders.SendDueRemindersAsync(tenantId, cancellationToken);
            if (result.Succeeded + result.Failed > 0)
            {
                logger.LogInformation(
                    "Collections reminders tenant {TenantId}: sent={Sent} failed={Failed} skipped={Skipped}",
                    tenantId,
                    result.Succeeded,
                    result.Failed,
                    result.Skipped);
            }
        }
    }

    private async Task WaitUntilNextRunAsync(CancellationToken cancellationToken)
    {
        var hour = Math.Clamp(options.Value.ScheduledRunHourUtc, 0, 23);
        var now = timeProvider.GetUtcNow();
        var next = new DateTimeOffset(now.Year, now.Month, now.Day, hour, 0, 0, TimeSpan.Zero);
        if (now >= next)
        {
            next = next.AddDays(1);
        }

        var delay = next - now;
        logger.LogDebug("Collections reminder job sleeping for {Delay}.", delay);
        await Task.Delay(delay, cancellationToken);
    }
}

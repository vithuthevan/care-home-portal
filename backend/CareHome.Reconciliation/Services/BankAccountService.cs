using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Reconciliation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Reconciliation.Services;

public sealed class BankAccountService(CareHomeDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<List<BankAccountDto>> ListAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.BankAccounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Name)
            .Select(a => new BankAccountDto
            {
                PublicId = a.PublicId,
                Name = a.Name,
                BankName = a.BankName,
                AccountReference = a.AccountReference,
                Currency = a.Currency,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BankAccountDto> CreateAsync(
        int tenantId,
        CreateBankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Bank account name is required.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "GBP"
            : request.Currency.Trim().ToUpperInvariant();

        var entity = new BankAccount
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            BankName = request.BankName?.Trim(),
            AccountReference = request.AccountReference?.Trim(),
            Currency = currency,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        dbContext.BankAccounts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BankAccountDto
        {
            PublicId = entity.PublicId,
            Name = entity.Name,
            BankName = entity.BankName,
            AccountReference = entity.AccountReference,
            Currency = entity.Currency,
            IsActive = entity.IsActive
        };
    }

    public async Task<BankAccount> RequireAsync(int tenantId, Guid publicId, CancellationToken cancellationToken)
    {
        return await dbContext.BankAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Bank account not found.");
    }
}

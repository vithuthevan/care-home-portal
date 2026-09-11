using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CareHome.Api.Common;

/// <summary>
/// SQL Server application locks scoped to the current database transaction.
/// Call only after <c>BeginTransactionAsync</c>; the lock is released on commit/rollback.
/// </summary>
public static class SqlAppLock
{
    public static Task AcquireExclusiveAsync(
        DatabaseFacade database,
        string resource,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        return database.ExecuteSqlInterpolatedAsync(
            $"""
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = {resource},
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 30000;
            IF @result < 0
                THROW 51000, 'Another conflicting operation is in progress. Retry shortly.', 1;
            """,
            cancellationToken);
    }
}

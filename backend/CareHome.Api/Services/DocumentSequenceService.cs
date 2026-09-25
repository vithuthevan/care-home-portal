using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public class DocumentSequenceService(CareHomeDbContext dbContext) : IDocumentSequence
{
    public async Task<string> NextAsync(
        int tenantId,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var sequence = await dbContext.DocumentSequences
            .FromSqlInterpolated(
                $@"SELECT * FROM DocumentSequences WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                   WHERE TenantId = {tenantId} AND DocumentType = {documentType}")
            .SingleAsync(cancellationToken);

        var value = sequence.NextValue;
        sequence.NextValue = checked(sequence.NextValue + 1);
        await dbContext.SaveChangesAsync(cancellationToken);

        var length = Math.Clamp(sequence.NumberLength, 1, 10);
        return $"{sequence.Prefix}{value.ToString("D" + length)}";
    }
}


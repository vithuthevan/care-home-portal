namespace CareHome.Api.Abstractions;

public interface IDocumentSequence
{
    Task<string> NextAsync(
        int tenantId,
        string documentType,
        CancellationToken cancellationToken = default);
}

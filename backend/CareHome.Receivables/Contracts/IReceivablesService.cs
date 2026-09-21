using CareHome.Api.Receivables.Dtos;

namespace CareHome.Api.Receivables.Contracts;

public interface IReceivablesService
{
    Task<ReceivablesSummaryDto> GetTenantSummaryAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default);

    Task<ReceivablesAgeingDto> GetAgeingAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default);

    Task<(List<ReceivableInvoiceDto> Items, int TotalCount)> ListInvoicesAsync(
        int tenantId,
        ReceivableInvoiceQuery query,
        CancellationToken cancellationToken = default);

    Task<List<FunderReceivableSummaryDto>> ListFunderSummariesAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default);

    Task<CareHomeReceivableSummaryDto?> GetCareHomeSummaryAsync(
        int tenantId,
        int careHomeId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default);

    Task<List<CareHomeReceivableSummaryDto>> ListCareHomeSummariesAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default);
}

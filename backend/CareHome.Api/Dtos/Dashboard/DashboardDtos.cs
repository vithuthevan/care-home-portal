namespace CareHome.Api.Dtos.Dashboard
{
    public class DashboardDto
    {
        public int TotalCareHomes { get; set; }

        public int CurrentClients { get; set; }

        public int AvailableBeds { get; set; }

        public int UpcomingBillingCount { get; set; }

        public int OutstandingInvoices { get; set; }

        public decimal OutstandingAmount { get; set; }

        public int InvoicesGenerated { get; set; }

        public List<OccupancyCardDto> OccupancyByHome { get; set; } = [];

        public List<RecentInvoiceDto> RecentInvoices { get; set; } = [];

        public List<DashboardBillingExceptionDto> BillingExceptions { get; set; } = [];

        public List<UpcomingInvoiceDto> UpcomingInvoices { get; set; } = [];

        public List<string> SetupHints { get; set; } = [];
    }

    public class DashboardBillingExceptionDto
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    public class OccupancyCardDto
    {
        public int CareHomeId { get; set; }

        public Guid PublicId { get; set; }

        public string CareHomeName { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public int Occupied { get; set; }

        public int Available { get; set; }
    }

    public class RecentInvoiceDto
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string CareHomeName { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        public decimal TotalAmount { get; set; }

        /// <summary>Sum of net line amounts (after non-void credits), aligned with Sage export and invoice reports.</summary>
        public decimal NetBilledAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class UpcomingInvoiceDto
    {
        public string CareHomeName { get; set; } = string.Empty;

        public string FundingAuthorityName { get; set; } = string.Empty;

        public string BillingFrequency { get; set; } = string.Empty;
    }

    public class CareHomeDashboardDto
    {
        public int CareHomeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public int Occupied { get; set; }

        public int Available { get; set; }

        public string? ManagerName { get; set; }

        public List<string> CurrentClients { get; set; } = [];

        public List<RecentInvoiceDto> RecentInvoices { get; set; } = [];

        public int OutstandingCount { get; set; }

        public decimal OutstandingAmount { get; set; }
    }
}


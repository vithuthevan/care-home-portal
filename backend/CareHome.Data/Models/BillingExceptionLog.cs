using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class BillingExceptionLog : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public int? ClientId { get; set; }

        public int? CareHomeId { get; set; }

        public int? ClientFundingContractId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Severity { get; set; } = "Error";

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateOnly? PeriodStart { get; set; }

        public DateOnly? PeriodEnd { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public Client? Client { get; set; }

        public CareHomeLocation? CareHome { get; set; }
    }
}


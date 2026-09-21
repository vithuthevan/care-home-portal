using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class MiscCharge : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public int ImportBatchId { get; set; }

        public int ClientId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ClientReference { get; set; } = string.Empty;

        public DateOnly UsedDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public int? NominalCodeId { get; set; }

        [MaxLength(20)]
        public string? NominalCodeValue { get; set; }

        public int SourceRowNumber { get; set; }

        public bool IsInvoiced { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public MiscChargeImportBatch ImportBatch { get; set; } = null!;

        public Client Client { get; set; } = null!;

        public NominalCode? NominalCode { get; set; }
    }
}


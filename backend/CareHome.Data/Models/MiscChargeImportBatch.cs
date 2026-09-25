using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class MiscChargeImportBatch : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        public DateTimeOffset ImportedAt { get; set; }

        [MaxLength(450)]
        public string? ImportedByUserId { get; set; }

        public int TotalRows { get; set; }

        public int AcceptedRows { get; set; }

        public int RejectedRows { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Committed";

        public Tenant Tenant { get; set; } = null!;

        public ICollection<MiscCharge> Charges { get; set; } = new List<MiscCharge>();
    }
}


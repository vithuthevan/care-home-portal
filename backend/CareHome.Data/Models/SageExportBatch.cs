using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class SageExportBatch : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public DateTimeOffset ExportedAt { get; set; }

        [MaxLength(450)]
        public string? ExportedByUserId { get; set; }

        public DateOnly DateFrom { get; set; }

        public DateOnly DateTo { get; set; }

        public int? CompanyId { get; set; }

        public int RecordCount { get; set; }

        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Completed";

        public Tenant Tenant { get; set; } = null!;

        public Company? Company { get; set; }

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}


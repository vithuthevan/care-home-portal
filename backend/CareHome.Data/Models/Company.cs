using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class Company : ITenantOwned
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int TenantId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public Tenant Tenant { get; set; } = null!;

        public ICollection<CareHomeLocation> CareHomes { get; set; }
            = new List<CareHomeLocation>();

        public ICollection<InvoiceTemplate> InvoiceTemplates { get; set; }
            = new List<InvoiceTemplate>();
    }

}
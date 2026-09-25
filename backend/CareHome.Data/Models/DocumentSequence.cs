using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class DocumentSequence : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Prefix { get; set; } = string.Empty;

        public int NumberLength { get; set; } = 4;

        public int NextValue { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}


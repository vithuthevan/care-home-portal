using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class EmailSendLog : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public DateTimeOffset AttemptedAt { get; set; }

        [Required]
        [MaxLength(40)]
        public string DocumentType { get; set; } = string.Empty;

        public int DocumentId { get; set; }

        [MaxLength(300)]
        public string? Recipient { get; set; }

        public bool Success { get; set; }

        public bool Simulated { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}


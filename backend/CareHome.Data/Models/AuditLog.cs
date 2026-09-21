using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class AuditLog : ITenantOwned
    {
        public long Id { get; set; }

        public int TenantId { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        [Required]
        [MaxLength(80)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? EntityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}


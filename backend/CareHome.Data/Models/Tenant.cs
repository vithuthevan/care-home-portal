using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? TradingName { get; set; }

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public TenantSettings Settings { get; set; } = null!;

        public ICollection<Company> Companies { get; set; } = new List<Company>();
    }
}


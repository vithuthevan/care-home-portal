using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class CareHomeLocation : ITenantOwned
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int TenantId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int BedCapacity { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(150)]
        public string? ManagerName { get; set; }

        [MaxLength(30)]
        public string? ManagerPhone { get; set; }

        [MaxLength(150)]
        public string? ManagerEmail { get; set; }

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        public string? BankDetails { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(20)]
        public string? PortalAccentTheme { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public Company? Company { get; set; }

        public ICollection<Client> Clients { get; set; }
            = new List<Client>();

        public ICollection<UserCareHomeAccess> UserAccess { get; set; }
            = new List<UserCareHomeAccess>();
    }

}
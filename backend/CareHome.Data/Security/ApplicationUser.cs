using CareHome.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace CareHome.Api.Security
{
    public class ApplicationUser : IdentityUser
    {
        public int? TenantId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool MustChangePassword { get; set; }

        public Tenant? Tenant { get; set; }

        public ICollection<UserCareHomeAccess> CareHomeAccess { get; set; }
            = new List<UserCareHomeAccess>();
    }
}


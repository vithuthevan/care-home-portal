using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class UserCareHomeAccess
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int CareHomeId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public CareHomeLocation CareHome { get; set; } = null!;
    }
}


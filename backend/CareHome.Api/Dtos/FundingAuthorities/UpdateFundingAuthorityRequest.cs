using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;

namespace CareHome.Api.Dtos.FundingAuthorities
{
    public class UpdateFundingAuthorityRequest
    {
        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? ContactName { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(150)]
        [OptionalEmailAddress]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [Required]
        [MaxLength(30)]
        public string BillingFrequency { get; set; } = string.Empty;

        public int? BillingIntervalDays { get; set; }

        public bool IsActive { get; set; }
    }
}

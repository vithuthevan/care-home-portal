namespace CareHome.Api.Dtos.FundingAuthorities
{
    public class FundingAuthorityDto
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string? ContactName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string BillingFrequency { get; set; } = string.Empty;

        public int? BillingIntervalDays { get; set; }

        public bool IsActive { get; set; }
    }
}

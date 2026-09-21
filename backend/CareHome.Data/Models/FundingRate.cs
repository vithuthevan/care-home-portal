using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Models
{
    public class FundingRate
    {
        public int Id { get; set; }

        public int ClientFundingContractId { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        [Required]
        [MaxLength(30)]
        public string Frequency { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ClientFundingContract ClientFundingContract { get; set; } = null!;
    }
}


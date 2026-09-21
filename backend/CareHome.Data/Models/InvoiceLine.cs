using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Models
{
    public class InvoiceLine
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public int ClientId { get; set; }

        public int ClientFundingContractId { get; set; }

        public int? FundingRateId { get; set; }

        public int? MiscChargeId { get; set; }

        [Required]
        [MaxLength(20)]
        public string SnapshotClientReferenceNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SnapshotSageId { get; set; } = string.Empty;

        [Required]
        [MaxLength(220)]
        public string SnapshotClientName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotCareHomeName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotCompanyName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string SnapshotFundingAuthorityCode { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotFundingAuthorityName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string SnapshotInvoiceCategoryCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SnapshotInvoiceCategoryName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string SnapshotNominalCode { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotNominalCodeName { get; set; } = string.Empty;

        public DateOnly ServicePeriodStart { get; set; }

        public DateOnly ServicePeriodEnd { get; set; }

        [Required]
        [MaxLength(30)]
        public string RateFrequency { get; set; } = string.Empty;

        public decimal RateAmount { get; set; }

        public int EligibleDays { get; set; }

        public decimal LineAmount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public Invoice Invoice { get; set; } = null!;

        public Client Client { get; set; } = null!;

        public ClientFundingContract ClientFundingContract { get; set; } = null!;

        public FundingRate? FundingRate { get; set; }

        public MiscCharge? MiscCharge { get; set; }

        public ICollection<CreditNoteLine> CreditNoteLines { get; set; }
            = new List<CreditNoteLine>();
    }
}


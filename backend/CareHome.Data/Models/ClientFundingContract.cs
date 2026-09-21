using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class ClientFundingContract : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        public int ClientId { get; set; }

        public int FundingAuthorityId { get; set; }

        public int InvoiceCategoryId { get; set; }

        public int NominalCodeId { get; set; }

        public int? InvoiceTemplateId { get; set; }

        public DateOnly ContractStartDate { get; set; }

        public DateOnly? ContractEndDate { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public Client Client { get; set; } = null!;

        public FundingAuthority FundingAuthority { get; set; } = null!;

        public InvoiceCategory InvoiceCategory { get; set; } = null!;

        public NominalCode NominalCode { get; set; } = null!;

        public InvoiceTemplate? InvoiceTemplate { get; set; }

        public ICollection<FundingRate> Rates { get; set; } = new List<FundingRate>();
    }
}


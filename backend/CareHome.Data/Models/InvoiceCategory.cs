using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class InvoiceCategory : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>PerFunder = one invoice for the funder and home. PerResident = one invoice per resident.</summary>
        [Required]
        [MaxLength(30)]
        public string GroupingMode { get; set; } = InvoiceGroupingModes.PerFunder;

        public bool IsActive { get; set; } = true;

        public Tenant Tenant { get; set; } = null!;

        public ICollection<ClientFundingContract> FundingContracts { get; set; }
            = new List<ClientFundingContract>();
    }
}

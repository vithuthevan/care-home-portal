using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class Client : ITenantOwned
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int TenantId { get; set; }

        public int CareHomeId { get; set; }

        [Required]
        [MaxLength(20)]
        public string SageId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Title { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        [Required]
        [MaxLength(30)]
        public string CareType { get; set; } = string.Empty;

        // Occupancy lifecycle: Current, Left, or Deceased.
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Current";

        public DateOnly AdmissionDate { get; set; }

        public DateOnly? DischargeDate { get; set; }

        [MaxLength(100)]
        public string? DischargeReason { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Visibility/history only; not an occupancy status.
        public bool IsArchived { get; set; } = false;

        public Tenant Tenant { get; set; } = null!;

        public CareHomeLocation CareHome { get; set; } = null!;

        public ICollection<ClientFundingContract> FundingContracts { get; set; }
            = new List<ClientFundingContract>();
    }

}
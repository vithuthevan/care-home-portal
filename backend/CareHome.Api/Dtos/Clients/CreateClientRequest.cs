using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Dtos.Clients
{
    public class CreateClientRequest
    {
        [Range(1, int.MaxValue)]
        public int CareHomeId { get; set; }

        [MaxLength(20)]
        public string? SageId { get; set; }

        [MaxLength(20)]
        public string? ReferenceNumber { get; set; }

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

        public DateOnly AdmissionDate { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}

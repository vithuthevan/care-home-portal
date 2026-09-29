using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;

namespace CareHome.Api.Dtos.Clients;

public class UpdateClientRequest
{
    [Range(1, int.MaxValue)]
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

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Current";

    public DateOnly AdmissionDate { get; set; }

    public DateOnly? DischargeDate { get; set; }

    [MaxLength(100)]
    public string? DischargeReason { get; set; }

    [MaxLength(150)]
    [OptionalEmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsArchived { get; set; }

    [MaxLength(150)]
    public string? GuardianName { get; set; }

    [MaxLength(80)]
    public string? GuardianRelationship { get; set; }

    [MaxLength(150)]
    [OptionalEmailAddress]
    public string? GuardianEmail { get; set; }

    [MaxLength(30)]
    public string? GuardianPhone { get; set; }

    [MaxLength(300)]
    public string? GuardianAddress { get; set; }
}

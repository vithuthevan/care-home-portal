namespace CareHome.Api.Dtos.Clients;

public class ClientDto
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public int CareHomeId { get; set; }

    public Guid CareHomePublicId { get; set; }

    public int? CompanyId { get; set; }

    public Guid? CompanyPublicId { get; set; }

    public string CareHomeName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string SageId { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string CareType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly AdmissionDate { get; set; }

    public DateOnly? DischargeDate { get; set; }

    public string? DischargeReason { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public bool IsArchived { get; set; }

    public string? PrimaryFundingAuthorityName { get; set; }

    public string? GuardianName { get; set; }

    public string? GuardianRelationship { get; set; }

    public string? GuardianEmail { get; set; }

    public string? GuardianPhone { get; set; }

    public string? GuardianAddress { get; set; }
}

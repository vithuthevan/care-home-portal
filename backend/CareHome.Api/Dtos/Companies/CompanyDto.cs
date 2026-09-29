namespace CareHome.Api.Dtos.Companies;

public class CompanyDto
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? LogoPath { get; set; }

    public bool IsActive { get; set; }

    public int CareHomeCount { get; set; }

    public int ActiveCareHomeCount { get; set; }

    public int ResidentCount { get; set; }
}

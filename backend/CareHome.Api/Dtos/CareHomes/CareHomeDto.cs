namespace CareHome.Api.Dtos.CareHomes;

public class CareHomeDto
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public int? CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int BedCapacity { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? ManagerName { get; set; }

    public string? ManagerPhone { get; set; }

    public string? ManagerEmail { get; set; }

    public string? LogoPath { get; set; }

    public string? BankDetails { get; set; }

    public bool IsActive { get; set; }

    public string? PortalAccentTheme { get; set; }
}

using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;

namespace CareHome.Api.Dtos.CareHomes;

public class CreateCareHomeRequest
{
    public int? CompanyId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int BedCapacity { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [OptionalEmailAddress]
    public string? Email { get; set; }

    [MaxLength(150)]
    public string? ManagerName { get; set; }

    [MaxLength(30)]
    public string? ManagerPhone { get; set; }

    [MaxLength(150)]
    [OptionalEmailAddress]
    public string? ManagerEmail { get; set; }

    public string? BankDetails { get; set; }
}

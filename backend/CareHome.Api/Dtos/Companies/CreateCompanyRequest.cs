using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;

namespace CareHome.Api.Dtos.Companies;

public class CreateCompanyRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [OptionalEmailAddress]
    public string? Email { get; set; }
}
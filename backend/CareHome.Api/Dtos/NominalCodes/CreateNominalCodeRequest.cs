using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Dtos.NominalCodes;

public class CreateNominalCodeRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

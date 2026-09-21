using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Dtos.CareHomes;

public class UpdateCareHomePortalAppearanceRequest
{
    [Required]
    [MaxLength(20)]
    public string PortalAccentTheme { get; set; } = string.Empty;
}

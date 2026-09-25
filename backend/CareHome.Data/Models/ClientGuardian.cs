using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class ClientGuardian : ITenantOwned
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int ClientId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Relationship { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public Client Client { get; set; } = null!;
}

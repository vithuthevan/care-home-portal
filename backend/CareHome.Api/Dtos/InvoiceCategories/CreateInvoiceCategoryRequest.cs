using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Dtos.InvoiceCategories;

public class CreateInvoiceCategoryRequest
{
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(30)]
    public string? GroupingMode { get; set; }
}

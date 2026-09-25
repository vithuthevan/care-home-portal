using CareHome.Api.Dtos.Common;

namespace CareHome.Api.Dtos.InvoiceCategories
{
    public class InvoiceCategoryDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        /// <summary>SystemDefault = provisioned with the tenant; Organisation = created by users.</summary>
        public string ConfigurationSource { get; set; } = "Organisation";

        public MasterDataUsageDto? Usage { get; set; }
    }
}

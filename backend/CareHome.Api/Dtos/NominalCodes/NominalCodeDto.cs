using CareHome.Api.Dtos.Common;

namespace CareHome.Api.Dtos.NominalCodes
{
    public class NominalCodeDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Organisation-defined nominal; not a platform-wide accounting standard.</summary>
        public string ConfigurationSource { get; set; } = "Organisation";

        public MasterDataUsageDto? Usage { get; set; }
    }
}

namespace CareHome.Api.Dtos.Companies
{
    public class CompanyDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int CareHomeCount { get; set; }

        public int ActiveCareHomeCount { get; set; }

        public int ResidentCount { get; set; }
    }
}

using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Documents;

public static class QuestPdfLicenseConfigurator
{
    public static void Configure(IConfiguration configuration)
    {
        var license = configuration["QuestPdf:LicenseType"]?.Trim();
        QuestPDF.Settings.License = string.Equals(license, "Professional", StringComparison.OrdinalIgnoreCase)
            ? LicenseType.Professional
            : LicenseType.Community;
    }
}

using Xunit;

namespace CareHome.Api.Tests.Integration;

public sealed class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        if (!IntegrationTestDatabase.IsAvailable)
        {
            Skip = "SQL Server is not available. Set CAREHOME_TEST_SQL or install LocalDB.";
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <summary>
    /// InitialCreate still HasData-inserts Sovereign Care Homes and Care Pro.
    /// That historical file is not edited. This forward-only step removes those
    /// companies only when they have no operational children, so existing
    /// customer databases that already use them are left unchanged.
    /// </summary>
    public partial class RemoveUnusedHistoricalCustomerSeedCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'Companies', 'U') IS NOT NULL
                BEGIN
                    DECLARE @sql nvarchar(max) = N'
                        DELETE FROM Companies
                        WHERE TenantId = 1
                          AND Name IN (N''Sovereign Care Homes'', N''Care Pro'')
                          AND NOT EXISTS (
                              SELECT 1 FROM CareHomes
                              WHERE CareHomes.CompanyId = Companies.Id)';

                    IF OBJECT_ID(N'Invoices', 'U') IS NOT NULL
                        SET @sql = @sql + N'
                          AND NOT EXISTS (
                              SELECT 1 FROM Invoices
                              WHERE Invoices.CompanyId = Companies.Id)';

                    IF OBJECT_ID(N'InvoiceTemplates', 'U') IS NOT NULL
                        SET @sql = @sql + N'
                          AND NOT EXISTS (
                              SELECT 1 FROM InvoiceTemplates
                              WHERE InvoiceTemplates.CompanyId = Companies.Id)';

                    IF OBJECT_ID(N'SageExportBatches', 'U') IS NOT NULL
                        SET @sql = @sql + N'
                          AND NOT EXISTS (
                              SELECT 1 FROM SageExportBatches
                              WHERE SageExportBatches.CompanyId = Companies.Id)';

                    EXEC sp_executesql @sql;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. Recreating customer-specific seed names would
            // reintroduce them to generic databases. Existing customer rows that
            // were preserved are not deleted by Up, so Down has nothing to restore.
        }
    }
}

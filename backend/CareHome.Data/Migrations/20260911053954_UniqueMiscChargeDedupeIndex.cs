using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueMiscChargeDedupeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM MiscCharges
                    GROUP BY TenantId, ClientId, UsedDate, Description, Amount
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 50001, 'Cannot create unique MiscCharges dedupe index: duplicate (TenantId, ClientId, UsedDate, Description, Amount) rows already exist. Resolve duplicates then re-run the migration.', 1;
                END
                """);

            migrationBuilder.DropIndex(
                name: "IX_MiscCharges_ClientId_UsedDate_Description_Amount",
                table: "MiscCharges");

            migrationBuilder.CreateIndex(
                name: "IX_MiscCharges_TenantId_ClientId_UsedDate_Description_Amount",
                table: "MiscCharges",
                columns: new[] { "TenantId", "ClientId", "UsedDate", "Description", "Amount" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MiscCharges_TenantId_ClientId_UsedDate_Description_Amount",
                table: "MiscCharges");

            migrationBuilder.CreateIndex(
                name: "IX_MiscCharges_ClientId_UsedDate_Description_Amount",
                table: "MiscCharges",
                columns: new[] { "ClientId", "UsedDate", "Description", "Amount" });
        }
    }
}

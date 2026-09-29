using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    [DbContext(typeof(CareHomeDbContext))]
    [Migration("20260925150000_AddOrganisationBillingPolicy")]
    public partial class AddOrganisationBillingPolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingPeriodMode",
                table: "TenantSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<bool>(
                name: "AllowPrivatePayer",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowGuardian",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FinanceModuleEnabled",
                table: "TenantSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GroupingMode",
                table: "InvoiceCategories",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PerFunder");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CycleAnchorDate",
                table: "FundingAuthorities",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientFundingContractId",
                table: "InvoiceLines",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "ClientGuardians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientGuardians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientGuardians_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientGuardians_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientGuardians_ClientId",
                table: "ClientGuardians",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientGuardians_TenantId",
                table: "ClientGuardians",
                column: "TenantId");

            migrationBuilder.Sql(
                """
                UPDATE InvoiceCategories
                SET GroupingMode = 'PerResident'
                WHERE Code IN ('RENT', 'MISC');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClientGuardians");

            migrationBuilder.AlterColumn<int>(
                name: "ClientFundingContractId",
                table: "InvoiceLines",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "CycleAnchorDate", table: "FundingAuthorities");
            migrationBuilder.DropColumn(name: "GroupingMode", table: "InvoiceCategories");
            migrationBuilder.DropColumn(name: "FinanceModuleEnabled", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "ShowGuardian", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "AllowPrivatePayer", table: "TenantSettings");
            migrationBuilder.DropColumn(name: "BillingPeriodMode", table: "TenantSettings");
        }
    }
}

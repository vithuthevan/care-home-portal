using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    [DbContext(typeof(CareHomeDbContext))]
    [Migration("20260926120000_AddCollectionReminderEmailAndPolicyTemplates")]
    public partial class AddCollectionReminderEmailAndPolicyTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RemindersEnabled",
                table: "CollectionPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReminderEmailBodyTemplate",
                table: "CollectionPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReminderEmailSubjectTemplate",
                table: "CollectionPolicies",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollectionReminderLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ReminderStage = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionReminderLogs_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionReminderLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReminderLogs_TenantId_InvoiceId_ReminderStage",
                table: "CollectionReminderLogs",
                columns: new[] { "TenantId", "InvoiceId", "ReminderStage" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CollectionReminderLogs");

            migrationBuilder.DropColumn(name: "RemindersEnabled", table: "CollectionPolicies");
            migrationBuilder.DropColumn(name: "ReminderEmailBodyTemplate", table: "CollectionPolicies");
            migrationBuilder.DropColumn(name: "ReminderEmailSubjectTemplate", table: "CollectionPolicies");
        }
    }
}

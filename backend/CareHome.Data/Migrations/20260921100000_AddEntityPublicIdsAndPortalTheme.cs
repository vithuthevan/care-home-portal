using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityPublicIdsAndPortalTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "CareHomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<string>(
                name: "PortalAccentTheme",
                table: "CareHomes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId_PublicId",
                table: "Companies",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareHomes_TenantId_PublicId",
                table: "CareHomes",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId_PublicId",
                table: "Clients",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_PublicId",
                table: "Invoices",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Invoices_TenantId_PublicId", table: "Invoices");
            migrationBuilder.DropIndex(name: "IX_Clients_TenantId_PublicId", table: "Clients");
            migrationBuilder.DropIndex(name: "IX_CareHomes_TenantId_PublicId", table: "CareHomes");
            migrationBuilder.DropIndex(name: "IX_Companies_TenantId_PublicId", table: "Companies");

            migrationBuilder.DropColumn(name: "PublicId", table: "Invoices");
            migrationBuilder.DropColumn(name: "PublicId", table: "Clients");
            migrationBuilder.DropColumn(name: "PortalAccentTheme", table: "CareHomes");
            migrationBuilder.DropColumn(name: "PublicId", table: "CareHomes");
            migrationBuilder.DropColumn(name: "PublicId", table: "Companies");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingAuthorityPublicIdAndInvoiceLineAmountBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountBasis",
                table: "InvoiceLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "FundingAuthorities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.CreateIndex(
                name: "IX_FundingAuthorities_TenantId_PublicId",
                table: "FundingAuthorities",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundingAuthorities_TenantId_PublicId",
                table: "FundingAuthorities");

            migrationBuilder.DropColumn(
                name: "AmountBasis",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "FundingAuthorities");
        }
    }
}

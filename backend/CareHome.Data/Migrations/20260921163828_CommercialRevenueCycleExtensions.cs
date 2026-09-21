using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class CommercialRevenueCycleExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankCsvMappingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    BankAccountId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MappingJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCsvMappingTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankCsvMappingTemplates_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankCsvMappingTemplates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CollectionPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    DueReminderDaysBefore = table.Column<int>(type: "int", nullable: false),
                    Overdue7Days = table.Column<int>(type: "int", nullable: false),
                    Overdue14Days = table.Column<int>(type: "int", nullable: false),
                    Overdue30Days = table.Column<int>(type: "int", nullable: false),
                    EscalationDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionPolicies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundingContractRenewals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    CurrentEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrentRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposedRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NextActionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AgreedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingContractRenewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingContractRenewals_ClientFundingContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "ClientFundingContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundingContractRenewals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RemittanceBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FundingAuthorityId = table.Column<int>(type: "int", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceFormat = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemittanceBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemittanceBatches_FundingAuthorities_FundingAuthorityId",
                        column: x => x.FundingAuthorityId,
                        principalTable: "FundingAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemittanceBatches_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemittanceBatches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevenueAssuranceFindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CareHomeId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    FundingContractId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    RuleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpectedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EstimatedImpact = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueAssuranceFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevenueAssuranceFindings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RemittanceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RemittanceBatchId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InvoiceReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResidentReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeductionReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MatchedInvoiceId = table.Column<int>(type: "int", nullable: true),
                    MatchConfidence = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemittanceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemittanceLines_Invoices_MatchedInvoiceId",
                        column: x => x.MatchedInvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemittanceLines_RemittanceBatches_RemittanceBatchId",
                        column: x => x.RemittanceBatchId,
                        principalTable: "RemittanceBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDisputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    FundingAuthorityId = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisputedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OpenedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ResolvedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RemittanceLineId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceDisputes_FundingAuthorities_FundingAuthorityId",
                        column: x => x.FundingAuthorityId,
                        principalTable: "FundingAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceDisputes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceDisputes_RemittanceLines_RemittanceLineId",
                        column: x => x.RemittanceLineId,
                        principalTable: "RemittanceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceDisputes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisputeMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDisputeId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeMessages_InvoiceDisputes_InvoiceDisputeId",
                        column: x => x.InvoiceDisputeId,
                        principalTable: "InvoiceDisputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisputeMessages_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankCsvMappingTemplates_BankAccountId",
                table: "BankCsvMappingTemplates",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankCsvMappingTemplates_TenantId_Name",
                table: "BankCsvMappingTemplates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_BankCsvMappingTemplates_TenantId_PublicId",
                table: "BankCsvMappingTemplates",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPolicies_TenantId_IsDefault",
                table: "CollectionPolicies",
                columns: new[] { "TenantId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPolicies_TenantId_PublicId",
                table: "CollectionPolicies",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_InvoiceDisputeId",
                table: "DisputeMessages",
                column: "InvoiceDisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_TenantId_PublicId",
                table: "DisputeMessages",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingContractRenewals_ContractId",
                table: "FundingContractRenewals",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingContractRenewals_TenantId_PublicId",
                table: "FundingContractRenewals",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingContractRenewals_TenantId_Status",
                table: "FundingContractRenewals",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDisputes_FundingAuthorityId",
                table: "InvoiceDisputes",
                column: "FundingAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDisputes_InvoiceId",
                table: "InvoiceDisputes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDisputes_RemittanceLineId",
                table: "InvoiceDisputes",
                column: "RemittanceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDisputes_TenantId_PublicId",
                table: "InvoiceDisputes",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDisputes_TenantId_Status",
                table: "InvoiceDisputes",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceBatches_FundingAuthorityId",
                table: "RemittanceBatches",
                column: "FundingAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceBatches_PaymentId",
                table: "RemittanceBatches",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceBatches_TenantId_ContentChecksum",
                table: "RemittanceBatches",
                columns: new[] { "TenantId", "ContentChecksum" },
                filter: "[ContentChecksum] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceBatches_TenantId_PublicId",
                table: "RemittanceBatches",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceBatches_TenantId_Status",
                table: "RemittanceBatches",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceLines_MatchedInvoiceId",
                table: "RemittanceLines",
                column: "MatchedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceLines_RemittanceBatchId_LineNumber",
                table: "RemittanceLines",
                columns: new[] { "RemittanceBatchId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceLines_TenantId_PublicId",
                table: "RemittanceLines",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevenueAssuranceFindings_TenantId_PublicId",
                table: "RevenueAssuranceFindings",
                columns: new[] { "TenantId", "PublicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevenueAssuranceFindings_TenantId_Status_RuleCode",
                table: "RevenueAssuranceFindings",
                columns: new[] { "TenantId", "Status", "RuleCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankCsvMappingTemplates");

            migrationBuilder.DropTable(
                name: "CollectionPolicies");

            migrationBuilder.DropTable(
                name: "DisputeMessages");

            migrationBuilder.DropTable(
                name: "FundingContractRenewals");

            migrationBuilder.DropTable(
                name: "RevenueAssuranceFindings");

            migrationBuilder.DropTable(
                name: "InvoiceDisputes");

            migrationBuilder.DropTable(
                name: "RemittanceLines");

            migrationBuilder.DropTable(
                name: "RemittanceBatches");
        }
    }
}

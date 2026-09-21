using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#nullable disable

#pragma warning disable CA1814

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    /// <inheritdoc />
    public partial class AddOperationalDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "CareHomes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NextValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSendLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Simulated = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSendLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MiscChargeImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ImportedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    AcceptedRows = table.Column<int>(type: "int", nullable: false),
                    RejectedRows = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiscChargeImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SageExportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExportedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SageExportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SageExportBatches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    InvoiceCategoryId = table.Column<int>(type: "int", nullable: false),
                    FundingAuthorityId = table.Column<int>(type: "int", nullable: true),
                    CareHomeId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    AuthorityLogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyLogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HeaderText1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HeaderText2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FooterText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BankAccountName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactJobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailSubjectTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EmailBodyTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceTemplates_InvoiceCategories_InvoiceCategoryId",
                        column: x => x.InvoiceCategoryId,
                        principalTable: "InvoiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceTemplates_FundingAuthorities_FundingAuthorityId",
                        column: x => x.FundingAuthorityId,
                        principalTable: "FundingAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceTemplates_CareHomes_CareHomeId",
                        column: x => x.CareHomeId,
                        principalTable: "CareHomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientFundingContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    FundingAuthorityId = table.Column<int>(type: "int", nullable: false),
                    InvoiceCategoryId = table.Column<int>(type: "int", nullable: false),
                    NominalCodeId = table.Column<int>(type: "int", nullable: false),
                    InvoiceTemplateId = table.Column<int>(type: "int", nullable: true),
                    ContractStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientFundingContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientFundingContracts_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientFundingContracts_FundingAuthorities_FundingAuthorityId",
                        column: x => x.FundingAuthorityId,
                        principalTable: "FundingAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientFundingContracts_InvoiceCategories_InvoiceCategoryId",
                        column: x => x.InvoiceCategoryId,
                        principalTable: "InvoiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientFundingContracts_NominalCodes_NominalCodeId",
                        column: x => x.NominalCodeId,
                        principalTable: "NominalCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientFundingContracts_InvoiceTemplates_InvoiceTemplateId",
                        column: x => x.InvoiceTemplateId,
                        principalTable: "InvoiceTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundingRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientFundingContractId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingRates_ClientFundingContracts_ClientFundingContractId",
                        column: x => x.ClientFundingContractId,
                        principalTable: "ClientFundingContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MiscCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ClientReference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NominalCodeId = table.Column<int>(type: "int", nullable: true),
                    NominalCodeValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    IsInvoiced = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiscCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiscCharges_MiscChargeImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "MiscChargeImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MiscCharges_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MiscCharges_NominalCodes_NominalCodeId",
                        column: x => x.NominalCodeId,
                        principalTable: "NominalCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CareHomeId = table.Column<int>(type: "int", nullable: false),
                    FundingAuthorityId = table.Column<int>(type: "int", nullable: false),
                    InvoiceCategoryId = table.Column<int>(type: "int", nullable: false),
                    InvoiceTemplateId = table.Column<int>(type: "int", nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SageExportBatchId = table.Column<int>(type: "int", nullable: true),
                    SageExportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SnapshotCompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SnapshotCareHomeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SnapshotCareHomeCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotFundingAuthorityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SnapshotFundingAuthorityCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotInvoiceCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SnapshotInvoiceCategoryCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotTemplateName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SnapshotHeaderText1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SnapshotHeaderText2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SnapshotFooterText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SnapshotBankAccountName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SnapshotSortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SnapshotAccountNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SnapshotContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SnapshotContactJobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SnapshotContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SnapshotContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_CareHomes_CareHomeId",
                        column: x => x.CareHomeId,
                        principalTable: "CareHomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_FundingAuthorities_FundingAuthorityId",
                        column: x => x.FundingAuthorityId,
                        principalTable: "FundingAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceCategories_InvoiceCategoryId",
                        column: x => x.InvoiceCategoryId,
                        principalTable: "InvoiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceTemplates_InvoiceTemplateId",
                        column: x => x.InvoiceTemplateId,
                        principalTable: "InvoiceTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_SageExportBatches_SageExportBatchId",
                        column: x => x.SageExportBatchId,
                        principalTable: "SageExportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    ClientFundingContractId = table.Column<int>(type: "int", nullable: false),
                    FundingRateId = table.Column<int>(type: "int", nullable: true),
                    MiscChargeId = table.Column<int>(type: "int", nullable: true),
                    SnapshotClientReferenceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SnapshotSageId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SnapshotClientName = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    SnapshotCareHomeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SnapshotCompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SnapshotFundingAuthorityCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotFundingAuthorityName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SnapshotInvoiceCategoryCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotInvoiceCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SnapshotNominalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SnapshotNominalCodeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ServicePeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ServicePeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    RateFrequency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RateAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EligibleDays = table.Column<int>(type: "int", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_ClientFundingContracts_ClientFundingContractId",
                        column: x => x.ClientFundingContractId,
                        principalTable: "ClientFundingContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_FundingRates_FundingRateId",
                        column: x => x.FundingRateId,
                        principalTable: "FundingRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_MiscCharges_MiscChargeId",
                        column: x => x.MiscChargeId,
                        principalTable: "MiscCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    CreditNoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditNoteId = table.Column<int>(type: "int", nullable: false),
                    InvoiceLineId = table.Column<int>(type: "int", nullable: false),
                    ServicePeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ServicePeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNoteLines_CreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "CreditNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNoteLines_InvoiceLines_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalTable: "InvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingExceptionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    CareHomeId = table.Column<int>(type: "int", nullable: true),
                    ClientFundingContractId = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingExceptionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingExceptionLogs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingExceptionLogs_CareHomes_CareHomeId",
                        column: x => x.CareHomeId,
                        principalTable: "CareHomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserCareHomeAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CareHomeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCareHomeAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCareHomeAccess_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCareHomeAccess_CareHomes_CareHomeId",
                        column: x => x.CareHomeId,
                        principalTable: "CareHomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "RoleNameIndex", table: "AspNetRoles", column: "NormalizedName", unique: true, filter: "[NormalizedName] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "EmailIndex", table: "AspNetUsers", column: "NormalizedEmail");
            migrationBuilder.CreateIndex(name: "UserNameIndex", table: "AspNetUsers", column: "NormalizedUserName", unique: true, filter: "[NormalizedUserName] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_AspNetRoleClaims_RoleId", table: "AspNetRoleClaims", column: "RoleId");
            migrationBuilder.CreateIndex(name: "IX_AspNetUserClaims_UserId", table: "AspNetUserClaims", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_AspNetUserLogins_UserId", table: "AspNetUserLogins", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_AspNetUserRoles_RoleId", table: "AspNetUserRoles", column: "RoleId");
            migrationBuilder.CreateIndex(name: "IX_DocumentSequences_Name", table: "DocumentSequences", column: "Name", unique: true);
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_LoggedAt", table: "AuditLogs", column: "LoggedAt");
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_EntityType_EntityId", table: "AuditLogs", columns: new[] { "EntityType", "EntityId" });
            migrationBuilder.CreateIndex(name: "IX_EmailSendLogs_AttemptedAt", table: "EmailSendLogs", column: "AttemptedAt");
            migrationBuilder.CreateIndex(name: "IX_InvoiceTemplates_InvoiceCategoryId", table: "InvoiceTemplates", column: "InvoiceCategoryId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceTemplates_FundingAuthorityId", table: "InvoiceTemplates", column: "FundingAuthorityId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceTemplates_CareHomeId", table: "InvoiceTemplates", column: "CareHomeId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceTemplates_CompanyId", table: "InvoiceTemplates", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_ClientFundingContracts_ClientId", table: "ClientFundingContracts", column: "ClientId");
            migrationBuilder.CreateIndex(name: "IX_ClientFundingContracts_FundingAuthorityId", table: "ClientFundingContracts", column: "FundingAuthorityId");
            migrationBuilder.CreateIndex(name: "IX_ClientFundingContracts_InvoiceCategoryId", table: "ClientFundingContracts", column: "InvoiceCategoryId");
            migrationBuilder.CreateIndex(name: "IX_ClientFundingContracts_NominalCodeId", table: "ClientFundingContracts", column: "NominalCodeId");
            migrationBuilder.CreateIndex(name: "IX_ClientFundingContracts_InvoiceTemplateId", table: "ClientFundingContracts", column: "InvoiceTemplateId");
            migrationBuilder.CreateIndex(name: "IX_FundingRates_ClientFundingContractId", table: "FundingRates", column: "ClientFundingContractId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_InvoiceNumber", table: "Invoices", column: "InvoiceNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Invoices_CareHomeId", table: "Invoices", column: "CareHomeId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_FundingAuthorityId", table: "Invoices", column: "FundingAuthorityId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_InvoiceCategoryId", table: "Invoices", column: "InvoiceCategoryId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_InvoiceDate_Status", table: "Invoices", columns: new[] { "InvoiceDate", "Status" });
            migrationBuilder.CreateIndex(name: "IX_Invoices_CompanyId", table: "Invoices", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_InvoiceTemplateId", table: "Invoices", column: "InvoiceTemplateId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_SageExportBatchId", table: "Invoices", column: "SageExportBatchId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_InvoiceId", table: "InvoiceLines", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_ClientId_ClientFundingContractId_ServicePeriodStart_ServicePeriodEnd", table: "InvoiceLines", columns: new[] { "ClientId", "ClientFundingContractId", "ServicePeriodStart", "ServicePeriodEnd" });
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_ClientFundingContractId", table: "InvoiceLines", column: "ClientFundingContractId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_FundingRateId", table: "InvoiceLines", column: "FundingRateId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_MiscChargeId", table: "InvoiceLines", column: "MiscChargeId");
            migrationBuilder.CreateIndex(name: "IX_CreditNotes_CreditNoteNumber", table: "CreditNotes", column: "CreditNoteNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_CreditNotes_InvoiceId", table: "CreditNotes", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_CreditNoteLines_CreditNoteId", table: "CreditNoteLines", column: "CreditNoteId");
            migrationBuilder.CreateIndex(name: "IX_CreditNoteLines_InvoiceLineId", table: "CreditNoteLines", column: "InvoiceLineId");
            migrationBuilder.CreateIndex(name: "IX_MiscCharges_ClientId", table: "MiscCharges", column: "ClientId");
            migrationBuilder.CreateIndex(name: "IX_MiscCharges_ImportBatchId", table: "MiscCharges", column: "ImportBatchId");
            migrationBuilder.CreateIndex(name: "IX_MiscCharges_NominalCodeId", table: "MiscCharges", column: "NominalCodeId");
            migrationBuilder.CreateIndex(name: "IX_MiscCharges_ClientId_UsedDate_Description_Amount", table: "MiscCharges", columns: new[] { "ClientId", "UsedDate", "Description", "Amount" });
            migrationBuilder.CreateIndex(name: "IX_SageExportBatches_CompanyId", table: "SageExportBatches", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_BillingExceptionLogs_LoggedAt", table: "BillingExceptionLogs", column: "LoggedAt");
            migrationBuilder.CreateIndex(name: "IX_BillingExceptionLogs_ClientId", table: "BillingExceptionLogs", column: "ClientId");
            migrationBuilder.CreateIndex(name: "IX_BillingExceptionLogs_CareHomeId", table: "BillingExceptionLogs", column: "CareHomeId");
            migrationBuilder.CreateIndex(name: "IX_UserCareHomeAccess_UserId_CareHomeId", table: "UserCareHomeAccess", columns: new[] { "UserId", "CareHomeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_UserCareHomeAccess_CareHomeId", table: "UserCareHomeAccess", column: "CareHomeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserCareHomeAccess");
            migrationBuilder.DropTable(name: "CreditNoteLines");
            migrationBuilder.DropTable(name: "BillingExceptionLogs");
            migrationBuilder.DropTable(name: "EmailSendLogs");
            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "CreditNotes");
            migrationBuilder.DropTable(name: "InvoiceLines");
            migrationBuilder.DropTable(name: "Invoices");
            migrationBuilder.DropTable(name: "FundingRates");
            migrationBuilder.DropTable(name: "MiscCharges");
            migrationBuilder.DropTable(name: "ClientFundingContracts");
            migrationBuilder.DropTable(name: "InvoiceTemplates");
            migrationBuilder.DropTable(name: "SageExportBatches");
            migrationBuilder.DropTable(name: "MiscChargeImportBatches");
            migrationBuilder.DropTable(name: "DocumentSequences");
            migrationBuilder.DropTable(name: "AspNetRoleClaims");
            migrationBuilder.DropTable(name: "AspNetUserClaims");
            migrationBuilder.DropTable(name: "AspNetUserLogins");
            migrationBuilder.DropTable(name: "AspNetUserRoles");
            migrationBuilder.DropTable(name: "AspNetUserTokens");
            migrationBuilder.DropTable(name: "AspNetRoles");
            migrationBuilder.DropTable(name: "AspNetUsers");
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 1);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 2);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 3);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 4);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 5);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 6);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 7);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 8);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 9);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 10);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 11);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 12);
            migrationBuilder.DeleteData(table: "CareHomes", keyColumn: "Id", keyValue: 13);
            migrationBuilder.DropColumn(name: "LogoPath", table: "CareHomes");
        }
    }
}

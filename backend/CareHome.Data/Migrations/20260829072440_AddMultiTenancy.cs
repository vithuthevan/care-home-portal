using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        private const string ExistingOrganisationPublicId = "9E4F2C11-7A8B-4D3E-9C10-1B2A3C4D5E6F";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PublicId",
                table: "Tenants",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreditNotePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumberLength = table.Column<int>(type: "int", nullable: false),
                    PaymentTermsDays = table.Column<int>(type: "int", nullable: false),
                    EmailFromName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EmailFromAddress = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PrimaryColour = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.Sql($"""
                SET IDENTITY_INSERT Tenants ON;
                INSERT INTO Tenants (Id, PublicId, Name, IsActive, CreatedAt)
                VALUES (1, '{ExistingOrganisationPublicId}', N'Existing Organisation', 1, SYSDATETIMEOFFSET());
                SET IDENTITY_INSERT Tenants OFF;

                INSERT INTO TenantSettings (
                    TenantId, CurrencyCode, CurrencySymbol, TimeZoneId,
                    InvoicePrefix, CreditNotePrefix, NumberLength, PaymentTermsDays)
                VALUES (1, 'GBP', N'£', 'Europe/London', 'INV-', 'CN-', 4, 30);
                """);

            AddNullableTenantId(migrationBuilder, "Companies");
            AddNullableTenantId(migrationBuilder, "CareHomes");
            AddNullableTenantId(migrationBuilder, "Clients");
            AddNullableTenantId(migrationBuilder, "FundingAuthorities");
            AddNullableTenantId(migrationBuilder, "InvoiceCategories");
            AddNullableTenantId(migrationBuilder, "NominalCodes");
            // Operational tables are created later by AddOperationalDomain on a fresh database.
            AddNullableTenantId(migrationBuilder, "ClientFundingContracts");
            AddNullableTenantId(migrationBuilder, "InvoiceTemplates");
            AddNullableTenantId(migrationBuilder, "Invoices");
            AddNullableTenantId(migrationBuilder, "CreditNotes");
            AddNullableTenantId(migrationBuilder, "MiscChargeImportBatches");
            AddNullableTenantId(migrationBuilder, "MiscCharges");
            AddNullableTenantId(migrationBuilder, "SageExportBatches");
            AddNullableTenantId(migrationBuilder, "AuditLogs");
            AddNullableTenantId(migrationBuilder, "BillingExceptionLogs");
            AddNullableTenantId(migrationBuilder, "EmailSendLogs");

            AddNullableTenantId(migrationBuilder, "AspNetUsers");
            AddSnapshotAndNumberColumns(migrationBuilder);
            ReshapeDocumentSequencesIfPresent(migrationBuilder);

            migrationBuilder.Sql("""
                UPDATE Companies SET TenantId = 1 WHERE TenantId IS NULL;
                UPDATE CareHomes SET TenantId = 1 WHERE TenantId IS NULL;
                UPDATE Clients SET TenantId = 1 WHERE TenantId IS NULL;
                UPDATE FundingAuthorities SET TenantId = 1 WHERE TenantId IS NULL;
                UPDATE InvoiceCategories SET TenantId = 1 WHERE TenantId IS NULL;
                UPDATE NominalCodes SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'ClientFundingContracts', 'U') IS NOT NULL UPDATE ClientFundingContracts SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'InvoiceTemplates', 'U') IS NOT NULL UPDATE InvoiceTemplates SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'Invoices', 'U') IS NOT NULL
                BEGIN
                    UPDATE Invoices SET TenantId = 1 WHERE TenantId IS NULL;
                    IF COL_LENGTH(N'Invoices', 'SnapshotTenantName') IS NOT NULL
                        UPDATE Invoices SET SnapshotTenantName = N'Existing Organisation'
                        WHERE SnapshotTenantName = '' OR SnapshotTenantName IS NULL;
                END
                IF OBJECT_ID(N'CreditNotes', 'U') IS NOT NULL UPDATE CreditNotes SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'MiscChargeImportBatches', 'U') IS NOT NULL UPDATE MiscChargeImportBatches SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'MiscCharges', 'U') IS NOT NULL UPDATE MiscCharges SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'SageExportBatches', 'U') IS NOT NULL UPDATE SageExportBatches SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'AuditLogs', 'U') IS NOT NULL UPDATE AuditLogs SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'BillingExceptionLogs', 'U') IS NOT NULL UPDATE BillingExceptionLogs SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'EmailSendLogs', 'U') IS NOT NULL UPDATE EmailSendLogs SET TenantId = 1 WHERE TenantId IS NULL;
                IF OBJECT_ID(N'DocumentSequences', 'U') IS NOT NULL AND COL_LENGTH(N'DocumentSequences', 'TenantId') IS NOT NULL
                    UPDATE DocumentSequences
                    SET TenantId = 1,
                        Prefix = CASE WHEN DocumentType = 'Invoice' THEN 'INV-' ELSE 'CN-' END,
                        NumberLength = 4
                    WHERE TenantId IS NULL;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'AspNetRoles', 'U') IS NOT NULL AND OBJECT_ID(N'AspNetUsers', 'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'PLATFORMADMIN')
                    BEGIN
                        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                        VALUES (NEWID(), 'PlatformAdmin', 'PLATFORMADMIN', NEWID());
                    END

                    IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'TENANTADMIN')
                    BEGIN
                        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                        VALUES (NEWID(), 'TenantAdmin', 'TENANTADMIN', NEWID());
                    END

                    INSERT INTO AspNetUserRoles (UserId, RoleId)
                    SELECT ur.UserId, pa.Id
                    FROM AspNetUserRoles ur
                    INNER JOIN AspNetRoles sa ON sa.Id = ur.RoleId AND sa.NormalizedName = 'SUPERADMIN'
                    CROSS JOIN AspNetRoles pa
                    WHERE pa.NormalizedName = 'PLATFORMADMIN'
                      AND NOT EXISTS (
                          SELECT 1 FROM AspNetUserRoles existing
                          WHERE existing.UserId = ur.UserId AND existing.RoleId = pa.Id);

                    UPDATE AspNetUsers
                    SET TenantId = 1
                    WHERE COL_LENGTH(N'AspNetUsers', 'TenantId') IS NOT NULL
                      AND TenantId IS NULL
                      AND Id NOT IN (
                          SELECT ur.UserId
                          FROM AspNetUserRoles ur
                          INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                          WHERE r.NormalizedName IN ('SUPERADMIN', 'PLATFORMADMIN'));
                END
                """);

            MakeTenantIdRequired(migrationBuilder, "Companies", "FK_Companies_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "CareHomes", "FK_CareHomes_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "Clients", "FK_Clients_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "FundingAuthorities", "FK_FundingAuthorities_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "InvoiceCategories", "FK_InvoiceCategories_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "NominalCodes", "FK_NominalCodes_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "ClientFundingContracts", "FK_ClientFundingContracts_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "InvoiceTemplates", "FK_InvoiceTemplates_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "Invoices", "FK_Invoices_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "CreditNotes", "FK_CreditNotes_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "MiscChargeImportBatches", "FK_MiscChargeImportBatches_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "MiscCharges", "FK_MiscCharges_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "SageExportBatches", "FK_SageExportBatches_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "AuditLogs", "FK_AuditLogs_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "BillingExceptionLogs", "FK_BillingExceptionLogs_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "EmailSendLogs", "FK_EmailSendLogs_Tenants_TenantId");
            MakeTenantIdRequired(migrationBuilder, "DocumentSequences", "FK_DocumentSequences_Tenants_TenantId");

            CreateIndexIfTableExists(migrationBuilder, "IX_AspNetUsers_TenantId", "AspNetUsers", "[TenantId]", unique: false);
            AddTenantForeignKeyIfMissing(migrationBuilder, "FK_AspNetUsers_Tenants_TenantId", "AspNetUsers");

            DropIndexIfExists(migrationBuilder, "IX_Companies_Name", "Companies");
            DropIndexIfExists(migrationBuilder, "IX_CareHomes_Code", "CareHomes");
            DropIndexIfExists(migrationBuilder, "IX_Clients_SageId", "Clients");
            DropIndexIfExists(migrationBuilder, "IX_Clients_ReferenceNumber", "Clients");
            DropIndexIfExists(migrationBuilder, "IX_FundingAuthorities_Code", "FundingAuthorities");
            DropIndexIfExists(migrationBuilder, "IX_InvoiceCategories_Code", "InvoiceCategories");
            DropIndexIfExists(migrationBuilder, "IX_NominalCodes_Code", "NominalCodes");
            DropIndexIfExists(migrationBuilder, "IX_Invoices_InvoiceNumber", "Invoices");
            DropIndexIfExists(migrationBuilder, "IX_CreditNotes_CreditNoteNumber", "CreditNotes");

            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_Companies_TenantId_Name", "Companies", "[TenantId], [Name]");
            CreateIndexIfTableExists(migrationBuilder, "IX_Companies_TenantId_IsActive", "Companies", "[TenantId], [IsActive]", unique: false);
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_CareHomes_TenantId_Code", "CareHomes", "[TenantId], [Code]");
            CreateIndexIfTableExists(migrationBuilder, "IX_CareHomes_TenantId_IsActive", "CareHomes", "[TenantId], [IsActive]", unique: false);
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_Clients_TenantId_SageId", "Clients", "[TenantId], [SageId]");
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_Clients_TenantId_ReferenceNumber", "Clients", "[TenantId], [ReferenceNumber]");
            CreateIndexIfTableExists(migrationBuilder, "IX_Clients_TenantId", "Clients", "[TenantId]", unique: false);
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_FundingAuthorities_TenantId_Code", "FundingAuthorities", "[TenantId], [Code]");
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_InvoiceCategories_TenantId_Code", "InvoiceCategories", "[TenantId], [Code]");
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_NominalCodes_TenantId_Code", "NominalCodes", "[TenantId], [Code]");
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_Invoices_TenantId_InvoiceNumber", "Invoices", "[TenantId], [InvoiceNumber]");
            CreateIndexIfTableExists(migrationBuilder, "IX_Invoices_TenantId", "Invoices", "[TenantId]", unique: false);
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_CreditNotes_TenantId_CreditNoteNumber", "CreditNotes", "[TenantId], [CreditNoteNumber]");
            CreateIndexIfTableExists(migrationBuilder, "IX_CreditNotes_TenantId", "CreditNotes", "[TenantId]", unique: false);
            CreateUniqueIndexIfTableExists(migrationBuilder, "IX_DocumentSequences_TenantId_DocumentType", "DocumentSequences", "[TenantId], [DocumentType]");
            CreateIndexIfTableExists(migrationBuilder, "IX_ClientFundingContracts_TenantId", "ClientFundingContracts", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_InvoiceTemplates_TenantId", "InvoiceTemplates", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_MiscChargeImportBatches_TenantId", "MiscChargeImportBatches", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_MiscCharges_TenantId", "MiscCharges", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_SageExportBatches_TenantId", "SageExportBatches", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_AuditLogs_TenantId", "AuditLogs", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_BillingExceptionLogs_TenantId", "BillingExceptionLogs", "[TenantId]", unique: false);
            CreateIndexIfTableExists(migrationBuilder, "IX_EmailSendLogs_TenantId", "EmailSendLogs", "[TenantId]", unique: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "AddMultiTenancy cannot be reversed without restoring global unique indexes and removing tenant isolation.");
        }

        private static void AddNullableTenantId(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL AND COL_LENGTH(N'{table}', 'TenantId') IS NULL
                BEGIN
                    ALTER TABLE [{table}] ADD [TenantId] int NULL;
                END
                """);
        }

        private static void MakeTenantIdRequired(MigrationBuilder migrationBuilder, string table, string foreignKeyName)
        {
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL AND COL_LENGTH(N'{table}', 'TenantId') IS NOT NULL
                BEGIN
                    UPDATE [{table}] SET [TenantId] = 1 WHERE [TenantId] IS NULL;
                    ALTER TABLE [{table}] ALTER COLUMN [TenantId] int NOT NULL;
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys WHERE name = '{foreignKeyName}')
                    BEGIN
                        ALTER TABLE [{table}] WITH CHECK ADD CONSTRAINT [{foreignKeyName}]
                            FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
                    END
                END
                """);
        }

        private static void AddSnapshotAndNumberColumns(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'Invoices', 'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'Invoices', 'SnapshotTenantName') IS NULL
                        ALTER TABLE [Invoices] ADD [SnapshotTenantName] nvarchar(150) NOT NULL CONSTRAINT [DF_Invoices_SnapshotTenantName] DEFAULT(N'');
                    ALTER TABLE [Invoices] ALTER COLUMN [InvoiceNumber] nvarchar(40) NOT NULL;
                END
                IF OBJECT_ID(N'CreditNotes', 'U') IS NOT NULL
                    ALTER TABLE [CreditNotes] ALTER COLUMN [CreditNoteNumber] nvarchar(40) NOT NULL;
                """);
        }

        private static void ReshapeDocumentSequencesIfPresent(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'DocumentSequences', 'U') IS NULL
                    RETURN;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentSequences_Name' AND object_id = OBJECT_ID(N'DocumentSequences'))
                    DROP INDEX [IX_DocumentSequences_Name] ON [DocumentSequences];

                IF COL_LENGTH(N'DocumentSequences', 'Name') IS NOT NULL AND COL_LENGTH(N'DocumentSequences', 'DocumentType') IS NULL
                    EXEC sp_rename N'DocumentSequences.Name', N'DocumentType', N'COLUMN';

                IF COL_LENGTH(N'DocumentSequences', 'TenantId') IS NULL
                    ALTER TABLE [DocumentSequences] ADD [TenantId] int NULL;

                IF COL_LENGTH(N'DocumentSequences', 'Prefix') IS NULL
                    ALTER TABLE [DocumentSequences] ADD [Prefix] nvarchar(20) NOT NULL CONSTRAINT [DF_DocumentSequences_Prefix] DEFAULT(N'');

                IF COL_LENGTH(N'DocumentSequences', 'NumberLength') IS NULL
                    ALTER TABLE [DocumentSequences] ADD [NumberLength] int NOT NULL CONSTRAINT [DF_DocumentSequences_NumberLength] DEFAULT(4);
                """);
        }

        private static void CreateIndexIfTableExists(
            MigrationBuilder migrationBuilder,
            string indexName,
            string table,
            string columns,
            bool unique)
        {
            var uniqueSql = unique ? "UNIQUE " : "";
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID(N'{table}'))
                BEGIN
                    CREATE {uniqueSql}INDEX [{indexName}] ON [{table}] ({columns});
                END
                """);
        }

        private static void CreateUniqueIndexIfTableExists(
            MigrationBuilder migrationBuilder,
            string indexName,
            string table,
            string columns)
        {
            CreateIndexIfTableExists(migrationBuilder, indexName, table, columns, unique: true);
        }

        private static void AddTenantForeignKeyIfMissing(MigrationBuilder migrationBuilder, string foreignKeyName, string table)
        {
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL
                   AND COL_LENGTH(N'{table}', 'TenantId') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{foreignKeyName}')
                BEGIN
                    ALTER TABLE [{table}] WITH CHECK ADD CONSTRAINT [{foreignKeyName}]
                        FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
                END
                """);
        }

        private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string indexName, string table)
        {
            migrationBuilder.Sql($"""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = '{indexName}' AND object_id = OBJECT_ID(N'{table}'))
                BEGIN
                    DROP INDEX [{indexName}] ON [{table}];
                END
                """);
        }
    }
}

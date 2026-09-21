using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHome.Api.Migrations
{
    /// <summary>
    /// AddOperationalDomain creates Identity and financial tables without TenantId.
    /// This brings those tables in line with the current tenant-aware model.
    /// Safe to run on databases that already have the columns.
    /// </summary>
    public partial class AlignOperationalTenantSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            EnsureTenantColumn(migrationBuilder, "AspNetUsers", required: false);
            EnsureTenantColumn(migrationBuilder, "ClientFundingContracts", required: true);
            EnsureTenantColumn(migrationBuilder, "InvoiceTemplates", required: true);
            EnsureTenantColumn(migrationBuilder, "Invoices", required: true);
            EnsureTenantColumn(migrationBuilder, "CreditNotes", required: true);
            EnsureTenantColumn(migrationBuilder, "MiscChargeImportBatches", required: true);
            EnsureTenantColumn(migrationBuilder, "MiscCharges", required: true);
            EnsureTenantColumn(migrationBuilder, "SageExportBatches", required: true);
            EnsureTenantColumn(migrationBuilder, "AuditLogs", required: true);
            EnsureTenantColumn(migrationBuilder, "BillingExceptionLogs", required: true);
            EnsureTenantColumn(migrationBuilder, "EmailSendLogs", required: true);
            EnsureTenantColumn(migrationBuilder, "DocumentSequences", required: true);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'Invoices', 'U') IS NOT NULL AND COL_LENGTH(N'Invoices', 'SnapshotTenantName') IS NULL
                    ALTER TABLE [Invoices] ADD [SnapshotTenantName] nvarchar(150) NOT NULL
                        CONSTRAINT [DF_Invoices_SnapshotTenantName] DEFAULT(N'');
                """);
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'Invoices', 'U') IS NOT NULL
                    ALTER TABLE [Invoices] ALTER COLUMN [InvoiceNumber] nvarchar(40) NOT NULL;
                IF OBJECT_ID(N'CreditNotes', 'U') IS NOT NULL
                    ALTER TABLE [CreditNotes] ALTER COLUMN [CreditNoteNumber] nvarchar(40) NOT NULL;
                """);
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'Invoices', 'U') IS NOT NULL AND COL_LENGTH(N'Invoices', 'SnapshotTenantName') IS NOT NULL
                    UPDATE Invoices SET SnapshotTenantName = N'Existing Organisation'
                    WHERE SnapshotTenantName = '' OR SnapshotTenantName IS NULL;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'DocumentSequences', 'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentSequences_Name' AND object_id = OBJECT_ID(N'DocumentSequences'))
                        DROP INDEX [IX_DocumentSequences_Name] ON [DocumentSequences];
                    IF COL_LENGTH(N'DocumentSequences', 'Name') IS NOT NULL AND COL_LENGTH(N'DocumentSequences', 'DocumentType') IS NULL
                        EXEC sp_rename N'DocumentSequences.Name', N'DocumentType', N'COLUMN';
                    IF COL_LENGTH(N'DocumentSequences', 'Prefix') IS NULL
                        ALTER TABLE [DocumentSequences] ADD [Prefix] nvarchar(20) NOT NULL CONSTRAINT [DF_DocumentSequences_Prefix] DEFAULT(N'');
                    IF COL_LENGTH(N'DocumentSequences', 'NumberLength') IS NULL
                        ALTER TABLE [DocumentSequences] ADD [NumberLength] int NOT NULL CONSTRAINT [DF_DocumentSequences_NumberLength] DEFAULT(4);
                END
                """);
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'DocumentSequences', 'U') IS NOT NULL AND COL_LENGTH(N'DocumentSequences', 'DocumentType') IS NOT NULL
                BEGIN
                    UPDATE DocumentSequences
                    SET TenantId = 1,
                        Prefix = CASE WHEN DocumentType = 'Invoice' THEN 'INV-' WHEN DocumentType = 'CreditNote' THEN 'CN-' ELSE Prefix END,
                        NumberLength = CASE WHEN NumberLength = 0 THEN 4 ELSE NumberLength END
                    WHERE TenantId IS NULL OR TenantId = 0;
                    IF NOT EXISTS (SELECT 1 FROM DocumentSequences WHERE TenantId = 1 AND DocumentType = 'Invoice')
                        INSERT INTO DocumentSequences (TenantId, DocumentType, Prefix, NumberLength, NextValue)
                        VALUES (1, 'Invoice', 'INV-', 4, 1);
                    IF NOT EXISTS (SELECT 1 FROM DocumentSequences WHERE TenantId = 1 AND DocumentType = 'CreditNote')
                        INSERT INTO DocumentSequences (TenantId, DocumentType, Prefix, NumberLength, NextValue)
                        VALUES (1, 'CreditNote', 'CN-', 4, 1);
                END
                """);

            DropIndexIfExists(migrationBuilder, "IX_Invoices_InvoiceNumber", "Invoices");
            DropIndexIfExists(migrationBuilder, "IX_CreditNotes_CreditNoteNumber", "CreditNotes");

            CreateIndexIfMissing(migrationBuilder, "IX_AspNetUsers_TenantId", "AspNetUsers", "[TenantId]", unique: false);
            AddFkIfMissing(migrationBuilder, "FK_AspNetUsers_Tenants_TenantId", "AspNetUsers");
            CreateIndexIfMissing(migrationBuilder, "IX_Invoices_TenantId_InvoiceNumber", "Invoices", "[TenantId], [InvoiceNumber]", unique: true);
            CreateIndexIfMissing(migrationBuilder, "IX_Invoices_TenantId", "Invoices", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_CreditNotes_TenantId_CreditNoteNumber", "CreditNotes", "[TenantId], [CreditNoteNumber]", unique: true);
            CreateIndexIfMissing(migrationBuilder, "IX_CreditNotes_TenantId", "CreditNotes", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_DocumentSequences_TenantId_DocumentType", "DocumentSequences", "[TenantId], [DocumentType]", unique: true);
            CreateIndexIfMissing(migrationBuilder, "IX_ClientFundingContracts_TenantId", "ClientFundingContracts", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_InvoiceTemplates_TenantId", "InvoiceTemplates", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_MiscChargeImportBatches_TenantId", "MiscChargeImportBatches", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_MiscCharges_TenantId", "MiscCharges", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_SageExportBatches_TenantId", "SageExportBatches", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_AuditLogs_TenantId", "AuditLogs", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_BillingExceptionLogs_TenantId", "BillingExceptionLogs", "[TenantId]", unique: false);
            CreateIndexIfMissing(migrationBuilder, "IX_EmailSendLogs_TenantId", "EmailSendLogs", "[TenantId]", unique: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("AlignOperationalTenantSchema cannot be reversed.");
        }

        private static void EnsureTenantColumn(MigrationBuilder migrationBuilder, string table, bool required)
        {
            var nullClause = required ? "NOT NULL" : "NULL";
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL AND COL_LENGTH(N'{table}', 'TenantId') IS NULL
                    ALTER TABLE [{table}] ADD [TenantId] int NULL;
                """);
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL AND COL_LENGTH(N'{table}', 'TenantId') IS NOT NULL
                    UPDATE [{table}] SET [TenantId] = 1 WHERE [TenantId] IS NULL;
                """);
            if (required)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'{table}', 'U') IS NOT NULL AND COL_LENGTH(N'{table}', 'TenantId') IS NOT NULL
                    BEGIN
                        ALTER TABLE [{table}] ALTER COLUMN [TenantId] int {nullClause};
                        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_{table}_Tenants_TenantId')
                            ALTER TABLE [{table}] WITH CHECK ADD CONSTRAINT [FK_{table}_Tenants_TenantId]
                                FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
                    END
                    """);
            }
        }

        private static void CreateIndexIfMissing(
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
                    CREATE {uniqueSql}INDEX [{indexName}] ON [{table}] ({columns});
                """);
        }

        private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string indexName, string table)
        {
            migrationBuilder.Sql($"""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID(N'{table}'))
                    DROP INDEX [{indexName}] ON [{table}];
                """);
        }

        private static void AddFkIfMissing(MigrationBuilder migrationBuilder, string foreignKeyName, string table)
        {
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'{table}', 'U') IS NOT NULL
                   AND COL_LENGTH(N'{table}', 'TenantId') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{foreignKeyName}')
                    ALTER TABLE [{table}] WITH CHECK ADD CONSTRAINT [{foreignKeyName}]
                        FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]);
                """);
        }
    }
}

# Backup and restore

**Production runbook:** See `operations/BACKUP_AND_RECOVERY_RUNBOOK.md` for automated backup strategy, Azure SQL PITR, document storage backup, responsibilities, and the full verification checklist.

A restore was **verified** on 29 August 2026 against disposable LocalDB databases `CareHomeHardeningDb` → backup → `CareHomeHardeningRestoreDb`. `CareHomeDb` was not used and was not wiped.

Verified after restore (API pointed at the restored database):

| Check | Result |
|---|---|
| Login | PASS (`tenantadmin-a@hard.test`) |
| Client | PASS (`SAGE001` / Alice Brown) |
| Funding contract | PASS (1 contract) |
| Invoice | PASS (`INV-0001`, **£7,639.29**, 3 lines) |
| Invoice lines | PASS |
| Credit note | PASS (`CN-0001`) |
| Audit | PASS (tenant-scoped rows present) |

This proves the backup format and application compatibility. It is **not** a substitute for a restore test on the eventual Production SQL Server instance.

## What to back up

1. **SQL database** — tenants, users, clients, contracts, invoices, credits, audit, sequences.
2. **Document storage** — invoice PDFs, credit-note PDFs, Sage export CSVs under `DocumentStorage:RootPath` (or `App_Data/documents`). Paths are `tenants/{publicId}/invoices|credit-notes|sage-exports/`.

A database-only backup leaves PDFs and Sage files unrestorable. Invoice rows store a relative `PdfPath`; if the file is missing the API regenerates from **snapshots**, but Sage batch files and any logo files are not in SQL.

## Azure SQL (production host)

Azure SQL automated backups are platform-managed. IaC configures **35-day PITR** and long-term retention on Standard S0+ (`infra/azure/main.bicep`).

**Before every migration:**

```powershell
.\scripts\Backup-CareHome.ps1 -Target Azure -ResourceGroup <rg> -SqlServerName <server> -SqlDatabaseName CareHome -Purpose PreMigration
```

Or use `.\scripts\Deploy-Azure.ps1 -PreMigrationBackup`.

**Point-in-time restore** (to a different database name):

```powershell
az sql db restore --dest-name CareHomeRestoreCheck --name CareHome --resource-group <rg> --server <server> --time "<utc-timestamp>"
```

Full procedure: `operations/BACKUP_AND_RECOVERY_RUNBOOK.md` sections 5.1–5.5.

## Backup procedure (on-premises SQL Server)

Take this **before every migration/release**. Example (replace names):

```sql
BACKUP DATABASE [CareHome]
TO DISK = N'D:\backups\CareHome_predeploy.bak'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;
```

Copy the document root in the same change window, for example:

```powershell
robocopy D:\carehome-documents D:\backups\carehome-documents-predeploy /MIR
```

LocalDB (lab only):

```sql
BACKUP DATABASE [CareHomeHardeningDb]
TO DISK = N'C:\path\CareHomeHardeningDb.bak'
WITH COPY_ONLY, INIT;
```

## Restore procedure

Restore to a **different** database name first (never overwrite Production to “test” the restore).

```sql
RESTORE FILELISTONLY FROM DISK = N'D:\backups\CareHome_predeploy.bak';

RESTORE DATABASE [CareHomeRestoreCheck]
FROM DISK = N'D:\backups\CareHome_predeploy.bak'
WITH REPLACE,
  MOVE N'<logical_data>' TO N'D:\data\CareHomeRestoreCheck.mdf',
  MOVE N'<logical_log>' TO N'D:\data\CareHomeRestoreCheck_log.ldf',
  CHECKSUM;
```

Restore document files to a parallel folder. Point a **non-production** API instance at the restored database and restored document root:

```text
ConnectionStrings__DefaultConnection=...Database=CareHomeRestoreCheck...
DocumentStorage__RootPath=D:\backups\carehome-documents-predeploy
```

## Verification procedure

After pointing the API at the restored database:

1. `GET /health/ready` → Healthy
2. Login as a known tenant user
3. Open one client, its funding contract, one invoice (header + lines), one credit note, audit
4. Download that invoice PDF (file present **or** regenerated from snapshots)
5. Confirm another tenant cannot see the restored tenant’s invoice (404)

Do not claim a Production instance is recoverable until this has been done on **that** SQL Server with **that** backup tool.

## Recommended schedule

Agreed defaults are documented in `operations/BACKUP_AND_RECOVERY_RUNBOOK.md` section 2. Summary:

| Item | Azure (production) | On-premises |
|---|---|---|
| SQL backup | Platform-managed continuous PITR (35 days) | Daily full + hourly log; before migrations |
| Document storage | Daily Azure Files backup (02:00 UTC) + pre-migration snapshot | Daily `robocopy` with SQL job |
| Off-site copy | Azure geo-redundant backup storage | Operator-managed secondary site |
| Restore test | P0-4 drill on production-tier SQL | Same checklist |
| Retention | 35 days PITR + LTR (IaC); legal sign-off required | Operator/legal decision |

## Rollback after a failed release

If a migration was applied and the release must be abandoned: restore the **pre-deployment** database backup and the matching document copy. Do not run EF `Down()` on live financial data.

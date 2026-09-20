# Restore Drill Procedure

**Application:** Care Home Back-Office Management System  
**Last updated:** 15 September 2026  
**Scope:** P0-4 — production-tier database restore **validation preparation** (drill only; not live recovery)  
**Related:** `BACKUP_AND_RECOVERY_RUNBOOK.md`, `../BACKUP_RESTORE.md`, `../pilot/P0_EXECUTION_ROADMAP.md` (P0-4)

---

## Purpose

This document defines a **safe, repeatable restore drill** that validates disaster-recovery readiness without touching the live production database. A restore drill proves that backups taken on the production SQL host can be recovered, paired with document storage, and used by the application.

**This procedure is preparation and validation only.** Do not execute a restore over production. Do not delete production databases. Do not deploy application changes as part of the drill.

---

## Safety rules (mandatory)

| Rule | Reason |
|------|--------|
| **Never** restore over the live `CareHome` database | Prevents data loss and service outage |
| **Always** restore to an isolated database name (default: `CareHomeRestoreCheck`) | Keeps production online during the drill |
| **Never** run EF migrations against the restored drill database | Restored DB already contains the schema at backup time; migrations would mutate drill state |
| **Never** set `Database__ApplyMigrations=true` on the drill API instance | Same as above |
| **Never** leave `Seed__AdminEmail` / `Seed__AdminPassword` set on a drill host | Avoids creating duplicate PlatformAdmin users |
| **Always** pair SQL restore timestamp with a document backup from the same window (±15 min) | Invoice PDF paths and Sage CSVs are not in SQL |
| **Always** delete or decommission the drill database and restored document copy after sign-off | Prevents stale PII sitting in an unmonitored database |

---

## Prerequisites

Complete these before scheduling the drill.

| # | Prerequisite | Owner | How to verify |
|---|--------------|-------|---------------|
| 1 | P0-3 automated backups running on production-tier SQL | DevOps | `.\scripts\Verify-BackupReadiness.ps1` returns PASS |
| 2 | At least one document storage backup exists (Azure Files or on-prem mirror) | DevOps | Recovery Services Vault shows recent backup, or robocopy mirror exists |
| 3 | Isolated restore target available (separate DB name; separate document folder/share) | DevOps | Capacity confirmed on SQL server / Azure subscription |
| 4 | Drill API host identified (local workstation, staging slot, or temporary App Service) | Developer | Not the live production App Service unless approved maintenance window |
| 5 | Known test credentials for at least one tenant user | Developer / Operations | Documented in secure ops register (not in git) |
| 6 | Pre-drill baseline counts captured from production (read-only) | DevOps | See section 4; used by `Verify-RestoreDrill.ps1` |
| 7 | Finance reference invoice identified for total spot-check | Finance | Invoice number and expected total recorded |
| 8 | Azure CLI (`az`) or `sqlcmd` available to operators | DevOps | Required for restore execution (outside this doc's scope) |
| 9 | Participants scheduled: DevOps, Developer, Finance (spot-check) | DevOps | Minimum three roles for sign-off |

**Dependency:** P0-4 cannot complete until P0-3 has produced at least one successful production-tier backup.

---

## System context (analysis summary)

### Backup configuration

| Component | Production default | Notes |
|-----------|-------------------|-------|
| SQL | Azure SQL Standard S0+, 35-day PITR, platform-managed | See `infra/azure/main.bicep`, `BACKUP_AND_RECOVERY_RUNBOOK.md` §2.1 |
| Documents | Azure Files daily backup (02:00 UTC) + pre-migration snapshot | Paths under `DocumentStorage__RootPath` |
| On-prem alternative | `BACKUP DATABASE ... WITH CHECKSUM` + `robocopy` | `scripts/Backup-CareHome.ps1 -Target OnPrem` |

### Database schema (critical entities)

The restored database must contain data in these tables (EF Core / ASP.NET Identity):

| Business concept | SQL table(s) | Notes |
|------------------|--------------|-------|
| Users | `AspNetUsers`, `AspNetUserRoles` | ASP.NET Identity |
| Roles | `AspNetRoles` | `PlatformAdmin`, `TenantAdmin`, `Administrator`, `LocationManager`, `ReadOnly` (+ legacy `SuperAdmin`) |
| Companies | `Companies` | Tenant-scoped operator companies |
| Residents (clients) | `Clients` | Care home residents; linked to `CareHomes` |
| Contracts | `ClientFundingContracts`, `FundingRates` | Funding agreements per client |
| Invoices | `Invoices`, `InvoiceLines` | Financial records |
| Payments | `Invoices.PaymentStatus` | `Paid` / `NotPaid` — no separate payments table |
| Credit notes | `CreditNotes`, `CreditNoteLines` | |
| Documents (metadata) | `Invoices.PdfPath`, `CreditNotes.PdfPath`, `SageExportBatches` | Files on disk under `tenants/{publicId}/` |
| Audit | `AuditLogs` | Tenant-scoped activity |
| Tenants | `Tenants`, `TenantSettings` | Multi-tenant isolation |

### Migration strategy

- Schema is managed by EF Core migrations in `backend/CareHome.Api/Migrations/` (25 migration files as of Sep 2026).
- Production deploys apply migrations via `dotnet ef database update` or `Database__ApplyMigrations=true` **only on the live database**.
- A restored database includes `__EFMigrationsHistory` at the backup point — **do not re-apply migrations** during a drill.
- Rollback after a failed release uses restore, not `migration Down()`.

### Application startup requirements (drill instance)

| Setting | Drill value | Notes |
|---------|-------------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` (recommended) or `Staging` | Matches production validation path |
| `ConnectionStrings__DefaultConnection` | Points to `CareHomeRestoreCheck` | **Not** production `CareHome` |
| `DocumentStorage__RootPath` | Restored document root | e.g. `/home/carehome-documents-restore` |
| `Jwt__Key` | Valid production or drill-specific key (≥32 chars) | Required outside Development |
| `Database__ApplyMigrations` | `false` or unset | **Critical** — do not migrate restored DB |
| `Seed__AdminEmail` / `Seed__AdminPassword` | **Empty** | Avoid seeding duplicate admins |
| `Email__Mode` | `Smtp` **or** `Email__AllowSimulationInProduction=true` | Production validator blocks simulated email unless explicitly allowed |
| `Cors__AllowedOrigins__*` | Drill host origin or empty (same-origin) | |

### Health checks

| Endpoint | Tag | Check |
|----------|-----|-------|
| `GET /health/live` | `live` | Process alive (`self` check) |
| `GET /health/ready` | `ready` | SQL connectivity via `SqlReadyHealthCheck` (`CanConnectAsync`) |

Both return JSON: `{ "status": "Healthy", "correlationId": "..." }`.

---

## Restore drill flow

```text
┌─────────────────────────────────────────────────────────────────────────┐
│ 1. CAPTURE BASELINE (read-only on production)                           │
│    Verify-RestoreDrill.ps1 -Mode Baseline ...                           │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 2. IDENTIFY BACKUP POINT                                                │
│    Azure: PITR timestamp / On-prem: latest .bak + document mirror       │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 3. RESTORE SQL → isolated database (CareHomeRestoreCheck)               │
│    Azure: az sql db restore  |  On-prem: RESTORE DATABASE ... WITH MOVE │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 4. RESTORE DOCUMENTS → parallel path/share                              │
│    Azure: Recovery Services restore / snapshot copy                     │
│    On-prem: robocopy mirror                                             │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 5. CONFIGURE DRILL API INSTANCE                                         │
│    Connection string + DocumentStorage__RootPath + no migrations/seed   │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 6. START APPLICATION → GET /health/ready = Healthy                      │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 7. VALIDATE (SQL counts + API smoke + finance spot-check)               │
│    Verify-RestoreDrill.ps1 -Mode Validate ...                           │
└───────────────────────────────┬─────────────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 8. DOCUMENT RESULTS + CLEANUP                                           │
│    Drill report signed; drop CareHomeRestoreCheck; remove doc copy      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Step-by-step procedure

### Step 1 — Capture pre-drill baseline (read-only)

Run against the **live production database** (SELECT only; no writes):

```powershell
.\scripts\Verify-RestoreDrill.ps1 `
  -Mode Baseline `
  -SqlServerName sql-carehome-pilot `
  -ResourceGroup rg-carehome `
  -SqlDatabaseName CareHome `
  -BaselineOutputPath .\artifacts\restore-drill-baseline.json
```

On-prem:

```powershell
.\scripts\Verify-RestoreDrill.ps1 `
  -Mode Baseline `
  -SqlServerInstance localhost `
  -SqlDatabaseName CareHome `
  -UseIntegratedSecurity `
  -BaselineOutputPath .\artifacts\restore-drill-baseline.json
```

Store the baseline file in the ops register (not in git if it reflects production counts).

### Step 2 — Identify backup point

Record the exact backup used:

| Host | Action |
|------|--------|
| **Azure SQL** | Note PITR timestamp (UTC). List window: `az sql db list-restore-windows --resource-group <rg> --server <server> --name CareHome` |
| **On-prem** | Note `.bak` file path, size, and `RESTORE VERIFYONLY` result |
| **Documents** | Note Azure Files backup job time or robocopy mirror timestamp |

**Coupling rule:** Document backup must be within **±15 minutes** of the SQL backup/PITR point.

### Step 3 — Restore SQL to isolated database

**Target database name:** `CareHomeRestoreCheck` (never `CareHome`).

#### Azure SQL (point-in-time restore)

```powershell
az sql db restore `
  --dest-name CareHomeRestoreCheck `
  --name CareHome `
  --resource-group rg-carehome `
  --server sql-carehome-pilot `
  --time "2026-09-15T10:30:00Z"
```

Or: Azure Portal → SQL database → **Restore** → new database name `CareHomeRestoreCheck`.

#### On-premises SQL Server

```sql
RESTORE FILELISTONLY FROM DISK = N'D:\backups\CareHome_predeploy.bak';

RESTORE DATABASE [CareHomeRestoreCheck]
FROM DISK = N'D:\backups\CareHome_predeploy.bak'
WITH REPLACE,
  MOVE N'<logical_data>' TO N'D:\data\CareHomeRestoreCheck.mdf',
  MOVE N'<logical_log>' TO N'D:\data\CareHomeRestoreCheck_log.ldf',
  CHECKSUM;
```

Full detail: `docs/BACKUP_RESTORE.md`.

### Step 4 — Restore document storage

| Host | Target path (examples) |
|------|------------------------|
| Azure App Service | `/home/carehome-documents-restore` (separate Azure Files share or restored copy) |
| On-prem | `D:\backups\carehome-documents-restore` |

**Azure Files from backup:** Recovery Services Vault → Backup items → Azure Files → **Restore** to a separate share.

**On-prem:**

```powershell
robocopy D:\backups\carehome-documents-predeploy D:\backups\carehome-documents-restore /MIR
```

Verify folder structure exists: `tenants/{tenantPublicId}/invoices/`, `credit-notes/`, `sage-exports/`.

### Step 5 — Configure drill API instance

Point a **non-production** API at the restored resources.

**Example environment (Azure App Service staging slot or local host):**

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=tcp:sql-carehome-pilot.database.windows.net,1433;Database=CareHomeRestoreCheck;User Id=<app-login>;Password=<secret>;Encrypt=True;TrustServerCertificate=False;
DocumentStorage__RootPath=/home/carehome-documents-restore
Jwt__Key=<≥32-char-secret>
Database__ApplyMigrations=false
Email__AllowSimulationInProduction=true
```

Do **not** set `Seed__AdminEmail` or `Seed__AdminPassword`.

Restart the drill API instance after configuration changes.

### Step 6 — Application smoke verification

Execute manually or via `Verify-RestoreDrill.ps1` (see section 7).

| # | Check | Method | Expected | Owner |
|---|-------|--------|----------|-------|
| 1 | Application starts | API process / App Service logs | No startup exceptions | Developer |
| 2 | `GET /health/ready` | HTTP | `200`, `status: Healthy` | Developer |
| 3 | Login | `POST /api/auth/login` with known tenant user | `200`, JWT returned | Developer |
| 4 | Users exist | SQL / API | `AspNetUsers` count matches baseline | Developer |
| 5 | Roles exist | SQL | All five `AppRoles.All` roles present | Developer |
| 6 | Companies exist | `GET /api/companies` or SQL | Count matches baseline | Developer |
| 7 | Residents (clients) exist | `GET /api/clients` or SQL | Count matches baseline | Developer |
| 8 | Contracts exist | SQL `ClientFundingContracts` | Count matches baseline | Developer |
| 9 | Invoices exist | `GET /api/invoices/{id}` | Known invoice opens with lines | Developer |
| 10 | Payments recorded | SQL `Invoices` where `PaymentStatus='Paid'` | Paid count matches baseline | Developer / Finance |
| 11 | Documents accessible | `GET /api/invoices/{id}/pdf` | `200`, body starts with `%PDF` | Developer |
| 12 | Credit note | `GET /api/credit-notes/{id}` | Known credit note present | Developer |
| 13 | Audit trail | SQL `AuditLogs` | Rows present for test tenant | Developer |
| 14 | Tenant isolation | Tenant B token on Tenant A invoice | `404` | Developer |
| 15 | Finance spot-check | Compare known invoice total | Matches pre-backup amount | Finance |
| 16 | Sage export file (if used) | File exists in restored `sage-exports/` folder | File present on disk | DevOps |

**LocalDB evidence (Aug 2026):** Restore to disposable LocalDB proved login, client `SAGE001`, contract, invoice `INV-0001` (£7,639.29), credit note `CN-0001`, and audit rows. That is **not** a substitute for this production-tier drill.

### Step 7 — Automated validation script

After the API is running against the restored database:

```powershell
.\scripts\Verify-RestoreDrill.ps1 `
  -Mode Validate `
  -SqlServerName sql-carehome-pilot `
  -ResourceGroup rg-carehome `
  -SqlDatabaseName CareHomeRestoreCheck `
  -BaselineInputPath .\artifacts\restore-drill-baseline.json `
  -ApiBaseUrl https://carehome-drill.azurewebsites.net `
  -TestUserEmail tenant.admin@example.org `
  -TestUserPassword <from-secure-register> `
  -DocumentRoot /home/carehome-documents-restore `
  -SampleInvoiceId 1 `
  -ReportOutputPath .\artifacts\restore-drill-report.json
```

The script performs **read-only** SQL queries and optional HTTP checks. It does **not** restore, delete, or modify any database.

### Step 8 — Document drill results

Complete the drill report template (section 9). Store in the ops register. Schedule the next drill (recommend: quarterly; mandatory before pilot go-live).

---

## Validation criteria (P0-4 completion)

All items must pass for the drill to be signed off:

- [ ] SQL restored to `CareHomeRestoreCheck` (or equivalent isolated name) on **production-tier** host
- [ ] Document storage restored to parallel path and paired with SQL backup window
- [ ] Drill API starts with `Database__ApplyMigrations=false`
- [ ] `GET /health/ready` returns `Healthy`
- [ ] Entity counts (users, roles, companies, clients, contracts, invoices, paid invoices) match baseline
- [ ] Known invoice opens with correct line count and total (finance sign-off)
- [ ] Invoice PDF downloads successfully (file on disk or regenerated from snapshots)
- [ ] Cross-tenant access returns `404`
- [ ] Drill report signed by DevOps, Developer, and Finance
- [ ] Cleanup completed (section 10)

---

## Rollback and cleanup

After sign-off or if the drill is abandoned:

| Resource | Cleanup action | Owner |
|----------|----------------|-------|
| `CareHomeRestoreCheck` database | `DROP DATABASE [CareHomeRestoreCheck]` (Azure Portal or `az sql db delete`) | DevOps |
| Restored document copy | Delete share/folder (`carehome-documents-restore`) | DevOps |
| Drill API configuration | Revert connection string to production DB and document root; stop staging slot if used | DevOps / Developer |
| Baseline / report files | Archive in ops register; do not commit to git | DevOps |
| Temporary secrets | Rotate if drill-specific credentials were created | DevOps |

**If the drill API was pointed at production by mistake:** Stop the instance immediately. Do not run migrations. Escalate to DevOps lead.

**Production was not modified** if only `CareHomeRestoreCheck` and a parallel document path were used.

---

## Owner responsibilities

| Role | Responsibilities |
|------|------------------|
| **DevOps** | Schedule drill; verify P0-3 backups; execute SQL and document restore; provision isolated targets; run baseline capture; delete drill resources after sign-off; maintain drill report |
| **Developer** | Configure drill API instance; verify health, login, API smoke, tenant isolation; run `Verify-RestoreDrill.ps1`; confirm no migrations applied |
| **Finance** | Identify reference invoice; spot-check totals and payment status on restored data |
| **Business / Operations** | Approve drill window; provide test user credentials from secure register |
| **DevOps lead** | Sign off P0-4 completion; schedule quarterly re-drills |

---

## Drill report template

```text
Restore Drill Report
====================
Date (UTC):           ____________________
Participants:         DevOps: ______  Developer: ______  Finance: ______

Backup source
  SQL type:           Azure PITR / On-prem .bak
  SQL timestamp:      ____________________
  Document backup:    ____________________
  Coupling (±15 min): PASS / FAIL

Restore execution
  Target DB name:     CareHomeRestoreCheck
  Restore started:    ____________________
  Restore completed:  ____________________
  Duration:           ______ minutes
  Document path:      ____________________

Validation
  Health ready:       PASS / FAIL
  Login:              PASS / FAIL
  Entity counts:      PASS / FAIL (see attached report JSON)
  Invoice spot-check: PASS / FAIL — Invoice #______ Total £______
  PDF download:       PASS / FAIL
  Tenant isolation:   PASS / FAIL

Outcome:              PASS / FAIL
Issues / actions:     ____________________
Next drill due:       ____________________

Signed: DevOps ______  Developer ______  Finance ______
```

---

## References

| Document / script | Purpose |
|-------------------|---------|
| `BACKUP_AND_RECOVERY_RUNBOOK.md` | Backup strategy, recovery cutover, monitoring |
| `docs/BACKUP_RESTORE.md` | SQL restore commands, LocalDB verification evidence |
| `P0_EXECUTION_ROADMAP.md` | P0-4 completion criteria |
| `docs/PRODUCTION_CONFIGURATION.md` | Environment variables for drill API |
| `docs/PRODUCTION_SMOKE_TEST.md` | Extended API smoke patterns |
| `scripts/Verify-BackupReadiness.ps1` | Pre-drill backup health check |
| `scripts/Verify-RestoreDrill.ps1` | Baseline capture and post-restore validation |
| `scripts/Backup-CareHome.ps1` | Coordinated backup before migrations |

---

*This procedure defines validation preparation only. Restore execution is performed by DevOps per the steps above during a scheduled maintenance window.*

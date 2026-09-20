# Sprint 0 — Pilot Safety and Infrastructure Readiness (Execution Report)

**Date:** 20 September 2026  
**Application:** Care Home Back-Office Management System (Angular + ASP.NET Core + **SQL Server**)  
**Sprint objective:** Make it safe to put the first real operator organisation’s data into the platform (controlled pilot).  
**Authority:** Current repository code and commands run in this sprint.  
**Production deployed:** **NO**

---

## Reference documents

| Document requested | Status |
|--------------------|--------|
| `FIRST_PAID_PILOT_READINESS_REPORT.md` | **Not present in repo** — used `PRODUCTION_LAUNCH_READINESS_REPORT.md`, `P0_EXECUTION_ROADMAP.md`, `PRODUCTION_READINESS_ACTION_PLAN.md` |
| `FIRST_PAID_PILOT_GAP_MATRIX.md` | **Not present** |
| `FIRST_PAID_PILOT_BUILD_PLAN.md` | **Not present** |
| `FIRST_PAID_PILOT_GO_LIVE_CHECKLIST.md` | **Created/updated in this sprint** (see file) |
| `PILOT_SUPPORT_RUNBOOK.md` | **Not present** — used `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md`, `docs/RUNBOOK.md` |
| `PILOT_DATA_EXIT_PLAN.md` | **Not present** — used `docs/DATA_PROTECTION.md`, `BACKUP_AND_RECOVERY_RUNBOOK.md` |
| Deployment / backup / security | `docs/PRODUCTION_DEPLOYMENT.md`, `BACKUP_AND_RECOVERY_RUNBOOK.md`, `RESTORE_DRILL_PROCEDURE.md`, `PRODUCTION_SECRETS_SETUP.md`, `infra/azure/main.bicep` |

**Terminology mapping (sprint brief vs this product):** “Firm” → **Tenant** (care home operator). Database is **SQL Server**, not PostgreSQL. There are no separate expense/income/bank-transaction modules; pilot financial data is **invoices, invoice lines, credit notes, misc charges, reports, and invoice PDFs**.

---

## P0 execution matrix

| P0 item | Initial state | Action performed | Evidence | Result | Remaining risk |
|---------|---------------|------------------|----------|--------|----------------|
| Production-like hosting configuration | Bicep defines App Service (HTTPS-only, TLS 1.2), Azure SQL S0+, Key Vault, Azure Files, RS vault for file backup | Code review of `infra/azure/main.bicep`; no Azure subscription deploy in sprint | `infra/azure/main.bicep` (webApp `httpsOnly`, `minTlsVersion`, SQL `minimalTlsVersion`, storage `supportsHttpsTrafficOnly`) | **EXTERNAL ACTION REQUIRED** (provision + configure App Service settings / Key Vault refs) | Pilot host not proven until deploy + smoke |
| TLS / HTTPS readiness | App uses `UseHttpsRedirection` + HSTS when not Development and `Https:Redirect=true` | Code review; Bicep `httpsOnly: true` | `Program.cs`, `appsettings.Production.json`, Bicep outputs `webAppUrl` as `https://` | **PASS** (design) / **EXTERNAL** (live cert + DNS on pilot URL) | HTTP-only misconfiguration on custom host |
| Production secrets | Empty placeholders in `appsettings.Production.json`; validators in `ProductionStartupValidator`, `JwtSigningKey` | Grep for committed secrets; `dotnet test` hardening tests; Production container start without SMTP | `appsettings.Production.json`, `artifacts/sprint0/dotnet-test-2026-09-20.log`, Docker run stderr (email fail-fast) | **PASS** (fail-safe behaviour) | Secrets must be set in Key Vault / host before go-live |
| Real SMTP | No SMTP credentials in environment | `ProductionStartupValidator.ValidateEmail` blocks non-Smtp in Production unless explicit simulation flag | Docker: `Production requires Email:Mode=Smtp...` on startup | **PASS** (config gate) / **EXTERNAL** (credentials + delivery test) | No live email until SMTP provisioned |
| Database backup | Scripts + Azure PITR documented; local Docker DB | `BACKUP DATABASE` inside `carehome-sql` to `sprint0-carehome.bak` | SQL output: “BACKUP DATABASE successfully processed 1314 pages” | **PASS** (disposable local) / **EXTERNAL** (Azure PITR + `Verify-BackupReadiness.ps1` on pilot RG) | Production-tier backup not executed without Azure |
| Document backup | Docker volume `carehome-documents` | `docker cp` from `carehome-api` to `artifacts/sprint0/documents-backup-v2/` (2 PDFs) | File paths under `artifacts/sprint0/documents-backup-v2/tenants/...` | **PASS** (local) / **EXTERNAL** (Azure Files backup job + vault) | Daily Azure Files protection requires post-Bicep script |
| Automated backup scheduling | Azure platform + `Enable-AzureFileBackup.ps1` documented | No cron/Task Scheduler on this machine; Azure not invoked | `BACKUP_AND_RECOVERY_RUNBOOK.md`, `scripts/Enable-AzureFileBackup.ps1` | **EXTERNAL ACTION REQUIRED** | RPO for documents depends on schedule |
| Restore procedure | Documented in `RESTORE_DRILL_PROCEDURE.md` | Restored `CareHomeRestoreCheck` from `.bak`; did **not** touch `CareHomeDb` | `artifacts/sprint0/restore-drill-validation.json` | **PASS** (local drill) | Azure PITR restore not run without `az` + RG |
| Actual restore drill | Previously LocalDB-only per launch report | Full drill: baseline counts → backup → restore → API on restored DB → login | `restore-drill-baseline.json`, `restore-drill-validation.json`, login on `:5093` | **PASS** (disposable) / **EXTERNAL** (production SQL tier drill) | Finance spot-check on pilot host still open |
| Runtime tenant-isolation verification | Only `ForTenant()` unit tests; UAT scripts historically | `dotnet test` 29/29; **no** HTTP integration test project | `artifacts/sprint0/dotnet-test-2026-09-20.log`, `TenantIsolationTests.cs` | **FAIL** (HTTP/runtime integration) / **PASS** (unit tests executed) | Cross-tenant ID leakage undetected by CI |
| Production-like integration tests | No Testcontainers/HTTP suite | Ran existing unit test assembly only | Same log file | **PASS** (tests run) with **coverage gap** | Billing/tenant 404 not automated |
| Minimum monitoring | `/health/live`, `/health/ready` implemented | `Invoke-RestMethod` → `Healthy` on `:5092` | Health JSON with `correlationId` | **PASS** (endpoints) / **EXTERNAL** (hosted probe) | No probe configured until deploy |
| Minimum alerting | Documented in `docs/OPERATIONS_CHECKLIST.md` only | No IaC alert rules or App Insights in repo | Ops checklist table | **FAIL** (not wired) | Silent outage until manual checks |
| Release rehearsal | Docker Compose dev path | `docker compose build api` succeeded; health OK on running stack | Docker build output | **PASS** | Azure slot swap not rehearsed |
| Release / rollback dry run | EF rollback limited | Build + start validated; rollback = redeploy previous image / SQL restore (documented) | `docs/PRODUCTION_DEPLOYMENT.md`, drill restore | **PASS** (procedure) / **PARTIAL** (no Azure slot) | Failed migration needs restore not `Down()` |

---

## Restore drill evidence (disposable Docker)

| Check | Expected | Observed |
|-------|----------|----------|
| Tenants | 5 | 5 |
| Clients | 4 | 4 |
| Invoices | 2 | 2 |
| Sum invoice totals | 5203.57 | 5203.57 |
| Audit logs | 43 | 43 |
| Users | 5 | 5 |
| Admin login on restored DB | JWT issued | **PASS** (disposable API `CareHomeRestoreCheck`, port 5093) |
| Production `CareHomeDb` preserved | Unchanged | **PASS** (still 5 tenants) |
| Document binary | SHA-256 match after restore copy | **PASS** (`document-restore-hash.json`, `match: true`) |
| HTTP PDF download on restore instance | Optional | **Not executed** (file hash + metadata used) |

---

## Tenant isolation — tests executed

```
dotnet test CareHome.Api.Tests/CareHome.Api.Tests.csproj
→ 29 passed, 0 failed (2026-09-20)
```

**Missing high-risk HTTP/runtime coverage (document only — no auth redesign in sprint):**

| Area | Automated HTTP cross-tenant 404 test |
|------|--------------------------------------|
| Clients | No |
| Users | No |
| Documents / invoice PDF | No |
| Invoices / credit notes | No (UAT script history only) |
| Expenses / income / bank tx | **N/A** (not in domain) |
| Bank accounts | **N/A** (template snapshot fields only) |
| Reconciliation | **N/A** |
| Requests / periods | Partial via billing UAT, not CI |
| Reports | No |
| Audit | No |
| Downloads (Sage CSV, PDF) | No |

**Note:** Sprint brief referenced PostgreSQL-backed isolation tests; **this codebase uses SQL Server** and has **no** PostgreSQL integration test project.

---

## SMTP and secrets

| Check | Result |
|-------|--------|
| Production rejects Development/log-only email without `Email__AllowSimulationInProduction` | **PASS** (container exit + exception message) |
| Production requires SMTP host/from when `Email__Mode=Smtp` | **PASS** (code: `ProductionStartupValidator.ValidateEmail`) |
| Real end-to-end email | **EXTERNAL ACTION REQUIRED — SMTP CREDENTIALS** |
| JWT / DB / SMTP / storage secrets in git | **PASS** — `appsettings.Production.json` uses empty strings; dev passwords only in `appsettings.Development.json` and docs |
| Missing critical secrets fail startup | **PASS** (JWT + email validated outside Development) |
| AI secrets | **N/A** (no AI integration in scope) |

---

## TLS and cookies

| Check | Result |
|-------|--------|
| TLS termination on Azure App Service | **PASS** (platform HTTPS; `httpsOnly: true`) |
| Live pilot certificate | **EXTERNAL** |
| Secure auth cookies | **PASS (N/A)** — authentication is **JWT Bearer** (client `localStorage`); no session cookies to mark `Secure` (see P1 XSS note in launch report) |

---

## Monitoring and alerts (pilot minimum)

| Alert intent | What failed? | How do I know? | What do I do? | Status |
|--------------|--------------|----------------|---------------|--------|
| Application unavailable | API down | `GET /health/live` non-200 | Restart App Service; check logs | **EXTERNAL** — configure probe |
| Database unavailable | SQL down | `GET /health/ready` unhealthy | Fix SQL / firewall / connection string | **EXTERNAL** |
| Disk / document storage | PDF writes fail | Free space on mount | Expand Azure Files / clear orphans | **EXTERNAL** |
| Backup failure | No PITR / no file backup | `Verify-BackupReadiness.ps1` or Portal | Re-run `Enable-AzureFileBackup.ps1`; open Azure support | **FAIL** (not automated) |
| Email failure | Invoices not sent | `EmailSendLogs` `Success=0` | Fix SMTP; resend manually | **EXTERNAL** |
| High 5xx rate | Exceptions | App logs / App Insights | Roll back release | **FAIL** (no App Insights in IaC) |

---

## Release rehearsal

| Step | Result |
|------|--------|
| Build API image | **PASS** (`docker compose build api`) |
| Migrations | Existing DB; drill used `Database__ApplyMigrations=false` on restore instance |
| Start + health | **PASS** (`/health/ready` Healthy on `:5092` and restore drill `:5093`) |
| Smoke login | **PASS** (`/api/auth/login` on dev and restore-drill instances) |
| Rollback | **PASS** (documented: redeploy prior artefact; DB rollback = restore not EF `Down()`) |

---

## Evidence files produced

| Path |
|------|
| `artifacts/sprint0/restore-drill-baseline.json` |
| `artifacts/sprint0/restore-drill-validation.json` |
| `artifacts/sprint0/document-restore-hash.json` |
| `artifacts/sprint0/dotnet-test-2026-09-20.log` |
| `artifacts/sprint0/documents-backup-v2/` (2 invoice PDFs) |
| `artifacts/sprint0/documents-restore/` (post-restore copy) |
| SQL backup: `/var/opt/mssql/backup/sprint0-carehome.bak` in container `carehome-sql` |
| Restored DB: `CareHomeRestoreCheck` on same container (disposable) |

---

## Sprint verdict

**SPRINT 0 CONDITIONALLY COMPLETE**

Safety mechanics are implemented and **proven locally** (backup, restore, fail-fast production config, health). **Production pilot host**, **Azure backup verification**, **SMTP delivery**, **alerting**, and **HTTP tenant-isolation integration tests** remain open.

**Exact next action:** Provision Azure pilot stack per `infra/azure/main.bicep`, run `scripts/Deploy-Azure.ps1` with `Email__Mode=Smtp` and Key Vault secrets, execute `scripts/Verify-BackupReadiness.ps1` and a **production-tier** restore drill per `RESTORE_DRILL_PROCEDURE.md`, then configure health probes and backup-failure alerts per `docs/OPERATIONS_CHECKLIST.md`.

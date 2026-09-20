# First Paid Pilot — Go-Live Checklist

**Product:** Care Home Back-Office Management System  
**Last updated:** 20 September 2026 (Sprint 0 evidence pass)  
**Rule:** Mark complete only when **evidence exists** (not when config/scripts exist only).

---

## Infrastructure and security

| # | Item | Complete | Evidence |
|---|------|----------|----------|
| 1 | Production App Service + Azure SQL + document storage provisioned | ☐ | Bicep deploy outputs + Portal screenshot / deploy log |
| 2 | TLS HTTPS on pilot URL | ☐ | Browser shows valid cert; `https://` only |
| 3 | `httpsOnly` / HSTS path verified | ☑ | Bicep + `Program.cs`; Sprint 0 code review |
| 4 | Production secrets in Key Vault (not git) | ☑ | Empty `appsettings.Production.json`; `../operations/PRODUCTION_SECRETS_SETUP.md` |
| 5 | JWT key ≥32 mixed chars configured | ☐ | App starts in Production on pilot host |
| 6 | SQL connection string (non-LocalDB) configured | ☐ | `/health/ready` on pilot |
| 7 | Bootstrap `Seed__*` removed after first admin | ☐ | App Service settings screenshot (redacted) |
| 8 | CORS origins correct (no localhost in Production) | ☐ | Pilot SPA login from production origin |

## Email

| # | Item | Complete | Evidence |
|---|------|----------|----------|
| 9 | `Email__Mode=Smtp` on pilot | ☐ | App Service setting |
| 10 | Production rejects simulated email without explicit flag | ☑ | Sprint 0 Docker Production start fail-fast log in `SPRINT_0_PILOT_SAFETY_EXECUTION_REPORT.md` |
| 11 | Real SMTP credentials configured | ☐ | **EXTERNAL** — Key Vault `Email-Smtp-Password` |
| 12 | End-to-end test message delivered | ☐ | **EXTERNAL** — received test email + `EmailSendLogs` row |

## Backup and restore

| # | Item | Complete | Evidence |
|---|------|----------|----------|
| 13 | Azure SQL automated backup / PITR active (S0+) | ☐ | `Verify-BackupReadiness.ps1` PASS on pilot RG |
| 14 | Document storage backup (Azure Files + RS vault) | ☐ | Vault backup item + `Enable-AzureFileBackup.ps1` run log |
| 15 | Automated backup schedule operational | ☐ | Portal policy / daily job success |
| 16 | Coordinated pre-migration backup (`Backup-CareHome.ps1`) | ☐ | Manifest JSON from pilot or on-prem |
| 17 | Restore drill on **production-tier** SQL | ☐ | `Verify-RestoreDrill.ps1` report signed |
| 18 | Restore drill — disposable local SQL Server | ☑ | `artifacts/sprint0/restore-drill-validation.json` |
| 19 | Document restore verified | ☑ | `artifacts/sprint0/document-restore-hash.json` |
| 20 | Financial totals unchanged after restore | ☑ | Invoice sum 5203.57 baseline vs `CareHomeRestoreCheck` |

## Data safety and tenancy

| # | Item | Complete | Evidence |
|---|------|----------|----------|
| 21 | Tenant isolation unit tests executed | ☑ | `artifacts/sprint0/dotnet-test-2026-09-20.log` (29/29) |
| 22 | Tenant isolation HTTP integration tests | ☐ | **Not implemented** — see Sprint 0 gap list |
| 23 | Pilot data only in dedicated tenant (not migration placeholder tenant Id=1) | ☐ | Operator sign-off |
| 24 | Demo environment preserved for sales | ☑ | Sprint 0: `CareHomeDb` not dropped |

## Monitoring and operations

| # | Item | Complete | Evidence |
|---|------|----------|----------|
| 25 | Health probe on `/health/live` | ☐ | Monitor config URL |
| 26 | Readiness probe on `/health/ready` | ☑ | Sprint 0 local `Healthy` JSON |
| 27 | Backup-failure alert | ☐ | Alert rule + test notification |
| 28 | Application-unavailable alert | ☐ | Alert rule + test notification |
| 29 | Release rehearsal on pilot-like host | ☑ | Sprint 0 `docker compose build api` + health |
| 30 | Rollback procedure understood | ☑ | `../operations/RESTORE_DRILL_PROCEDURE.md` + deployment doc |

## Business gates (out of Sprint 0 engineering scope but P0 for live invoicing)

| # | Item | Complete | Evidence |
|---|------|----------|----------|
| 31 | Billing formula sign-off | ☐ | `docs/PRODUCTION_BUSINESS_SIGNOFF.md` APPROVED rows |
| 32 | Sage CSV import validated in target Sage company | ☐ | Finance checklist in `docs/SAGE50_EXPORT.md` |

---

## Deployment status

| Check | Required value |
|-------|----------------|
| Production deployed | **NO** (as of Sprint 0) |
| Application feature scope changed in Sprint 0 | **NO** |

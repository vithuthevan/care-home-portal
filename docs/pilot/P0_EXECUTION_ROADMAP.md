# P0 Execution Roadmap

**Date:** 15 September 2026  
**Application:** Care Home Back-Office Management System  
**Source plan:** `PRODUCTION_READINESS_ACTION_PLAN.md`  
**Review type:** Planning only — no code changes, no deployments  
**Scope:** Close all P0 production blockers before controlled pilot go-live with real financial or resident data

---

## Overview

This roadmap maps each P0 blocker to the current repository implementation and defines the exact work required to close it. Items are ordered by recommended execution sequence. Several items can run in parallel once owners are assigned.

| # | Blocker | Primary owner | Depends on |
|---|---------|---------------|------------|
| 1 | Business approval (billing, invoice calculation, Sage mapping) | Business / Finance | — |
| 2 | Production secrets management | DevOps (+ Developer for bootstrap) | — |
| 3 | Automated database backups | DevOps | Production SQL host provisioned |
| 4 | Database restore verification | DevOps (+ Developer for smoke) | #3 (at least one backup exists) |
| 5 | Production email configuration | DevOps (+ Business for mailbox) | #2 (SMTP secrets) |
| 6 | Tagged release build process | Developer | WIP reconciled on pilot branch |

**Estimated calendar time (parallel work):** 1–2 weeks  
**Hard gate before live funder invoicing:** Items 1, 4, 5, and 6 must be complete. Items 2–3 must be complete before first production deploy.

---

## P0-1: Business Approval — Billing Rules, Invoice Calculation, Sage Mapping

### Current State

**Billing and invoice calculation rules are implemented but not stakeholder-approved.**

| Area | What exists today | Relevant files / docs |
|------|-------------------|------------------------|
| Weekly proration | `(weekly rate / 7) × inclusive eligible days`, rounded to 2 dp | `backend/CareHome.Api/Billing/RateCalculator.cs`, `backend/CareHome.Api/Common/Money.cs` |
| Monthly proration | Per calendar month: `(monthly amount / days-in-month) × eligible days`, summed and rounded | `backend/CareHome.Api/Billing/RateCalculator.cs` |
| Inclusive billing-day rule | Eligible period = request ∩ occupancy ∩ contract ∩ rate, minus finalized invoice coverage; day counts use inclusive dates | `backend/CareHome.Api/Common/DateRanges.cs`, `backend/CareHome.Api/Billing/BillingService.cs` |
| Invoice date / due date | Invoice date = `request.PeriodEnd`; due date = invoice date + `TenantSettings.PaymentTermsDays` (default 30) | `backend/CareHome.Api/Billing/BillingService.cs` (lines ~114–119, ~176–177) |
| Partial-period billing | Already-billed days excluded; remainder invoiced as separate lines | `DateRanges.Subtract`, `BillingService` |
| Business sign-off tracker | Four items all **PENDING** | `docs/PRODUCTION_BUSINESS_SIGNOFF.md` |
| Worked examples | £575/week × 31 days = £2,546.43/client; 3 clients = £7,639.29 (UAT evidence) | `docs/PRODUCTION_BUSINESS_SIGNOFF.md`, `docs/BACKUP_RESTORE.md` |
| Sage 50 CSV export | Provisional 8-column map; `T0` tax placeholder; department = care home code snapshot | `backend/CareHome.Api/Export/Sage50ColumnMap.cs`, `backend/CareHome.Api/Export/SageExportService.cs`, `backend/CareHome.Api/Controllers/SageExportsController.cs` |
| Sage sign-off checklist | Finance checklist exists but unchecked | `docs/SAGE50_EXPORT.md` |
| UAT coverage | Manual UAT PASS (Aug 2026) against implemented formulas — not a substitute for finance sign-off | `docs/UAT_FINAL_RETEST_REPORT.md`, `scripts/uat-*.ps1` |

### Gap

- No finance/operations approval recorded for weekly proration, monthly proration, inclusive day counting, or invoice-date rule.
- No confirmed import of a sample CSV into the **target Sage 50 company** used in production.
- `TaxCode` is hardcoded to `T0` — VAT treatment not agreed.
- `Sage50ColumnMap.cs` and `RateCalculator.cs` are explicitly marked **PROVISIONAL** in code comments.
- If any rule is **REJECTED**, replacement logic must be implemented, re-UAT'd, and re-signed before live invoicing.

### Required Action

**Phase A — Billing formula review (Finance + Operations, 3–5 business days)**

1. Schedule a sign-off session with finance and operations stakeholders.
2. Walk through each rule in `docs/PRODUCTION_BUSINESS_SIGNOFF.md` using worked examples:
   - Weekly: £575/week, 1–31 May 2026 → £2,546.43 per client.
   - Monthly: £2,000/month, 1–15 April → £1,000.00.
   - Inclusive day: single admission/discharge day = 1 billable day.
   - Invoice date = period end; due date = period end + payment terms.
3. For each item, record **APPROVED** or **REJECTED** with approver name and date in `docs/PRODUCTION_BUSINESS_SIGNOFF.md`.
4. If **REJECTED**: document the replacement rule, hand to Developer for implementation, schedule re-UAT (`docs/UAT_CHECKLIST.md` billing scenarios), then re-sign.

**Phase B — Sage mapping validation (Finance, 2–3 business days)**

1. In a **test tenant** on a non-production environment, generate invoices covering weekly, monthly, and partial-period scenarios.
2. Run `POST /api/sage-exports/preview` then `POST /api/sage-exports` for a date range including those invoices.
3. Download the CSV from `tenants/{publicId}/sage-exports/`.
4. Import into the **target Sage 50 company** (not Excel-only review).
5. Complete every checkbox in `docs/SAGE50_EXPORT.md` sign-off checklist.
6. Confirm: `AccountRef` = client Sage ID snapshot, `NominalCode` = line nominal snapshot, `Department` = care home **code** snapshot (`HOME01`, not display name).
7. Record **APPROVED** or **REJECTED** for Sage mapping in `docs/PRODUCTION_BUSINESS_SIGNOFF.md`.

**Phase C — Gate decision**

- **Go:** All four sign-off rows are APPROVED → proceed to live funder invoicing.
- **No-go:** Any REJECTED item blocks live invoicing until code change + re-UAT + re-approval.

### Owner

| Sub-task | Owner |
|----------|-------|
| Formula review and approval decision | **Business** / **Finance** |
| Sage CSV generation in test tenant | **Developer** (support) |
| Sage 50 import and posting validation | **Finance** |
| Code changes if rules rejected | **Developer** |
| Re-UAT after formula changes | **Developer** + **Business** |

### Priority

**P0**

### Completion Criteria

- [ ] `docs/PRODUCTION_BUSINESS_SIGNOFF.md` shows **APPROVED** for: Weekly proration, Monthly proration, Inclusive billing-day rule, Sage mapping.
- [ ] Each APPROVED row has approver name and approval date filled in.
- [ ] Sage sign-off checklist in `docs/SAGE50_EXPORT.md` is fully checked with finance signatory and date.
- [ ] A sample Sage CSV from the target environment was successfully imported into the production Sage 50 company with correct account, nominal, department, and amount postings.
- [ ] No REJECTED items remain open without a documented replacement plan and re-UAT schedule.

### Risk if Not Completed

Incorrect invoice amounts billed to funders; contractual disputes; finance audit exposure; Sage postings to wrong accounts/nominals/departments; manual reconciliation rework; inability to defend billing methodology in a regulatory or contractual review. A 31-day month at £575/week bills £2,546.43 per client — not 4× weekly (£2,300).

---

## P0-2: Production Secrets Management

### Current State

| Secret / config | What exists today | Relevant files / docs |
|-----------------|-------------------|------------------------|
| JWT signing key | Empty in `appsettings.json`; fail-fast validation outside Development | `backend/CareHome.Api/appsettings.json`, `backend/CareHome.Api/Security/JwtSigningKey.cs`, `backend/CareHome.Api/Program.cs` |
| Database connection | LocalDB default in appsettings; Production rejects LocalDB | `appsettings.json`, `backend/CareHome.Api/Security/ProductionStartupValidator.cs` |
| SMTP credentials | Empty host/user/password; `Email:Mode=Development` default | `appsettings.json`, `backend/CareHome.Api/Email/ConfigurableEmailSender.cs` |
| Bootstrap admin | `Seed:AdminEmail` / `Seed:AdminPassword` empty; dev credentials blocked in Production | `backend/CareHome.Api/Security/IdentitySeeder.cs`, `ProductionStartupValidator.ValidateProductionSeed` |
| Env var mapping | Fully documented with `__` nesting convention | `docs/PRODUCTION_CONFIGURATION.md` |
| Azure deploy script | Generates JWT + SQL password at deploy time; writes plain App Service settings (no Key Vault) | `scripts/Deploy-Azure.ps1` (lines ~95–107, ~172–199) |
| Azure IaC | No Key Vault, no connection string or JWT in Bicep outputs | `infra/azure/main.bicep` |
| Dev Docker secrets | `.env.example` has dev-only SA password | `.env.example`, `docker-compose.yml` |
| Startup validation | Rejects LocalDB, weak JWT, dev bootstrap, incomplete SMTP when `Email__Mode=Smtp`, localhost CORS | `ProductionStartupValidator.cs`, `CareHome.Api.Tests/ProductionHardeningTests.cs` |
| Post-bootstrap cleanup | `Deploy-Azure.ps1` removes `Seed__*` settings after first deploy | `scripts/Deploy-Azure.ps1` (lines ~246–251) |

**Required Production environment variables (minimum):**

| Variable | Secret? |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT=Production` | No |
| `ConnectionStrings__DefaultConnection` | **Yes** |
| `Jwt__Key` (≥32 mixed chars) | **Yes** |
| `Email__Smtp__Password` (if SMTP auth required) | **Yes** |
| `Seed__AdminPassword` (first bootstrap only) | **Yes** |
| `Email__Mode`, `Email__Smtp__Host`, `Email__FromAddress`, `DocumentStorage__RootPath`, `Cors__AllowedOrigins__*` | No (but required for correct behaviour) |

### Gap

- No production secrets store provisioned (Azure Key Vault or equivalent).
- Secrets are not configured on any production host — application would fail JWT validation or use simulated email.
- `Deploy-Azure.ps1` stores secrets as plain App Service settings (acceptable for pilot bootstrap, not for GA).
- No documented secret inventory, rotation schedule, or access-control list for operators.
- No separate SQL **application login** — deploy script uses SQL admin account in connection string.
- Bootstrap credentials may be generated but not recorded in a secure vault before `Seed__*` removal.

### Required Action

**Step 1 — Choose secrets store and document inventory (DevOps)**

1. Provision Azure Key Vault (or equivalent: Windows DPAPI, Kubernetes Secrets, HashiCorp Vault).
2. Create a secrets inventory spreadsheet/register listing: secret name, purpose, rotation frequency, who can read/write, last rotated.
3. Map each secret to its environment variable name per `docs/PRODUCTION_CONFIGURATION.md`.

**Step 2 — Generate and store secrets (DevOps)**

1. Generate `Jwt__Key`: ≥32 mixed characters, not the development placeholder (`DEVELOPMENT-ONLY-CHANGE-ME-TO-A-LONG-SECRET-KEY`).
2. Create production SQL database and a dedicated **application login** with `db_datareader`, `db_datawriter` (migrations use a separate elevated account).
3. Build `ConnectionStrings__DefaultConnection` per documented format (Encrypt=True, TrustServerCertificate=False).
4. Store JWT key, SQL password, and future SMTP password in the secrets store.

**Step 3 — Configure production host (DevOps)**

1. Set all required app settings on the target host (App Service, IIS, systemd) referencing Key Vault where possible (`@Microsoft.KeyVault(SecretUri=...)` on Azure).
2. Set `DocumentStorage__RootPath` to the production document volume (`/home/carehome-documents` on Azure App Service with Azure Files mount per `infra/azure/main.bicep`).
3. Leave `Cors__AllowedOrigins` empty for same-origin deploy; set explicit HTTPS origin if SPA is split-hosted.

**Step 4 — Bootstrap PlatformAdmin once (Developer + DevOps)**

1. On a fresh production database (after migrations), set `Seed__AdminEmail` and `Seed__AdminPassword` to unique non-dev values.
2. Start API once; confirm PlatformAdmin login works.
3. **Immediately remove** `Seed__AdminEmail` and `Seed__AdminPassword` from host configuration (automated in `Deploy-Azure.ps1`).
4. Record bootstrap credentials in the secrets store before removal if not already done.

**Step 5 — Verify fail-fast behaviour (Developer)**

1. Confirm API starts with all secrets set → `/health/ready` returns Healthy.
2. Confirm API **refuses to start** if `Jwt__Key` is missing, weak, or dev placeholder (see `scripts/uat-tc306-334.ps1` TC-306/307 patterns).
3. Confirm LocalDB connection string is rejected in Production.

### Owner

| Sub-task | Owner |
|----------|-------|
| Key Vault / secrets store provisioning | **DevOps** |
| SQL login creation and connection string | **DevOps** |
| JWT key generation and storage | **DevOps** |
| App settings / Key Vault references on host | **DevOps** |
| Bootstrap admin first login verification | **Developer** |
| Fail-fast validation smoke | **Developer** |

### Priority

**P0**

### Completion Criteria

- [ ] All required secrets exist in a managed secrets store — not in source control.
- [ ] Production host starts successfully with `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] `GET /health/ready` returns `Healthy` against production SQL.
- [ ] `Jwt__Key` is ≥32 characters, not the development placeholder, stored only in the secrets store and host config/Key Vault reference.
- [ ] `ConnectionStrings__DefaultConnection` points to production SQL Server (not LocalDB).
- [ ] PlatformAdmin exists and `Seed__AdminEmail` / `Seed__AdminPassword` are **removed** from host config.
- [ ] Secrets inventory document exists with owner, rotation date, and access list.
- [ ] No production secrets committed to git (verify `.gitignore` excludes `.env`).

### Risk if Not Completed

Application fails to start; weak or missing JWT enables token forgery; SQL credentials exposed in plain App Service settings or logs; no admin access to provision tenants; bootstrap password left on host enables unauthorized PlatformAdmin access; compliance audit failure for secrets handling.

---

## P0-3: Automated Database Backups

### Current State

| Area | What exists today | Relevant files / docs |
|------|-------------------|------------------------|
| Backup procedure | Documented manual SQL `BACKUP ... WITH CHECKSUM` + `robocopy` for documents | `docs/BACKUP_RESTORE.md` |
| Restore evidence | Verified on LocalDB disposable DBs (Aug 2026) — not production-tier SQL | `docs/BACKUP_RESTORE.md`, `scripts/uat-tc306-334.ps1` (TC-313–321) |
| Document storage | PDFs, credit-note PDFs, Sage CSVs under `DocumentStorage:RootPath`; **not included in SQL backup** | `docs/PRODUCTION_DEPLOYMENT.md`, `infra/azure/main.bicep` (Azure Files mount) |
| Pre-deploy backup | Documented as mandatory before every migration | `docs/PRODUCTION_DEPLOYMENT.md` |
| IaC automation | **No** scheduled backup jobs, no Azure Backup vault, no SQL automated backup policy beyond Basic tier defaults | `infra/azure/main.bicep` |
| Azure SQL Basic tier | Bicep provisions Basic tier — limited PITR compared to Standard+ | `infra/azure/main.bicep` (sqlDatabase sku) |
| RPO/RTO | Suggested schedule in docs; not agreed with legal/finance | `docs/BACKUP_RESTORE.md` |

**What must be backed up:**

1. SQL database — tenants, users, clients, contracts, invoices, credits, audit, sequences.
2. Document storage root — `tenants/{publicId}/invoices|credit-notes|sage-exports/` (Sage CSVs and logos are **not** recoverable from SQL alone).

### Gap

- No scheduled backup job exists in IaC, cron, Azure Automation, or SQL Agent.
- No off-site copy of backups.
- Document storage backup is not coupled to the SQL backup job (risk of point-in-time mismatch).
- No backup monitoring or alert on job failure.
- Azure SQL Basic tier may not meet RPO requirements — needs tier review with finance.
- No backup taken immediately before first production migration.

### Required Action

**Step 1 — Agree RPO/RTO with stakeholders (Business + DevOps)**

1. Define Recovery Point Objective (how much data loss is acceptable) and Recovery Time Objective (how fast to restore).
2. Document agreed values in ops runbook (`docs/RUNBOOK.md` or operator-specific doc).

**Step 2 — SQL automated backups (DevOps)**

*Option A — Azure SQL (recommended for Azure deploy):*

1. Evaluate upgrading from Basic to Standard S1+ for automated backups and point-in-time restore.
2. Enable Azure SQL automated backups (enabled by default on paid tiers; verify retention period — recommend ≥7 days, 35 for production).
3. Document how to restore via Azure Portal / `az sql db restore`.

*Option B — Self-hosted SQL Server:*

1. Create SQL Agent job (or Windows Task Scheduler + `sqlcmd`) for daily full backup:
   ```sql
   BACKUP DATABASE [CareHome]
   TO DISK = N'D:\backups\CareHome_daily.bak'
   WITH CHECKSUM, INIT, STATS = 10;
   ```
2. If FULL recovery model: add hourly transaction-log backups during business hours.
3. Add pre-migration `COPY_ONLY` backup step to deploy runbook.

**Step 3 — Document storage backup (DevOps)**

1. Schedule daily copy/snapshot of `DocumentStorage__RootPath`:
   - Azure: Azure Files snapshot or `az storage file upload-batch` to geo-redundant storage.
   - On-prem: `robocopy` to backup volume per `docs/BACKUP_RESTORE.md`.
2. **Couple timing:** run document backup in the same window as SQL backup (±15 minutes).

**Step 4 — Off-site copy (DevOps)**

1. Copy SQL `.bak` files and document snapshots to geo-redundant / off-site storage daily.
2. Encrypt backups in transit and at rest.

**Step 5 — Monitoring and deploy integration (DevOps)**

1. Alert on backup job failure (email, Teams, or monitoring platform).
2. Add "verify last backup < 24h old" to `docs/OPERATIONS_CHECKLIST.md` daily checks.
3. Mandate pre-migration backup in deploy sequence (`docs/PRODUCTION_DEPLOYMENT.md` step 1).

### Owner

| Sub-task | Owner |
|----------|-------|
| RPO/RTO agreement | **Business** / **Finance** |
| SQL backup job / Azure tier decision | **DevOps** |
| Document storage backup job | **DevOps** |
| Off-site copy and encryption | **DevOps** |
| Backup failure alerting | **DevOps** |
| Pre-migration backup gate in deploy process | **DevOps** |

### Priority

**P0**

### Completion Criteria

- [ ] Daily automated SQL backup job is scheduled and has run successfully at least once on the **production SQL instance**.
- [ ] Daily automated document storage copy/snapshot is scheduled and has run successfully at least once.
- [ ] At least one off-site copy of SQL backup + documents exists.
- [ ] Backup job failure triggers an alert to an operations channel.
- [ ] RPO/RTO documented and agreed with business.
- [ ] Pre-migration backup step is mandatory in the deploy runbook and was executed before first production migration.
- [ ] Backup retention period is documented (operator/legal decision).

### Risk if Not Completed

Total loss of financial records, resident PII, invoice PDFs, and Sage export files on corruption, ransomware, hardware failure, or failed migration. Invoice PDFs can regenerate from snapshots, but Sage CSVs and logo files cannot. No rollback path after a bad deploy.

---

## P0-4: Database Restore Verification

### Current State

| Area | What exists today | Relevant files / docs |
|------|-------------------|------------------------|
| Restore procedure | Documented: restore to **different** DB name, `MOVE` clauses, parallel document root | `docs/BACKUP_RESTORE.md` |
| LocalDB verification | PASS Aug 2026: login, client, contract, invoice (£7,639.29), credit note, audit | `docs/BACKUP_RESTORE.md` |
| UAT restore scripts | TC-313–321 in `scripts/uat-tc306-334.ps1` — disposable LocalDB only | `scripts/uat-tc306-334.ps1` |
| Verification checklist | 5-step post-restore smoke (health, login, entities, PDF, cross-tenant 404) | `docs/BACKUP_RESTORE.md` |
| Azure SQL restore | **Not tested** on production-tier host | — |
| Production host restore | **Not performed** | — |

### Gap

- Restore confidence is based on LocalDB only — not the target production SQL Server (Azure SQL or on-prem).
- Azure SQL restore differs from on-prem tooling (no `MOVE` to local disk; uses Azure restore APIs).
- Document storage restore not verified on production Azure Files mount.
- No signed restore drill report for production environment.
- Operators may not know Azure-specific restore steps.

### Required Action

**Step 1 — Prerequisites (DevOps)**

1. Complete P0-3: at least one production-tier backup exists (SQL + documents).
2. Provision an isolated restore target:
   - Azure: restore to `CareHomeRestoreCheck` on same server (or geo-replica).
   - On-prem: separate database name per `docs/BACKUP_RESTORE.md`.

**Step 2 — SQL restore drill (DevOps)**

1. Restore latest production backup to `CareHomeRestoreCheck` (never overwrite production).
2. For Azure SQL:
   ```bash
   az sql db restore --dest-name CareHomeRestoreCheck --name CareHome --resource-group <rg> --server <server> --time "<utc-timestamp>"
   ```
   Or restore from `.bacpac` / backup file per Azure docs.
3. For on-prem: follow `RESTORE DATABASE ... WITH MOVE` procedure in `docs/BACKUP_RESTORE.md`.

**Step 3 — Document storage restore (DevOps)**

1. Restore document backup to a parallel folder (e.g. `D:\backups\carehome-documents-restore` or separate Azure Files share).
2. Point a **non-production** API instance at restored DB + restored document root:
   ```text
   ConnectionStrings__DefaultConnection=...Database=CareHomeRestoreCheck...
   DocumentStorage__RootPath=<restored-document-path>
   ```

**Step 4 — Application verification (Developer + Business)**

Execute every check in `docs/BACKUP_RESTORE.md`:

| # | Check | Expected |
|---|-------|----------|
| 1 | `GET /health/ready` | `Healthy` |
| 2 | Login as known tenant user | Success |
| 3 | Open client, funding contract, invoice (header + lines), credit note, audit | Data matches pre-backup |
| 4 | Download invoice PDF | File present or regenerated from snapshots |
| 5 | Cross-tenant invoice access | 404 (tenant isolation intact) |

**Step 5 — Document results (DevOps)**

1. Record restore drill date, backup used, restore duration, verification results, and any issues.
2. Store report in ops documentation (e.g. `docs/BACKUP_RESTORE.md` appendix or operator wiki).
3. Schedule periodic re-drill (recommend: quarterly, minimum before pilot go-live).

### Owner

| Sub-task | Owner |
|----------|-------|
| SQL restore execution | **DevOps** |
| Document storage restore | **DevOps** |
| API smoke verification (health, login, entities, PDF) | **Developer** |
| Invoice total / financial spot-check | **Finance** |
| Cross-tenant isolation check | **Developer** |
| Restore drill report | **DevOps** |

### Priority

**P0**

### Completion Criteria

- [ ] Full restore drill completed on the **production-tier SQL host** (Azure SQL or target on-prem instance) — not LocalDB.
- [ ] Restored database name is different from production (e.g. `CareHomeRestoreCheck`).
- [ ] Document storage restored to parallel path and paired with SQL restore.
- [ ] All 5 verification checks in `docs/BACKUP_RESTORE.md` pass.
- [ ] Invoice totals on restored DB match pre-backup values (finance spot-check).
- [ ] Signed restore drill report exists with date, participants, and outcome.
- [ ] Restore procedure updated with any production-host-specific notes (Azure vs on-prem).

### Risk if Not Completed

False confidence in disaster recovery; extended outage if restore fails on real host due to path, permission, edition, or Azure-specific issues; inability to rollback a failed migration; financial data loss undiscovered until an actual incident.

---

## P0-5: Production Email Configuration

### Current State

| Area | What exists today | Relevant files / docs |
|------|-------------------|------------------------|
| Email sender | `ConfigurableEmailSender` — `Smtp` mode sends via `System.Net.Mail`; any other mode simulates | `backend/CareHome.Api/Email/ConfigurableEmailSender.cs` |
| Default mode | `Email:Mode=Development` in appsettings and Bicep | `appsettings.json`, `infra/azure/main.bicep` (line 117–118) |
| Deploy script | Explicitly sets `Email__Mode=Development` on App Service | `scripts/Deploy-Azure.ps1` (line 177) |
| Startup validation | Logs **warning** for simulated email; **blocks startup** only if `Mode=Smtp` but host/from missing | `ProductionStartupValidator.ValidateEmail` |
| Simulated send behaviour | Returns `Success=true, Simulated=true` — invoice send appears to succeed | `ConfigurableEmailSender.cs` (lines 20–29) |
| SMTP config surface | `Email__Smtp__Host`, `Port`, `User`, `Password`, `EnableSsl`, `FromAddress`, `FromName` | `docs/PRODUCTION_CONFIGURATION.md` |
| Invoice send flow | `POST /api/invoices/{id}/send` attaches PDF via email | `backend/CareHome.Api/Controllers/` (InvoicesController) |
| Send logging | `EmailSendLogs` table records attempts | Database model |
| Production docs | SMTP required for live email; simulation acceptable only with explicit operator acknowledgment | `docs/PRODUCTION_DEPLOYMENT.md`, `docs/PRODUCTION_CONFIGURATION.md` |

### Gap

- Production deploy path (`Deploy-Azure.ps1`, `main.bicep`) configures simulated email — funders receive nothing.
- No production SMTP mailbox provisioned or credentials stored.
- No end-to-end test of invoice email delivery from production host.
- Operators may believe invoices were sent when only a log entry was created.
- Alternative workflow (manual send outside system) is not documented or trained.

### Required Action

**Choose one path — both require explicit business decision:**

#### Path A — Live SMTP (recommended for pilot with funder invoicing)

1. **Business:** Provision a billing mailbox (e.g. `billing@operator-domain.org`) with SMTP relay access.
2. **DevOps:** Store SMTP credentials in secrets store (P0-2).
3. **DevOps:** Configure production host:
   ```text
   Email__Mode=Smtp
   Email__FromAddress=billing@operator-domain.org
   Email__FromName=Care Home Billing
   Email__Smtp__Host=smtp.<provider>
   Email__Smtp__Port=587
   Email__Smtp__User=<smtp-user>
   Email__Smtp__Password=<from-secrets-store>
   Email__Smtp__EnableSsl=true
   ```
4. **DevOps:** Update `scripts/Deploy-Azure.ps1` and/or `infra/azure/main.bicep` to set `Email__Mode=Smtp` (or remove the `Development` override so host config prevails).
5. **Developer:** Send a test invoice email from production (or staging with production SMTP) to a monitored mailbox.
6. **Business:** Confirm receipt: correct from address, PDF attached, subject/body from template.

#### Path B — Simulated email with manual send workflow (interim only)

1. **Business:** Explicitly accept that the system will **not** deliver emails.
2. **Operations:** Document manual workflow: generate invoice → download PDF from UI → send via Outlook/external mail.
3. **Business:** Train operators on the manual process.
4. **DevOps:** Ensure startup logs prominently show `PRODUCTION EMAIL IS SIMULATED`.
5. **Not acceptable** for unattended live funder invoicing — treat as temporary.

**Regardless of path:**

1. Verify `ProductionStartupValidator` logs email mode at startup.
2. Add email delivery check to post-deploy smoke test (`docs/PRODUCTION_SMOKE_TEST.md`).

### Owner

| Sub-task | Owner |
|----------|-------|
| Billing mailbox provisioning | **Business** |
| SMTP credentials and host config | **DevOps** |
| Deploy script / Bicep email mode update | **DevOps** (+ **Developer** if script change needed) |
| Test invoice email send | **Developer** |
| Confirm funder-ready email delivery | **Business** |
| Manual send workflow (Path B only) | **Business** / **Operations** |

### Priority

**P0**

### Completion Criteria

- [ ] **Path A:** `Email__Mode=Smtp` on production host with all required SMTP settings.
- [ ] **Path A:** Test invoice email delivered to a real mailbox with PDF attachment; `EmailSendLogs` shows success, `Simulated=false`.
- [ ] **Path A:** Startup log confirms `Email mode is Smtp` (not simulated warning).
- [ ] **Path B (interim):** Written operator procedure for manual invoice delivery exists and is trained.
- [ ] **Path B (interim):** Business sign-off acknowledging simulated email until SMTP is configured.
- [ ] Deploy script and/or Bicep no longer force `Email__Mode=Development` without override.
- [ ] Post-deploy smoke test includes email verification step.

### Risk if Not Completed

Operators believe invoices were emailed; funders receive nothing; revenue collection delayed; reputational harm; `EmailSendLogs` show success (simulated) while no delivery occurred; pilot fails its primary business purpose.

---

## P0-6: Tagged Release Build Process

### Current State

| Area | What exists today | Relevant files / docs |
|------|-------------------|------------------------|
| Build script | `Publish-CareHome.ps1` — builds Angular, copies to API `wwwroot`, `dotnet publish` | `scripts/Publish-CareHome.ps1` |
| Deploy script | `Deploy-Azure.ps1` — provisions, migrates, deploys; no commit SHA recording | `scripts/Deploy-Azure.ps1` |
| CI/CD | **None** — no `.github/workflows`, no Azure DevOps pipeline | Repository root |
| Git state | Uncommitted WIP: client-profile changes, Docker files, `Program.cs`, etc. | Git status (Sep 2026) |
| Artifact output | `artifacts/carehome-api/` and `artifacts/carehome-api.zip` | `Publish-CareHome.ps1`, `Deploy-Azure.ps1` |
| Version traceability | No commit SHA, tag, or build number embedded in deploy artifact or logs | — |
| Rollback | Documented as redeploy previous publish folder | `docs/PRODUCTION_DEPLOYMENT.md` |
| Migration gate | `dotnet ef migrations has-pending-model-changes` documented but not automated | `docs/PRODUCTION_DEPLOYMENT.md` |

### Gap

- No git tag marks a known-good release commit.
- WIP changes on main/working branch would contaminate a pilot build.
- Deployed artifact cannot be traced to a specific commit SHA.
- No release branch or freeze policy for pilot.
- No build manifest (version, commit, date) stored with artifact.
- Cannot reliably roll back to a known artifact version.

### Required Action

**Step 1 — Reconcile WIP (Developer)**

1. Review uncommitted changes (client-profile, Docker, `Program.cs`, etc.).
2. Either commit completed work to main, or stash/exclude from pilot branch.
3. Ensure pilot branch builds cleanly:
   ```powershell
   .\scripts\Publish-CareHome.ps1
   ```

**Step 2 — Create pilot release branch and tag (Developer)**

1. Create a release branch from a known-good commit:
   ```bash
   git checkout -b release/pilot-1.0
   ```
2. Tag the release commit:
   ```bash
   git tag -a v1.0.0-pilot -m "Pilot release 1.0.0"
   ```
3. Record tag name and commit SHA in deploy log.

**Step 3 — Build artifact from tag (Developer)**

1. Checkout tag:
   ```bash
   git checkout v1.0.0-pilot
   ```
2. Build:
   ```powershell
   .\scripts\Publish-CareHome.ps1
   ```
3. Verify no pending model changes:
   ```powershell
   cd backend\CareHome.Api
   dotnet ef migrations has-pending-model-changes
   ```
4. Store artifact with manifest:
   ```text
   artifacts/carehome-api/
   artifacts/RELEASE_MANIFEST.txt   # tag, commit SHA, build date, builder
   ```

**Step 4 — Deploy from tagged artifact (DevOps)**

1. Deploy only from the tagged build — not from an uncommitted working tree.
2. Record in deploy log:
   - Git tag (e.g. `v1.0.0-pilot`)
   - Commit SHA (`git rev-parse HEAD`)
   - Build timestamp
   - Deployer identity
   - Target environment
3. Keep previous artifact zip for rollback.

**Step 5 — Establish release policy (Developer + DevOps)**

1. Document: all pilot/production deploys must originate from a git tag on `release/*` branch.
2. No deploy from uncommitted or untagged commits.
3. WIP stays on feature branches until merged and tagged.
4. Future: add CI pipeline to automate tag → build → artifact (P1-01).

### Owner

| Sub-task | Owner |
|----------|-------|
| WIP reconciliation and pilot branch | **Developer** |
| Git tag creation | **Developer** |
| Build from tag + manifest | **Developer** |
| Deploy from tagged artifact | **DevOps** |
| Deploy log with commit SHA | **DevOps** |
| Release policy documentation | **Developer** |

### Priority

**P0**

### Completion Criteria

- [ ] All WIP changes are either committed or excluded from the pilot release branch.
- [ ] Git tag exists (e.g. `v1.0.0-pilot`) on the deployed commit.
- [ ] `artifacts/RELEASE_MANIFEST.txt` (or equivalent) records tag, commit SHA, build date.
- [ ] Deploy log records tag + SHA + deployer + timestamp.
- [ ] `dotnet ef migrations has-pending-model-changes` returns no pending changes at tag commit.
- [ ] Previous artifact retained for rollback.
- [ ] No pilot deploy performed from an untagged or dirty working tree.

### Risk if Not Completed

Unpredictable pilot behaviour; inability to reproduce or roll back defects; deployed code does not match any known commit; WIP bugs ship to production; financial calculations or UI changes untested in the deployed build.

---

## Execution Timeline (Recommended)

```text
Week 1
├── P0-1  Business sign-off sessions (Finance)          [parallel]
├── P0-2  Secrets store + production config (DevOps)  [parallel]
├── P0-6  Tag release branch + build artifact (Dev)   [parallel]
└── P0-3  Backup jobs once SQL host exists (DevOps)   [after P0-2 SQL provisioned]

Week 2
├── P0-4  Restore drill on production SQL (DevOps)    [after P0-3 first backup]
├── P0-5  SMTP config + test email (DevOps + Business)[after P0-2 secrets]
├── P0-1  Sage CSV import sign-off (Finance)          [after P0-6 tagged build in test]
└── GO/NO-GO decision for controlled pilot
```

---

## Go / No-Go Checklist (All P0 Items)

| # | Item | Done? |
|---|------|-------|
| 1 | All four business sign-off rows APPROVED in `docs/PRODUCTION_BUSINESS_SIGNOFF.md` | ☐ |
| 2 | Production secrets in vault; PlatformAdmin bootstrapped; `Seed__*` removed | ☐ |
| 3 | Daily automated SQL + document backups running with off-site copy | ☐ |
| 4 | Restore drill passed on production-tier SQL with signed report | ☐ |
| 5 | SMTP live (Path A) or manual send workflow signed (Path B interim) | ☐ |
| 6 | Pilot deployed from git tag with manifest and deploy log | ☐ |

**Pilot go-live requires all six checked.** Live funder invoicing additionally requires item 1 (billing + Sage) and item 5 Path A (live SMTP).

---

## References

| Document | Purpose |
|----------|---------|
| `PRODUCTION_READINESS_ACTION_PLAN.md` | Source P0/P1/P2 analysis |
| `docs/PRODUCTION_BUSINESS_SIGNOFF.md` | Billing and Sage approval tracker |
| `docs/PRODUCTION_CONFIGURATION.md` | Environment variables and secrets |
| `docs/PRODUCTION_DEPLOYMENT.md` | Deploy sequence and bootstrap |
| `docs/BACKUP_RESTORE.md` | Backup, restore, verification |
| `docs/SAGE50_EXPORT.md` | Sage column map and finance checklist |
| `docs/PRODUCTION_SMOKE_TEST.md` | Post-deploy smoke test |
| `scripts/Publish-CareHome.ps1` | Build artifact |
| `scripts/Deploy-Azure.ps1` | Azure deploy automation |
| `infra/azure/main.bicep` | Azure IaC defaults |

---

*This roadmap reflects repository state as of 15 September 2026. No application code was modified during this review.*

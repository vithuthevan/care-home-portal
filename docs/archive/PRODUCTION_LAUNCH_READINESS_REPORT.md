# Production Launch Readiness Report

**Date:** 15 September 2026  
**Scope:** Full-stack Care Home Back-Office Management System (Angular 22 SPA + ASP.NET Core .NET 10 API + SQL Server)  
**Review type:** Pre-launch audit — code, architecture, security, operations, testing, and business readiness  
**Method:** Repository inspection, existing UAT/hardening documentation cross-check, deployment and infrastructure review. No code changes were made.

---

## Application Summary (Context)

| Dimension | Finding |
|-----------|---------|
| **Purpose** | Multi-tenant back-office for care home operators: master data, client occupancy, funding contracts, billing/invoicing, PDFs, email, credit notes, misc CSV charges, reports, Sage 50 CSV export, users, and audit. |
| **User roles** | `PlatformAdmin` (tenant provisioning), `TenantAdmin` / `Administrator` (full tenant ops), `LocationManager` (scoped care homes), `ReadOnly` (read-only; writes blocked API-side). |
| **Main workflows** | Configure org → companies/care homes → funding authorities/categories/nominals → clients & contracts → billing preview → invoice generate → PDF download/email → payment status → credit notes → Sage export. |
| **Frontend** | Angular 22 standalone components, zoneless change detection, signals for async state, reactive forms, JWT in `localStorage`, route guards (`authGuard`, `adminGuard`, `platformGuard`, etc.), HTTP interceptor for 401/403. |
| **Backend** | ASP.NET Core modular monolith, global auth filter + `ReadOnlyGuardFilter`, EF Core, Identity + JWT, tenant context middleware, applocks on billing/credit/Sage paths. |
| **Database** | SQL Server, 12 EF migrations, tenant-scoped entities, indexes on tenant/invoice/client keys, financial snapshots on invoices. |
| **External integrations** | SMTP email (or simulated Development mode), QuestPDF for PDFs, ClosedXML for Excel, local/Azure Files document store, provisional Sage 50 CSV export. |
| **Deployment** | Manual PowerShell (`Publish-CareHome.ps1`, `Deploy-Azure.ps1`), Azure Bicep (App Service B1 + SQL Basic + Azure Files). Docker Compose is **development-only** (`ng serve`, auto-migrate). |
| **UAT status** | Final retest **PASS** (29 Aug 2026). Prior P0 defects (overlap billing, zoneless loading, nominal blocking) closed. |

---

## 1. Overall Assessment

### 🟡 Almost ready — requires final improvements

**Verdict:** The application is **not ready for unrestricted production launch with live funder invoicing**, but it **can support a controlled pilot** once operational prerequisites and business sign-off gates are cleared.

**Why:**

- **Functionally**, core workflows are implemented end-to-end and passed structured UAT on disposable databases. Billing overlap protection, tenant isolation, read-only enforcement, PDF generation, credit notes, and Sage export have been exercised and hardened.
- **Technically**, production fail-fast validation exists (JWT key, LocalDB rejection, CORS rules, dev credential blocking), health endpoints work, and financial concurrency controls (applocks) cover billing generate, credit notes, funding contract overlap, misc charge dedupe, and Sage export.
- **However**, four billing business rules and Sage column mapping remain **PENDING stakeholder approval** (`docs/PRODUCTION_BUSINESS_SIGNOFF.md`). Launching live financial operations without sign-off is a material business risk.
- **Operationally**, production deployment is manual with no CI/CD, no automated backups in infrastructure, no centralized monitoring/alerting wired in, and restore has only been verified on LocalDB — not on the target production SQL host.
- **Azure IaC defaults** set `Email__Mode=Development` (simulated email), which is inappropriate for a production pilot unless explicitly accepted.

The project's own hardening report (`docs/PRODUCTION_READINESS_REPORT.md`, 29 Aug 2026) reached the same conclusion: **"READY FOR CONTROLLED PILOT — BUSINESS SIGN-OFF REQUIRED"**. This review confirms that assessment and adds current gaps around CI/CD, observability, and production-host restore validation.

**Not production-ready for:** multi-tenant SaaS at scale, unattended operations, or live funder invoicing without finance sign-off.

**Acceptable for:** a dedicated pilot tenant on Azure or on-prem with manual ops, simulated or configured SMTP, and finance oversight of every invoice batch until Sage mapping is validated.

---

## 2. Launch Blockers (Must Fix Before Release)

| Priority | Area | Issue | Risk | Recommended Action |
|----------|------|-------|------|--------------------|
| P0 | Business | Weekly proration, monthly proration, inclusive billing-day rule, and Sage column mapping are **PENDING** sign-off | Incorrect invoices to funders; Sage postings to wrong accounts/nominals/departments; contractual and regulatory exposure | Obtain explicit APPROVED/REJECTED decisions per `docs/PRODUCTION_BUSINESS_SIGNOFF.md`. If rejected, re-implement, re-UAT, and re-sign. **Do not invoice real funders until complete.** |
| P0 | Business / Finance | Sage 50 export mapping is provisional; no confirmed import into target Sage company | Finance cannot post; duplicate or mis-posted journals | Finance user imports a pilot CSV into the **actual** Sage 50 company and signs off column mapping before go-live billing. |
| P0 | Operations | Production secrets and config not automated — `Jwt__Key`, SQL connection, SMTP, bootstrap admin must be set per environment | App fails to start, weak auth, or credentials leak | Configure per `docs/PRODUCTION_CONFIGURATION.md`. Use a secrets store (Key Vault or equivalent). Never deploy with empty `Jwt:Key` or dev credentials. |
| P0 | Operations | No automated database or document backup jobs in IaC or application | Total data loss on corruption, failed migration, or host failure; PDFs/Sage files lost separately from SQL | Implement scheduled SQL backups + document storage copy before pilot. Document RPO/RTO. Test restore on the **production SQL instance**. |
| P0 | Operations | Backup restore verified only on LocalDB, not production SQL Server / Azure SQL | Restore procedure may fail silently on real host (paths, permissions, edition differences) | Perform a full restore drill on the production-tier database before first live invoice. |
| P0 | Deployment | Azure Bicep sets `Email__Mode=Development` by default | Operators believe invoices were emailed; funders receive nothing | Set `Email__Mode=Smtp` with real SMTP secrets for pilot, or document and accept simulated email with a manual send workflow. |
| P1 | Security | JWT stored in `localStorage` (no httpOnly cookie, no refresh-token rotation) | XSS on any page can steal session token; no server-side logout invalidation until password change | Accept for controlled pilot with strict CSP and trusted users, or implement httpOnly cookie auth / token refresh before broader rollout. |
| P1 | Testing | No integration or E2E tests; only ~25 backend unit tests and thin frontend smoke tests | Regressions in billing, tenant isolation, or UI flows ship undetected | Minimum: smoke test suite against staging API before each deploy; ideally add API integration tests for billing generate, tenant 404, and read-only 403. |

---

## 3. High Priority Improvements

| Priority | Area | Improvement | Benefit |
|----------|------|-------------|---------|
| P1 | DevOps | Add CI pipeline: `dotnet build`, `dotnet test`, `npm ci`, `npm run build`, `ef migrations has-pending-model-changes` | Catches broken builds and schema drift before deploy |
| P1 | DevOps | Scripted deploy with health-check gate (`Verify-AzureDeploy.ps1` pattern) as mandatory post-deploy step | Reduces downtime from bad releases |
| P1 | Observability | Wire Application Insights / Log Analytics (or equivalent) for 5xx, latency, and correlation IDs | Faster incident detection; required for unattended ops |
| P1 | Observability | Alert on `/health/ready` failures, disk space on document store, `EmailSendLogs` failures, billing exception spikes | Prevents silent billing/email failures |
| P1 | Security | Add rate limiting beyond login (e.g. billing generate, misc CSV upload) | Reduces abuse and accidental double-submit impact |
| P1 | Security | Key Vault + managed identity for SQL/SMTP/JWT in Azure (Bicep currently has no Key Vault) | Eliminates secrets in App Service settings |
| P1 | Email | Idempotency or send-state guard on `POST /api/invoices/{id}/send` | Prevents duplicate emails on retry/double-click |
| P1 | Data | Automated document storage backup alongside SQL | Restores Sage CSVs, logos, and non-regenerable files |
| P2 | Performance | Load test billing generate with realistic tenant size (20+ homes, 300+ clients per docs scale notes) | Validates Azure SQL Basic + B1 App Service under month-end load |
| P2 | UX | Confirm all list pages have empty states (most do; verify reports/platform pages) | Reduces operator confusion on new tenants |

---

## 4. Medium Priority Improvements

| Priority | Area | Improvement | Benefit |
|----------|------|-------------|---------|
| P2 | DevOps | Production-shaped Docker image (published SPA in `wwwroot`, no auto-migrate) | Consistent container deploys; removes dev-compose footgun |
| P2 | DevOps | Staging slot or blue-green deploy on App Service | Safer releases with instant rollback |
| P2 | Database | Azure SQL tier above Basic for production (PITR, higher DTU) | Better recovery and month-end query performance |
| P2 | Database | Review `AspNetUsers` / audit log growth; plan archival | Long-term storage cost and query performance |
| P2 | Security | WAF or App Gateway in front of public App Service | DDoS and common web attack mitigation |
| P2 | Security | Secret scanning in repo (gitleaks) | Prevents accidental credential commits |
| P2 | API | Global idempotency-key middleware for money paths | Safer retries from proxies and user double-clicks |
| P2 | Frontend | Expand `canWrite()` coverage audit (most pages covered; verify reports, platform) | Consistent read-only UX |
| P2 | Compliance | Formal retention policy and GDPR/DPIA for resident PII | Legal defensibility (`docs/DATA_PROTECTION.md` notes gaps) |
| P2 | Product | Subject-access / data export workflow for residents | Operational efficiency for compliance requests |

---

## 5. Nice-to-Have Improvements

| Priority | Area | Improvement | Benefit |
|----------|------|-------------|---------|
| P3 | Product | Self-service password reset / forgot-password flow | Reduces admin support load |
| P3 | Product | In-app onboarding wizard for new tenants | Faster time-to-first-invoice |
| P3 | Testing | Playwright/Cypress E2E for login → billing → invoice → Sage | Regression safety for UI |
| P3 | Testing | Testcontainers-based API integration tests | Realistic DB tests without LocalDB |
| P3 | Performance | Frontend lazy-loaded feature routes | Smaller initial bundle |
| P3 | Ops | Runbook automation (backup verify, cert expiry checks) | Less manual ops toil |
| P3 | Infra | Private endpoints for SQL and storage | Reduced attack surface |
| P3 | Infra | Auto-scaling App Service plan | Handle month-end spikes |

---

## 6. Security Risks

| Risk | Severity | Detail | Mitigation status |
|------|----------|--------|-------------------|
| Billing formula not business-approved | **Critical** | Weekly/monthly/inclusive rules affect every invoice amount | PENDING sign-off |
| JWT in localStorage | **High** | Vulnerable to XSS token theft; 8-hour expiry, no server revoke on logout | Partial — security stamp invalidates on password change only |
| Simulated email in production | **High** | Bicep default + startup warning only; no hard block | Configure SMTP or accept manual workflow |
| No API rate limit on write endpoints | **Medium** | Only login is rate-limited (10/min/IP) | Add limits on generate/send/upload |
| Tenant isolation is application-enforced | **Medium** | No EF global filters; relies on `TenantId` in every query | Unit tests exist; no HTTP-level integration tests |
| PlatformAdmin correctly blocked from tenant APIs | **Low** | No tenant claim on platform JWT | Verified in UAT/hardening |
| ReadOnly bypass via direct API | **Low** | `ReadOnlyGuardFilter` blocks non-GET except change-password | Verified — 403 on generate |
| LocationManager scoping | **Low** | Unassigned home returns 404 | Implemented in `UserAccessService` |
| CORS misconfiguration | **Low** | Production rejects localhost; empty = same-origin | Fail-fast validator |
| Document path traversal | **Low** | `LocalDocumentStore` rejects `..` and paths outside root | Implemented |
| Misc CSV upload | **Low** | 2 MB, `.csv` only, formula injection sanitized | Hardening tests exist |
| Secrets in source | **Low** | Dev SQL password in `docker-compose.yml` default | Dev-only; document clearly |
| No field-level encryption for PII | **Medium** | Resident notes, DOB in SQL plaintext | TLS + access control only; policy decision needed |
| Audit log immutability | **Low** | No delete API for audit | Good — but no automated retention |

---

## 7. Performance Risks

| Risk | Severity | Detail |
|------|----------|--------|
| Azure SQL Basic tier | **High** | Bicep provisions Basic DB — limited DTU, no advanced PITR; month-end billing may slow |
| App Service B1 | **Medium** | Single instance, 1.75 GB RAM; concurrent PDF generation + billing may throttle |
| Client list at scale | **Low** | 303-client list ~123 ms in hardening test; pagination exists on invoices (50 default, max 200) |
| Billing generate applock | **Medium** | Serializes per-tenant generate — correct for integrity but second user waits with "in progress" |
| Frontend bundle | **Low** | Production budget 2 MB initial max; Angular Material + app — monitor with `npm run build` stats |
| No CDN for static assets | **Low** | Same-origin SPA from App Service — acceptable for pilot |
| Document storage on Azure Files | **Medium** | Network latency for PDF read/write; 50 GB share quota in Bicep |
| N+1 queries | **Unknown** | No profiling evidence in repo; review under load test |

---

## 8. Operational Risks

| Risk | Severity | Detail |
|------|----------|--------|
| Manual deploy only | **High** | Human error in migration order, wrong package, missed smoke test |
| No CI/CD | **High** | Broken code can ship; no automated test gate |
| Migrations manual + no Down() policy | **Medium** | Failed migration requires DB restore — documented but stressful |
| Docker auto-migrate in dev compose | **Medium** | `Database__ApplyMigrations: true` in `docker-compose.yml` — dangerous if copied to prod |
| Split backup (SQL vs files) | **High** | DB restore without document copy leaves missing Sage files; PDFs regenerable from snapshots |
| No monitoring/alerting wired | **High** | Ops checklist is manual (`docs/OPERATIONS_CHECKLIST.md`) |
| Certificate expiry | **Medium** | Documented but no automated alert |
| Email failure visibility | **Medium** | `EmailSendLogs` table exists; no dashboard or alert |
| First-admin bootstrap | **Medium** | Must not use dev credentials; one-time seed or manual PlatformAdmin creation |
| Support workflow | **Medium** | No in-app help desk, error reporting, or user impersonation for support |
| Multi-tenant on one DB | **Medium** | Pilot OK; blast radius if misconfiguration — tenant isolation is code-level |
| Uncommitted WIP in repo | **Low** | `client-profile` changes in git status — ensure pilot build is from a tagged release |

---

## 9. Missing Production Requirements

| Requirement | Status | Notes |
|-------------|--------|-------|
| **Monitoring** | ❌ Missing | Health endpoints only; no APM, no Log Analytics in Bicep |
| **Alerting** | ❌ Missing | Documented signals in ops checklist; not automated |
| **Centralized logging** | ❌ Missing | ASP.NET default + correlation ID middleware only |
| **Error tracking** | ❌ Missing | No Sentry/App Insights exception tracking |
| **Automated backups** | ❌ Missing | Procedure documented; no scheduled jobs in IaC |
| **Restore tested on prod host** | ❌ Missing | LocalDB only (Aug 2026) |
| **CI/CD pipeline** | ❌ Missing | No `.github/workflows` or equivalent |
| **Staging environment** | ❌ Missing | No staging slot in Bicep |
| **Secrets management** | ⚠️ Partial | Env vars documented; no Key Vault |
| **SMTP production config** | ⚠️ Partial | Code supports Smtp mode; Azure default is Development |
| **Deployment runbook** | ✅ Present | `docs/PRODUCTION_DEPLOYMENT.md`, `docs/RUNBOOK.md` |
| **Smoke test checklist** | ✅ Present | `docs/PRODUCTION_SMOKE_TEST.md` |
| **User documentation** | ✅ Present | `docs/USER_GUIDE.md` |
| **Business sign-off tracker** | ✅ Present | All items PENDING |
| **GDPR/DPIA** | ⚠️ Partial | `docs/DATA_PROTECTION.md` — not a certification |
| **Disaster recovery plan** | ⚠️ Partial | Backup/restore doc exists; RPO/RTO not defined |
| **On-call / escalation** | ❌ Missing | Not in repo |
| **SLA / uptime target** | ❌ Missing | Not defined |

---

## 10. Recommended Launch Plan

### Before launch

**Business (blocking for live invoicing)**
- [ ] Finance reviews and signs off weekly proration, monthly proration, inclusive dates (`docs/PRODUCTION_BUSINESS_SIGNOFF.md`)
- [ ] Finance imports pilot Sage CSV into target Sage 50 and approves column mapping
- [ ] Define pilot scope: one tenant, one or two care homes, limited client set

**Infrastructure**
- [ ] Provision production Azure (or on-prem) per `docs/PRODUCTION_DEPLOYMENT.md`
- [ ] Set `Jwt__Key` (≥32 chars), SQL connection, `Email__Mode=Smtp` + SMTP secrets (or document simulated-email workaround)
- [ ] Configure `DocumentStorage__RootPath` on persistent disk / Azure Files
- [ ] Set `Seed__AdminEmail` / `Seed__AdminPassword` for first PlatformAdmin (not dev credentials)
- [ ] Enable HTTPS/TLS and DNS
- [ ] Schedule SQL + document backups; verify restore on production SQL instance
- [ ] Fix Bicep `Email__Mode` default before deploy (or override in deploy script)

**Application**
- [ ] Tag release commit; run `dotnet publish` + `npm run build` via `Publish-CareHome.ps1`
- [ ] Apply EF migrations manually with pre-migration backup
- [ ] Run `docs/PRODUCTION_SMOKE_TEST.md` on staging/pilot URL
- [ ] Provision pilot tenant via PlatformAdmin UI
- [ ] Create tenant admin users; assign LocationManager home scopes if needed

**Testing**
- [ ] Manual smoke: login, create client, contract, preview billing, generate invoice, PDF, credit note, Sage export
- [ ] Verify read-only user cannot generate or edit
- [ ] Verify cross-tenant 404 on invoice/PDF

### Launch day

- [ ] Confirm backup completed within last 24 hours
- [ ] Deploy during low-traffic window
- [ ] `GET /health/live` and `/health/ready` return Healthy
- [ ] Run production smoke test checklist
- [ ] Pilot users log in; confirm no dev credentials work
- [ ] Generate **one** test invoice; finance validates amount against spreadsheet
- [ ] Monitor logs for 5xx, billing exceptions, email failures for first 4 hours
- [ ] Communicate to pilot users: reporting channel for issues

### After launch

- [ ] Daily: check health endpoints, email send logs, billing exception logs
- [ ] Weekly: review audit log for anomalies; verify backup job success
- [ ] First month-end: supervised billing run with finance present
- [ ] Add CI pipeline and integration tests before expanding beyond pilot
- [ ] Wire monitoring/alerting before second tenant or public marketing
- [ ] Revisit JWT storage and idempotency before general availability

---

## 11. Estimated Readiness

| Metric | Estimate |
|--------|----------|
| **Current readiness (controlled pilot)** | **~72%** |
| **Current readiness (full production / live funder invoicing)** | **~48%** |
| **Minimum work required (pilot)** | **1–2 weeks** — business sign-off, production env + secrets, backup/restore drill on real SQL, SMTP decision, supervised first billing cycle |
| **Minimum work required (full production)** | **6–10 weeks** — above plus CI/CD, integration tests, monitoring/alerting, Sage validation, load test, security hardening (JWT/cookies), Key Vault, staging environment |
| **Recommended improvements (post-pilot, pre-GA)** | CI/CD, Application Insights, automated backups in IaC, API integration test suite, rate limiting on write endpoints, email idempotency, E2E tests for billing path, Azure SQL tier upgrade |

---

## Appendix: Functional Readiness Detail

### Core workflows — complete with caveats

| Workflow | Status | Notes |
|----------|--------|-------|
| Org / company / care home CRUD | ✅ Complete | Pagination on lists; empty states on major lists |
| Client + funding contract + rates | ✅ Complete | Overlap rejection implemented and UAT-passed |
| Billing preview / generate | ✅ Complete | Applock + overlap re-check; formula pending business sign-off |
| Invoice PDF / email / void / payment | ✅ Complete | Email simulated by default; send not idempotent |
| Credit notes | ✅ Complete | Applock + re-preview under lock (post-hardening) |
| Misc CSV charges | ✅ Complete | Dedupe index + commit re-validation |
| Sage export | ⚠️ Provisional | Mapping not finance-approved |
| Reports | ✅ Present | Coverage not deeply tested in this review |
| Users / roles / audit | ✅ Complete | Admin-only routes guarded front and back |
| Platform tenant provisioning | ✅ Complete | PlatformAdmin isolated from tenant APIs |
| Multi-tenancy | ✅ Complete | Application-enforced; unit tests for filter inventory |

### UX patterns

| Pattern | Status |
|---------|--------|
| Loading states | ✅ `LoadingStateComponent` + signals (post UAT-002 fix) |
| Empty states | ✅ Most list pages |
| Error states | ✅ `getApiErrorMessage`; 404/403 pages |
| Form validation | ✅ Reactive forms with validators |
| Read-only UI | ✅ `auth.canWrite()` on major write actions |

### Testing coverage assessment

| Layer | Coverage | Protects critical paths? |
|-------|----------|---------------------------|
| Backend unit | ~25 tests | Partial — overlap, misc import, tenant model inventory, JWT/cipher, void rules |
| Frontend unit | 12 spec files | Thin — mostly smoke + auth cipher |
| API integration | None | **No** |
| E2E browser | None | **No** |
| Manual UAT | Extensive | **Yes** — but not repeatable in CI |

**Conclusion:** Manual UAT provides confidence for the pilot scope; automated regression protection is insufficient for ongoing production without a CI test gate.

---

*This report reflects the repository state as of 15 September 2026. It supersedes no internal document but aligns with `docs/PRODUCTION_READINESS_REPORT.md` (29 August 2026) while emphasizing operational and business gates that remain open.*

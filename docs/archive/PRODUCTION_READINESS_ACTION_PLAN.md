# Production Readiness Action Plan

**Date:** 15 September 2026  
**Application:** Care Home Back-Office Management System  
**Stack:** Angular 22 SPA · ASP.NET Core .NET 10 API · SQL Server · EF Core · Identity + JWT  
**Review type:** Pre-production architecture review (planning only — no code changes)  
**Current verdict:** **Conditionally ready for a controlled pilot** — not ready for unrestricted production launch or live funder invoicing without business sign-off and operational hardening.

---

## Executive Summary

The application is a pragmatic modular monolith with end-to-end care-home billing workflows, multi-tenant isolation, financial concurrency controls, and extensive manual UAT. Production fail-fast validation, health endpoints, audit logging, and deployment runbooks exist.

**Primary gaps before production launch:**

1. Four billing/Sage business rules remain **PENDING** stakeholder approval.
2. Operational prerequisites (secrets, backups, restore drill on target SQL, SMTP) are documented but not automated or verified on the production host.
3. No CI/CD, centralized monitoring, or API integration test suite.
4. Azure IaC defaults (`Email__Mode=Development`, SQL Basic, B1 App Service) are pilot-inappropriate without overrides.

**Estimated effort to pilot-ready:** 1–2 weeks (business + ops).  
**Estimated effort to full production (GA):** 6–10 weeks (above + CI/CD, observability, integration tests, security hardening, staging).

---

## Architecture Snapshot

| Area | Current State |
|------|---------------|
| **Application** | Modular monolith: Angular SPA + ASP.NET Core API, same-origin deploy preferred (`wwwroot`) |
| **Backend** | 18 controllers, service-layer billing/PDF/email/export, global `[Authorize]` + `ReadOnlyGuardFilter`, tenant middleware |
| **Frontend** | Standalone components, zoneless CD, signals, route guards, JWT in `localStorage`, HTTP interceptor |
| **Database** | SQL Server, 12 EF migrations, tenant-scoped entities, financial snapshots, applocks on money paths |
| **Auth** | ASP.NET Identity + JWT (8h default), RSA-encrypted login passwords, role-based + care-home scoping |
| **Deployment** | PowerShell publish/deploy scripts, Azure Bicep; Docker Compose is **development-only** |
| **Testing** | ~25 backend unit tests, 12 frontend spec files; no API integration or E2E tests |
| **Observability** | Correlation ID middleware, ASP.NET default logging, `/health/live` + `/health/ready`; no APM/alerting |

---

## Production Blockers (P0)

Items that must be resolved before any production launch with real financial or resident data.

---

### P0-01: Billing formula business sign-off incomplete

| Field | Detail |
|-------|--------|
| **Issue** | Weekly proration, monthly proration, inclusive billing-day rule, and Sage column mapping are all **PENDING** in `docs/PRODUCTION_BUSINESS_SIGNOFF.md`. |
| **Business impact** | Incorrect invoice amounts to funders; contractual disputes; regulatory/finance audit exposure. A 31-day month at £575/week bills £2,546.43 per client (not 4× weekly). |
| **Technical impact** | `RateCalculator.cs` and `DateRanges` implement fixed formulas; changing them post-launch requires re-UAT of all billing scenarios. |
| **Recommended solution** | Finance and operations review each rule with worked examples. Record APPROVED/REJECTED per item. If rejected, implement replacement, re-UAT, and re-sign before live invoicing. |
| **Estimated effort** | 3–5 business days (review) + 1–3 dev weeks if formulas change |
| **Priority** | **P0** |

---

### P0-02: Sage 50 export mapping not validated in target system

| Field | Detail |
|-------|--------|
| **Issue** | `Sage50ColumnMap.cs` is provisional. No confirmed import into the operator's actual Sage 50 company. |
| **Business impact** | Finance cannot post invoices; wrong nominal/account/department postings; manual rework and reconciliation delays. |
| **Technical impact** | Export CSV structure is code-defined; changes require deploy. Machine columns (Sage ID, nominal) are intentionally not formula-sanitized. |
| **Recommended solution** | Generate a pilot CSV from a test tenant. Finance imports into target Sage 50. Sign off column mapping per `docs/SAGE50_EXPORT.md` checklist. |
| **Estimated effort** | 2–3 business days |
| **Priority** | **P0** |

---

### P0-03: Production secrets and bootstrap not configured

| Field | Detail |
|-------|--------|
| **Issue** | `Jwt:Key` is empty in `appsettings.json`. Production requires `Jwt__Key`, SQL connection string, SMTP secrets, and one-time PlatformAdmin bootstrap via `Seed__AdminEmail` / `Seed__AdminPassword` (not dev credentials). |
| **Business impact** | Application fails to start; weak authentication; credential leakage; no admin access to provision tenants. |
| **Technical impact** | `ProductionStartupValidator` fail-fast rejects LocalDB, weak JWT, dev admin seed, and incomplete SMTP when `Email__Mode=Smtp`. |
| **Recommended solution** | Use a secrets store (Azure Key Vault or equivalent). Configure per `docs/PRODUCTION_CONFIGURATION.md`. Bootstrap PlatformAdmin once, then remove seed env vars. |
| **Estimated effort** | 1–2 days |
| **Priority** | **P0** |

---

### P0-04: No automated backup jobs for database and documents

| Field | Detail |
|-------|--------|
| **Issue** | Backup procedure is documented (`docs/BACKUP_RESTORE.md`) but no scheduled SQL backup jobs or document storage copy exist in IaC or ops automation. |
| **Business impact** | Total loss of financial records, resident data, invoice PDFs, and Sage export files on corruption, ransomware, or failed migration. |
| **Technical impact** | Database backup does not include `DocumentStorage:RootPath` files. Invoice PDFs can regenerate from snapshots; Sage CSVs and logos cannot. |
| **Recommended solution** | Schedule daily SQL `BACKUP ... WITH CHECKSUM` + document root copy/snapshot. Store off-site. Define RPO/RTO with legal/finance. Run before every migration. |
| **Estimated effort** | 2–3 days (setup + documentation) |
| **Priority** | **P0** |

---

### P0-05: Restore not verified on production-tier SQL host

| Field | Detail |
|-------|--------|
| **Issue** | Restore was verified on LocalDB disposable databases (Aug 2026) only — not on Azure SQL or the target production SQL Server instance. |
| **Business impact** | False confidence in disaster recovery; extended outage if restore fails on real host (paths, permissions, edition). |
| **Technical impact** | Restore procedure requires `MOVE` clauses, isolated test database, and matching document root. Azure SQL differs from on-prem SQL tooling. |
| **Recommended solution** | Perform full restore drill on production-tier SQL before first live invoice. Verify login, invoice totals, credit notes, audit, PDF download, cross-tenant 404 per `docs/BACKUP_RESTORE.md`. |
| **Estimated effort** | 1 day |
| **Priority** | **P0** |

---

### P0-06: Azure IaC defaults simulated email in Production

| Field | Detail |
|-------|--------|
| **Issue** | `infra/azure/main.bicep` sets `Email__Mode=Development` on App Service. No Key Vault, no connection string or JWT in Bicep outputs. |
| **Business impact** | Operators believe invoices were emailed; funders receive nothing; revenue collection delayed. |
| **Technical impact** | `ConfigurableEmailSender` logs success in simulation mode. Startup logs a warning but does not block launch. |
| **Recommended solution** | Override Bicep/deploy script to set `Email__Mode=Smtp` with SMTP secrets, **or** document and train operators on manual send workflow with explicit UI/process acknowledgment. |
| **Estimated effort** | 0.5–1 day (config) or 2 days (Bicep + Key Vault update) |
| **Priority** | **P0** |

---

### P0-07: Tagged release build not established for pilot deploy

| Field | Detail |
|-------|--------|
| **Issue** | Repository has uncommitted WIP (client-profile, Docker files). No CI gate ensures the deployed artifact matches a known commit. |
| **Business impact** | Unpredictable pilot behaviour; inability to reproduce or roll back defects. |
| **Technical impact** | Manual `Publish-CareHome.ps1` / `Deploy-Azure.ps1` without commit SHA traceability. |
| **Recommended solution** | Tag a release commit. Build publish artifact from that tag. Record commit SHA in deploy log. Freeze WIP out of pilot branch. |
| **Estimated effort** | 0.5 day |
| **Priority** | **P0** |

---

## High Priority Improvements (P1)

Required for safe pilot operation and before expanding beyond a single controlled tenant.

---

### P1-01: No CI/CD pipeline

| Field | Detail |
|-------|--------|
| **Issue** | No `.github/workflows`, Azure DevOps, or equivalent. Builds and tests are manual. |
| **Business impact** | Broken code or schema drift ships to production; human error in deploy steps. |
| **Technical impact** | No automated gate for `dotnet build`, `dotnet test`, `npm ci`, `npm run build`, `ef migrations has-pending-model-changes`. |
| **Recommended solution** | Add CI pipeline on main/release branches: build, test, migration check, artifact publish. Block deploy on failure. |
| **Estimated effort** | 2–3 days |
| **Priority** | **P1** |

---

### P1-02: No centralized monitoring or alerting

| Field | Detail |
|-------|--------|
| **Issue** | Only health endpoints and console logging exist. No Application Insights, Log Analytics, Sentry, or automated alerts. |
| **Business impact** | Silent outages, undetected billing/email failures, delayed incident response. |
| **Technical impact** | `CorrelationIdMiddleware` and `RequestLoggingScopeMiddleware` exist but logs are not aggregated. Ops checklist (`docs/OPERATIONS_CHECKLIST.md`) is manual. |
| **Recommended solution** | Wire APM/log aggregation. Alert on `/health/ready` failure, 5xx rate, disk space on document store, `EmailSendLogs` failures, `BillingExceptionLogs` spikes. |
| **Estimated effort** | 3–5 days |
| **Priority** | **P1** |

---

### P1-03: JWT stored in localStorage without refresh or server-side revocation

| Field | Detail |
|-------|--------|
| **Issue** | `AuthService` persists token in `localStorage`. No httpOnly cookie, no refresh token, no server-side logout invalidation (security stamp only on password change). |
| **Business impact** | XSS compromise exposes 8-hour session; stolen token usable until expiry; no instant revoke on logout or role change. |
| **Technical impact** | `JwtSecurityStamp.OnTokenValidated` validates stamp but role changes require re-login. Frontend `authInterceptor` clears token on 401 only. |
| **Recommended solution** | For pilot: enforce strict SPA CSP (see P1-04), trusted user base, short `Jwt__ExpiryHours`. For GA: migrate to httpOnly cookie auth or refresh-token rotation with server-side deny list. |
| **Estimated effort** | Pilot acceptance: 0 days. Full fix: 1–2 weeks |
| **Priority** | **P1** |

---

### P1-04: SPA Content-Security-Policy not implemented

| Field | Detail |
|-------|--------|
| **Issue** | `SecurityHeadersMiddleware` applies API-only CSP to `/api` and `/health`. Static SPA assets served from `wwwroot` do not get the documented SPA CSP from `docs/PRODUCTION_CONFIGURATION.md`. |
| **Business impact** | Weaker XSS mitigation for the primary attack surface (browser UI handling resident PII). |
| **Technical impact** | Same-origin deploy serves HTML/JS without `default-src 'self'` CSP. Angular requires `style-src 'unsafe-inline'`. |
| **Recommended solution** | Add SPA CSP via reverse proxy or middleware branch for non-API static paths. Test Angular Material and component styles. |
| **Estimated effort** | 1–2 days |
| **Priority** | **P1** |

---

### P1-05: No API integration tests for critical paths

| Field | Detail |
|-------|--------|
| **Issue** | ~25 backend unit tests cover rules/helpers only. No HTTP-level tests for billing generate, tenant isolation 404, read-only 403, or login flow. 12 frontend specs are thin smoke tests. |
| **Business impact** | Billing, tenancy, or auth regressions ship undetected between UAT cycles. |
| **Technical impact** | `TenantIsolationTests` validates `ForTenant()` helper, not controller behaviour. Financial applocks and overlap rules lack integration coverage. |
| **Recommended solution** | Add `WebApplicationFactory` integration tests for: login, cross-tenant invoice 404, read-only generate 403, billing preview. Gate CI on pass. |
| **Estimated effort** | 1–2 weeks |
| **Priority** | **P1** |

---

### P1-06: Rate limiting only on login endpoint

| Field | Detail |
|-------|--------|
| **Issue** | `RateLimiter` policy `"login"` only — 10 req/min/IP on `/api/auth/login`. Write endpoints (billing generate, invoice send, misc CSV upload) are unlimited. |
| **Business impact** | Abuse, accidental double-submit amplification, month-end load spikes affecting all users. |
| **Technical impact** | Billing has applocks but email send and CSV upload do not. Reverse-proxy retries could duplicate side effects. |
| **Recommended solution** | Add rate limits on billing generate, invoice send, misc upload. Add idempotency guard on invoice send (see P1-07). |
| **Estimated effort** | 2–3 days |
| **Priority** | **P1** |

---

### P1-07: Invoice email send not idempotent

| Field | Detail |
|-------|--------|
| **Issue** | `POST /api/invoices/{id}/send` can be retried or double-clicked without idempotency key or send-state guard. |
| **Business impact** | Duplicate emails to funders; operator confusion; reputational harm. |
| **Technical impact** | `EmailSendLogs` records attempts but does not prevent duplicate sends on retry. |
| **Recommended solution** | Reject or no-op if invoice already in `Sent` status with successful send log; optional `Idempotency-Key` header. |
| **Estimated effort** | 1–2 days |
| **Priority** | **P1** |

---

### P1-08: Secrets not in Key Vault / managed identity (Azure)

| Field | Detail |
|-------|--------|
| **Issue** | Bicep provisions App Service app settings but no Key Vault reference. Deploy script prompts for secrets in plain App Service configuration. |
| **Business impact** | Secret exposure via portal, logs, or backup of app settings; compliance audit findings. |
| **Technical impact** | SQL admin password, JWT key, SMTP password stored as App Service settings. |
| **Recommended solution** | Add Key Vault + managed identity to Bicep. Reference secrets via `@Microsoft.KeyVault(...)`. Use separate SQL app login (not admin) at runtime. |
| **Estimated effort** | 3–5 days |
| **Priority** | **P1** |

---

### P1-09: Mandatory post-deploy smoke test not automated

| Field | Detail |
|-------|--------|
| **Issue** | `docs/PRODUCTION_SMOKE_TEST.md` exists but is manual. `scripts/uat-*.ps1` patterns exist but are not wired to deploy pipeline. |
| **Business impact** | Bad deploys reach users before detection. |
| **Technical impact** | Health endpoints alone do not validate auth, billing, or tenancy. |
| **Recommended solution** | Script smoke test (login, health, read-only 403, cross-tenant 404) as mandatory deploy gate. Fail deploy on non-200. |
| **Estimated effort** | 2–3 days |
| **Priority** | **P1** |

---

### P1-10: GDPR / data protection policy gaps

| Field | Detail |
|-------|--------|
| **Issue** | `docs/DATA_PROTECTION.md` is operational, not a certification. No retention schedule, DPIA, erasure procedure, or subprocessor list. PII (DOB, notes) stored in plaintext SQL. |
| **Business impact** | Regulatory non-compliance; inability to respond to subject access/erasure requests; legal exposure for resident data. |
| **Technical impact** | No automated purge. `IsArchived` hides clients; no GDPR erasure tool. Audit logs retain user IDs indefinitely. |
| **Recommended solution** | Legal/privacy function defines lawful basis, retention, erasure vs financial record conflict, subprocessor list. Document SAR export procedure (manual tenant-scoped extraction). |
| **Estimated effort** | 1–2 weeks (legal/business) |
| **Priority** | **P1** |

---

## Medium Priority Improvements (P2)

Address after pilot launch or in parallel if capacity allows.

---

### P2-01: Docker Compose is development-only with unsafe production patterns

| Field | Detail |
|-------|--------|
| **Issue** | `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT=Development`, `Database__ApplyMigrations=true`, `ng serve` for frontend, default SA password in `.env.example`. |
| **Business impact** | Accidental production deploy from Compose exposes dev credentials, auto-migrates without backup gate. |
| **Technical impact** | Web container runs dev server, not production build. API Dockerfile does not embed SPA in `wwwroot`. |
| **Recommended solution** | Add `docker-compose.prod.yml` with published SPA, `Production` env, no auto-migrate, secrets from env. Document Compose as dev-only in README. |
| **Estimated effort** | 2–3 days |
| **Priority** | **P2** |

---

### P2-02: Azure SQL Basic tier and App Service B1 undersized

| Field | Detail |
|-------|--------|
| **Issue** | Bicep provisions SQL Basic and App Service B1 (1.75 GB RAM, single instance). |
| **Business impact** | Slow month-end billing; limited point-in-time recovery on Basic tier; outage if single instance fails. |
| **Technical impact** | Concurrent PDF generation + billing generate may throttle. 50 GB Azure Files quota may be insufficient at scale. |
| **Recommended solution** | Upgrade to Standard S1+ SQL with automated backups/PITR. Use S1 or P1v3 App Service for pilot with 2 instances if SLA required. |
| **Estimated effort** | 1 day (config + cost approval) |
| **Priority** | **P2** |

---

### P2-03: No staging environment or deployment slot

| Field | Detail |
|-------|--------|
| **Issue** | Single App Service in Bicep. No staging slot, blue-green, or pre-production environment. |
| **Business impact** | Migrations and deploys tested directly on production; longer rollback time. |
| **Technical impact** | Manual migration with backup is the only safety net. |
| **Recommended solution** | Add App Service deployment slot or separate staging resource group. Run migrations and smoke tests on staging first. |
| **Estimated effort** | 2–4 days |
| **Priority** | **P2** |

---

### P2-04: Tenant isolation is application-enforced only

| Field | Detail |
|-------|--------|
| **Issue** | No EF global query filters. Every query must include `TenantId`. Platform routes use explicit `tenantId` parameters. |
| **Business impact** | Single missed filter in new code leaks cross-tenant data — high severity in multi-tenant SaaS. |
| **Technical impact** | `ForTenant()` helper exists; `ITenantOwned` inventory tested in unit tests. No HTTP-level regression test. |
| **Recommended solution** | Add integration tests per tenant-scoped controller. Consider Roslyn analyzer or code review checklist for new queries. Optional: row-level security in SQL Server for defense in depth. |
| **Estimated effort** | 1 week (tests + optional RLS) |
| **Priority** | **P2** |

---

### P2-05: No WAF or DDoS protection on public endpoint

| Field | Detail |
|-------|--------|
| **Issue** | App Service is public with `httpsOnly`. No Azure Front Door, Application Gateway WAF, or rate limiting at edge. |
| **Business impact** | Vulnerable to common web attacks and volumetric abuse. |
| **Technical impact** | Login rate limit is app-level only (10/min/IP). |
| **Recommended solution** | Add Front Door or App Gateway WAF for production. Enable Azure DDoS Protection Standard if required by policy. |
| **Estimated effort** | 3–5 days |
| **Priority** | **P2** |

---

### P2-06: Load testing not performed at realistic scale

| Field | Detail |
|-------|--------|
| **Issue** | Hardening used 3 homes + 303 clients (~123 ms list). No test at 20+ homes or concurrent billing users. |
| **Business impact** | Month-end billing timeout or failure under real operator load. |
| **Technical impact** | Billing applock serializes per-tenant generate; second user sees "in progress". N+1 query risk unprofiled. |
| **Recommended solution** | Load test billing preview/generate with realistic tenant. Profile SQL queries. Tune indexes and App Service plan. |
| **Estimated effort** | 3–5 days |
| **Priority** | **P2** |

---

### P2-07: Audit and Identity table growth unmanaged

| Field | Detail |
|-------|--------|
| **Issue** | No archival or purge for `AuditLogs`, `BillingExceptionLogs`, `EmailSendLogs`, `AspNetUsers`. |
| **Business impact** | Increasing storage cost; slower audit queries over time. |
| **Technical impact** | Audit rows are immutable by design. No partitioning. |
| **Recommended solution** | Define retention policy. Plan annual archive to cold storage or partitioned tables. |
| **Estimated effort** | 1 week (policy + implementation) |
| **Priority** | **P2** |

---

### P2-08: Secret scanning not in repository workflow

| Field | Detail |
|-------|--------|
| **Issue** | No gitleaks, GitHub secret scanning, or pre-commit hook for credentials. |
| **Business impact** | Accidental commit of JWT key or SMTP password. |
| **Technical impact** | `.env.example` contains dev-only SQL password (acceptable if clearly marked). |
| **Recommended solution** | Add gitleaks to CI. Enable GitHub secret scanning if using GitHub. |
| **Estimated effort** | 0.5 day |
| **Priority** | **P2** |

---

### P2-09: Frontend routes not lazy-loaded

| Field | Detail |
|-------|--------|
| **Issue** | `app.routes.ts` statically imports all feature components. Initial bundle includes entire app. |
| **Business impact** | Slower first load on poor connections; higher bandwidth on mobile. |
| **Technical impact** | Angular budget allows 2 MB initial max. Material + full app may approach limit. |
| **Recommended solution** | Convert feature areas to `loadComponent` / `loadChildren` lazy routes. |
| **Estimated effort** | 2–3 days |
| **Priority** | **P2** |

---

### P2-10: On-call, escalation, and SLA undefined

| Field | Detail |
|-------|--------|
| **Issue** | No on-call rotation, escalation path, or uptime SLA documented. |
| **Business impact** | Extended outages during pilot; unclear accountability. |
| **Technical impact** | `docs/RUNBOOK.md` exists but no paging integration. |
| **Recommended solution** | Define pilot support hours, escalation contacts, and target response times. Wire critical alerts to on-call channel. |
| **Estimated effort** | 1–2 days (ops planning) |
| **Priority** | **P2** |

---

## Future Improvements (P3)

Post-GA or when scaling beyond initial pilot tenants.

---

### P3-01: Self-service password reset

| Field | Detail |
|-------|--------|
| **Issue** | No forgot-password flow. Admins must reset passwords manually. |
| **Business impact** | Support overhead; user lockout delays. |
| **Technical impact** | Identity token providers exist but no reset endpoint or email template. |
| **Recommended solution** | Add forgot-password API + UI with email token flow. |
| **Estimated effort** | 3–5 days |
| **Priority** | **P3** |

---

### P3-02: Playwright/Cypress E2E test suite

| Field | Detail |
|-------|--------|
| **Issue** | No browser E2E tests. UAT scripts are PowerShell API calls, not UI flows. |
| **Business impact** | UI regressions (e.g. zoneless loading bugs) not caught automatically. |
| **Technical impact** | Prior P0 defect (UAT-002) was zoneless CD — would benefit from E2E. |
| **Recommended solution** | E2E for login → billing preview → generate → invoice detail → Sage export. |
| **Estimated effort** | 2–3 weeks |
| **Priority** | **P3** |

---

### P3-03: Testcontainers-based API integration tests

| Field | Detail |
|-------|--------|
| **Issue** | Unit tests use in-memory lists, not real SQL Server behaviour (applocks, transactions). |
| **Business impact** | Concurrency edge cases may slip through. |
| **Technical impact** | `UPDLOCK, ROWLOCK, HOLDLOCK` and `SqlAppLock` need real SQL to validate. |
| **Recommended solution** | Testcontainers SQL Server in CI for billing generate and credit note integration tests. |
| **Estimated effort** | 1–2 weeks |
| **Priority** | **P3** |

---

### P3-04: Private endpoints for SQL and storage

| Field | Detail |
|-------|--------|
| **Issue** | Bicep enables public SQL network access with Azure services firewall rule. |
| **Business impact** | Larger attack surface for data exfiltration. |
| **Technical impact** | App Service connects over public endpoint. |
| **Recommended solution** | VNet integration, private endpoint for SQL and Azure Files. |
| **Estimated effort** | 1–2 weeks |
| **Priority** | **P3** |

---

### P3-05: In-app tenant onboarding wizard

| Field | Detail |
|-------|--------|
| **Issue** | New tenants require manual configuration of 5+ master data entities before first invoice. |
| **Business impact** | Slow time-to-first-invoice; operator errors on setup. |
| **Technical impact** | `TenantProvisioningService` seeds categories and sequences only. |
| **Recommended solution** | Guided setup wizard with checklist (company → care home → authority → category → nominal). |
| **Estimated effort** | 2–3 weeks |
| **Priority** | **P3** |

---

### P3-06: Auto-scaling and CDN

| Field | Detail |
|-------|--------|
| **Issue** | Single App Service instance. Static assets served from app, not CDN. |
| **Business impact** | Cannot handle traffic spikes without manual plan upgrade. |
| **Technical impact** | PDF generation is CPU-bound on app instance. |
| **Recommended solution** | Auto-scale rules on CPU/memory. Azure CDN for static assets. |
| **Estimated effort** | 3–5 days |
| **Priority** | **P3** |

---

### P3-07: Field-level encryption for resident PII

| Field | Detail |
|-------|--------|
| **Issue** | DOB, notes, contact details stored in plaintext SQL columns. |
| **Business impact** | Higher impact if database backup or SQL access is compromised. |
| **Technical impact** | Would affect search, reporting, and snapshot behaviour. |
| **Recommended solution** | Policy decision first. If required: encrypt notes/DOB at rest with key in Key Vault. |
| **Estimated effort** | 3–4 weeks |
| **Priority** | **P3** |

---

### P3-08: Subject-access request (SAR) export feature

| Field | Detail |
|-------|--------|
| **Issue** | No built-in resident data export for GDPR subject access requests. |
| **Business impact** | Manual operational extraction is slow and error-prone. |
| **Technical impact** | Data spans clients, contracts, invoices, audit — all tenant-scoped. |
| **Recommended solution** | Admin-triggered SAR export (JSON/PDF bundle) per client. |
| **Estimated effort** | 1–2 weeks |
| **Priority** | **P3** |

---

## Domain Review Summary

### 1. Application Architecture

**Strengths:** Clear modular monolith; same-origin deploy; documented request flow; financial logic centralized in services; applocks on money paths.  
**Gaps:** No bounded context split (acceptable for MVP); no event bus or async processing for email/PDF at scale; manual deploy only.

### 2. Backend Architecture

**Strengths:** Global auth + read-only guard; `ProductionStartupValidator`; correlation ID; generic 500 handler; `ApiExceptionHandler`; tenant middleware chain.  
**Gaps:** No structured logging sink; no API versioning; no OpenAPI security documentation for external integrators.

### 3. Frontend Architecture

**Strengths:** Standalone components; zoneless CD with signals (post-UAT fix); route guards by role; `canWrite()` pattern; secret query param stripping in interceptor.  
**Gaps:** JWT in localStorage; no token expiry handling in UI; all routes eagerly loaded; SPA CSP not applied.

### 4. Database Design

**Strengths:** 12 migrations; tenant-scoped unique indexes; financial snapshots on invoices; `DateOnly` for business dates; dedupe index on misc charges; document sequences with row locks.  
**Gaps:** No row-level security; child tables without `TenantId` rely on parent joins; no archival strategy; Basic tier limits on Azure.

### 5. Authentication and Authorization

**Strengths:** Identity + JWT; RSA login password encryption; lockout 5/15 min; login rate limit; security stamp validation; role + care-home scoping; PlatformAdmin isolated from tenant APIs; inactive tenant/user handling.  
**Gaps:** No refresh tokens; no server-side logout; 8-hour token lifetime without proactive renewal; role change does not invalidate existing JWT until re-login.

### 6. Deployment Configuration

**Strengths:** `Publish-CareHome.ps1`, `Deploy-Azure.ps1`, Bicep IaC, production deployment docs, rollback procedure.  
**Gaps:** Bicep dev email default; no CI/CD; Docker is dev-only; no staging slot; migrations manual (correct for financial DB but requires discipline).

### 7. Environment Configuration

**Strengths:** Comprehensive `docs/PRODUCTION_CONFIGURATION.md`; env var mapping documented; fail-fast validation; dev credentials blocked in Production.  
**Gaps:** No Key Vault in IaC; `AllowedHosts: *` in appsettings; forwarded headers known proxies empty by default (rely on platform defaults).

### 8. Logging and Monitoring

**Strengths:** Correlation ID on request/response/500 body; request logging scope; health live/ready; ops checklist defines signals.  
**Gaps:** No APM, no log aggregation, no dashboards, no automated alerts, no Sentry/App Insights.

### 9. Backup Strategy

**Strengths:** Documented SQL + file backup; restore verified on LocalDB; rollback = restore not `Down()`.  
**Gaps:** No automated jobs; no off-site copy in IaC; restore not tested on Azure SQL; RPO/RTO undefined; document backup not coupled to SQL job.

### 10. Testing Coverage

| Layer | Count | Critical path coverage |
|-------|-------|------------------------|
| Backend unit | ~25 tests (5 files) | Partial — rules, helpers, startup validation |
| Frontend unit | 12 spec files | Thin — smoke and cipher |
| API integration | 0 | **None** |
| E2E browser | 0 | **None** |
| Manual UAT | Extensive (PASS Aug 2026) | **Yes** — not repeatable in CI |

---

## Recommended Launch Sequence

### Phase 1 — Blockers (before pilot go-live)

1. Obtain business sign-off on billing rules and Sage mapping (P0-01, P0-02).
2. Configure production secrets and bootstrap PlatformAdmin (P0-03).
3. Set up automated SQL + document backups (P0-04).
4. Run restore drill on production-tier SQL (P0-05).
5. Configure SMTP or document simulated-email workflow (P0-06).
6. Tag and build release artifact (P0-07).

### Phase 2 — Pilot hardening (week 1–2 of pilot)

1. Add CI pipeline (P1-01).
2. Wire monitoring and alerts (P1-02).
3. Implement post-deploy smoke test gate (P1-09).
4. Add SPA CSP (P1-04).
5. Begin API integration tests (P1-05).

### Phase 3 — Pre-GA (before second tenant or public marketing)

1. Key Vault + managed identity (P1-08).
2. Rate limiting and email idempotency (P1-06, P1-07).
3. Staging environment (P2-03).
4. Load testing (P2-06).
5. GDPR policy completion (P1-10).
6. Evaluate JWT storage migration (P1-03).

---

## Readiness Estimate

| Milestone | Readiness | Effort to reach |
|-----------|-----------|-----------------|
| Controlled pilot (single tenant, supervised billing) | ~72% | 1–2 weeks |
| Full production (live funder invoicing, unattended ops) | ~48% | 6–10 weeks |

---

## References

| Document | Purpose |
|----------|---------|
| `docs/ARCHITECTURE.md` | System design |
| `docs/PRODUCTION_CONFIGURATION.md` | Environment variables |
| `docs/PRODUCTION_DEPLOYMENT.md` | Deploy sequence |
| `docs/PRODUCTION_BUSINESS_SIGNOFF.md` | Billing gates |
| `docs/BACKUP_RESTORE.md` | Backup/restore procedure |
| `docs/OPERATIONS_CHECKLIST.md` | Monitoring signals |
| `docs/DATA_PROTECTION.md` | Privacy posture |
| `docs/PRODUCTION_READINESS_REPORT.md` | Aug 2026 hardening result |
| `infra/azure/main.bicep` | Azure IaC |

---

*This plan reflects repository state as of 15 September 2026. No application code was modified during this review.*

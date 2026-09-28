# Care Home Back-Office — Project Analysis for CV Use

Evidence-only report from the `CarehomeSystem` repository. Counts were verified by inspecting project files, controllers, DbSets, tests, and workflows. Authorship is **not** assumed: statements use “The project contains…” unless otherwise noted.

---

## 1. PROJECT OVERVIEW

| Field | Finding |
|--------|---------|
| **Project name** | Care Home Back-Office Management System (`CarehomeSystem`) |
| **Project type** | Multi-tenant SaaS-style back-office SPA + REST API (modular monolith) |
| **Primary purpose** | Care-home organisations manage residents, funding contracts, billing, invoices, collections, payments, bank reconciliation, remittances, and Sage 50 export |
| **Problem being solved** | Replace fragmented care-home billing and receivables operations with a single revenue-cycle system: funder contracts → invoice → collect → reconcile → accounting export |
| **Likely target users** | Tenant operations/finance staff (`TenantAdmin`, `Administrator`, `LocationManager`, `ReadOnly`) and platform operators (`PlatformAdmin`) |
| **Current maturity** | **Pilot-ready** |

**Maturity evidence:** Root `README.md` labels the product an MVP. `docs/PRODUCTION_READINESS_REPORT.md` (29 August 2026) rates it **CONDITIONALLY READY** / “READY FOR CONTROLLED PILOT — BUSINESS SIGN-OFF REQUIRED”. Paid-pilot checklists, UAT reports, Docker/Azure/OCI deploy paths, and security hardening exist. Weekly/monthly proration, inclusive day counting, and Sage mapping remain pending business decisions. This is beyond prototype, short of unqualified production-ready.

### What the system actually does

Operators configure a tenant organisation, legal companies, care homes, funding authorities, nominal codes, and invoice templates. They register residents and effective-dated funding contracts/rates, then preview and generate invoices (QuestPDF PDFs, optional SMTP email), issue credit notes, and track receivables and collections. Cash is recorded as payments and allocations; bank CSVs and remittance files are imported and matched; revenue-assurance scans and dispute workflows sit on top of that AR pipeline; Sage 50 CSV export closes the accounting loop. Isolation is shared-database multi-tenancy via JWT `tenant_id` and explicit `TenantId` filters (not EF global query filters). The Angular SPA calls a single ASP.NET Core API; production can serve the SPA from API `wwwroot` on Azure App Service or an OCI VM with Docker.

---

## 2. TECHNOLOGY STACK

Technologies listed only where found in manifests, source, or infra files.

| Area | Technologies found |
|------|-------------------|
| **Frontend** | Angular 22 (standalone components, Reactive Forms, HttpClient), Angular Material + CDK, Tailwind CSS 4, RxJS 7.8, Zone.js, TypeScript ~6.0 |
| **Backend** | ASP.NET Core Web API, .NET 10 (`net10.0`) |
| **Languages** | C#, TypeScript, PowerShell (deploy/backup scripts), Bicep, YAML (GitHub Actions), SQL (EF migrations / locking hints) |
| **Frameworks** | ASP.NET Core, Entity Framework Core, ASP.NET Core Identity, Angular |
| **Database** | SQL Server / LocalDB / Azure SQL (Compose uses `mcr.microsoft.com/mssql/server:2022-latest`) |
| **ORM/data access** | EF Core + EF Core SqlServer; `CareHomeDbContext` extends `IdentityDbContext<ApplicationUser>` |
| **Authentication** | ASP.NET Core Identity + JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11); optional RSA login-password cipher |
| **Authorization** | Identity roles + named capability policies (`CareHomePolicies`); `ReadOnlyGuardFilter`; care-home scope via `UserAccessService` |
| **Cloud** | Azure (App Service, Azure SQL, Key Vault, Azure Files, Recovery Services — `infra/azure/main.bicep`); Oracle Cloud Infrastructure Compute VM + Docker |
| **Containers** | Docker, Docker Compose (`docker-compose.yml`, `.dev.yml`, `.prod.yml`) |
| **CI/CD** | GitHub Actions (`ci.yml`, `quality-gates.yml`, `deploy-oracle.yml`); Azure deploy scripts (`scripts/Deploy-Azure.ps1`) |
| **Testing** | xUnit, `Microsoft.AspNetCore.Mvc.Testing`, EF InMemory/SQL; Angular Vitest + jsdom; Playwright scripts (manual, not in CI) |
| **Monitoring/logging** | OpenTelemetry (ASP.NET, HTTP, EF Core); Azure Monitor OpenTelemetry when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set; health checks `/health/live`, `/health/ready`; correlation IDs |
| **Messaging/queues** | **None found** (no Redis, RabbitMQ, Service Bus, Hangfire, Quartz) |
| **Caching** | **None found** (no `IMemoryCache` / Redis) |
| **External APIs** | SMTP (`System.Net.Mail.SmtpClient`); Sage 50 is file-based CSV export, not a live Sage API |
| **Other important technologies** | QuestPDF (invoice/credit-note PDFs); ClosedXML (Excel remittance/reports); Caddy reverse-proxy example (`infra/oracle/Caddyfile.example`); Prettier |

**Not found as implemented products:** OAuth/OIDC, Stripe/card gateways, Kubernetes, Terraform, AWS, GCP, Redis, Hangfire, FluentValidation, ESLint, SonarQube, OpenAI/ML packages.

---

## 3. ARCHITECTURE

**Actual architecture: pragmatic modular monolith** with a layered API host (controllers → services → EF Core). Documented in `docs/ARCHITECTURE.md`: no Domain/Application/Infrastructure split, no generic repository, no NgRx.

This is **not** microservices, hexagonal, or event-driven. Domain work is split into class libraries that share `CareHome.Api` namespaces and are hosted by a single API process.

### Major modules / services

**Backend projects (10 under `backend/`):**

| Project | Role |
|---------|------|
| `CareHome.Api` | Host: controllers, DI, middleware, email, PDF, export, security |
| `CareHome.Data` | `CareHomeDbContext`, models, migrations |
| `CareHome.Abstractions` | Shared contracts (`ITenantContext`, `IAuditWriter`, `IEmailSender`, etc.) |
| `CareHome.Billing` | Invoice preview/generate, rates, grouping, credit notes |
| `CareHome.Funding` | Funding contracts and rates |
| `CareHome.Receivables` | Outstanding balances and ageing |
| `CareHome.Payments` | Payments and allocations |
| `CareHome.Reconciliation` | Bank import and match scoring |
| `CareHome.Remittance` | Remittance CSV/XLSX import and matching |
| `CareHome.RevenueAssurance` | Leakage-detection scans |

Plus `CareHome.Api.Tests` at repo root.

**Frontend:** `frontend/care-home-web` with 30 feature folders (billing, invoices, payments, banking, remittances, collections, platform, etc.).

### Important boundaries

- **Tenant (Organisation)** is the isolation boundary. Company is a legal entity under a tenant, not the tenant itself (`docs/MULTI_TENANCY.md`).
- PlatformAdmin has no `tenant_id`; `[RequireTenant]` operational APIs return 403.
- LocationManager is scoped by `UserCareHomeAccess` after tenant match.
- Documents live under `tenants/{publicId}/…` (`LocalDocumentStore`).
- Clients must not send `tenantId` in bodies; JWT supplies tenant.
- Feature flag `Features:CommercialRevenueEnabled` gates commercial revenue APIs.

### Request / data flow

```
Angular feature → AuthService (JWT in localStorage)
  → authInterceptor (Authorization: Bearer)
  → ASP.NET controller ([Authorize] global + policies / [RequireTenant])
  → domain services (Billing, Payments, Collections, …)
  → CareHomeDbContext → SQL Server
```

PDFs/CSVs go to the filesystem document store. Email goes to SMTP or Development simulation.

### Database structure

Shared SQL Server database. Hierarchy: Tenant → TenantSettings, Companies → CareHomes → Clients → ClientFundingContracts → FundingRates. Operational tables cover invoices, credit notes, payments/allocations, bank transactions, remittances, collections, disputes, audit, and Sage export batches. See section 6.

### Authentication flow

1. Optional `GET /api/auth/login-key` for public key (password cipher).
2. Rate-limited `POST /api/auth/login`: Identity password check, lockout, tenant-active check → JWT (`sub`, roles, `tenant_id` / public id / name, `security_stamp`).
3. Frontend stores the token; interceptor attaches Bearer.
4. `OnTokenValidated` reloads the user; stamp mismatch or inactive user rejects the token.
5. Middleware: inactive tenant, must-change-password; `ReadOnlyGuardFilter` blocks mutating verbs for ReadOnly.

### Deployment architecture

| Mode | Shape |
|------|--------|
| Local Docker | SQL + API + Angular `ng serve` (`docker-compose.yml`) |
| Production Docker (OCI) | Multi-stage `Dockerfile.prod` (Angular built into `wwwroot`) + optional SQL; TLS via Caddy/nginx |
| Azure | Single App Service (SPA+API same origin) + Azure SQL + Key Vault + Azure Files + Recovery Services (`infra/azure/main.bicep`) |
| CI | Quality gates (build/test/migrations/container) → OCI SSH deploy on `revenue-cycle-v2` |

### Multi-tenancy

**Shared-database, explicit tenant scoping.** Not EF global query filters. Helpers: `ITenantOwned`, `q.ForTenant(tenantId)`. Per-tenant document sequences with SQL locking hints. PlatformAdmin is a separate plane for tenant provisioning.

---

## 4. FEATURES IMPLEMENTED

| Feature | What it does | Technologies involved | Complexity | Evidence / location |
|---------|--------------|----------------------|------------|---------------------|
| Authentication | Login, change/forgot/reset password, `/me`, lockout, must-change-password | Identity, JWT Bearer, rate limiter | High | `AuthController.cs`, `Program.cs`, `JwtSecurityStamp.cs`, `MustChangePasswordMiddleware` |
| Authorization / RBAC | 5 active roles, 14 named policies, care-home scoping, ReadOnly write block | Identity roles, `[Authorize(Policy=…)]`, `UserAccessService` | High | `AppRoles.cs`, `CareHomePolicies.cs`, `ReadOnlyGuardFilter.cs` |
| User management | Create/update users, deactivate, reset password, roles, care-home access | Identity, tenant-scoped APIs | Medium | `UsersController.cs`, `UserCareHomeAccess`, frontend `features/users/` |
| Multi-tenancy | Tenant context, platform tenant admin, inactive-tenant gate, provisioning | JWT claims, `ITenantContext`, `ForTenant` | High | `HttpTenantContext.cs`, `PlatformTenantsController.cs`, `TenantProvisioningService.cs` |
| Companies / care homes / clients | Org hierarchy plus guardians, logos, portal theme | EF Core, `IDocumentStore` | Medium–High | `CompaniesController`, `CareHomesController`, `ClientsController` |
| Funding master data | Funding authorities, invoice categories, nominal codes | CRUD + tenant-unique codes | Medium | `FundingAuthoritiesController`, `InvoiceCategoriesController`, `NominalCodesController` |
| Funding contracts & rates | Effective-dated contracts/rates; overlap validation; applock | `CareHome.Funding` | High | `FundingContractService.cs`, `FundingContractOverlap.cs` |
| Contract renewals | Renewal dashboard, create/agree workflow | Workflow service | Medium | `ContractRenewalsController.cs`, `ContractRenewalWorkflowService.cs` |
| Billing / invoice generation | Preview + generate for a period; daily/weekly/monthly rates; grouping; coverage | `CareHome.Billing`, QuestPDF, `SqlAppLock` | High | `BillingService.cs`, `RateCalculator.cs`, `BillingInvoiceGrouper.cs` |
| Invoices | List/filter, PDF, email, bulk send, payment status, void | PDF + SMTP + document store | High | `InvoicesController.cs`, `InvoicePdfService.cs`, `DocumentEmailService.cs` |
| Credit notes | Preview/generate, PDF, email | Transactions + sequences | High | `CreditNotesController.cs`, `CreditNoteService.cs` |
| Invoice templates | Templates + logo upload/delete | CRUD + `IDocumentStore` | Medium | `InvoiceTemplatesController.cs` |
| Misc charge import | CSV preview/confirm with dedupe index | Import service | Medium | `MiscChargeImportService.cs`, migration `UniqueMiscChargeDedupeIndex` |
| Receivables / ageing | Outstanding AR, ageing buckets, funder/care-home views | `CareHome.Receivables` | High | `ReceivableBalance.cs`, `ReceivablesController.cs` |
| Payments & allocations | Record payments, allocate/reverse, capacity checks, rowversion | `CareHome.Payments` | High | `PaymentService.cs`, `InvoiceAllocationCapacity.cs` |
| Bank CSV import | Accounts, CSV preview/commit, mapping templates | `CareHome.Reconciliation`, `IFormFile` | High | `BankingController.cs`, `BankImportService.cs` |
| Bank reconciliation | Scored suggestions, confirm/ignore/unapplied/reverse | Rule-based scorer (not ML) | High | `ReconciliationMatchScorer.cs`, `ReconciliationMatchingEngine.cs` |
| Remittance import | CSV/XLSX batches, checksum dedupe, auto-match | ClosedXML | High | `RemittanceService.cs`, `RemittancesController.cs` |
| Collections / reminders | Policy, dashboard, manual + scheduled stage emails with PDF | `BackgroundService`, SMTP | High | `CollectionReminderService.cs`, `CollectionsReminderHostedService.cs` |
| Disputes | Open disputes, messages, resolve | Workflow service | Medium | `DisputesController.cs`, `DisputeWorkflowService.cs` |
| Revenue assurance | Scan for unbilled residents, expired contracts, near-expiry | Rule-based findings | High | `RevenueAssuranceService.cs` |
| Reporting | 9 reports (census, rates, invoices, income, occupancy, exceptions, outstanding); CSV/XLSX | `ReportService`, ClosedXML | Medium–High | `ReportsController.cs` |
| Sage 50 export | Preview/export invoices to Sage CSV; retry/download | `SqlAppLock`, document store | High | `SageExportService.cs`, `SageExportsController.cs` |
| Email / notifications | SMTP or simulated send; logs; resend; reminder templates | `IEmailSender`, `EmailSendLog` | Medium | `ConfigurableEmailSender.cs`, `EmailSendLogsController.cs` |
| Audit trail | Append-only audit log with JSON old/new values | `IAuditWriter` | Medium | `AuditService.cs`, `AuditController.cs` |
| Dashboard / finance attention | Tenant KPIs and finance attention feed | Controllers + services | Medium | `DashboardController.cs`, `FinanceAttentionService.cs` |
| Organisation settings | Tenant email and billing-policy settings | Settings API | Medium | `OrganisationSettingsController.cs` |
| Document / file handling | Logos, PDFs, Sage CSVs; 2 MB multipart limit; path confinement | `LocalDocumentStore`, QuestPDF | Medium | `Documents/LocalDocumentStore.cs` |
| Search / filter | Query-string search on clients, companies, invoices, banking | REST query params | Low–Medium | e.g. `ClientsController`, `InvoicesController` |
| Import / export | Misc CSV, bank CSV, remittance CSV/XLSX, Sage CSV, report export | ClosedXML / CSV | High | Controllers and services above |
| Background jobs | Daily collections reminders (configurable UTC hour) | `BackgroundService` only | Medium | `CollectionsReminderHostedService.cs` |
| Platform tenant admin | Provision/activate tenants | Platform-only policy | Medium | `PlatformTenantsController.cs` |
| AI functionality | **Not implemented** | — | — | Reconciliation scoring is deterministic rules |
| Card/payment gateway | **Not present** — internal payment recording only | Domain payments module | — | `PaymentService.cs` |

---

## 5. BACKEND ENGINEERING

### REST API design

30 controllers under `backend/CareHome.Api/Controllers/`. Routes use `/api/...`, a global authenticated-user filter, `[RequireTenant]` on operational APIs, and named policies on sensitive controllers.

**Approximate meaningful HTTP endpoints: 161**

| Verb | Count (controller attributes) |
|------|------------------------------:|
| GET | 76 |
| POST | 60 |
| PUT | 14 |
| PATCH | 2 |
| DELETE | 9 |
| **Total** | **161** |

### Controllers / endpoints

| Controller | Endpoints |
|------------|----------:|
| BankingController | 15 |
| InvoicesController | 9 |
| ReportsController | 9 |
| CareHomesController | 8 |
| InvoiceTemplatesController | 8 |
| PaymentsController | 8 |
| CompaniesController | 7 |
| CreditNotesController | 7 |
| AuthController | 6 |
| FundingContractsController | 6 |
| RemittancesController | 6 |
| ReceivablesController | 6 |
| UsersController | 6 |
| ClientsController | 5 |
| DisputesController | 5 |
| FundingAuthoritiesController | 5 |
| InvoiceCategoriesController | 5 |
| NominalCodesController | 5 |
| SageExportsController | 5 |
| ContractRenewalsController | 4 |
| PlatformTenantsController | 4 |
| RevenueAssuranceController | 4 |
| BillingController | 3 |
| CollectionsController | 3 |
| MiscChargesController | 3 |
| OrganisationSettingsController | 3 |
| DashboardController | 2 |
| EmailSendLogsController | 2 |
| AuditController | 1 |
| FinanceController | 1 |

### Services / business logic

Module packs registered in `Program.cs`: `AddCareHomeFunding()`, `AddCareHomeBilling()`, `AddCareHomeReceivables()`, `AddCareHomePayments()`, `AddCareHomeReconciliation()`, `AddCareHomeRemittance()`, `AddCareHomeRevenueAssurance()`. Workflow services: `CollectionsWorkflowService`, `DisputeWorkflowService`, `ContractRenewalWorkflowService`. Host services for PDF, email, Sage, sequences, audit, reports, and tenant provisioning.

### Validation

Data annotations on DTOs/entities. Imperative validators such as `BillingPreviewRequestValidator` and `CollectionPolicyValidator`. `ProductionStartupValidator` fail-fast for JWT, CORS, LocalDB-in-prod, SMTP gaps. **No FluentValidation package.**

### Exception handling

`AddExceptionHandler<ApiExceptionHandler>()` + `AddProblemDetails()`. Unhandled errors return generic 500 JSON with `correlationId` (`ApiExceptionHandler.cs`). Correlation middleware in the pipeline.

### Database transactions

`BeginTransactionAsync` in Billing, CreditNote, Funding, Payment, Reconciliation, Sage export, and Tenant provisioning. Often paired with `SqlAppLock` (`sp_getapplock`) for exclusive critical sections (`backend/CareHome.Data/Common/SqlAppLock.cs`).

### Pagination / filtering / sorting

Shared helper `backend/CareHome.Api/Common/Pagination.cs` (page ≥ 1, size clamped 1–200). Used on list endpoints. Filtering via query params; ordering in services/controllers.

### Concurrency handling

SQL Server `rowversion` on `Payment`, `BankTransaction`, `PaymentReconciliation`. `SqlAppLock` for billing/funding/Sage/credit-note generate. Document sequences use `UPDLOCK, ROWLOCK, HOLDLOCK`. Unique indexes prevent duplicate misc charges.

### Scheduled / background processing

`CollectionsReminderHostedService` : `BackgroundService` (daily UTC hour, iterates active tenants). No Hangfire or Quartz.

### Idempotency

DTO fields named `IdempotencyKey` exist on remittance/banking DTOs. **Service-layer consumption of those keys was not found** — design stub, not a full idempotency store. Remittance import uses content checksums for duplicate-file prevention. Billing generate uses coverage of already-billed days plus an exclusive app lock.

### Caching

None found.

### Rate limiting

ASP.NET Core `AddRateLimiter` fixed-window policy `"login"`: 10 requests/minute per remote IP on login, forgot-password, and reset-password.

### File handling

Multipart body limit 2 MB. `IFormFile` for bank import, remittance, misc charges, logos. `LocalDocumentStore` sanitizes names and rejects `..` path traversal. QuestPDF writes invoice/credit-note PDFs under tenant folders.

### API integrations

SMTP email. Sage 50 CSV files (not a live Sage API). Oracle Cloud is a **hosting** target, not an Oracle Database driver.

---

## 6. DATABASE ENGINEERING

| Item | Finding |
|------|---------|
| **Database** | Microsoft SQL Server (LocalDB locally; SQL Server 2022 in Docker/CI; Azure SQL in Bicep). **No Oracle EF provider.** |
| **Important tables/entities** | **43** `DbSet<>` properties on `CareHomeDbContext`; ~43 domain entity classes + Identity tables |
| **Migrations** | **20** EF Core migrations (plus snapshot) |

### Relationships

Configured in `CareHomeDbContext.OnModelCreating`. FKs mostly `OnDelete(DeleteBehavior.Restrict)`; client↔guardian uses Cascade. Child tables without `TenantId` (invoice lines, funding rates, care-home access) are loaded only through a tenant-scoped parent.

### Constraints and indexes

- Unique indexes: `(TenantId, Name)` companies; `(TenantId, Code)` care homes/categories/nominals; `(TenantId, SageId)` / `(TenantId, ReferenceNumber)` clients; `(TenantId, InvoiceNumber)`; `(TenantId, DocumentType)` sequences; misc-charge dedupe unique index.
- Tenant-scoped `PublicId` uniqueness on many entities.
- Composite operational indexes (invoice date+status, audit entity type+id).
- Rowversion concurrency tokens on payment/banking entities.

### Transaction handling

Explicit EF transactions + `SqlAppLock` for contested writes. Document numbering uses row locks inside the current transaction.

### Audit / history tables

- `AuditLogs` — append-only with JSON old/new values.
- Operational logs (not SQL temporal tables): `BillingExceptionLog`, `EmailSendLog`, `CollectionReminderLog`, `SageExportBatch`.

### Multi-tenant separation

`TenantId` on owned aggregates; `ITenantOwned` + `TenantQuery.ForTenant`. Users carry `TenantId`. PlatformAdmin has no tenant claim. **No separate schema-per-tenant or database-per-tenant in code** (ops docs recommend a dedicated database per Git branch/environment).

### Particularly complex data models

1. **Funding → billing → invoice lines** — contracts, effective-dated rates, amount basis, document sequences.
2. **Payments + allocations + bank reconciliation** — match groups, suggestions, rowversion, scored candidates.
3. **Receivables** — dual-path legacy Paid flag vs allocation model in `ReceivableBalance`.
4. **Collections** — policy thresholds, reminder logs, scheduled send.
5. **Remittance + disputes** — imported lines, matching, dispute messages.

### Strongest database design examples

- Per-tenant unique document sequences with `UPDLOCK, ROWLOCK, HOLDLOCK` (`DocumentSequenceService.cs`).
- Exclusive `sp_getapplock` around billing generate and Sage export (`SqlAppLock.cs`).
- Unique misc-charge dedupe index to prevent double-import.
- Explicit decision **not** to use EF global query filters, documented with the reasons (`docs/MULTI_TENANCY.md`).
- Funding-contract overlap cannot be expressed as a unique index (open-ended inclusive dates); enforced in domain code (`FundingContractOverlap.cs`).

---

## 7. SECURITY

| Feature | Status | Where / how |
|---------|--------|-------------|
| **JWT** | Implemented | `Program.cs` `AddJwtBearer`; HS256; issuer/audience/lifetime/signing key; clock skew 0–5 min; `AuthController.BuildResponse` issues token (default 8h, 1–12 configurable); `JwtSigningKey` rejects missing/weak/placeholder keys outside Development |
| **OAuth/OIDC** | Not present | No OpenIdConnect / OAuth handlers |
| **Sessions** | Not present | Stateless JWT; token in frontend `localStorage` (`auth.service.ts`) |
| **Password hashing** | Identity default (PBKDF2) | `AddIdentity` + `CheckPasswordAsync`; policy: length ≥12, digit, lower, upper, non-alphanumeric. Optional `LoginPasswordCipher` encrypts password **in transit** for login/change/reset; hashing remains Identity |
| **RBAC / permissions** | Implemented | 5 active roles (`AppRoles`); 14 named policies (`CareHomePolicies` / `CareHomeAuthorizationExtensions`); global `AuthorizeFilter`; `ReadOnlyGuardFilter`; `UserAccessService` for care-home scope |
| **CSRF** | Not implemented as antiforgery | Bearer JWT API (no cookie auth / `ValidateAntiForgeryToken`). Docs note XSS risk of localStorage JWT |
| **CORS** | Implemented | Policy `AllowAngularApp`; origins from config; production empty origins deny all (same-origin SPA); `*` and localhost forbidden outside Development (`ProductionStartupValidator`) |
| **Rate limiting** | Implemented | Fixed-window 10/min/IP on login, forgot-password, reset-password |
| **Lockout** | Implemented | Identity: 5 failed attempts, 15-minute lockout; `AuthController.Login` checks `IsLockedOutAsync` |
| **Input validation** | Implemented | DataAnnotations on DTOs/entities; domain validators; 2 MB upload limit; file-type checks for CSV |
| **Secrets management** | Implemented (ops + fail-fast) | Env vars, Compose `env_file`, GitHub `OCI_*` secrets, Azure Key Vault in Bicep; JWT/CORS/LocalDB/SMTP fail-fast in production. **No `UserSecretsId` in csproj.** OCI Vault not present |
| **Tenant isolation** | Implemented | JWT tenant claims; `[RequireTenant]`; explicit `ForTenant` / `Id && TenantId` queries; `InactiveTenantMiddleware`; tests in `TenantIsolationTests.cs` |
| **Secure headers** | Implemented | `SecurityHeadersMiddleware`: nosniff, DENY frame, no-referrer, Permissions-Policy, CSP split for `/api` vs SPA; production HTTPS redirection + HSTS when enabled |
| **Token refresh** | Not present | No refresh-token endpoint or store |
| **Token revocation** | Partial | No server blacklist; logout clears localStorage only. Password change / `UpdateSecurityStampAsync` invalidates JWTs via stamp check; inactive users fail validation |
| **Audit logging** | Implemented | `AuditService` / `IAuditWriter` → `AuditLogs`; `AuditController` behind `CanViewAudit` |

**Roles:** `PlatformAdmin`, `TenantAdmin`, `Administrator`, `LocationManager`, `ReadOnly`. Legacy `SuperAdmin` is mapped to `PlatformAdmin` on login/seed.

**Policies:** `PlatformOnly`, `CanManageOrganisation`, `CanViewAudit`, `CanManageCareHome`, `CanManageBilling`, `CanManageFunding`, `CanManageReceivables`, `CanViewReceivables`, `CanViewFinancialReports`, `CanManagePayments`, `CanViewPayments`, `CanManageBanking`, `CanViewBanking`, `CanReconcilePayments`.

---

## 8. TESTING AND QUALITY

| Layer | Location | Framework |
|-------|----------|-----------|
| Backend unit / domain / hardening | `CareHome.Api.Tests/*.cs` | xUnit |
| Backend API / SQL integration | `CareHome.Api.Tests/Integration/` | xUnit + `Microsoft.AspNetCore.Mvc.Testing`; `[SqlIntegrationFact]` skips without SQL |
| Frontend unit | 13 `*.spec.ts` files | Vitest + jsdom via Angular unit-test builder |
| E2E / smoke | `scripts/live-ui-test.mjs`, `live-smoke-test.mjs` | Playwright scripts; **not in CI** |

**Counts (verified):**

- Backend test files named `*Tests.cs`: **25**
- Backend test methods (`[Fact]`, `[Theory]`, `[SqlIntegrationFact]` attributes): **102**
- Frontend spec files: **13**
- Frontend `it(` cases: **21**

**Quality tools**

| Tool | Present? |
|------|----------|
| GitHub Actions quality gates | Yes — restore/build, EF `has-pending-model-changes`, apply migrations against CI SQL Server 2022, `dotnet test`, Angular build + Vitest, `Dockerfile.prod` build |
| Prettier | Yes (`format` / `format:check`) |
| EditorConfig | Yes (frontend + limited backend) |
| ESLint | No |
| SonarQube | No |
| TreatWarningsAsErrors / AnalysisLevel | Not configured beyond nullable |
| Coverage reports / Coverlet | **Not found** |

**Coverage not verified.**

---

## 9. DEVOPS / CLOUD

| Concern | Present? | Evidence |
|---------|----------|----------|
| Docker | Yes | `backend/CareHome.Api/Dockerfile`, `Dockerfile.dev`, `Dockerfile.prod`; frontend Dockerfile |
| Docker Compose | Yes | 3 files: `docker-compose.yml`, `docker-compose.dev.yml`, `docker-compose.prod.yml` |
| Kubernetes | No | — |
| Terraform | No | — |
| Bicep | Yes | `infra/azure/main.bicep` — App Service (.NET 10), Azure SQL (TLS 1.2), Key Vault, Azure Files, Recovery Services backup |
| AWS / GCP | No | — |
| Oracle Cloud | Yes | `docs/ORACLE_CLOUD_DEPLOYMENT.md`, `deploy-oracle.yml`, `infra/oracle/Caddyfile.example` |
| GitHub Actions | Yes | `ci.yml`, `quality-gates.yml`, `deploy-oracle.yml` |
| GitLab CI | No | — |
| Reverse proxy / TLS | Yes | Caddy example; Azure `httpsOnly` + min TLS 1.2; Compose prod binds API to localhost |
| Environment configuration | Yes | `appsettings*.json`, `.env.example`, `.env.production.example`, `.env.demo.example` |
| Secrets management | Yes | Env vars, GitHub secrets, Azure Key Vault references in docs/Bicep |
| Database backup/recovery | Yes | Bicep PITR/LTR; `scripts/Backup-CareHome.ps1`; `docs/BACKUP_RESTORE.md`; restore drill runbook |
| Monitoring/alerts | Partial | OpenTelemetry + optional Azure Monitor; health endpoints. No full alerting product found in code |

### Deployment flow (visible)

**A. Automated Oracle (CI/CD)** — `.github/workflows/deploy-oracle.yml`:

1. Trigger: push to `revenue-cycle-v2` or `workflow_dispatch`.
2. Quality gates must pass.
3. Build: Node 22 Angular production build → .NET 10 publish → copy SPA into `wwwroot` → tar artifact.
4. Deploy (`environment: production`): SCP to OCI VM → stop systemd `carehome-api` → extract (preserve `App_Data`) → `apply-database-migrations-on-host.sh` → start → poll `http://127.0.0.1:5000/health/live`.

**B. Docker Compose on OCI VM (manual):** `docker-compose.prod.yml` + `.env.production`; TLS via Caddy.

**C. Azure:** `infra/azure/main.bicep` / `scripts/Deploy-Azure.ps1`; same-origin SPA+API; Key Vault secrets.

**D. Local:** Compose or `dotnet watch` + `ng serve`; migrations via `dotnet ef` or `--apply-migrations`. Production does **not** auto-apply migrations at API startup.

---

## 10. COMPLEX ENGINEERING PROBLEMS

### 1. Period billing engine with coverage, grouping, and concurrency

- **Problem:** Generate invoices across residents/contracts/rates for a date range without double-billing, supporting daily/weekly/monthly rates, grouping, templates, and concurrent generate runs.
- **Implementation:** `BillingService` builds preview lines, exception logs, and coverage of already-billed days; `RateCalculator` applies inclusive-day and monthly pro-rata math; `BillingInvoiceGrouper` groups invoices; generate takes `SqlAppLock` `billing-generate-{tenantId}`.
- **Why technically challenging:** Inclusive date math, monthly slices across month boundaries, idempotent coverage, multi-key grouping, exclusive lock + transaction.
- **Files:** `backend/CareHome.Billing/Billing/BillingService.cs`, `RateCalculator.cs`, `BillingInvoiceGrouper.cs`, `InvoiceTemplateResolver.cs`, `docs/BILLING_ENGINE.md`
- **Skills demonstrated:** Domain financial calculation, concurrency control, transactional workflows

### 2. Bank reconciliation suggestion scoring

- **Problem:** Suggest how bank credits map to open invoices (single or multi-invoice exact sum) with explainable scores.
- **Implementation:** Weighted factors (exact amount 50, invoice ref 30, funder 10, date proximity 5, historical payer 5); candidate windows; persisted score explanations as JSON.
- **Why technically challenging:** Fuzzy reference matching, multi-invoice exact-sum, money rounding, tenant-scoped candidate loading.
- **Files:** `backend/CareHome.Reconciliation/Domain/ReconciliationMatchScorer.cs`, `Services/ReconciliationMatchingEngine.cs`, `Services/ReconciliationService.cs`
- **Skills demonstrated:** Heuristic matching, scoring design, AR integration

### 3. Remittance import and auto invoice matching

- **Problem:** Import funder remittance CSV/XLSX, prevent duplicate files, auto-match lines, then drive payment allocation.
- **Implementation:** Parsers including ClosedXML; content checksum; `AutoMatchBatchAsync` reuses `ReconciliationMatchScorer` against `InvoiceAllocationCapacity`.
- **Why technically challenging:** Cross-format ETL, duplicate prevention, shared scoring with bank rec, outstanding-capacity consistency.
- **Files:** `backend/CareHome.Remittance/Services/RemittanceService.cs`
- **Skills demonstrated:** Import pipelines, matching reuse, modular composition

### 4. Payment allocation with capacity and concurrency safety

- **Problem:** Allocate cash to invoices without over-allocation; support reversal; stay consistent with receivables.
- **Implementation:** `PaymentService` + `InvoiceAllocationCapacity.RemainingCollectible` (mirrors receivable math so Payments stays independent of Receivables); integration tests for concurrent over-allocation.
- **Why technically challenging:** Cross-module money invariants, legacy Paid-flag compatibility, race conditions.
- **Files:** `backend/CareHome.Payments/Services/PaymentService.cs`, `Domain/InvoiceAllocationCapacity.cs`, `CareHome.Api.Tests/Integration/PaymentsIntegrationTests.cs`
- **Skills demonstrated:** Money invariants, modular boundaries, concurrency testing

### 5. Authoritative receivables balance and ageing

- **Problem:** Single source of truth for outstanding AR across gross, credits, allocations, and a legacy Paid flag.
- **Implementation:** Pure domain `ReceivableBalance.Calculate` + `ReceivableAgeing`; used by collections/reminders via `InvoiceReceivableReadModel`.
- **Why technically challenging:** Dual-path accounting without corrupting balances; deterministic ageing as-of a date.
- **Files:** `backend/CareHome.Receivables/Domain/ReceivableBalance.cs`, `ReceivableAgeing.cs`, `backend/CareHome.Api/Services/InvoiceReceivableReadModel.cs`
- **Skills demonstrated:** Domain modelling, migration-safe accounting logic

### 6. Collections reminder stages and multi-tenant scheduled job

- **Problem:** Policy-driven pre-due / overdue / escalation stages; send once per stage; attach PDF; run across tenants.
- **Implementation:** `CollectionReminderStageResolver`; `CollectionReminderService` filters outstanding and logs successes; `CollectionsReminderHostedService` iterates active tenants.
- **Why technically challenging:** Idempotent stage tracking, policy validation, email/PDF composition, hosted-service tenant fan-out.
- **Files:** `backend/CareHome.Api/Services/CollectionReminderStageResolver.cs`, `CollectionReminderService.cs`, `CollectionsReminderHostedService.cs`, `CollectionPolicyValidator.cs`
- **Skills demonstrated:** Workflow state, background processing, notification ops

### 7. Two-level multi-tenant + location authorization

- **Problem:** Platform vs tenant operators; JWT tenant claims; LocationManager limited to assigned homes; cross-tenant denial.
- **Implementation:** `HttpTenantContext`; `UserAccessService`; capability policies; `TenantProvisioningService`; isolation tests.
- **Why technically challenging:** Two-level authz (tenant + location), platform-admin carve-outs, every operational query must filter `TenantId`.
- **Files:** `backend/CareHome.Api/Security/HttpTenantContext.cs`, `UserAccessService.cs`, `Authorization/CareHomePolicies.cs`, `CareHome.Api.Tests/TenantIsolationTests.cs`
- **Skills demonstrated:** Multi-tenancy, claims-based security

### 8. Funding-contract overlap validation

- **Problem:** SQL cannot UNIQUE-index inclusive open-ended date overlaps; identity is tenant+client+authority+category.
- **Implementation:** Pairwise period checks in `FundingContractOverlap`; billing blocked with structured messages; dedicated tests.
- **Why technically challenging:** Open-ended dates, business identity vs DB constraints, billing-time enforcement.
- **Files:** `backend/CareHome.Funding/FundingContractOverlap.cs`, `FundingContractService.cs`, `CareHome.Api.Tests/FundingContractOverlapTests.cs`
- **Skills demonstrated:** Temporal domain rules, constraint design

### 9. Concurrent document numbering

- **Problem:** Unique invoice/credit numbers under concurrent generates.
- **Implementation:** `DocumentSequenceService` selects with `UPDLOCK, ROWLOCK, HOLDLOCK`, then increments `NextValue`.
- **Why technically challenging:** Correct SQL locking semantics for sequence generation under load.
- **Files:** `backend/CareHome.Api/Services/DocumentSequenceService.cs`, `docs/LEARNING_NOTES_03_CONCURRENCY.md`
- **Skills demonstrated:** Database concurrency

### 10. Production hardening and dual-cloud deploy

- **Problem:** Fail closed in production (JWT, CORS, LocalDB, secrets) while supporting Azure App Service and OCI VM + Docker.
- **Implementation:** `ProductionStartupValidator`, `JwtSigningKey`, quality-gates workflow (including pending-migration check), Bicep + Oracle SSH deploy, backup runbooks.
- **Why technically challenging:** Environment-specific fail-fast, same-origin SPA hosting, migrations applied deliberately (not at every API start), backup/restore evidence in docs.
- **Files:** `backend/CareHome.Api` startup validators, `.github/workflows/quality-gates.yml`, `deploy-oracle.yml`, `infra/azure/main.bicep`, `docs/PRODUCTION_READINESS_REPORT.md`
- **Skills demonstrated:** Operational security, CI/CD, cloud packaging

---

## 11. SOFTWARE ENGINEERING PRACTICES

| Practice | Evidence |
|----------|----------|
| **SOLID / interfaces** | `ITenantContext`, `IAuditWriter`, `IDocumentSequence`, `ICareHomeAccessScope`, `IEmailSender`, module contracts in `CareHome.Abstractions` |
| **Design patterns** | Options pattern (`EmailOptions`, `CollectionsReminderOptions`); middleware pipeline; hosted service; pure static domain scorers/calculators; DI module extension methods |
| **DTOs** | Extensive `Dtos/` under API and modules (billing, payments, receivables, collections, remittance, etc.) |
| **Dependency injection** | Primary constructors; `Program.cs` `AddScoped`/`AddSingleton`/`AddHostedService`; `AddCareHomeBilling()` and sibling extensions |
| **Repository / service** | **No generic repository.** Service + `DbContext` throughout — an explicit architecture choice (`docs/ARCHITECTURE.md`) |
| **Domain modelling** | Pure types: `ReceivableBalance`, `ReconciliationMatchScorer`, `FundingContractOverlap`, `InvoiceAllocationCapacity`, `Money`, `DateRanges` |
| **Modularization** | 10 backend projects; 30 frontend feature folders |
| **Separation of concerns** | Thin controllers; workflows in `*WorkflowService`; matching in Reconciliation domain; email in `Email/` |
| **Configuration management** | `appsettings*.json`, `IOptions<>`, env examples, `ProductionStartupValidator` |
| **Error handling** | `ApiExceptionHandler` + ProblemDetails; correlation id; generic 500 bodies |
| **Observability** | `CorrelationIdMiddleware`; OpenTelemetry; custom meters (`CareHomeTelemetry`); `/health/live` and `/health/ready` |
| **API versioning** | **Not present** (no `ApiVersion` / versioned routes) |
| **Feature flags** | `CommercialRevenueFeature` + `CommercialRevenueApiGateMiddleware` |

---

## 12. OWNERSHIP SIGNALS

Based only on repository evidence. Do not treat this as proof of personal authorship.

- **Architecture:** The project contains a documented modular-monolith revenue-cycle split (`CareHome.Billing`, `.Payments`, `.Receivables`, `.Reconciliation`, `.Remittance`, `.Funding`, `.RevenueAssurance`) with abstractions and DI extension registration.
- **Backend:** The project contains a full ASP.NET Core API with 30 controllers, 161 HTTP endpoints, and domain services from billing through collections.
- **Frontend:** The project contains an Angular 22 SPA with 30 feature directories covering the same revenue-cycle surfaces plus platform admin.
- **Database:** The project contains EF Core `CareHomeDbContext` with 43 DbSets, SQL Server, 20 migrations, applocks, and tenant-scoped uniqueness.
- **Security:** The project contains JWT Identity auth, role/policy authorization, tenant claims, lockout, security headers, rate-limited login, stamp-based token invalidation, and production startup validation.
- **Deployment:** The project contains Docker Compose (dev/prod), an OCI GitHub Actions deploy workflow, and Azure Bicep.
- **Infrastructure:** The project contains health checks, forwarded headers, CORS policies, OpenTelemetry wiring, and Caddy TLS examples.
- **Documentation:** The project contains a large `docs/` tree (architecture, billing engine, multi-tenancy, production, Oracle, Azure, UAT, runbooks, demo, pilot) — 100+ markdown files.
- **Testing:** The project contains 102 backend xUnit methods (including SQL-gated integration tests) plus 21 frontend Vitest cases, wired into CI quality gates.
- **Operational readiness:** The project contains production email setup, backup/restore runbooks, smoke-test docs, a paid-pilot go-live checklist, and a scheduled collections reminder host.

---

## 13. METRICS WE CAN SAFELY USE

| Metric | Count | How verified |
|--------|------:|--------------|
| HTTP endpoints | **161** | `[HttpGet/Post/Put/Patch/Delete]` on controllers |
| Controllers | **30** | `*Controller.cs` in `CareHome.Api/Controllers` |
| Domain `DbSet<>` properties | **43** | `CareHomeDbContext.cs` |
| Domain entity files under `Models/` | **40** | file count (some files contain nested types) |
| EF Core migrations | **20** | `CareHome.Data/Migrations/` (excluding Designer/Snapshot) |
| Backend library projects | **10** | `.csproj` under `backend/` |
| Frontend feature folders | **30** | `frontend/care-home-web/src/app/features/` |
| Active roles | **5** (+ 1 legacy `SuperAdmin` alias) | `AppRoles.cs` |
| Named authorization policies | **14** | `CareHomePolicies.cs` |
| Named workflow services | **3** | Collections, Disputes, ContractRenewals |
| Report endpoints | **9** | `ReportsController` HttpGets |
| Backend test files | **25** | `*Tests.cs` |
| Backend test methods | **102** | Fact/Theory/SqlIntegrationFact attributes |
| Frontend spec files | **13** | `*.spec.ts` |
| Frontend `it(` cases | **21** | grep |
| GitHub workflow files | **3** | `.github/workflows/` |
| Docker Compose files | **3** | repo root |
| Cloud/hosting targets documented | **2** | Azure App Service; OCI Compute VM |
| Integrations (code-backed) | **4** | SMTP, Sage 50 CSV, QuestPDF, ClosedXML/bank CSV (not live third-party SaaS APIs) |

**Not claimed:** number of users, tenants in production, revenue, time saved, customer counts, coverage percentage, or live-pilot user counts — none of these are documented as measured outcomes in the repo.

---

## 14. STRONGEST CV MATERIAL

Ranked by technical depth, without exaggeration.

### 1. Multi-tenant revenue-cycle REST API (161 endpoints, 10 modules)

- **Fact:** The project implements a modular-monolith ASP.NET Core API covering billing, receivables, payments, bank rec, remittance, collections, and Sage export, with explicit tenant scoping.
- **Why valuable:** Shows end-to-end domain APIs, not CRUD-only work.
- **Evidence:** 30 controllers; 10 backend projects; `docs/ARCHITECTURE.md`, `docs/MULTI_TENANCY.md`
- **Suitable roles:** Backend, .NET, Full Stack, Architect-track

### 2. Shared-database multi-tenancy without global query filters

- **Fact:** Tenant isolation uses JWT claims, `[RequireTenant]`, and explicit `ForTenant` / `Id && TenantId` queries, with documented reasons for avoiding EF global filters.
- **Why valuable:** Interviewers care about *how* isolation is enforced and tested.
- **Evidence:** `HttpTenantContext.cs`, `TenantQuery.cs`, `TenantIsolationTests.cs`, `docs/MULTI_TENANCY.md`
- **Suitable roles:** Backend, .NET, Architect-track, Solutions Engineer

### 3. Billing engine with rate math, coverage, and SQL applocks

- **Fact:** Period billing supports daily/weekly/monthly rates, already-billed-day coverage, invoice grouping, and exclusive `sp_getapplock` around generate.
- **Why valuable:** Real financial-domain complexity and concurrency.
- **Evidence:** `BillingService.cs`, `RateCalculator.cs`, `SqlAppLock.cs`, `docs/BILLING_ENGINE.md`
- **Suitable roles:** Backend, Java-equivalent domain (explain as .NET), .NET, Architect-track

### 4. Receivables + payment allocation invariants

- **Fact:** Outstanding AR is a pure calculation over gross, credits, allocations, and a legacy Paid flag; payments enforce remaining collectible capacity under concurrency tests.
- **Why valuable:** Accounting correctness and modular boundaries.
- **Evidence:** `ReceivableBalance.cs`, `PaymentService.cs`, `PaymentsIntegrationTests.cs`
- **Suitable roles:** Backend, .NET

### 5. Bank reconciliation scoring engine

- **Fact:** Deterministic weighted matcher (amount, invoice ref, funder, date, historical payer) with explainable factor JSON, reused by remittance auto-match.
- **Why valuable:** Algorithmic matching without claiming ML.
- **Evidence:** `ReconciliationMatchScorer.cs`, `RemittanceService.cs`
- **Suitable roles:** Backend, .NET, Full Stack

### 6. RBAC with 14 capability policies and location scoping

- **Fact:** Five roles, named policies, ReadOnly mutation guard, and LocationManager home-level access after tenant match.
- **Why valuable:** Authorization beyond “admin vs user”.
- **Evidence:** `AppRoles.cs`, `CareHomePolicies.cs`, `ReadOnlyGuardFilter.cs`, `UserAccessService.cs`
- **Suitable roles:** Backend, .NET, Full Stack, Solutions Engineer

### 7. Production fail-closed security (JWT, lockout, headers, rate limit)

- **Fact:** Identity lockout 5/15, login rate limit 10/min/IP, security-stamp token invalidation, production JWT/CORS/LocalDB fail-fast, security headers, audit log.
- **Why valuable:** Shows security was designed, not bolted on.
- **Evidence:** `Program.cs`, `JwtSigningKey.cs`, `ProductionStartupValidator`, `SecurityHeadersMiddleware.cs`, `docs/PRODUCTION_READINESS_REPORT.md`
- **Suitable roles:** Backend, .NET, Cloud, Solutions Engineer

### 8. Dual-cloud packaging: Azure Bicep + OCI Docker CI/CD

- **Fact:** The repo includes Azure App Service/SQL/Key Vault Bicep **and** a GitHub Actions pipeline that quality-gates, builds SPA into API `wwwroot`, and SSH-deploys to an OCI VM with hosted migrations.
- **Why valuable:** Cloud/ops ownership beyond “it runs on localhost”.
- **Evidence:** `infra/azure/main.bicep`, `.github/workflows/deploy-oracle.yml`, `docs/ORACLE_CLOUD_DEPLOYMENT.md`, `docs/AZURE_HOSTING.md`
- **Suitable roles:** Cloud, Full Stack, Solutions Engineer, Architect-track

### 9. SQL concurrency: document sequences + applocks + rowversion

- **Fact:** Invoice numbers use `UPDLOCK, ROWLOCK, HOLDLOCK`; contested workflows use transaction-scoped `sp_getapplock`; payments/bank rows use `rowversion`.
- **Why valuable:** Concrete database-concurrency skills most CRUD apps never demonstrate.
- **Evidence:** `DocumentSequenceService.cs`, `SqlAppLock.cs`, `CareHomeDbContext` rowversion config
- **Suitable roles:** Backend, .NET, Architect-track

### 10. Quality gates with SQL integration tests and migration checks

- **Fact:** CI restores, builds, asserts no pending EF model changes, applies migrations against SQL Server 2022, runs 102 backend tests (including SQL integration), Angular tests, and a production Docker build.
- **Why valuable:** Engineering discipline; deploy is blocked on gates.
- **Evidence:** `.github/workflows/quality-gates.yml`, `CareHome.Api.Tests/Integration/`
- **Suitable roles:** Backend, Full Stack, Cloud

---

## 15. POSSIBLE CV BULLETS

1. Designed and implemented a multi-tenant ASP.NET Core / .NET 10 REST API (30 controllers, 161 endpoints) for care-home billing, receivables, payments, bank reconciliation, remittance, and Sage 50 export, with JWT tenant claims and explicit `TenantId` query scoping.

2. Built a period billing engine in C# that calculates daily/weekly/monthly rates, skips already-billed days, groups invoices by configurable keys, and serializes generate with SQL Server `sp_getapplock` to prevent concurrent double-billing.

3. Modelled authoritative accounts-receivable balances (gross − credits − allocations, with a legacy Paid-flag path) and enforced payment allocation capacity under SQL integration tests for concurrent over-allocation.

4. Implemented a deterministic bank-reconciliation matcher (weighted amount, invoice reference, funder, date, and historical-payer factors) and reused the same scorer to auto-match funder remittance CSV/XLSX imports.

5. Delivered RBAC with 5 roles and 14 named capability policies, a ReadOnly mutation guard, and LocationManager care-home scoping on top of tenant isolation; PlatformAdmin is carved out of operational APIs.

6. Hardened production startup to fail closed on weak/missing JWT keys, LocalDB connections, and unsafe CORS; added Identity lockout (5 attempts / 15 minutes), login rate limiting (10/min/IP), security headers, and security-stamp JWT invalidation.

7. Structured the backend as a modular monolith (Billing, Funding, Receivables, Payments, Reconciliation, Remittance, RevenueAssurance) with interface-based DI, DTOs, and OpenTelemetry instrumentation rather than a generic repository layer.

8. Packaged the Angular 22 SPA into the ASP.NET Core `wwwroot` for same-origin production hosting, with Docker Compose (dev/prod), Azure Bicep (App Service, Azure SQL, Key Vault, backups), and a GitHub Actions OCI SSH deploy gated on tests and EF migration checks.

9. Implemented per-tenant document numbering with `UPDLOCK, ROWLOCK, HOLDLOCK` and tenant-confined PDF/CSV storage (QuestPDF + path sanitization) for invoices, credit notes, and Sage exports.

10. Added policy-driven collections reminders as an `IHostedService` that fans out across active tenants, tracks reminder stages idempotently, and emails invoice PDFs via configurable SMTP. [VERIFY] confirm you personally designed the stage model vs later iteration.

11. Wrote 102 xUnit tests (domain + SQL-gated integration) covering tenant isolation, billing, payments, receivables, reconciliation, remittance, and collections, wired into CI quality gates that also run Angular Vitest and a production container build. [VERIFY] confirm which tests you authored.

12. Authored operational documentation for architecture, multi-tenancy, billing rules, backup/restore, and dual-cloud deploy (Azure + OCI). [VERIFY] confirm you wrote these docs rather than only consuming them.

---

## 16. INTERVIEW TALKING POINTS

### 1. Why a modular monolith, not microservices?

- **Interviewer may ask:** Why not split billing/payments into separate services? Why no repository layer?
- **Explain:** One API host, domain class libraries, service + DbContext, documented in `ARCHITECTURE.md`. Avoid distributed transactions for money.
- **Files:** `docs/ARCHITECTURE.md`, `backend/CareHome.Api/Program.cs`, module `*.csproj` files

### 2. Multi-tenancy without EF global query filters

- **Interviewer may ask:** How do you stop tenant A reading tenant B? Why not `HasQueryFilter`?
- **Explain:** JWT `tenant_id`, `[RequireTenant]`, `ForTenant`, never `FindAsync(id)` alone; filters hurt migrations and platform CRUD.
- **Files:** `docs/MULTI_TENANCY.md`, `HttpTenantContext.cs`, `RequireTenantAttribute.cs`, `TenantIsolationTests.cs`

### 3. Billing generate concurrency and coverage

- **Interviewer may ask:** Two users generate the same period — what happens? How do you avoid double invoices?
- **Explain:** Coverage of billed days + exclusive applock + transaction; rate math in `RateCalculator`.
- **Files:** `BillingService.cs`, `RateCalculator.cs`, `SqlAppLock.cs`, `docs/BILLING_ENGINE.md`

### 4. Inclusive days and monthly proration (open business decision)

- **Interviewer may ask:** How do you count days? How does monthly pro-rata work?
- **Explain:** `DateRanges.InclusiveDays`; monthly slices by calendar month. Be honest that proration is still pending business sign-off.
- **Files:** `RateCalculator.cs`, `docs/OPEN_BUSINESS_DECISIONS.md`, `docs/PRODUCTION_READINESS_REPORT.md`

### 5. Receivable balance vs legacy Paid flag

- **Interviewer may ask:** How is outstanding calculated after you introduced allocations?
- **Explain:** `ReceivableBalance.Calculate`: allocations win; legacy Paid only if allocated = 0.
- **Files:** `ReceivableBalance.cs`, `InvoiceAllocationCapacity.cs`, `InvoiceReceivableReadModel.cs`

### 6. Reconciliation scoring (and why it is not AI)

- **Interviewer may ask:** How do you match bank lines to invoices? Did you use ML?
- **Explain:** Weighted deterministic factors, explainable JSON, multi-invoice exact-sum; reused by remittance.
- **Files:** `ReconciliationMatchScorer.cs`, `ReconciliationMatchingEngine.cs`, `RemittanceService.cs`

### 7. JWT security stamp vs refresh tokens

- **Interviewer may ask:** How do you revoke a token after password change? Why no refresh tokens?
- **Explain:** Stamp claim revalidated on each request; logout is client-side; no blacklist. Know the XSS trade-off of localStorage.
- **Files:** `JwtSecurityStamp.cs`, `AuthController.cs`, `frontend/.../auth.service.ts`

### 8. LocationManager vs tenant vs platform

- **Interviewer may ask:** Walk through authorization for a LocationManager hitting another home’s invoice.
- **Explain:** Authenticated → tenant match → policy → `UserAccessService` home scope → 404 not 403 for unassigned (as documented in production readiness).
- **Files:** `UserAccessService.cs`, `CareHomePolicies.cs`, `ReadOnlyGuardFilter.cs`, `docs/AUTHORIZATION.md`

### 9. Document sequences and SQL locking hints

- **Interviewer may ask:** How do you generate `INV-0001` uniquely under concurrency?
- **Explain:** `UPDLOCK, ROWLOCK, HOLDLOCK` then increment; contrast with applock for whole billing generate.
- **Files:** `DocumentSequenceService.cs`, `docs/LEARNING_NOTES_03_CONCURRENCY.md`

### 10. Production fail-fast and dual-cloud deploy

- **Interviewer may ask:** What happens if JWT key is missing in production? How does OCI deploy differ from Azure?
- **Explain:** `JwtSigningKey` / `ProductionStartupValidator` throw at startup; Azure is App Service + Key Vault Bicep; OCI is VM + Docker/systemd + Caddy; migrations applied as a deploy step, not on every API start.
- **Files:** `JwtSigningKey.cs`, `quality-gates.yml`, `deploy-oracle.yml`, `infra/azure/main.bicep`

---

## 17. INFORMATION STILL NEEDED FROM YOU

These cannot be answered from the codebase and would materially improve CV bullets:

1. Did you build this largely yourself, or in a team — and which modules were yours?
2. Was it commercial, a paid pilot, academic, or a personal/portfolio product?
3. Was it actually deployed (Azure, OCI, or both), or only pipelined/documented?
4. Did real care-home staff use it, or only UAT/demo?
5. How long did development take (calendar and your effort)?
6. What production incidents or bugs did you personally diagnose?
7. Any measurable outcomes (invoices generated, tenants onboarded, UAT pass rate) you can [VERIFY]?
8. Which of the 20 migrations / 10 modules did you introduce vs inherit?
9. Were you responsible for the Angular UI as well as the API?
10. Is the company/product name you should put on the CV different from “Care Home Back-Office”?

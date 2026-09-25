# Phase 0 — Architecture Revalidation

**Date:** 21 September 2026  
**Scope:** Repository inspection only (no product feature work).  
**Goal:** Confirm foundations before evolving toward a **Care Provider Revenue Cycle, Funding & Billing Platform**.

---

## Stack confirmation

| Area | Documented (`docs/ARCHITECTURE.md`, `README.md`) | Repository state | Notes |
|------|--------------------------------------------------|------------------|-------|
| ASP.NET Core | Web API modular monolith | **.NET 10** (`net10.0`), packages **10.0.11** | Matches README; ahead of older docs that said “.NET 8” in some archive files |
| EF Core | SQL Server / LocalDB | **EF Core 10** (`Microsoft.EntityFrameworkCore.SqlServer` 10.*) | Single `CareHomeDbContext` in `backend/CareHome.Api/Data` |
| Frontend | Angular standalone | **Angular 22** (`frontend/care-home-web`) | Vitest-based unit tests via `@angular/build:unit-test` |
| Auth | Identity + JWT | Confirmed in `Program.cs`, `AuthController` | Global `[Authorize]` + `ReadOnlyGuardFilter`; `tenant_id` claim for tenant users |
| PDF | QuestPDF | **2024.12.3**, `LicenseType.Community` in code | Commercial licensing needs explicit configuration (Phase 1.4) |
| Excel | ClosedXML | 0.105.0 | Reports / imports |
| Azure | App Service + SQL + Key Vault | `infra/azure/main.bicep`, `docs/AZURE_HOSTING.md` | Document backup, PITR, Key Vault RBAC |

---

## Tenant model

- **Entity:** `Tenant`, `TenantSettings`, `ApplicationUser.TenantId` (nullable for PlatformAdmin).
- **Runtime context:** `ITenantContext` / `HttpTenantContext` from JWT `tenant_id`.
- **Enforcement:** `[RequireTenant]` on operational controllers; queries use `TenantId` filters and `ForTenant()` (`Common/TenantQuery.cs`).
- **No EF global tenant filter** — isolation is explicit per query (documented in `docs/MULTI_TENANCY.md`). Platform and provisioning flows remain possible without filter bypass hacks.
- **Inactive tenants:** `InactiveTenantMiddleware` returns 403 for authenticated tenant users.
- **Provisioning:** `TenantProvisioningService` + `PlatformTenantsController` (PlatformAdmin only).

**Difference from greenfield RCM designs:** Invoice already has `PaymentStatus` (`NotPaid`, etc.) but no Payment / allocation domain yet (Phase 3–4).

---

## Roles and authorization

| Role | Purpose |
|------|---------|
| PlatformAdmin / SuperAdmin | Tenant provisioning only; no `tenant_id`; operational APIs 403 via `[RequireTenant]` |
| TenantAdmin, Administrator | Full tenant operations |
| LocationManager | `UserCareHomeAccess` scoping; out-of-scope resources **404** |
| ReadOnly | `ReadOnlyGuardFilter` blocks mutating HTTP methods |

**Gap vs Phase 1.6:** Most enforcement is role strings on controllers or implicit in services; named capability policies are not yet centralized (addressed in Phase 1).

---

## Billing and funding (preserve)

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `BillingService` | `Billing/BillingService.cs` | Preview, generate, concurrency via `SqlAppLock`, exception logging |
| `RateCalculator` | `Billing/RateCalculator.cs` | Weekly/monthly proration (marked provisional in business sign-off docs) |
| `InvoiceTemplateResolver` | `Billing/InvoiceTemplateResolver.cs` | Template matching |
| `CreditNoteService` | `Billing/CreditNoteService.cs` | Credit note preview/generate with locks |
| Funding | `ClientFundingContract`, `FundingRate` | Effective-dated rates; overlap rules tested in `FundingContractOverlapTests` |

**Invoice lifecycle:** `Invoice.Status` (e.g. Generated, Sent, Void) separate from `Invoice.PaymentStatus` (e.g. NotPaid, Paid) — suitable base for Phase 3 AR expansion.

---

## Documents, export, audit

| Area | Implementation |
|------|----------------|
| PDF | `Documents/InvoicePdfService.cs`, tenant-scoped paths via `LocalDocumentStore` |
| Sage | `Export/SageExportService.cs`, `Sage50ColumnMap.cs` (provisional per business docs) |
| Audit | `Audit/AuditService.cs`, `AuditLog` entity |
| Sequences | `DocumentSequenceService` per tenant/document type |
| Email | `ConfigurableEmailSender`, `EmailSendLog` |

---

## Observability (before Phase 1)

| Capability | Status |
|------------|--------|
| Correlation ID | `CorrelationIdMiddleware` |
| Structured request scope | `RequestLoggingScopeMiddleware` (UserId, TenantId, Endpoint) |
| Health | `/health/live`, `/health/ready` (SQL ready check) |
| OpenTelemetry / App Insights | **Not present** (Phase 1.3) |
| Billing/email metrics | **Not present** (Phase 1.3) |

---

## Testing (before Phase 1)

| Layer | Status |
|-------|--------|
| Unit tests | `CareHome.Api.Tests` — tenant query helpers, void rules, misc import, JWT/production validators, funding overlap |
| Integration tests | **Not present** (recommended in archive action plan) |
| CI pipeline | **No `.github/workflows`** (Phase 1.1) |
| Frontend tests | Specs exist (e.g. `auth.service.spec.ts`); not gated in CI |

---

## Differences from documentation

1. **Public IDs / portal theme:** Recent migration `20260921100000_AddEntityPublicIdsAndPortalTheme` and UUID routing on frontend — ahead of some older UUID audit docs (partial rollout documented under `docs/ui-ux-refactor/`).
2. **README positioning:** Still “MVP back-office” wording; commercial RCM positioning is strategic (this program), not yet reflected in product copy.
3. **Payment domain:** Docs and schema do not yet describe allocations, remittance, or AR ageing — planned Phases 3–6.
4. **Modularization:** Still single `CareHome.Api` project; Phase 2 will introduce libraries without splitting deployment.

---

## Implementation map (Phases 0–10)

Execution order per product brief. Dependencies shown as “→”.

```
Phase 0  Architecture revalidation (this document)
    ↓
Phase 1  Production hardening
    ├─ 1.1 CI/CD (build, test, migration check, artifacts)
    ├─ 1.2 Integration tests (WebApplicationFactory + SQL Server)
    ├─ 1.3 Observability (OTel + metrics hooks)
    ├─ 1.4 QuestPDF licensing configuration/docs
    ├─ 1.5 Tenant isolation test strategy
    └─ 1.6 Named authorization policies (capabilities)
    ↓
Phase 2  Pragmatic modules (CareHome.Billing, Funding, …) in-solution, same deployable
    ↓
Phase 3  Accounts receivable (amounts, ageing, payment status lifecycle)
    → extends Invoice financial fields; dashboards
    ↓
Phase 4  Payment domain (Payment, PaymentAllocation, idempotency)
    → derives PaymentStatus; transactions + concurrency
    ↓
Phase 5  Bank import & reconciliation (CSV, scoring, confirm workflow)
    → uses Phase 4 allocations
    ↓
Phase 6  Remittance processing (batch/line/match, workspace UI)
    → links to payments and invoices
    ↓
Phase 7  Funder account management (statement, performance KPIs)
    → builds on AR + payments + remittance
    ↓
Phase 8  Funding contract renewal workflow
    → new effective-dated rates only (no history mutation)
    ↓
Phase 9  Revenue leakage detection (deterministic rules, findings)
    → uses billing, contracts, occupancy, misc charges
    ↓
Phase 10 Occupancy-to-billing reconciliation
    → integrates with Phase 9 findings
```

**Reuse in later phases (do not rewrite):** `BillingService`, `RateCalculator`, `CreditNoteService`, `TenantProvisioningService`, `UserAccessService`, `SqlAppLock`, `DocumentSequenceService`, `AuditService`, `SageExportService`, multi-tenancy patterns.

**Schema touchpoints (future):** Phases 3–6 add receivables/payment/remittance tables; Phase 8 adds renewal entities; Phase 9–10 add assurance/reconciliation entities. All require tenant-scoped indexes and audit.

---

## Phase 0 exit criteria

- [x] Repository inspected
- [x] Stack and boundaries confirmed
- [x] Gaps vs target RCM platform identified
- [x] Implementation map for Phases 1–10 produced
- [x] No code changes required for Phase 0

Proceed to **Phase 1** implementation.

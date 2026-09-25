# Phase 2 — Internal Modularization

Phase 2 introduces pragmatic class-library boundaries while keeping **one deployable ASP.NET Core application** (`CareHome.Api`). No microservices, no billing rewrite, no schema churn.

## Before — dependency structure

All revenue-cycle logic lived in `CareHome.Api`:

```
Controllers (HTTP)
  → BillingService / CreditNoteService / RateCalculator / InvoiceTemplateResolver
  → FundingContractsController (DbContext + audit + locks inline)
  → CareHomeDbContext + EF entities
  → AuditService, UserAccessService, DocumentSequenceService, SqlAppLock
  → Security (JWT, ITenantContext, policies)
```

**Risks identified**

| Risk | Detail |
|------|--------|
| Circular reference risk | Billing and funding both needed shared EF models; without a data layer, modules could not reference `CareHomeDbContext` without referencing the API host. |
| Controller → DbContext | `FundingContractsController` orchestrated transactions, `sp_getapplock`, overlap rules, and audit directly. |
| Billing ↔ funding coupling | `BillingService` loaded `ClientFundingContract` / `FundingRate` via EF includes; overlap rules lived under `Billing` namespace. |
| Magic strings | Invoice / payment / contract statuses scattered as literals. |
| Infrastructure in domain | `BillingService` depended on concrete `AuditService`, `UserAccessService`, `DocumentSequenceService`. |

## After — project / module structure

```
CareHome.Api                    (host: controllers, security, audit impl, DI composition, SPA)
  ↓
CareHome.Billing                (BillingService, CreditNoteService, RateCalculator, templates, billing DTOs)
  ↓
CareHome.Funding                (FundingContractService, overlap rules, funding DTOs, IFundingContractQuery)
  ↓
CareHome.Data                   (CareHomeDbContext, entities, migrations, shared primitives)
  ↑
CareHome.Abstractions           (IAuditWriter, ICareHomeAccessScope, IDocumentSequence, ITenantContext, telemetry meters)
```

**Dependency rules**

| Rule | Enforcement |
|------|-------------|
| 1 | `CareHome.Billing` must not reference `CareHome.Api`. |
| 2 | `CareHome.Funding` must not reference `CareHome.Api`. |
| 3 | Future Receivables/Payments interact with billing through explicit contracts, not invoice internals. |
| 4 | Future Revenue Assurance uses read/query contracts, not rules embedded in `BillingService`. |
| 5 | Single shared `CareHomeDbContext` in `CareHome.Data` (no split DbContexts in Phase 2). |
| 6 | No microservice extraction without operational evidence. |
| 7 | No generic `IRepository<T>` abstraction. |
| 8 | Invoice snapshots and billing invariants unchanged unless fixing a verified bug. |

Allowed references:

- `CareHome.Api` → Abstractions, Data, Billing, Funding  
- `CareHome.Billing` → Abstractions, Data, Funding  
- `CareHome.Funding` → Abstractions, Data  
- `CareHome.Data` → (EF / Identity packages only)  
- `CareHome.Abstractions` → BCL only  

## Billing ownership (`CareHome.Billing`)

- `BillingService`, `CreditNoteService`, `RateCalculator`, `InvoiceTemplateResolver`, `InvoiceVoidRules`
- DTOs: `Dtos/Billing`, `Dtos/CreditNotes`
- DI: `AddCareHomeBilling()`
- Depends on `IFundingContractOverlap` behavior via **`CareHome.Funding`** (`FundingContractOverlap`)
- Depends on infrastructure through **`IAuditWriter`**, **`ICareHomeAccessScope`**, **`IDocumentSequence`**

Billing still uses EF directly inside the module for preview/generate (same behavior). Phase 3 can introduce **`IFundingRateProvider` / `IFundingContractQuery`** consumption from billing without exposing entities.

## Funding ownership (`CareHome.Funding`)

- `FundingContractOverlap`, `FundingContractService`, `FundingContractQuery`
- DTOs: `Dtos/FundingContracts`
- Contract: **`IFundingContractQuery`** (overlap checks, contract-used-on-invoice queries)
- DI: `AddCareHomeFunding()`
- Effective-date / rate overlap validation for CRUD lives in `FundingContractService` (moved out of the controller)

**Not moved (still API host):** `FundingAuthoritiesController` — master data CRUD; can move in a later slice if needed.

## Shared persistence (`CareHome.Data`)

- `CareHomeDbContext`, all EF entities, migrations (unchanged snapshot)
- Shared primitives: `Money`, `DateRanges`, `SqlAppLock`, `DocumentTypes`, `DefaultInvoiceCategories`, `RateFrequencies`
- **`DomainStatuses`**: centralized persisted status constants (`InvoiceStatuses`, `PaymentStatuses`, `CreditNoteStatuses`, `FundingContractStatuses`)
- `ApplicationUser` (Identity entity required by DbContext)
- `ITenantOwned` / `TenantQuery`

## Shared infrastructure (intentionally in `CareHome.Api`)

- JWT / Identity composition, authorization policies, middleware
- `AuditService` (implements `IAuditWriter`)
- `UserAccessService` (implements `ICareHomeAccessScope`)
- `DocumentSequenceService` (implements `IDocumentSequence`)
- Document store, email, QuestPDF, Sage export, telemetry **host registration** (`AddCareHomeTelemetry`)
- Tenant provisioning, correlation ID, exception handling

## Cross-module contracts (`CareHome.Abstractions` + `IFundingContractQuery`)

| Interface | Purpose |
|-----------|---------|
| `IAuditWriter` | Modules log audit events without referencing HTTP audit implementation. |
| `ICareHomeAccessScope` | Care-home scoping for billing/credit notes without `UserAccessService`. |
| `IDocumentSequence` | Invoice/credit note numbering without SQL sequence implementation details. |
| `ITenantContext` | Tenant id for host middleware; defined in abstractions, implemented in API. |
| `IFundingContractQuery` | Read-side funding checks for services and future billing decoupling. |

## Domain status types

Persisted values unchanged. Constants live in `CareHome.Data/Common/DomainStatuses.cs`. Payment states for Phase 3 (`PARTIALLY_PAID`, `OVERDUE`, etc.) are **not** added yet.

## Public ID convention (target)

- **New** externally exposed commercial modules should prefer stable **non-sequential public identifiers** (GUID/`PublicId`) on routes where practical.
- Existing int routes (`/api/invoices/{id:int}`, funding contracts, etc.) remain for backward compatibility; migrate opportunistically.

## Future modules (documented only — not implemented)

- `CareHome.Receivables`
- `CareHome.Payments`
- `CareHome.Remittance`
- `CareHome.RevenueAssurance`
- `CareHome.Integrations`

## Controller map (after)

```
BillingController           → BillingService
InvoicesController          → DbContext + PDF/email (unchanged; AR phase may extract)
CreditNotesController       → CreditNoteService
FundingContractsController  → FundingContractService
FundingAuthoritiesController → DbContext (unchanged)
ReportsController           → ReportService (unchanged)
```

## EF migrations

Design-time and CI check:

```bash
dotnet ef migrations has-pending-model-changes \
  --project backend/CareHome.Data/CareHome.Data.csproj \
  --startup-project backend/CareHome.Api/CareHome.Api.csproj
```

## Angular

No intentional UI or route changes in Phase 2.

## Architecture risks remaining

1. **`BillingService` still composes large EF graphs** for funding contracts — acceptable for Phase 2; introduce `IFundingRateProvider` before Phase 3 AR if test isolation becomes painful.
2. **Invoice list/void/send** remain controller-heavy — natural home for `CareHome.Receivables` in Phase 3.
3. **Single DbContext** — module boundaries are logical until operational pain justifies bounded contexts or read replicas.

---

**Phase status:** PHASE 2 = COMPLETE (modular boundaries, DI extensions, funding service extraction, status constants, docs; behavior preserved).

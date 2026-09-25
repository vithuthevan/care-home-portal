# Architecture

This repository is a **pragmatic modular monolith**.

- Backend: `backend/CareHome.Api` — ASP.NET Core Web API, EF Core, SQL Server / LocalDB
- Frontend: `frontend/care-home-web` — Angular 22 standalone SPA, Reactive Forms, HttpClient, `/api` proxy

There is no Domain/Application/Infrastructure split, no generic repository, and no NgRx.

## Request flow

Angular feature page → feature service or `HttpClient` → `authInterceptor` (JWT) → ASP.NET controller → service (billing, PDF, email, export) → `CareHomeDbContext` → SQL Server.

Business-heavy work lives in services:

| Area | Location |
|---|---|
| Billing preview/generate | `Billing/BillingService.cs` |
| Rate formulas | `Billing/RateCalculator.cs` |
| Template matching | `Billing/InvoiceTemplateResolver.cs` |
| Credit notes | `Billing/CreditNoteService.cs` |
| PDF | `Documents/InvoicePdfService.cs` |
| File storage | `Documents/LocalDocumentStore.cs` |
| Email | `Email/ConfigurableEmailSender.cs` |
| Sage CSV | `Export/SageExportService.cs`, `Export/Sage50ColumnMap.cs` |
| Audit | `Audit/AuditService.cs` |
| Authz scope | `Security/UserAccessService.cs`, `Security/ITenantContext.cs` |
| Tenant onboarding | `Security/TenantProvisioningService.cs` |

## Authentication

ASP.NET Core Identity + JWT Bearer. Global `[Authorize]` filter; `[AllowAnonymous]` on login. Claims include `sub`, roles, and `tenant_id` (omitted for PlatformAdmin). Read-only users are blocked from mutating HTTP methods by `ReadOnlyGuardFilter`. Location Managers are scoped by `UserCareHomeAccess` **after** tenant match. PlatformAdmin cannot read operational APIs (403).

See `docs/MULTI_TENANCY.md`.

Named capability policies (`CareHomePolicies`) complement role checks on sensitive controllers. See `docs/AUTHORIZATION.md`.

## Observability

OpenTelemetry instruments HTTP, outbound HTTP, and EF Core. Custom meters cover billing generate and email failures. Correlation IDs flow through `CorrelationIdMiddleware` and request logging scopes. Azure Monitor activates when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set.

## Documents

Generated PDFs and Sage files are stored under `tenants/{publicId}/invoices|credit-notes|sage-exports/` inside `App_Data/documents` (or `DocumentStorage:RootPath`).

# Phase 1 — Production & commercial hardening

**Date:** 21 September 2026  
**Status:** Implemented (foundation for revenue-cycle evolution)

## 1.1 CI/CD

- Added `.github/workflows/ci.yml`:
  - Backend restore, build, EF migration model check, tests, publish artifact
  - Frontend `npm ci`, build, unit tests, SPA artifact
  - SQL Server service container for integration tests (`CAREHOME_TEST_SQL`)
  - No automatic production deploy

## 1.2 Integration tests

- `CareHome.Api.Tests/Integration/` — `WebApplicationFactory`, SQL Server, JWT auth helpers
- Scenarios: login, cross-tenant invoice 404, location scope 404, read-only billing 403, billing preview/generate, duplicate billing, invoice PDF auth, credit-note read-only 403, platform provision + operational 403

## 1.3 Observability

- OpenTelemetry (ASP.NET Core, HTTP, EF Core) with console exporter in Development
- Azure Monitor when `APPLICATIONINSIGHTS_CONNECTION_STRING` or `ApplicationInsights:ConnectionString` is set
- Meters: `carehome.billing.generate.*`, `carehome.email.send.failures`
- Existing correlation ID and request logging scopes preserved

## 1.4 QuestPDF licensing

- `QuestPdfLicenseConfigurator` + `docs/QUESTPDF_LICENSING.md`
- `QuestPdf:LicenseType` in `appsettings.json` (Community default)

## 1.5 Tenant isolation strategy

- `docs/revenue-cycle/TENANT_ISOLATION_TEST_STRATEGY.md`
- Integration suite encodes mandatory negative tests

## 1.6 Authorization policies

- `CareHomePolicies` + registration extension
- Applied to billing, credit notes (mutations), reports, audit, organisation settings, platform tenants

## Commercial strength

- Repeatable CI proof of build, migrations, and tenant-safety scenarios
- Telemetry ready for pilot operations and finance cutover
- Clear licensing path for PDF at scale
- Capability-based authz foundation for receivables/payments work in Phase 3–4

## Next

Phase 2 — extract billing/funding modules within the monolith without changing deployable shape.

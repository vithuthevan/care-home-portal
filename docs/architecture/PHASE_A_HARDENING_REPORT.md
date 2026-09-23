# Phase A — V1 Hardening Report

**Date:** 2026-09-23  
**Source of truth:** `docs/architecture/CURRENT_PRODUCT_AUDIT.md`  
**Commercial Revenue:** Not enabled (unchanged).

---

## 1. Summary

Phase A focused on safer master data, clearer billing/credit-note UX, Sage export recovery, audit usability, and regression tests—without rewriting `BillingService`, changing Sage column semantics, or enabling Commercial Revenue.

**Status: PARTIAL** — core hardening items in this batch are implemented; a full A10 UI consistency sweep and deeper form/UUID audits remain for follow-up.

---

## 2. Changes made

| Area | Change |
|------|--------|
| A1 Master data | Usage counts from real FKs/snapshots; deactivate confirmations reference usage; no hard deletes added |
| A2 Nominal codes | `ConfigurationSource`, `Usage` on API; list shows usage; organisation-only labelling |
| A2 Invoice categories | `SystemDefault` vs `Organisation`; block code change & deactivation for default categories |
| A3–A4 Billing | Ready vs requires-attention banners on existing preview (no calculation changes) |
| A5 Credit notes | Form reset + success message after generate |
| A7 Sage export | `RetryFileWriteAsync` + `POST /api/sage-exports/{id}/retry-file`; UI Retry CSV for `FileMissing` |
| A9 Audit | Action filter, more entity types, reference column, safe navigation links |
| A13 Tests | `PhaseAHardeningTests` for default category codes and usage DTO totals |

---

## 3. Files changed

### Backend

- `backend/CareHome.Data/Common/DefaultInvoiceCategories.cs`
- `backend/CareHome.Api/Dtos/Common/MasterDataUsageDto.cs` (new)
- `backend/CareHome.Api/Services/MasterDataUsageService.cs` (new)
- `backend/CareHome.Api/Controllers/NominalCodesController.cs`
- `backend/CareHome.Api/Controllers/InvoiceCategoriesController.cs`
- `backend/CareHome.Api/Controllers/FundingAuthoritiesController.cs`
- `backend/CareHome.Api/Dtos/NominalCodes/NominalCodeDto.cs`
- `backend/CareHome.Api/Dtos/InvoiceCategories/InvoiceCategoryDto.cs`
- `backend/CareHome.Api/Dtos/FundingAuthorities/FundingAuthorityDto.cs`
- `backend/CareHome.Api/Export/SageExportService.cs`
- `backend/CareHome.Api/Controllers/SageExportsController.cs`
- `backend/CareHome.Api/Program.cs`

### Frontend

- `frontend/care-home-web/src/app/shared/format/master-data-usage.ts` (new)
- Nominal codes, invoice categories, billing workspace, credit notes, sage export, audit list (TS/HTML/models)

### Tests & docs

- `CareHome.Api.Tests/PhaseAHardeningTests.cs` (new)
- `docs/architecture/PHASE_A_HARDENING_REPORT.md` (this file)

---

## 4. Database changes

None. No EF migrations.

---

## 5. API changes

| Endpoint | Change |
|----------|--------|
| `GET /api/nominal-codes`, `GET /api/nominal-codes/{id}` | `configurationSource`, `usage` |
| `GET /api/invoice-categories`, `GET /api/invoice-categories/{id}` | `configurationSource`, `usage` |
| `PUT /api/invoice-categories/{id}` | Rejects changing code of default categories |
| `DELETE /api/invoice-categories/{id}` | Rejects deactivating default categories |
| `GET /api/funding-authorities`, `GET /api/funding-authorities/{key}` | `usage` |
| `POST /api/sage-exports/{id}/retry-file` | Regenerates CSV for `FileMissing` batches |

Existing audit `GET /api/audit` already supported `action`; UI now sends it.

---

## 6. UI changes

- Nominal code list: usage column; richer deactivate confirmation.
- Invoice category list: blocks deactivating system defaults client-side; usage-aware confirm.
- Billing workspace: **Ready to bill** / **Requires attention** banners.
- Credit note workspace: clears create form after successful generate.
- Sage export: batch status column; **Retry CSV** when `FileMissing`.
- Audit: action filter, entity reference, links to invoice/resident/company/care home where safe.

Accent colour remains on care-home portal settings only; global user menu keeps light/dark mode only.

---

## 7. Tests added/updated

- `PhaseAHardeningTests` — `DefaultInvoiceCategories.IsSystemDefaultCode`, `MasterDataUsageDto` aggregation.
- Existing integration tests (`ApiIntegrationTests`) already cover tenant scope, billing duplicate protection, location manager scope, read-only billing.

---

## 8. Existing risks discovered

- Sage export still commits invoice marks before CSV write (by design); failure leaves `FileMissing` until retry.
- Audit list DTO still omits before/after JSON (unchanged).
- Invoice template list does not yet surface usage counts (API helper exists; UI not extended).

---

## 9. Risks fixed / mitigated

- Operators can recover `FileMissing` Sage batches without re-exporting invoices.
- Default invoice categories (incl. `MISC`) cannot be deactivated or renamed away.
- Master data lists show real financial usage instead of implied safety.
- Billing preview distinguishes pass vs blocked generation at a glance.

---

## 10. Risks intentionally deferred

- Full A10 spacing/theme pass across all V1 screens.
- A11 exhaustive form required/optional audit (resident reference, Sage ID, etc.).
- A12 UUID route audit for every secondary link.
- Invoice template usage in UI; funding authority list usage column.
- Dedicated SQL integration test for Sage retry (network restore required for full test run).

---

## 11. Commercial Revenue status

**Disabled.** No changes to `COMMERCIAL_REVENUE_ENABLED`, middleware, or guards.

---

## 12. Sage export status

- **Preview / validation:** unchanged (`Sage50ColumnMap`, line snapshots).
- **Write failure:** batch → `FileMissing`; invoices stay marked exported.
- **Recovery:** `POST /api/sage-exports/{id}/retry-file` rebuilds CSV from linked invoices.

---

## 13. Remaining V1 work (suggested)

1. Invoice template & funding authority list usage columns (API ready for authorities/categories/nominals).
2. A10–A12 UI consistency and form labelling pass.
3. Optional integration test: Sage `FileMissing` → retry → download.
4. Dashboard API regression tests (noted in audit).

---

## Phase completion

**PHASE A: PARTIAL** — financially sensitive paths hardened in this batch; broader UI/UUID/form sweeps and template usage UX remain.

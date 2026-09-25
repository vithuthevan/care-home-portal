# Phase A3 — Implementation report

**Date:** 2026-09-23  
**Commercial Revenue:** Disabled (unchanged)  
**Financial rules / migrations:** Unchanged per scope

---

## 1. Completed

| Part | Deliverable |
|------|-------------|
| 1 UUID routes | Audit report; credit note invoice links; care-home create redirect; company→add home query; `careHome` / UUID billing handoff; client & invoice list filters |
| 2 Breadcrumbs | UUID-safe `setFromUrl` patterns; invoice list Billing parent; care home settings crumb; pages set entity names (existing + invoice list) |
| 3 Navigation continuity | Care home → residents/invoices filters; billing context resolution for UUID query keys |
| 4 Billing handoff | `company`, `careHome`, `client` query resolution; scope banners preserved |
| 5 Credit notes | List invoice link via `invoicePublicId`; existing invoice→credit-note query context unchanged |
| 6–9 Forms/tables/icons | Relied on A2.5 form audit + existing shared components; company save disabled when invalid |
| 10 Pagination | Documented: nominal/category/template/platform remain numeric routes; lists already paged where API supports |
| 11 Theme | Verified unchanged (portal accent on care home settings only) |
| 12 Company details | Reviewed — uses API fields only (counts, status, care homes table) |
| 13 Payment UI | Verified single section on invoice detail (no `PaidAt` — documented deferral) |
| 14 Reports | Dashboard outstanding KPI → `report=outstanding` |
| 15 Audit | No permission changes; Phase A audit UX retained |
| 16 Accessibility | Invoice select `aria-label` already present; icon actions use `ariaLabel` |
| 17 Visual | No redesign; incremental consistency only |
| 18 Manual QA | `docs/qa/PHASE_A3_MANUAL_QA.md` |
| 19 Regression | `entity-route.spec.ts`, client profile UUID spec; dotnet build; npm build; `PhaseAHardeningTests` |
| 20 Reports | This file + `PHASE_A3_UUID_COMPLETION_REPORT.md` |

---

## 2. Partially completed

| Item | Notes |
|------|-------|
| Master data UUID routes | Nominal codes, invoice categories, templates — **no `PublicId` in domain**; numeric edit URLs retained |
| Full A10 table/icon sweep | Existing patterns reused; not every screen re-audited line-by-line |
| Care home list unpaged fetch for search | Still documented in audit when API returns full set |
| Invoice mutation URLs (PDF/pay/void) | Still use internal id from loaded DTO (API int routes) |
| Credit note preview API | **`InvoiceId` not on preview contract** — workflow uses period + optional `ClientId`; documented |

---

## 3. Not implemented

| Item | Reason |
|------|--------|
| `PaidAt` on invoices | No domain column; product decision required |
| PublicId for nominal/category/template | Would need migration + API dual-key |
| Finance read-only audit access | No domain evidence to change permissions |
| Commercial Revenue surfaces | Explicitly out of scope |
| Revenue / Revenue Assurance | Stop condition |

---

## 4. Files changed

### Backend

- `backend/CareHome.Billing/Dtos/CreditNotes/CreditNoteDtos.cs`
- `backend/CareHome.Api/Controllers/CreditNotesController.cs`

### Frontend

- `frontend/care-home-web/src/app/features/dashboard/dashboard.ts`
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-form/care-home-form.ts`
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-dashboard/care-home-dashboard.ts`
- `frontend/care-home-web/src/app/features/companies/pages/company-detail/company-detail.html`
- `frontend/care-home-web/src/app/features/companies/pages/company-form/company-form.html`
- `frontend/care-home-web/src/app/features/clients/pages/client-list/client-list.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-profile/client-profile.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-profile/client-profile.spec.ts`
- `frontend/care-home-web/src/app/features/billing/pages/billing-workspace/billing-workspace.ts`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-list/invoice-list.ts`
- `frontend/care-home-web/src/app/features/credit-notes/pages/credit-note-workspace/credit-note-workspace.ts`
- `frontend/care-home-web/src/app/features/credit-notes/pages/credit-note-workspace/credit-note-workspace.html`
- `frontend/care-home-web/src/app/shared/ui/breadcrumb.service.ts`
- `frontend/care-home-web/src/app/shared/routing/entity-route.spec.ts` (new)

### Docs

- `docs/architecture/PHASE_A3_UUID_COMPLETION_REPORT.md`
- `docs/architecture/PHASE_A3_IMPLEMENTATION_REPORT.md`
- `docs/qa/PHASE_A3_MANUAL_QA.md`

---

## 5. API changes

| Endpoint | Change |
|----------|--------|
| `GET /api/credit-notes` (paged list items) | `invoicePublicId` on `CreditNoteDto` |
| `GET /api/credit-notes/{id}` | `invoicePublicId` on detail DTO |

No billing, Sage, or master-data calculation changes.

---

## 6. Database changes

None.

---

## 7. Routes changed (user-facing)

See `PHASE_A3_UUID_COMPLETION_REPORT.md`.

---

## 8. UX changes

- Outstanding dashboard KPI → outstanding report.
- Care home create → dashboard with stable id.
- Filter/query params prefer UUID keys for care home and billing handoff.
- Credit note list invoice links use public invoice id.

---

## 9. Accessibility changes

- Company save respects `form.invalid` (prevents silent invalid submit).
- No regression to invoice checkbox labels.

---

## 10. Tests

| Test | Purpose |
|------|---------|
| `entity-route.spec.ts` | UUID vs numeric route keys |
| `client-profile.spec.ts` | Profile loads with UUID param |
| `PhaseAHardeningTests` | Existing master-data regression |

---

## 11. Manual QA status

Script authored; **not executed in CI** (operator sign-off table empty).

---

## 12. Remaining risks

- Bookmarks with legacy `careHomeId` / `clientId` query ints still work but new links prefer UUID keys.
- Credit note list without `invoicePublicId` on old cached API responses falls back to numeric invoice segment.
- Master data edit URLs expose integer ids until Phase B domain ids exist.

---

## 13. Deferred product decisions

- `PaidAt` and payment history display.
- `InvoiceId` on credit note preview for invoice-scoped adjustments only.
- `PublicId` for configuration entities (nominal, category, template).
- Server-side-only care home search/pagination API.

---

PHASE A3 PARTIAL

# Care Home UX — implementation report

**Date:** 2026-09-22

## Summary

Refined the care home list, dashboard, create/edit flows, and handoffs to residents, billing, and invoices while preserving PublicId routing, portal theme scope, pagination, and auth.

---

## Files changed

### Documentation
- `docs/ui-ux-refactor/CARE_HOME_UX_AUDIT.md` (new)
- `docs/ui-ux-refactor/CARE_HOME_FORM_FIELD_REQUIREMENTS.md` (new)
- `docs/ui-ux-refactor/CARE_HOME_UX_QA.md` (new)
- `docs/ui-ux-refactor/CARE_HOME_UX_IMPLEMENTATION_REPORT.md` (this file)

### Backend
- `backend/CareHome.Api/Dtos/Dashboard/DashboardDtos.cs` — `RecentInvoiceDto` adds `ClientName`, `PeriodStart`, `PeriodEnd`
- `backend/CareHome.Api/Controllers/DashboardController.cs` — project new fields for tenant and care-home dashboard recent invoices

### Frontend
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-dashboard/care-home-dashboard.html`
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-dashboard/care-home-dashboard.ts`
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-form/care-home-form.html`
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-form/care-home-form.ts`
- `frontend/care-home-web/src/app/features/billing/pages/billing-workspace/billing-workspace.html`
- `frontend/care-home-web/src/app/features/billing/pages/billing-workspace/billing-workspace.ts`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-list/invoice-list.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-list/client-list.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-form/client-form.ts`

---

## UI improvements

| Area | Change |
|------|--------|
| **Dashboard** | KPI hints and semantic tones; merged status into Contact & location; section headers with **View all →**; empty states with CTAs |
| **Residents table** | UUID links; keyboard activation |
| **Recent invoices** | Richer columns when API provides data; payment labeled status; empty state |
| **Form** | Business sections, required legend, clearer labels, conditional submit label |
| **Billing** | Context banner when scoped to a care home |

---

## Routing & navigation

- Resident and invoice rows use `entityRouteKey()`.
- Create care home → `/care-homes` + form reset.
- Edit save / cancel → care home dashboard (PublicId route).
- Billing handoff passes `companyId` + `careHomeId` query params.
- Invoice list honours `careHomeId` filter from dashboard **View all**.
- Residents list breadcrumb when filtered by care home.
- Add resident from dashboard preselects care home via query param.

---

## Breadcrumbs

- Dashboard: Care Homes → {name}
- Edit form: Care Homes → {name} → Edit care home
- Create form: Care Homes → Add care home
- Residents (care-home filter): Care Homes → {name} → Residents

---

## UUID / identifiers

- No numeric IDs in user-facing path segments for care homes, residents, or invoices from dashboard.
- Business **code** still shown in subtitle; UUIDs not displayed in UI.

---

## Accessibility

- Table rows: `tabindex="0"`, Enter/Space handlers alongside `row-clickable` links.
- Required field legend with `aria-hidden` asterisk explanation pattern (matches company form).
- Payment status uses `app-labeled-status` with group aria-label.

---

## Build result

```
npm run build — success (exit 0)
```

Pre-existing bundle budget warnings only; no new compile errors.

---

## Remaining items

All P1/P2 items from the initial pass are addressed in this follow-up (invoice detail UUID links, resident add CTAs, care home list rows, invoice breadcrumbs, company preselect on add care home).

| Priority | Item |
|----------|------|
| P2 | Company link on invoice detail still shows name only (no company PublicId on DTO) |

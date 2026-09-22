# Dashboard redesign — implementation report

## Summary

The tenant **Operations centre** dashboard (`/dashboard`) was restructured around four operational questions: attention, billing preparation, finance snapshot, and recent billing/occupancy work — using only existing metrics plus minimal additive API fields for UUID routing and payment status.

## What changed

- Header: organisation + month context, operational lead line, primary **Start billing** action.
- KPIs grouped under **Operations** and **Finance** with corrected receivables presentation (£ primary, count in hint).
- **Attention needed** always visible with structured rows, mapped setup-hint actions, billing exception headlines, and positive empty state.
- **Billing** panel with honest copy and **Review billing** CTA (no fabricated readiness %).
- **Recent invoices** compact table with Payment column and UUID invoice links.
- **Upcoming billing** includes frequency column; improved empty states.
- **Occupancy** linked care home names and View → via `publicId`.
- Load error **Retry**; header visible during load.

## Files changed

| Path | Change |
|------|--------|
| `frontend/care-home-web/src/app/features/dashboard/dashboard.ts` | DTO types, helpers, load/retry, routing |
| `frontend/care-home-web/src/app/features/dashboard/dashboard.html` | Full template restructure |
| `frontend/care-home-web/src/app/features/dashboard/dashboard.scss` | Dashboard-specific layout |
| `backend/CareHome.Api/Dtos/Dashboard/DashboardDtos.cs` | `PublicId`, `PaymentStatus`, exception DTO |
| `backend/CareHome.Api/Controllers/DashboardController.cs` | Project new fields |
| `docs/ui-ux-refactor/DASHBOARD_REDESIGN_AUDIT.md` | Pre/post audit |
| `docs/ui-ux-refactor/DASHBOARD_REDESIGN_QA.md` | Manual QA |
| `docs/ui-ux-refactor/DASHBOARD_REDESIGN_IMPLEMENTATION_REPORT.md` | This file |

## Components reused

- `app-page-header`, `app-kpi-card`, `app-status-badge`, `app-labeled-status`
- `app-loading-state`, `app-api-error`, `app-empty-state`, `app-section-header`
- `entityRouteKey`, `billingExceptionHeadline`, `billingExceptionLabel`

## New components

None (dashboard-scoped SCSS only).

## API changes

| Change | Necessary because |
|--------|------------------|
| `OccupancyCardDto.PublicId` | Occupancy links must not use numeric care-home routes |
| `RecentInvoiceDto.PublicId`, `PaymentStatus` | Invoice links and payment column |
| `DashboardBillingExceptionDto` (`code`, `message`) | Actionable attention titles from real log data |

No billing, receivables, or auth logic changed.

## Data limitations

- No per-resident billing readiness or progress on dashboard.
- Upcoming billing has no next-run dates (frequency only).
- Recent activity not implemented (`GET /api/audit` is admin-scoped, not a dashboard feed).
- Generated invoice count is organisation-wide non-void, not “this period”.

## UUID routing verification

- Invoice links: `entityRouteKey({ id, publicId })`.
- Care home links: `entityRouteKey({ careHomeId, publicId })`.
- Numeric-only paths removed from dashboard template.

## Theme verification

- No global theme or accent changes; portal appearance remains under care-home settings elsewhere.
- Semantic KPI tones unchanged (info/success/finance/attention).

## Responsive / accessibility

- Existing grid/table responsive classes retained; dashboard grid tuned for xl two-column invoices/upcoming.
- Section headings with `aria-labelledby`; attention dots decorative; links/buttons for actions.
- Invoice rows: link on invoice number only (not whole-row click with mixed semantics).

## Build result

- **Frontend:** `npm run build` in `frontend/care-home-web` — **success** (existing bundle budget warnings only).
- **Backend:** `dotnet build` on `CareHome.Api` — **success** (0 errors; existing NU1902 / obsolete API warnings only).

## Remaining recommendations

| Priority | Item |
|----------|------|
| P0 | None identified for demo if dashboard API deploys with DTO changes |
| P1 | Dashboard activity feed endpoint or audit projection for finance users |
| P1 | Optional `collectionStatus` on recent invoices for parity with invoice list |
| P2 | Care-home dashboard page: same UUID + table polish as tenant dashboard |
| P2 | Upcoming billing: next period dates when product defines source fields |
| P3 | Section-level partial load if dashboard API is split later |

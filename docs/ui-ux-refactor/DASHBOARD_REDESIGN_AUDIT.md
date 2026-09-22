# Dashboard redesign audit — Operations Centre

Date: September 2026  
Scope: Tenant portal main dashboard (`/dashboard`, `DashboardPage`).

## Current dashboard structure

| Area | Implementation |
|------|----------------|
| Route | `/dashboard` → `features/dashboard/dashboard.ts` + `dashboard.html` |
| API | `GET /api/dashboard` → `DashboardController.GetDashboard()` |
| Layout | `app-page-header`, grouped KPI grids, conditional attention block, two-column invoices/upcoming, occupancy table |
| Loading | Single `app-loading-state` while entire payload loads |
| Error | `app-api-error` (full page; no retry button) |
| Styles | Global `styles.scss` (`.dashboard-section`, `.attention-queue`, `.panel`, `.data-table`) |

Platform admins without a tenant context are redirected to `/platform/tenants` (unchanged).

## Data available from `GET /api/dashboard`

| Field | Meaning |
|-------|---------|
| `totalCareHomes` | Active care homes in user scope |
| `currentClients` | Residents with status Current, not archived |
| `availableBeds` | Sum capacity − occupied |
| `upcomingBillingCount` | Count of distinct rows in upcoming billing list (active funding contracts) |
| `outstandingInvoices` | Open invoice count (receivables summary) |
| `outstandingAmount` | Total outstanding £ (receivables summary) |
| `invoicesGenerated` | All non-void invoices in scope (not “current period” only) |
| `occupancyByHome[]` | `careHomeId`, name, capacity, occupied, available |
| `recentInvoices[]` | `id`, number, home, amount, `status` (no `publicId` / `paymentStatus` today) |
| `billingExceptions[]` | Last 8 exception **messages** only (no codes in DTO) |
| `upcomingInvoices[]` | Home, funding authority, `billingFrequency` (no period dates) |
| `setupHints[]` | Human-readable setup strings from `BuildSetupHints` |

### Client context (no extra API)

- `AuthUser.tenantName` — organisation label for header
- `AuthUser` roles — `canWrite()` for billing / add-home CTAs

## Data NOT available (do not fake)

| Desired UI | Gap |
|------------|-----|
| Billing readiness “X of Y residents ready” | No dashboard field; billing preview is workspace-only |
| Upcoming billing calendar dates | `UpcomingInvoiceDto` has frequency only, not next run date |
| Recent activity feed | `GET /api/audit` exists but requires `CanViewAudit` (admin); no dashboard activity projection |
| Per-exception resident counts | Exceptions are log lines, not aggregated counts |
| “Generated invoices this period” | API returns lifetime non-void count in scope |
| Collection status on recent invoices | Not in `RecentInvoiceDto` today |

## Reusable UI components

| Component | Use on dashboard |
|-----------|------------------|
| `app-page-header` | Title, subtitle, actions |
| `app-kpi-card` | Operations / finance metrics |
| `app-status-badge` / `app-labeled-status` | Invoice status / payment |
| `app-loading-state` | Initial load |
| `app-api-error` | Load failure |
| `app-empty-state` | Empty invoices, occupancy, upcoming |
| `app-section-header` | Panel titles with actions |
| `entityRouteKey()` | Invoice / care-home links when `publicId` present |
| `billingExceptionHeadline()` | Exception titles when `code` exposed |

## Routing / UUID gaps (pre-redesign)

- Recent invoices: `routerLink` uses numeric `row.id`
- Occupancy: `careHomeId` in URL
- **Required API extension (additive):** `publicId` on occupancy + recent invoice rows; `paymentStatus` on recent invoices; structured `billingExceptions` with `code` + `message` (from `BillingExceptionLogs`)

No change to billing algorithms or receivables logic.

## Proposed UI sections

1. **Header** — Operations centre; `{tenantName} • {month year}`; “What needs attention today.”; Start billing (if `canWrite()`).
2. **Operations KPIs** — Care homes, current residents, available beds.
3. **Finance KPIs** — Outstanding receivables (£ primary, count hint), generated invoices, upcoming billing.
4. **Attention needed** — Always visible; unpaid invoices, setup hints with links, billing exceptions with Review → `/billing`; positive empty state.
5. **Billing** — Simple “prepare next run” panel with `upcomingBillingCount` when > 0; CTA Review billing (no fake progress %).
6. **Recent invoices** — Compact table + payment column; UUID links; View all.
7. **Upcoming billing** — Home, authority, frequency; empty state with guidance.
8. **Occupancy by home** — Linked home names; View → care-home dashboard via `publicId`.
9. **Recent activity** — **Not implemented** (see gap above).

## Sections without backend changes

- Header copy and grouping
- Attention layout, setup-hint → route mapping, positive empty state
- Billing CTA panel (uses existing counts)
- Empty states, loading header visibility, retry
- KPI copy and navigation targets

## Backend changes required

| Change | Why |
|--------|-----|
| `OccupancyCardDto.PublicId` | Care-home dashboard URLs must use UUID |
| `RecentInvoiceDto.PublicId`, `PaymentStatus` | Invoice detail URLs + payment column |
| `DashboardBillingExceptionDto` (`Code`, `Message`) | Actionable attention titles without inventing counts |

## Theme / navigation

- Care-home portal theme: unchanged (global shell + per-home accent elsewhere).
- Sidebar: out of scope; dashboard nav links only.
- Existing guards and `canWrite()` preserved.

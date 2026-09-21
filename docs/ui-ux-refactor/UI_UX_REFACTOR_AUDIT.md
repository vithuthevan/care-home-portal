# UI/UX refactor — application audit

**Scope:** `frontend/care-home-web` (+ API contracts where routing, identifiers, pagination, or theme persistence depend on backend).  
**Date:** 2026-09-21  
**Prior work:** First/second-pass redesign documented in `UI_UX_SECOND_PASS_AUDIT.md`, `UI_UX_REDESIGN_IMPLEMENTATION_REPORT.md`.

## Executive summary

The app is a single Angular back-office shell (Material sidenav, design tokens in `styles.scss`, shared list primitives: `page-header`, `filter-bar`, `table-shell`, `table-pagination`, `status-badge`, `kpi-card`). Most create flows already use dedicated routes (`/entity/new`). Routes and API lookups still use **numeric `id`** everywhere except **tenant** (`Tenant.PublicId` in JWT). **Business references** already exist for residents (`ReferenceNumber`, `SageId`), care homes (`Code`), invoices/credit notes (`InvoiceNumber` / prefixes from tenant settings). **Companies** have no separate business reference field—only name.

Gaps vs target spec:

| Area | Current | Risk |
|------|---------|------|
| Entity routes | `:id` int | URLs expose internal IDs; bookmarks not stable across DB restore |
| UUID in API | Only `Tenant.PublicId` | Frontend cannot use UUID routes without backend `PublicId` columns |
| Theme | Global toolbar + org settings + `localStorage` | Violates “care home portal only”; not tenant/care-home isolated |
| Pagination | Strong on companies, homes, residents, invoices, users, audit, sage, misc | Setup lists (categories, nominal codes, templates) load full collections |
| Form UX | Partial hints on resident form | Inconsistent required/optional/auto labels across forms |
| Company detail | KPIs + tabs; model only has name/status/counts | Cannot show registration/address without schema change |
| Invoice payment | Dedicated panel exists; hero duplicates payment badge | Minor duplication |

## Routing inventory

| Route pattern | Component | Param type |
|---------------|-----------|------------|
| `/companies`, `/companies/new` | list / form | — |
| `/companies/:id`, `/companies/:id/edit` | detail / form | int |
| `/care-homes`, `/care-homes/new`, `/care-homes/:id/edit` | list / form | int |
| `/care-homes/:id/dashboard` | dashboard | int |
| `/clients`, `/clients/new`, `/clients/:id`, `/clients/:id/edit` | list / profile / form | int |
| `/funding-authorities`, `.../new`, `.../:id/edit` | list / form | int |
| `/invoice-categories`, `/nominal-codes`, `/invoice-templates` | list + dedicated forms | int edit |
| `/billing`, `/invoices`, `/invoices/:id` | workspace / list / detail | int |
| `/credit-notes`, `/misc-charges`, `/reports`, `/sage-exports` | workspace / pages | — |
| `/users`, `/users/new` | list / form | int (API) |
| `/audit`, `/settings/organisation` | list / settings | — |
| `/platform/tenants`, `.../:id` | platform admin | int |

**Target convention (after `PublicId` rollout):** `/entity/:publicId`, `/entity/:publicId/edit`, `/entity/new`; care-home nested paths optional phase 2 (`/care-homes/:publicId/residents`).

## Navigation & shell

- **Layout:** `app.html` + `app.scss` — 248px / 72px collapsible sidenav, section accordions, `overflow-x: hidden` on `html/body` and nav.
- **Issue:** Accent colour picker in **global toolbar** and **user menu** — applies to all users including platform admin.
- **Breadcrumbs:** `BreadcrumbService` + `PageHeaderComponent`; entity names set per page.

## Shared UI components

| Component | Role |
|-----------|------|
| `page-header` | Title, subtitle, actions, breadcrumbs |
| `filter-bar` | List toolbar |
| `table-pagination` | Page size + range display |
| `status-badge`, `labeled-status` | Status semantics |
| `icon-action-button` | Monochrome row actions |
| `kpi-card`, `entity-summary-strip` | Detail/dashboard context |
| `workflow-steps` | Billing workflow |

## Tables & density

- Global `.data-table`, `.table-shell`, `.table-container` in `styles.scss` (width 100%, financial `.num` alignment).
- Most operational lists use `table-shell`; some setup lists use plain panels.

## Pagination (backend)

| API | Paged when `page`/`pageSize` sent |
|-----|-------------------------------------|
| `/api/companies` | Yes |
| `/api/care-homes` | Yes |
| `/api/clients` | Yes (always paged) |
| `/api/funding-authorities` | Yes |
| `/api/invoices` | Yes |
| `/api/users` | Yes |
| `/api/audit` | Yes |
| `/api/credit-notes` | Optional |
| `/api/invoice-categories` | **No** — full list |
| `/api/nominal-codes` | **No** |
| `/api/invoice-templates` | **No** |

## Theme

- `ThemeService` — `localStorage` key `care-home-ui-theme`, `data-app-theme` on `<html>`.
- Organisation settings includes browser-only accent section (not care-home scoped).
- Tenant `PrimaryColour` in organisation settings DTO is for **branding metadata**, not SPA accent tokens.

---

## Implementation plan

### Files to create

| File | Purpose |
|------|---------|
| `docs/ui-ux-refactor/*.md` | Audits + QA |
| `frontend/.../care-home-portal-settings/*` | Care home portal appearance |
| `frontend/.../shared/routing/entity-route.ts` | `publicId` vs legacy id in links |
| `backend/.../Common/EntityRouteKey.cs` | Parse GUID or int in API routes |
| EF migration | `PublicId` on core entities; `PortalAccentTheme` on care homes |

### Files to modify (batch order)

**Batch 1 — Shell:** `app.html`, `app.ts`, `organisation-settings.html`, `app.scss` (theme removal from global/org).  
**Batch 2 — Theme:** `theme.service.ts`, care home settings page, `CareHomeLocation.PortalAccentTheme`, dashboard link.  
**Batch 3 — Identifiers:** Models, DTOs, controllers (dual key), Angular models/services/templates `routerLink`.  
**Batch 4 — Forms:** `company-form`, `client-form`, shared field hints, create → list redirects.  
**Batch 5 — Invoice:** `invoice-detail.html` payment area only.  
**Batch 6 — Pagination:** Document backend gaps; optional client-side slice only where lists stay small.

### Backend dependencies

- Add non-destructive `PublicId` `uniqueidentifier` NOT NULL with default `NEWID()` for existing rows.
- API routes accept **either** int id or GUID `PublicId` during transition.
- `PortalAccentTheme` persisted per care home (max 20 chars, values: green|blue|teal|purple|slate).

### Migration risks

| Risk | Mitigation |
|------|------------|
| Breaking bookmarked int URLs | Keep int resolution in API and optional redirect |
| Cross-tenant GUID guess | Always filter by `TenantId` |
| Theme in localStorage only | Replace with care home field; clear global keys |
| Company business reference | Do not invent `COMP-001` without product sign-off; use name + future column |

### Components to reuse

`page-header`, `table-pagination`, `loading-state`, `empty-state`, `api-error`, `toast`, `confirm-dialog`.

---

## Ambiguities / product decisions needed

1. **Company business reference** — schema has no code; only name is unique per tenant.
2. **“Care home portal”** — implemented as care home dashboard + settings (no separate SPA).
3. **Nested routes** (`/care-homes/:id/residents`) — defer until list scoping by home is default UX.
4. **Platform admin theme** — fixed slate/green; no picker.

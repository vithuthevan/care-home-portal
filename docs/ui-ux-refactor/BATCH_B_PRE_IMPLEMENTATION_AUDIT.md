# Batch B — Pre-implementation audit

**Date:** 2026-09-21  
**Scope:** Finance workflow continuity, UUID secondary navigation, billing/credit-note context, reports, pagination, forms, wayfinding.  
**Prerequisite:** Batch A (P0) completed per `BATCH_A_IMPLEMENTATION_REPORT.md`.

This document records **current state** after code and doc inspection. **No Batch B code changes** were made in this phase.

---

## Summary

| Theme | Already in good shape | Main gaps for Batch B |
|-------|----------------------|------------------------|
| UUID primary lists | Companies, care homes, residents, invoices use `entityRouteKey()` | Secondary links still use numeric ids; dashboard DTOs lack `PublicId` |
| Billing context | Query-param handoff exists (int ids); profile banner + period suggestion | Missing `companyId` from profile; query params are ints not UUIDs; scope summary could be clearer |
| Credit notes | Resident autocomplete; invoice→CN query context (client, period, invoice number) | `invoiceId` query unused; no invoice total in banner; generate does not reset/redirect; list invoice links numeric |
| Reports | Column metadata, loading/disable Run, `report` query param | No row count; dashboard has no “outstanding report” deep link (`report=outstanding`) |
| Pagination | Most lists server-paged | Care home **search** loads full API list then client-slices |
| Breadcrumbs | Per-page `BreadcrumbService.set()` on key entities | `setFromUrl()` regexes only match **numeric** `:id` paths |
| Forms | Dedicated `/new` routes; company/client create→list | Care home create→dashboard; edit company/client→list; required `*` inconsistent |
| Payment | Single `payment-status-panel` on invoice detail | No `PaidAt` in API (document only) |
| Theme | No global toolbar theme picker in `app.html` | Portal theme still applies via `documentElement` (P2) |

---

## Audit matrix

| Area | Current state | Existing support | Gap | Proposed change | Risk |
|------|---------------|------------------|-----|-----------------|------|
| **UUID — primary lists** | List rows link with `entityRouteKey()` for company, care home, client, invoice | `shared/routing/entity-route.ts`; DTOs expose `publicId` on core four | — | None on primary lists | Low |
| **UUID — Batch A fixes** | Profile/edit care home load by string key; create care home redirects with `entityRouteKey` | Dual-key API GET/PUT/DELETE | — | Leave unchanged | — |
| **UUID — company detail** | Care home table uses `[routerLink]="['/care-homes', home.id, 'dashboard']"` | `CareHomeDto.publicId` on paged company homes | Numeric URLs when `publicId` exists | Use `entityRouteKey(home)` in `company-detail.html` | Low |
| **UUID — client profile** | Company/care home links use `client.companyId`, `client.careHomeId`; invoice table uses `invoice.id` | Client DTO has `publicId`; invoices from list API may include `publicId` | Secondary navigation exposes ints | Links via `entityRouteKey` where DTO loaded; invoice rows use list DTO `publicId` if present | Low |
| **UUID — invoice detail** | Resident/care home links use `clientId`, `careHomeId` on lines/header | Invoice detail loaded by UUID/int key | Cross-entity links numeric | Add optional `clientPublicId` / `careHomePublicId` on line DTO **or** resolve from loaded client/home services only when navigating from same page (prefer minimal DTO fields if already cheap to project) | Low–Med (API surface) |
| **UUID — dashboard** | Recent invoices `row.id`; occupancy `row.careHomeId` | `RecentInvoiceDto`, `OccupancyCardDto` have int ids only | No `publicId` on dashboard payloads | Extend dashboard DTOs with `PublicId` for invoice + care home; frontend `entityRouteKey` | Med (small API + migration-free) |
| **UUID — care home dashboard** | Residents loaded via `ClientService` (have `publicId`) but template links `resident.id` | Separate residents fetch | Wrong link key despite data | `entityRouteKey(resident)` in `care-home-dashboard.html` | Low |
| **UUID — credit note list** | Invoice link `note.invoiceId` (int) | Credit note list DTO | Numeric invoice URLs | Map invoice `publicId` in list DTO or fetch N+1 — prefer DTO field on join | Med |
| **UUID — query params** | Billing/credit-note use `companyId`, `careHomeId`, `clientId` as **numbers** | Billing API body requires int FKs | Spec prefers UUID in URLs for handoff | Accept int query params for API compatibility **or** resolve UUID→id on init (no API change) | Low |
| **UUID — breadcrumbs** | `BreadcrumbService.setFromUrl` matches `/clients/\d+`, `/invoices/\d+`, etc. | Custom crumbs on profile, invoice detail, care home dashboard | UUID paths fall through to generic crumbs | Extend regex to UUID pattern or rely on page-level `set()` only | Low |
| **UUID — canonical redirect** | API dual-key GET works; no Angular guard redirect int→uuid | — | Bookmarks may stay numeric | **Document only** in `BATCH_B_UUID_ROUTE_REPORT.md`; no 301 guard unless trivial | Low |
| **Billing — context handoff** | `billing-workspace.ts` reads `careHomeId`, `companyId`, `clientId`, `periodStart`, `periodEnd`; preselects company from home; shows profile banner | `client-profile` `billingQueryParams()` passes home, client, period, name | **Missing `companyId`** from profile; no explicit Company/Care Home summary block like spec example | Add `companyId: client.companyId` to profile link; optional read-only scope strip (company name, home name, period) | Low |
| **Billing — behaviour** | Preview/generate unchanged; requires `companyId` for preview | Existing billing API | — | Do not auto-generate; keep explicit Preview | — |
| **Credit note — invoice flow** | Invoice detail passes `creditNoteQueryParams`: `invoiceId`, `invoiceNumber`, `clientId`, names, period | Workspace shows banner; prefills resident via autocomplete | `invoiceId` **not read** in `applyQueryContext`; API `CreditNotePreviewRequest` has **no** `InvoiceId` — matching is client+period | Keep client+period model; show invoice number/total in UI from invoice detail query; document that backend does not take `invoiceId` | Low |
| **Credit note — selection UX** | Mat-autocomplete resident search (200 residents); hint says optional | No raw “Client ID” field | Resident marked optional though invoice flow implies required; reason not marked required visually | Required labels per `CreditNotePreviewRequest` (`PeriodStart/End`, `Reason`); lock resident when context from invoice | Low |
| **Credit note — post-generate** | Toast + `loadNotes()`; form state retained | — | Stale preview/reason; no redirect to list | Clear preview/reason; optional navigate `/credit-notes` with list focus; disable double submit (already `isWorking`) | Low |
| **Reports — columns/format** | `reportColumns` + `SHARED_COLUMN_LABELS`; currency/date/status formatters in template | Loading state; Run disabled while loading | No “Showing N results”; export not guarded beyond `isLoading` on Run | Add row count; ensure export buttons respect loading (partially done) | Low |
| **Reports — dashboard link** | Outstanding KPI links `/invoices?paymentStatus=NotPaid` | Reports support `?report=outstanding` in `reports.ts` ngOnInit | No dashboard link to **Reports** outstanding type | Add link e.g. `/reports?report=outstanding` on finance KPI or attention queue (align param name `report` not `type`) | Low |
| **Pagination — operational** | Companies, clients, invoices, users, audit, funding authorities, credit note history use `table-pagination` + API | Documented in `PAGINATION_COVERAGE.md` | — | Verify wiring only | Low |
| **Pagination — care homes** | Paged when no search | `getCareHomesPaged` | **Search** calls unpaged `getCareHomes()` then filters/slices in browser | Add API `search` query + page on `CareHomesController` **or** document defer if out of scope | Med |
| **Pagination — reports** | Full result set in memory | Acceptable for demo | Production scale | Document in report; no fake pagination | Low |
| **Care home → resident** | Dashboard loads `Client[]` with `publicId` | — | Template still uses `resident.id` | Fix template only | Low |
| **Breadcrumbs** | Entity pages set name (profile, company, care home, invoice) | `page-header` renders `BreadcrumbService` | Static URL crumbs break for UUID; no parent link to company from resident | UUID-aware `setFromUrl`; add company parent on profile when data loaded | Low |
| **Form requirements** | Documented in `FORM_FIELD_REQUIREMENTS_AUDIT.md` | Partial hints on resident form | Company submit not tied to `form.invalid`; credit note period/reason labels | Apply audit labels/hints across listed forms without inventing rules | Low |
| **Create → list redirects** | Company create→`/companies`; client create→`/clients` | — | Care home **create→dashboard** (not list); company **edit→list** (spec says stay on edit for edit) | Care home create→`/care-homes` per Batch B spec; keep edit on page with toast | Low |
| **Form page structure** | All major creates on `/new` per `FORM_PAGE_RESTRUCTURE_AUDIT.md` | — | None required | No new routes | — |
| **Invoice payment** | One panel; confirm via `confirmPay`; `isPaying` disables buttons | Batch A pattern | No paid date field | Document `PaidAt` as future enhancement | — |
| **Terminology** | UI mixes “Residents” nav / “Clients” API | `BUSINESS_REFERENCE_STRATEGY.md` | Inconsistent in docs vs code | `BATCH_B_TERMINOLOGY_REPORT.md` — prefer “Resident” in UI, “Client” internal | Low |
| **Icons / actions** | Invoice detail uses icon buttons with `aria-label` + tooltip | `icon-action-button` on lists | Not all tables audited for consistency | B6 pass: compact icons where repeated, keep destructive actions clear | Low |
| **Tables** | Global `.data-table`, `.num` alignment | Responsive hide on some lists | Dashboard occupancy not clickable on name | Minor column width pass in B6 | Low |
| **Sidebar** | `overflow-x: hidden`; collapsible sections; no theme in toolbar | `app.scss` | User-reported gap/scroll issues need visual QA | CSS tuning only if reproduced | Low |
| **Theme** | `CareHomePortalSettingsPage` only; `ThemeService` on portal | `portalAccentTheme` on care home | Global `data-app-theme` per tab (P2) | Verify admin default green; no global picker — **no new architecture** | Low |
| **Quality pass** | Shared pipes/components used on reports, billing, lists | `app-loading-state`, `app-api-error`, `app-empty-state` | Spot checks only in B6 | Reuse existing primitives | Low |
| **Business logic** | Billing/credit calculations in backend services | — | — | **No changes** unless defect found | — |

---

## Numeric route / link inventory (secondary)

| Location | Pattern | Fix approach |
|----------|---------|--------------|
| `company-detail.html` | `home.id` in care home links | `entityRouteKey(home)` |
| `client-profile.html` | `companyId`, `careHomeId`, `invoice.id` | UUID keys from client + invoice `publicId` |
| `invoice-detail.html` | `clientId`, `careHomeId` on links | DTO public ids or int fallback via `entityRouteKey({id, publicId})` |
| `dashboard.html` | `row.id`, `row.careHomeId` | API add `PublicId` to DTOs |
| `care-home-dashboard.html` | `resident.id`, `row.id` (invoices) | `entityRouteKey` / dashboard DTO |
| `credit-note-workspace.html` | `note.invoiceId` | Invoice `publicId` on credit note list DTO |
| Query params | `companyId`, `careHomeId`, `clientId` ints | Resolve UUID query strings to ints on load (optional UX upgrade) |

Primary list pages **already** use UUID when `publicId` is present.

---

## Credit note API contract (verified)

`CreditNotePreviewRequest` (`backend/CareHome.Api/Dtos/CreditNotes/CreditNoteDtos.cs`):

- `ClientId` (optional int), `FundingAuthorityId`, `InvoiceCategoryId`, `PeriodStart`, `PeriodEnd`, `CreditNoteDate`, `Reason`, `LineAmounts`
- **No `InvoiceId`** — credit matching is by resident + period (+ optional filters), not direct invoice FK in preview body.

**Implication:** Invoice → Credit Note workflow should prefill **client + period + reason context** and display invoice number/total from navigation state; do not add `InvoiceId` to API without separate approved change.

---

## Billing API contract (verified)

`billing-workspace` POST body uses int `companyId`, `careHomeId`, `clientIds[]` — query-param handoff must resolve to these ints internally whether URLs use UUID or int.

---

## Dashboard API gaps (verified)

`RecentInvoiceDto` / `OccupancyCardDto` (`DashboardDtos.cs`): int `Id` / `CareHomeId` only — frontend cannot build UUID invoice/home links without extension.

`CareHomeDashboardDto.CurrentClients`: `List<string>` names only — **not used** for links; frontend already loads paged `Client` entities for the residents table.

---

## Reports (verified)

- Query param: **`report`** (e.g. `outstanding`), not `type`.
- Column humanization and loading states **already implemented** in `reports.ts` / `reports.html`.
- Missing: result count; dashboard deep link.

---

## Pagination (verified)

- Care home list: `care-home-list.ts` lines 109–127 unpaged fetch when `searchText` non-empty.
- `CareHomesController.GetCareHomes`: no `search` parameter — **NEEDS API** for true server-side search pagination.

---

## Payment UX (verified)

`invoice-detail.html`: single `payment-status-panel`; `confirmPay` + `isPaying` in `invoice-detail.ts`. Sub-routes `/api/invoices/{id:int}/payment-status` — detail uses loaded int `id` after UUID route load (acceptable).

---

## Theme (verified)

- `app.html`: no accent theme picker in global toolbar.
- Portal theme: `care-home-portal-settings` + `CareHomePortalThemeService`.
- Organisation settings: verify accent section removed per audit (spot-check during B6).

---

## Form redirects (verified)

| Flow | Current | Batch B target |
|------|---------|----------------|
| Create company | → `/companies` | OK |
| Create resident | → `/clients` | OK |
| Create care home | → `/care-homes/{uuid}/dashboard` | → `/care-homes` |
| Update company | → `/companies` | Consider stay on detail (product: spec says edit stays) |
| Update resident | → `/clients` | Consider stay on profile |

---

## Deferred / out of Batch B scope

- Phase B `PublicId` on funding authority, category, nominal, template, credit note entities.
- Invoice PDF/send/payment/void API routes `{id:int}` only.
- Funding contracts `/api/clients/{clientId:int}/funding-contracts`.
- Canonical router redirect numeric URL → UUID URL.
- `PaidAt` on invoices.
- Multi-tab portal theme isolation (P2).

---

## Recommended sub-batch implementation order

Aligned with user spec **Phase 21**:

### B1 — UUID route cleanup, care home → resident, breadcrumbs

- Fix secondary `routerLink` / navigate targets (company detail, profile, invoice detail, dashboards, credit note list) using `entityRouteKey` or extended dashboard DTOs.
- Care home dashboard resident links.
- UUID-aware breadcrumb `setFromUrl` or ensure all detail pages call `set()` with entity names.
- Produce `BATCH_B_UUID_ROUTE_REPORT.md`.

### B2 — Billing + reports context

- Add `companyId` to profile → billing query params; optional scope summary UI.
- Optional: accept UUID query params and resolve to ints.
- Dashboard → `/reports?report=outstanding`.

### B3 — Credit note workflow polish

- Invoice banner: total amount from invoice detail params if available.
- Required field labels; resident required when from invoice.
- Post-generate form reset; consider list focus.
- Document no `InvoiceId` in API.

### B4 — Reports + pagination

- Row count on reports.
- Care home search pagination (API `search` + FE) if approved; else document.

### B5 — Forms

- Required/optional labels per `FORM_FIELD_REQUIREMENTS_AUDIT.md`.
- Care home create redirect to list.
- Review edit redirects vs spec.

### B6 — Polish

- Tables, icons, sidebar, terminology report, consistency pass.

### B22–B24 — QA, build, final report

- `BATCH_B_QA.md`, `npm run build`, `dotnet build`, `BATCH_B_IMPLEMENTATION_REPORT.md`.

---

## STOP point

**Phase 0 complete.** Proceed to implementation only after review of this audit. First code work: **Batch B1** as above.

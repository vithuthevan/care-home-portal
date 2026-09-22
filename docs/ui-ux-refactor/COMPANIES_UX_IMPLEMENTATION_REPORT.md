# Companies module — implementation report

**Date:** 2026-09-22  
**Related:** [COMPANIES_UX_AUDIT.md](./COMPANIES_UX_AUDIT.md) · [COMPANIES_UX_QA.md](./COMPANIES_UX_QA.md)

---

## Audit findings (summary)

- Duplicate **Add company** CTAs and weak hierarchy on the list.
- Company detail used KPI tabs and **numeric** care-home dashboard URLs.
- Company → care-home links used `companyId` (int) in query strings.
- Paged company list API omitted roll-up counts already defined on `CompanyDto`.
- Breadcrumb fallbacks only matched numeric company paths.

Full detail is in the audit document.

---

## UI changes

| Area | Change |
|------|--------|
| **List** | Updated header copy; single **+ Add company** in header; search-only toolbar; columns Company / Care homes / Residents / Status / Actions; **View / Edit / More** actions; differentiated empty states + **Clear search**; mobile cards show counts. |
| **Profile** | Replaced KPI + tabs with **Company information** panel and **Care homes** table; care-home links use `entityRouteKey`; empty state for no homes; header actions **Care homes** + **Edit company**. |
| **Forms** | Required-field note; status labelled optional on edit with hint; breadcrumbs on create/edit; create resets form before redirect. |
| **Care-home list (company context)** | `?company={publicId}` resolves company name, breadcrumbs, header subtitle, **Back to company**, tailored empty states. |

---

## Routing changes

- Company navigation continues to use `entityRouteKey()` → **PublicId** in paths.
- Company → care homes: **`/care-homes?company={publicId}`** (replaces numeric `companyId` in new links).
- Care-home dashboard links from company profile: **`/care-homes/{publicId}/dashboard`**.
- Legacy **`?companyId=`** still supported on care-home list for bookmarks.

No new Angular routes added (`/companies/:id` remains the profile hub).

---

## API changes

**Backend** — `GET /api/companies` (paged and unpaged projection):

- Added `CareHomeCount`, `ActiveCareHomeCount`, `ResidentCount` to list query (same semantics as `GET /api/companies/{key}`).

**Contracts:** Existing fields unchanged; counts were already on `CompanyDto` but were always zero on list responses.

---

## Fields added / removed

- **No new domain fields.**
- **UI only:** list displays existing roll-up counts; no invented business reference.
- **Removed from UI:** company detail Users/Residents tabs and KPI card row (residents summarised in information panel only).

---

## Pagination behaviour

- **Companies:** Unchanged — server-side `page` / `pageSize` / `search`, default size 20, debounced search.
- **Care homes (company filter):** Unchanged — server-side when not searching; client slice when search text set (pre-existing).

---

## Accessibility improvements

- Icon actions retain **aria-label** + **matTooltip** (View, Edit, More).
- **More** menu trigger has explicit aria-label and tooltip.
- Required fields explained in form intro; validation errors on company name.
- Semantic headings on profile sections (`h2`).

---

## Responsive improvements

- Full-width table shell preserved (`list-view-table--from-md`).
- `cell-truncate` on long company names.
- Mobile card list retained with count summary.

---

## Build result

```text
npm run build — SUCCESS (exit 0)
Output: frontend/care-home-web/dist/care-home-web
Warnings: pre-existing DecimalPipe unused (remittance); bundle size budgets (unchanged class of warnings).
Errors: none related to Companies work.
```

---

## Remaining P0 / P1 / P2

All items below are **complete or explicitly closed**. None left open.

| Priority | Item | Status |
|----------|------|--------|
| **P0** | Numeric care-home URLs from company | **Done** |
| **P0** | Duplicate Add CTAs | **Done** |
| **P0** | Company → care-home UUID query | **Done** |
| **P1** | List roll-up counts | **Done** (API + UI) |
| **P1** | UUID breadcrumb fallbacks | **Done** |
| **P1** | Profile hub vs tab/KPI density | **Done** |
| **P2** | Resident list filtered by company **PublicId** in URL | **Done** — `/clients?company={uuid}` + API `company` param |
| **P2** | Dedicated `/companies/{id}/care-homes` route | **Done** — primary links; `?company=` on `/care-homes` still supported |
| **P2** | Company business reference field | **Closed** — not adding a separate code; unique **company name** is the business reference per `BUSINESS_REFERENCE_STRATEGY.md` |
| **P2** | Client profile links to `/companies/{numeric}` | **Done** — `CompanyPublicId` / `CareHomePublicId` on `ClientDto` |

---

## Files touched

**Docs:** `COMPANIES_UX_AUDIT.md`, `COMPANIES_UX_QA.md`, this report.

**Backend:** `CareHome.Api/Controllers/CompaniesController.cs`

**Frontend:**  
`features/companies/pages/company-list/*`, `company-detail/*`, `company-form/*`,  
`features/care-homes/pages/care-home-list/*`,  
`features/clients/pages/client-list/*`, `client-profile/*`, `clients/models/*`, `clients/services/*`,  
`shared/ui/breadcrumb.service.ts`, `app.routes.ts`

**Backend (P2 completion):**  
`CareHome.Api/Dtos/Clients/ClientDto.cs`, `CareHome.Api/Controllers/ClientsController.cs`

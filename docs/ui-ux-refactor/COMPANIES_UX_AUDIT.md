# Companies module — UX audit

**Date:** 2026-09-22  
**Scope:** Companies list, detail/profile, create/edit, company → care-home navigation.

---

## 1. Current Companies UX

| Area | Current behavior |
|------|------------------|
| **List** | Page header with subtitle “Manage care home operating companies.” Duplicate **Add company** in header and filter toolbar. Table: Company, Status, Actions. Mobile card list. Server-side search + pagination. |
| **Actions** | `home_work` → `/care-homes?companyId={numeric id}`; `edit`; inline `block` deactivate. Company name links to detail. No dedicated **View** icon; care-home icon is ambiguous. |
| **Detail** | Exists at `/companies/:id` (param name `id`, value is PublicId via `entityRouteKey`). KPI cards + Material tabs (Details, Care homes, Residents, Users). Care-home rows link with **numeric** `home.id` in URL. “View all” uses numeric `companyId` query. |
| **Create** | `/companies/new`, name only. Success toast + redirect to `/companies`. |
| **Edit** | `/companies/:id/edit`, name + Active checkbox. Success toast + redirect to `/companies`. |
| **Care homes (from company)** | Global `/care-homes?companyId={int}` with muted filter hint and “Show all”. Empty state not tailored to company context; no “Back to company”. |

---

## 2. Current available company fields

**Database (`Company`):** `Id`, `PublicId`, `TenantId`, `Name` (max 150, unique per tenant), `IsActive`.

**No** dedicated business reference/code (see `BUSINESS_REFERENCE_STRATEGY.md` — name is the business label).

**Frontend model:** `id`, `publicId?`, `name`, `isActive`, optional `careHomeCount`, `activeCareHomeCount`, `residentCount`.

**Forms:** Create — `name` only. Edit — `name`, `isActive`.

---

## 3. Current API capabilities

| Endpoint | Behaviour |
|----------|-----------|
| `GET /api/companies` | Optional `search`, `page`, `pageSize`. **List projection:** `Id`, `PublicId`, `Name`, `IsActive` only (counts default to 0 on DTO). Unpaged mode returns full list. |
| `GET /api/companies/{key}` | `EntityRouteKey` (PublicId or legacy int). Returns counts: `CareHomeCount`, `ActiveCareHomeCount`, `ResidentCount`. |
| `POST /api/companies` | `{ name }` → active by default. |
| `PUT /api/companies/{key}` | `{ name, isActive }` with duplicate-name and deactivate guards. |
| `DELETE /api/companies/{key}` | Soft deactivate (`IsActive = false`). |

**Care homes:** `GET /api/care-homes?companyId={int}` filters by internal company id (unchanged).

---

## 4. Current pagination

- **Companies list:** Server-side via `getCompaniesPaged(page, pageSize, search)` — default page 1, size 20. Debounced search (300ms) resets to page 1.
- **Care homes (company filter):** Server-side when no search text; client-side fetch + slice when search text is set (pre-existing care-home list behaviour).

---

## 5. Current routing

| Route | Component | Key |
|-------|-----------|-----|
| `/companies` | `CompanyList` | — |
| `/companies/new` | `CompanyForm` | — |
| `/companies/:id` | `CompanyDetail` | PublicId preferred in links via `entityRouteKey()` |
| `/companies/:id/edit` | `CompanyForm` | Same |

**Gaps:** Breadcrumb service still matches **numeric** `/companies/\d+` only. Company → care homes uses **`companyId` int** in query string. Company detail care-home dashboard links used numeric ids.

---

## 6. Existing business information

- Company **name** is the human-facing identifier (unique per tenant).
- **Status** Active/Inactive drives assignment and deactivation rules.
- Roll-up **counts** exist on single-company GET only.
- Hierarchy intent: Organisation → **Company** → Care homes → Residents → billing (documented in detail copy, not fully reflected in list/navigation).

---

## 7. Missing information (backend / product)

| Item | Notes |
|------|--------|
| **List roll-up counts** | Not returned on paged list today; fields exist on `CompanyDto` but are zero in list responses. **Fix:** extend list projection (non-breaking DTO fields). |
| **Company business reference** | Not in schema; do not invent `COMP-xxx`. |
| **Company-scoped care-home route** | No dedicated `/companies/{publicId}/care-homes` API; use care-homes list + company filter. |
| **Resident list by company PublicId** | Clients API/list uses `companyId` int; UUID query for residents is out of immediate scope. |

---

## 8. Recommended implementation

1. **Audit doc** (this file) before code changes.
2. **Backend:** Add count projection to paged/unpaged company list (same expressions as `GetCompany`).
3. **List UI:** Single **+ Add company** CTA; updated subtitle; table columns Company / Care homes / Residents / Status / Actions when counts available; **View / Edit / More** actions; remove duplicate add button; differentiated empty states + clear search.
4. **Detail UI:** Profile hub (information + care homes on one page); `entityRouteKey` for care-home links; care-home list at `/companies/{publicId}/care-homes` (legacy `?company=` supported); improved empty state + back link.
5. **Care-home list (navigation only):** Resolve `company` query param (PublicId), show company name in header/breadcrumbs, company-specific empty copy, **Back to company**.
6. **Forms:** Required/optional labelling; edit breadcrumbs; create success clears form (redirect to list).
7. **Breadcrumbs:** UUID-aware patterns for company edit/detail; care-home list under company.
8. **Docs:** QA checklist + implementation report; `npm run build`.

---

## 9. P0 / P1 / P2

| Priority | Item |
|----------|------|
| **P0** | Numeric care-home URLs from company detail; numeric `companyId` in company → care-home links; duplicate Add CTAs; ambiguous list actions. |
| **P0** | List empty vs search-empty not actionable (no clear search). |
| **P1** | List columns without counts until API extended. |
| **P1** | Breadcrumb service numeric-only company patterns. |
| **P1** | Company detail KPI/tab density vs profile hub spec. |
| **P2** | Resident list filtered by company PublicId in URL. |
| **P2** | Dedicated `/companies/{id}/care-homes` child route (query param sufficient). |
| **P2** | Company business reference field | **Closed** — use company **name** only; no `CompanyCode` in schema |

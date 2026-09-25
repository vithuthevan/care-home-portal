# Configuration module — pre-implementation audit

Date: 2026-09-22  
Scope: Funding Authorities, Nominal Codes, Invoice Categories, Invoice Templates (Back Office)

## Reference UI patterns (source of truth)

| Pattern | Reference |
|--------|-----------|
| Page header + primary action | `company-list.html`, `client-list.html` |
| Search toolbar (server-side only) | `company-list.html` (`CompanyService` paged + search) |
| Table hierarchy (name primary) | `company-list.html` (company name column) |
| Icon actions + tooltips | `IconActionButtonComponent`, company/client lists |
| Empty / loading / error | `EmptyStateComponent`, `LoadingStateComponent`, dashboard retry row |
| Form layout | `company-form.html` (required legend, `form-actions`) |
| Sections | `styles.scss` (`.form-section`, `.form-section__title`) |
| Pagination | `TablePaginationComponent` + API `PagedResult` |
| Breadcrumbs | `breadcrumb.service.ts` via `PageHeaderComponent` |
| UUID routes | `entityRouteKey()` — **not applicable** to these four entities (numeric `id` only in DTOs/API) |

## Routes (Angular)

| Module | List | Create | Edit |
|--------|------|--------|------|
| Funding Authorities | `/funding-authorities` | `/funding-authorities/new` | `/funding-authorities/:id/edit` |
| Nominal Codes | `/nominal-codes` | `/nominal-codes/new` | `/nominal-codes/:id/edit` |
| Invoice Categories | `/invoice-categories` | `/invoice-categories/new` | `/invoice-categories/:id/edit` |
| Invoice Templates | `/invoice-templates` | `/invoice-templates/new` | *(no edit route before refactor; API supports PUT)* |

All guarded with `authGuard`. Sidebar: nested links under Configuration in `app.html`.

## API contracts

### Funding Authorities (`FundingAuthoritiesController`)

- **GET** `/api/funding-authorities` — optional `activeOnly`, optional **pagination** (`page`, `pageSize` → `PagedResult`).
- **No search** parameter.
- **POST/PUT** — `CreateFundingAuthorityRequest` / `UpdateFundingAuthorityRequest`.
- **DELETE** `{id}` — deactivates (`IsActive = false`).

### Nominal Codes (`NominalCodesController`)

- **GET** `/api/nominal-codes` — list (optional `activeOnly`). **No pagination, no search**.
- **DELETE** — deactivate.

### Invoice Categories (`InvoiceCategoriesController`)

- Same shape as nominal codes: **full list**, no pagination/search.

### Invoice Templates (`InvoiceTemplatesController`)

- **GET** `/api/invoice-templates` — **full list**, ordered by name.
- **GET/PUT/DELETE** `{id}` — get, update, deactivate.
- **No pagination, no search**.

## Required vs optional fields (backend + frontend alignment)

### Funding authority (create)

| Field | Required |
|-------|----------|
| Code | Yes (`[Required]`, max 30) |
| Name | Yes |
| Type | Yes (NHS, Council, Private, Other) |
| Contact name, phone, email, address | Optional |
| Billing frequency | Yes |
| Billing interval days | Required when frequency = `CustomDays` (backend `ValidateBilling`) |
| isActive | Edit only (update DTO) |

### Nominal code / invoice category (create)

| Field | Required |
|-------|----------|
| Code | Yes |
| Name | Yes |
| Description | Optional (max 500) |
| isActive | Edit only |

### Invoice template (upsert)

| Field | Required |
|-------|----------|
| Name | Yes (non-empty trim on server) |
| Invoice category id | Yes (must exist for tenant) |
| Funding authority, care home, company ids | Optional scoping |
| Header/footer/bank/contact/email template fields | Optional |
| isActive | Edit (default true on create) |

## Current implementation summary

### Funding Authorities

- **List**: Page header, duplicate “Add” in filter bar, paged API, pagination component, icon edit/deactivate, mobile cards, empty state.
- **Table**: Separate Code and Name columns (weak hierarchy).
- **Create form**: Stays on form after success (toast + reset) — **should navigate to list** per product spec.
- **Edit**: Navigates to list on save (OK).
- **Deactivate**: Confirm dialog present.

### Nominal Codes / Invoice Categories

- **List**: Duplicate Add in header + filter bar; no pagination (correct — API limitation); no loading-only-on-first paint distinction; empty state after table block (order inverted vs companies).
- **Forms**: Navigate to list on create (OK); generic “Save” label; no section headings; no required-field legend.

### Invoice Templates

- **List**: No loading state, no empty state, **no row actions** (edit/deactivate API exists).
- **Form**: Create only; cancel duplicated in header; navigates to list on success (OK); subset of DTO fields exposed (intentional minimal UI — full upsert has more optional fields).

## Reusable components already available

- `PageHeaderComponent`, `EmptyStateComponent`, `LoadingStateComponent`, `ApiErrorComponent`
- `StatusBadgeComponent`, `IconActionButtonComponent`, `TablePaginationComponent`
- `FilterBarComponent` (search when API supports it — **not used** for these modules)
- `ConfirmDialogService`, `ToastService`
- Global styles: `.table-shell`, `.data-table`, `.form-actions`, `.form-section__title`, `.mobile-card-list`

## UI inconsistencies (four modules)

1. Duplicate primary “Add” buttons (header + filter bar).
2. Filter bars used only for static helper text, not search.
3. Inconsistent button labels (`Add Authority` vs `Add nominal code`).
4. Inconsistent empty-state ordering (nominal/category show table block before empty).
5. Table primary field hierarchy (code/name equal weight).
6. Form actions: mixed `mt-4 flex` vs `form-actions`; save labels not entity-specific.
7. Funding authority create does not return to list.
8. Invoice templates missing loading, empty, error retry, actions, edit route.
9. Breadcrumbs lack new/edit crumbs for funding/nominal/categories (templates partial).
10. Pagination only on funding authorities (correct); others must not fake pagination.
11. Confirm dialog titles/messages vary slightly.

## Proposed changes (UI/UX only)

1. **Shared list pattern**: Header + single Add; remove filter bar when no search; error row with Retry; initial full-page loading; empty state when zero rows; table with `table-cell-stack` hierarchy; icon actions; mobile cards where applicable.
2. **Shared form pattern**: Required legend; section headings where logical; `form-actions` with Cancel + “Save {entity}”; navigate to list after successful **create** (fix funding authority).
3. **Pagination**: Keep server pagination **only** on funding authorities; document full-list APIs for other three.
4. **Invoice templates**: Add loading/empty/error retry; edit + deactivate actions; edit route wired to existing PUT/DELETE API; align list/form with other modules.
5. **Breadcrumbs**: Add list / new / edit crumbs for all four modules.
6. **Styles**: Add `.table-cell-stack`, `.list-error-row` tokens.
7. **UUID**: No change — config entities use numeric ids in API and routes (no `publicId` on DTOs).

## Must NOT change

- Database schema, migrations, billing/funding/invoice calculation logic
- Authentication, authorization, tenancy
- API endpoints or request/validation rules (except consuming existing template update/delete from UI)
- Global care-home portal accent/theme picker
- Numeric id routing for configuration entities (no fabricated UUIDs)

## API limitations to document in QA

| Module | Pagination | Search |
|--------|------------|--------|
| Funding Authorities | Server-side | Not supported |
| Nominal Codes | Full list only | Not supported |
| Invoice Categories | Full list only | Not supported |
| Invoice Templates | Full list only | Not supported |

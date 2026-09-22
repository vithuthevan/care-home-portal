# Configuration module — implementation report

Date: 2026-09-22

## 1. What was inspected

- Angular routes (`app.routes.ts`), sidebar (`app.html`)
- All list/form components for funding authorities, nominal codes, invoice categories, invoice templates
- Services, models, and direct HTTP usage (templates)
- Backend controllers and DTOs for field requirements, pagination, and deactivate behavior
- Reference patterns: companies, residents, billing/invoices lists, shared UI components, global styles

See [CONFIGURATION_MODULE_PRE_IMPLEMENTATION_AUDIT.md](./CONFIGURATION_MODULE_PRE_IMPLEMENTATION_AUDIT.md) for the full pre-change audit.

## 2. What was changed

Unified Back Office configuration UX: one list pattern (header, single primary action, error+retry, loading, empty, full-width table, cell hierarchy, icon actions, mobile cards where used), one form pattern (required legend, sections, `form-actions`, entity-specific save labels, post-create navigation to list).

## 3. Components reused

- `PageHeaderComponent`, `EmptyStateComponent`, `LoadingStateComponent`, `ApiErrorComponent`
- `StatusBadgeComponent`, `IconActionButtonComponent`, `TablePaginationComponent` (funding authorities only)
- `ConfirmDialogService`
- Existing global classes: `.table-shell`, `.data-table`, `.form-section`, `.form-actions`, `.mobile-card-list`

## 4. Components newly created

- None (added model file only: `invoice-template.model.ts`)
- New global CSS utilities: `.table-cell-stack`, `.list-error-row`

## 5. Funding Authorities

- Removed filter bar duplicate Add and static toolbar text
- Table: combined name/code column with hierarchy
- Confirm dialog wording aligned
- Form: sections (basic, contact, address, billing), save authority, navigate to list after create
- Loading only when list empty

## 6. Nominal Codes

- Aligned list with shared pattern; removed filter bar duplicate Add
- Mobile cards; error retry; empty/loading order fixed
- Form: subtitle, sections, save nominal code

## 7. Invoice Categories

- Same list/form alignment as nominal codes
- Save category label; deactivate confirm wording

## 8. Invoice Templates

- List: loading, empty, error retry, edit/deactivate actions, table hierarchy
- Route: `invoice-templates/:id/edit`
- Form: edit mode (GET/PUT), sections, removed header Cancel duplicate, `isActive` on edit
- Typed `InvoiceTemplate` model

## 9. Required / optional field decisions

Documented in audit; UI marks `*` only for fields required by frontend validators matching backend `[Required]` / conditional CustomDays interval. Optional fields use “(optional)” on template form labels or no asterisk elsewhere.

## 10. Pagination behavior

- **Funding authorities**: unchanged server pagination via `getFundingAuthoritiesPaged`
- **Others**: no pagination UI (API returns full lists — not faked)

## 11. UUID routing verification

Configuration DTOs expose numeric `Id` only. Routes remain `/…/:id/edit`. No regression to other modules’ `entityRouteKey()` usage.

## 12. Responsive / overflow

- `list-view-table--from-md` / `--from-lg` on table shells
- Mobile card lists for all four modules where desktop tables hide
- Table uses content width without filter bar clutter

## 13. Business logic explicitly preserved

No backend, billing, funding calculation, invoice generation, auth, or database changes.

## 14. Build / test results

| Command | Result |
|---------|--------|
| `npm run build` (frontend/care-home-web) | Success (pre-existing budget / unused import warnings) |
| `dotnet build` (backend/CareHome.Api) | Success (pre-existing NU1902 / obsolete API warnings) |

No new automated tests added (UI-only scope).

## 15. Remaining limitations

- No server-side search on any configuration list API
- Invoice template form still exposes a subset of `UpsertInvoiceTemplateRequest` fields (scoping fields like funding authority / care home / company not in UI — API supports them)
- Full-list modules may need server pagination if tenant data grows large

## 16. Files changed

| File |
|------|
| `docs/ui-ux-refactor/CONFIGURATION_MODULE_PRE_IMPLEMENTATION_AUDIT.md` |
| `docs/ui-ux-refactor/CONFIGURATION_MODULE_QA.md` |
| `docs/ui-ux-refactor/CONFIGURATION_MODULE_IMPLEMENTATION_REPORT.md` |
| `frontend/care-home-web/src/styles.scss` |
| `frontend/care-home-web/src/app/app.routes.ts` |
| `frontend/care-home-web/src/app/shared/ui/breadcrumb.service.ts` |
| `frontend/care-home-web/src/app/features/funding-authorities/pages/funding-authority-list/*` |
| `frontend/care-home-web/src/app/features/funding-authorities/pages/funding-authority-form/*` |
| `frontend/care-home-web/src/app/features/nominal-codes/pages/nominal-code-list/*` |
| `frontend/care-home-web/src/app/features/nominal-codes/pages/nominal-code-form/nominal-code-form.html` |
| `frontend/care-home-web/src/app/features/invoice-categories/pages/invoice-category-list/*` |
| `frontend/care-home-web/src/app/features/invoice-categories/pages/invoice-category-form/invoice-category-form.html` |
| `frontend/care-home-web/src/app/features/invoice-templates/models/invoice-template.model.ts` |
| `frontend/care-home-web/src/app/features/invoice-templates/pages/invoice-template-list/*` |
| `frontend/care-home-web/src/app/features/invoice-templates/pages/invoice-template-form/*` |

Breadcrumbs: expanded `breadcrumb.service.ts` for add/edit paths on all four modules.

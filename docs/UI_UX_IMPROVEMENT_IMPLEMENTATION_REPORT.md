# UI/UX Improvement Implementation Report

## 1. Summary

Platform-wide UX consistency pass: shared design tokens, theme selector, navigation spacing, table layout and icon row actions, server-backed pagination on major lists, company detail hierarchy, resident reference auto-generation (optional on create), invoice payment control grouping, and terminology/accessibility improvements. Numeric route IDs retained (no database UUID migration).

## 2. Issues fixed

| Issue | Status | Files |
|-------|--------|-------|
| Excessive nav section gaps | Fixed | `app.scss`, `app.html` |
| Text-heavy table actions | Fixed (all planned list screens) | `icon-action-button.ts`, list templates |
| Table width / column balance | Fixed (global + column classes) | `styles.scss`, list templates |
| Company detail / hierarchy | Fixed | `company-detail/*`, `CompaniesController.cs`, `CompanyDto.cs`, routes |
| Manual resident IDs | Fixed (optional + auto-gen) | `ClientIdentifierService.cs`, `ClientsController.cs`, `client-form/*` |
| UUID exposure | Display-only pass (no migration) | `breadcrumb.service.ts`, profile/invoice labels, `docs/IDENTIFIER_AND_UUID_ROADMAP.md` |
| Form required/optional clarity | Improved (resident create + profile funding forms + dedicated create routes) | `client-form.html`, `client-profile.*`, `invoice-template-form/*`, `user-form/*`, company/care-home/funding forms |
| Sidebar horizontal scroll | Mitigated | `app.scss`, `app.html` |
| Colour / theme tokens | Fixed | `styles.scss`, `theme.service.ts`, `app.html` |
| Pagination consistency | Fixed (major lists + sage/misc + optional API paging) | `table-pagination.ts`, list pages, `Pagination.cs`, controllers |
| Invoice payment UX | Fixed | `invoice-detail.*` |
| Terminology (Client → Resident) | Improved (lists, profile, reports, misc charges, care home form) | Multiple templates |

## 3. Shared components changed

- `theme.service.ts` (new)
- `table-pagination.ts` (new)
- `icon-action-button.ts` (new)
- Existing: `page-header`, `empty-state`, `toast`, `confirm-dialog` (unchanged behaviour)

## 4. Navigation changes

- Tighter section spacing, nested link indent (`nav-link-nested`)
- Label ellipsis / no horizontal overflow on sidenav
- Breadcrumbs: company detail route

## 5. Table changes

- `table-layout: fixed`, action/status/date/reference column widths
- `.table-actions` icon groups on all operational list screens (including nominal codes, invoice categories, platform tenants, users, credit notes, sage export, misc charges)

## 6. Form changes

- Resident create: optional Sage ID / reference with hints; navigate to new profile on success
- Edit resident: identifiers still required
- Invoice template create: `/invoice-templates/new` (list is read-only)
- User create: `/users/new` (admin; inline form removed from list)
- Company create → company detail; care home create → care home dashboard; funding authority create resets form
- Resident profile: funding contract/rate sub-forms with required markers, saving state, reset after success

## 7. Identifier changes

See audit table in § report body (user message). Implemented: auto `{CareHomeCode}-{###}` reference when blank on create; Sage ID defaults to reference when blank.

## 8. UUID changes

**No migration performed.** Tenants already use `PublicId` (Guid). Operational entities remain `int` PK in API/routes. Recommendation: add optional `PublicId` per entity in a future phased migration.

## 9. Theme changes

- CSS variables per accent: green (default), blue, teal, purple, slate
- Primary flat/raised Material buttons follow `--app-primary` / `--app-primary-hover`
- Persisted in `localStorage` (`care-home-ui-theme`)
- User menu accent picker

## 10. Pagination changes

- Residents, invoices, audit, sage exports, misc charge imports: `app-table-pagination` wired to `PagedResult`
- Companies, care homes, funding authorities, users, credit notes: optional `page` / `pageSize` query params (full list when omitted); Angular lists default to page size 50

## 11. Payment UX changes

- Dedicated payment status panel on invoice detail; single primary action; confirm includes invoice number; loading guard on pay

## 12. Accessibility changes

- Icon actions: `aria-label` + Material tooltip on all `app-icon-action` usages touched

## 13. Responsive changes

- Existing mobile card breakpoints preserved; table horizontal scroll contained in `.table-container`

## 14. Database changes

**None**

## 15. API changes

- `GET /api/companies/{id}`: `CompanyDto` adds `careHomeCount`, `activeCareHomeCount`, `residentCount`
- `POST /api/clients`: `sageId` and `referenceNumber` optional; server generates when omitted
- `GET` list endpoints (companies, care homes, funding authorities, users, credit notes): optional `page` / `pageSize` → `PagedResult`; omit params for legacy full list
- `ClientDto`: additive `CompanyId` for profile company link

## 16. Breaking changes

**None** (optional create fields backward compatible; demo manual IDs still work)

## 17. Verification performed

- Backend `dotnet build` attempted; compile succeeded but output copy blocked by running `CareHome.Api` process (file lock)
- No browser/visual verification (manual QA required)

## 18. Remaining issues

- Invoice template **edit** still list-only or future route (create extracted only)
- Report API keys remain `client-census` / `invoices-by-client` (labels updated in UI)
- Routes still use numeric IDs internally (`/clients/27`); see `docs/IDENTIFIER_AND_UUID_ROADMAP.md` for migration phases
- Company model has no address/contact fields in database — detail shows counts + navigation only
- Occasional code/API names still say “client” (DTOs, CSV column `ClientReference`)

## 19. Manual QA checklist

1. **Navigation**: Collapse/expand sections; confirm spacing; resize to 1024px and 360px — no sidenav horizontal scrollbar.
2. **Theme**: User menu → change accent → refresh page → preference persists; primary buttons match accent (not fixed Material green only).
3. **Companies**: Open company name → detail page with counts; links to care homes (filtered) and residents; list pagination + search.
4. **Residents list**: Search + care home filter; pagination; icon Open/Edit/Archive with tooltips; loading copy says “residents”.
5. **Create resident**: Leave reference/Sage blank → save → lands on profile with generated reference; breadcrumb shows name + reference.
6. **Resident profile**: Company name links to company detail when `CompanyId` present; add contract/rate → Saving… → form clears; optional end/effective-to hints.
7. **Invoices list**: Filters + pagination; row click opens detail; mobile uses icon/tap (no redundant Open button).
8. **Invoice detail**: Payment panel; breadcrumb/confirm use invoice number; resident links from lines.
9. **Credit notes**: List pagination; PDF download icon; invoice link by number.
10. **Audit**: Filter by entity type “Resident”; paginate.
11. **Care homes / funding authorities / users**: Icon row actions, pagination, table width on desktop.
12. **Sage export / misc charges**: Pagination on import/batch lists; misc CSV help mentions resident reference.
13. **Invoice templates / users**: Create only via `/invoice-templates/new` and `/users/new`; breadcrumbs on new routes.
14. **Nominal codes / invoice categories / platform tenants**: Icon edit/deactivate actions with tooltips.
15. **Reports**: Dropdown labels “Resident census” / “Invoices by resident”.

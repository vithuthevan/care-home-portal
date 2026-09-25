# Configuration module — QA checklist

Manual verification checklist after UI/UX consistency refactor.

## Funding Authorities

- [ ] List loads with paged data
- [ ] Pagination changes page and page size (API `page` / `pageSize`)
- [ ] No duplicate “Add authority” when records exist (header only)
- [ ] Create: required fields validated (code, name, type, billing frequency; interval when CustomDays)
- [ ] Create: optional contact/address fields save empty
- [ ] Create: success navigates to `/funding-authorities`
- [ ] Edit loads and saves; returns to list
- [ ] Deactivate shows confirmation; inactive row styling
- [ ] Loading shows on first load only (not full blank on page change)
- [ ] Error shows message + Retry
- [ ] Table shows name over code hierarchy
- [ ] Mobile cards at smaller widths

## Nominal Codes

- [ ] List loads full set (no fake pagination)
- [ ] No search toolbar (API has no search)
- [ ] Create/edit validation (code, name required; description optional)
- [ ] Save labels: “Save nominal code”
- [ ] Deactivate confirmation
- [ ] Empty + loading + error retry
- [ ] Name/code hierarchy in table

## Invoice Categories

- [ ] Same checks as nominal codes (category-specific labels)
- [ ] Save label: “Save category”

## Invoice Templates

- [ ] List loading, empty, error retry
- [ ] Edit route `/invoice-templates/:id/edit` loads template
- [ ] Create and edit save to list
- [ ] Deactivate via icon + confirmation
- [ ] Required: name, category
- [ ] Optional fields labeled optional
- [ ] No duplicate Cancel in page header

## Cross-module

- [ ] Breadcrumbs: list / add / edit for each module
- [ ] Configuration entities still use numeric `:id` routes (no PublicId on DTOs)
- [ ] No global theme/accent picker on these pages
- [ ] Icon actions neutral; tooltips/aria-labels present
- [ ] Consistent page headers, empty states, form-actions layout
- [ ] No horizontal page overflow at laptop width
- [ ] Sidebar configuration links unchanged functionally

## Known API limitations (expected)

- Search not available on any of the four list APIs
- Server pagination only on funding authorities
- Nominal codes, invoice categories, invoice templates return full lists

## Build verification (2026-09-22)

- [x] `npm run build` (care-home-web) — succeeded (existing bundle budget warnings)
- [x] `dotnet build` (CareHome.Api) — succeeded

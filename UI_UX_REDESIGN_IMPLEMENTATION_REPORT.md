# Care Home Back Office — UI/UX redesign implementation report

## 1. Summary

The back office was visually restyled to a compact UK care-home finance SaaS look: restrained tokens, tinted KPI cards, full-width tables, compact toolbars, and numbered pagination. Existing shared components and routes were reused. Billing, funding, invoice, credit-note, Sage, PDF, email, auth, and tenant isolation behaviour were not changed.

## 2. Design system changes

- Extended CSS tokens in `frontend/care-home-web/src/styles.scss`.
- Kept `--app-*` names and added aliases (`--primary`, `--surface`, `--text-muted`, semantic `-light` tokens).
- Added KPI tints (`--kpi-info-bg`, `--kpi-success-bg`, `--kpi-finance-bg`, `--kpi-attention-bg`, `--kpi-critical-bg`).
- Smaller radii (8px / 10px), lighter/no default panel shadows.
- Accent themes (green, blue, teal, purple, slate) now also set light/dark primary tokens.
- Fixed undefined `--shadow-sm` on KPI hover.

## 3. Navigation changes

- Sidebar remains 272px; tighter spacing; soft active state (no heavy inset bar).
- Truncating labels with tooltips.
- Header accent picker added (same five presets). No notification bell (no API).
- User menu accent options retained.

## 4. Table changes

- `.data-table` uses `table-layout: auto` so columns follow content.
- Compact row padding, subtle separators, hover, numeric right-align, action columns right.
- List tables use full workspace width (page max-width no longer 1280px).
- Forms use `.page--narrow`.

## 5. Form changes

- Create/edit forms stay on dedicated routes.
- Resident form: required `*`, optional labels, generated-reference hint, “Save resident” / “Saving resident...”.
- Company and other CRUD forms use the narrow page layout.

## 6. Identifier changes

- No UUID / PK migration.
- Routes still use numeric `:id`.
- UI continues to show business keys (`ReferenceNumber`, invoice/credit numbers, care-home `Code`).
- Resident profile subtitle is `{reference} · {care home}` (not Sage ID / numeric ID).
- Credit notes still use resident search, not Client ID fields.

## 7. Pagination changes

- Numbered pages plus “Showing x–y of z”.
- Page sizes 20 / 50 / 100.
- Core lists remain server-paged.
- Care-home search filters a small unpaged set when a search term is entered (care homes are a small dataset).
- Reports remain full-result (no fake client-side paging of large downloads).

## 8. Payment UX changes

- Invoice detail still shows only the opposite payment action, with confirmation.
- Invoice list bulk Mark paid / Mark unpaid is now selection-aware (not both equally when nothing relevant is selected).

## 9. Credit note changes

- Context banner (invoice, resident, period) kept and restyled with the design system.
- Default notes page size 20.

## 10. Company changes

- Detail page KPIs and related care-homes table via existing `GET /api/care-homes?companyId=`.
- No invented address/reference fields (`CompanyDto` still name/status/counts).

## 11. Care home changes

- Search toolbar on the list.
- Dashboard: tinted KPIs, location details from existing model, residents table with name/reference/status.
- No weekly-rate column (`ClientDto` has no rate).

## 12. Resident changes

- Compact search toolbar with debounce.
- Empty state distinguishes filters vs none.
- Profile hero subtitle uses reference + care home.
- UI copy stays “Resident”; routes remain `/clients`.

## 13. Billing changes

- Empty preview copy updated to the specified guidance sentence.
- Resident → Start billing query context unchanged.

## 14. Invoice changes

- Compact list toolbar.
- Detail: Download PDF primary; Email / Credit note as labelled icon actions; Void under More.
- Grouped customer / funding / period / lines retained.

## 15. Report changes

- Column labels already humanized; payment columns use status badges.
- Export CSV / Excel / PDF unchanged.

## 16. Audit changes

- Read-only `UserDisplayName` added to the audit list DTO.
- UI shows display name, humanized action, description as entity label, and “Resident” for Client entity type.
- Logging behaviour unchanged.

## 17. Accessibility changes

- Icon actions keep `aria-label` + tooltip.
- Accent picker has an accessible name.
- Pagination page buttons have `aria-label` / `aria-current`.
- Status badges include text; Not Paid uses warning tone, Void/Cancelled use danger.
- Focus rings remain on `--app-primary`.

## 18. Responsive changes

- Lists use full width; forms stay readable.
- Existing mobile card lists retained.
- Filter toolbars wrap; page overflow remains `hidden` on `html, body`.
- Sidebar overlay ≤1024px unchanged.

## 19. Files changed

Frontend (principal):

- `frontend/care-home-web/src/styles.scss`
- `frontend/care-home-web/src/app/app.html`, `app.scss`, `app.ts`
- `frontend/care-home-web/src/app/shared/ui/kpi-card.ts` (new)
- `frontend/care-home-web/src/app/shared/ui/filter-bar.ts`
- `frontend/care-home-web/src/app/shared/ui/table-pagination.ts`
- `frontend/care-home-web/src/app/shared/ui/status-badge.ts`
- `frontend/care-home-web/src/app/shared/ui/page-header.ts`
- `frontend/care-home-web/src/app/shared/ui/workflow-steps.ts`
- Dashboard, companies, care homes, residents, billing, invoices, credit notes, reports, audit, settings, users, funding authorities, misc charges, Sage export, and related form pages.

Backend:

- `backend/CareHome.Api/Dtos/Audit/AuditLogDto.cs`
- `backend/CareHome.Api/Controllers/AuditController.cs`

Docs:

- `UI_UX_REDESIGN_MANUAL_QA.md`
- `UI_UX_REDESIGN_IMPLEMENTATION_REPORT.md`

## 20. Backend changes

**Backend changed: YES** (audit list display name only)

## 21. Database changes

**Database changed: NO**

## 22. Migrations

**Migration required: NO**

## 23. Remaining issues

- Organisation `PrimaryColour` (invoice branding) is still separate from the UI accent picker by design.
- Material’s `mat.theme()` primary palette remains green; app chrome/buttons follow CSS variables.
- Reference-data lists (invoice categories, nominal codes, templates, platform tenants) stay unpaged because they are small.
- Reports have no server pagination; large exports still download the full result.
- Care-home name search is client-filtered when a search term is present (backend has no search query today).
- Weekly rate is not shown on the care-home resident table because the client list DTO has no rate field.
- No in-app notification centre (no notification API).

## 24. Manual QA instructions

Follow `UI_UX_REDESIGN_MANUAL_QA.md`.

---

**Backend changed: YES** (audit `UserDisplayName` join only)

**Database changed: NO**

**Migration required: NO**

**Breaking changes: NO**

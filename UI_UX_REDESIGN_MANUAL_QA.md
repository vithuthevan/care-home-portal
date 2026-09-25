# Care Home Back Office — UI/UX redesign manual QA

Literal click-path checks after the visual redesign. Do not change billing, funding, or payment rules while executing these steps.

Use a tenant user with write access unless a step says otherwise.

---

### Shell

1. Sign in and open any authenticated page.
2. Confirm the left sidebar is about 272px wide and does not scroll horizontally.
3. Confirm the organisation name appears under the Care Home mark.
4. Confirm the active nav item has a soft coloured background and stronger text/icon colour.
5. Hover a long label such as Organisation Settings and confirm a tooltip appears.
6. Collapse and expand Operations, Billing Setup, Billing, Reporting, and Administration.
7. Confirm the top bar shows: menu toggle, breadcrumb, accent palette button, avatar, name, and role.
8. Open the accent picker and switch Green → Blue → Teal → Purple → Slate.
9. Refresh the browser and confirm the selected accent is still applied.
10. Resize to 1024px. Confirm the sidebar overlays and the menu toggle works.
11. Resize to a mobile width. Confirm no whole-page horizontal scrollbar.

### Dashboard

1. Open `/dashboard`.
2. Confirm the sidebar is visible and Dashboard is the active item.
3. Confirm no horizontal scrollbar.
4. Confirm KPI cards use tinted backgrounds (operations, residents, outstanding, upcoming).
5. Confirm primary actions (Add resident, Start billing) are aligned in the header/action strip.
6. Click Current residents KPI and confirm `/clients` opens.
7. Click Outstanding invoices KPI and confirm the invoice list is filtered to Not paid.
8. Resize to 1024px and confirm cards wrap.
9. Resize to mobile and confirm cards stack.

### Companies

1. Open Companies.
2. Confirm the search field is a compact toolbar, not a large separate card.
3. Type a company name and wait; confirm results update without a full page reload of the app.
4. Confirm the table uses available width and the Actions column is on the right.
5. Hover the edit icon and confirm the tooltip says "Edit company".
6. Open a company.
7. Confirm breadcrumb: Home → Companies → {name}.
8. Confirm KPI cards for status, care homes, and residents.
9. Confirm the care homes table lists code, name, capacity, and status with dashboard links.
10. Confirm no internal numeric company ID is displayed as a business label.
11. Click Edit and confirm the form is a dedicated page with a readable width.

### Care Homes

1. Open Care Homes.
2. Confirm search is visible.
3. Search by name or code and confirm matching rows.
4. Confirm empty-state copy says “residents”, not “clients”.
5. Open a care home dashboard.
6. Confirm subtitle shows `{CODE} · {Company}`.
7. Confirm KPI cards: Capacity, Occupancy, Available beds, Outstanding.
8. Confirm Location shows address/phone/email when present.
9. Confirm Current residents is a table with name (link), reference, and status — not a bullet list of names.
10. Open a resident from that table.

### Residents

1. Open Residents (`/clients`).
2. Confirm the table uses available width.
3. Confirm search placeholder is “Search residents...”.
4. Filter by care home and confirm pagination still uses the filter.
5. Confirm pagination shows “Showing x–y of z” and page numbers, with 20 / 50 / 100.
6. Hover the edit icon and confirm tooltip “Edit resident”.
7. Confirm keyboard Tab reaches the icon buttons.
8. Open a resident.
9. Confirm breadcrumb: Home → Residents → {name}.
10. Confirm subtitle is `{Reference} · {Care home name}` and not a numeric ID or Sage ID as the primary subtitle.
11. Confirm tabs: Resident details, Funding, Billing, Invoices.
12. Click Add resident.
13. Confirm required fields show `*` (First name, Last name, Care home, Care type, Admission date).
14. Confirm optional fields are labelled optional.
15. Confirm Resident reference hint says it is automatically generated when left blank.
16. Save a valid resident and confirm navigation to the new profile.

### Billing

1. Open Billing Workspace.
2. Confirm steps: Scope → Preview → Review → Generate.
3. With no preview, confirm the empty message:
   “Choose a company, care home and billing period, then preview billing to see eligible residents.”
4. From a resident profile click Start billing.
5. Confirm company/care home context is preselected.

### Invoices

1. Open Invoices.
2. Confirm compact search and status filters.
3. Select an unpaid invoice only. Confirm Mark paid is available and Mark unpaid is not equally shown.
4. Select a paid invoice only. Confirm Mark unpaid is available.
5. Open an unpaid invoice.
6. Confirm header shows invoice number, total, and payment status.
7. Confirm primary action is Download PDF.
8. Confirm Email and Credit note are compact icon actions with tooltips:
   “Send invoice email” and “Create credit note”.
9. Confirm Void is under More, not a large equal button.
10. Confirm Mark as paid is shown and Mark as unpaid is not.
11. Confirm a confirmation dialog appears before changing payment status.
12. After success, confirm the UI updates and the opposite action appears.
13. Confirm resident name is a link and the reference is shown, not a raw Client ID.

### Credit notes

1. From an invoice, choose Credit note.
2. Confirm the banner shows original invoice number, resident name/reference, and period.
3. Confirm the Resident field is a search, not a numeric Client ID box.
4. Preview, then confirm existing notes paginate with 20 / 50 / 100.

### Reports

1. Open Reports.
2. Run Resident census.
3. Confirm column headers are business labels (Resident, not `clientName`).
4. Confirm currency and dates are formatted.
5. Confirm Payment Status uses badges on the outstanding report.
6. Confirm Export CSV, Excel, and PDF remain available.
7. Confirm an empty run shows an empty state, not a blank page.

### Audit

1. Open Audit (admin).
2. Confirm columns: Actor, Action, Entity, When.
3. Confirm the actor is a display name (or System), not a raw user id.
4. Confirm entity type Client is labelled Resident.
5. Confirm description/business text is preferred over a raw numeric entity id.
6. Change pages and confirm the entity-type filter remains applied.

### Settings

1. Open Organisation Settings.
2. Confirm Back office accent offers Green, Blue, Teal, Purple, Slate.
3. Change accent and confirm the chrome colour updates.
4. Confirm this does not require saving organisation branding / primary colour.

### Users

1. Open Users.
2. Confirm the table uses available width and pagination is present.
3. Add user via the dedicated form page.

### Responsive

1. At 1920px and 1440px, confirm list tables fill the workspace (not a narrow 1280px column with large empty sides).
2. At 1280px and 1024px, confirm filters wrap and buttons do not overflow.
3. At 768px and mobile, confirm tables switch to cards where the list-view pattern is used.
4. Confirm only tables (not the page) may scroll horizontally.

### Accessibility

1. Tab through the sidebar, header, filters, table, and pagination.
2. Confirm visible focus rings.
3. Confirm every icon-only button has an accessible name (screen reader / tooltip).
4. Confirm status badges include text, not colour alone.
5. Confirm required fields are indicated in the label as well as by validation messages.

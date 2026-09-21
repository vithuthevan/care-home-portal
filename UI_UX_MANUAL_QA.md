# Care Home — UI/UX manual QA (second pass)

Use a desktop browser logged in as a tenant user with read/write access. Repeat key steps at **1920**, **1280**, **1024**, **768**, and a **mobile** width. The page body must not horizontal-scroll; only tables may scroll inside `.table-container`.

For each area: complete steps 1–12 where applicable.

---

## Dashboard

1. Open `/dashboard`.
2. Verify layout: title **Operations centre**, breadcrumb `Home / Dashboard`, finance and ops KPI rows, attention queue, recent invoices table.
3. Verify primary action: **Start billing** (if write access).
4. Search/filter: N/A.
5. Table: recent invoices — columns align, amounts right-aligned.
6. Pagination: N/A on dashboard widgets.
7. Loading: hard refresh — spinner then content.
8. Empty state: N/A unless API returns zeros (KPIs show 0).
9. Errors: stop API — `app-api-error` message.
10. Keyboard: Tab to KPI links and invoice rows; Enter opens invoice.
11. Responsive: KPIs stack; tables scroll inside container only.
12. Next step: click outstanding KPI or **Start billing** → billing workspace.

---

## Companies

1. Open `/companies`.
2. Verify page header breadcrumb, title, subtitle, **Add company**.
3. Primary action: **Add company** (header and/or table toolbar).
4. Search: type in toolbar search — list updates.
5. Table: company name, status, actions.
6. Pagination: change page size if many companies.
7. Loading: spinner on first load.
8. Empty: no companies — empty state with CTA.
9. Errors: API failure banner.
10. Keyboard: Tab through search, table links, icon actions.
11. Responsive: mobile cards below breakpoint.
12. Next step: open company → detail tabs.

---

## Care home dashboard

1. Open a home via **Care Homes** → dashboard icon.
2. Verify summary strip (capacity, occupied, available, outstanding), **Preview billing** (write), residents and invoices in table shells.
3. Click a resident row → profile; **All residents** → list filtered by `careHomeId` query.
4.–12. Loading, errors, keyboard, responsive as for other detail pages.

---

## Care homes

1. Open `/care-homes`.
2. Header + **Add care home**.
3. Primary action: add care home.
4. Search in table toolbar.
5. Table: code, name, company, capacity, status.
6. Pagination: if paged.
7. Loading / 8. Empty / 9. Errors — as companies.
10. Keyboard navigation on rows and actions.
11. Responsive layout.
12. Open **dashboard** icon → care home dashboard.

---

## Residents

1. Open `/clients`.
2. Header, breadcrumb, **Add resident**.
3. Primary: add resident (header + toolbar when data present).
4. Search + care home filter + archived checkbox.
5. Table: resident, reference, home, status, admission.
6. Pagination: page controls at bottom.
7.–9. Loading, empty (filtered vs none), errors.
10. Keyboard: row opens profile; actions stop propagation.
11. Responsive: mobile cards.
12. Click row → resident profile.

---

## Resident profile

1. Open a resident from the list.
2. Breadcrumb includes resident name/reference; summary strip (funding, rate, period, outstanding).
3. Primary: **Start billing** (write).
4. Filters: N/A on profile; tabs filter content.
5. Tables: invoices tab if present.
6. Pagination on invoice tab if many.
7. Loading skeleton/state.
8. Empty funding — warning to add contract.
9. Errors on load.
10. Keyboard: tabs, buttons, tables.
11. Responsive: hero stacks.
12. **Start billing** → billing with query params; placement links to care home.

---

## Funding (authorities list)

1. Open `/funding-authorities`.
2. List header and add action.
3.–6. Search/filter, table, pagination as implemented.
7.–12. States, keyboard, responsive, navigate to edit form.

---

## Billing

1. Open `/billing`.
2. Workflow steps visible (Scope → Preview → Review → Generate).
3. Primary: **Preview billing** after scope filled.
3b. After preview: hero shows period title, care home scope, eligible count, total £, exception count.
4. Scope fields: company, home, category, dates.
5. Preview table: residents, amounts, exceptions.
6. N/A pagination on preview.
7. Working states on preview/generate.
8. Empty before preview — empty state copy.
9. API errors surfaced.
10. Keyboard through form and buttons.
11. Responsive grid on scope fields.
12. Generate → invoices list or success banner; from resident profile confirm prefill banner.

---

## Invoices

1. Open `/invoices`.
2. Header breadcrumb under Billing trail.
3. Bulk **Mark paid** when selection allows.
4. Toolbar search + status + payment filters inside table shell.
5. Table: select, invoice #, period, amounts, statuses.
6. Pagination.
7.–9. Loading, empty, errors.
10. Keyboard: checkboxes, row navigation.
11. Responsive cards.
12. Open invoice detail.

---

## Invoice detail

1. Open `/invoices/:id`.
2. Breadcrumb; hero with amount, invoice + payment status, resident link + reference.
3. Primary: **Download PDF** in header.
4. N/A filters.
5. Line items table.
6. N/A.
7. PDF loading disables button.
8. N/A.
9. Errors.
10. Payment mark paid/unpaid with confirmation.
11. Responsive hero stacks.
12. Credit note / resident profile links if shown.

---

## Payments (invoice detail)

1. On unpaid invoice, payment panel shows **Not paid** and **Mark as paid**.
2. Confirm dialog — status updates.
3. Paid state shows **Mark as unpaid** only as secondary.
4. Visual status badges on hero and panel.

---

## Credit notes

1. Open `/credit-notes`.
2. Resident search (no “Client ID” field).
3. From invoice: open with `invoiceId` query — context banner.
4. Create flow fields labelled in plain language.
5.–12. Submit states, errors, keyboard, responsive.

---

## Reports

1. Open `/reports`.
2. Report type, date range, **Run report**.
3. Column headers are human labels (e.g. Resident, not `clientName`).
4. Summary/table/export CSV/Excel/PDF.
5. Pagination: N/A for full export; table scroll if wide.
6.–12. Loading, empty results, errors, keyboard, responsive.

---

## Audit

1. Open `/audit` (admin).
2. List layout, filters if present.
3.–12. Table density, pagination, loading, empty, errors, keyboard, responsive.

---

## Users (admin)

1. Open `/users`.
2. Page header, breadcrumb, **Add user**.
3. Table shell with user count in toolbar; sticky table (name, email, role, homes, status).
4.–8. Pagination, empty state, errors.
9.–12. Deactivate confirmation, keyboard, responsive cards, **Add user** → form.

---

## Miscellaneous charges

1. Open `/misc-charges`.
2. Subtitle explains CSV + resident reference (no internal IDs).
3. **Choose CSV file** in import toolbar (write access).
4. Preview shell: valid/invalid counts, table, **Confirm import** (disabled if invalid rows).
5. Import history `table-shell` with pagination or empty state.
6.–12. Success banner, API errors, keyboard, responsive.

---

## Settings

1. Open `/settings/organisation`.
2. SETTINGS layout, sections clear.
3. Theme: toolbar palette + user menu accent options — selection persists after reload.
4.–12. Form labels, required markers, save feedback, keyboard, responsive.

---

## Shell (cross-cutting)

1. Toggle sidebar on desktop — collapses to icons; reload — state persists.
2. Mobile: drawer overlay; nav closes on navigate.
3. Breadcrumbs only in page headers (not top toolbar).
4. User menu: sign out, theme.
5. No horizontal scroll on `main.page`.

---

## Sign-off checklist

- [ ] Product feels like finance operations SaaS, not a raw CRUD admin.
- [ ] Each screen has one obvious primary action.
- [ ] Lists use toolbar + table shell pattern.
- [ ] Detail pages show entity context (strip or hero).
- [ ] Business references visible; raw DB IDs not shown to users.

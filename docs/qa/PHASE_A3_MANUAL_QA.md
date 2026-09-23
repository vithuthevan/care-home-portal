# Phase A3 — Manual QA (operator script)

**Date:** 2026-09-23  
**Environment:** Local or staging with V1 navigation (Commercial Revenue **disabled**).

For each test: record pass/fail and note any failure indicators.

---

## TEST 01 — Login

**Prerequisites:** API and database running; valid tenant user credentials.

**Steps:**

1. Open the application URL.
2. Sign in with username and password.

**Expected:**

1. Dashboard loads without errors.
2. Navigation shows Operations, Billing setup, Billing, Reporting, Administration.
3. Commercial Revenue modules are **not** visible.

**Failure indicators:** 401 loop, blank shell, receivables/payments nav visible.

---

## TEST 02 — Dashboard

**Prerequisites:** TEST 01 passed.

**Steps:**

1. Open Dashboard.
2. Click **Outstanding invoices** KPI (or equivalent label when Commercial Revenue is off).

**Expected:**

1. KPI opens **Reports** with **Payment status / outstanding** selected (`report=outstanding`).
2. Care home occupancy links use UUID paths in the browser address bar.
3. Recent invoice links open invoice detail with UUID in URL.

**Failure indicators:** KPI opens generic invoice list only; numeric-only entity URLs from dashboard links.

---

## TEST 03 — Create company

**Prerequisites:** User with write access.

**Steps:**

1. Companies → Add company.
2. Enter company name; leave optional fields empty.
3. Save.

**Expected:**

1. Success toast.
2. Return to Companies list; new company visible.
3. Save disabled while invalid or saving.
4. No stale values if you open Add again.

**Failure indicators:** Duplicate submit creates two companies; form stays populated incorrectly.

---

## TEST 04 — Company detail & care homes

**Prerequisites:** At least one company.

**Steps:**

1. Open a company from the list.
2. Confirm breadcrumbs: Companies → {name}.
3. From empty state or table, use **Add care home**.

**Expected:**

1. Company information and care home counts visible (no invented fields).
2. Add care home opens create form with company preselected (`company` query uses UUID).
3. Care home table links use `/care-homes/{uuid}/dashboard`.

**Failure indicators:** Internal numeric company id in URL; broken add-home context.

---

## TEST 05 — Create care home

**Prerequisites:** Active company.

**Steps:**

1. Care Homes → Add care home (or from company detail).
2. Complete required fields; save.

**Expected:**

1. Success toast.
2. Redirect to **care home dashboard** with UUID in URL (not `/care-homes/12/...` unless legacy fallback only).

**Failure indicators:** Redirect to list only; edit link from list fails to load.

---

## TEST 06 — Care home dashboard continuity

**Prerequisites:** Care home with optional residents/invoices.

**Steps:**

1. Open care home dashboard.
2. Use **View all** residents and **View all** invoices.
3. Use browser Back.

**Expected:**

1. Residents list filtered with `careHome={uuid}` in query (not required to expose int id).
2. Invoices list shows breadcrumb: Billing → Invoices (and care home context when filtered).
3. Back returns to dashboard.

**Failure indicators:** Filter lost; numeric care home id in query only with broken filter.

---

## TEST 07 — Create resident

**Prerequisites:** Active care home.

**Steps:**

1. Residents → Add resident (optionally from care home context).
2. Fill required fields; save.

**Expected:**

1. Success; list or profile shows new resident.
2. Profile URL uses UUID.

**Failure indicators:** Profile fails to load (NaN / 404) after save from list.

---

## TEST 08 — Resident profile & billing handoff

**Prerequisites:** Resident with care home assigned.

**Steps:**

1. Open resident profile.
2. Billing tab → **Start billing** (or equivalent).
3. On billing workspace, read scope banner before Preview.

**Expected:**

1. Breadcrumbs: Residents → {name}.
2. Billing shows company, care home, and period context when provided.
3. Query uses `company`, `careHome`, `client` UUID keys (legacy int params still work if bookmarked).

**Failure indicators:** Empty scope; preview runs without visible company/home.

---

## TEST 09 — Funding authority

**Prerequisites:** Write access.

**Steps:**

1. Create funding authority with required fields.
2. Open list; edit via row action.

**Expected:**

1. Edit URL uses UUID when `publicId` present.
2. Usage column and deactivate confirmation reference real counts.

**Failure indicators:** 404 on edit from list.

---

## TEST 10 — Funding contract & rate

**Prerequisites:** Resident, funding authority, category, nominal code.

**Steps:**

1. Resident profile → Funding → add contract.
2. Add rate on contract.

**Expected:**

1. Return to resident funding tab with breadcrumbs intact.
2. No manual internal IDs required.

**Failure indicators:** Stuck on form; wrong resident context.

---

## TEST 11 — Billing preview

**Prerequisites:** Funded resident; billing period valid.

**Steps:**

1. Billing workspace → set scope → Preview.

**Expected:**

1. Ready / Requires attention banners match preview state.
2. No change to calculated amounts vs prior baseline.

**Failure indicators:** Silent calculation change; preview without scope labels.

---

## TEST 12 — Billing generation

**Prerequisites:** Preview **can generate**.

**Steps:**

1. Generate invoices.
2. Open Invoices list.

**Expected:**

1. Success feedback; new invoice(s) listed.
2. Invoice detail URL uses UUID.

**Failure indicators:** Duplicate generation without warning (if applicable); wrong tenant scope.

---

## TEST 13 — Invoice detail & PDF

**Prerequisites:** Generated invoice.

**Steps:**

1. Open invoice detail.
2. Download PDF.
3. Follow links to resident, care home, company.

**Expected:**

1. Breadcrumbs: Billing → Invoices → {number}.
2. PDF downloads.
3. Cross-links use UUID routes.

**Failure indicators:** PDF 404; links use raw numeric paths in address bar.

---

## TEST 14 — Invoice list bulk mark paid / unpaid

**Prerequisites:** Unpaid invoice; write access. Invoice list open.

**Steps:**

1. Select one or more unpaid invoices.
2. Choose **Mark as paid** → confirm → wait for completion.
3. Select the same invoices.
4. Choose **Mark as unpaid** → confirm.

**Expected:**

1. Confirmation dialog uses **Update payment status** / **Mark as paid** / **Mark as unpaid**.
2. Buttons disable during the request (no double submit).
3. Success feedback; list payment status updates to **Paid** then **Unpaid**.
4. Invoice status (Generated/Sent/Void) is unchanged.

**Failure indicators:** No confirmation; status does not refresh; PaidAt/Paid on appears; invoice status overwritten by payment status.

---

## TEST 15 — Credit note from invoice

**Prerequisites:** Invoice with lines eligible for credit.

**Steps:**

1. Invoice detail → Create credit note.
2. Confirm resident, period, reason prefilled where applicable.
3. Preview → Generate.

**Expected:**

1. Success toast; form resets; list refreshes.
2. Existing credit notes link to invoice via UUID.
3. Operator never types client or invoice internal IDs.

**Failure indicators:** Credit note list links `/invoices/42`; preview allowed without reason.

---

## TEST 16 — Reports

**Prerequisites:** Some invoice/resident data.

**Steps:**

1. Reports → run **Payment status / outstanding**.
2. Export if available.

**Expected:**

1. Friendly column labels; currency and dates formatted.
2. Loading and empty states behave.

**Failure indicators:** Raw property names in grid.

---

## TEST 17 — Sage export (history)

**Prerequisites:** At least one previous Sage export batch.

**Steps:**

1. Open **Reporting → Sage Export**.
2. Read the page explanation and previous-export columns.

**Expected:**

1. Page does not say “provisional mapping”.
2. Columns distinguish invoices exported vs CSV file availability.
3. **Retry CSV** appears only when the CSV is unavailable.

**Failure indicators:** Raw **FileMissing** enum shown to the user; user told invoices were not exported when a batch exists.

---

## TEST 18 — Audit

**Prerequisites:** Admin or permitted role.

**Steps:**

1. Audit → filter by entity type and action.
2. Follow safe entity link (invoice, resident, company, care home).

**Expected:**

1. No internal implementation noise in reference column.
2. Links use UUID where entity supports it.

**Failure indicators:** Broken links; numeric-only paths where UUID expected.

---

## TEST 19 — Theme (care home portal)

**Prerequisites:** Care home with settings access.

**Steps:**

1. Care home → Portal settings → change accent.
2. Confirm global toolbar has **no** care-home accent picker (only light/dark).

**Expected:**

1. Portal accent applies to that care-home context only.

**Failure indicators:** Global theme polluted.

---

## TEST 20 — UUID navigation sweep

**Prerequisites:** Data with `publicId` populated.

**Steps:**

1. From each primary list, open detail and copy URL.
2. Open in new tab; use Back through workflow Organisation → company → home → resident → invoice.

**Expected:**

1. URLs use UUID for core entities.
2. Bookmarks with legacy numeric ids still load detail where API dual-key supported.

**Failure indicators:** Profile or edit forms fail on UUID URLs.

---

## TEST 21 — Validation failures

**Prerequisites:** Write access.

**Steps:**

1. Submit company form empty.
2. Submit credit note preview without reason.

**Expected:**

1. Inline errors; submit blocked.
2. Credit note shows validation message before API call.

**Failure indicators:** API 400 with no user message.

---

## TEST 22 — Pagination & search

**Prerequisites:** Enough rows on companies, care homes, residents, invoices, credit notes, audit.

**Steps:**

1. Change page size and page on each major list.
2. Search residents/companies where server-side search exists.

**Expected:**

1. Server-backed pagination where implemented.
2. No client-side-only fake paging on large API lists.

**Failure indicators:** Entire dataset loaded in browser for care homes (known limitation if unpaged API used).

---

## TEST 23 — Invoice detail → Mark as paid

**Prerequisites:** Generated unpaid invoice; write access; Commercial Revenue disabled.

**Steps:**

1. Open the invoice from the invoices list (detail URL).
2. In **Payment status**, confirm the badge shows **Unpaid**.
3. Click **Mark as paid**.
4. Read the confirmation; confirm.
5. Wait until the button is no longer in the updating state.
6. Click **Mark as paid** again immediately if it is still visible (should not be).

**Expected:**

1. Detail is the payment-status control (not only a More-menu action).
2. Confirmation is required.
3. Button is disabled while the request runs.
4. Success feedback is shown.
5. Payment status updates to **Paid** and the action becomes **Mark as unpaid**.
6. Invoice status remains Generated/Sent (not replaced by payment status).
7. No **Paid on** / PaidAt field is shown.

**Failure indicators:** Action missing on detail; “Legacy” wording; double submit creates conflicting state; invoice status changes to Paid.

---

## TEST 24 — Invoice detail → Mark as unpaid

**Prerequisites:** TEST 23 passed, or an invoice already marked paid; write access.

**Steps:**

1. Open the paid invoice detail.
2. Confirm **Payment status** shows **Paid**.
3. Click **Mark as unpaid**.
4. Confirm in the dialog.
5. Wait for completion.

**Expected:**

1. Confirmation uses **Mark as unpaid**.
2. Loading/disabled state during the request.
3. Success feedback.
4. Payment status updates to **Unpaid** and the action becomes **Mark as paid**.
5. Invoice status is unchanged.

**Failure indicators:** Mark as unpaid missing on detail; operator must return to the list to reverse payment status.

---

## TEST 25 — Paid invoice → reopen detail

**Prerequisites:** Invoice marked paid in TEST 23.

**Steps:**

1. Leave invoice detail (Invoices list or another page).
2. Reopen the same invoice.

**Expected:**

1. Payment status shows **Paid**.
2. **Mark as unpaid** is available (write access).
3. Invoice status is still the invoice status, shown separately.

**Failure indicators:** Detail reloads as Unpaid; Mark as unpaid missing after refresh.

---

## TEST 26 — Unpaid invoice → reopen detail

**Prerequisites:** Invoice marked unpaid in TEST 24, or a never-paid invoice.

**Steps:**

1. Leave invoice detail.
2. Reopen the same invoice.

**Expected:**

1. Payment status shows **Unpaid**.
2. **Mark as paid** is available (write access).

**Failure indicators:** Detail reloads as Paid incorrectly; Mark as paid missing.

---

## TEST 27 — Funding overlap error

**Prerequisites:** Resident with two overlapping funding arrangements for the same authority and invoice category covering the billing period. Write access.

**Steps:**

1. Either save an overlapping funding arrangement on the resident, or run Billing Workspace preview for that resident/period.
2. Read the error shown in the UI (form error and/or billing exception).

**Expected:**

1. Message describes overlapping funding arrangements for the resident, authority/category, and dates where available.
2. Billing remains blocked.
3. No contract integer IDs, UUIDs, or “Contract IDs:” text in the user message.

**Failure indicators:** “Contract IDs: 12, 15” (or similar); billing proceeds despite overlap.

---

## TEST 28 — Sage successful export

**Prerequisites:** Invoices eligible for Sage export (Sage ID and nominal present); write access.

**Steps:**

1. Sage Export → set date range → **Validate**.
2. Confirm ready-to-export count and no blocking errors.
3. **Export CSV**.
4. Download the file from previous exports.

**Expected:**

1. Success message states the Sage 50 CSV was exported and is available.
2. History row shows invoices exported and CSV available.
3. Download opens/saves a CSV.

**Failure indicators:** “Provisional mapping” copy; success claimed when the file cannot be downloaded; validation errors ignored.

---

## TEST 29 — Sage FileMissing

**Prerequisites:** An export batch whose CSV file is unavailable (status FileMissing in the database), or a write failure after invoices were marked exported.

**Steps:**

1. Open Sage Export previous exports.
2. Locate the batch with unavailable CSV.

**Expected:**

1. User-facing text is **Export recorded, but the CSV file is unavailable** (or equivalent).
2. Invoices exported column still indicates the export was recorded.
3. Download is hidden; **Retry CSV** is shown for write users.
4. The UI does not say that invoices were not exported.

**Failure indicators:** Raw **FileMissing** label only; message implying no export occurred.

---

## TEST 30 — Sage Retry CSV

**Prerequisites:** TEST 29 batch; write access.

**Steps:**

1. Click **Retry CSV**.
2. Wait for completion (button disabled while retrying).
3. Download the file.

**Expected:**

1. CSV becomes available without creating a second export of the same invoices.
2. Success feedback that the file is available.
3. Invoices remain marked exported (no duplicate Sage export marks).

**Failure indicators:** Retry missing; retry re-exports invoices; download still unavailable with no explanation.

---

## TEST 31 — Technical wording check

**Prerequisites:** V1 navigation (Commercial Revenue off). Access to invoices, billing workspace, Sage export, and a funding overlap or exception if available.

**Steps:**

1. Open invoice detail payment status (paid and unpaid).
2. Open invoice list payment filter and bulk actions.
3. Open Sage Export header and a FileMissing row if present.
4. Trigger or inspect a funding overlap error.
5. Scan visible labels for: Legacy, provisional, FileMissing, DTO, API, database, UUID, PublicId, contractId, clientId, invoiceId.

**Expected:**

1. Payment wording is **Payment status**, **Paid**, **Unpaid**, **Mark as paid**, **Mark as unpaid**.
2. Sage page explains CSV export in business language.
3. Overlap errors use resident / authority / dates, not contract IDs.
4. Remaining technical terms are only legitimate business fields (for example **Sage ID**) or admin-only notes documented in `docs/qa/V1_TECHNICAL_WORDING_AUDIT.md`.

**Failure indicators:** “Legacy: mark as paid”; “provisional mapping”; “Contract IDs:”; raw FileMissing as the only explanation.

---

## Sign-off

| Tester | Date | Result |
|--------|------|--------|
| | | |

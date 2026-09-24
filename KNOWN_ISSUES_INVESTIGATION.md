# Known Issues Investigation

Static review of the Angular app (`frontend/care-home-web`) and the .NET API (`backend/CareHome.*`). No code, migrations, seed data, or packages were changed. Behaviour below is what the current implementation does. Items that depend on a running database or a generated PDF are marked as needing runtime verification.

## Executive Summary

1. **Duplicate Add buttons** are a repeated layout choice, not a responsive double-render. The page header always shows the action, and the same action is rendered again in the empty state and, on a few lists, in the filter toolbar.
2. **Optional email** fails when the field is left blank. The UI and database treat email as optional. Angular allows an empty value. ASP.NET `[EmailAddress]` rejects `""`.
3. **Invoice header and footer text are wired** from the template onto the invoice snapshot and into the PDF, including the PDF attached to email. They are not shown on the on-screen invoice page. A second header field, logos, and email subject/body templates are only partly used.
4. **Light/dark mode** lives inside the user menu. Moving one icon button to the left of the user chip is a small shell change.
5. **Nominal codes are tenant-owned and currently unseeded.** New organisations get invoice categories, not nominal codes. Billing contracts and Sage export cannot proceed until someone creates them by hand.
6. **Default sort is not “newest first” everywhere.** Residents are alphabetical. Invoices and credit notes already sort by document date descending, which is the better business order. Reference lists sort by name.
7. **Credit notes adjust receivable balance correctly**, but income reports and Sage export ignore them. The screen always credits the full remaining amount. Partial amounts and void exist in the API model and are not exposed. A credit started from an invoice is not locked to that invoice id.
8. **Nine reports are implemented and return real queries.** Income-style reports are gross invoice lines and omit credits. Outstanding balance does subtract credits. The report screen only sends a date range, even where the API accepts more filters.
9. **Miscellaneous charges are connected to billing.** CSV import stores them; preview/generate picks unbilled rows in the period, writes an invoice line, and marks the charge invoiced. They need a nominal code and a funding contract. Crediting the line does not release the charge for rebilling.

| # | Area | Finding | Severity | Fix Needed |
|---|---|---|---|---|
| 1 | Duplicate Add buttons | Header action is also rendered in the empty state, and on residents, care homes, and users it is rendered again in the filter bar. | Low (UX) | YES |
| 2 | Optional email | Blank optional emails are posted as `""` and rejected by `[EmailAddress]`. | Medium | YES |
| 3 | Invoice header/footer | Header/footer text is snapshotted and printed on the PDF. On-screen preview does not show them. Email body templates are stored and unused. | Low–medium | YES (gaps only) |
| 4 | Theme toggle | Control is inside the user menu, not beside the user chip. | Low (UX) | YES |
| 5 | Sage nominal codes | Tenant-specific master data with no starter rows. Contracts and Sage export depend on them. | High (setup blocker) | YES |
| 6 | Default sorting | Residents (and similar directories that should show recent records) are not newest-first. Document and reference lists already use a deliberate order. | Medium | YES (selective) |
| 7 | Credit notes | Balance math is correct. Income reports, Sage export, partial credit UI, void, and invoice pinning are not. | High | YES |
| 8 | Reports | All nine reports run real queries. Income figures ignore credit notes. Several API filters are not on the screen. | High | YES |
| 9 | Miscellaneous charges | Import → billing → invoice line → Sage snapshot is wired. No manual entry, no cancel, and credits do not unbill the charge. | Medium | YES (gaps) |

---

## Issue 1 — Duplicate Add Buttons

**Classification:** inconsistent UX. Confirmed by template structure. Desktop and mobile lists do not each render their own Add button. The mobile card list repeats row actions only.

**Intended pattern, based on the rest of the app:** one primary action in the page header, on every state. When there are rows, show the table. When there are no rows, show the empty-state message and keep the header button. Filter bars should keep search and “clear” only. Empty states should keep messages and “clear filters” only.

That matches companies, invoice templates, nominal codes, categories, and funding authorities once they have rows: the only Add button is in the header. The empty-state copy of that button is the extra control.

### Root cause

`app-page-header` actions are not hidden when the list is empty. `app-empty-state` projects a second button through `<ng-content>`. On three operational lists, `app-filter-bar` `filterActions` adds a third copy that is visible whenever rows exist. `EmptyStateComponent` (`frontend/care-home-web/src/app/shared/ui/empty-state.ts`) does not create a button itself.

### Affected pages

| Page | Route | Button 1 source | Button 2 source | When duplication happens |
|---|---|---|---|---|
| Residents | `/clients` | `client-list.html` page header “Add resident” | Same file, empty state “Add resident” | Empty list, no active filters, user can write |
| Residents | `/clients` | Page header “Add resident” | Filter bar `filterActions` “Add resident” | List has rows |
| Care homes | `/care-homes` and `/companies/:id/care-homes` | `care-home-list.html` page header | Empty state “Add care home” / “Add Care Home” | Empty list with no search |
| Care homes | same | Page header | Filter bar `filterActions` | List has rows |
| Companies | `/companies` | `company-list.html` page header “Add company” | Empty state “Add company” | Empty list and no search. Not duplicated once rows exist |
| Users | `/users` | `user-list.html` page header “Add user” | Filter bar `filterActions` | List has rows |
| Users | `/users` | Page header | Empty state “Add user” | Empty list |
| Invoice templates | `/invoice-templates` | `invoice-template-list.html` header “Add template” | Empty state | Empty list only |
| Nominal codes | `/nominal-codes` | `nominal-code-list.html` header “Add nominal code” | Empty state | Empty list only |
| Invoice categories | `/invoice-categories` | `invoice-category-list.html` header “Add category” | Empty state | Empty list only |
| Funding authorities | `/funding-authorities` | `funding-authority-list.html` header “Add authority” | Empty state | Empty list only |
| Resident profile | `/clients/:id` (Funding tab, no contracts) | Summary card “Add funding contract” in `client-profile.html` | Funding-tab empty state, same label | Both are on the page when the Funding tab is open and there are no contracts |

### Not the same bug

- Invoices (`/invoices`) put “Start billing” only in the empty state. The header does not repeat it.
- Miscellaneous charges, payments, receivables, audit, and reports do not duplicate a create action.
- Platform organisations (`/platform/tenants`) have a header button only.
- Dashboard and care-home dashboard empty states are section calls to action. The dashboard header “Start billing” can sit above a widget that also says “Start billing”. Treat that as the same pattern if those widgets are in view, not as a second list-page bug.
- Label inconsistency on care homes: header/empty state say “Add Care Home”; the filter bar says “Add care home”.

### Recommended fix

Remove the Add/Create control from empty states and from filter toolbars on the pages in the table. Leave the page-header button. Leave “Clear” / “Clear filters” / “Back” in the empty state. On the resident profile, keep the Funding-tab empty-state button and remove the duplicate from the always-visible summary card when the tab already offers it.

Files: the HTML templates named above. No shared list component is injecting the second button.

---

## Issue 2 — Optional Email Validation

**Classification:** confirmed bug for optional contact emails.

**Decision: Option A.** Email stays optional. Blank must become `null`, and format validation runs only when a value is present.

That matches current usage:

- Resident email is labelled “Email address (optional)” and is not a login.
- Care home email, manager email, funding-authority email, organisation email, and “email from address” are nullable and are not marked required.
- Invoice recipient email falls back from the template contact email to the funding authority email, and sending is skipped when it is blank (`InvoicesController.TrySendInvoiceAsync`).
- Columns are nullable (`nvarchar`, `nullable: true`) on clients, care homes, and funding authorities.
- DTOs use `string?` and do not use `[Required]` on these fields.
- User login, user create, and organisation administrator email are genuinely required and should stay required.

Invoice template contact email is already Option A on the client (`contactEmail: raw.contactEmail || null`) and the template DTO has no `[EmailAddress]`, so a blank template contact email does not hit this bug.

### What each value does

| Input | Angular `Validators.email` | API `[EmailAddress]` | Stored value if it got past validation |
|---|---|---|---|
| `""` | Valid (empty is skipped) | Invalid. Model state fails before the controller. Typical message: “The Email field is not a valid e-mail address.” | Not stored |
| `null` or omitted | Valid | Valid (`EmailAddressAttribute` returns true for null) | Null |
| Valid address | Valid | Valid | Trimmed string |
| Invalid address | Invalid, form does not submit | Invalid if posted directly | Not stored |

`nonNullable` form groups always post `""` for an untouched optional email. Controllers that do `request.Email?.Trim()` never see the request, because validation fails first.

### Affected screens and APIs

| Screen | Route | Frontend | API | Optional in UI / model? |
|---|---|---|---|---|
| Resident create/edit | `/clients/new`, `/clients/:id/edit` | `client-form.ts` posts `email: value.email`. Validator is `Validators.email` without `required`. Label says optional. | `CreateClientRequest` / `UpdateClientRequest` `[EmailAddress]` on `string? Email`. `POST/PUT /api/clients` | Yes |
| Care home create/edit | `/care-homes/new`, `/care-homes/:id/edit` | `care-home-form.ts` posts `email` and `managerEmail` as raw strings. Both use `Validators.email` only. Labels do not say optional, and the controls are not required. | `CreateCareHomeRequest` / `UpdateCareHomeRequest` `[EmailAddress]` on `Email` and `ManagerEmail` | Yes (nullable, not required) |
| Funding authority create/edit | `/funding-authorities/new`, `/funding-authorities/:id/edit` | `funding-authority-form.ts` posts `email: value.email` | `CreateFundingAuthorityRequest` / `UpdateFundingAuthorityRequest` `[EmailAddress]` | Yes |
| Organisation settings | `/settings/organisation` | `organisation-settings.ts` `PUT`s `form.getRawValue()`, so blank `email` and `emailFromAddress` are `""` | `UpdateOrganisationSettingsRequest` `[EmailAddress]` on `Email` and `EmailFromAddress`. `PUT /api/settings/organisation` | Yes |
| Add organisation (platform) | `/platform/tenants/new` | Organisation `email` has no email validator and is posted as `""`. Admin email is required. | `CreateTenantRequest.Email` has **no** `[EmailAddress]`. Provisioning uses `NullIfEmpty`. Admin email is validated separately. | Organisation email is optional and this path likely accepts blank. Admin email must stay required. |
| Invoice template contact email | `/invoice-templates/new`, edit | Empty becomes `null` | No `[EmailAddress]` | Optional, already safe |
| Users | `/users/new` | `Validators.required` + `Validators.email` | Identity user email required | Mandatory. Leave it. |
| Login | `/login` | Required | Authentication | Mandatory. Leave it. |

### Root cause

Optional fields are initialised to `""`. The client treats that as “no email”. `[EmailAddress]` treats `""` as a present, invalid address. There is no shared “blank means null” mapper.

### Recommended fix

On each optional payload, send `null` when the trimmed value is empty (the invoice-template form already does this). On the API, treat null and whitespace as null before `[EmailAddress]` runs (custom attribute, or clear the value in a filter). Do not mark these fields required.

Database nullability is already correct. No migration is required for the bug itself.

---

## Issue 3 — Invoice Template Header/Footer

**Classification:** header and footer text are properly wired into generated invoices and PDFs. Several neighbouring template fields are only partly wired.

### Storage

`InvoiceTemplate` (`backend/CareHome.Data/Models/InvoiceTemplate.cs`) is tenant-owned (`TenantId`).

| Field | Editable in the form? | Used later? |
|---|---|---|
| `HeaderText1` | Yes. Label “Header (optional)”. `invoice-template-form.html` | Snapshotted and printed |
| `HeaderText2` | No control. API and PDF support it | Only if set outside this form |
| `FooterText` | Yes. Label “Footer (optional)” | Snapshotted and printed |
| Bank account, sort code, account number | Yes | Snapshotted and printed when present |
| Contact name, email, phone | Contact name and email yes. Job title / phone not in the form section reviewed | Snapshotted; contact line printed in the PDF footer |
| `EmailSubjectTemplate`, `EmailBodyTemplate` | Yes, with defaults | Saved on the template only. Send does not read them |
| `CompanyLogoPath`, `AuthorityLogoPath` | Not on this form | PDF reads the **current** template/care-home/tenant path at render time, not a snapshot |

Templates are chosen per invoice category, with optional authority, care home, and company scope (`BillingService` template resolution).

### Generation and snapshot

`BillingService` copies the template onto the invoice at generate time:

- `SnapshotHeaderText1`, `SnapshotHeaderText2`, `SnapshotFooterText`
- bank and contact snapshot fields
- `InvoiceTemplateId` (live link, used for logos)

Changing the template afterwards does not change header/footer text on invoices already generated. Those strings live on the invoice row.

Logos are the exception: `InvoicePdfService.GetOrCreateInvoicePdfAsync` reads logo bytes from the current template, care home, or tenant path every time it renders. Invoice PDFs are regenerated on download and email; they are not served from a frozen file the way credit-note PDFs are (`GetOrCreateCreditNotePdfAsync` returns the stored file when `PdfPath` is set).

### Where header and footer appear

| Area | Header used? | Footer used? | Correct? | Notes |
|---|---|---|---|---|
| Template configuration | Yes (`HeaderText1`, `FooterText`) | Yes | Partial | `HeaderText2` is not editable. Logos are not editable here |
| Billing preview | No | No | Expected for this screen | Preview is charge calculation, not a document layout |
| Invoice detail page | No | No | Gap | `invoice-detail.html` does not render snapshot header/footer |
| PDF download | Yes, from invoice snapshot | Yes, in `page.Footer()` | Yes for text | `InvoicePdfService.ComposeInvoiceHeader` and the footer block |
| Email | Yes, inside the attached PDF | Yes, inside the attached PDF | Partial | `InvoicesController` sends a hardcoded subject and body: “Invoice {number}” / “Please find invoice {number} attached.” Template email subject and body are ignored. Recipient is `RecipientEmail` (template contact, else authority email) |
| Default dev/empty-tenant templates | `HeaderText1` set (“Care Home Invoice” / “Miscellaneous Charges”) | General-care template has “Thank you for your payment.” Misc template has no footer | Yes, when that seeder runs | `IdentitySeeder.DevelopmentMasterDataSeeder` and `EmptyTenantMasterDataSeeder`. Neither runs for a normal new tenant |
| Credit note PDF | No template header | No template footer | Different document | Simple layout in `RenderCreditNote`. Company name plus “Credit Note” only |

### What the template is for

It is the billing document setup for a category: header/footer copy, bank details, contact, and (intended) email wording. Billing copies the text and bank details onto the invoice so later template edits do not rewrite history. It is not a full layout engine, and the on-screen invoice is not a preview of that layout.

### Related gap

Email subject and body templates are stored and never applied. That is missing behaviour beside the header/footer path, which itself is used.

---

## Issue 4 — Theme Toggle Position

**Classification:** UI placement change. Current behaviour works; it is in the wrong place.

### Current implementation

- State: `ThemeService` in `frontend/care-home-web/src/app/shared/ui/theme.service.ts`.
- Colour mode (`light` / `dark`) is stored in `localStorage` under `carehome.colorMode` and applied as `data-app-color-mode` on `<html>`.
- `toggleColorMode()` already exists.
- Accent colour (green/blue/teal/purple/slate) is separate and is set from care-home portal settings, not from this control.
- Shell: `frontend/care-home-web/src/app/app.html` header `.app-toolbar`.
- The only light/dark control is inside `mat-menu` `#userMenu`, under the label “Appearance”, after Sign out. `app.ts` `setColorMode()` calls the service.
- There is no standalone theme-toggle component.

**Current location:** inside the user menu, below Sign out.

**Desired location:** in `.app-toolbar`, immediately before the user chip: `[theme icon] [display name / role]`.

**Header component:** inline header in `app.html` (not a separate header component).

**Theme component:** none. `ThemeService` plus two menu items.

### Files that would change

- `frontend/care-home-web/src/app/app.html` — add an icon button before `.user-chip`; remove the Appearance block from the menu so there is one control.
- `frontend/care-home-web/src/app/app.ts` — call `themeService.toggleColorMode()` (already has `setColorMode`).
- `frontend/care-home-web/src/app/app.scss` — spacing so the icon sits in the existing toolbar gap. `.user-meta` is already `display: none` below 480px, so the chip collapses to avatar + caret on small screens. An icon button in the toolbar stays visible.

### Responsive note

Keep it a toolbar icon button with an accessible name (“Switch to dark mode” / “Switch to light mode”). Do not put it only inside the menu. The toolbar is sticky and present on mobile. No sidenav change is required.

---

## Issue 5 — Sage Nominal Codes

**Classification:** missing reference data. The feature is implemented. New tenants are not given codes.

### Existing implementation

| Question | Answer |
|---|---|
| Table / entity | `NominalCodes` / `NominalCode` (`backend/CareHome.Data/Models/NominalCode.cs`) |
| Scope | Tenant-specific. `TenantId`, unique index `(TenantId, Code)` |
| Fields | `Code` (20), `Name` (150), `Description` (optional), `IsActive`. No type/category column |
| Seed today | None in `TenantProvisioningService`. Development and empty-tenant seeders create categories, authorities, and templates, not nominal codes. Demo script tells the operator to type `4000` / `Care income` by hand |
| If none exist | Dashboard setup hint: “Add nominal codes for Sage posting.” Funding contract save fails with “Nominal code was not found”. Misc billing skips a charge with `MISSING_NOMINAL` when the row has no code. Sage preview marks a line ineligible when `SnapshotNominalCode` is blank |

### Where they are used

- Funding contract: required `NominalCodeId` (`FundingContractService`).
- Billing copies the contract nominal onto the invoice line snapshot (`SnapshotNominalCode`, `SnapshotNominalCodeName`).
- Miscellaneous charge CSV may include a nominal code. Import rejects unknown codes. Billing requires a code, and falls back to the contract nominal id if the charge has a code string but no id.
- Invoice PDF prints the snapshotted code.
- Sage 50 CSV column `NominalCode` comes from the line snapshot (`Sage50ColumnMap`, `docs/SAGE50_EXPORT.md`). Export is invoices only. Credit notes are not exported.
- Screens: `/nominal-codes`, contract form dropdown `GET /api/nominal-codes?activeOnly=true`.

There is no nominal type enum. VAT is a placeholder tax code `T0` on the Sage file, not a nominal.

### Recommended seed strategy

Match invoice categories: **insert per tenant when the tenant is created**, in `TenantProvisioningService`, inside the same transaction as `DefaultInvoiceCategories`.

Do not use a global table. Do not use EF `HasData` in a migration (tenants do not exist at migration design time). A migration that inserts rows for existing tenants is reasonable as a one-off backfill for organisations that currently have zero nominal codes. Do not overwrite codes a tenant has already created. Skip when `(TenantId, Code)` already exists.

`EmptyTenantMasterDataSeeder` is demo data behind a flag. It is the wrong place for required accounting reference data.

### Proposed initial codes

Only income codes that match the four system invoice categories. The Sage file posts the line nominal with tax code `T0`. The app does not post debtors, VAT, or bank control accounts, so those codes should not be seeded.

| Code | Name | Usage |
|---|---|---|
| 4000 | Care income | Default for General Care contracts and Sage sales lines. Matches the demo script |
| 4001 | Outreach income | Out-Reach Services Invoice category |
| 4002 | Rent income | Rent Invoice category |
| 4003 | Miscellaneous income | Miscellaneous Invoice category and misc-charge imports |

These are starter codes, not a full Sage chart. Operators can add or deactivate others. Deactivating a code that is already on a contract does not rewrite invoice snapshots.

---

## Issue 6 — Default Sorting

**Classification:** confirmed inconsistency on resident lists. Several other orders are deliberate and should stay.

Lists render API order. The frontend does not re-sort these collections.

Residents, companies, care homes, users, categories, nominal codes, templates, and funding authorities have **no `CreatedAt`**. “Newest first” for those tables means `Id DESC` unless a `CreatedAt` column is added. Invoices, credit notes, contracts, rates, payments, and misc charges do have `CreatedAt`.

### Matrix

| Section | Current sort | Expected sort | Backend / frontend | Change needed? |
|---|---|---|---|---|
| Residents `/clients` | `FirstName`, then `LastName` (`ClientsController`) | Newest first. No `CreatedAt`; use `Id DESC` or add `CreatedAt` | Backend | YES |
| Companies `/companies` | `Name` | Name (directory) | Backend | NO |
| Care homes `/care-homes` | `Name` | Name (directory) | Backend | NO |
| Funding authorities | `Name` | Name (lookup) | Backend | NO |
| Invoice categories | `Name` | Name (lookup) | Backend | NO |
| Nominal codes | `Name` | Name or code (lookup). Code order is slightly more natural for a chart | Backend | NO for “newest”. Optional if finance wants code order |
| Invoice templates | `Name` | Name (lookup) | Backend | NO |
| Funding contracts (service list) | `ContractStartDate DESC` | Contract start, not insert time | Backend | NO |
| Funding rates | `EffectiveFrom` ascending on history; latest rate chosen with `EffectiveFrom DESC` | Chronological rate history | Backend | NO |
| Invoices `/invoices` | `InvoiceDate DESC`, then `Id DESC` | Document date descending. Invoice date can differ from `CreatedAt` when billing sets the date | Backend | NO |
| Credit notes `/credit-notes` | `CreditNoteDate DESC` | Document date descending. UI sets this date from the period end | Backend | NO |
| Users `/users` | `Email` | Directory order. No `CreatedAt` on the user | Backend | NO |
| Audit `/audit` | `LoggedAt DESC` | Newest first | Backend | NO |
| Misc import history | `ImportedAt DESC` | Newest batch first | Backend | NO |
| Payments | `ReceivedDate DESC` | Receipt date | Backend | NO |
| Receivables | `DueDate DESC`, then outstanding | Collections work queue | Backend | NO |
| Disputes | `OpenedDate DESC` | Newest dispute first | Backend | NO |
| Renewals | `CurrentEndDate` ascending | Soonest renewal first | Backend | NO |
| Sage export history | `ExportedAt DESC` | Newest export first | Backend | NO |
| Bank transactions | `TransactionDate DESC` | Statement order | Backend | NO |
| Platform organisations | `Name` | Name (lookup) | Backend | NO |
| Dashboard recent invoices | `GeneratedAt DESC` | Newest generated | Backend | NO |

Report queries are separate. Resident census is ordered by home then name (correct). Current rates, invoices-by-resident, invoices-by-care-home, income-by-category, and occupancy have **no `OrderBy`**. Result order is not guaranteed. That is a report stability gap, not a list-page default.

---

## Issue 7 — Credit Notes

**Classification:** mixed. Receivable maths and the “cannot exceed remaining” rule are correct. Reporting, Sage, void, partial entry, and invoice pinning are incomplete or inconsistent.

### Lifecycle

1. Invoice detail links to `/credit-notes` with query params (`invoiceId`, invoice number, first line’s resident, period). The banner shows that context.
2. `CreditNoteWorkspacePage.body()` posts `clientId`, period, reason, and `creditNoteDate` = period end. It does **not** post `invoiceId` or `lineAmounts`.
3. `POST /api/credit-notes/preview` and `POST /api/credit-notes/generate` (`CreditNotesController`, `CreditNoteService`).
4. Eligible lines are invoice lines in the tenant whose **service period overlaps** the requested period, excluding void invoices, optionally filtered by resident, authority, and category, and by the user’s care-home scope.
5. If the match spans more than one invoice, generate is blocked.
6. The note is stored against `InvoiceId`, status `Generated`, lines with **negative** amounts, `TotalAmount` = sum of those negatives.
7. PDF: `GET /api/credit-notes/{id}/pdf`. Email: `POST /api/credit-notes/{id}/send` (no button in the workspace).
8. Outstanding balance is computed at read time. Invoice `Status` is not changed to a “credited” status.

There is no standalone credit note. `InvoiceId` is required. `CreditNoteStatuses` contains `Generated` and `Void`, and every balance query ignores `Void`, but **no void endpoint or button exists**.

### Invoice integration

| Question | Behaviour |
|---|---|
| Only against an invoice? | Yes |
| Standalone credit notes? | No |
| Amount greater than the invoice line? | Rejected. Remaining = line amount + sum of non-void credit line amounts (credits are negative) |
| Partial credit? | API: `LineAmounts` dictionary. UI: not sent. Preview text says partial “may require backend support”, which is inaccurate. The UI always credits the full remaining amount of every overlapping line |
| Full credit? | Yes, by default |
| Outstanding balance? | Yes. `ReceivableBalance` is gross − credits − allocated payments. Invoice list/detail outstanding comes from `InvoiceReceivableReadModel` |
| Invoice status? | Stays `Generated` or `Sent`. Payment status is not rewritten. A fully credited unpaid invoice can still show `NotPaid` while outstanding is 0 |
| Already credited amounts? | Yes, included in remaining |
| Duplicate credits? | A second credit of the same remaining balance is rejected. A tenant-wide SQL app lock serialises generate |
| Cancel / void the credit note? | Not implemented. Status value exists and is honoured by readers |
| Closed accounting periods? | No period lock in this codebase |
| Tenant and home scope? | List, get, PDF, and send check tenant and care-home access. Generate uses the same home filter when loading lines |
| Void the invoice after a credit? | Blocked by `InvoiceVoidRules` if any credit notes exist, including if a void status were ever written |

### Accounting

- Nominal codes and VAT are not stored on `CreditNote` or `CreditNoteLine`. A credit does not create an accounting line. Sage export (`SageExportService`) loads invoices only.
- Reports that sum `InvoiceLines.LineAmount` ignore credit notes (see Issue 8). Outstanding and receivables do not.
- Crediting a miscellaneous-charge line does not set `MiscCharge.IsInvoiced` back to false. Voiding the invoice does.

### UI inconsistencies

- One workspace (`credit-note-workspace.html`) combines create and list. Invoices have separate list and detail pages.
- Preview amounts are positive. Stored `TotalAmount` and PDF line amounts are negative (`0.00` format, not `CurrencyDisplay`).
- List has PDF download. Send exists on the API and is not in the list UI. Invoices expose send from the detail page.
- No credit-note detail route. No void, no edit, no delete.
- Empty list is an empty state inside the same page as the create form, which is reasonable.
- Date on the PDF is `yyyy-MM-dd`. Invoice PDFs use `d MMM yyyy`.
- Deep link shows “Original invoice” but selection is by period overlap. If the user changes the dates, the banner can still name the original invoice while preview selects another single overlapping invoice. `invoiceId` is never sent.

### Confirmed bugs / missing behaviour

| Item | Class |
|---|---|
| Income and invoice activity reports ignore credits | Confirmed functional gap (wrong revenue if read as net) |
| Sage export ignores credits | Confirmed functional gap |
| UI cannot enter a partial amount even though the API can | Missing UI. The on-screen warning is wrong |
| Credit started from an invoice is not constrained to that invoice | Confirmed wiring gap / potential wrong document |
| Cannot void a credit note although `Void` is part of the model | Missing behaviour |
| No nominal on the credit note | Missing accounting representation |
| Negative total on the list versus positive preview | UI inconsistency |
| No closed-period rule | Not built. Do not invent one without a business rule |
| Balance reduction and over-credit prevention | Correct |

### Files

- `frontend/care-home-web/src/app/features/credit-notes/pages/credit-note-workspace/`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-detail/invoice-detail.ts` (`creditNoteQueryParams`)
- `backend/CareHome.Billing/Billing/CreditNoteService.cs`
- `backend/CareHome.Api/Controllers/CreditNotesController.cs`
- `backend/CareHome.Api/Documents/InvoicePdfService.cs` (`RenderCreditNote`)
- `backend/CareHome.Receivables/Domain/ReceivableBalance.cs`
- `backend/CareHome.Api/Services/InvoiceReceivableReadModel.cs`
- `backend/CareHome.Data/Models/CreditNote.cs`, `CreditNoteLine.cs`
- `backend/CareHome.Billing/Billing/InvoiceVoidRules.cs`

---

## Issue 8 — Reports

**Classification:** fully wired queries with incomplete filters and one systematic credit-note omission. None of the nine are placeholders.

Screen: `/reports` (`reports.html`, `reports.ts`). API: `GET /api/reports/{name}` (`ReportsController`). Policy: `CanViewFinancialReports`. Tenant comes from the request context. Care-home scope is applied in `ReportService.AllowedHomes` for reports that load homes.

The screen always sends only `from` and `to`. It does not send `companyId`, `careHomeId`, `clientId`, `clientStatus`, `fundingAuthorityId`, `categoryId`, or `contractId`, even when the API accepts them. CSV, Excel, and PDF export use the same query. PDF export is a text dump, not the invoice PDF.

Date filters on invoice reports use **invoice date**, not service period and not `CreatedAt`.

### Credit-note treatment

| Report | Credits |
|---|---|
| Invoices by resident | Ignored. Row amount is `InvoiceLine.LineAmount` |
| Invoices by care home | Ignored. Same rows, then filtered by care-home **name** |
| Income by category | Ignored. Sum of line amounts |
| Outstanding | Subtracted. Amount is receivable outstanding (gross − credits − payments) |
| All other reports | Not financial documents; credits do not apply |

Void invoices are excluded from the three invoice/income queries. Outstanding excludes void unless a document-status filter asks for it.

### Report catalogue

#### Resident census — `client-census`

| | |
|---|---|
| Purpose | Who is in each home right now |
| Who | Operations / admissions |
| Source | `Clients` joined to care home. Excludes archived |
| Filters | API: company, care home, plus home access. UI: none of those. Dates are ignored |
| Calculation | One row per resident. No money |
| Output | Name, reference, home, status, care type, admission date. Ordered by home, then name |
| Wiring | Fully wired |
| Verify | 1. Add Resident A at Home 1, status Current. 2. Archive or omit Resident B. 3. Run census. 4. A appears. B does not. Changing From/To does not change the rows |

#### Current rates — `current-rates`

| | |
|---|---|
| Purpose | The rate in force today on active contracts |
| Who | Finance checking what will be billed |
| Source | `FundingRates` → contract → resident, authority, category |
| Filters | API: company, home, resident status, authority, category. UI: none. Dates ignored |
| Calculation | `EffectiveFrom <= today` and (`EffectiveTo` null or `>= today`) and contract `Active`. Amount is the rate amount, not a billed total. **No OrderBy** |
| Wiring | Fully wired query; screen filters missing |
| Verify | 1. Contract rate £100 weekly from 1 Jan, no end. 2. Add a later rate £120 from 1 Jun. 3. Run on a date in June. 4. Expect £120, not both, for that contract |

#### Invoices by resident — `invoices-by-client`

| | |
|---|---|
| Purpose | Invoice **lines** in a date range. The name says invoices; the query is lines |
| Who | Finance reviewing what was billed |
| Source | `InvoiceLines` → invoice. Status not `Void` |
| Filters | API: `clientId`, `from`, `to` on **invoice date**, home access. UI sends dates only, so it is all residents |
| Calculation | `Amount` = `LineAmount`. Payment status is the stored invoice flag, not outstanding after credits. Credits ignored. No OrderBy. UI column list mentions period start/end; the DTO does not include them, so those columns never appear |
| Wiring | Fully wired, likely misleading as “net income” |
| Verify | 1. Invoice line £1,000 dated in range. 2. Credit note £200 against it. 3. Run with From/To covering the invoice date. 4. Expect a £1,000 row, not £800. Outstanding (below) is the report that shows the net |

#### Invoices by care home — `invoices-by-care-home`

| | |
|---|---|
| Purpose | Same line rows, optionally one home |
| Source | Reuses invoices-by-resident, then filters `CareHomeName ==` the home’s current name |
| Filters | API: `careHomeId`, dates. UI: dates only, so every home |
| Calculation | Same gross line amounts. Name match can drop or mis-file a row if the home was renamed after the snapshot |
| Wiring | Partial: filter not on the screen; name match is fragile |
| Verify | Same £1,000 / £200 case. Without a home filter, the £1,000 line still appears. Net is not £800 |

#### Income by category — `income-by-category`

| | |
|---|---|
| Purpose | Billed line totals grouped by snapshotted category name |
| Who | Finance, if they treat this as revenue |
| Source | `InvoiceLines` in range, invoice date, not void |
| Filters | `from` and `to` required. No tenant leak: tenant and allowed homes are applied |
| Calculation | `Sum(LineAmount)` per `SnapshotInvoiceCategoryName`. Credits ignored. Misc lines count under the miscellaneous category name |
| Wiring | Fully wired and **likely inaccurate as net revenue** |
| Verify | 1. General Care line £1,000. 2. Credit £200. 3. Misc line £50 in the same invoice-date range. 4. Expect General Care £1,000 and Miscellaneous £50. Do not expect £800 |

#### Occupancy / availability — `occupancy`

| | |
|---|---|
| Purpose | Beds versus current residents |
| Who | Operations |
| Source | `CareHomes.BedCapacity` and clients with status `Current` and not archived |
| Filters | API: `companyId`. UI ignores it. Dates ignored |
| Calculation | `AvailableBeds = Capacity - CurrentClients` |
| Wiring | Fully wired |
| Verify | Home capacity 24, one Current resident. Expect current 1, available 23. A Left or archived resident does not count |

#### Funding rate history — `rate-history`

| | |
|---|---|
| Purpose | Audit of rate rows |
| Source | `FundingRates` for allowed homes |
| Filters | API: `contractId`. UI does not send it, so all visible contracts. Ordered by `EffectiveFrom` |
| Calculation | No “current” filter. Every historical row |
| Wiring | Fully wired; cannot pick one contract from the screen |
| Verify | Two rates on one contract (£100 then £120). Report shows both, earlier date first |

#### Billing exceptions — `billing-exceptions`

| | |
|---|---|
| Purpose | Why preview/generate skipped someone |
| Source | `BillingExceptionLogs`, latest 500, tenant and home scope |
| Filters | None beyond scope. Dates ignored. Ordered by `LoggedAt DESC` |
| Wiring | Fully wired |
| Verify | Run billing for a resident with no contract. Exception log row appears with the billing code and message |

#### Payment status / outstanding — `outstanding`

| | |
|---|---|
| Purpose | Open receivables |
| Who | Credit control |
| Source | `ReceivablesService.ListInvoicesAsync` with `OpenReceivablesOnly`, page size 10,000 |
| Filters | None on this action. Dates ignored. Ordered by due date |
| Calculation | `Amount` = outstanding after credits and allocated payments. Paid-in-full and zero-outstanding rows drop out. `IsDue` is true when days overdue > 0 |
| Wiring | Fully wired. This is the report that understands credits |
| Verify | 1. Invoice £1,000, unpaid. 2. Credit £200. 3. Run outstanding. 4. Expect £800, not £1,000. 5. Allocate a £800 payment. 6. The invoice disappears from this report |

### Summary

| Report | Wiring | Accuracy |
|---|---|---|
| Resident census | Full | Sound for a point-in-time list |
| Current rates | Full query, filters hidden | Sound for “rate today” |
| Invoices by resident | Full | Gross lines, not net of credits; not one row per invoice |
| Invoices by care home | Partial filters; name match | Same gross-line limitation |
| Income by category | Full | Gross billed, credits omitted |
| Occupancy | Full | Sound |
| Rate history | Full query, contract filter hidden | Sound |
| Billing exceptions | Full | Sound, capped at 500 |
| Outstanding | Full | Sound, including credits |

Impossible to call a report “wrong” where the business has not defined net versus gross. The outstanding report shows the system already knows how to net credits. Income and invoice activity reports do not use that calculation. That split should be treated as a defect if those reports are used as revenue.

---

## Issue 9 — Miscellaneous Charges

**Classification:** partially wired feature that **is** connected to invoice generation. It is not a placeholder.

### Business purpose

Ad-hoc amounts imported from CSV, then picked up by billing as invoice lines. The screen says: resident reference, used date, description, amount, nominal code. Examples in the product are “other chargeable items” under the `MISC` invoice category, not a separate catalogue of transport or hairdressing.

There is no manual “add one charge” form. Creation is CSV preview then confirm (`MiscChargeImportService`, `/misc-charges`).

### Data model — `MiscCharge`

| Field | Role |
|---|---|
| `TenantId` | Tenant isolation |
| `ImportBatchId` | Parent `MiscChargeImportBatch` (file name, `ImportedAt`, accepted rows) |
| `ClientId`, `ClientReference` | Resident |
| `UsedDate` | Service date used for billing selection |
| `Description`, `Amount` | Invoice line text and amount |
| `NominalCodeId`, `NominalCodeValue` | Sage nominal. Id resolved at import when the code exists |
| `SourceRowNumber` | CSV row |
| `IsInvoiced` | Billed flag. This is the status. There is no Pending / Billed / Cancelled enum |
| `CreatedAt` | Insert time |

Unique index: `(TenantId, ClientId, UsedDate, Description, Amount)`. The same row cannot be imported twice.

Import writes an audit row `MiscChargeImport`.

### Billing integration

`BillingService.AddUnbilledMiscChargesAsync` runs when the preview category is omitted or is the `MISC` category.

Selection:

- same tenant
- `IsInvoiced == false`
- `UsedDate` inside the billing period
- resident’s company matches the billing company
- resident not archived
- optional care home and client ids
- user’s allowed homes

Each charge becomes a preview line: category Miscellaneous, frequency `AdHoc`, amount = charge amount, `MiscChargeId` set, nominal from the charge or the contract.

It will not bill when:

- nominal code is missing (`MISSING_NOMINAL`)
- no funding authority exists (`MISSING_CONTRACT`)
- no funding contract exists to hang the line on
- `MISC` category is missing
- template is missing (warning; line can still be added with a null template id, and generate later fails closed if the group has no template)

On successful generate, `IsInvoiced = true`. The invoice line stores `MiscChargeId`.

Double billing: the flag blocks a second pick-up. Voiding the invoice sets `IsInvoiced = false` for those charges so they can be billed again (`InvoicesController.Void`).

### Integration matrix

| Area | Wired? | Evidence | Problem |
|---|---|---|---|
| Client | Partial | Charge stores `ClientId`. No list of charges on the resident profile | Operators only see import batches, not per-resident charges |
| Billing preview | Yes | `AddUnbilledMiscChargesAsync` | Needs nominal, authority, and a contract. Easy to look “unwired” when those are missing |
| Invoice | Yes | Line `MiscChargeId`, description, amount, nominal snapshot | Line is hung on a funding contract even though the charge is ad hoc |
| Invoice PDF | Yes | Normal invoice line table, including nominal | No special misc layout. Template header/footer still apply |
| Credit note | Partial | Any overlapping invoice line can be credited, including misc | Credit does not clear `IsInvoiced`, so the charge is not billed again. Void does clear it |
| Reports | Indirect | Misc lines are ordinary invoice lines under the misc category | No misc-charge report. Income includes them gross. Credits against them are ignored there and applied in Outstanding |
| Sage / accounting | Yes, via the invoice snapshot | Export uses `SnapshotNominalCode`. Import rejects unknown codes | No nominal on the charge means the line never reaches an invoice. Credits are not exported |
| Audit | Partial | Import batch is audited. Per-charge edits do not exist | No cancel audit because cancel does not exist |
| Tenant isolation | Yes | Import, list, and billing filter `TenantId` | Unique key is per tenant |

### Lifecycle that actually exists

Imported (not invoiced) → included in a billing preview for that used date → invoiced (`IsInvoiced`) → optional credit of the invoice line (charge stays invoiced) → or void invoice (charge returns to not invoiced).

There is no cancel status.

---

## Cross-cutting notes

Only items that showed up while tracing the nine issues:

- Care-home Add labels differ (“Add Care Home” vs “Add care home”).
- Credit-note money is negative in storage, PDF, and the list, and positive in the preview.
- Credit-note PDF dates are ISO; invoice PDF dates are “d MMM yyyy”.
- Invoice activity reports show stored payment status, which is not the same as outstanding after credits.
- Invoice email subject/body templates are unused, same class of “saved but not applied” as `HeaderText2`.
- Report screens collect From/To for every report, including census and occupancy, where the API ignores dates.
- Nominal codes, categories, and templates load full lists (no paging). That is existing behaviour, not part of these nine fixes.

---

## Priority fix plan

### P0 — Financial correctness

- Income-by-category and the two invoice activity reports must define and apply credit notes the same way receivables already do, or be clearly gross and paired with a net figure. Today they overstate revenue.
- Sage export omits credit notes, so a file of invoices does not match outstanding or a credited invoice.
- Credit generation from an invoice must target that invoice. Period overlap alone can select a different document.
- Do not treat miscellaneous double-billing as open: `IsInvoiced` plus the unique import key already prevent a second bill. Do not weaken that when changing credits.

### P1 — Core workflow

- Optional email: blank to null on the client and server (Option A).
- Seed the four nominal codes per tenant at creation, and backfill tenants that have none.
- Resident list sort: newest first (`Id DESC` until `CreatedAt` exists).
- Credit-note UI: partial amount entry using `LineAmounts`, remove the incorrect “backend may not support this” copy, and decide whether void is in scope.
- Miscellaneous charges: surface why a row did not bill (missing nominal or contract). A resident-level charge list is useful and is not required to make billing work.

### P2 — UX consistency

- Remove duplicate Add buttons; keep the page-header action.
- Move the theme icon to the left of the user chip.
- Show snapshot header/footer on the invoice page or document that PDF is the only preview.
- Align credit-note amount sign and date format with invoices.
- Wire or hide unused template fields (`HeaderText2`, email subject/body) so the form matches what generate and send actually use.

---

## Proposed implementation phases

### Phase 1 — Financial correctness

Credit notes and reports together, then Sage credit lines, then the invoice-id pin on credit generate.

Miscellaneous billing is already connected. Only change it in this phase if a credit is defined to reverse the charge (today it does not).

Invoice header/footer text does not need a financial fix. Leave PDF behaviour alone unless Phase 4 is changing the template form.

### Phase 2 — Reference data

Nominal code seed in tenant provisioning, plus a backfill that inserts only missing codes. Do this before expecting misc import or new contracts to succeed on a fresh tenant.

### Phase 3 — Form and list consistency

Optional emails, then resident (and only resident) default sort. Do not reorder invoices, credit notes, rates, renewals, or lookups.

### Phase 4 — UI consistency

Duplicate Add buttons, then theme toggle position, then credit-note presentation and template-field cleanup.

This order is safer than starting with UI. Report and Sage changes depend on the credit-note rules. Nominal codes are independent but block new billing setup. Email and sort do not affect money.

---

## Final verdict

### Safe to fix independently

- Duplicate Add buttons (templates only).
- Theme toggle position (`app.html` / `app.ts` / `app.scss`).
- Optional email null handling.
- Resident list `OrderBy`.
- Nominal code seed (new rows only; do not renumber existing contracts).
- Invoice on-screen display of header/footer text (read-only).

### Must fix together

- Credit-note selection rules, income reports, invoices-by-resident, invoices-by-care-home, and Sage export. They all answer “what is revenue after credits?”
- Outstanding does not need a formula change if the others adopt `ReceivableBalance` / credit totals. It is the reference implementation.
- Miscellaneous charges and credit notes only if the business wants a credit to un-bill the charge. Otherwise leave `IsInvoiced` as it is and only fix report/Sage treatment of the invoice line.

### Requires a business decision

- Income reports: gross billed versus net of credit notes. The code currently implements gross and does not say so.
- Whether a credit note can be voided, and whether that restores receivable balance only (the readers already ignore `Void`) or also affects Sage.
- Whether partial credit is required in the UI. The API already allows it.
- Whether crediting a miscellaneous line should allow that charge to be invoiced again.
- Sage nominal code numbers beyond the four income codes. Tax code remains `T0` until VAT is specified (`docs/SAGE50_EXPORT.md`).
- Whether invoices should sort by `CreatedAt` instead of invoice date. Document date is the better default and should stay unless finance wants entry order.
- Whether `HeaderText2`, logos, and email subject/body templates are in scope. Header/footer text already print.

### Requires runtime verification

- Exact validation message string returned to the browser for `email: ""` (expected ASP.NET email message; not executed here).
- A generated PDF actually showing a non-empty snapshot header and footer after billing. The render code is present; a file was not produced in this review.
- Whether a renamed care home drops rows from invoices-by-care-home. The filter compares the live name to `SnapshotCareHomeName`.
- Credit-note email delivery (sender configuration). The API path exists; the workspace has no send button.
- Fresh-tenant behaviour of `Seed:MasterDataForEmptyTenants` (off unless configured). Nominal codes are absent either way.

### Recommended first fix

Pin credit generation to the invoice the user opened, and make income / invoice-activity reports subtract non-void credit notes using the same totals as `InvoiceReceivableReadModel`.

That is the first fix because it is the only change that stops the product showing two different answers for one invoice (£1,000 billed and £200 credited still appears as £1,000 income, while outstanding shows £800). Sage export should follow immediately so the file does not reintroduce the gross figure. Nominal seeds and optional emails are next, because they block data entry, and they do not depend on the credit calculation.

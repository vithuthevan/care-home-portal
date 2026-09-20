# UI ↔ API Field Consistency Audit

**Scope:** Static code trace across Angular frontend (`frontend/care-home-web`) and .NET API (`backend/CareHome.Api`). No code, schema, or configuration was modified.

**Audit date:** 2026-09-16

---

## 1. Executive Summary

| Metric | Count |
|--------|------:|
| Major entities / workflows audited | 17 |
| Total field-level mismatches documented | 42 |
| Genuine functional inconsistencies (UI gap vs persisted/API capability) | 18 |
| Intentional / by-design differences | 14 |
| Requires product/business decision | 6 |
| Unclear / low evidence | 4 |

**Original Care Home `email` observation:** **Not confirmed as a create/edit UI gap.** The Care Home form (`care-home-form.html` / `care-home-form.ts`) includes **Email** and **Manager email**, maps them into the create/update payload, and the API persists them on `CareHomeLocation.Email` / `ManagerEmail`. The more accurate finding is **display consistency**: email (and phone, address) are **not shown** on the care home **list** or **dashboard**, only on the dedicated create/edit form.

**Answer to the core question:** Meaningful user-entered data generally flows **UI → API → database** for master-data forms (care homes, clients, funding authorities, organisation settings). Broken or incomplete links appear mainly in **secondary screens** (list/detail/dashboard), **workflow UIs** that implement only a subset of API capabilities (funding contract edit, user edit, invoice template scoping, reports/sage filters, credit note adjustments), and **logo/asset fields** stored in the database but never exposed in the UI.

---

## 2. Original Care Home Email Finding

| Layer | Email present? |
|-------|----------------|
| Database (`CareHomes.Email`, `ManagerEmail`) | Yes (`CareHomeLocation`) |
| Entity | Yes |
| Request DTO (`CreateCareHomeRequest` / `UpdateCareHomeRequest`) | Yes (optional, `[EmailAddress]`, max 150) |
| Response DTO (`CareHomeDto`) | Yes |
| Frontend model (`care-home.model.ts`) | Yes |
| Create UI | **Yes** (`formControlName="email"`) |
| Edit UI | **Yes** (same form) |
| Detail UI (dashboard) | **No** (manager name only; no email/phone/address) |
| List UI | **No** |
| API payload (create/update) | **Yes** (`email`, `managerEmail` in `save()`) |
| Persisted in controller | **Yes** (`CareHomesController` maps `request.Email`) |

**Explanation:** The reported issue likely came from inspecting **network payloads** or an **older build**, or from expecting email on the **list/dashboard** rather than the form. End-to-end persistence is wired correctly. **Gap type:** `RESPONSE FIELD NOT DISPLAYED` on list and dashboard, not `BACKEND FIELD NOT EXPOSED IN UI` for create/edit.

**Email required?** Optional everywhere (DB nullable, DTO optional, UI no `Validators.required`).

---

## 3. Complete Findings

| Priority | Entity | Field | UI | API | Database | Issue | Risk | Recommendation |
|----------|--------|-------|----|-----|----------|-------|------|----------------|
| P1 | Care Home | email, phone, address, managerEmail | Create/edit only | Returned in GET | Stored | RESPONSE FIELD NOT DISPLAYED on list/dashboard | Users cannot verify contact data without opening Edit | Product: add read-only contact block on dashboard or list |
| P1 | Funding contract | invoiceTemplateId, status, end-date edit | Create-only partial UI; no edit | Create/Update accept | Stored | BACKEND FIELD NOT EXPOSED IN UI; no PUT from UI | Cannot override template per contract; cannot close/edit contracts in UI | Add contract edit dialog or document API-only workflow |
| P1 | Funding contract | invoiceTemplateId | Not sent on create (`client-profile`) | Optional on create | Nullable FK | Relies on billing template resolution at invoice time | Billing may fail if no matching template | Ensure category-default templates exist; or expose template picker |
| P1 | User | role, careHomeIds, displayName, isActive (edit) | Create + deactivate only | PUT `/api/users/{id}` | Identity + `UserCareHomeAccess` | BACKEND FIELD NOT EXPOSED IN UI | Cannot fix role/home assignment without API/admin tools | Add user edit form |
| P1 | User | reset password | No UI | POST `reset-password` | N/A | API capability not in UI | Admins must use external process | Add reset-password action if required for ops |
| P1 | Invoice template | fundingAuthorityId, careHomeId, companyId, headerText2, contactPhone, contactJobTitle, isActive (edit) | Create subset only; no edit page | Full `UpsertInvoiceTemplateRequest` | Stored | BACKEND FIELD NOT EXPOSED IN UI | Cannot create authority/home-specific templates from UI | Extend template form or accept API-only setup |
| P1 | Invoice template | emailSubjectTemplate, emailBodyTemplate | In FormGroup defaults; **not in HTML** | Accepted | Stored | Hidden defaults sent on every create | Users cannot customize email copy in UI | Expose fields or document defaults |
| P1 | Reports | companyId, careHomeId, clientId, fundingAuthorityId, categoryId, clientStatus, contractId | Only `from`/`to` + report type | Many query params on `ReportsController` | N/A | BACKEND FILTERS NOT EXPOSED IN UI | Reports less useful than API allows | Add filters per report type |
| P1 | Sage export | companyId, careHomeId, status, includeAlreadyExported | Date range only | `SageExportRequest` | Batch metadata | BACKEND FIELD NOT EXPOSED IN UI | Exports may be broader than intended | Add optional filters |
| P2 | Client | notes, dischargeDate, dischargeReason | Edit form; not on profile Details tab | Returned | Stored | RESPONSE FIELD NOT DISPLAYED | Incomplete profile view | Show on Details tab |
| P2 | Funding authority | email, phone | Create/edit | Returned | Stored | List shows contactName only | Harder to spot contact info in list | Optional columns |
| P2 | Credit note | fundingAuthorityId, invoiceCategoryId, lineAmounts | Not in workspace | Preview/generate accept | N/A | BACKEND FIELD NOT EXPOSED IN UI | Less control over credit scope/amounts | Product decision |
| P2 | Client list | status, fundingAuthorityId, contractStatus, companyId | Service supports `extra`; list UI does not | Query params on GET clients | N/A | API filters not in UI | Harder to filter large client lists | Add filters if needed |
| P2 | Care Home | logoPath | None | In `CareHomeDto` | `LogoPath` column | DATABASE FIELD NOT EXPOSED | Logos never set via UI | Product: upload or remove field |
| P2 | Organisation / Tenant | logoPath | None | Tenant has field; org settings DTO omits logo | `Tenants.LogoPath` | DATABASE FIELD NOT EXPOSED | Same as above | Product decision |
| P2 | Invoice template | authorityLogoPath, companyLogoPath | None | Not in DTO | DB columns | DATABASE FIELD NOT EXPOSED | PDF branding incomplete if relied on | Product decision |
| P2 | Platform tenant (edit) | adminEmail, adminDisplayName | Still on form; sent on PUT | `UpdateTenantRequest` ignores | N/A | Extra JSON ignored (harmless) | Confusing edit form fields | Hide admin fields in edit mode |
| P3 | Care Home / Client / FA | empty string for optional email/phone | Sends `""` | `?.Trim()` may store `""` | Nullable columns | Minor normalization | Empty string vs null | Optional: coerce empty to null in API or UI |
| P3 | Funding rate | notes | `newRate.notes` always default | API accepts | Stored | No input in profile UI | Notes never captured | Add notes field if needed |
| P3 | Reports UI | column headers | Raw JSON keys | N/A | N/A | Cosmetic | Poor readability | Format column labels |
| P2 | User | email (after create) | Display only | Update does not change email | Identity username | Intentional immutability | Security/account stability | Document as by-design |
| P2 | Client | status on create | Hidden (defaults Current) | Create forces `Current` | Stored | Intentional | Correct lifecycle | Document |
| P2 | Company | contact fields | N/A | N/A | Only `Name` in model | Intentional minimal model | None | N/A |

---

## 4. Email Field Audit

| Entity | Stored | API accepts | UI create | UI edit | UI display | Used (billing/PDF/email) | Consistent? |
|--------|--------|-------------|-----------|---------|------------|---------------------------|-------------|
| Organisation (tenant) | Yes (`Tenants.Email`) | Yes (org settings PUT) | Yes (platform tenant create) | Yes (platform + org settings) | Platform list; org settings form | Indirect (organisation identity) | **Mostly yes** |
| Organisation email-from | Yes (`TenantSettings`) | Yes | Yes (org settings) | Yes | Org settings | Invoice/credit note send (`EmailFromAddress`) | **Yes** |
| Care Home | Yes | Yes create/update | Yes | Yes | List/dashboard **no**; form yes | Not seen in invoice send path in trace | **Create/edit yes; display no** |
| Care Home manager | Yes (`ManagerEmail`) | Yes | Yes | Yes | Dashboard shows manager **name** only | — | **Partial** |
| Client | Yes | Yes create/update | Yes | Yes | Profile Details tab | Not primary invoice recipient in traced code | **Yes** |
| Funding Authority | Yes | Yes | Yes | Yes | List: contact name only | Template/billing context | **Partial display** |
| User (login) | Yes (Identity) | Create only | Yes | **No** (immutable) | User list | Authentication | **By design** |
| Invoice template contact | Yes (`ContactEmail`) | Yes | Yes (create form) | **No edit UI** | List does not show | Invoice email/PDF context | **Partial** |
| Platform admin (provision) | N/A (creates user) | `AdminEmail` on tenant create | Required on create | N/A on tenant update | Notice after create | Welcome email | **Yes for create** |
| SMTP / `EmailOptions` | Config only | N/A | N/A | N/A | N/A | `ConfigurableEmailSender` | **Intentional** (ops config) |

---

## 5. Phone Field Audit

| Entity | Stored | API accepts | UI create | UI edit | UI display | Consistent? |
|--------|--------|-------------|-----------|---------|------------|-------------|
| Organisation | Yes | Yes | Yes | Yes | Org settings | **Yes** |
| Care Home | Yes | Yes | Yes | Yes | Not on list/dashboard | **Partial** |
| Care Home manager | Yes (`ManagerPhone`) | Yes | Yes | Yes | Not displayed | **Partial** |
| Client | Yes | Yes | Yes | Yes | Profile Details | **Yes** |
| Funding Authority | Yes | Yes | Yes | Yes | Not on list | **Partial** |
| Invoice template | Yes (`ContactPhone`) | Yes | **No** in template form | No edit | No | **Gap** |
| User | No dedicated phone | — | — | — | — | N/A |

**Naming:** Single `Phone` / `ManagerPhone` / `ContactPhone` — consistent per entity; no separate Mobile field in schema.

---

## 6. Address Field Audit

| Entity | Representation | UI | API | DB | Consistent? |
|--------|----------------|----|-----|-----|-------------|
| Organisation | Single `Address` textarea (max 300 DB) | Yes | Yes | Yes | **Yes** |
| Care Home | Single `Address` (max 200) | Yes | Yes | Yes | **Yes** (display gap on list/dashboard) |
| Funding Authority | Single `Address` (max 300) | Yes | Yes | Yes | **Yes** |
| Client | **No address fields** | — | — | — | **Consistent absence** |
| Company | **No address** | — | — | — | **By design** |

No structured line1/city/postcode model — all optional free-text where present.

---

## 7. Required Field Consistency

| Entity | Field | UI Required? | API Required? | DB Required? | Consistent? |
|--------|-------|--------------|---------------|--------------|-------------|
| Care Home | code, name, companyId | Yes | Yes (`[Required]`) | Yes | **Yes** |
| Care Home | email | No | No | No | **Yes** |
| Care Home | bedCapacity | Yes (UI); min 0 | Range 0+ | No `[Required]` on entity | **Mostly yes** |
| Client | sageId, referenceNumber, names, careType, admissionDate, careHomeId | Yes | Yes | Yes | **Yes** |
| Client | dischargeDate | UI when status ≠ Current | API when status ≠ Current | No | **Yes** |
| Client | email | No | No | No | **Yes** |
| Company | name | Yes | Yes | Yes | **Yes** |
| Funding Authority | code, name, type, billingFrequency | Yes | Yes | Yes | **Yes** |
| Funding Authority | billingIntervalDays | Yes when CustomDays | Validated server-side | No | **Yes** |
| User | email, displayName, password (create) | Yes | Password required | Identity | **Yes** |
| Organisation settings | name | Yes | Yes (custom check) | Yes | **Yes** |
| Organisation settings | paymentTermsDays, numberLength | UI required; no min/max in UI | 0–365 / 1–10 | Defaults | **Validation gap**: UI lacks API bounds on numberLength/paymentTermsDays |
| Invoice template | name, invoiceCategoryId | Name required; category min(1) | Category must exist | Yes | **Yes** |
| Nominal / invoice category | code, name | Yes | Yes | Yes | **Yes** |

---

## 8. Create vs Edit Consistency

| Entity | Field | Create UI | Edit UI | API Create | API Update | Consistent? |
|--------|-------|-----------|---------|------------|------------|-------------|
| Care Home | isActive | Hidden (always active) | Checkbox | Default true | Yes | **Intentional** |
| Care Home | email, phone, address, manager* | Yes | Yes | Yes | Yes | **Yes** |
| Client | status, discharge*, isArchived | No | Yes | Status fixed Current | Yes | **Intentional** |
| Client | email, phone, notes | Yes | Yes | Yes | Yes | **Yes** |
| Funding Authority | isActive | Hidden | Checkbox | Default true | Yes | **Intentional** |
| Funding contract | all core fields | Partial (profile) | **None** | POST | PUT exists | **No** |
| Funding rate | add only | Yes | **No edit** | POST only | — | **By design?** |
| User | all | Create form | **No edit** | POST | PUT exists | **No** |
| Invoice template | full upsert fields | Subset | **No edit UI** | POST | PUT exists | **No** |
| Company | isActive | Hidden | Checkbox | Default true | Yes | **Intentional** |
| Platform tenant | adminEmail | Required | Shown but unused on PUT | Required on create | Ignored | **Edit mismatch (cosmetic)** |

---

## 9. Enum / Dropdown Consistency

| Entity | Field | Backend Values | UI Values | Difference |
|--------|-------|----------------|-----------|------------|
| Client | careType | Nursing, Residential | Same | **None** |
| Client | status | Current, Left, Deceased | Same (edit only) | **None** |
| Funding Authority | type | NHS, Council, Private, Other | Same | **None** |
| Funding Authority | billingFrequency | Daily, Weekly, Monthly, AdHoc, CustomDays | Same | **None** |
| Funding rate | frequency | Daily, Weekly, Monthly (`RateFrequencies`) | Same | **None** |
| User | role | TenantAdmin, Administrator, LocationManager, ReadOnly | Same | **None** |
| Invoice | paymentStatus | Paid, NotPaid | Same on detail | **None** |
| Invoice | status | (issued, void, etc. — read in UI) | Display badges | N/A |

---

## 10. Response / Display Consistency

Fields **returned by API** but **not shown** where users often look:

- **Care Home:** `email`, `phone`, `address`, `managerPhone`, `managerEmail`, `logoPath` — absent from list and dashboard.
- **Client profile:** `notes`, `dischargeDate`, `dischargeReason`, `isArchived` — on edit form / API only.
- **Funding authority list:** `email`, `phone`, `address` — only `contactName`.
- **Invoice template list:** bank/contact/email template fields — name/category/authority/home/status only.
- **Funding contract list (profile):** `invoiceTemplateId` not shown.

---

## 11. Intentional Differences

| Area | Why it should not be “fixed” without product sign-off |
|------|--------------------------------------------------------|
| **Company** only has `Name` + `IsActive` | Domain model is intentionally minimal; care homes hold location/contact detail. |
| **Client status on create** | API sets `Current`; discharge workflow is edit-only. |
| **User email** not editable via PUT | Username/email stability and security; standard pattern. |
| **TenantId / PublicId / audit fields** | Internal/multi-tenant plumbing. |
| **MustChangePassword** | Driven by auth flow, not CRUD forms. |
| **JWT / password cipher fields** | Security transport; not form fields. |
| **Billing/credit preview computed fields** | Read-only projections. |
| **Logo paths on care home/tenant/template** | May be future feature or document-store integration — currently no upload API traced in controllers audited. |

---

## 12. Recommended Fixes

### Must Fix Before Client Demo

Only if the demo script includes these flows:

1. **Care home contact verification** — If demo shows dashboard/list after creating a home with email, add read-only contact lines on dashboard **or** state in script that contact is verified via Edit (data is saved).
2. **Funding contract + billing** — Confirm demo data includes invoice templates for each category; UI cannot pick `invoiceTemplateId` on contracts.
3. **User management** — If demo needs “change manager’s homes,” note that UI cannot edit users (only create/deactivate).

### Must Fix Before Production

1. User **edit** (role, homes, display name) aligned with `UpdateUserRequest`.
2. Funding contract **update** (dates, status, template) or documented API-only procedure.
3. Invoice template **scoping** fields (authority/home/company) if multi-template resolution is required in production.
4. Organisation settings **client-side validation** for `paymentTermsDays` (0–365) and `numberLength` (1–10) to match API.

### Can Wait Until After Launch

1. Reports and Sage export **filter parity** with API.
2. Credit note **advanced filters** and line amount overrides.
3. Client list **status/funding** filters (service already supports).
4. Rate **notes** field on client profile.
5. Report table **column label** formatting.

### Product Decisions Required

1. Should **care home / client email** drive invoice emailing, or only template `contactEmail` / authority email?
2. Are **logo** fields in scope for v1?
3. Should **invoice template** create form expose email subject/body and scoping dimensions?
4. Is **contract edit** required in UI or acceptable via support/API?
5. **Credit note** partial line credits — UI needed?
6. **Immutable user email** — confirm acceptable for tenant admins.

---

## 13. Recommended Next Actions

1. **Reconcile demo script** with findings: Care Home email is on the form; update any checklist that says otherwise.
2. **Prioritize P1 workflow gaps** (user edit, contract edit, template scoping) against launch checklist.
3. **Add read-only contact panels** on care home dashboard and/or client profile Details if operators need verify-without-edit.
4. **Validate organisation settings** numeric fields in UI to match API bounds.
5. **Product workshop** on logo fields and invoice email recipient strategy.

---

## Appendix A — Entity Trace Summaries

### Organisation (tenant settings)

- **Path:** `organisation-settings.ts` → `PUT /api/settings/organisation` → `OrganisationSettingsController` → `Tenants` + `TenantSettings`.
- **Fields:** Name, trading name, registration, address, phone, email, website, currency, timezone, invoice/credit prefixes, number length, payment terms, email-from name/address, primary colour — **aligned**.

### Company

- **Model:** `Company` — `Name`, `IsActive` only.
- **UI/API:** `company-form` ↔ `CompaniesController` — **fully aligned** (no hidden company contact fields).

### Care Home

- **Full stack aligned** for create/edit including `email`.
- **Display:** list/dashboard omit contact fields.

### Client

- **Create/edit:** `client-form` payload matches `CreateClientRequest` / `UpdateClientRequest`; API maps to `Client` entity.
- **Profile:** contracts/rates/invoices via raw `HttpClient`; contract POST omits `invoiceTemplateId`.

### Funding Authority

- **Form ↔ API ↔ `FundingAuthority` entity** — aligned including email, phone, address.

### Funding Contract & Rates

- **API:** `FundingContractsController` — create/update, rates POST.
- **UI:** `client-profile` — create contract, add rate; **no update contract**, no template id, no contract status change.

### Users

- **Create:** `user-list` sends `email`, `displayName`, `role`, `careHomeIds`, `passwordCipher` — matches `CreateUserRequest`.
- **Update/reset:** API only.

### Invoice Templates

- **Create:** partial `UpsertInvoiceTemplateRequest`; defaults for `emailSubjectTemplate` / `emailBodyTemplate` sent from FormGroup.
- **No PUT from UI.**

### Invoice Categories & Nominal Codes

- **Dedicated forms** — fields match create/update DTOs and entities.

### Billing

- **UI body** matches `BillingPreviewRequest` (`companyId`, `careHomeId`, `invoiceCategoryId`, `periodStart`, `periodEnd`, `clientIds`).

### Invoices

- **Detail:** read + send, payment status, void — matches invoice endpoints; payment values `Paid` / `NotPaid`.

### Credit Notes

- **UI** sends subset of `CreditNotePreviewRequest` / generate (clientId, periods, reason, creditNoteDate).

### Misc Charges, Sage Export, Reports

- **Misc charges:** file upload preview/confirm — aligned with import endpoints.
- **Sage:** dates only vs richer `SageExportRequest`.
- **Reports:** `from`/`to` only vs per-report filters on `ReportsController`.

### Auth

- Login email/password — `AuthController`; change password flow separate — aligned.

---

## Appendix B — Issue Type Legend (used above)

| Code | Meaning |
|------|---------|
| BACKEND FIELD NOT EXPOSED IN UI | Persisted/API accepted but no form control |
| UI FIELD NOT SENT TO API | Rare in this codebase; not a major pattern |
| API FIELD NOT PERSISTED | Not found for audited master data |
| DATABASE FIELD NOT EXPOSED | Column/entity field not in DTO or UI (e.g. some logos) |
| RESPONSE FIELD NOT DISPLAYED | GET returns field; list/detail omit it |
| CREATE/EDIT FIELD MISMATCH | Differs between modes or missing edit surface |
| INCONSISTENT UI FIELD SET | Different screens show different subsets |

---

*End of audit.*

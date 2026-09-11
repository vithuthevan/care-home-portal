# Care Home Back Office — Product User Guide

A–Z guide for new users of **Care Home Back Office**. It walks every main section in the left menu and the usual first-time setup path.

In this product, **Client** means a **resident**.

---

## Contents

1. [Getting started](#1-getting-started)
2. [Dashboard](#2-dashboard)
3. [Operations](#3-operations)
4. [Billing Setup](#4-billing-setup)
5. [Billing](#5-billing)
6. [Reporting](#6-reporting)
7. [Administration](#7-administration)
8. [Recommended first-time setup](#8-recommended-first-time-setup)
9. [Day-to-day billing cycle](#9-day-to-day-billing-cycle)
10. [Appendix: Platform Organisations](#10-appendix-platform-organisations)

---

## 1. Getting started

### Sign in

1. Open the application URL provided by your organisation.
2. Enter your email and password on the login screen.
3. After a successful login you land on the **Dashboard** (or **Organisations** if you are a platform administrator with no organisation assigned).

Your organisation name appears in the header under **Care Home**.

### Change password (first login)

If you were given a temporary password, you must change it before you can use the rest of the app. Complete the change-password screen, then sign in again if prompted.

### Sign out

Use the user menu (your name / role chip) and choose **Sign out**.

### What you can see and do (roles)

| Role | Typical use |
|------|-------------|
| **TenantAdmin** / **Administrator** | Full access in your organisation, including Users, Audit, and Organisation Settings |
| **LocationManager** | Same menus, but data is limited to care homes you are assigned to |
| **ReadOnly** | Can view lists and details; Add / Edit / Generate / Import actions are hidden or blocked |

You cannot see another organisation’s data. If a screen is missing from the menu, your role does not include it.

---

## 2. Dashboard

**Menu:** Dashboard

Your home screen for the organisation. Use it to check health before and after billing runs.

Typical areas:

- **KPIs** — care homes, clients, beds, outstanding invoices, upcoming billing
- **Recent invoices** — quick jump into invoice detail
- **Setup checklist** — guided hints when the organisation is not fully configured yet (company → care home → funding authorities → nominal codes → invoice template → clients → funding contracts & rates)
- **Billing exceptions** — issues that may block or affect billing
- **Occupancy by home** — high-level occupancy snapshot

Open a care home’s own dashboard from **Care Homes** when you need a location-focused view.

---

## 3. Operations

**Menu section:** Operations

### Companies

**Menu:** Operations → Companies

Companies are the legal / trading entities that own care homes and appear on billing.

**What to do**

1. Open **Companies**.
2. Add a company (name must be unique in your organisation).
3. Edit details later if the trading name or related fields change.

You normally create at least one company before adding care homes.

### Care Homes

**Menu:** Operations → Care Homes

Care homes are locations (sites) linked to a company, with capacity and contact details.

**What to do**

1. Open **Care Homes**.
2. Add a home: link it to a company, give it a unique code, and set bed capacity.
3. Open a home’s **Dashboard** for occupancy, outstanding amounts, and residents at that location.
4. Edit the home when capacity or manager contact details change.

**Note:** The “manager” fields on a care home form are contact details, not the security role **LocationManager**.

### Clients (residents)

**Menu:** Operations → Clients

Clients are residents. Billing depends on their **funding contracts** and **rates**, which live on the client profile (not as separate top-level menu items).

**What to do**

1. Open **Clients** and add a client (reference / Sage ID must be unique in the organisation).
2. Open the **client profile** to:
   - View and edit details
   - Add **funding contracts** (authority, invoice category, nominal code, start date, status)
   - Add **rates** (weekly / daily / monthly amounts with effective dates)
   - See related **invoices**
3. Archive only when status rules allow (for example a current resident may need to be set to Left with a discharge date before archive).

**Important**

- Rates must cover the period you want to bill.
- Overlapping rates on the same contract are not allowed.
- After a contract has been used on finalized invoices, you typically close it and create a new contract instead of rewriting core identity fields.

---

## 4. Billing Setup

**Menu section:** Billing Setup

Complete these before you generate invoices.

### Funding Authorities

**Menu:** Billing Setup → Funding Authorities

Funders who pay for care (for example a council, NHS body, or private payer).

**What to do**

1. Add each funder you invoice.
2. Set type and billing frequency as needed.
3. Select the authority later when you create a funding contract on a client.

### Invoice Categories

**Menu:** Billing Setup → Invoice Categories

Categories group charge lines and help scope billing (for example General Care).

**What to do**

1. Review the default categories (if any).
2. Add or edit categories your organisation uses on contracts and billing runs.

### Nominal Codes

**Menu:** Billing Setup → Nominal Codes

General-ledger / Sage posting codes used on invoice lines and exports.

**What to do**

1. Add codes (for example `4000`) with a clear name.
2. Duplicate codes are rejected.
3. Assign the correct nominal on funding contracts and miscellaneous charges.

### Invoice Templates

**Menu:** Billing Setup → Invoice Templates

Templates control PDF content such as bank details, footer text, and contact email for sending invoices.

**What to do**

1. Add a template for each category you invoice (at least General Care).
2. Fill bank name, sort code, account number, footer, and a contact email.
3. Invoices cannot be emailed without a recipient email address on the template / send path.

Without a suitable template, billing preview or PDF generation may fail with a clear error.

---

## 5. Billing

**Menu section:** Billing

### Billing Workspace

**Menu:** Billing → Billing Workspace

Where you preview and generate invoices for a period.

**What to do**

1. Select **company**, optional **care home**, **invoice category**, and **billing period**.
2. Click **Preview**. Review eligible lines (days, rate, amount) or read any errors (missing rate, missing template, already billed, and so on).
3. If the preview looks correct, **Generate** invoices in one go.

**Useful rules**

- A period that is **already fully billed** is blocked (you should see an already-billed style message).
- A period that **overlaps** an older invoice but still has **new billable days** can show requested / already billed / remaining periods — that is allowed.
- Do not double-click Generate; concurrent runs for the same period are protected so you should not get two invoices for the same days.

### Invoices

**Menu:** Billing → Invoices

Historical invoice list. Invoices are snapshots: changing a live client name later does **not** rewrite old invoice PDFs.

**What to do**

1. Browse and open an invoice.
2. On the detail screen you can:
   - Download **PDF**
   - **Email** the invoice (when email is configured)
   - Mark **Paid** / **Not paid**
   - **Void** if the invoice must be cancelled (void invoices cannot be marked paid)
3. Prefer void + credit / reinvoice over deleting history — invoices are not hard-deleted in normal use.

### Credit Notes

**Menu:** Billing → Credit Notes

Credits reverse invoiced amounts without rewriting the original invoice.

**What to do**

1. Preview a credit for a period that maps to **one** invoice.
2. Enter a reason and generate (for example `CN-0001`).
3. Credit each invoice separately if more than one invoice is involved.
4. You cannot credit more than the remaining invoiced amount.

After a credit, reinvoice from the Billing Workspace if you need a corrected charge.

### Miscellaneous Charges

**Menu:** Billing → Miscellaneous Charges

Ad-hoc charges imported from CSV (for example one-off items not covered by the standard rate).

**Expected columns**

`ClientReference`, `UsedDate` (`yyyy-MM-dd`), `Description`, `Amount`, `NominalCode`

**What to do**

1. Upload a CSV and **preview**.
2. Fix any row errors (messages include the row number). Confirm is refused while any row is invalid — nothing from that file is saved.
3. Confirm a clean file. Charges appear in import history and can feed billing.

---

## 6. Reporting

**Menu section:** Reporting

### Reports

**Menu:** Reporting → Reports

Run operational and finance reports for a company / home / period, then export as CSV, Excel, or PDF (depending on the report).

Common report types include census, rates, invoices, income, occupancy, exceptions, and outstanding balances.

**What to do**

1. Choose the report and filters.
2. Run it and check totals against invoices you know about.
3. Export if you need a file for finance or audit.

### Sage Export

**Menu:** Reporting → Sage Export

Produces a provisional Sage 50–style CSV for a date range.

**What to do**

1. Choose a date range that includes the invoices you want to post.
2. **Validate** — eligible rows need Sage ID and nominal information on the invoice.
3. **Export** the CSV and open it to check invoice number, date, Sage client ID, nominal, amount, and row count.
4. Exporting the same range again without including already-exported items skips previous exports.

Keep exports as part of your period-close checklist.

---

## 7. Administration

**Menu section:** Administration  
*(Visible to TenantAdmin / Administrator.)*

### Users

**Menu:** Administration → Users

Manage who can sign in to your organisation.

**What to do**

1. Create users with email, role, and (for Location Managers) assigned care homes.
2. Deactivate users who should no longer access the system.
3. You cannot create a PlatformAdmin from this screen.

Assign the least privilege needed: ReadOnly for viewers, LocationManager for site staff, Administrator / TenantAdmin for full organisation control.

### Audit

**Menu:** Administration → Audit

Append-only log of important actions (entity, action, description). Use filters when investigating who changed what and when.

### Organisation Settings

**Menu:** Administration → Organisation Settings

Organisation identity and invoice defaults.

Typical fields include organisation / trading name, contact details, branding colour, currency, invoice and credit-note number prefixes, and payment terms.

**What to do**

1. Set prefixes and payment terms before your first live billing run.
2. Save and refresh to confirm the values stuck.
3. ReadOnly users cannot save changes here.

---

## 8. Recommended first-time setup

Follow this order the first time you configure an organisation:

1. **Organisation Settings** — name, prefixes, payment terms  
2. **Company**  
3. **Care Home** (linked to the company)  
4. **Funding Authority**  
5. **Invoice Category** (if you need more than the defaults)  
6. **Nominal Code**  
7. **Invoice Template** (bank details + email contact)  
8. **Client**  
9. **Funding Contract** on the client profile  
10. **Rate** on that contract  
11. **Billing Workspace** — Preview, then Generate  
12. Open the **Invoice** — PDF / email / payment status  
13. **Credit Notes** only if a correction is needed  
14. **Sage Export** when you are ready to post  

The Dashboard setup checklist mirrors most of these steps.

---

## 9. Day-to-day billing cycle

Once setup is complete, a typical period looks like this:

1. Check **Dashboard** for exceptions, upcoming billing, and outstanding invoices.  
2. Update **Clients** — new admissions, discharges, contracts, and rates.  
3. Import **Miscellaneous Charges** if you have ad-hoc CSV charges.  
4. Run **Billing Workspace** preview → generate for the period.  
5. Work **Invoices** — PDF, email, mark paid, void if required.  
6. Issue **Credit Notes** and reinvoice when corrections are needed.  
7. Run **Reports** for management packs.  
8. **Sage Export** at period close.  

---

## 10. Appendix: Platform Organisations

**Menu:** Platform → Organisations  
*(PlatformAdmin / SuperAdmin only.)*

Used by platform operators to create purchasing organisations (tenants) and seed the first organisation admin credentials. Day-to-day finance users do not use this section.

If you only have a platform login and no organisation, you will see **Organisations** alone until you use a tenant user account for billing work.

---

## Need help?

- Blank screens or endless loading after login — contact IT.  
- Invoice totals that do not match line sums — stop and escalate.  
- Historical PDF showing a renamed client — that is a defect; report it.  
- Duplicate invoice numbers — stop billing and escalate.  

For technical operators, see also `docs/UAT_CHECKLIST.md` (screen-by-screen test script) and the main workflow in the repository `README.md`.

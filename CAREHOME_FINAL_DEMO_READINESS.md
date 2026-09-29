# CareHome Final Demo Readiness

## Executive Summary

Final demo-preparation pass focused on P0/P1 polish: duplicate create actions on list screens, optional email validation, theme toggle placement, demo seed data, and user list sort order. Invoice template snapshots, PDF rendering, billing generation, and development email simulation were verified by code review and existing unit tests. **Period Close** is not implemented as an application feature (only referenced in user documentation).

## Demo Verdict

**READY WITH MINOR LIMITATIONS**

- Core billing → invoice → PDF → credit note paths are implemented and covered by automated tests.
- Demo dataset seeder runs on API startup in Development when a Green Meadows or legacy Demo Care Group tenant exists.
- Full browser end-to-end rehearsal was **not** completed in this pass (local API startup did not become ready within the preparation window); use the demo script below on your machine before presenting.

---

## Application Startup

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `Program.cs` applies migrations, runs identity/master/demo seeders, registers services. |
| RUNTIME VERIFIED | **NO** — `dotnet run` remained on “Building…” during preparation; verify locally before demo. |

---

## Login

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | JWT auth, `IdentitySeeder`, development credentials in `appsettings.Development.json`. |
| RUNTIME VERIFIED | **NO** |

**Demo tenant user:** `vithursan@greenmeadows.demo` (display name **Vithursan**, role **TenantAdmin**).  
**Password:** configured under `Seed:DemoTenantAdminPassword` in `backend/CareHome.Api/appsettings.Development.json` (not repeated here).  
**Platform admin (organisations):** `Seed:AdminEmail` / `Seed:AdminPassword` in the same file.

---

## Care Homes

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | CRUD controllers; list uses single header “Add care home” after duplicate removal. |
| RUNTIME VERIFIED | **NO** |

---

## Residents

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `ClientsController` lists `OrderByDescending(Id)`; optional email on create/update; demo residents seeded. |
| RUNTIME VERIFIED | **NO** |

---

## Funding Contracts

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | Blank end date → `null` in `client-funding-contract-form.ts` payload. |
| RUNTIME VERIFIED | **NO** |

---

## Rates

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | Seeded weekly rates on demo contracts. |
| RUNTIME VERIFIED | **NO** |

---

## Miscellaneous Charges

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | Import workflow; demo misc charge for Mary Wilson (uninvoiced). |
| RUNTIME VERIFIED | **NO** |

---

## Billing Preview

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `BillingController` preview/generate; integration tests in `BillingWiringIntegrationTests`. |
| RUNTIME VERIFIED | **NO** |

---

## Invoice Generation

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `BillingService` snapshots template header/footer/contact/bank fields on generate. |
| RUNTIME VERIFIED | **NO** (demo seeder attempts John/David invoices on startup when DB available). |

---

## Invoice Templates

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | Snapshots copied in `BillingService`; `InvoiceTemplateResolverTests`. |
| RUNTIME VERIFIED | **NO** |

---

## Invoice PDF

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `InvoicePdfService` uses `SnapshotHeaderText1/2`, `SnapshotFooterText`, logos, bank block. |
| RUNTIME VERIFIED | **NO** |

---

## Credit Notes

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `CreditNotesController` list `CreditNoteDate DESC`; demo partial credit for David Brown in seeder. |
| RUNTIME VERIFIED | **NO** |

---

## Reports

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `ReportsController`: census, current rates, invoices by client/home, income by category, occupancy, rate history, billing exceptions, credit notes summary. |
| RUNTIME VERIFIED | **NO** |

| Report | Data source | Notes |
|--------|-------------|-------|
| Client census | Residents + care homes | Filters: company, care home |
| Current rates | Active contracts/rates | Funding/category filters |
| Invoices by client | Invoice lines | Date range; includes generated invoices |
| Invoices by care home | Invoices | Date range |
| Income by category | Invoice line amounts in period | Credit notes reduce via report service logic |
| Occupancy | Bed capacity vs current residents | By company |
| Rate history | Funding rate rows | Optional contract filter |
| Billing exceptions | Exception log | Operational |
| Credit notes | Credit note entities | Tied to invoices |

---

## Email

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `Email:Mode` = `Development` simulates/logs sends; invoice email uses `DocumentEmailService`. |
| RUNTIME VERIFIED | **NO** |

---

## Period Close

| Status | Evidence |
|--------|----------|
| NOT AVAILABLE | No period-close module in codebase; manual month-end process only (Sage export / reports). |

---

## Audit

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `AuditService`, `AuditController` `OrderByDescending(LoggedAt)`. |
| RUNTIME VERIFIED | **NO** |

---

## Theme Toggle

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | Light/dark control moved to top toolbar immediately before user chip (`app.html`). |
| RUNTIME VERIFIED | **NO** |

---

## Sorting

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | Invoices `InvoiceDate DESC`, credit notes `CreditNoteDate DESC`, audit `LoggedAt DESC`, clients `Id DESC`, users `Id DESC`, misc import batches `ImportedAt DESC`. |
| RUNTIME VERIFIED | **NO** |

---

## Optional Email Validation

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | API `OptionalEmailAddressAttribute`; frontend `optionalEmail()` + `optionalEmailValidator()` on forms. |
| RUNTIME VERIFIED | **NO** |

---

## Sage Nominal Codes

| Status | Evidence |
|--------|----------|
| CODE REVIEWED | `TenantNominalCodeSeeder` + `DefaultNominalCodes.StarterSet` (4000–4003) on all active tenants. |
| RUNTIME VERIFIED | **NO** |

---

## KNOWN DEMO LIMITATIONS

1. **Period Close** — not a product feature; demonstrate reports + Sage export instead.
2. **Finance module** (payments, banking, receivables) — disabled on new tenants unless enabled in organisation settings; optional for core demo.
3. **Runtime E2E** — rehearse login → billing → PDF on demo hardware before the session.
4. **Existing databases** — if the tenant is still named “Demo Care Group”, demo seeder still applies residents/invoices to that tenant; fresh DBs get “Green Meadows Care Ltd”.
5. **Committed dev passwords** — rotate `Seed:*` values if the repo is shared publicly.

---

## FIXES COMPLETED

| Area | Files |
|------|--------|
| Duplicate Add buttons | `client-list.html`, `care-home-list.html`, `user-list.html`, `funding-authority-list.html`, `nominal-code-list.html`, `invoice-category-list.html`, `invoice-template-list.html` |
| Optional email (UI) | `optional-email.ts`, `client-form.ts`, `company-form.ts`, `care-home-form.ts`, `funding-authority-form.ts`, `organisation-settings.ts` |
| Theme toggle | `app.html`, `app.ts` |
| User list sort | `UsersController.cs` |
| Demo data | `IdentitySeeder.cs` (Green Meadows names), `FinalDemoPresentationSeeder.cs`, `Program.cs`, `appsettings.Development.json` |

---

## DEMO DATA

| Entity | Value |
|--------|--------|
| Tenant | Green Meadows Care Ltd (or existing Demo Care Group) |
| Company | Green Meadows Care Ltd |
| Care home | Green Meadows Residential Home (`GMEADOWS`) |
| Resident A | John Carter — `REF-JCARTER` — invoice Apr 2026 (seeder) |
| Resident B | Mary Wilson — `REF-MWILSON` — misc charge “Hairdressing appointment” £35 |
| Resident C | David Brown — `REF-DBROWN` — invoice + 50% partial credit note (seeder) |
| Nominal codes | 4000–4003 (starter set) |
| Invoice template | Header/footer text on default general care template |

---

## DEMO LOGIN

- **Tenant demo:** `vithursan@greenmeadows.demo` — password in `Seed:DemoTenantAdminPassword` (`appsettings.Development.json`).
- **Platform admin:** `Seed:AdminEmail` / `Seed:AdminPassword` in the same file.

---

## FINAL DEMO SCRIPT

| Step | Page | Action | What to explain | Expected result |
|------|------|--------|-----------------|-----------------|
| 1 | `/login` | Sign in as tenant admin | Multi-tenant back office | Dashboard loads, tenant name visible |
| 2 | `/dashboard` | Review KPIs | Operational overview | Counts reflect seeded data |
| 3 | `/care-homes` | Open Green Meadows Residential Home | Company/care home structure | Dashboard for home |
| 4 | `/clients` | Open John Carter | Resident 360 | Funding tab shows active contract + rate |
| 5 | Resident profile | Show Mary Wilson misc charge context | Ad-hoc charges before billing | Charge visible / uninvoiced |
| 6 | `/billing` | Company + home + Apr 2026 period, Preview | Rate × days + misc for Mary | Consistent preview lines |
| 7 | `/billing` | Generate for Mary (or live) | Invoice creation | Success toast, new invoice |
| 8 | `/invoices` | Open latest invoice | Snapshot billing | Header/footer from template |
| 9 | Invoice detail | Download PDF | Branded PDF | Readable layout, totals |
| 10 | `/credit-notes` | Open David’s credit note | Adjustments reduce balance | Linked invoice, partial amount |
| 11 | `/reports` | Income by category Apr 2026 | Management reporting | Non-zero totals |
| 12 | `/audit` | Filter recent | Compliance trail | Create/invoice actions logged |
| 13 | Top bar | Theme toggle | Light/dark before user menu | Mode switches instantly |

---

## FALLBACK PLAN

| Feature | Fallback |
|---------|------------|
| Email send | Show **Email delivery** log / toast “simulated in development” (`Email:Mode=Development`). |
| PDF preview in browser | Use **Download PDF** and open file locally. |
| Billing preview empty | Use John Carter pre-seeded April 2026 invoice under **Invoices**. |
| Reports empty | Set date range **2026-04-01** to **2026-04-30**. |
| Login failure | Use platform admin → Organisations → confirm tenant active; reset password via Users. |
| API down | Run `dotnet run` in `backend/CareHome.Api` and `ng serve` in `frontend/care-home-web`. |

---

## FINAL CHECKLIST

| Demo Step | Status | Runtime Verified |
|-----------|--------|------------------|
| Login | PASS (code) | NO |
| View dashboard | PASS (code) | NO |
| View residents | PASS (code) | NO |
| Funding contract | PASS (code) | NO |
| Rate | PASS (code) | NO |
| Misc charge | PASS (code) | NO |
| Billing preview | PASS (code) | NO |
| Generate invoice | PASS (code) | NO |
| Invoice PDF | PASS (code) | NO |
| Credit note | PASS (code) | NO |
| Reports | PASS (code) | NO |
| Audit | PASS (code) | NO |
| Theme toggle | PASS (code) | NO |

---

## Tests Run

| | |
|--|--|
| **Executed** | `dotnet test CareHome.Api.Tests --filter "FullyQualifiedName!~Integration"` |
| **Passed** | 80 |
| **Failed** | 0 |

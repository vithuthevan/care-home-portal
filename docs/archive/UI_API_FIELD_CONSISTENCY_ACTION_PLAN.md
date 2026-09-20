# UI ↔ API Field Consistency — Action Plan

**Status:** Review / planning only. No code, schema, configuration, tests, or deployments were changed.  
**Date:** 16 September 2026  
**Source of truth:** `UI_API_FIELD_CONSISTENCY_AUDIT.md`, demo/production documents listed below, and current application code.

**Documents reviewed:**

- `UI_API_FIELD_CONSISTENCY_AUDIT.md`
- `../demo/CLIENT_DEMO_A_TO_Z_SCRIPT.md`
- `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md`
- `../demo/DEMO_DATA_ENTRY_CHECKLIST.md`
- `../demo/DEMO_DATA_PREPARATION_GUIDE.md`
- `CLIENT_DEMO_DRY_RUN_REPORT.md`
- `../archive/CREDIT_NOTE_INVESTIGATION.md`
- `PRODUCTION_LAUNCH_READINESS_REPORT.md`
- `PRODUCTION_READINESS_ACTION_PLAN.md`

---

## 1. Corrected original finding — Care Home Email

### Do not add another Care Home Email field

Care Home Email is already supported end to end. The original observation was **not** a create/edit UI gap.

| Check | Result |
|-------|--------|
| Care Home Email already exists in create UI | **Yes** — `care-home-form.html` `formControlName="email"` |
| Care Home Email already exists in edit UI | **Yes** — same form; edit patches `careHome.email` |
| Care Home Email is sent in API payload | **Yes** — `care-home-form.ts` `save()` includes `email` and `managerEmail` |
| Care Home Email is persisted | **Yes** — `CareHomesController` maps `request.Email` onto `CareHomeLocation.Email` |
| Care Home Email is returned | **Yes** — `CareHomeDto` / frontend `CareHomeLocation.email` |
| Care Home Email is missing only from list/dashboard display | **Yes** — list shows code/name/company/capacity/manager **name**/status; dashboard shows manager **name** only |

**Do not add another Care Home Email field.**

### What the actual issue is

Contact fields (`email`, `phone`, `address`, `managerEmail`, `managerPhone`) are captured, stored, and returned. They are **not rendered** on:

- Care home **list** (`/care-homes`)
- Care home **dashboard** (`/care-homes/{id}/dashboard`)

Verification today requires opening **Edit**.

Manager **name** is already shown on both list and dashboard. Email/phone/address are the display gap.

### Does this need to change?

| Milestone | Required? | Why |
|-----------|-----------|-----|
| **Client demo** | **No** | A-to-Z §7 shows Code, Name, Company, Capacity. It does not claim email on list/dashboard. Demo data checklist does not even enter a care home email. Presenter can open Edit if asked. |
| **Controlled pilot** | **Optional** | Operators can verify via Edit. A read-only contact block reduces friction; it is not an operational blocker. |
| **Full production** | **Usability, not integrity** | No data-loss risk. Add a read-only contact panel if operators need verify-without-edit. Do **not** treat this as a financial or security blocker. |

**Recommended action:** If changed at all, add **read-only display** of existing fields on dashboard (preferred) and/or list. Do **not** add a second Email input on the create/edit form.

**Also note:** Care Home Email is **not** the invoice send recipient. Billing sets `Invoice.RecipientEmail` from invoice template `ContactEmail`, then falls back to funding authority `Email`. Care home / client emails are master-data only unless product later changes recipient strategy (Decision 3).

---

## 2. P1 findings — required or not?

Legend:

- **Demo** = live client demonstration using the A-to-Z script
- **Pilot** = controlled single-tenant operational use
- **Prod** = full production / live funder invoicing
- **Wait** = can safely wait past that milestone
- **Business first** = do not implement until the client answers the question in §6

### Summary

| ID | Finding | Demo | Pilot | Prod | Can wait? | Business decision first? |
|----|---------|------|-------|------|-----------|--------------------------|
| A | Care Home contact display | No | Optional | Usability | Yes for demo | No (display only) |
| B | Funding Contract edit | No | **Yes** | **Yes** | No past pilot | Partly (how end-dating should work) |
| C | Funding Contract `invoiceTemplateId` | No | No (as currently designed) | Only if product intends contract-level override | Yes | **Yes** — field is stored but **billing does not use it** |
| D | User edit | No | **Yes** | **Yes** | No past pilot | Partly (role/home rules) |
| E | User reset password | No | **Yes** (admin reset) | **Yes** | Self-service forgot-password can wait | Yes for self-service vs admin-only |
| F | Invoice Template editing/scoping | No (category default is enough) | Should | **Yes** if multi-home/funder branding | Category-default create can wait for demo | **Yes** for scoping model |
| G | Invoice Template email subject/body | No | Should if real SMTP | **Yes** if sending live email | Yes for simulated-email demo | Copy/branding only |
| H | Reports filters | No | Should | Should | Date-only is enough for demo | **Yes** (which filters finance needs) |
| I | Sage export filters | No (not shown in demo) | If Sage used in pilot | **Yes** before live Sage posting | Yes until Sage is in scope | **Yes** (mapping still unsigned) |

### A. Care Home contact display

- **Issue:** `email`, `phone`, `address`, `managerEmail`, `managerPhone` returned by GET; omitted from list/dashboard.
- **Demo:** Not required. Script never presents those fields on list/dashboard.
- **Pilot:** Optional convenience.
- **Production:** Usability only. Not a data-integrity issue.
- **Can wait:** Yes.
- **Business decision:** No, unless the client wants care-home email to drive invoicing (that is a different decision).
- **Correct fix if done:** Read-only contact block. Dashboard already loads full `CareHomeLocation` via `homes.getCareHome(id)` and simply does not bind email/phone/address.

### B. Funding Contract edit

- **Issue:** UI can **create** a contract and **add rates**. There is **no** contract update UI. API `PUT /api/funding-contracts/{id}` already accepts dates, status, nominal, category, authority, optional `invoiceTemplateId`.
- **Demo:** Not required. Demo contracts are created once and left Active/open-ended.
- **Pilot:** **Required.** Real operations must end-date, close, or correct a contract without API tools.
- **Production:** **Required.** Inability to maintain contracts is an operational blocker.
- **Can wait:** Past demo only.
- **Business decision:** Confirm allowed edits (start date after invoicing? status values? reopen closed contracts?). Do not invent rules.

### C. Funding Contract `invoiceTemplateId`

**Important correction beyond the audit wording.**

The UI does not send `invoiceTemplateId` on create. That is true. Billing does **not** fail for that reason if a **category-default template** exists.

`InvoiceTemplateResolver.ResolveAsync` matches active templates by:

1. care home + funding authority  
2. authority only  
3. care home only  
4. company only  
5. category default (all of home/authority/company null)

It **does not read** `ClientFundingContract.InvoiceTemplateId`. Billing then stamps `Invoice.InvoiceTemplateId` from the **resolved** template.

So:

- Demo needs a **General Care** category-default template (operator runbook / checklist already do this).
- Demo does **not** need a contract-level template picker.
- Adding a picker **without** changing billing would persist a field operators think they selected, then **ignore it at invoice time**. That would misrepresent the product.

| Milestone | Required? |
|-----------|-----------|
| Demo | **No** — create category default; do not “link on contract” |
| Pilot | **No**, unless client requires per-contract override |
| Production | Only after Decision 2 (scoping vs contract override) |

`../demo/DEMO_DATA_PREPARATION_GUIDE.md` still says “create template then link on each funding contract.” That is **inaccurate** against both UI and billing. Operator runbook §4.6 already states the UI does not link a template.

### D. User edit

- **Issue:** UI can create users and deactivate. No edit for `displayName`, `role`, `careHomeIds`, or reactivate. API `PUT /api/users/{id}` (`UpdateUserRequest`) already exists. Email is intentionally immutable on update.
- **Demo:** Not required. Users are created once (`demo-admin`, optional `demo-viewer`). Script does not change roles or homes.
- **Pilot:** **Required.** Wrong LocationManager home assignment cannot be fixed in UI.
- **Production:** **Required.** Inability to administer users is a production blocker.
- **Can wait:** Past demo only.
- **Business decision:** Confirm email stays immutable; confirm who may change TenantAdmin roles.

### E. User reset password

- **Issue:** No UI. API `POST /api/users/{id}/reset-password` exists (admin sets password; `MustChangePassword = true`). This is **not** the same as P3 self-service forgot-password in the production plan.
- **Demo:** Not required (passwords stored in password manager; forced change already done before client joins).
- **Pilot:** **Should have** admin reset so a locked/forgotten password does not require API/scripts.
- **Production:** Admin reset **required** for operations. Self-service forgot-password remains a later enhancement (`PRODUCTION_READINESS_ACTION_PLAN.md` P3-01).
- **Can wait:** Self-service yes; admin reset should not wait past pilot.
- **Business decision:** Admin-set password vs email token reset.

### F. Invoice Template editing / scoping

- **Issue:** UI create form is a **category default** subset (name, category, header, footer, bank, contact name/email). No edit/deactivate. Scoping (`fundingAuthorityId`, `careHomeId`, `companyId`), `headerText2`, `contactPhone`, `contactJobTitle`, `isActive` exist on `UpsertInvoiceTemplateRequest` and in the database.
- **Demo:** **Not required.** One General Care category default is enough. Billing subtitle already says “most specific matching template.”
- **Pilot:** **Should** have edit (correct bank details / deactivate wrong template). Scoping only if multiple homes/funders need different PDFs.
- **Production:** Edit/deactivate **required**. Scoping **required** if the resolver’s specificity model is part of the sold product; otherwise document “category default only in v1.”
- **Can wait:** Scoping can wait if the client accepts one template per category.
- **Business decision:** Decision 2.

`../demo/DEMO_DATA_PREPARATION_GUIDE.md` asks for Company, Care home, Funding authority, contact job title, and contact phone on the template. Those fields are **not in the create form**. Operator runbook already notes job title/phone are data-guide-only.

### G. Invoice Template email subject/body

- **Issue:** `emailSubjectTemplate` / `emailBodyTemplate` exist on the FormGroup with defaults (`Invoice {{InvoiceNumber}}` / `Please find the invoice attached.`) and are **posted on every create**, but there are **no HTML inputs**. Users cannot customise copy.
- **Demo:** Not required. Email is **simulated**; script discloses that.
- **Pilot:** Should expose if SMTP is live, so finance can control what funders see.
- **Production:** Required before live invoice email, or document that copy is fixed defaults.
- **Can wait:** Yes while email remains simulated.
- **Business decision:** Copy ownership (finance vs product defaults).

### H. Reports filters

- **Issue:** UI sends `from` / `to` only. `ReportsController` accepts `companyId`, `careHomeId`, `clientId`, `fundingAuthorityId`, `categoryId`, `clientStatus`, etc. per report.
- **Demo:** **Not required.** A-to-Z uses outstanding (no dates) and invoices-by-client with August 2026 dates — both possible today. Empty invoices-by-client is a **date** mistake, not a missing filter widget.
- **Pilot:** Should add the filters finance will actually use (ask first).
- **Production:** Same. Not a data-integrity issue; it is operational usefulness.
- **Can wait:** Yes for demo; do not build a full filter matrix before Decision 8.
- **Known separate caveat (already in demo docs):** Outstanding report uses invoice **header** totals and does **not** net credit notes. That is report behaviour, not a missing UI filter.

### I. Sage export filters

- **Issue:** UI sends `dateFrom` / `dateTo` only. `SageExportRequest` also has `companyId`, `careHomeId`, `status`, `includeAlreadyExported`. Default `includeAlreadyExported = false` is actually the safe direction.
- **Demo:** **Not required.** A-to-Z §24: do not show Sage unless the client asks.
- **Pilot:** Required only if Sage export is in the pilot scope **and** mapping is signed off (production P0-02 still pending).
- **Production:** Required before unsupervised Sage posting, together with mapping sign-off.
- **Can wait:** Yes until Sage is in scope.
- **Business decision:** Sage mapping and whether operators may re-export.

---

## 3. Credit notes (separate)

Source: `../archive/CREDIT_NOTE_INVESTIGATION.md` plus current `credit-note-workspace` code.

### What the backend supports

`POST /api/credit-notes/preview` and `POST /api/credit-notes/generate` accept `CreditNotePreviewRequest`:

- Required for a successful generate: `periodStart`, `periodEnd`, `reason` (and period order)
- Optional filters: `clientId`, `fundingAuthorityId`, `invoiceCategoryId`
- Optional `lineAmounts`: `Dictionary<int, decimal>` keyed by **invoice line id**
- Rules: one source invoice; cannot exceed remaining; cannot credit void invoices
- Period overlap **selects lines**; it does **not** prorate amount
- Omit `lineAmounts` → credit **full remaining** on each matched line (UAT TC-193)
- Provide `lineAmounts` → partial credit (UAT TC-192)

This is intentional API design, not a calculation bug.

### What the UI supports

The workspace sends `clientId`, `periodStart`, `periodEnd`, `reason`, `creditNoteDate` (set to period end).

It does **not** send `lineAmounts`, `fundingAuthorityId`, or `invoiceCategoryId`. Preview Credit/Remaining columns are read-only. There is no amount field, day count, or per-line editor.

### Why £600 cannot be entered through the UI

Demo story: 7 days × £600/week ÷ 7 = **£600** on INV-0002.

INV-0002 is **one** line for 1–31 Aug totalling **£2,657.14**. Period 25–31 Aug **overlaps** that line, so the line is eligible. With no `lineAmounts`, requested credit = remaining = **£2,657.14**.

There is no server path that computes £600 from “7 days.” Achieving £600 requires `lineAmounts: { "<invoiceLineId>": 600.00 }` (API) or a different invoice shape (not how August billing works).

### Demo blocker?

**Not a workflow blocker. It is a narrative blocker if £600 is claimed.**

| Path | Safe? |
|------|-------|
| A-to-Z Path B: Preview only; do not Generate if Credit ≠ intended amount | **Yes** |
| Generate a **full** credit and say the amount on screen | **Yes**, if numbers match Preview |
| Claim CN-0001 = £600 from period dates | **No** — materially misrepresents the product |
| Force £600 via API in front of the client | **No** — API-only, not the product they would use |

A-to-Z §16, operator runbook Part 7.2, dry-run warnings, and the data-entry checklist already document this. `../demo/DEMO_DATA_PREPARATION_GUIDE.md` §9 still targets £600 as if the UI could produce it.

**Client demo code change is not required** if the presenter follows the safe path.

### Production blocker?

| If the client… | Rating |
|----------------|--------|
| Only ever issues **full line / full invoice** credits | Not a blocker. Train that period ≠ proration. Improve helper text. |
| Needs **partial** credits in daily UI work | **Production / finance go-live blocker** (P1). API can do it; UI cannot. Wrong-full-credit is a material misstatement. Preview still shows the amount before Generate, so this is **mis-operation risk**, not silent corruption. |

`../archive/CREDIT_NOTE_INVESTIGATION.md` correctly says this is **not** a P0 silent-corruption defect. It **is** a P1 finance-completeness issue if partial credits are required.

Reversibility: no credit-note void API was found. A mistaken full credit consumes remaining balance.

### Minimum UI enhancement (if partial credits are required)

Do **not** implement yet. Smallest useful change:

1. After Preview, show each line with remaining and an **editable credit amount** defaulting to remaining.
2. Send those values as `lineAmounts` on both subsequent Preview and Generate.
3. Validate `0 < amount ≤ remaining` in the UI; API already enforces the ceiling.
4. Helper text: **period filters which invoice lines are eligible; it does not prorate by days.**
5. Optional later: day-proration as a **new business rule** (not current design). Do not build proration unless the client asks.

No database/schema change is required for that minimum.

---

## 4. Demo script vs application

Compared to the audit and current UI. Environment data readiness is separate (dry-run: Demo Care Group **not loaded**).

| Topic | Script / docs | Application | Verdict |
|-------|---------------|-------------|---------|
| **Care Home Email** | A-to-Z does **not** claim email on list. Checklist does not enter care home email. | Email is on create/edit form; not on list/dashboard. | **Accurate.** Do not add a field. Do not spend demo time “missing email.” |
| **Funding Contract** | A-to-Z shows authority/category/nominal/start/status/rate. Checklist creates those fields only. | Create + rates only; no edit; no template picker. | **A-to-Z / checklist / runbook accurate.** **Prep guide inaccurate** (“link template on contract”). |
| **Invoice Templates** | Runbook: category default form; no job title/phone; no contract link. Checklist matches. | Matches runbook. Hidden email subject/body defaults are posted. | **Runbook/checklist accurate.** **Prep guide over-specifies** scoping and contact phone/title. |
| **Credit Notes** | A-to-Z §16: Preview is authoritative; £600 only if Credit column shows £600. | Full remaining without `lineAmounts`. | **Script/runbook accurate.** **Prep guide §9 still implies £600 via UI.** |
| **Email** | Simulated in Development; do not claim inbox delivery. Recipient not discussed. | Send uses template `ContactEmail` then authority email. Toast may say queued/sent. | **Simulation disclosure is accurate.** If asked “who receives it?”, say template contact email (`finance@demo-care-group.example`), not care home/client email. |
| **Reports** | Outstanding + invoices-by-client with August dates. Outstanding does not net credits. | Date filters exist; extra API filters do not. Header totals caveat is real. | **Accurate** for the two demo reports. Raw JSON column keys are cosmetic only. |
| **ReadOnly** | Optional login as `demo-viewer`; writes hidden. | Role exists; API `ReadOnlyGuardFilter` blocks writes. Create-user UI includes ReadOnly. | **Accurate**, provided the viewer user is created in rehearsal. Cannot **edit** that user later in UI (not needed for demo). |
| **Sage / misc charges** | Do not show unless asked. | Sage date-only export exists. | **Correct omission.** |
| **Billing amounts** | Finance-sign-off disclaimer; August proration ≠ 4 × weekly. | Implemented demo formula. | **Accurate.** |

### Inaccurate / impossible / misleading items still in docs

These are **documentation** issues, not product defects to code before demo:

1. `../demo/DEMO_DATA_PREPARATION_GUIDE.md` §5/§6 — “Invoice template: General Care Template (create then **link**)”. **Impossible in UI** and **unused by billing**.
2. Same guide template table — Company / Care home / Funding authority / contact job title / contact phone. **Not on the create form.**
3. Same guide §9 — CN-0001 target **£600** as a UI outcome. **Impossible** without `lineAmounts`.
4. Rate **notes** in the prep guide (`Council residential rate — demo`) — profile rate form always sends default empty notes; no notes input (P3). Harmless if omitted.

A-to-Z, operator runbook, and data-entry checklist are the operators’ source of truth for the live session.

### API-only functionality the demo must not imply is in the UI

- Partial credit amounts (`lineAmounts`)
- Contract update / close / template override
- User edit and admin password reset
- Invoice template edit, scoping, email-body editing
- Report dimension filters beyond From/To
- Sage company/home/status/re-export filters
- Logo upload

---

## 5. Priority groups

### A. MUST FIX BEFORE CLIENT DEMO

Only items that could **fail the demonstration** or **materially misrepresent** the application.

**No field-consistency code changes belong here.**

| Item | Type | Why it is in Group A |
|------|------|----------------------|
| Load Demo Care Group + fictional dataset | **Environment / data** | Dry-run 16 Sep: tenant, users, clients, invoices **absent**. A-to-Z cannot run. Follow `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md` Parts 2–11. |
| Credit-note narrative | **Process / rehearsal** | Do not claim £600 unless Preview shows £600. Prefer Path B (preview-only) or a full-credit narrative that matches the Credit column. |
| Do **not** add a Care Home Email field | **Anti-fix** | Would duplicate a working field and waste demo prep. |
| Treat prep guide template-link / £600 rows as superseded | **Docs / operator brief** | Runbook + A-to-Z + checklist already correct. Prep guide can confuse rehearsal. |

If those are done, the A-to-Z walkthrough is **product-capable**. Remaining P1 UI gaps are not on the demo path.

### B. SHOULD FIX BEFORE CONTROLLED PILOT

Real operational usage; do not block the demonstration.

| Item | Why |
|------|-----|
| User **edit** (display name, role, care home assignments, reactivate) | Cannot correct access mistakes without API |
| Admin **reset password** (existing API) | Pilot lockout otherwise needs scripts |
| Funding contract **edit** (end date, status) | Cannot close or correct contracts in UI |
| Invoice template **edit / deactivate** (same fields as create, plus active flag) | Bank/contact mistakes cannot be corrected |
| Credit-note **helper text** (period filters lines, not days) | Prevents full-credit accidents even before partial-amount UI |
| Organisation settings UI min/max for `paymentTermsDays` (0–365) and `numberLength` (1–10) | Small; API already rejects out-of-range |

**Pilot with workarounds (not preferred):** API/scripts for user/contract/template maintenance; full credits only; simulated or tightly supervised email.

### C. MUST FIX BEFORE FULL PRODUCTION

Data integrity, financial, security, administration, or serious usability.

| Item | Why |
|------|-----|
| User administration in UI (edit + admin reset) | Cannot run a live tenant without it |
| Funding contract maintenance in UI | Live contract lifecycle |
| Invoice template maintenance in UI | Live PDF/bank/contact data |
| Partial credit UI **if** client requires partial credits | Financial completeness; API-only is not acceptable for unsupervised finance |
| Invoice email copy fields **if** SMTP is live | Operators must control funder-facing text |
| Template scoping **or** an explicit “category default only” product decision | Resolver already supports specificity; UI cannot create it |
| Production items **outside this audit** still apply | Billing/Sage **business sign-off**, secrets, backups, restore drill, SMTP vs simulated email (`PRODUCTION_LAUNCH_READINESS_REPORT.md` P0s) |

### D. CAN WAIT

| Item | Why |
|------|-----|
| Care home contact columns / dashboard block | Verify via Edit |
| Funding authority list email/phone columns | Contact name is shown; Edit has full fields |
| Client profile Details: notes / discharge | On edit form already |
| Client list extra filters | Service supports `extra`; nice-to-have |
| Reports extra filters | Date range covers demo and basic ops |
| Sage extra filters | Out of demo; blocked on Sage sign-off anyway |
| Rate notes input | Unused |
| Report column-label formatting | Cosmetic |
| Logo fields (care home / tenant / template) | No upload API; Decision 4 |
| Platform tenant edit showing unused admin email | Harmless extra JSON |
| Empty-string vs null normalisation for optional contact fields | P3 |
| Credit-note void workflow; outstanding report netting of credits | Known limitations; document for now |
| Automatic day-proration of credits | New business rule, not a gap fill |
| Self-service forgot-password | P3 in production plan |
| Client ID numeric field → client picker on credit notes | UX only |

---

## 6. Business decisions required

Do **not** decide these on behalf of the client.

| Decision | Why needed | Affected feature | Suggested question for client |
|----------|------------|------------------|-------------------------------|
| **Partial credit notes** | API supports per-line amounts; UI credits full remaining. Period looks like proration but is not. | Credit Notes workspace | “When you credit a funder, do you need to credit part of a line (e.g. 7 days of a monthly invoice), or only whole invoices / whole lines?” |
| **Invoice template scoping** | Resolver already matches home/authority/company; create UI only makes category defaults. Contract `invoiceTemplateId` is stored and **not used in billing**. | Invoice Templates; Funding Contracts; Billing | “Do you need different invoice layouts per home or funder, or is one template per invoice category enough? Should a contract be allowed to override the template, knowing billing does not do that today?” |
| **Invoice email recipient strategy** | Send uses template `ContactEmail`, then funding authority `Email`. Care home and client emails are **not** on that path. | Invoice Email; Invoice Templates; Funding Authorities; Care Homes | “Who should receive invoice email: a template contact, the funder’s billing email, the care home, the resident/family, or a choice at send time?” |
| **Logo support** | `LogoPath` columns exist on care home / tenant / templates; no upload UI or traced upload API. | PDFs, organisation branding | “Is organisation/care-home logo on invoices required for v1, or is name + bank block enough?” |
| **Contract editing** | Create-only UI; PUT exists. Invoiced periods make start-date edits dangerous. | Funding contracts | “After invoices exist, which contract changes must staff do themselves (end date, close, rate add) versus request from support?” |
| **User email immutability** | Update DTO cannot change email; username stability. | Users | “If someone types the wrong email on create, is ‘deactivate and create a new user’ acceptable, or must email be editable?” |
| **User role / home editing** | Create + deactivate only. LocationManager scoping is security-relevant. | Users; care-home access | “Who is allowed to change a user’s role and assigned homes after create? Must LocationManagers always have at least one home (API already requires this)?” |
| **Report filters** | API has dimensions; UI has From/To. | Reports | “Which report filters do finance and home managers need daily (home, funder, resident, category), and which can stay as export-from-API later?” |

Related decisions already listed for the client in A-to-Z §23 (billing rules, Sage mapping, payment workflows) remain in force and are **outside** this field-consistency plan.

---

## 7. Minimum fix set

Smallest practical set. No unnecessary improvements.

### A. Safe client demo

**Code:** none from this audit.

**Must do:**

1. Complete rehearsal data (`../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md` Parts 2–11 / `../demo/DEMO_DATA_ENTRY_CHECKLIST.md`).
2. Rehearse Credit Notes Preview; lock the live narrative to the Credit column.
3. Presenters use A-to-Z + runbook + checklist; treat prep-guide “link template” and “£600 via UI” as outdated.
4. Do not add a Care Home Email field.

**Optional non-code:** one-line presenter note if asked about care home email: “It’s on the care home form; this list is operational (code, name, company, beds).”

### B. Safe controlled pilot

On top of demo safety:

1. **User edit** UI → existing `PUT /api/users/{id}` (no DB change).
2. **Admin reset password** UI → existing `POST /api/users/{id}/reset-password` (no DB change).
3. **Funding contract edit** UI (end date + status at minimum) → existing `PUT /api/funding-contracts/{id}` (no DB change).
4. **Invoice template edit / deactivate** for the fields the create form already sends (no scoping unless Decision 2 says yes).
5. Credit-note **copy**: period does not prorate.
6. Organisation settings **numeric bounds** in the UI.

**Do not require for a tightly supervised pilot:** partial credit UI, report/Sage extra filters, logos, care home contact columns, template scoping.

**Still required by the production plan (not this audit):** billing/Sage sign-off, secrets, backups, restore drill, SMTP decision.

### C. Full production

On top of pilot:

1. Implement whatever Decisions 1–2, 3, 8 require (partial credits, template scoping **or** documented category-default-only, recipient strategy, report filters).
2. If SMTP is live: expose template email subject/body (FormGroup already has defaults).
3. If Sage is live: expose Sage filters **after** mapping sign-off.
4. All P0 items in `PRODUCTION_LAUNCH_READINESS_REPORT.md` / `PRODUCTION_READINESS_ACTION_PLAN.md`.

Do **not** implement logos, credit day-proration, or a second care home email field unless the client asks.

---

## 8. Implementation order

Do **not** start these until this plan is accepted. Sequence is for when implementation is authorised.

### Phase 1 — Demo-critical (no product code)

| Item | Priority | Affected files / components | Backend? | Frontend? | Database? | Business decision? | Complexity | Dependencies | Risk if deferred |
|------|----------|------------------------------|----------|-----------|-----------|--------------------|------------|--------------|------------------|
| Complete Demo Care Group data | A | Operator process; not a code change | No | No | Data only (via UI) | No | M (operator time) | Healthy Docker stack | **Demo cannot run** (current dry-run) |
| Lock credit-note live narrative | A | Rehearsal; optional later doc tidy of prep guide | No | No | No | No for demo path | S | INV-0002 exists | Client sees £2,657.14 presented as £600 |
| Brief operators: no duplicate Care Home Email; prep guide template-link is wrong | A | Operator briefing | No | No | No | No | S | This plan | Wasted build / confused rehearsal |

### Phase 2 — Operational administration

| Item | Priority | Affected files / components | Backend? | Frontend? | Database? | Business decision? | Complexity | Dependencies | Risk if deferred |
|------|----------|------------------------------|----------|-----------|-----------|--------------------|------------|--------------|------------------|
| User edit | B/C | `user-list.ts` / `user-list.html`; `UsersController.Update`; `UpdateUserRequest` | No new API | **Yes** | No | Role/home rules (Decision 7); email stays read-only (Decision 6) | M | None | Wrong access cannot be corrected in UI |
| Admin reset password | B/C | `user-list` (+ dialog); `POST .../reset-password` | No new API | **Yes** | No | Admin-set vs email-token (Decision 6/7) | S–M | User edit UX can share the page | Pilot lockouts need scripts |
| Funding contract edit | B/C | `client-profile.ts` / `.html`; `PUT /api/funding-contracts/{id}` | No new API for basic edit | **Yes** | No | Allowed edit set (Decision 5) | M | Do **not** add `invoiceTemplateId` until Decision 2 | Cannot close/correct contracts |
| Invoice template edit / deactivate | B/C | `invoice-template-list.ts` / `.html`; existing PUT | No new API | **Yes** | No | Scoping later (Decision 2) | M | None | Wrong bank/contact frozen on future invoices (already-issued PDFs stay snapshotted) |
| Org settings numeric bounds | B | `organisation-settings.ts` / `.html` | No | **Yes** | No | No | S | None | API 400 on invalid save; low |

### Phase 3 — Financial workflow completeness

| Item | Priority | Affected files / components | Backend? | Frontend? | Database? | Business decision? | Complexity | Dependencies | Risk if deferred |
|------|----------|------------------------------|----------|-----------|-----------|--------------------|------------|--------------|------------------|
| Credit-note period helper text | B | `credit-note-workspace.html` | No | **Yes** | No | No | S | None | Operators assume proration |
| Partial credit amounts | C **if required** | `credit-note-workspace.ts` / `.html`; send `lineAmounts` | No new API | **Yes** | No | **Decision 1** | M | Helper text first | Full-credit mis-operation; API-only partials |
| Template email subject/body fields | C if SMTP live | `invoice-template-list.html` (controls already in FormGroup) | No | **Yes** | No | Copy ownership | S | SMTP decision (production P0-06) | Fixed default email copy |
| Template scoping fields | C if required | template form + lists of authorities/homes/companies | No new API | **Yes** | No | **Decision 2** | M | Template edit | Cannot create home/funder-specific PDFs from UI |
| Honour contract `invoiceTemplateId` in billing **or** hide the field | C only if Decision 2 chooses contract override | `InvoiceTemplateResolver` / `BillingService`; `client-profile` | **Yes if honouring** | Yes if exposing | No | **Decision 2** | M–L | Must not expose picker while billing ignores it | Operators think they selected a template |

### Phase 4 — Production hardening (this audit’s leftovers + known P0s)

| Item | Priority | Affected files / components | Backend? | Frontend? | Database? | Business decision? | Complexity | Dependencies | Risk if deferred |
|------|----------|------------------------------|----------|-----------|-----------|--------------------|------------|--------------|------------------|
| Care home contact display | D / usability | `care-home-list.html`; `care-home-dashboard.html` (`home()` already loaded) | No | **Yes** | No | No | S | None | Verify via Edit |
| Reports extra filters | D until Decision 8 | `reports.ts` / `.html`; existing query params | No | **Yes** | No | **Decision 8** | M | Client list of needed filters | Broader-than-needed report grids |
| Sage extra filters | D until Sage in scope | `sage-export.ts` / `.html`; `SageExportRequest` | No | **Yes** | No | Sage mapping sign-off | S–M | Production P0-02 | Over-broad export |
| Invoice recipient strategy | C if changing | `BillingService` recipient assignment; possibly send UI | **Maybe** | Maybe | Maybe later | **Decision 3** | M–L | Do not start without answer | Emails go to template/authority, not home/client |
| Logos | D | New upload API + document store + PDF | **Yes** | **Yes** | Path columns exist; upload not exposed | **Decision 4** | L | Storage/backup | PDFs stay text-branded |
| Production P0 ops (secrets, backup, restore, SMTP, billing sign-off) | C | See `PRODUCTION_READINESS_ACTION_PLAN.md` | Ops | Ops | Ops | Finance sign-off | L | Outside this audit | **Do not live-invoice funders** |

Approximate complexity: **S** = hours; **M** = 1–3 days; **L** = multi-day / new capability.

---

## 9. Special check — Care Home Email

| # | Statement | Verified |
|---|-----------|----------|
| 1 | Care Home Email already exists in create UI | **[x]** `care-home-form.html` Email `formControlName="email"` |
| 2 | Care Home Email already exists in edit UI | **[x]** Same form; `patchValue({ email: careHome.email ?? '' })` |
| 3 | Care Home Email is sent in API payload | **[x]** `save()` request includes `email` (and `managerEmail`) |
| 4 | Care Home Email is persisted | **[x]** Create/Update DTOs + `CareHomesController` → `CareHomeLocation.Email` |
| 5 | Care Home Email is returned | **[x]** `CareHomeDto` / `CareHomeLocation.email` |
| 6 | Care Home Email is missing only from list/dashboard display | **[x]** List and dashboard omit it; form shows it |

All six are true.

**Do not add another Care Home Email field.**

---

## 10. Final recommendation

### CURRENT STATE

The original Care Home Email concern is **closed as a create/edit gap**. Persistence works. The remaining product theme is **secondary-screen completeness**: list/dashboard display, and workflow UIs that implement a subset of existing APIs (users, contracts, templates, partial credits, extra filters).

**Client demo (field-consistency / product walkthrough):** the A-to-Z path is implementable in the current UI if rehearsal data exists and the credit-note amount is not invented.

**Client demo (environment):** dry-run 16 September found **no Demo Care Group dataset**. That is the actual demo blocker, not a missing Email input.

### CLIENT DEMO: 🟡

Walkthrough is product-capable. Remaining risks: missing rehearsal data; credit-note £600 mis-claim if Preview is ignored; leftover prep-guide inaccuracies. **Not 🔴** for field gaps. **Not 🟢** until data is loaded and the credit narrative is rehearsed.

### CONTROLLED PILOT: 🟡

Core billing → invoice → PDF → payment flag → reports → audit can run with trained operators. **User / contract / template maintenance** is create-or-API-only. Partial credits are API-only. Acceptable only as a **supervised** pilot with those workarounds, plus production-plan P0 gates (sign-off, backup, SMTP).

### FULL PRODUCTION: 🔴

Not ready for unsupervised live funder invoicing from this audit **or** from the production launch reports: cannot administer users/contracts/templates in UI; partial credits missing if required; recipient/template/logo decisions open; billing/Sage sign-off, secrets, backup, and restore still outstanding.

---

### NEXT ACTION

**Complete Demo Care Group rehearsal data using `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md` Parts 2–11 (or `../demo/DEMO_DATA_ENTRY_CHECKLIST.md`), and lock the live credit-note story to the Preview Credit column — do not add a Care Home Email field and do not start P1 UI builds until that rehearsal exists.**

---

*End of action plan. No code was modified.*

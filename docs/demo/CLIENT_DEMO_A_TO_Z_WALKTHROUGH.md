# Client Demo A–Z Walkthrough

**Who this is for:** Someone who has **never used the application before**. You will open each screen and **enter values field by field** in the order below — the same way you would teach a new colleague.

**Two phases (read this first)**

| Phase | When | What you do |
|-------|------|-------------|
| **Phase 1 — Rehearsal (data entry)** | Days before the client, or on an empty tenant | Follow **Steps 1–72** and type every value. Allow **60–90 minutes**. |
| **Phase 2 — Client demonstration** | Client is watching | Follow **Steps 73–95** to **show** the story (login → dashboard → residents → invoices → reports). **Do not** re-enter master data unless something is missing. |

**If demo data already exists** (your Docker volumes were prepared earlier):

- On each Phase 1 step, read **Skip if exists** — verify the values, then go to the next step.
- **Do not** create a second company, resident, or invoice.
- **Do not** run `docker compose down -v` (wipes all data).
- For the client session, start at **Phase 2, Step 73**.

**Environment:** Local Docker Compose · Development · simulated email · **not** Azure · **not** Production.

| URL | Purpose |
|-----|---------|
| http://localhost:4200/login | Sign in |
| http://localhost:5092/health/live | API alive |
| http://localhost:5092/health/ready | API + SQL ready |

**Password placeholders (never show on screen):** `<DEMO_PLATFORM_ADMIN_PASSWORD>`, `<DEMO_TENANT_ADMIN_PASSWORD>`, `<DEMO_VIEWER_PASSWORD>`.

---

## How the UI works (read before Phase 1)

Use this map so you know **why** each area exists, not only **where** to click.

### Side navigation — what each section is for

| Section | Purpose in this demo |
|---------|----------------------|
| **Operations** | Legal and physical structure: **Companies**, **Care Homes**, and **Residents (Clients)**. Master data for who lives where before any billing. |
| **Billing Setup** | Reference data for finance: **Funding Authorities** (who pays), **Nominal Codes** (ledger codes), **Invoice Categories** (line types), **Invoice Templates** (PDF wording and bank block). |
| **Billing** | Day-to-day revenue cycle: **Billing Workspace** (calculate charges), **Invoices**, **Credit Notes**, **Payments**, **Collections**, **Disputes**, **Remittances**. |
| **Revenue Cycle** | Operational follow-up on money already billed (work queues — lightly used in this walkthrough). |
| **Revenue Assurance** | Rules and renewals — data quality and contract end dates (not the main demo path). |
| **Reporting** | **Reports** and **Sage Export** for management reconciliation and finance export. |
| **Administration** | **Users**, **Audit**, and **Organisation Settings** (numbering, terms, currency). |

Platform operators (Step 1) only see **Organisations** until they impersonate or sign out.

### Appearance — light and dark mode

After sign-in, open the **user chip** (top right) → **Appearance** → **Light mode** or **Dark mode**. The choice is saved in your browser (`localStorage`). Use **light mode** on a projector; **dark mode** is fine for rehearsal on your own screen.

### Dates — calendar picker

All date fields use a **calendar icon** next to the field. Click the field or icon, pick the day in the popup, and the system stores **`yyyy-MM-dd`** (same values as in the tables below). You do not need to type dates manually unless you prefer to.

**Using calendar (example):** for `2026-04-01`, open the picker → navigate to **April 2026** → select **1**.

---
## Safety — never during client demo

- `docker compose down -v` — wipes database and PDFs  
- Void invoices, archive clients, mark INV-0001 **Mark unpaid**  
- Generate invoices or credit notes when preview is wrong  
- Generate a credit note to “make £600 work” — Preview only in Phase 2  
- Expose passwords, `.env`, SQL, DevTools, or claim real email delivery  

---

## Part 0 — Environment (every session)

### Step 0.1 — Docker

**Go to:** Repository root (`docker-compose.yml`).

**Run:**

```powershell
cd C:\Users\HP\Downloads\CarehomeSystem
docker compose ps
```

**You should see:** `carehome-sql` and `carehome-api` **healthy**; `carehome-web` **Up**.

**STOP if:** Containers down → `docker compose up -d --build` ( **never** `down -v` if data exists).

### Step 0.2 — Health

```powershell
Invoke-RestMethod http://localhost:5092/health/live
Invoke-RestMethod http://localhost:5092/health/ready
```

**You should see:** `Healthy`.

### Step 0.3 — Login page

**Go to:** http://localhost:4200/login  

**You should see:** **Care Home Back Office**, **Email address**, **Password**, **Sign in**.

**What this step is for:** Confirms the web app is reachable before you sign in. Appearance (dark mode) is available **after** sign-in from the user menu.

---

# Phase 1 — Rehearsal: enter demo data (new user)

Complete **Demo Care Group** only. Never create residents in **Existing Organisation** (migration placeholder).

---

## Step 1 — Platform sign-in

### What this step is for

Sign in as the **platform administrator** who can create tenant organisations. This account does not run the day-to-day demo — it only sets up **Demo Care Group**.

### Fields and actions

**Go to:** http://localhost:4200/login  

**Click:** **Email address**  

**Enter:** `admin@localhost` — platform operator identity.  

**Click:** **Password**  

**Enter:** `<DEMO_PLATFORM_ADMIN_PASSWORD>`  

**Save/Action:** **Sign in**  

**You should see:** Side nav with **Organisations** only (no Dashboard / Clients).  

**Check before continuing:** You are PlatformAdmin, not TenantAdmin.  

**What to say to the client:** *(Phase 1 only — say nothing; this is setup.)*  

**STOP if:** You need the main demo but only have TenantAdmin — skip to Step 8 if org already exists.

---

## Step 2 — Open add organisation

### What this step is for

Open the form that creates a **new tenant** (isolated care group) with its first administrator.

**Go to:** http://localhost:4200/platform/tenants  

**Click:** **Add organisation**  

**Save/Action:** Open form  

**You should see:** Page title **Add organisation**.  

**Skip if exists:** **Demo Care Group** is already in the list and **Active** → jump to **Step 4**.

---

## Step 3 — Create Demo Care Group (every field)

### What this step is for

Create the **tenant** used for the entire demo: legal identity, contact details, and the first **TenantAdmin** login.

**Go to:** **Add organisation**  

**Enter / select:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Name** | `Demo Care Group` | Legal / registered name of the care group (tenant). |
| **Trading name** | `Demo Care Group` | Name shown on correspondence if different from legal name. |
| **Registration number** | *(leave blank)* | Optional company registration — not needed for demo. |
| **Address** | `1 Demo Lane, Anytown, AN1 2BC` | Head office address for the organisation record. |
| **Phone** | `01234 567890` | Main contact phone for the group. |
| **Email** | `info@demo-care-group.example` | General organisation email (not the admin login). |
| **Website** | *(leave blank)* | Optional web address. |
| **Active** | ✓ checked | Inactive tenants cannot sign in or bill. |
| **Admin email** | `demo-admin@example.com` *(or your chosen `<DEMO_TENANT_ADMIN_EMAIL>`)* | Login for the person who runs Phase 1 and Phase 2 demo. |
| **Admin display name** | `Demo Administrator` | Name shown in the UI and audit trail for that user. |

**Save/Action:** **Save**  

**You should see:** **Organisation created**; Development banner **Email is simulated in Development. Temporary password:** …  

**Check before continuing:** Copy temporary password to password manager (**never** show in client demo).  

**Click:** **Back to organisations**  

**STOP if:** Save fails — check API health.

---

## Step 4 — Deactivate Existing Organisation

### What this step is for

Hide the seeded **migration placeholder** tenant so demo data is only created under **Demo Care Group**.

**Go to:** http://localhost:4200/platform/tenants  

**Click:** **Edit** on **Existing Organisation**  

**Enter:** Uncheck **Active** — prevents accidental use of the wrong tenant.  

**Save/Action:** **Save**  

**You should see:** **Existing Organisation** **Inactive** in list.  

**Skip if exists:** Already inactive — continue.

---

## Step 5 — Sign out PlatformAdmin

### What this step is for

Leave the platform account so you can sign in as the new **TenantAdmin**.

**Go to:** Any  

**Click:** User menu → **Sign out**  

**You should see:** Login page.

---

## Step 6 — TenantAdmin first sign-in (temporary password)

### What this step is for

First login for the care group administrator using the **temporary password** from Step 3.

**Go to:** http://localhost:4200/login  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Email address** | Same as **Admin email** in Step 3 | Tenant-scoped user identity. |
| **Password** | Temporary password from Step 3 | One-time password until changed in Step 7. |

**Save/Action:** **Sign in**  

**You should see:** **Set your password** (`/change-password`).  

**Skip if exists:** Login goes straight to Dashboard → jump to **Step 8**.

---

## Step 7 — Set permanent TenantAdmin password

### What this step is for

Replace the temporary password with the credential you will use for rehearsal and client demo.

**Go to:** `/change-password`  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Temporary password** | From Step 3 | Proves you received the simulated welcome email flow. |
| **New password** | `<DEMO_TENANT_ADMIN_PASSWORD>` | Your long-term demo password (store in password manager). |
| **Confirm new password** | Same as new password | Prevents typos locking you out. |

**Save/Action:** **Save password and continue**  

**You should see:** Full shell; side nav subtitle **Demo Care Group**.  

**Check before continuing:** `/companies` loads (not 403).

---

## Step 8 — Confirm organisation billing settings

### What this step is for

Verify **invoice numbering**, **credit note numbering**, **payment terms**, and **currency** for the tenant before billing.

**Go to:** **Administration** → **Organisation Settings** (`/settings/organisation`)  

**Verify only (do not change unless wrong):**

| Field | Expected | Purpose |
|-------|----------|---------|
| Name | **Demo Care Group** | Confirms you are in the correct tenant. |
| Invoice prefix | `INV-` | Prefix on invoice numbers (e.g. **INV-0001**). |
| Credit note prefix | `CN-` | Prefix on credit note numbers. |
| Payment terms | 30 days | Default “due by” offset on invoices. |
| Currency | GBP | Currency for all amounts and PDFs. |

**Save/Action:** None  

**Skip if exists:** Values already correct.

---

## Step 9 — Add company

### What this step is for

Add the **legal company** within the care group that will own care homes and appear on invoices.

**Go to:** **Operations** → **Companies** → **Add Company** (`/companies/new`)  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Company name** | `Demo Care Ltd` | Registered/trading company name used in billing scope. |

**Save/Action:** **Save**  

**You should see:** **Demo Care Ltd** on `/companies`.  

**Skip if exists:** Company listed → continue.

---

## Step 10 — Add care home

### What this step is for

Add the **physical home** where residents live — links residents to a site and capacity for reporting.

**Go to:** **Operations** → **Care Homes** → **Add Care Home** (`/care-homes/new`)  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Company** | `Demo Care Ltd` | Which company operates this home. |
| **Care home code** | `RIVER01` | Short code for integrations (e.g. Sage). |
| **Care home name** | `River View House` | Display name on resident and invoice context. |
| **Bed capacity** | `24` | Registered capacity for occupancy reporting. |
| Address, phone, email, manager fields | *(optional — leave blank)* | Optional contact and management details. |

**Save/Action:** **Save**  

**You should see:** **River View House** on `/care-homes`.  

**Skip if exists:** Home listed → continue.

---

## Step 11 — Add funding authority

### What this step is for

Define **who pays** for care (local council in this story) and how they are contacted for billing.

**Go to:** **Billing Setup** → **Funding Authorities** → **Add Authority** (`/funding-authorities/new`)  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Code** | `ATC-COUNCIL` | Short reference code for the funder. |
| **Name** | `Anytown Council` | Name on contracts and invoice context. |
| **Type** | `Council` | Category of funder for reporting. |
| **Contact name** | `Adult Social Care Billing` | Billing contact at the authority. |
| **Phone** | `01234 567001` | Billing phone. |
| **Email** | `billing@anytown-council.example` | Address for simulated invoice email. |
| **Address** | `Adult Social Care, Anytown Council, Civic Centre, Anytown, AN1 1AA` | Postal address on records. |
| **Billing frequency** | `Monthly` | How often the authority expects billing (metadata). |

**Save/Action:** **Save**  

**You should see:** **Anytown Council** in list.  

**Skip if exists:** Authority listed → continue.

---

## Step 12 — Add nominal code

### What this step is for

Map invoice lines to a **general ledger code** for finance export (Sage).

**Go to:** **Billing Setup** → **Nominal Codes** → **Add Nominal Code** (`/nominal-codes/new`)  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Code** | `4000` | Ledger account code. |
| **Name** | `Care income` | Human-readable account name. |
| **Description** | *(optional)* | Extra detail for finance users. |

**Save/Action:** **Save**  

**Skip if exists:** Code `4000` present → continue.

---

## Step 13 — Verify invoice categories (do not recreate)

### What this step is for

Confirm seeded **line types** exist — contracts and billing attach to a category (e.g. **General Care**).

**Go to:** **Billing Setup** → **Invoice Categories** (`/invoice-categories`)  

**You should see:** **General Care** and **Miscellaneous** (seeded).  

**Save/Action:** None.

---

## Step 14 — Add invoice template

### What this step is for

Default **PDF layout and payment instructions** for a category’s invoices.

**Go to:** **Billing Setup** → **Invoice Templates** (`/invoice-templates`)  

**Scroll to:** **Add category default template**  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Name** | `General Care Template` | Label for this template in the list. |
| **Category** | `General Care` | Which invoice category uses this layout. |
| **Header** | *(optional)* | Optional text at top of PDF. |
| **Footer** | `Payment due within 30 days. Bank details are fictional for demonstration purposes only.` | Legal/payment text at bottom of PDF. |
| **Bank account** | `Demo Care Group Client Account` | Account name on invoice. |
| **Sort code** | `00-00-00` | Fictional sort code for demo. |
| **Account number** | `00000000` | Fictional account number for demo. |
| **Contact name** | `Demo Finance Team` | Who to contact about payment. |
| **Contact email** | `finance@demo-care-group.example` | Finance contact email on PDF. |

**Save/Action:** **Save template**  

**You should see:** Template in table, status **Active**.  

**Skip if exists:** Template listed → continue.

---

## Step 15 — Add client Alex Morgan

### What this step is for

Create the **first demo resident** — identity, placement, and admission date drive all later billing.

**Go to:** **Operations** → **Clients** → **Add client** (`/clients/new`)  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Care home** | `Demo Care Ltd - River View House` | Where the resident is placed. |
| **Sage ID** | `DEMO001` | External finance system identifier. |
| **Client reference number** | `RVH-001` | Your internal reference for this resident. |
| **Title** | `Ms` | Salutation on records and PDFs. |
| **First name** | `Alex` | Given name. |
| **Last name** | `Morgan` | Family name. |
| **Date of birth** | `1948-03-12` | Optional demographic; calendar → **12 Mar 1948**. |
| **Care type** | `Residential` | Level of care for reporting and contracts. |
| **Admission date** | `2026-04-01` | Start of placement; calendar → **1 Apr 2026**. |
| **Email address** | `alex.morgan@example.com` | Contact email (fictional). |
| **Phone** | `07700 900101` | Contact phone (fictional). |
| **Notes** | `Previous address (fictional): 14 Willow Close, Anytown, AN1 3DE. Demo resident — not real.` | Free text for staff context. |

**Save/Action:** **Save**  

**You should see:** Alex profile at `/clients/{id}`, status **Current**.  

**Skip if exists:** **Alex Morgan** / **RVH-001** in list → open profile, continue at Step 16.

---

## Step 16 — Alex funding contract

### What this step is for

Link Alex to **Anytown Council** paying for **General Care**, with ledger code and active dates.

**Go to:** Alex profile → tab **Funding** → section **Add contract**  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Funding authority** | `Anytown Council` | Who pays under this contract. |
| **Invoice category** | `General Care` | Which invoice line type applies. |
| **Nominal code** | `4000` | Ledger code on generated lines. |
| **Start date** | `2026-04-01` | Contract effective start; calendar → **1 Apr 2026**. |
| **End date** | *(leave blank — open-ended)* | Blank = contract still active with no end date. |

**Save/Action:** **Save contract**  

**You should see:** Contract **Active** on card.  

**Skip if exists:** Contract already shown → continue.

---

## Step 17 — Alex weekly rate

### What this step is for

Set the **weekly fee** the billing engine uses for Alex under the council contract.

**Go to:** Same tab → section **Add rate**  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Contract** | `Anytown Council / General Care` | Which contract this price applies to. |
| **Effective from** | `2026-04-01` | Date rate starts; calendar → **1 Apr 2026**. |
| **Effective to** | *(blank)* | Blank = rate still current. |
| **Frequency** | `Weekly` | How often the amount repeats (weekly pro-rata in billing). |
| **Amount** | `575.00` | Weekly fee in GBP before period calculation. |

**Save/Action:** **Add rate**  

**You should see:** **£575.00** / week in **Funding summary**.  

**Skip if exists:** Rate already **575.00** weekly → continue.

**STOP if:** Jordan Blake already exists — you may have wrong order; Alex must be billed **before** Jordan is created for clean INV-0001.

---

## Step 18 — Preview billing for Alex (August)

### What this step is for

Run **Step 1 — Scope** and **Preview billing** to calculate August charges for Alex **before** creating an invoice.

**Go to:** **Billing** → **Billing Workspace** (`/billing`)  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Company** | `Demo Care Ltd` | Legal entity for this billing run. |
| **Care home** | `River View House` | Limit to residents at this home. |
| **Invoice category** | `General Care` | Only contracts in this category. |
| **Period start** | `2026-08-01` | First day of billable period; calendar → **1 Aug 2026**. |
| **Period end** | `2026-08-31` | Last day of period; calendar → **31 Aug 2026**. |

**Save/Action:** **Preview billing**  

**You should see:**

- **Step 2 — Preview:** **1** resident line — **Alex Morgan** — **£2,546.43**
- Banner **Ready to generate — review the lines below.**

**Check before continuing:** Total **£2,546.43**; **only Alex** (Jordan must not exist yet).  

**STOP if:** Wrong amount or wrong residents — fix contracts/rates; **do not** click **Generate invoices**.

---

## Step 19 — Generate INV-0001

### What this step is for

Turn the approved preview into a **posted invoice record** and PDF (**INV-0001**).

**Go to:** Billing workspace (same preview)  

**Save/Action:** **Generate invoices** — commits invoice lines; cannot silently undo in demo.  

**You should see:** Green banner `Generated 1 invoice(s). Total £2,546.43.`  

**Skip if exists:** **INV-0001** already on `/invoices` with **£2,546.43** → do **not** generate again.

---

## Step 20 — Verify INV-0001

### What this step is for

Confirm the invoice total, period, and PDF match what you will show the client.

**Go to:** **Billing** → **Invoices** → open **INV-0001**  

**You should see:** Total **£2,546.43**, period Aug 2026, Alex, payment **Not paid** (until Step 26).  

**Save/Action:** **Download PDF** — confirm PDF opens and total matches.

---

## Step 21 — Add client Jordan Blake

### What this step is for

Add the **second resident** with a later admission date — used to show separate August billing and outstanding debt.

**Go to:** **Clients** → **Add client**  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Care home** | `Demo Care Ltd - River View House` | Same home as Alex. |
| **Sage ID** | `DEMO002` | Second finance identifier. |
| **Client reference number** | `RVH-002` | Internal reference for Jordan. |
| **Title** | `Mr` | Salutation. |
| **First name** | `Jordan` | Given name. |
| **Last name** | `Blake` | Family name. |
| **Date of birth** | `1952-07-22` | Calendar → **22 Jul 1952**. |
| **Care type** | `Nursing` | Different care type from Alex. |
| **Admission date** | `2026-05-15` | Later admission; calendar → **15 May 2026**. |
| **Email address** | `jordan.blake@example.com` | Fictional contact email. |
| **Phone** | `07700 900102` | Fictional contact phone. |
| **Notes** | `Previous address (fictional): 8 Meadow Lane, Anytown, AN1 4FG. Demo resident — not real.` | Staff notes. |

**Save/Action:** **Save**  

**Skip if exists:** Jordan in list → Step 22.

---

## Step 22 — Jordan contract and rate

### What this step is for

Give Jordan the same funder and category as Alex, with a **£600/week** rate from admission.

**Go to:** Jordan profile → **Funding** → **Add contract**  

| Field | Value | Purpose |
|-------|-------|---------|
| **Funding authority** | `Anytown Council` | Paying authority. |
| **Invoice category** | `General Care` | Line type for invoices. |
| **Nominal code** | `4000` | Ledger code. |
| **Start date** | `2026-05-15` | Calendar → **15 May 2026**. |
| **End date** | *(blank)* | Open-ended contract. |

**Save/Action:** **Save contract**  

**Add rate:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Contract** | `Anytown Council / General Care` | Contract being priced. |
| **Effective from** | `2026-05-15` | Calendar → **15 May 2026**. |
| **Frequency** | `Weekly` | Billing frequency. |
| **Amount** | `600.00` | Weekly fee (higher than Alex for demo contrast). |

**Save/Action:** **Add rate**  

**You should see:** **£600.00** / week.

---

## Step 23 — Preview billing for Jordan (August)

### What this step is for

Preview August billing again — should bill **Jordan only** because Alex is already invoiced.

**Go to:** `/billing` — same company, home, category, period as Step 18  

**Using calendar:** **Period start** **1 Aug 2026**, **Period end** **31 Aug 2026** (same as Step 18).  

**Save/Action:** **Preview billing**  

**You should see:**

- **Jordan Blake** only, **£2,657.14**
- Alex skipped (already billed) — see **Coverage detail** or zero lines for Alex
- **Ready to generate**

**STOP if:** Alex appears for new charges or total wrong — **do not** generate.

---

## Step 24 — Generate INV-0002

### What this step is for

Create Jordan’s August invoice (**INV-0002**) from the preview.

**Save/Action:** **Generate invoices**  

**You should see:** `Generated 1 invoice(s). Total £2,657.14.`  

**Skip if exists:** **INV-0002** present → skip generate.

---

## Step 25 — Verify INV-0002 PDF

### What this step is for

Confirm Jordan’s PDF total before the client session.

**Go to:** **INV-0002** → **Download PDF**  

**You should see:** **£2,657.14**, Jordan, August 2026.

---

## Step 26 — Mark INV-0001 paid (rehearsal)

### What this step is for

Practice **payment status** so Phase 2 can show one paid and one outstanding invoice.

**Go to:** **INV-0001**  

**Click:** **Mark paid** — manual cash receipt tracking (not bank feed).  

**You should see:** Toast **Payment status updated.** Payment badge **Paid**.  

**Skip if exists:** Already **Paid** — do not click **Mark unpaid**.

---

## Step 27 — Rehearse simulated email on INV-0002

### What this step is for

Practice the **Email** action and the required “simulated email” narrative.

**Go to:** **INV-0002**  

**Click:** **Email**  

**You should see:** **Invoice email simulated successfully in this demonstration environment.**  

**What to say (practice):** “This environment uses simulated email — workflow only, not real delivery.”

---

## Step 28 — Credit note preview (rehearsal — do not generate)

### What this step is for

See how **credit notes** preview from an invoice — stop at Preview in client demo.

**Go to:** **INV-0002** → **Credit note**  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **Reason** | `Partial period adjustment — demo scenario` | Audit text explaining why credit is requested. |
| Period / resident | Prefilled from invoice | Scope of the credit (from original invoice). |

**Save/Action:** **Preview** only (not **Generate credit note**)  

**You should see:** **Preview amount** typically **£2,657.14** (full remaining line — not £600).  

**STOP if:** You expected £600 — UI cannot partial-credit without extra capability; narrate actual Preview in Phase 2.

---

## Step 29 — Rehearse reports

### What this step is for

Confirm **outstanding** and **invoices by resident** reports match your invoice data.

**Go to:** **Reporting** → **Reports**  

**Report:** **Payment status / outstanding** → **Run report** → **INV-0002** only.  

**Report:** **Invoices by client**  

**Enter:** **From** `2026-08-01`, **To** `2026-08-31` — use calendar for August 2026 range.  

**Save/Action:** **Run report** → both invoices with correct amounts.

---

## Step 30 — Rehearse audit

### What this step is for

Verify **who did what** is logged for compliance narrative in Phase 2.

**Go to:** **Administration** → **Audit**  

**You should see:** Recent invoice, payment, email, and setup events.

---

### Phase 1 complete

You now have the full dataset. Before the client: close terminals, hide passwords, optionally sign out.

---

# Phase 2 — Client demonstration (show the system)

**Prerequisite:** Phase 1 complete **or** data verified via **Skip if exists**.  

**Do not** type master data in front of the client unless you agreed a “live build” demo.

Each step uses the same structure.

---

## Step 73 — Open login for the client

### What this step is for

Show the **tenant login** the client’s staff would use daily.

**Go to:** http://localhost:4200/login  

**Enter:** `<DEMO_TENANT_ADMIN_EMAIL>` / `<DEMO_TENANT_ADMIN_PASSWORD>`  

**Save/Action:** **Sign in**  

**You should see:** **Dashboard**, subtitle **Demo Care Group**.  

**What to say to the client:**  
“This is the secure entry point for finance and operations staff.”  

**STOP if:** Wrong organisation or 403.

---

## Step 74 — Dashboard overview

### What this step is for

Orient the client: KPIs, recent billing, and optional **Appearance** (light/dark) from the user menu.

**Optional (30 seconds):** User chip → **Appearance** → show **Dark mode**, explain personal preference and saved setting → switch back to **Light mode** for projector if needed.

**Go to:** `/dashboard`  

**Click:** Point at KPI cards and **Recent invoices**  

**You should see:** Residents, outstanding count, **INV-0001** / **INV-0002** in recent list.  

**What to say to the client:**  
“This is the operational overview — homes, residents, outstanding money, and recent billing activity in one place.”

---

## Step 75 — Organisation settings (show, don’t edit)

### What this step is for

Show **tenant-wide billing defaults** without changing live settings during the demo.

**Go to:** **Administration** → **Organisation Settings**  

**You should see:** **Demo Care Group**, **INV-** prefix, 30-day terms, GBP.  

**What to say to the client:**  
“Each care group has isolated settings for numbering and payment terms.”  

**Save/Action:** None.

---

## Step 76 — Company and care home

### What this step is for

Show how **company → care home** hierarchy appears in operations and on invoices.

**Go to:** **Companies** → show **Demo Care Ltd**  

**Go to:** **Care Homes** → show **River View House**, capacity **24**  

**What to say to the client:**  
“The company and home structure drives what appears on invoices.”

---

## Step 77 — Open Alex Morgan

### What this step is for

Walk through a **resident record**: placement, funding summary, and contract/rate history.

**Go to:** **Clients** → **Open** **Alex Morgan**  

**You should see:** **RVH-001**, **River View House**, **Funding summary** **£575.00**/week.  

**Click:** Tab **Funding** — show contract and rate history.  

**What to say to the client:**  
“The resident record links placement, funder, and rate — that drives billing.”

---

## Step 78 — Billing preview (Alex already billed)

### What this step is for

Demonstrate **duplicate prevention** — August is already invoiced for Alex.

**Go to:** Alex → **Start billing** (or **Billing** tab → **Open billing workspace**)  

**Enter:** Company **Demo Care Ltd**, home **River View House**, category **General Care**, period **2026-08-01** to **2026-08-31** (calendar: **1** and **31 Aug 2026**).  

**Save/Action:** **Preview billing**  

**You should see:** No new charge for Alex — **already fully billed** message / coverage detail.  

**What to say to the client:**  
“Preview prevents duplicate invoices for the same period.”  

**STOP if:** Preview offers to bill Alex again — do not generate.

---

## Step 79 — Show INV-0001 and PDF

### What this step is for

Show the **formal invoice** and that PDF matches system totals.

**Go to:** **Invoices** → **INV-0001**  

**You should see:** **£2,546.43**, **Paid**  

**Save/Action:** **Download PDF** — walk through header, period, total.  

**What to say to the client:**  
“This is the formal August invoice for Alex; PDF matches the system record.”  

*(If line name shows **Alexa Morgan**, explain snapshot at invoice time.)*

---

## Step 80 — Payment status (verify or demonstrate once)

### What this step is for

Explain **paid vs outstanding** tracking for finance reports.

**Go to:** **INV-0001**  

**If Paid:** Point at badge — explain payment tracking. **Do not** **Mark unpaid**.  

**If Not paid:** Click **Mark paid** once → show **Paid**.  

**What to say to the client:**  
“Finance marks invoices paid for outstanding reporting — not a full bank feed.”

---

## Step 81 — Jordan profile and funding

### What this step is for

Contrast **second resident**, later admission, and **£600/week** rate.

**Go to:** **Clients** → **Jordan Blake** → **Funding**  

**You should see:** **RVH-002**, **£600.00**/week from **15 May 2026**.  

**What to say to the client:**  
“Jordan admitted later; August billing uses the active weekly rate for the full month.”

---

## Step 82 — Show INV-0002

### What this step is for

Show **outstanding** invoice for the second resident in the same period.

**Go to:** **INV-0002**  

**You should see:** **£2,657.14**, **Not paid**  

**Save/Action:** **Download PDF**  

**What to say to the client:**  
“Second resident, same period, separate invoice — still outstanding in the demo.”

---

## Step 83 — Simulated email

### What this step is for

Show invoice **dispatch workflow** with mandatory simulated-email disclaimer.

**Go to:** **INV-0002**  

**Save/Action:** **Email**  

**You should see:** Simulated success banner.  

**What to say to the client (required):**  
“This demonstration environment uses simulated email, so this confirms the application workflow without sending a real customer email.”  

**Do not say:** “Email has been delivered.”

---

## Step 84 — Credit note preview from invoice

### What this step is for

Show **adjustment workflow** without posting a credit in front of the client.

**Go to:** **INV-0002** → **Credit note**  

**Enter:** **Reason** — e.g. `Partial period adjustment — demo scenario`  

**Save/Action:** **Preview** only  

**You should see:** **Preview amount** (record actual — expect **£2,657.14**).  

**What to say to the client:**  
“The system previews the credit amount; partial-day credits would need a further product decision. We stop at preview in this demo.”  

**STOP if:** Tempted to click **Generate credit note** for show.

---

## Step 85 — Outstanding report

### What this step is for

Management view of **unpaid invoices** for collection.

**Go to:** **Reports** → **Payment status / outstanding** → **Run report**  

**You should see:** **INV-0002**; not INV-0001.  

**What to say to the client:**  
“Unpaid invoices for follow-up and cash collection.”

---

## Step 86 — Invoices by client

### What this step is for

Reconcile **invoices by resident** over a date range.

**Go to:** **Reports** → **Invoices by client**  

**Enter:**

| Field | Value | Purpose |
|-------|-------|---------|
| **From** | `2026-08-01` | Report start; calendar → **1 Aug 2026**. |
| **To** | `2026-08-31` | Report end; calendar → **31 Aug 2026**. |

**Save/Action:** **Run report**  

**You should see:** INV-0001 and INV-0002 with amounts and payment status.  

**What to say to the client:**  
“Reconciliation by resident and period.”

---

## Step 87 — Audit trail

### What this step is for

Demonstrate **compliance and accountability** — immutable activity log.

**Go to:** **Administration** → **Audit**  

**You should see:** Invoice, payment, email, and setup actions.  

**What to say to the client:**  
“An append-only record of who did what and when.”

---

## Step 88 — Optional ReadOnly

### What this step is for

Show **role-based access** if a read-only demo user exists.

**Only if** `demo-viewer@example.com` exists: **Sign out** → sign in as ReadOnly → show read-only access → **Sign out** → TenantAdmin again.  

**Otherwise:** Skip.

---

## Step 89 — Closing story

### What this step is for

Tie the demo back to the **end-to-end revenue cycle** in one narrative chain.

**What to say to the client (short chain):**  
Organisation → company → care home → resident → funding & rate → billing preview → invoice → PDF → payment → credit preview → reports → audit.

---

# If something goes wrong

| Problem | What to do |
|---------|------------|
| Login fails | Check email/password, Docker health; **no** `down -v` |
| Duplicate invoice risk | **Preview billing** first; never generate if August already billed |
| PDF fails | Retry once; check `/health/ready` |
| Email error | Stay on Development simulation |
| Credit preview not £600 | Show actual amount; Preview only |
| Missing data | Complete Phase 1 steps with **Skip if exists** |

---

# A–Z final checklist

- [ ] Docker / API / login OK  
- [ ] Phase 1 entered OR verified (Demo Care Group, Demo Care Ltd, River View House)  
- [ ] Alex **RVH-001** £575/week  
- [ ] Jordan **RVH-002** £600/week  
- [ ] INV-0001 **£2,546.43** **Paid**  
- [ ] INV-0002 **£2,657.14** **Not paid**, PDF OK  
- [ ] Email simulated on INV-0002  
- [ ] Credit **Preview** recorded (no false £600 claim)  
- [ ] Reports + audit rehearsed  
- [ ] Phase 2 narrative ready  
- [ ] No `down -v`, no credentials on screen  
- [ ] Presenter understands: **new user enters in Phase 1; client sees Phase 2**

---

*UI labels match current app: **Funding** tab, **Preview billing**, **Generate invoices**, **Mark paid**, **Credit note** from invoice; **calendar** date fields; **Light mode** / **Dark mode** under user menu → **Appearance**. Demo amounts are illustrative — not finance-approved.*

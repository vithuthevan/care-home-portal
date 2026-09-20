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

---

# Phase 1 — Rehearsal: enter demo data (new user)

Complete **Demo Care Group** only. Never create residents in **Existing Organisation** (migration placeholder).

---

## Step 1 — Platform sign-in

**Go to:** http://localhost:4200/login  

**Click:** **Email address**  

**Enter:** `admin@localhost`  

**Click:** **Password**  

**Enter:** `<DEMO_PLATFORM_ADMIN_PASSWORD>`  

**Save/Action:** **Sign in**  

**You should see:** Side nav with **Organisations** only (no Dashboard / Clients).  

**Check before continuing:** You are PlatformAdmin, not TenantAdmin.  

**What to say to the client:** *(Phase 1 only — say nothing; this is setup.)*  

**STOP if:** You need the main demo but only have TenantAdmin — skip to Step 8 if org already exists.

---

## Step 2 — Open add organisation

**Go to:** http://localhost:4200/platform/tenants  

**Click:** **Add organisation**  

**Save/Action:** Open form  

**You should see:** Page title **Add organisation**.  

**Skip if exists:** **Demo Care Group** is already in the list and **Active** → jump to **Step 4**.

---

## Step 3 — Create Demo Care Group (every field)

**Go to:** **Add organisation**  

**Enter / select:**

| Field | Value |
|-------|-------|
| **Name** | `Demo Care Group` |
| **Trading name** | `Demo Care Group` |
| **Registration number** | *(leave blank)* |
| **Address** | `1 Demo Lane, Anytown, AN1 2BC` |
| **Phone** | `01234 567890` |
| **Email** | `info@demo-care-group.example` |
| **Website** | *(leave blank)* |
| **Active** | ✓ checked |
| **Admin email** | `demo-admin@example.com` *(or your chosen `<DEMO_TENANT_ADMIN_EMAIL>`)* |
| **Admin display name** | `Demo Administrator` |

**Save/Action:** **Save**  

**You should see:** **Organisation created**; Development banner **Email is simulated in Development. Temporary password:** …  

**Check before continuing:** Copy temporary password to password manager (**never** show in client demo).  

**Click:** **Back to organisations**  

**STOP if:** Save fails — check API health.

---

## Step 4 — Deactivate Existing Organisation

**Go to:** http://localhost:4200/platform/tenants  

**Click:** **Edit** on **Existing Organisation**  

**Enter:** Uncheck **Active**  

**Save/Action:** **Save**  

**You should see:** **Existing Organisation** **Inactive** in list.  

**Skip if exists:** Already inactive — continue.

---

## Step 5 — Sign out PlatformAdmin

**Go to:** Any  

**Click:** User menu → **Sign out**  

**You should see:** Login page.

---

## Step 6 — TenantAdmin first sign-in (temporary password)

**Go to:** http://localhost:4200/login  

**Enter:**

| Field | Value |
|-------|-------|
| **Email address** | Same as **Admin email** in Step 3 |
| **Password** | Temporary password from Step 3 |

**Save/Action:** **Sign in**  

**You should see:** **Set your password** (`/change-password`).  

**Skip if exists:** Login goes straight to Dashboard → jump to **Step 8**.

---

## Step 7 — Set permanent TenantAdmin password

**Go to:** `/change-password`  

**Enter:**

| Field | Value |
|-------|-------|
| **Temporary password** | From Step 3 |
| **New password** | `<DEMO_TENANT_ADMIN_PASSWORD>` |
| **Confirm new password** | Same as new password |

**Save/Action:** **Save password and continue**  

**You should see:** Full shell; side nav subtitle **Demo Care Group**.  

**Check before continuing:** `/companies` loads (not 403).

---

## Step 8 — Confirm organisation billing settings

**Go to:** **Administration** → **Organisation Settings** (`/settings/organisation`)  

**Verify only (do not change unless wrong):**

| Field | Expected |
|-------|----------|
| Name | **Demo Care Group** |
| Invoice prefix | `INV-` |
| Credit note prefix | `CN-` |
| Payment terms | 30 days |
| Currency | GBP |

**Save/Action:** None  

**Skip if exists:** Values already correct.

---

## Step 9 — Add company

**Go to:** **Operations** → **Companies** → **Add Company** (`/companies/new`)  

**Enter:**

| Field | Value |
|-------|-------|
| **Company name** | `Demo Care Ltd` |

**Save/Action:** **Save**  

**You should see:** **Demo Care Ltd** on `/companies`.  

**Skip if exists:** Company listed → continue.

---

## Step 10 — Add care home

**Go to:** **Operations** → **Care Homes** → **Add Care Home** (`/care-homes/new`)  

**Enter:**

| Field | Value |
|-------|-------|
| **Company** | `Demo Care Ltd` |
| **Care home code** | `RIVER01` |
| **Care home name** | `River View House` |
| **Bed capacity** | `24` |
| Address, phone, email, manager fields | *(optional — leave blank)* |

**Save/Action:** **Save**  

**You should see:** **River View House** on `/care-homes`.  

**Skip if exists:** Home listed → continue.

---

## Step 11 — Add funding authority

**Go to:** **Billing Setup** → **Funding Authorities** → **Add Authority** (`/funding-authorities/new`)  

**Enter:**

| Field | Value |
|-------|-------|
| **Code** | `ATC-COUNCIL` |
| **Name** | `Anytown Council` |
| **Type** | `Council` |
| **Contact name** | `Adult Social Care Billing` |
| **Phone** | `01234 567001` |
| **Email** | `billing@anytown-council.example` |
| **Address** | `Adult Social Care, Anytown Council, Civic Centre, Anytown, AN1 1AA` |
| **Billing frequency** | `Monthly` |

**Save/Action:** **Save**  

**You should see:** **Anytown Council** in list.  

**Skip if exists:** Authority listed → continue.

---

## Step 12 — Add nominal code

**Go to:** **Billing Setup** → **Nominal Codes** → **Add Nominal Code** (`/nominal-codes/new`)  

**Enter:**

| Field | Value |
|-------|-------|
| **Code** | `4000` |
| **Name** | `Care income` |
| **Description** | *(optional)* |

**Save/Action:** **Save**  

**Skip if exists:** Code `4000` present → continue.

---

## Step 13 — Verify invoice categories (do not recreate)

**Go to:** **Billing Setup** → **Invoice Categories** (`/invoice-categories`)  

**You should see:** **General Care** and **Miscellaneous** (seeded).  

**Save/Action:** None.

---

## Step 14 — Add invoice template

**Go to:** **Billing Setup** → **Invoice Templates** (`/invoice-templates`)  

**Scroll to:** **Add category default template**  

**Enter:**

| Field | Value |
|-------|-------|
| **Name** | `General Care Template` |
| **Category** | `General Care` |
| **Header** | *(optional)* |
| **Footer** | `Payment due within 30 days. Bank details are fictional for demonstration purposes only.` |
| **Bank account** | `Demo Care Group Client Account` |
| **Sort code** | `00-00-00` |
| **Account number** | `00000000` |
| **Contact name** | `Demo Finance Team` |
| **Contact email** | `finance@demo-care-group.example` |

**Save/Action:** **Save template**  

**You should see:** Template in table, status **Active**.  

**Skip if exists:** Template listed → continue.

---

## Step 15 — Add client Alex Morgan

**Go to:** **Operations** → **Clients** → **Add client** (`/clients/new`)  

**Enter:**

| Field | Value |
|-------|-------|
| **Care home** | `Demo Care Ltd - River View House` |
| **Sage ID** | `DEMO001` |
| **Client reference number** | `RVH-001` |
| **Title** | `Ms` |
| **First name** | `Alex` |
| **Last name** | `Morgan` |
| **Date of birth** | `1948-03-12` |
| **Care type** | `Residential` |
| **Admission date** | `2026-04-01` |
| **Email address** | `alex.morgan@example.com` |
| **Phone** | `07700 900101` |
| **Notes** | `Previous address (fictional): 14 Willow Close, Anytown, AN1 3DE. Demo resident — not real.` |

**Save/Action:** **Save**  

**You should see:** Alex profile at `/clients/{id}`, status **Current**.  

**Skip if exists:** **Alex Morgan** / **RVH-001** in list → open profile, continue at Step 16.

---

## Step 16 — Alex funding contract

**Go to:** Alex profile → tab **Funding** → section **Add contract**  

**Enter:**

| Field | Value |
|-------|-------|
| **Funding authority** | `Anytown Council` |
| **Invoice category** | `General Care` |
| **Nominal code** | `4000` |
| **Start date** | `2026-04-01` |
| **End date** | *(leave blank — open-ended)* |

**Save/Action:** **Save contract**  

**You should see:** Contract **Active** on card.  

**Skip if exists:** Contract already shown → continue.

---

## Step 17 — Alex weekly rate

**Go to:** Same tab → section **Add rate**  

**Enter:**

| Field | Value |
|-------|-------|
| **Contract** | `Anytown Council / General Care` |
| **Effective from** | `2026-04-01` |
| **Effective to** | *(blank)* |
| **Frequency** | `Weekly` |
| **Amount** | `575.00` |

**Save/Action:** **Add rate**  

**You should see:** **£575.00** / week in **Funding summary**.  

**Skip if exists:** Rate already **575.00** weekly → continue.

**STOP if:** Jordan Blake already exists — you may have wrong order; Alex must be billed **before** Jordan is created for clean INV-0001.

---

## Step 18 — Preview billing for Alex (August)

**Go to:** **Billing** → **Billing Workspace** (`/billing`)  

**Enter:**

| Field | Value |
|-------|-------|
| **Company** | `Demo Care Ltd` |
| **Care home** | `River View House` |
| **Invoice category** | `General Care` |
| **Period start** | `2026-08-01` |
| **Period end** | `2026-08-31` |

**Save/Action:** **Preview billing**  

**You should see:**

- **Step 2 — Preview:** **1** resident line — **Alex Morgan** — **£2,546.43**
- Banner **Ready to generate — review the lines below.**

**Check before continuing:** Total **£2,546.43**; **only Alex** (Jordan must not exist yet).  

**STOP if:** Wrong amount or wrong residents — fix contracts/rates; **do not** click **Generate invoices**.

---

## Step 19 — Generate INV-0001

**Go to:** Billing workspace (same preview)  

**Save/Action:** **Generate invoices**  

**You should see:** Green banner `Generated 1 invoice(s). Total £2,546.43.`  

**Skip if exists:** **INV-0001** already on `/invoices` with **£2,546.43** → do **not** generate again.

---

## Step 20 — Verify INV-0001

**Go to:** **Billing** → **Invoices** → open **INV-0001**  

**You should see:** Total **£2,546.43**, period Aug 2026, Alex, payment **Not paid** (until Step 26).  

**Save/Action:** **Download PDF** — confirm PDF opens and total matches.

---

## Step 21 — Add client Jordan Blake

**Go to:** **Clients** → **Add client**  

**Enter:**

| Field | Value |
|-------|-------|
| **Care home** | `Demo Care Ltd - River View House` |
| **Sage ID** | `DEMO002` |
| **Client reference number** | `RVH-002` |
| **Title** | `Mr` |
| **First name** | `Jordan` |
| **Last name** | `Blake` |
| **Date of birth** | `1952-07-22` |
| **Care type** | `Nursing` |
| **Admission date** | `2026-05-15` |
| **Email address** | `jordan.blake@example.com` |
| **Phone** | `07700 900102` |
| **Notes** | `Previous address (fictional): 8 Meadow Lane, Anytown, AN1 4FG. Demo resident — not real.` |

**Save/Action:** **Save**  

**Skip if exists:** Jordan in list → Step 22.

---

## Step 22 — Jordan contract and rate

**Go to:** Jordan profile → **Funding** → **Add contract**  

| Field | Value |
|-------|-------|
| **Funding authority** | `Anytown Council` |
| **Invoice category** | `General Care` |
| **Nominal code** | `4000` |
| **Start date** | `2026-05-15` |
| **End date** | *(blank)* |

**Save/Action:** **Save contract**  

**Add rate:**

| Field | Value |
|-------|-------|
| **Contract** | `Anytown Council / General Care` |
| **Effective from** | `2026-05-15` |
| **Frequency** | `Weekly` |
| **Amount** | `600.00` |

**Save/Action:** **Add rate**  

**You should see:** **£600.00** / week.

---

## Step 23 — Preview billing for Jordan (August)

**Go to:** `/billing` — same company, home, category, period as Step 18  

**Save/Action:** **Preview billing**  

**You should see:**

- **Jordan Blake** only, **£2,657.14**
- Alex skipped (already billed) — see **Coverage detail** or zero lines for Alex
- **Ready to generate**

**STOP if:** Alex appears for new charges or total wrong — **do not** generate.

---

## Step 24 — Generate INV-0002

**Save/Action:** **Generate invoices**  

**You should see:** `Generated 1 invoice(s). Total £2,657.14.`  

**Skip if exists:** **INV-0002** present → skip generate.

---

## Step 25 — Verify INV-0002 PDF

**Go to:** **INV-0002** → **Download PDF**  

**You should see:** **£2,657.14**, Jordan, August 2026.

---

## Step 26 — Mark INV-0001 paid (rehearsal)

**Go to:** **INV-0001**  

**Click:** **Mark paid**  

**You should see:** Toast **Payment status updated.** Payment badge **Paid**.  

**Skip if exists:** Already **Paid** — do not click **Mark unpaid**.

---

## Step 27 — Rehearse simulated email on INV-0002

**Go to:** **INV-0002**  

**Click:** **Email**  

**You should see:** **Invoice email simulated successfully in this demonstration environment.**  

**What to say (practice):** “This environment uses simulated email — workflow only, not real delivery.”

---

## Step 28 — Credit note preview (rehearsal — do not generate)

**Go to:** **INV-0002** → **Credit note**  

**Enter:**

| Field | Value |
|-------|-------|
| **Reason** | `Partial period adjustment — demo scenario` |
| Period / resident | Prefilled from invoice |

**Save/Action:** **Preview** only (not **Generate credit note**)  

**You should see:** **Preview amount** typically **£2,657.14** (full remaining line — not £600).  

**STOP if:** You expected £600 — UI cannot partial-credit without extra capability; narrate actual Preview in Phase 2.

---

## Step 29 — Rehearse reports

**Go to:** **Reporting** → **Reports**  

**Report:** **Payment status / outstanding** → **Run report** → **INV-0002** only.  

**Report:** **Invoices by client**  

**Enter:** **From** `2026-08-01`, **To** `2026-08-31`  

**Save/Action:** **Run report** → both invoices with correct amounts.

---

## Step 30 — Rehearse audit

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

**Go to:** http://localhost:4200/login  

**Enter:** `<DEMO_TENANT_ADMIN_EMAIL>` / `<DEMO_TENANT_ADMIN_PASSWORD>`  

**Save/Action:** **Sign in**  

**You should see:** **Dashboard**, subtitle **Demo Care Group**.  

**What to say to the client:**  
“This is the secure entry point for finance and operations staff.”  

**STOP if:** Wrong organisation or 403.

---

## Step 74 — Dashboard overview

**Go to:** `/dashboard`  

**Click:** Point at KPI cards and **Recent invoices**  

**You should see:** Residents, outstanding count, **INV-0001** / **INV-0002** in recent list.  

**What to say to the client:**  
“This is the operational overview — homes, residents, outstanding money, and recent billing activity in one place.”

---

## Step 75 — Organisation settings (show, don’t edit)

**Go to:** **Administration** → **Organisation Settings**  

**You should see:** **Demo Care Group**, **INV-** prefix, 30-day terms, GBP.  

**What to say to the client:**  
“Each care group has isolated settings for numbering and payment terms.”  

**Save/Action:** None.

---

## Step 76 — Company and care home

**Go to:** **Companies** → show **Demo Care Ltd**  

**Go to:** **Care Homes** → show **River View House**, capacity **24**  

**What to say to the client:**  
“The company and home structure drives what appears on invoices.”

---

## Step 77 — Open Alex Morgan

**Go to:** **Clients** → **Open** **Alex Morgan**  

**You should see:** **RVH-001**, **River View House**, **Funding summary** **£575.00**/week.  

**Click:** Tab **Funding** — show contract and rate history.  

**What to say to the client:**  
“The resident record links placement, funder, and rate — that drives billing.”

---

## Step 78 — Billing preview (Alex already billed)

**Go to:** Alex → **Start billing** (or **Billing** tab → **Open billing workspace**)  

**Enter:** Company **Demo Care Ltd**, home **River View House**, category **General Care**, period **2026-08-01** to **2026-08-31**  

**Save/Action:** **Preview billing**  

**You should see:** No new charge for Alex — **already fully billed** message / coverage detail.  

**What to say to the client:**  
“Preview prevents duplicate invoices for the same period.”  

**STOP if:** Preview offers to bill Alex again — do not generate.

---

## Step 79 — Show INV-0001 and PDF

**Go to:** **Invoices** → **INV-0001**  

**You should see:** **£2,546.43**, **Paid**  

**Save/Action:** **Download PDF** — walk through header, period, total.  

**What to say to the client:**  
“This is the formal August invoice for Alex; PDF matches the system record.”  

*(If line name shows **Alexa Morgan**, explain snapshot at invoice time.)*

---

## Step 80 — Payment status (verify or demonstrate once)

**Go to:** **INV-0001**  

**If Paid:** Point at badge — explain payment tracking. **Do not** **Mark unpaid**.  

**If Not paid:** Click **Mark paid** once → show **Paid**.  

**What to say to the client:**  
“Finance marks invoices paid for outstanding reporting — not a full bank feed.”

---

## Step 81 — Jordan profile and funding

**Go to:** **Clients** → **Jordan Blake** → **Funding**  

**You should see:** **RVH-002**, **£600.00**/week from **15 May 2026**.  

**What to say to the client:**  
“Jordan admitted later; August billing uses the active weekly rate for the full month.”

---

## Step 82 — Show INV-0002

**Go to:** **INV-0002**  

**You should see:** **£2,657.14**, **Not paid**  

**Save/Action:** **Download PDF**  

**What to say to the client:**  
“Second resident, same period, separate invoice — still outstanding in the demo.”

---

## Step 83 — Simulated email

**Go to:** **INV-0002**  

**Save/Action:** **Email**  

**You should see:** Simulated success banner.  

**What to say to the client (required):**  
“This demonstration environment uses simulated email, so this confirms the application workflow without sending a real customer email.”  

**Do not say:** “Email has been delivered.”

---

## Step 84 — Credit note preview from invoice

**Go to:** **INV-0002** → **Credit note**  

**Enter:** **Reason** — e.g. `Partial period adjustment — demo scenario`  

**Save/Action:** **Preview** only  

**You should see:** **Preview amount** (record actual — expect **£2,657.14**).  

**What to say to the client:**  
“The system previews the credit amount; partial-day credits would need a further product decision. We stop at preview in this demo.”  

**STOP if:** Tempted to click **Generate credit note** for show.

---

## Step 85 — Outstanding report

**Go to:** **Reports** → **Payment status / outstanding** → **Run report**  

**You should see:** **INV-0002**; not INV-0001.  

**What to say to the client:**  
“Unpaid invoices for follow-up and cash collection.”

---

## Step 86 — Invoices by client

**Go to:** **Reports** → **Invoices by client**  

**Enter:** **From** `2026-08-01`, **To** `2026-08-31`  

**Save/Action:** **Run report**  

**You should see:** INV-0001 and INV-0002 with amounts and payment status.  

**What to say to the client:**  
“Reconciliation by resident and period.”

---

## Step 87 — Audit trail

**Go to:** **Administration** → **Audit**  

**You should see:** Invoice, payment, email, and setup actions.  

**What to say to the client:**  
“An append-only record of who did what and when.”

---

## Step 88 — Optional ReadOnly

**Only if** `demo-viewer@example.com` exists: **Sign out** → sign in as ReadOnly → show read-only access → **Sign out** → TenantAdmin again.  

**Otherwise:** Skip.

---

## Step 89 — Closing story

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

*UI labels match current app: **Funding** tab, **Preview billing**, **Generate invoices**, **Mark paid**, **Credit note** from invoice. Demo amounts are illustrative — not finance-approved.*

# Care Home Client Demo — A-to-Z Script

**Environment:** Local Docker Compose · `Development` · simulated email · fictional data · **Demo Care Group** tenant  
**Presenter login (main demo):** `demo-admin@example.com` — password in your password manager only (**never on screen**)  
**Application URL:** http://localhost:4200  

**UI vs older docs (use the app):**

| Docs sometimes say | Application actually shows |
|--------------------|----------------------------|
| “Residents” menu | **Operations → Clients** (`/clients`) |
| “Funding” tab | **Funding contracts** tab on the client profile |
| Invoice status “Finalized” | Status badge **Generated** (plus separate **Paid** / **Not paid** payment badge) |
| Credit note £600 from period dates alone | **Credit Notes** UI has **no partial amount fields**; Preview **Credit** column is authoritative (see §16) |

Data entry belongs in rehearsal (`CLIENT_DEMO_OPERATOR_RUNBOOK.md` / `DEMO_DATA_ENTRY_CHECKLIST.md`). **Do not enter master data or generate invoices during the client meeting** unless you have explicitly agreed to a “live build” demo.

---

## 1. Demo Objective

You will walk the client through one coherent story: **how a care organisation manages a resident, turns funding contracts into invoices, collects payment, handles adjustments and communication, and reports with accountability.**

They should leave understanding:

- One back-office system for **operations + billing + finance visibility**
- **Resident and contract data** drive invoice lines (not ad hoc spreadsheets)
- **Invoices, PDFs, payment status, credit notes, and email** form a traceable revenue cycle
- **Reports and audit** support management and compliance
- **Roles** can limit who can change data

Today’s session uses **fictional** people, funders, and bank details on a **local development** stack. Email is **simulated**. Billing formulas are **implemented for demonstration** — **finance sign-off on rules is still pending**.

---

## 2. Before Client Joins

Complete this checklist **after** rehearsal data entry, **immediately before** the client connects. No passwords on screen; no Docker terminals visible.

### Infrastructure

- [ ] Docker Desktop running  
- [ ] `docker compose ps` — `carehome-sql` and `carehome-api` **healthy**; `carehome-web` **running**  
- [ ] `GET http://localhost:5092/health/live` → Healthy  
- [ ] `GET http://localhost:5092/health/ready` → Healthy  
- [ ] http://localhost:4200/login → **Care Home Back Office** sign-in page  
- [ ] Browser: fresh profile or cleared `localStorage` key `carehome.auth`  

### Organisation and access

- [ ] **Demo Care Group** exists and is **Active**  
- [ ] **Existing Organisation** is **Inactive** (migration placeholder — not used for demo data)  
- [ ] TenantAdmin `demo-admin@example.com` works; forced password change **already completed**  
- [ ] Side nav brand subtitle shows **Demo Care Group**  

### Demo data (Demo Care Group only)

- [ ] Company **Demo Care Ltd**  
- [ ] Care home **River View House** (`RIVER01`, capacity **24**)  
- [ ] Funding authority **Anytown Council**  
- [ ] Client **Alex Morgan** — Sage **DEMO001**, reference **RVH-001**  
- [ ] Client **Jordan Blake** — Sage **DEMO002**, reference **RVH-002**  
- [ ] Active funding contracts + weekly rates (**£575.00** Alex, **£600.00** Jordan)  
- [ ] **INV-0001** — Alex — August 2026 — total **£2,546.43**  
- [ ] **INV-0002** — Jordan — August 2026 — total **£2,657.14** — payment **Not paid**  
- [ ] **INV-0001** payment status **Not paid** until you perform the live “Mark Paid” step (if rehearsal left it Paid, open INV-0001 → **Mark unpaid** once before the client joins)  
- [ ] **CN-0001** — only if Preview during rehearsal showed the amount you intend to claim (see §16); optional if you will Preview-only live  
- [ ] INV-0001 and INV-0002 **Download PDF** both open valid PDFs  
- [ ] **Reports** spot-checked (outstanding + invoices by client)  
- [ ] **Audit** shows recent setup actions  

### Presenter environment

- [ ] No IDE, README, `.env`, or terminal on shared screen  
- [ ] Zoom / resolution checked  
- [ ] This script or §25 cheat sheet open on a **second monitor** only  

---

## 3. A — OPENING

| | |
|---|---|
| **WHAT I DO** | Share browser only (already on login page or a neutral slide). Do **not** show Docker, Azure, or credentials. |
| **WHAT I SAY** | “Thank you for joining. Today I’ll show how your teams could manage residents and the full financial path around them — from organisation setup and funding contracts through billing, invoices, payments, adjustments, and reporting — all in one place. Everything you’ll see is fictional demo data on our local development environment, so we can explore the workflow safely.” |
| **WHAT THE CLIENT SEES** | Your intro slide or the **Care Home Back Office** login branding (**CH** mark, title **Care Home Back Office**). |

---

## 4. B — LOGIN

| Item | Detail |
|------|--------|
| **URL** | http://localhost:4200/login (or http://localhost:4200 → redirects to login) |
| **Screen** | **Care Home Back Office** — fields **Email address**, **Password**, button **Sign in** |
| **Action** | Enter **Email address:** `demo-admin@example.com`. Enter password from password manager (**do not read aloud or type visibly slow enough to expose**). Click **Sign in**. |
| **Expected result** | Full app shell loads — side nav includes **Dashboard**, **Operations**, **Billing**, **Reporting**, **Administration**. Brand subtitle: **Demo Care Group**. Land on **Dashboard** (`/dashboard`). |

**Do not** log in as PlatformAdmin (`admin@localhost`) for the main story — that account only sees **Organisations**.

---

## 5. C — DASHBOARD

| | |
|---|---|
| **WHAT I DO** | Stay on `/dashboard`. Glance at KPI cards and **Recent invoices** — do not dwell on empty widgets if data is missing (that means rehearsal incomplete). |
| **WHAT I SAY** | “This is the operational picture for the organisation: how many homes and current clients we have, capacity, what’s outstanding financially, and recent invoice activity. Finance and operations can start the day here rather than jumping between spreadsheets.” |
| **WHAT THE CLIENT SEES** | KPIs such as **Care Homes**, **Current Clients**, **Available Beds**, **Outstanding Invoices** (count + £ unpaid), **Recent invoices** table with links to **INV-0001** / **INV-0002**. |
| **CLICK** | Optional: click an invoice link in **Recent invoices** later; for now 30–60 seconds on the dashboard only. |

**Skip** deep dives into **Setup checklist**, **Billing exceptions**, or **Occupancy by home** unless the client asks — mention they exist for day-to-day ops.

---

## 6. D — ORGANISATION

| | |
|---|---|
| **WHAT I DO** | **Administration** (expand) → **Organisation Settings** → `/settings/organisation`. Scroll; **do not save** unless correcting a rehearsal mistake. |
| **WHAT I SAY** | “Each care group runs in its own isolated organisation. These settings control how money is presented — currency, how invoice and credit note numbers are prefixed, and standard payment terms. That keeps finance consistent across every home in the group.” |
| **WHAT THE CLIENT SEES** | **Organisation Settings** — sections **Organisation details**, **Invoice settings**, **Payment terms**. |
| **SHOW** | Name **Demo Care Group**; **Invoice prefix** `INV-`; **Credit note prefix** `CN-`; **Payment terms (days)** **30**; **Currency** GBP / **£**. |
| **VERIFY** | Values match rehearsal; no accidental edits. |

---

## 7. E — CARE HOME

| | |
|---|---|
| **WHAT I DO** | **Operations** → **Care Homes** → `/care-homes`. Locate **River View House**. Optional: click **Dashboard** for that row (`/care-homes/{id}/dashboard`) for a home-centric view, or stay on the list. |
| **WHAT I SAY** | “Organisations typically have one or more operating companies and the care homes themselves. River View House sits under Demo Care Ltd — that structure drives which company name appears on invoices.” |
| **WHAT THE CLIENT SEES** | Table: **Code** `RIVER01`, **Care Home** **River View House**, **Company** **Demo Care Ltd**, **Capacity** **24**. |
| **SHOW** | Company relationship and bed capacity (operational planning + occupancy context). |

---

## 8. F — RESIDENT

| | |
|---|---|
| **WHAT I DO** | **Operations** → **Clients** → `/clients`. Open **Alex Morgan** → `/clients/{id}`. Tab **Details** (default). |
| **WHAT I SAY** | “Every billing line ties back to a person in care. The resident record holds identity, placement, and references your finance system needs — here Sage ID and an internal reference — so invoices stay unambiguous.” |
| **WHAT THE CLIENT SEES** | Header: **Alex Morgan**; subtitle with **RVH-001 · DEMO001 · River View House**; status **Current**. Cards: **Personal information**, **Care information** (e.g. **Residential**, admitted **2026-04-01**), **Contact information**. |
| **SHOW** | Reference **RVH-001**, Sage **DEMO001**, care type, admission date, care home. |
| **Optional** | Tab **Funding contracts** — point to active **Anytown Council / General Care** contract (full funding in §9). |

---

## 9. G — FUNDING

| | |
|---|---|
| **WHAT I DO** | On Alex’s profile → tab **Funding contracts**. Expand **Rate history** (same tab) or scroll to rate table. Optional cross-reference: **Billing Setup** → **Funding Authorities** → `/funding-authorities` → **Anytown Council** (master data reused across residents). |
| **WHAT I SAY** | “The council funds Alex under a contract for General Care, with a weekly rate and effective dates. When we run billing for a period, the system uses the active contract and rate — not a one-off spreadsheet cell.” |
| **WHAT THE CLIENT SEES** | Contract: authority **Anytown Council**, category **General Care**, nominal **4000**, start **2026-04-01**, status **Active**. Rate: **£575.00** weekly from **2026-04-01**. |
| **SHOW** | Authority, contract, rate, rate history. |
| **Disclaimer** | “The August amount you’ll see is calculated from weekly rate and days in period for this demo — your finance team would still sign off the exact proration rules before production.” |

---

## 10. H — BILLING PREVIEW

| | |
|---|---|
| **WHAT I DO** | **Billing** → **Billing Workspace** → `/billing`. **Step 1 — Select billing scope:** |

| Field | Value |
|-------|--------|
| Company | **Demo Care Ltd** |
| Care home | **River View House** (not “All care homes”) |
| Invoice category | **General Care** |
| Period start | `2026-08-01` |
| Period end | `2026-08-31` |

Click **Preview** only — **do not click Generate** during the client demo (invoices already exist).

| **WHAT I SAY** | “This is where finance selects scope — company, home, category, and period — and previews what would be invoiced. The preview shows who is billable, what was already billed, line amounts, and any exceptions before anything is committed.” |
| **WHAT THE CLIENT SEES** | **Step 2 — Preview**, **Step 3 — Review exceptions**, **Step 4 — Generate** with line detail and total. |

**Expected results (two scenarios — be honest on screen):**

1. **If August is not yet invoiced in your database (unlikely after rehearsal):** Step 2 lists **Alex Morgan** (and possibly others); Step 4 total **£2,546.43** for Alex-only if Jordan does not exist yet; banner **Ready to generate**. *Do not generate live — open **INV-0001** instead.*  
2. **If August is already invoiced (normal after rehearsal):** Preview may show **skipped already-billed** days, **Generation blocked**, or **no remaining billable** lines. **Say:** “August has already been invoiced for this home — that’s why there’s nothing left to generate. The invoices we’ll open next are the result of this same preview workflow during setup.”

**VERIFY** | You explained calculation intent even if preview is empty/blocked; you did **not** create duplicate invoices.

---

## 11. I — INVOICE INV-0001

| | |
|---|---|
| **WHAT I DO** | **Billing** → **Invoices** → `/invoices`. Open **INV-0001**. |
| **WHAT I SAY** | “Here is Alex’s August invoice — produced from her contract and rate. One document per billing run groups the funder, home, and category, with a clear line for the resident.” |
| **WHAT THE CLIENT SEES** | Header **INV-0001**; badges **Generated** and **Not paid** (before payment step); total **£2,546.43**. Sections **Invoice information**, **Billing information**, **Funding authority**, **Line items**. |
| **SHOW** | Invoice number; resident **Alex Morgan**; period **2026-08-01** to **2026-08-31**; line amount **£2,546.43**; funder **Anytown Council**; references **DEMO001** / **RVH-001**. |
| **VERIFY** | Total **£2,546.43** matches rehearsal. |

---

## 12. J — INVOICE PDF

| | |
|---|---|
| **WHAT I DO** | On **INV-0001** detail → **Download PDF**. PDF opens in a new browser tab. |
| **WHAT I SAY** | “This is the customer-facing document the funder receives — layout, line detail, totals, and payment instructions. It’s a snapshot at generation time; amending the resident record later doesn’t rewrite issued PDFs.” |
| **WHAT THE CLIENT SEES** | PDF with organisation/company branding area, **INV-0001**, dates (invoice **31 Aug 2026**, due **30 Sep 2026** with 30-day terms), **River View House**, **Alex Morgan**, **DEMO001**, service period August 2026, rate **£575.00** weekly, line total **£2,546.43**, footer/template bank details (**fictional** `00-00-00` / `00000000` for demo). |
| **POINT OUT** | Funder name, resident identifiers, period, money, payment terms, bank block marked as demo-only verbally. |
| **VERIFY** | PDF opens; total matches on-screen invoice. |

---

## 13. K — PAYMENT

| | |
|---|---|
| **WHAT I DO** | Same **INV-0001** detail (close PDF tab). Click **Mark Paid**. |
| **WHAT I SAY** | “Payment status is tracked on the invoice itself — paid versus still outstanding — so collections can see what’s settled without maintaining a separate shadow spreadsheet. This is a status flag for workflow and reporting, not a full payment gateway.” |
| **WHAT THE CLIENT SEES** | Toast **Payment status updated.** Payment badge **Paid** (green). **Generated** status unchanged. |
| **VERIFY** | **Paid** badge visible; total still **£2,546.43**. |

**Do not** click **Void** during the demo.

---

## 14. L — SECOND RESIDENT

| | |
|---|---|
| **WHAT I DO** | **Operations** → **Clients** → open **Jordan Blake**. Tab **Details** → brief look → **Funding contracts** / rates. |
| **WHAT I SAY** | “The same model scales to every resident — Jordan is in nursing care at the same home with his own references and council contract, but a different weekly rate.” |
| **WHAT THE CLIENT SEES** | **RVH-002 · DEMO002**; **Nursing**; admitted **2026-05-15**; contract **Anytown Council**; rate **£600.00** weekly. |
| **SHOW** | Parallel structure only — do not re-read every field. |

---

## 15. M — SECOND INVOICE

| | |
|---|---|
| **WHAT I DO** | **Billing** → **Invoices** → open **INV-0002**. |
| **WHAT I SAY** | “Jordan’s August invoice is still outstanding — this is what finance would chase. We deliberately keep one paid and one unpaid example to show reporting and collections.” |
| **WHAT THE CLIENT SEES** | **INV-0002**; **Generated**; payment **Not paid**; total **£2,657.14**; line for **Jordan Blake**. |
| **VERIFY** | **£2,657.14**; **Not paid**. |

---

## 16. N — CREDIT NOTE

| | |
|---|---|
| **WHAT I DO** | **Billing** → **Credit Notes** → `/credit-notes`. |

**Business story:** “Jordan was billed for part of August that shouldn’t have been charged — we issue a **credit note** linked to the original invoice instead of editing history.”

### Safe demonstration paths

**Path A — CN-0001 already exists from rehearsal (preferred if amount is correct):**

- Scroll **Existing credit notes** → show **CN-0001**, original invoice **INV-0002**, reason, total matching Preview.  
- Optional: **PDF** on that row.  
- **Say:** “Credits are separate controlled documents — the invoice header total stays as issued; the credit note adjusts what we claim overall.”

**Path B — Live workflow without risky generate:**

1. Note Jordan’s numeric id from the browser URL on `/clients/{id}` (e.g. `2`).  
2. **Client ID (optional):** enter that id.  
3. **Period start** `2026-08-25`, **Period end** `2026-08-31`.  
4. **Reason:** `Partial period adjustment — 7 days not billable (demo)`  
5. Click **Preview** → read **Remaining** and **Credit** columns for **INV-0002**.  
6. **If Credit = £600.00** and **Generate** is enabled → you may **Generate** → expect **CN-0001** at **£600.00**.  
7. **If Credit = £2,657.14** (full remaining) → **do not click Generate.** Explain limitation below.

### UI limitation — exact presenter wording if client asks

> “The credit note screen filters which invoice lines are in scope by period, but it doesn’t yet let us type a partial amount in the browser. The API supports partial credits, but this workspace doesn’t send per-line amounts — so Preview defaults to the full remaining balance on the line. We wouldn’t generate that in production without matching your finance rules; it’s on our enhancement list to expose partial amounts in the UI.”

**Also note if relevant:** Outstanding report still shows invoice **header** totals and does **not** net credit notes.

**VERIFY** | You did **not** claim **£600** unless Preview **Credit** showed **£600.00** or **CN-0001** list total shows **£600.00**.

---

## 17. O — EMAIL

| | |
|---|---|
| **WHAT I DO** | Open **INV-0002** → click **Email**. |
| **WHAT I SAY** | “Email delivery is **simulated** in today’s development demonstration. The workflow completes, the system records the send, and audit captures it — but **no message leaves this machine**. Production will use your configured email service.” |
| **WHAT THE CLIENT SEES** | Green banner: **Send completed (or simulated in development).** Toast: **Email queued/sent successfully.** *(Do not treat the toast as proof of inbox delivery.)* After refresh, invoice **status** may show **Sent**. |
| **VERIFY** | You stated simulation clearly; you did **not** claim the council received email. |

---

## 18. P — OUTSTANDING REPORT

| | |
|---|---|
| **WHAT I DO** | **Reporting** → **Reports** → `/reports`. Report: **Payment status / outstanding** (`outstanding`). Leave **From** / **To** blank unless you need to narrow. Click **Run**. |
| **WHAT I SAY** | “Finance can see everything still marked unpaid. Alex’s invoice dropped off once we marked it paid; Jordan’s remains because it’s still outstanding.” |
| **WHAT THE CLIENT SEES** | Grid including **INV-0002** / Jordan; **INV-0001** absent. |
| **VERIFY** | Only unpaid invoice(s) listed; if **CN-0001** exists, row may still show **£2,657.14** header — explain credits are separate documents. |

---

## 19. Q — INVOICES BY CLIENT

| | |
|---|---|
| **WHAT I DO** | Same page. Report: **Invoices by client** (`invoices-by-client`). **From** `2026-08-01`, **To** `2026-08-31`. Click **Run**. |
| **WHAT I SAY** | “Management can see revenue by resident for a period — useful for home managers and finance reviewing who was billed and how much.” |
| **WHAT THE CLIENT SEES** | Rows for **Alex Morgan** → **INV-0001** → **£2,546.43** and **Jordan Blake** → **INV-0002** → **£2,657.14** (column names come from the API). |
| **VERIFY** | If grid is empty, dates were not set — set August 2026 and **Run** again. |

---

## 20. R — AUDIT

| | |
|---|---|
| **WHAT I DO** | **Administration** → **Audit** → `/audit`. Optional: **Entity type** `Invoice` or `Client` → **Filter**. |
| **WHAT I SAY** | “Every significant action is recorded — who did what and when. That supports accountability, traceability, and financial review.” |
| **WHAT THE CLIENT SEES** | Table: **When**, **Entity**, **Action**, **Description** — creates/updates for clients, invoices, payment status, email send, credit note (if generated), etc. Subtitle: **Append-only. Records cannot be edited.** |
| **VERIFY** | Recent demo actions visible. |

---

## 21. S — READONLY ACCESS (OPTIONAL)

| | |
|---|---|
| **WHAT I DO** | Header user menu → **Sign out**. Log in as **demo-viewer@example.com** (ReadOnly — password in manager only). Open **Clients** → Jordan; **Invoices** → **INV-0002**. |
| **WHAT I SAY** | “Some staff only need visibility — carers, auditors, or board viewers — without ability to change financial records.” |
| **WHAT THE CLIENT SEES** | Data visible; no **Mark Paid**, **Email**, **Generate**, **Add Client**, or **Save** on write actions (`ReadOnly` role). |
| **WHAT I DO after** | **Sign out** → log back in as **demo-admin@example.com** before closing. |

Skip entirely in the 15-minute version.

---

## 22. T — END-TO-END SUMMARY

| | |
|---|---|
| **WHAT I DO** | Return to **Dashboard** or stay on **Audit** — no clicks required. |
| **WHAT I SAY** | “We followed one resident and their money end to end: **resident record → funding contract and rate → billing preview → invoice → PDF to the funder → payment status → outstanding invoice → credit note capability → reports → audit trail**. The value is one controlled system instead of disconnected spreadsheets, with permissions and history built in. Next step is your feedback on rules, reports, integrations, and roles so we can align production with how you actually work.” |

**Journey (display if helpful):**

```text
Resident (Client)
    ↓
Funding contract + rate
    ↓
Billing preview
    ↓
Invoice (INV-0001 / INV-0002)
    ↓
PDF
    ↓
Payment status
    ↓
Outstanding invoice + credit note
    ↓
Reports
    ↓
Audit
```

---

## 23. CLIENT QUESTIONS

Ask these — **do not answer for them**; capture answers for requirements:

1. How do you currently manage resident billing?  
2. Which billing rules are mandatory for go-live?  
3. How should partial-period billing work?  
4. How should partial credit notes work?  
5. What payment workflows are required (status only vs bank feeds vs allocations)?  
6. What reports do finance teams need daily / monthly?  
7. What Sage integration or nominal mapping is required?  
8. Which roles should different staff have?  
9. What information should be visible to each role?  
10. What documents need to be exported (PDF, CSV, Sage)?  
11. What email workflows are required (invoice send, reminders, credit notes)?  

---

## 24. THINGS NOT TO SHOW

- Azure, production hosting, Key Vault, production SMTP setup  
- SQL connection strings, JWT secrets, `.env`, Docker Compose, terminal logs (except private operator recovery)  
- PlatformAdmin provisioning unless the client explicitly asked for multi-tenant setup  
- **Existing Organisation** as the working tenant (it is a migration placeholder — inactive only)  
- Passwords, temporary org-create passwords, README dev credentials  
- Real customer PII, real NHS numbers, real funder contacts  
- Source code, DevTools, internal deployment scripts (`Deploy-Azure.ps1`, etc.)  
- **Void invoice** or **`docker compose down -v`** during the meeting  
- Claiming simulated email or demo bank details are production-ready  
- Misc charges, Sage export, or unfinished features unless the client asks  

---

## 25. PRESENTER CHEAT SHEET

| SCREEN | ACTION | ONE SENTENCE TO SAY | EXPECTED RESULT |
|--------|--------|---------------------|-----------------|
| Login `/login` | Sign in TenantAdmin | “We work inside one care organisation’s secure back office.” | **Demo Care Group** shell |
| Dashboard | Glance KPIs | “Operational and financial snapshot in one place.” | Counts + recent invoices |
| Organisation Settings | Show prefixes/terms | “Invoice and credit numbering and payment terms are configured once.” | INV-, CN-, 30 days, GBP |
| Care Homes | Show River View | “Homes sit under companies — that flows to invoices.” | RIVER01, Demo Care Ltd, 24 beds |
| Clients → Alex | Details + funding | “The resident anchors all billing.” | RVH-001, DEMO001, contract |
| Funding contracts tab | Rate £575 | “Weekly rate and dates drive line amounts.” | Active contract |
| Billing Workspace | Preview Aug scope | “Finance previews before committing.” | Preview or already-billed message |
| INV-0001 | Open detail | “August invoice for Alex from her contract.” | £2,546.43 |
| INV-0001 | Download PDF | “What the funder receives.” | PDF opens |
| INV-0001 | Mark Paid | “Collections tracked on the invoice.” | **Paid** badge |
| Clients → Jordan | Brief profile | “Same workflow, second resident.” | RVH-002, £600 rate |
| INV-0002 | Open detail | “Still outstanding — collections focus.” | £2,657.14, Not paid |
| Credit Notes | Preview / list CN | “Adjustments without rewriting invoices.” | Safe Preview or CN-0001 |
| INV-0002 | Email | “Simulated send today — real SMTP in production.” | Green dev banner |
| Reports | Outstanding | “Who still owes money.” | INV-0002 only |
| Reports | Invoices by client | “Revenue by resident for August.” | Both invoices |
| Audit | Recent rows | “Full traceability.” | Create/update entries |
| ReadOnly (opt.) | View only | “Some users see but cannot change.” | Writes hidden |

---

## 26. 30-MINUTE TIMING

| Minutes | Block | Sections |
|---------|-------|----------|
| 0–3 | Introduction + login | A, B |
| 3–7 | Dashboard + organisation + care home | C, D, E |
| 7–12 | Alex resident + funding | F, G |
| 12–17 | Billing preview + INV-0001 + PDF | H, I, J |
| 17–20 | Mark paid + Jordan + INV-0002 | K, L, M |
| 20–23 | Credit note + email | N, O |
| 23–27 | Reports + audit | P, Q, R |
| 27–30 | Summary + client questions | T, 23 |

Adjust ±2 minutes for questions during the flow.

---

## 27. 15-MINUTE VERSION

| Min | Flow |
|-----|------|
| 1 | A + B — intro + TenantAdmin login |
| 1 | D — Organisation settings (prefixes/terms only) |
| 3 | F + G — Alex profile + **Funding contracts** / rate |
| 4 | I + J — **INV-0002** (or INV-0001) detail + **Download PDF** |
| 2 | K + M — **Mark Paid** on INV-0001 + show INV-0002 **Not paid** |
| 2 | N — Credit Notes — **Existing CN-0001** or Preview-only (no unsafe Generate) |
| 1 | O — Email on INV-0002 + simulation disclaimer |
| 1 | P — Outstanding report |
| 1 | R — Audit — one screen |
| 1 | T + one question from §23 |

**Skip:** E care home deep dive, H billing preview, L Jordan detail, Q second report, S ReadOnly.

---

## 28. DEMO RECOVERY PLAN

Use calm language to the client: “Let me refresh — the data is already in the system.”

| Symptom | Safe recovery |
|---------|----------------|
| Slow page | Wait 5–10s; single **browser refresh**; avoid repeated clicks |
| Failed API / red error banner | Refresh; if persists, **sign out** and sign in again; operator may `docker compose restart api` off-screen, wait for healthy |
| Stale login / wrong menu | **Sign out**; clear `carehome.auth` if needed; TenantAdmin only |
| PDF fails | Retry **Download PDF**; fall back to PDF opened during rehearsal (local copy) — say “same document as in the system” |
| Wrong tenant / 403 | Logged in as PlatformAdmin — sign out → TenantAdmin |
| Billing preview errors / blocked | Do not regenerate — open existing **INV-0001** / **INV-0002** |
| Empty reports | Set **From** / **To** for invoices-by-client (Aug 2026) |
| Login failure | Check email; wait 1 min (rate limit); verify password manager |
| Database unhealthy | Wait 1–2 min; operator checks `/health/ready` off-screen |

**Never during client call:** `docker compose down -v`, voiding invoices, SQL edits, or API hacks for credit amounts.

**After client leaves** (isolated demo machine): full reset per operator runbook if needed.

---

# FINAL DEMO FLOW

Compact sequence — **CLICK · SHOW · SAY · VERIFY** for each step.

1. **Login**  
   - **CLICK:** `/login` → `demo-admin@example.com` → **Sign in**  
   - **SHOW:** **Demo Care Group** in nav  
   - **SAY:** One system for operations and finance  
   - **VERIFY:** Dashboard loads, not `/change-password`

2. **Dashboard**  
   - **CLICK:** `/dashboard`  
   - **SHOW:** Outstanding + recent invoices  
   - **SAY:** Daily operational picture  
   - **VERIFY:** KPIs populated (not zero if data ready)

3. **Organisation**  
   - **CLICK:** **Administration → Organisation Settings**  
   - **SHOW:** INV-, CN-, 30 days, GBP  
   - **SAY:** Organisation-wide billing configuration  
   - **VERIFY:** Demo Care Group name

4. **Care Home**  
   - **CLICK:** **Operations → Care Homes**  
   - **SHOW:** River View House / Demo Care Ltd / 24 beds  
   - **SAY:** Org structure to invoice header  
   - **VERIFY:** RIVER01 row

5. **Alex**  
   - **CLICK:** **Operations → Clients → Alex Morgan**  
   - **SHOW:** RVH-001, DEMO001, admission, care type  
   - **SAY:** Resident record drives billing  
   - **VERIFY:** Status Current

6. **Funding**  
   - **CLICK:** Tab **Funding contracts** (+ rates)  
   - **SHOW:** Anytown Council, General Care, £575/week  
   - **SAY:** Contract + rate → invoice lines  
   - **VERIFY:** Active contract

7. **Billing Preview**  
   - **CLICK:** **Billing Workspace** → scope Aug 2026 → **Preview**  
   - **SHOW:** Steps 2–4 or already-billed explanation  
   - **SAY:** Preview before commit; proration disclaimer  
   - **VERIFY:** No **Generate** click

8. **INV-0001**  
   - **CLICK:** **Invoices → INV-0001**  
   - **SHOW:** Line £2,546.43, Alex, August  
   - **SAY:** Generated from contract  
   - **VERIFY:** Total matches

9. **PDF**  
   - **CLICK:** **Download PDF**  
   - **SHOW:** Funder-facing layout + total  
   - **SAY:** Snapshot document; fictional bank details  
   - **VERIFY:** PDF opens

10. **Mark Paid**  
    - **CLICK:** **Mark Paid** on INV-0001  
    - **SHOW:** **Paid** badge  
    - **SAY:** Status vs invoice document  
    - **VERIFY:** Toast success

11. **Jordan**  
    - **CLICK:** **Clients → Jordan Blake**  
    - **SHOW:** RVH-002, nursing, £600 rate  
    - **SAY:** Same model, second resident  
    - **VERIFY:** Contract visible

12. **INV-0002**  
    - **CLICK:** **Invoices → INV-0002**  
    - **SHOW:** £2,657.14, **Not paid**  
    - **SAY:** Outstanding example  
    - **VERIFY:** Totals match

13. **Credit Note**  
    - **CLICK:** **Credit Notes** — list **CN-0001** and/or **Preview** only  
    - **SHOW:** Link to INV-0002; Preview **Credit** column  
    - **SAY:** Controlled adjustment; UI partial-amount limitation if asked  
    - **VERIFY:** No false £600 claim

14. **Email**  
    - **CLICK:** **INV-0002 → Email**  
    - **SHOW:** Green simulated banner  
    - **SAY:** Development simulation, not inbox delivery  
    - **VERIFY:** Disclaimer spoken

15. **Outstanding Report**  
    - **CLICK:** **Reports → Payment status / outstanding → Run**  
    - **SHOW:** INV-0002; not INV-0001  
    - **SAY:** Unpaid filter  
    - **VERIFY:** Matches payment flags

16. **Invoices by Client**  
    - **CLICK:** **Invoices by client**, Aug dates → **Run**  
    - **SHOW:** Alex + Jordan totals  
    - **SAY:** Management view  
    - **VERIFY:** Both rows present

17. **Audit**  
    - **CLICK:** **Administration → Audit**  
    - **SHOW:** Recent invoice/payment/email actions  
    - **SAY:** Accountability  
    - **VERIFY:** Entries exist

18. **Optional ReadOnly**  
    - **CLICK:** Sign out → viewer login → view client/invoice  
    - **SHOW:** No write buttons  
    - **SAY:** Role for view-only staff  
    - **VERIFY:** Sign back in as TenantAdmin

19. **Summary**  
    - **CLICK:** None  
    - **SHOW:** —  
    - **SAY:** End-to-end journey + invite requirements (§23)  
    - **VERIFY:** Client questions captured

20. **Client Questions**  
    - **CLICK:** None  
    - **SHOW:** —  
    - **SAY:** Ask §23 list  
    - **VERIFY:** Notes taken

---

*Script aligned to UI routes in `frontend/care-home-web/src/app/app.routes.ts` and screens verified September 2026. Rehearsal data: `CLIENT_DEMO_OPERATOR_RUNBOOK.md`, `DEMO_DATA_ENTRY_CHECKLIST.md`.*

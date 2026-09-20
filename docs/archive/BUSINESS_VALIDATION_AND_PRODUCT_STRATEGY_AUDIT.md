AUDIT DATE: 20 September 2026

APPLICATION: Care Home Back Office

PURPOSE: Determine whether this application can become a commercially viable UK care-home finance SaaS.

This audit is based on source-code inspection of the repository, internal product documentation, and current public UK market research. Implemented capability is distinguished from planned or marketing language. Business inferences are labelled as such. This document is not legal advice, not a GDPR assessment, and not proof of product-market fit.

---

# Executive Summary

**The painful problem is real. Willingness to pay for *this* product is not proven.**

UK residential care billing is genuinely hard: split funding (local authority + NHS/ICB + private top-up), weekly versus monthly cycles, admissions, discharges, backdated fee changes, credit notes, and Sage posting. A finance clerk still doing this in Excel has a real job to protect. That is not in dispute.

What *is* in dispute is whether **this application**, as it exists today, is something a UK care-home operator would pay for instead of Excel, Sage, CareHQ, CoolCare, Syncurio, CareMaster, or Fusion.

**Verdict in one line:** this is a technically serious **funding-aware billing engine** wrapped in a large set of CRUD screens. It is not yet a revenue-protection product, not yet a month-end control system that runs itself, and not yet a commercially validated SaaS.

**What is genuinely valuable (verified in code):**

- Preview-before-generate billing that explains remaining unbilled days versus already-billed days.
- Split-funder invoices grouped by company + care home + funding authority + invoice category.
- Refusal to invent a £0 rate when a rate or contract is missing.
- Duplicate-service-date protection and overlapping-contract blocking.
- Immutable invoice snapshots, credit notes capped at remaining amount, and tenant-scoped audit.

**What is commercially weak (verified in code or by absence):**

- Rate formulas (weekly / monthly / inclusive days) are **provisional and unsigned**. The product itself documents this as a production gate.
- Sage 50 is a **provisional CSV**, not a proven posting. VAT is hardcoded `T0`.
- Funder billing frequency is stored but **not used by the billing engine**. Operators must remember to run billing.
- Residents without a funding contract are **silently skipped** on a normal company/home run. That is a revenue-leakage hole, not a feature.
- There is no occupancy-versus-billed completeness scan, no expected-fee versus invoiced variance, no backdated-rate auto-credit, no invoice approval, no Xero, no personal allowances, no resident transfers, no MFA/SSO.
- The UK market already has products that advertise the same pain, often with more of the surrounding workflow (ledgers, discrepancy flags, accounting integrations, CRM).
- Automated tests covering the billing engine as a customer would use it are thin. There are no paying customers in this repository.

**Commercial implication:** do not spend the next quarter adding features. Spend it confirming, with finance managers, that they still live in spreadsheets, that the calculation rules match how *their* funders bill, and that they would pay for a finance-only billing control tool that sits beside (not inside) a care-planning system.

If those conversations fail, this remains a well-built internal tool. If they succeed, the product should become famous for one workflow: **explainable, funding-aware month-end billing that refuses to guess.**

---

# What the Product Actually Is

## Product in one sentence

This product helps **UK care-home finance staff** solve **the month-end problem of turning each resident’s mix of council, NHS and private funding into invoices without double-billing or inventing missing rates** by **previewing eligible days, exceptions and amounts, then generating immutable invoices, credit notes, PDFs and a Sage CSV**.

## Product in plain English

A finance employee in a care group would use this as the place where residents, funders and weekly/monthly fees live, and where monthly invoices are produced.

They would:

1. Set up the legal company, each care home, and each payer (council, NHS, private family).
2. Record each resident’s stay (admission, discharge) and one or more funding contracts with dated rates.
3. Choose a period, press Preview, and see who will be billed, for how many days, at which rate, and what is blocked (missing rate, missing template, overlapping contracts, already billed).
4. Generate invoices, download or email PDFs, raise credit notes if something was wrong, mark invoices paid or unpaid, and export a CSV for Sage 50.

They would **not** use it to write care plans, roster staff, run payroll, take card payments, reconcile the bank, or keep residents’ pocket money. Those things are not in the application.

## What the software actually is (by behaviour, not marketing)

| Candidate category | Fit | Why |
|---|---|---|
| Care management software | No | No care plans, medication, CQC evidence, family portal, or daily notes as a care record. |
| Accounting software | No | No nominal ledger, trial balance, VAT return, purchase ledger, or bank rec. Sage is export-only. |
| ERP | No | No procurement, HR, payroll, inventory, or multi-module operations. |
| Billing software | **Yes — primary** | The core write path is preview → generate invoices from occupancy ∩ contract ∩ rate, minus already billed days. |
| Finance workflow software | **Yes — secondary** | Credit notes, payment status, reports, audit, templates, Sage CSV, user roles. |
| Revenue protection software | **Not yet** | Exceptions fire when an operator runs billing. Completeness of “who should have been billed” is incomplete (silent skip). No expected-versus-actual fee RAG. |

**Implemented surface (verified):**

- **Frontend routes:** login, dashboard, companies, care homes (+ home dashboard), clients (+ profile and funding contracts), funding authorities, invoice categories, nominal codes, invoice templates, billing workspace, invoices, credit notes, miscellaneous CSV charges, reports, Sage export, users, audit, organisation settings, platform tenants.
- **Backend:** ASP.NET modular monolith; EF Core; SQL Server; JWT + Identity roles; tenant isolation by `TenantId` in application code (not EF global query filters).
- **Billing engine:** `BillingService` + `RateCalculator` + `InvoiceTemplateResolver` + overlap rules.
- **Demo data:** Development seed creates “Demo Care Group”, one company, one home (Sunrise House, 20 beds), three example funders (NHS / Council / Private), two invoice templates. It does **not** seed residents. Historical migrations still mention an older “Existing Organisation” seed; that is leftover data, not a live customer proof.

**Provisional, not signed off (product’s own documents):** weekly proration, monthly proration, inclusive day counting, Sage column map, VAT. See `docs/PRODUCTION_BUSINESS_SIGNOFF.md` — all four items are `PENDING`.

---

# Customer & Buyer

## Buyer (who would approve payment)

**Most aligned with the current product: group finance manager / finance director of a small-to-medium independent care group (roughly 3–10 homes).**

Why this person, not a generic “care-home owner”:

- The application’s daily language is invoices, nominal codes, Sage IDs, credit notes, payment status, audit, and funding authorities. That is a **finance** vocabulary, not a registered-manager or CQC vocabulary.
- Navigation is organised as Operations → Billing Setup → Billing → Reporting → Administration. There is no occupancy-sales CRM, no enquiry pipeline, no care-quality module. An operations director would find little of their world here.
- A single-home owner-manager might feel the pain, but they often accept Excel + Sage because volume is low. The buyer who feels *budget authority* plus *month-end load* is the person responsible for invoicing several homes to mixed funders.

A managing director may sign the cheque, but they will be sold by the finance lead’s statement: “month-end takes three days and we still miss people.”

## Daily user

**Finance officer / billing clerk / assistant accountant**, sometimes the home administrator for data entry of admissions and rates, with a Location Manager role if access must be limited to assigned homes.

Roles implemented: PlatformAdmin, TenantAdmin, Administrator, LocationManager, ReadOnly.

## Economic beneficiary

**The care-group owner / FD**, via (a) staff time at month-end and (b) recovered or avoided billing errors. Time saving is easier to claim today than leakage recovery, because leakage detection is incomplete.

## Person who experiences the pain

**The person who actually builds the monthly invoices** — usually one or two people in a small group, sitting in Excel, PDF, and Sage, answering council queries, and reconstructing “what did we bill Mrs X in March?” from email. They are not always the buyer.

**Inference, not a customer quote:** the product was built as if that person is the primary user. That is directionally correct. It has not been validated with them.

---

# Core Customer Problem

## What painful process this is replacing

The process this product is *trying* to replace is **manual resident billing**, typically:

- A spreadsheet of residents, weekly fees, and who pays what.
- Separate tabs or workbooks when a resident has council + NHS nursing contribution + family top-up.
- Manual day-count for admissions, discharges, deaths, and mid-month rate changes.
- Copy-paste into Word/PDF invoices, or Sage sales invoices keyed by hand.
- Credit notes when the council queries the days or the rate.
- A month-end scramble to prove nothing was billed twice and nobody was missed.

That process maps closely to objects that exist in this codebase: Client, Funding Authority (NHS / Council / Private / Other), Funding Contract, dated Funding Rate, Billing Preview, Invoice, Credit Note, Sage CSV, Audit.

## The must-have problem

**The single most valuable problem the product currently solves is: calculating and issuing funder-correct invoices for a period without double-charging days that were already invoiced, and without silently charging £0 when a rate is missing.**

If the customer stopped using this product tomorrow, they would return to:

- Spreadsheet day-counts and manual Sage invoicing, **if they had fully adopted it as the billing system of record**; or
- **Nothing painful**, if they had only used it as a demo / parallel tool and still ran Excel as source of truth.

The second outcome is the honest risk. Switching cost out of Excel is low until this system holds the live contracts and invoice history. Switching cost *into* this system is high (data load, rate-rule trust, Sage map). That asymmetry is commercially dangerous.

## If the answer is unclear — it is, slightly

The engine’s strongest property is **correctness under overlap**, not **completeness of billing**. Duplicate protection is real. Missed-resident protection is not complete. A customer whose nightmare is “we billed twice” is better served today than a customer whose nightmare is “we forgot three people.”

---

# Revenue Leakage Opportunities

**Important:** exceptions are produced when an operator runs `POST /api/billing/preview` or `generate`. There is no scheduled completeness job. `BillingExceptionLog` is a log of those runs, not an independent revenue-protection scanner.

A critical code path in `BillingService.BuildPreviewAsync`: if a resident has **no** active funding contracts, `MISSING_CONTRACT` is raised **only** when the operator filtered by invoice category or specific client IDs. On a normal company or home run, that resident is `continue`d with **no exception**. Occupancy that does not overlap the requested period is also skipped silently.

| Revenue Risk | Does current system detect it? | Evidence in code | Business value | Missing capability |
|---|---|---|---|---|
| Residents who should have been billed but were not | **Partial / mostly no** | No-contract residents skipped silently unless category or client filter is set (`BillingService` ~369–379). Occupancy miss: `continue` with no warning (~359–362). | High — this *is* leakage | Occupancy-versus-billed completeness report; always-on `MISSING_CONTRACT` on full runs |
| Incorrect funding rates | **No** | Engine bills the stored `FundingRate.Amount`. No expected weekly fee, no RAG, no comparison to a “package total”. | High | Expected-versus-invoiced variance |
| Expired contracts | **Partial** | Inactive or non-overlapping contracts are ignored. No “contract ends this month / already ended, resident still Current” alert. Dashboard “upcoming invoices” is just distinct funder frequencies, not expiry. | Medium–high | Contract expiry and open-ended-contract review queue |
| Missing rates | **Yes** | `MISSING_RATE` error; will not assume £0 (`BillingService` ~554–621). | High | Could add a standing “contracts with rate gaps” report so it is found *before* month-end |
| Incorrect effective dates | **No** | Dates are trusted. No check that rate `EffectiveFrom` aligns with admission, funder letter, or previous invoice. | High | Date-sense validations and “rate starts after occupancy” warnings |
| Partial-period billing mistakes | **Partial** | Remaining fragments and `PARTIAL_PERIOD_BILLING` / coverage DTO are real. Formulas and inclusive-day rule are **PENDING** sign-off. A “correct fragment, wrong pounds” error would not be flagged. | High | Signed-off proration; funder-specific day rules (bill day of death / not) |
| Duplicate invoices | **Yes** | Subtract finalized coverage; generate re-checks overlap; `ALREADY_FULLY_BILLED`; tenant billing lock. | High | — |
| Unbilled days | **Partial** | Catch-up of remaining fragments **if** the operator requests a window that includes them. No “March still has 12 unbilled days across 4 residents” unless someone previews March. | High | Period completeness / catch-up worklist |
| Incorrect care-home assignment | **No** | Client has a single `CareHomeId`. No transfer history. Billing uses current home. A mid-period move would be wrong or unmodelled. | Medium | Resident transfers with split billing by home |
| Funding authority mismatches | **No** | Bills the contract’s authority. No “this looks like FNC but category is General Care” logic. Types are NHS / Council / Private / Other labels only. | Medium | Funder/category sanity checks (FNC, CHC, top-up as first-class) |
| Rate changes not reflected in invoices | **By design, no** | Invoices store snapshots; later rate edits do not rewrite history. There is **no** “recalculate period vs billed” variance after a backdated rate. | High | Proposed credit + reinvoice from a rate change |
| Credit-note mistakes | **Partial** | Cannot exceed remaining invoiced amount; cannot span two invoices; reason required; cannot credit void. No check that the credit period matches occupancy or the funder’s query. | Medium | Guided “credit these days / this rate error” from occupancy or rate change |
| Outstanding invoices | **Yes, shallow** | `PaymentStatus` is `Paid` or `NotPaid` only. Outstanding report and dashboard totals exist. No partial payment, remittance, or Sage receipt import. | Medium | Aged debt with partials; payment matching |

**Bottom line:** the product is stronger at **preventing double billing** than at **finding missed billing**. Calling it a revenue-protection platform today would be marketing, not evidence.

---

# Business Value

All figures below are **illustrative assumptions**, not customer data. Sources for market context: DHSC MSIF provider fee reporting 2025–26; Which? / Lottie self-funder fee ranges 2026; NHS England Capacity Tracker occupancy (week ending 15 December 2025).

## Market context used as assumptions

| Assumption | Figure | Source / basis |
|---|---|---|
| LA residential fee (65+) | ~£956 / week | DHSC MSIF 2025–26 provisional average |
| LA nursing fee (65+), excluding FNC | ~£1,089 / week | Same |
| UK self-funder residential / nursing | ~£1,300 / £1,512 per week | Which? citing Lottie, early 2026 |
| Blended weekly fee used below | **£1,100** | Midpoint of LA and private mix; **assumption** |
| England care-home occupancy | 86.1% | Capacity Tracker, Dec 2025 |
| Occupancy used below | **85%** | **Assumption** |
| Error / miss rate in spreadsheet billing | **0.5%–2% of fee income** | **Assumption** — not measured |
| Finance time on month-end billing (spreadsheet) | **1–4 days per month per group**, scaling with homes | **Assumption** |
| Loaded cost of a finance officer | **£35,000–£50,000** | **Assumption** |

One missed week for one resident at £1,100 is £1,100. Ten missed resident-weeks a year is £11,000. That is how operators should think about this product — not “digital transformation.”

## Small group — 2–3 homes

| Item | Estimate (assumption) |
|---|---|
| Beds | 40–80 |
| Occupied residents | ~35–70 |
| Annual fee income | ~£2.0m–£4.0m |
| Billing events | ~35–70 residents × 1–3 funders × 12 periods ≈ **500–2,500 invoice lines / year** |
| Operational complexity | Manageable in Excel; pain spikes on discharges, April uplifts, and split funders |
| Manual workload | 1 person, 1–3 days at month-end plus query handling |
| Value of automation | Time (perhaps 0.2–0.5 FTE) plus error avoidance. At 1% leakage on £3m = **~£30k/year** — **illustrative** |
| What they care about | “Don’t make Sage harder.” “Don’t bill the council the wrong days.” Cheap, simple, one training session. |

They might pay **if** Excel is already breaking. Many will not; volume is still human-scale.

## Medium group — 5–10 homes

| Item | Estimate (assumption) |
|---|---|
| Beds | 120–350 |
| Occupied residents | ~100–300 |
| Annual fee income | ~£6m–£17m |
| Billing events | **2,000–10,000+ invoice lines / year**, mixed weekly/monthly funders |
| Operational complexity | High: multiple councils, NHS FNC/CHC, private top-ups, home-level administrators, Sage per company |
| Manual workload | 1–3 finance staff; month-end is a production event |
| Value of automation | Time (0.5–1.5 FTE) plus leakage. 1% on £10m = **~£100k/year** — **illustrative** |
| What they care about | Completeness, explainability when a LA queries, Sage export that actually imports, audit trail, not another all-in-one care system |

**This is the most plausible first commercial beachhead** — large enough that Excel hurts, small enough that CoolCare/CareHQ may not yet be fully embedded.

## Large group — 20+ homes

| Item | Estimate (assumption) |
|---|---|
| Beds | 500+ |
| Occupied residents | 400+ |
| Annual fee income | tens of millions |
| Billing events | industrial |
| Operational complexity | Multiple legal entities, mixed accounting systems (Sage 200 / Xero / NetSuite), procurement, internal IT |
| Manual workload | Dedicated finance ops; they already bought *something* |
| Value of automation | High in absolute pounds; **switching cost is also high** |
| What they care about | SSO, SLA, dedicated database, data migration, Xero/Sage 200, approval workflows, contract beds, personal allowances |

They are unlikely to be the first customer unless they are stuck on a dying on-prem tool and this product already matches their posting rules. Today it does not.

## What not to claim

Do not tell a prospect “we will save you 1% of revenue.” This codebase cannot currently **prove** missed billing across a period. Time-saving is the more honest early claim; leakage recovery is the better *future* claim if completeness scanning is built.

---

# UK Competitor Analysis

Research date: 20 September 2026. Distinction: **documented** = vendor help/docs or explicit product pages; **marketing claim** = homepage promises without independent verification; **inference** = this audit’s judgement.

**CareLedger:** the name does not currently identify a clear UK care-home billing competitor. `careledger.co.uk` is a news/content site. Other “CareLedger” products found are US medical billing, family care coordination, or analytics — not UK residential invoicing. It is **not treated as a comparable product** below.

| Competitor | Target customer | Main problem solved | Billing | Funding | Credit notes | Accounting integration | Revenue protection | Pricing if public |
|---|---|---|---|---|---|---|---|---|
| **This product (Care Home Back Office)** | Care-group finance (inferred) | Month-end resident invoicing from contracts and rates | **Documented in code:** preview/generate, partial periods, PDFs, email | **In code:** multiple contracts per resident; types NHS/Council/Private/Other. No first-class FNC/CHC/top-up. Billing frequency **not used by engine** | **In code:** preview/generate, remaining-amount cap, PDF | **In code:** Sage 50 CSV only; map **PENDING**; VAT `T0` | **Partial:** duplicate block and missing-rate block; missed-resident scan incomplete | Not sold; no price |
| **CareHQ** | UK care providers; CRM + billing | Enquiries, residents, invoices, expenses, reconciliation | **Marketing + product PDF:** preview, generate, advance/arrears/mid-period, 4-4-5, days after death, consolidate to funder | Multiple payers; discrepancy if contracts ≠ agreed weekly fee (**vendor claim**) | Not independently verified | **Documented on site:** Xero, Sage, Intuit, Tradeshift, generic CSV/Excel | **Vendor claim:** who has/hasn’t been billed; discrepancy alerts | **Public:** £150 + VAT / home / month rolling; discounts for term/volume ([carehq.co.uk/pricing](https://carehq.co.uk/pricing), product overview PDF) |
| **Syncurio** | UK care homes, resident management | Multi-source fees, historic changes, missed revenue | **Marketing:** invoices regenerate after historic change; custom invoices; contract beds; 1:1 care | **Marketing:** LA, self-funder, CHC, top-ups in one view | **Marketing:** auto regenerate credits | Direct debit export claimed; accounting package not clearly documented on the billing page reviewed | **Marketing:** RAG where fees ≠ expected; “missed revenue spotted” | Not public |
| **CareMaster** | Residential/nursing homes; bureau option | Repeat invoicing to multiple funders + Sage | **Vendor site:** repeat invoicing, LA layouts, occupancy/fee reporting | Multiple funding sources, different cycles (**vendor**) | Not independently verified | **Vendor:** **direct post** to Sage 50 (not CSV-only); Xero version advertised | Not positioned as leakage analytics | Not public |
| **CoolCare** | Care homes (broader ops: occupancy, payroll, rostering, finance) | Run the home; invoices in few clicks | **Vendor + invoicing guide:** fee contracts, extras, print/email, backdated contribution changes with auto credits/debits | LA and private contracts | **Vendor:** auto credits/debits on backdated changes | **Vendor:** Sage Line 50/200, Xero, NetSuite, Access, Sun, others | Occupancy pipeline claimed; not a dedicated leakage engine | Not public |
| **Fusion eCare** | UK care providers | Resident admin + invoicing | **Marketing:** auto invoices, ad-hoc, email | Families and LAs | Not independently verified | **Marketing:** Sage, Xero, Opera | Occupancy/income data claimed | Not public |
| **Care Vision** | Care homes (wide CMS) | Combined care + accounts | **Marketing:** auto invoices, reminders, pocket money | Not detailed on accounts page reviewed | Not independently verified | **Marketing:** Sage, “Quid Books”, “Zero” (likely QuickBooks/Xero — **wording is sloppy**) | Not clear | Not public |
| **PASS (everyLIFE)** | **Domiciliary / home care**, not residential homes | Invoice from **logged visits** | **Documented:** visit-based invoices, funder rates, approval, rounding modes | LA, ICB, private | Credit notes mentioned in feature set | Export/email; Sage not specified on invoicing page reviewed | Visit-versus-invoice, not bed-occupancy | Not public |

**How to read this table:** CareHQ, CoolCare, CareMaster and Syncurio are the real residential competitors. PASS is a different job (hourly home care). This product is narrower than CoolCare/CareHQ (finance-only) and **weaker on accounting connectivity** than CareMaster (live Sage) and CareHQ (several packages). Syncurio’s marketing already occupies the “missed revenue / RAG / historic regenerate” story that this repo does not yet implement.

---

# Market Gap

Do not manufacture differentiation. Compare **this codebase** to **public competitor capability**.

| Differentiator | Already common? | Strong differentiation? | Evidence | What would be required? |
|---|---|---|---|---|
| Billing correctness (no double-day, no £0 guess) | Partially — any serious billing tool claims accuracy | **Modest.** Conservatism (block on overlap/missing rate) is good; not unique | `OVERLAPPING_FUNDING_CONTRACTS`, `MISSING_RATE`, coverage subtract | Signed-off formulas; funder-specific day rules |
| Funding-aware billing | **Common** (CareHQ, Syncurio, CareMaster, CoolCare) | **No** as a slogan. Split contracts exist; FNC/CHC/top-up are not first-class | Multiple `ClientFundingContract`s; types are labels | Package total = sum of streams; CHC/FNC/top-up templates |
| Revenue leakage detection | Claimed by CareHQ and Syncurio | **Not yet** — this product is weaker than those claims | Silent skip of uncontracted residents | Completeness scan + expected-fee RAG |
| Pre-generation validation | CareHQ also advertises preview | **Possible**, if preview is clearer than theirs | Billing workspace: lines, exceptions, coverage, `canGenerate` | Exception worklist that must be cleared; don’t bury skips |
| Explainable calculations | Rare as a *product story* | **Possible** | Preview lines show days, frequency, rate, amount, already-billed vs remaining | Line-level “why this £”; printable calculation pack for LA queries |
| Auditability | Common at a basic level | **Modest** | `AuditService` on generate/void/payment/credit/Sage; no audit delete API | Field-level rate/contract history UI; export for accountants |
| Multi-tenant SaaS | CareHQ/Syncurio/Fusion are cloud; CareMaster historically on-prem/LAN | **Not unique** | Tenant + JWT `tenant_id`; shared DB; no Stripe | Billing for the SaaS itself; optional dedicated DB |
| Simplicity / finance-only | CoolCare/CareHQ/Fusion are broader | **Yes, if positioned as a feature** | No care, HR, CRM, payroll in routes | Ruthless scope; integrations *out* to PCS/Nourish rather than cloning them |
| Sage integration | Very common; CareMaster is deeper | **No** — CSV + unsigned map is a lag | `Sage50ColumnMap` PENDING | Customer-confirmed import; then Xero |
| Exception management | CareHQ discrepancy alerts; Syncurio RAG | **Weak today** | Logs of preview/generate only | Standing exception queue, not a report of last runs |
| Spreadsheet replacement | Every vendor claims this | **Only if data migration is easy** | Misc CSV import exists; no Excel resident/rate importer as a product | Excel mapping wizard |
| Faster month-end | Common claim | **Unproven** | Operator-triggered generate; no schedule from `BillingFrequency` | Calendar of due invoice runs per funder |

**The only honest gap:** a **finance-only, explainable, conservative billing control layer** that can sit next to an existing care-planning system, for groups that do not want another CMS. That gap closes quickly if CareHQ/CoolCare already sit in the account.

---

# Feature vs Business Assessment

**Current state: B, with incomplete C. Not D. Not yet E.**

| Option | Verdict |
|---|---|
| **A. A collection of CRUD screens** | **Too harsh as the whole truth.** Companies, homes, categories, nominals, templates *are* CRUD. The billing/credit/Sage path is not. |
| **B. A useful internal tool** | **Yes — this is what it most resembles commercially.** One organisation could run month-end on it after sign-off. There is no pricing, no self-serve signup, no customer success motion, no migration factory, no evidence anyone has paid. |
| **C. A vertical SaaS product** | **Architecture yes, business no.** Multi-tenant organisations, roles, platform admin, Azure docs exist. Subscriptions, plans, feature flags, and a sales ICP in-market do not. |
| **D. A revenue-protection product** | **No.** Leakage detection is incomplete; completeness is not a product loop. |
| **E. A potential care-home finance platform** | **Only if** completeness, signed formulas, Sage/Xero that actually post, and a narrow ICP are proven. Today it is a billing module, not a platform. |

**Why this is not vague:** a SaaS *product* has a repeatable buyer, a must-have workflow they already pay time or money for, and a reason to switch. This repo has the workflow engine. It does not have the buyer, the signed rules, or the switch reason versus CareHQ at £150/home.

If development continues without customer validation, the likely outcome is a **prettier internal billing database**, not a company.

---

# Killer Workflow

## Strongest end-to-end path already present

Organisation  
→ Company  
→ Care Home  
→ Resident (Client)  
→ Funding Contract + dated Rate  
→ Billing Preview (lines, coverage, exceptions)  
→ Generate Invoice (grouped by home + funder + category)  
→ PDF / email  
→ Payment status  
→ Credit note (if needed)  
→ Sage CSV  
→ Audit / exception log

That is a **real** finance journey. It is also **operator-driven and incomplete**: no “it is the 28th, Council X is due, here are the exceptions to clear.”

## Is it a compelling customer journey today?

**It is compelling as a demo of correctness. It is not yet compelling as a monthly operating system.**

Gaps that break the journey in real life:

- Billing frequency on the funder does not schedule the run.
- Uncontracted residents can vanish from a full-home preview.
- Rate math may not match the council’s spreadsheet.
- Sage CSV may not import.
- Backdated fee letters still require manual credit + new invoice.
- No approval step before emails go to families or funders.

## What single workflow the product should become famous for

**“Month-end billing control: show me exactly who will be billed, who will not, and why — then generate invoices that do not guess.”**

Not famous for CRM. Not famous for care plans. Not famous for being another Sage. Famous for **the preview that finance will defend in a local-authority query.**

---

# What NOT to Build

Remain a **care-home finance billing control** product. Do not become a CMS.

| Area | Recommendation | Why |
|---|---|---|
| Care planning, eMAR, CQC evidence | **Do not build** | PCS, Nourish, and others own this. Building it dilutes the finance story and explodes support. |
| Payroll, rotas, HR | **Do not build** | CoolCare already bundles this; it is a different buyer. |
| Enquiry CRM / occupancy sales | **Do not build yet** | CareHQ’s wedge. Only consider much later if finance-led land-and-expand is proven. |
| General ledger / full accounting | **Do not build** | Sage/Xero remain the ledger. Compete on *what gets posted*, not on replacing the accountant. |
| Card payments / banking / direct debit rails | **Do not build yet** | Trust, PCI, and reconciliation burden. Syncurio already markets DD export. |
| Personal allowances / pocket money | **Do not build yet** | Real finance pain, but a different ledger; competitors use it as CMS glue. |
| Inventory, maintenance, chef, housekeeping | **Do not build** | Unrelated. |
| Platform impersonation, Stripe, custom domains | **Infrastructure later** | Needed to *be* SaaS at scale; not the reason a first customer buys. |

**Do not remove** existing invoices, credits, audit, templates, or Sage CSV. They are the workflow. **Do not expand** them into an ERP.

**Existing occupancy cards and census reports** are fine as *billing context*. They should not grow into a care-home operations suite.

---

# Top 10 Missing Capabilities

Ranked by customer pain × revenue impact × frequency × differentiation, not by engineering interest.

| Rank | Feature | Customer problem | Business value | Differentiation | Complexity |
|---|---|---|---|---|---|
| 1 | **Billing completeness worklist** (occupied + current, or left mid-period, versus billed days; never silent-skip missing contracts) | “Who did we miss?” | Direct leakage; makes the product D, not just B | CareHQ/Syncurio claim this; you cannot win without it | Medium |
| 2 | **Stakeholder-signed rate and day rules** (including day of death / discharge) | Wrong pounds even when days look right | Without this, you cannot take live funder money | Table stakes, but currently a **blocker** | Low–medium (policy), high (if every LA differs) |
| 3 | **Funder billing calendar** (use `BillingFrequency` / interval to propose due runs) | Operators forget or mix weekly council with monthly private | Month-end reliability | CareHQ already has schedules | Medium |
| 4 | **Backdated rate change → proposed credit + reinvoice** | April letters and LA revisions | This is the spreadsheet nightmare | Syncurio/CoolCare market this strongly | High |
| 5 | **Expected package fee versus billed RAG** | Split funding that does not add up to the agreed weekly fee | Catches wrong contracts, not just missing rates | Syncurio/CareHQ claim it | Medium |
| 6 | **Confirmed Sage 50 import (then Xero)** | Finance will not leave Sage | Without posting, you are a PDF printer | CareMaster posts live; you lag | Medium (CSV sign-off), higher (API) |
| 7 | **Invoice approval before send** | Fear of emailing the wrong family/LA | Trust | Common in PASS/Syncurio claims | Medium |
| 8 | **Excel/CSV migration of residents, contracts, rates** | Switching cost | Determines whether you get a pilot | Few tools make this delightful | Medium–high |
| 9 | **Resident transfers and occupancy events that split billing** | Moves, absences, hospital — depending on local rules | Wrong home / wrong days | Domain depth | High |
| 10 | **Funder-specific invoice packs** (LA layouts, NHS SBS identifiers, PO numbers) | Rejected invoices delay cash | Cash flow, not just accuracy | CareMaster already sells LA layouts | High (per funder) |

**Not in the top 10 on purpose:** MFA/SSO (needed for trust, not for willingness-to-pay at 3 homes), VAT engine (needed before some live invoices, but first confirm with the customer), personal allowances, CRM.

---

# First-Customer MVP

## Must have before first paying customer (or paid pilot)

- Calculation rules **signed by that customer’s finance lead** (weekly, monthly, inclusive days, discharge/death).
- Completeness: full-home preview **always** lists people who will not be billed and why (including no contract).
- Sage CSV **imported successfully into their Sage 50 company** (their nominals, departments, customer codes).
- Invoice PDF they are willing to send to at least one funder type they actually use.
- Credit note path they accept for a known error.
- Tenant isolation, backups, HTTPS, named users, audit of generate/void/credit/export.
- A loaded dataset: their homes, residents, contracts, rates — not demo Sunrise House.
- Written scope: “this is billing control, not care records.”

## Should have shortly after

- Funder run calendar.
- Standing exception queue (not only last-generate log).
- Expected weekly fee versus sum of streams.
- Approval before bulk email.
- Excel import improvements.
- Xero or second Sage variant **if that customer uses it**.

## Do not build yet

- Care planning, payroll, CRM, pocket money, banking, Stripe subscriptions, dedicated DB per tenant, SSO/Entra, NHS e-invoicing portals, Tradeshift, contract-bed optimisation, AI.

---

# Ideal Customer Profile

**Geography:** England first (CQC, LA billing, NHS FNC/CHC). Wales/Scotland only after one England pilot — fee and statutory details differ.

**Organisation type:** Independent commercial care-home group (not a local-authority in-house home, not a national chain with an incumbent ERP).

**Number of homes:** **3–8**. Below 3, Excel often survives. Above ~10, CoolCare/CareHQ/internal systems are likely entrenched.

**Number of residents:** **80–250 occupied**. Enough split-funding pain; still loadable by hand or from spreadsheets in a pilot.

**Finance team:** 1 finance manager + 1 billing clerk (or a FD who still does invoices). Sage 50 user. Not a shared-services centre of 15.

**Existing tools that should coexist:**

- Sage 50 (or Xero — then your Sage CSV is a problem).
- A care-planning system (PCS, Nourish, or paper) — **do not replace it**.
- Outlook and Excel (the thing you are replacing for *billing*, not for everything).
- Capacity Tracker / occupancy spreadsheets.

**Current process:** resident fees in Excel; invoices typed in Sage or Word; credits when the council emails; April uplifts applied late; one person “who knows how the formulas work.”

**Buying trigger (what would make them search):**

- The billing person leaves or goes on long leave.
- A local authority disputes several months and they cannot reconstruct days.
- They discover unbilled occupancy after a home manager change.
- They add a second/third home and Excel breaks.
- Their accountant refuses to keep re-keying sales invoices.

**Anti-ICP:** national groups; homes already live on CoolCare or CareHQ billing; domiciliary-only providers (PASS territory); anyone needing live Sage posting in week one; anyone whose “weekly” rate is not days÷7.

---

# Pricing Models

Do not treat the following as a price list. It is a **test design**.

| Model | Advantage for this product | Disadvantage |
|---|---|---|
| **Per home** | Matches how groups think; matches CareHQ (£150/home). Simple quote. | A 20-bed home and a 70-bed home are not equal work or value. |
| **Per active resident** | Aligns with billing volume and leakage value. | Punishes occupancy (the customer’s goal); messy with leavers; procurement hates variable bills. |
| **Per user** | Easy to meter. | Finance teams are small; they will share logins; fights your audit story. |
| **Base subscription + resident fee** | Floor covers support; usage tracks value. | Harder to sell; needs a clean “active resident” definition. |
| **Tiered SaaS** (homes bands, Sage, extra entities) | Room to attach Sage pack / extra company. | Premature until you know what they value. |

**Structure to test (not a final price):**

- **Pilot:** 8–12 weeks, fixed fee covering onboarding (suggested test band: **£1,500–£4,000** one-off, waived only if you are buying learning, not if you are buying logos).
- **Ongoing test quote:** **£80–£140 per home per month** for billing+invoices+credits+Sage CSV, unlimited finance users, compared explicitly to CareHQ’s public **£150 + VAT per home** for a *broader* CRM.
- Optional **per-resident** conversation: “If we priced at £1.50–£3 per occupied resident, what happens to your bill at 90% occupancy?” Use it to **learn**, not to invoice.

If they flinch at ~£100/home, they are not in pain enough, or they already have a tool. If they ask to pay per home *before* you mention CareHQ, that is a signal.

---

# Customer Validation Plan

**Objective:** discover whether a real finance manager will pay, not whether they like a demo.

**Design:** 12 conversations before any further major feature work. Mix: 6 groups on Excel/Sage only; 3 on an incumbent (CareHQ/CoolCare/CareMaster); 3 who recently switched tools.

**Success is not compliments.** Success is: they walk you through last month’s actual invoices; they show the spreadsheet; they name a recent miss or dispute; they ask about cost or a data trial.

## 15 interview questions (non-leading)

1. Walk me through what happens when you prepare monthly resident billing, from the first spreadsheet to the invoice landing in Sage.
2. Which systems or files are involved, in order, including anything on someone’s desktop?
3. For a typical resident, how many different payers might appear on the same month, and how do you split the fee?
4. What did you actually do the last time a local authority changed a rate after you had already invoiced?
5. When a resident is admitted or discharged mid-month, who calculates the days, and how do you check it?
6. Tell me about the last billing mistake you found. How was it found, and what did it cost in time or money?
7. How do you currently know that every occupied bed was billed for the right days?
8. What happens when two people in the team both think they have invoiced the same period?
9. How do you produce a credit note today, and what does Sage need to see?
10. Which invoice layouts or reference numbers do your councils / NHS bodies insist on, and what happens if those are wrong?
11. How long did month-end billing take last month, and who stayed late?
12. If the person who “owns the billing spreadsheet” were unavailable for four weeks, what would break?
13. What did you evaluate the last time you looked at care-home software, and why did you not buy (or why did you)?
14. Where does Sage (or Xero) remain non-negotiable, regardless of any new tool?
15. When you last spent money on admin software, what triggered the purchase, and who signed it?

---

# Customer Interview Script

**30 minutes. Discovery, not a pitch. Do not open with the product name as a solution.**

### 0–5 minutes — Organisation

- Homes, beds, occupancy (rough).
- Mix of LA / NHS / private.
- Who invoices, who signs Sage, who owns the relationship with councils.
- Accounting package.

### 5–15 minutes — Current billing workflow

- Last month, start to finish.
- Screen-share or sit with the spreadsheet if they allow.
- Weekly vs monthly vs 4-weekly cycles.
- How PDFs get to families vs how invoices get to LAs.
- Credit notes and re-invoices.

### 15–20 minutes — Problems and money

- Last error, last dispute, last miss.
- Time: hours last month-end.
- “How would you know if three residents were unbilled?”
- Do not suggest answers.

### 20–25 minutes — Show **one** workflow only

Show **Billing Preview** on a realistic split-funded resident: occupancy ∩ contract ∩ rate, remaining days, a `MISSING_RATE` or overlap block, then stop.

Ask: “Where does this match or contradict how you calculate today?”  
Do **not** ask “would you use this?”

### 25–30 minutes — Change and pay (indirect)

- “If this existed with your data, what would still have to happen in Sage / Excel?”
- “What would make this a waste of time?”
- If they ask price, give the **test band**, not a performance. Note who would have to approve.

**After the call:** write down signals (below). Do not update the roadmap from one compliment.

---

# Validation Signals

| Strength | Examples | What to do |
|---|---|---|
| **Strong** | “How much does it cost?” / “Can we try this with our data?” / “Send that to our FD.” / They volunteer last month’s invoice CSV. / They book a follow-up with the Sage operator without you asking. | Prioritise a paid or tightly scoped data pilot. |
| **Medium-strong** | They argue about day-of-discharge rules in detail. / They show the spreadsheet unprompted. / They name a live LA dispute. | Good problem fit; do not confuse with purchase intent. |
| **Medium** | “Can you add X?” (Xero, pocket money, occupancy CRM) | Note X. If X is outside ICP, stay narrow. If X is Sage map or completeness, it is a real gap. |
| **Weak** | “Looks interesting.” / “Very slick.” / “We should look at this in the new year.” | Default to no. |
| **Negative** | “We already do this easily in Sage/Excel.” / “We’re on CareHQ/CoolCare and billing is fine.” / “Your weekly ÷ 7 is not how [Council] works.” / They will not show a real invoice. | Do not recruit them as a design partner for the current engine. |
| **Fatal for live billing** | They require live Sage posting, NHS SBS fields, or a specific LA layout as week-one must-haves, and will not trial CSV. | Either that customer is out of ICP, or MVP is larger than you think. |

**Thresholds (practical):**

- **Proceed to paid pilot:** ≥3 strong signals from **different** organisations in the 3–8 home Excel/Sage segment, including at least one who will load real (or realistic) contracts.
- **Reposition or stop:** 8+ interviews in that segment with mostly weak/negative signals, especially “Excel is fine” or “CareHQ already does this.”
- **Change the engine, don’t sell:** repeated rejection of ÷7 weekly or inclusive days.

---

# Positioning Options

Do **not** pick a winner without interview evidence. Option B/E are the best *hypotheses* given the code.

### Position A — Care-home billing platform

- **Target:** any care home that invoices residents.
- **Promise:** raise invoices faster.
- **Main competitor:** CareHQ, CoolCare, CareMaster.
- **Strength:** easy to understand.
- **Weakness:** crowded; you are not broader or cheaper in a proven way.
- **Required:** completeness, schedules, Sage/Xero, migration, support.

### Position B — Funding-aware billing platform

- **Target:** groups with split LA/NHS/private.
- **Promise:** every payer billed from the same occupancy, without mixing streams.
- **Main competitor:** Syncurio, CareHQ.
- **Strength:** matches the data model (multiple contracts per resident).
- **Weakness:** “funding-aware” is table stakes in this niche; FNC/CHC/top-up are not first-class here.
- **Required:** package totals, funder calendars, funder invoice packs.

### Position C — Care-home revenue protection platform

- **Target:** FD who suspects missed income.
- **Promise:** find unbilled days and wrong rates before and after invoice runs.
- **Main competitor:** Syncurio (RAG / missed revenue marketing).
- **Strength:** highest willingness-to-pay *if true*.
- **Weakness:** **the product does not do this yet.** Using this positioning now would be dishonest.
- **Required:** completeness scan, expected-fee RAG, backdated recast.

### Position D — Finance operations platform

- **Target:** group finance teams.
- **Promise:** one system for billing ops, credits, audit, Sage, users.
- **Main competitor:** CoolCare finance module + Sage.
- **Strength:** matches the menu structure.
- **Weakness:** “operations platform” invites payroll, HR, and ERP creep; payments are binary.
- **Required:** aged debt, partial payments, approvals, more integrations.

### Position E — Month-end billing control platform

- **Target:** the person who actually closes resident billing each month.
- **Promise:** a controlled run: preview, exceptions, generate, explain, export — no guessing.
- **Main competitor:** Excel + Sage; CareHQ billing module.
- **Strength:** matches the **best code** (preview, coverage, locks, snapshots, exceptions).
- **Weakness:** sounds operational, not emotional; must still prove time and error value.
- **Required:** completeness (no silent skips), signed rules, Sage import, calendar.

**Hypothesis to test first:** Position E in the customer’s words, with B as the domain proof. Adopt C only after completeness exists.

---

# Product Roadmap

## Phase 1 — First paying customer

- **Features:** completeness worklist; signed calculation profile per customer; Sage CSV signed off on *their* company; PDF they will send; credit notes; no silent skip.
- **Infrastructure:** single production tenant or tightly controlled multi-tenant; backups of SQL **and** documents; HTTPS.
- **Security:** named users, roles, password change on invite, no shared demo passwords in production.
- **Integrations:** Sage 50 CSV only, proven.
- **Reporting:** completeness, outstanding, exception queue, invoice register.
- **Support:** founder-led WhatsApp/email; a runbook for month-end.
- **Commercial:** paid pilot; ICP 3–8 homes; one success letter (with permission).

## Phase 2 — Product-market fit

- **Features:** funder calendar; expected-fee RAG; backdated change → proposed credit/reinvoice; approval; Excel import; second accounting export if demanded (Xero).
- **Infrastructure:** monitoring, email deliverability, document retention policy implemented as process.
- **Security:** MFA at least for TenantAdmin; session review (JWT-in-localStorage is a known weakness).
- **Integrations:** optional care-system resident feed later, not a CMS.
- **Reporting:** leakage recovered; time-to-generate; query pack for LAs.
- **Support:** office-hours SLA; onboarding playbook measured in days-to-first-invoice.
- **Commercial:** 3–10 paying groups; pricing experiment; refuse CRM/payroll requests.

## Phase 3 — Scale to multiple care groups

- **Features:** more LA layouts; NHS identifiers where customers require; resident transfers; aged debt with partials.
- **Infrastructure:** per-tenant document isolation review; consider dedicated DB for larger groups.
- **Security:** SSO for groups that ask; pen test; DPIA support pack (not a claim of certification).
- **Integrations:** Sage 200 / Xero as evidence-led, not speculative.
- **Reporting:** group-level FD pack.
- **Support:** CS capacity; training videos; status page.
- **Commercial:** partner with accountants who already serve care groups; do not sell via CQC/care-quality channels.

## Phase 4 — Expansion

- Only after PMF: revenue-protection analytics productisation; maybe personal allowances; maybe occupancy CRM **if** finance-led expansion is requested in interviews.
- Do not expand into care planning.

---

# Trust & Compliance Requirements

This is **not** a claim of legal compliance. UK providers handling resident names, DOB, and invoices will expect a serious conversation. Product facts below are from `docs/DATA_PROTECTION.md`, `docs/AUTHORIZATION.md`, `docs/BACKUP_RESTORE.md`, and code.

| Area | Current evidence | Required before **pilot** | Required before **production live invoicing** | Required when **scaling** |
|---|---|---|---|---|
| Multi-tenancy | `TenantId` on operational roots; `[RequireTenant]`; no EF global filters; PlatformAdmin cannot call operational APIs | Prove isolation with the pilot’s data; no shared demo tenant | Isolation tests in CI that hit HTTP, not only `ForTenant()` unit tests | Consider dedicated DB for large groups |
| Data isolation | Application-enforced; LocationManager 404 outside assignment | Written tenancy diagram for the customer | Independent review of IDOR-style access | Pen test |
| Access control / RBAC | Roles implemented; ReadOnly blocked on mutating verbs | Named users only; no shared finance login | Joiners/leavers process; admin MFA | SSO (Entra) |
| Audit | Generate/void/payment/credit/Sage/user events; no audit delete API | Show them the audit screen | Retain audit for agreed period | Export for external accountants |
| Financial auditability | Invoice/line snapshots; PDFs from snapshots | Sample invoice vs preview maths | Immutable documents + credit trail signed off by *their* accountant | Statutory retention alignment |
| Exportability | Reports CSV/XLSX/PDF; Sage CSV | They can leave with CSV | Documented export of residents, contracts, invoices | Full offboarding pack |
| Backups / DR | Azure PITR documented; LocalDB restore drill existed | Backup of **DB + files**; restore test on the actual host | RPO/RTO agreed; restore drill evidence | Regular drills, secondary region policy |
| Encryption | TLS assumed in production; Identity password hashes; **no field-level PII encryption** | HTTPS only | Disk encryption via host (Azure SQL TDE etc.) as standard | Review notes/DOB handling |
| GDPR | No erasure tool; archival only; no SAR portal | Contract: controller vs processor; lawful basis discussion | DPA; retention schedule; SAR/erasure **procedure** (legal vs invoice immutability) | DPO processes; subprocessors list |
| Change history | Rate rows versioned; invoices not rewritten | Show rate history report | UI that finance can use in a query | — |
| Data retention | No purge job | Agree “we keep invoices even if a resident leaves” | Written retention; backups included | Automated holds/purges per policy |

**Needs professional / legal review (do not DIY):**

- Controller vs processor in a multi-tenant SaaS.
- Lawful basis for resident finance data.
- Conflict between GDPR erasure and immutable invoices.
- Whether invoice PDFs may show Sage IDs and rates.
- International transfers if any processor is outside the UK.
- NHS/LA information-governance questionnaires for some funders.

---

# Business Risks

| Risk | Probability | Impact | Evidence | Mitigation |
|---|---|---|---|---|
| Strong incumbents (CareHQ, CoolCare, CareMaster, Syncurio) | High | Existential for “billing platform” positioning | Public products and pricing (CareHQ £150/home) | Position as finance-only control; win Excel users first |
| Customer switching cost | High | Slow sales | Spreadsheets + Sage are familiar; migration not a product | Paid onboarding; Excel import; run parallel one cycle |
| Sage dependency / wrong map | High | Deal-breaker | `Sage50ColumnMap` PENDING; CareMaster posts live | Import into *their* Sage before go-live |
| Incorrect billing calculations | High until signed | Legal/commercial disaster | Sign-off doc all PENDING; ÷7 can exceed 4× weekly in 31-day months | Customer-specific rules; do not invoice live funders until signed |
| Lack of integrations (Xero, Sage 200, care systems) | Medium–high | Lost deals | Sage CSV only | ICP filter; add second export only when a paying customer needs it |
| Customer trust (new vendor, financial data) | High | No pilot | No public customers; JWT in localStorage noted in launch docs | Pilot contract, backups, named support, insurance |
| Data migration | High | Failed pilots | Demo seed has no residents; no first-class Excel wizard | Manual concierge migration for customer one |
| UK funding complexity (LA/FNC/CHC/top-up/4-4-5) | High | Engine mismatch | Types are four labels; frequency unused; no 4-4-5 | Interview-driven rules; do not claim CHC/FNC expertise yet |
| Support burden of exceptions | Medium | Margin destruction | Every LA can be special | Narrow ICP; freeze rules per tenant |
| Regulatory / IG expectations | Medium | Delay | GDPR tooling absent; no MFA | Don’t sell to NHS-heavy groups first; legal review |
| Small addressable beachhead | Medium | Ceiling | Best ICP is 3–8 home independents | Price for value; land-and-expand later or stay a lifestyle SaaS |
| Feature creep into CMS/ERP | High (internally) | Lose the plot | Repo already has occupancy dashboards, census, bed capacity | Kill-list in this document; say no |
| Silent missed billing | High | Opposite of the promise | `MISSING_CONTRACT` gated on filters | Fix before any “revenue protection” language |
| Thin automated billing tests | Medium | Regressions in money | ~24 unit facts; overlap/void/misc/tenant helpers; not a full engine suite | Golden-file billing cases per signed rule |
| Shared-database tenancy | Medium | Enterprise deals | Documented: no dedicated DB | Dedicated DB as a paid tier later |
| No willingness-to-pay evidence | **Certain today** | You may be building the wrong thing | No customers in repo | Interviews before features |

---

# What We Know

Facts supported by this repository or cited external sources:

- The application is a multi-tenant Angular + ASP.NET **resident billing back office**, not a care-management system.
- Billing eligibility is occupancy ∩ active contract ∩ rate, minus finalized invoice coverage; generate is transactional with a tenant lock and document sequences.
- Missing rates/templates/nominals can block generation; overlapping active contracts on the same stream block that stream.
- Uncontracted residents can be omitted from a normal preview **without** an error.
- Payment status is binary; VAT is not applied; Sage is unsigned CSV; rate formulas are unsigned.
- Roles, tenant scoping, invoice snapshots, credit remaining-amount caps, misc CSV charges, and audit logging exist.
- Production docs themselves say **controlled pilot only**, business sign-off required, not fully production-ready for live funder invoicing.
- UK care-home fees are large relative to software cost (LA ~£956–£1,089/week; self-funder often £1,100–£1,500+). Occupancy in England was 86.1% in mid-December 2025.
- CareHQ publicly charges £150 + VAT per home per month for a broader CRM+billing product.
- CareMaster, CoolCare, Fusion, Syncurio and CareHQ already sell into this pain.

---

# What We Believe

Inferences — reasonable, not proven:

- The buyer is a small-group finance lead, not a registered manager.
- Excel + Sage 50 still runs a meaningful share of 3–8 home independents.
- Explainable preview is the best wedge this codebase has.
- Completeness + signed maths + Sage import is the difference between a demo and a product.
- Revenue-protection positioning is the highest-value *future* story and a **lie** if used now.
- Large groups are a poor first customer.
- Finance-only is a viable niche only if integrations to care planning remain “out of scope, on purpose.”

---

# What We Don't Know

Questions that only conversations, pilots, and invoices can answer:

- Do target finance managers still live in Excel, or has CareHQ/CoolCare already taken the beachhead?
- Will they accept weekly ÷ 7 and inclusive days, or do their councils use four-week months, 4-4-5, or exclusive discharge days?
- How often do they actually miss billing, in pounds, last year?
- Is Sage 50 CSV enough, or is live posting a must?
- Who signs software spend under £2k vs £10k?
- Will they run this **beside** PCS/Nourish, or will they insist on one system?
- Does a £80–£140/home price feel cheaper-than-CareHQ (finance-only) or worse-than-Excel (free)?
- Can a stranger be trusted with resident names and fees in a shared-tenant database?
- After a pilot, do they stop the spreadsheet or keep it as “the real one”?

---

# Recommended Next 5 Actions

Do these in order. Do not start with more features.

1. **Book 12 discovery calls** with finance managers in independent groups of 3–8 homes who still touch Excel and Sage. Use the script and questions above. Record signals, not compliments.
2. **Sit through one real month-end** (paid or unpaid observational) and photograph the actual artefacts: spreadsheet columns, Sage invoice, LA email, credit note. Compare each column to this data model. Write a one-page “fit / conflict” note.
3. **Fix the honesty gap before any sales deck:** treat silent skips as a product defect in messaging even if you do not code yet — never claim revenue protection. If you build anything, build completeness visibility, not a new module.
4. **Run a calculation workshop**, not a UI workshop, with the first serious prospect: their weekly rate, a 31-day month, a mid-month discharge, a split LA+private resident. If numbers disagree, you do not have a product.
5. **Offer one paid data pilot** only after a strong signal: their data, their Sage import, their sign-off of day rules, a fixed price, a written kill-list (no care planning). Stop the pilot if they keep Excel as source of truth after one cycle.

---

**The strongest opportunity:** become the **month-end billing control** system for small UK care groups who already have a care-planning tool and Sage, and who are still reconciling split funding in spreadsheets. The code’s conservative preview is the seed of that product. It is not the product yet.

**The biggest weakness:** a crowded market plus **unsigned money maths** plus **incomplete missed-billing detection** plus **zero evidence of willingness to pay**. Technical quality of the billing engine will not save a product that calculates the wrong pounds or that nobody will switch to.

---

IMPORTANT: This audit does not prove product-market fit. Only conversations, pilots, and ultimately paying customers can validate willingness to pay.

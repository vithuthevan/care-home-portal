# Final UI Visual QA Report

**Date:** 17 September 2026  
**Scope:** Care Home Back Office (`frontend/care-home-web`) — post–UI beautification  
**Environment:** Local Development — Web `http://localhost:4200`, API `http://localhost:5092` (Docker Compose)  
**References:** `UI_RESPONSIVE_BEAUTIFICATION_REPORT.md`, `UI_UX_DEMO_AUDIT.md`, `FINAL_CLIENT_DEMO_UX_VERIFICATION.md`, `FINAL_DEMO_DATA_READY.md`

### Review methodology

| Method | What was verified |
|--------|-------------------|
| **VERIFIED LIVE** | `http://localhost:4200/login` returns HTTP 200; `http://localhost:5092/health/ready` returns Healthy; Docker `carehome-web` / `carehome-api` reported up in operator terminal. |
| **CODE / STATIC REVIEW** | All layout, responsive breakpoints, typography, table/filter/form patterns, demo workflow wiring, and state components — from Angular templates, `styles.scss`, `app.scss`, and shared UI components. |
| **Not performed** | Interactive browser inspection at 320–1440px, Playwright, builds, lint, or automated tests (per task instructions). |

**Important:** Responsive behaviour below is **inferred from CSS/Tailwind breakpoints and markup** unless marked **VERIFIED LIVE**. A presenter should still do a 10-minute desktop rehearsal before the client session.

---

## 1. Executive Summary

The beautification pass delivered a **coherent green Material + panel design system**: spacing tokens, `app-page-header`, `app-filter-bar`, `data-table` styling, mobile card lists on primary list screens, financial emphasis (`amount-hero`, `app-currency-display`), and improved shell chrome (sticky toolbar, wrapping breadcrumbs, touch-friendly nav).

For a **desktop-led client demo**, the main billing narrative (dashboard → Alex Morgan → funding/rate → billing → invoices → PDF/payment → credit note preview → reports → audit) is **visually structured and professionally styled** in code. Gaps are mostly **secondary screens**, **dense tables on narrow viewports**, **button crowding on invoice detail**, and **copy that signals “demonstration environment”** (email simulation) — not wholesale visual brokenness.

Demo **data** is reported ready in `FINAL_DEMO_DATA_READY.md` (including Jordan Blake, INV-0002, paid INV-0001). One **content** inconsistency remains for narration: INV-0001 line snapshot may still show **Alexa Morgan** while the resident record is **Alex Morgan**.

---

## 2. Overall Verdict

**Classification:** 🟡 **Client-demo visual ready with minor polish**

**Rationale:** Core demo surfaces match a single design language and explain hierarchy well. Remaining issues are unlikely to derail a guided desktop demo but would show on tablet/phone walkthroughs, administration screens, or if the presenter scrolls invoice line tables on a narrow window.

---

## 3. Global Shell

**CODE / STATIC REVIEW**

| Area | Assessment |
|------|------------|
| Sidebar | 272px side nav, grouped sections, active link with inset accent; overlay mode below 1024px (`mat-sidenav` `over` vs `side`). |
| Toolbar | Sticky, 56px / 64px height; menu toggle with `aria-expanded`. |
| Breadcrumbs | `Home` link + dynamic trail; wraps; on &lt;640px current crumb stacks (`app.scss`). Entity-aware crumbs set on client profile, invoice detail, care home dashboard. |
| Page content | `.page` max-width 1280px, responsive padding; `main.min-w-0` reduces horizontal blowout. |
| User menu | 44px min-height chip; name/role hidden &lt;480px (avatar only). Sign out only — no change-password link when logged in. |
| Navigation groups | Collapsible Operations / Billing Setup / Billing / Reporting / Administration — sensible for demo if left expanded. |

**Desktop:** Balanced; brand block and nav density look intentional.  
**Tablet:** Overlay sidenav; content not permanently covered when menu closed.  
**Mobile:** Long breadcrumb trails (e.g. Billing › Invoices › INV-0001) may wrap to two lines — acceptable; toolbar competes with crumb width but `max-width` on `.crumb` limits collision.

**Hesitation factors:** Nav label **Clients** vs dashboard **Current residents**; Administration hidden from non–user-managers.

---

## 4. Dashboard

**CODE / STATIC REVIEW**

- Clear sections: “What should I do next?”, Operations/Finance KPIs, Attention, Recent invoices, Upcoming billing, Occupancy.
- KPI cards use shared `kpi-card` / `kpi-icon` pattern; financial outstanding amount in subtitle uses tabular formatting.
- Recent invoices: sticky header, `col-hide-sm` for care home on small widths.
- Quick actions: primary **Add client** (if write), stroked secondary actions — hierarchy OK.

**5–10 second comprehension (desktop):** **Expected yes** — subtitle + KPIs + “What should I do next?” communicate purpose.

**Mobile:** Dashboard tables remain tables (no card fallback) — usable with horizontal scroll; home column hidden on recent invoices below 768px.

---

## 5. Clients

**CODE / STATIC REVIEW**

- `app-filter-bar` with search + status; Search/Clear pattern.
- Desktop: `panel--flat` table with `col-hide-sm` (care type, admission).
- Mobile (`md:hidden`): `mobile-data-card` with Open action — **strong demo screen**.

**Hesitation:** Sage/reference in list depends on column set; mobile cards preserve name, reference, home, status.

---

## 6. Client Profile

**CODE / STATIC REVIEW**

- Header: name, reference · Sage ID, Back / Edit / **Start billing** (primary).
- Placement panel + **funding chain** (resident → authority → category → rate) + **rate-highlight** hero (£/week).
- Breadcrumb updates to `Clients › {First Last}` when loaded.
- Tabs scroll horizontally on narrow screens (`client-profile.scss`).
- **Start billing** passes query params (`billingQueryParams`) — context banner on billing workspace.

**Demo story (Alex → River View → Anytown → contract → £575/week):**

| Element | Visual clarity |
|---------|----------------|
| River View House | **Placement** panel (above chain) — presenter must connect placement to chain |
| Anytown Council | In chain and funding summary |
| £575/week | **Current rate** hero — prominent |

**Mobile:** `profile-hero` stacks to one column &lt;900px; forms in Funding tab use grids — expected single column on narrow widths.

---

## 7. Funding

**Funding Authorities list:** Page header + empty state; desktop table / mobile cards (`lg` breakpoint — tablets use cards). **Acceptable.**

**Profile funding tab:** Contract cards, rate history table, inline add contract/rate — grouped panels; template-driven forms without inline validation (errors via API only).

---

## 8. Billing

**CODE / STATIC REVIEW**

- `app-workflow-steps`: Scope → Preview → Review → Generate.
- Empty state until preview (`app-empty-state`).
- Context banner when opened from profile.
- Step 1: **Preview billing** = primary flat button; Step 4: **Generate invoices** = primary flat — **visually distinct step and label from preview**.
- Generate total uses `amount-hero`.
- Loading: `app-loading-state` during preview when no data yet.

**Friction (visual/UX, not logic):** Step 2 preview table is minimal (resident + amount); coverage in `<details>` — easy to miss on projector. Review/exceptions tables are wide — horizontal scroll on narrow viewports.

---

## 9. Invoices

**List — CODE / STATIC REVIEW**

- Filter bar (3 fields + Search + bulk actions for writers).
- Desktop table with checkbox, statuses, right-aligned totals; mobile cards with checkbox + **Open**.
- Bulk actions in filter row may wrap heavily on tablet — still usable.

**Detail — CODE / STATIC REVIEW**

- Hero: statuses, linked resident, period, **amount-hero** total.
- **Download PDF** primary; Email / payment / credit note / void grouped — **many stroked buttons of equal weight** (void separated on mobile via `btn-void`).
- Line table: 7 columns — **horizontal scroll** in `.table-container` (documented limitation).

**Hesitation:** Mark paid / Mark unpaid same style; email success uses demonstration copy (see §10).

---

## 10. Credit Notes

**CODE / STATIC REVIEW**

- **Resident autocomplete** (no raw Client ID field) — suitable for business users.
- Invoice context banner when navigated with query params from invoice detail.
- **Preview amount** in `rate-highlight` + `app-currency-display` size hero — prominent.
- Preview vs Generate: stroked Preview, flat Generate — clear.
- `app-loading-state` while preview/generate working.

**Existing credit notes table:** Seven columns, no mobile card list — scroll on phone.

**Disclaimer** under full-line preview is long but honest — may prompt technical questions (not a visual defect).

---

## 11. Reports

**CODE / STATIC REVIEW**

- Section header per report type; embedded `app-filter-bar`.
- **Run report** primary; export buttons stroked; disabled while loading.
- `app-loading-state` while generating.
- Column headers via `columnLabel()` / `SHARED_COLUMN_LABELS` — human-readable in code.
- Results: sticky header, horizontal scroll container.

**Hesitation:** Date filters always visible for all report types; no row count beyond array length.

---

## 12. Audit

**CODE / STATIC REVIEW**

- Compact filter bar (entity type free text).
- Desktop table / mobile cards — consistent with other admin lists.
- Timestamps via `displayDateTime`.

**Hesitation:** **Who** column shows raw `userId` or “System” — looks internal on a projector.

---

## 13. Care Homes

**List:** Table + mobile cards; responsive column hide for manager.

**Dashboard:** KPI grid; residents as **linked list** to `/clients/:id`; recent invoices table (3 columns) — fine on desktop; scroll on very narrow.

**Breadcrumb:** Can show care home name when dashboard loads (dynamic set in component).

---

## 14. Companies

**CODE / STATIC REVIEW**

- List: table + mobile cards; consistent panels.
- No drill-down filters to homes/clients — visual only; navigation is separate nav clicks.

---

## 15. Administration

**CODE / STATIC REVIEW**

| Screen | Visual consistency |
|--------|---------------------|
| Users | Page header; **developer-facing subtitle**; table + mobile cards |
| Organisation Settings | Inherits global `page` / `panel` (not migrated to filter-bar) |
| Funding Authorities | Polished list pattern |
| Invoice Categories / Nominal Codes / Templates | Global table styles; **no mobile cards**; no `app-filter-bar` |
| Misc Charges | **CSV column names in subtitle**; ad-hoc success banner classes (not `feedback-banner`) |
| Sage Export | Table in panel — scroll on mobile |

These screens **feel like the same app** (colors, tables, headers) but **less refined** than billing workflow — acceptable for demo if not highlighted.

---

## 16. Authentication

**Login / Change password — CODE / STATIC REVIEW**

- Centered `panel`, CH mark, outline fields, full-width primary submit.
- Login: `aria-busy`, validation `mat-error`, `app-api-error`.

**Forbidden / Not Found — CODE / STATIC REVIEW**

- `state-panel` with icon, short copy, primary CTA — aligned with design system.

**Mobile:** `p-6` padding; `max-w-md` forms — should fit 320px with box-sizing.

---

## 17. Responsive Tables

Classification: **A** = excellent · **B** = acceptable · **C** = needs improvement · **D** = broken

| Table | Location | Grade | Notes |
|-------|----------|-------|-------|
| Client list | Clients | **A** | `md` breakpoint + mobile cards |
| Invoice list | Invoices | **A** | Mobile cards + sticky desktop |
| Company list | Companies | **A** | |
| Care home list | Care homes | **A** | |
| User list | Users | **A** | |
| Audit list | Audit | **A** | |
| Funding authority list | Funding authorities | **A** | Cards below `lg` (1024px) |
| Dashboard recent invoices | Dashboard | **B** | Hides home col &lt;768px; scroll |
| Dashboard upcoming billing | Dashboard | **B** | 2 columns — fine |
| Dashboard occupancy | Dashboard | **B** | 5 columns — scroll on mobile |
| Care home recent invoices | Care home dashboard | **B** | Small table |
| Billing preview (step 2) | Billing | **B** | 2 columns |
| Billing review (step 3) | Billing | **C** | 4 columns; period strings dense on mobile |
| Billing coverage detail | Billing | **C** | Multi-line cells; wide |
| Client profile rate history | Client profile | **C** | No card fallback |
| Client profile invoices tab | Client profile | **C** | Desktop-oriented |
| Invoice detail lines | Invoice detail | **C** | 7 columns; scroll only — **acceptable for desktop demo; poor on 320px** |
| Credit note preview lines | Credit notes | **B** | 4 columns |
| Credit note existing list | Credit notes | **C** | 7 columns; no mobile cards |
| Reports results | Reports | **B** | Intentional horizontal scroll + sticky header |
| Nominal codes | Admin | **C** | Table only |
| Invoice categories | Admin | **C** | Table only |
| Invoice templates | Admin | **C** | Table only |
| Misc charges preview / batches | Misc charges | **C** | Table only |
| Sage export | Reporting | **C** | Table only |
| Platform tenants | Platform | **C** | Table only |

### C-grade detail (summary)

**Invoice detail lines (C):** On mobile, user must horizontal-scroll to see rate, nominal, amount. Important fields partially preserved in hero above table. **Recommendation (not implemented):** card per line or hide nominal/description below `md`.

**Credit note existing list (C):** Same pattern — scroll; PDF button remains in last column. **Recommendation:** mobile cards with amount + status + PDF.

**Billing review / coverage (C):** Wide period and billed-range cells wrap but feel cramped. **Recommendation:** stack coverage as cards per resident on narrow viewports.

**Admin setup tables (C):** Acceptable horizontal scroll for rare demo use; cards optional.

**No D-grade tables identified** for desktop demo; none appear structurally broken in markup (containers use `overflow-x: auto`).

---

## 18. Responsive Filters

**CODE / STATIC REVIEW**

| Screen | Desktop | Tablet | Mobile |
|--------|---------|--------|--------|
| Clients | `filter-bar__fields--3` grid | Wraps at 768px to multi-column | Single column stack |
| Invoices | 3 fields + action row | Bulk buttons wrap | Stacked fields; bulk actions may need vertical scroll |
| Audit | 1 field + actions | OK | OK |
| Reports | 4-column grid embedded | Report select spans 2 cols at `md` | Single column |
| Lists without filter-bar | N/A | — | — |

**Risk:** Invoice list filter actions (Search + 3 bulk buttons) — **many equally stroked buttons** in one row on tablet; controls do not become unusably narrow (minmax grids).

---

## 19. Responsive Forms

**CODE / STATIC REVIEW**

- Global `form-grid` / `form-actions` in `styles.scss` for configuration pages.
- Billing scope: `grid-cols-1 md:grid-cols-3` — **single column on mobile**.
- Credit note: `md:grid-cols-4` with resident spanning 2 — stacks on mobile.
- Client profile funding: Material outline fields in panels.
- Login: single column `max-w-md`.

**320–430px:** `overflow-x: hidden` on `body` reduces page-level scroll; wide Material fields may still feel tight — typical for Material on small phones.

**Dialogs:** Not exhaustively inventoried; no obvious viewport-exceeding dialog markup on demo path.

---

## 20. Responsive Cards

**CODE / STATIC REVIEW**

- KPI and panel padding consistent (`--space-4/5`).
- Mobile data cards: consistent label uppercase pattern.
- Invoice hero stacks amount below badges on &lt;640px — good hierarchy.
- Client profile hero stacks at &lt;900px.

**Minor:** Misc charges uses inline Tailwind banner instead of shared `feedback-banner` — slight visual inconsistency.

---

## 21. Typography

**CODE / STATIC REVIEW**

- Page titles: `.page-title` 1.5rem semibold.
- Section: `.text-section` / `.dashboard-section__title` (uppercase micro-labels).
- Table headers: 0.75rem uppercase muted.
- Financial: `amount-hero`, `amount-lg`, tabular nums on `.num`.

**Inconsistency:** Mix of `£{{ x | number }}` and `app-currency-display` — same family, minor weight/size differences in KPIs vs hero.

---

## 22. Colour & Contrast

**CODE / STATIC REVIEW**

- Primary green `#15803d` on white; muted text `#6b7280`.
- Status badges text-based (`app-status-badge`, `app-labeled-status`) — not colour-only.
- Success/warning/danger banners bordered + text.

**Projector:** Warning `#b45309` on `#fff7ed` — should be read; not verified live.

---

## 23. Spacing & Alignment

**CODE / STATIC REVIEW**

- Shared `--space-*` scale adopted in shell and filters.
- Tables: consistent cell padding; numeric right alignment.
- **Breaks:** Misc charges info banner; some admin pages use raw Tailwind spacing without `panel` sections for file upload.

---

## 24. Button Consistency

**CODE / STATIC REVIEW**

| Pattern | Usage |
|---------|--------|
| Primary | `mat-flat-button color="primary"` — billing generate, PDF, Search, Run report |
| Secondary | `mat-stroked-button` — most navigation and bulk actions |
| Danger | `color="warn"` on void / deactivate |

**Crowding:** Invoice detail action bar — up to 6 actions same visual tier (except void placement). Invoice list bulk actions in filter footer — same issue.

---

## 25. Loading / Empty / Error States

**CODE / STATIC REVIEW**

| State | Coverage |
|-------|----------|
| Loading | `app-loading-state` on major lists, dashboard, invoice detail, reports, credit notes (when working), billing preview |
| Empty | `app-empty-state` on lists, billing pre-preview, reports post-run |
| Error | `app-api-error` → `feedback-banner--error`, `role="alert"` |
| Success | `feedback-banner`, `alert-banner--success`, invoice info banner |

**Gaps:** No universal retry on error banners. Dashboard table rows use inline “No …” text rather than empty-state component — acceptable.

---

## 26. Accessibility

**CODE / STATIC REVIEW — report only**

**Positives:** Loading `role="status"`; errors `role="alert"`; workflow steps; labeled status groups; focus-visible on shell nav; login `aria-busy`.

**Gaps:**

- Invoice list checkbox column: empty header, no per-row `aria-label`.
- Clickable table rows (`row-clickable`) not keyboard-focusable — rely on links/Open where present.
- Audit actor display (`userId`) poor for screen reader meaning.

---

## 27. Complete Demo Workflow

**CODE / STATIC REVIEW** (data per `FINAL_DEMO_DATA_READY.md`)

| Step | Next action obvious? | Context preserved? | Prominent info? | Unfinished / internal look? |
|------|------------------------|--------------------|-----------------|---------------------------|
| Login | Yes | N/A | Branding clear | Temp password hint is operational |
| Dashboard | Yes | Section-level | KPIs + actions | Outstanding report not deep-linked to outstanding report type |
| Alex Morgan | Yes | Breadcrumb name | Rate hero | Sage in subtitle |
| Funding / £575 | Yes | Tabs + cards | Rate highlight | Care home only in placement, not chain |
| Start Billing | Yes | Banner + preselect | Scope fields | Must set August dates manually |
| August Preview | Yes | Steps + tables | Totals | Alex may show as already billed — exceptions text |
| INV-0001 | Yes | Breadcrumb number | PDF primary | Line name snapshot may say Alexa Morgan |
| PDF | Yes | — | — | — |
| Mark Paid | Yes | Status badges | — | No confirm dialog |
| Jordan Blake | Yes | Profile pattern | — | — |
| INV-0002 | Yes | — | Total hero | — |
| Email | Yes | Banner | — | **“simulated… demonstration environment”** — intentional but client may ask |
| Credit Note Preview | Yes | Banner + hero amount | £2,657.14 | Long partial-credit disclaimer |
| Reports | Yes | Titles | Formatted columns | Date fields always shown |
| Audit | Yes if TenantAdmin | — | Humanized actions | Raw user IDs in Who |

---

## 28. Client Impression

**“What would make me hesitate?”** (grounded in implementation)

1. **Email success copy** explicitly says demonstration simulation — reduces “production ready” feel (by design in Development).
2. **Invoice detail** many similar buttons — fear of mis-click (Mark paid vs unpaid).
3. **Audit “Who”** shows GUIDs — feels like a developer tool.
4. **Users page subtitle** mentions API enforcement.
5. **Misc charges subtitle** exposes CSV schema.
6. **Clients vs residents** terminology mix.
7. **INV-0001 line name** vs resident record (data snapshot) — trust question, not CSS.
8. **Funding chain** omits care home step the script may verbalize.

---

## 29. P0 Findings

*None classified P0 for **visual** demo embarrassment on a desktop-guided walkthrough.*  

(Functional/data issues — wrong credentials, missing INV-0002 — are out of scope for this visual QA; see `FINAL_DEMO_DATA_READY.md` for current data posture.)

---

## 30. P1 Findings

| ID | Finding | Evidence |
|----|---------|----------|
| P1-1 | Invoice detail action bar stacks many peer stroked actions; payment toggles equal weight | `invoice-detail.html` actions-bar |
| P1-2 | Email success banner states demonstration simulation | `invoice-detail.ts` copy when `simulated: true` |
| P1-3 | Audit list **Who** column shows raw `userId` | `audit-list.ts` `actorLabel()` |
| P1-4 | Invoice line table poor on narrow viewports (scroll-heavy) | 7-column `data-table`, no mobile fallback |
| P1-5 | Dashboard **Outstanding report** button does not preselect outstanding report type | `dashboard.html` → `/reports` only |
| P1-6 | Reports always show date range — may confuse which reports need dates | `reports.html` |
| P1-7 | Invoice list bulk actions lack select-all / guardrails; empty checkbox header | `invoice-list.html` |
| P1-8 | Content: INV-0001 line snapshot name may not match Alex Morgan on profile | `FINAL_DEMO_DATA_READY.md` note |

---

## 31. P2 Findings

| ID | Finding |
|----|---------|
| P2-1 | Funding visual chain excludes care home (placement is separate) |
| P2-2 | Client profile subtitle leads with Sage ID |
| P2-3 | Users list developer-facing subtitle |
| P2-4 | Misc charges non–design-system success banner styling |
| P2-5 | Credit note existing notes table — no mobile cards |
| P2-6 | Billing coverage hidden in `<details>` — low visual salience |
| P2-7 | Funding authority list uses `lg` breakpoint vs `md` elsewhere — inconsistent tablet behaviour |
| P2-8 | KPI cards not clickable (missed navigation affordance) |
| P2-9 | Keyboard navigation weak on row-clickable tables |
| P2-10 | Nominal codes / categories / templates — table-only on mobile |

---

## 32. P3 Findings

| ID | Finding |
|----|---------|
| P3-1 | Migrate remaining admin lists to `app-filter-bar` |
| P3-2 | Shared compact loading for inline table refresh |
| P3-3 | Dark mode |
| P3-4 | Invoice list mobile could expose funding/date in cards (hidden on desktop table cols) |
| P3-5 | Change password link in user menu |
| P3-6 | Retry affordance on `app-api-error` |

---

## 33. Recommended Fix Order

1. **Pre-demo presenter pass (no code):** Desktop 1280px rehearsal; narrate placement + funding chain; use `demo-admin1@example.com` per data doc; expect email simulation banner.
2. **P1-2:** If client must not see “simulation” wording — adjust copy or banner styling in a future pass (functional message policy, not layout).
3. **P1-1 / P1-4:** Invoice detail — group secondary actions; optional line-item cards below `md`.
4. **P1-3:** Audit actor display names (when API provides them).
5. **P2 admin polish:** Misc charges banner + subtitles; credit note list mobile pattern.
6. **P3:** Filter-bar migration on setup screens.

---

## 34. Final Verdict

| Question | Answer |
|----------|--------|
| Professional appearance? | **Yes** on primary workflow (CODE / STATIC REVIEW) |
| Visually consistent? | **Mostly** — admin/misc screens slightly behind |
| Responsive? | **Good** on list screens with mobile cards; **weak** on wide data tables (invoice lines, some billing tables) |
| Demo workflow polished? | **Yes** for guided desktop demo with prepared script and data |
| Visual regressions from beautification? | **None identified** — changes align with documented design system |
| Live UI sign-off? | **Incomplete** — requires human browser pass at target breakpoints |

### 🟡 Client-demo visual ready with minor polish

Proceed with a **desktop-first** client demonstration after a short presenter dry run. Treat tablet/phone views of invoice detail and administration lists as **out of scope** unless the client will use mobile widths.

---

*End of Final UI Visual QA Report.*

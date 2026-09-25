# Dashboard redesign — manual QA

## Prerequisites

- Tenant user with care-home data (or empty tenant for empty-state checks).
- Read-only user optional (CTA visibility).

## Steps

1. Log in as a tenant finance/operations user.
2. Open **Dashboard** (`/dashboard`).
3. Verify header shows **Operations centre**, organisation name and month/year context line, and **What needs attention today.**
4. Verify **Start billing** appears only when the user can write.
5. Under **Operations**, verify Care homes, Current residents, and Available beds match API/data.
6. Click **Care homes** and **Current residents** KPIs — confirm navigation works.
7. Under **Finance**, verify Outstanding receivables shows £ as primary value and invoice count in supporting text.
8. Verify Generated invoices and Upcoming billing values and links (receivables, invoices, billing).
9. Open **Attention needed** — verify unpaid invoices, setup hints, and billing exceptions show actionable links where applicable.
10. Click each attention action (View invoices, setup links, Review billing) — no dead ends.
11. When nothing needs attention, verify **Everything looks up to date** empty state.
12. Open **{Month} billing** panel — **Review billing** opens billing workspace (no fake progress bar).
13. In **Recent invoices**, open an invoice via invoice number — URL must use UUID/publicId, not numeric id only.
14. Verify Status and Payment columns render badges.
15. Click **View all** — invoice list opens.
16. **Recent invoices** empty state: message and **Start billing** (if permitted).
17. **Upcoming billing** table or empty state with guidance.
18. **Occupancy by home** — care home name and **View →** use `/care-homes/{publicId}/dashboard`.
19. Occupancy empty state and **Add care home** when no homes.
20. Throttle network or refresh — loading indicator under header; no full blank page.
21. Simulate API failure — error banner and **Retry** reloads data.
22. Resize to **1920px**, **1440px**, **1280px** — KPI grids reflow; tables use width; no horizontal page scroll.
23. Tablet and mobile — responsive table classes; sidebar behaviour unchanged.
24. Keyboard: tab through links/buttons; visible focus on KPI links and table links.
25. Care-home portal theme/accent unchanged on dashboard shell.
26. Platform admin without tenant still redirects away from dashboard to organisations.

## UUID verification

- Invoice detail from dashboard: `/invoices/{uuid}`.
- Care home from occupancy: `/care-homes/{uuid}/dashboard`.

## Out of scope (expected)

- **Recent activity** section not present (no dashboard activity API).
- Billing readiness percentage not shown (data not provided).

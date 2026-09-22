# Resident module — manual QA checklist

**Date:** 2026-09-22  
**Environment:** Local dev with seeded tenant data and write access.

---

## Steps

1. Open **Residents** (`/clients`).
2. Search for a resident using the search field; press Enter or wait for debounce.
3. Open a resident from the table or mobile card.
4. Verify the URL uses a **PublicId** (UUID), not a numeric id only.
5. Verify breadcrumb: **Home / Residents / {Resident name}** (no reference suffix, no UUID).
6. Verify summary strip: Funding, Current rate, Current billing period, Outstanding — with hints beneath values.
7. With no funding, verify **Funding not configured** copy in the strip and funding panel.
8. Open **Resident details** tab; confirm labels (Full name, Sage ID shows — when empty, etc.).
9. Open **Funding** tab.
10. Click **Add funding contract**; confirm route `/clients/{publicId}/funding/new`.
11. Verify required fields marked `*` and optional fields say `(optional)`; read helper text on dates.
12. Save a valid contract; confirm success toast and return to profile **Funding** tab (`?tab=funding`).
13. Click **Add rate**; confirm route `/clients/{publicId}/funding/rates/new`.
14. Complete rate form (contract, effective from, frequency, amount); save.
15. Verify rate appears in contract card and **Current rate** in summary.
16. Open **Billing** tab; read explanatory copy.
17. Click **Open billing workspace**.
18. Verify banner: **Billing for {name}**, care home · billing period, preselection message (no “Opened from profile”).
19. Verify **Company** dropdown matches resident’s company.
20. Verify **Care home** matches resident’s home.
21. Verify billing period matches prior month suggestion.
22. Run **Preview** (optional if data allows).
23. Open **Invoices** tab on profile.
24. If invoices exist, verify columns: Invoice, Period, Amount, Status, Payment status.
25. Click an invoice; verify URL `/invoices/{publicId}`.
26. Open invoice detail; use back navigation to return to resident profile.
27. Click **Edit**; verify title **Edit resident** and subtitle about updating identity/placement.
28. Verify breadcrumb: **Home / Residents / {Name} / Edit**.
29. Save a minor change; confirm navigation back to resident profile (not list).
30. Clear funding on a test resident (or use one without contracts); verify empty funding state and CTA.
31. Use resident with no invoices; verify **No invoices yet** empty state and **Start billing** CTA.
32. Tab through header actions, table rows, and icon actions using keyboard only.
33. Resize to ~375px width; confirm no horizontal page scroll; tabs and cards stack.
34. Repeat at 1024px and 1920px widths.

---

## Pass criteria

- No numeric database ids in user-visible URLs for residents or invoices.
- Billing handoff preserves resident, company, care home, and period without re-selection.
- Funding contract and rate forms live on dedicated pages (not inline on profile).
- Primary action on profile header is **Start billing**; **Edit** is secondary.

---

## Known limitations

- Funding contracts have no PublicId; `contractId` may appear only as an internal query param when preselecting a contract for **Add rate** (not shown in banners).

# Care Home UX — manual QA

**Date:** 2026-09-22

1. Log in with a user that has care-home read/write access.
2. Open **Care Homes** from the shell navigation.
3. Confirm breadcrumb: **Home / Care Homes**.
4. Open a care home dashboard (table icon or mobile card).
5. Confirm URL is `/care-homes/{uuid-or-public-id}/dashboard` — not a bare numeric segment.
6. Confirm breadcrumb shows the **care home name**, not a UUID.
7. Confirm subtitle shows `{code} · {company name}`.
8. Confirm KPI strip: Capacity, Occupied, Available, Outstanding with updated hints.
9. Confirm **Contact & location** section shows address, phone, email, manager, and status badge.
10. In **Current residents**, click a resident row.
11. Confirm navigation to `/clients/{publicId}`.
12. Press Back; use **View all →** on residents.
13. Confirm `/clients?careHomeId=…` and breadcrumb **Home / Care Homes / {name} / Residents**.
14. Return to the care home dashboard.
15. Click **Preview billing** (primary action).
16. Confirm URL includes `companyId` and `careHomeId` query params (numeric query OK).
17. Confirm billing banner: **Billing for: {care home} · {company}**.
18. Run billing preview (optional smoke) without changing scope.
19. Return to the care home dashboard.
20. In **Recent invoices**, confirm columns: Invoice, Resident, Period, Amount, Payment (when data exists).
21. Click an invoice row; confirm `/invoices/{publicId}`.
22. Use **View all →** on invoices; confirm list filtered by `careHomeId`.
23. Open **Edit home** from the dashboard.
24. Confirm breadcrumb **Home / Care Homes / {name} / Edit care home**.
25. Confirm form sections: General, Capacity, Location, Contact, Management.
26. Confirm required fields marked with * and legend at top.
27. Save changes; confirm redirect to **dashboard** and success toast.
28. Open **Add care home**; create a new home.
29. Confirm redirect to **Care Homes list**, success toast, and cleared form if you navigate back to new.
30. From dashboard with no residents, confirm empty state copy and **Add resident** (write users only).
31. From dashboard with no invoices, confirm empty state and **View billing** CTA.
32. Resize to 1280px and ~375px width; confirm no horizontal page scroll on dashboard and form.
33. Tab to resident/invoice rows and activate with Enter.
34. Confirm action buttons remain usable on mobile header wrap.

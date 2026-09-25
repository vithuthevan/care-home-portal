# Billing Workspace UX — implementation report

**Date:** 2026-09-22

## Summary

Refined the Billing Workspace so operators can quickly see scope, billable total, blocking exceptions, and generation readiness without changing billing rules, preview/generate APIs, or calculation logic.

---

## Files changed

| File | Change |
|------|--------|
| `frontend/care-home-web/src/app/features/billing/pages/billing-workspace/billing-workspace.html` | Scope read-only mode, consolidated status summary, exception panel, review table with status, generation ready/blocked states |
| `frontend/care-home-web/src/app/features/billing/pages/billing-workspace/billing-workspace.ts` | Scope editing flag, review row builder, resident/funding links, separate preview/generate loading, stepper indices |
| `frontend/care-home-web/src/app/shared/ui/workflow-steps.ts` | Compact horizontal stepper with complete / current / blocked states |
| `frontend/care-home-web/src/styles.scss` | Replaced gradient hero with neutral billing summary/exception/generation surfaces |
| `docs/ui-ux-refactor/BILLING_WORKSPACE_QA.md` | QA checklist (this pass) |
| `docs/ui-ux-refactor/BILLING_WORKSPACE_IMPLEMENTATION_REPORT.md` | This report |

---

## UI changes (by requirement)

1. **Stepper** — Lower-height horizontal indicator with connectors; Generate step marked blocked when preview cannot generate.
2. **Scope after preview** — Read-only dl grid + **Change**; full form when editing or before first preview.
3. **Exceptions** — Primary `billing-exceptions` panel with resident name, API message, **View resident** / **Fix funding** (existing routes only).
4. **Duplicate warnings** — Removed parallel success/warning banners; status lives in summary + Review/Generate sections.
5. **Review table** — Added Status column; exception-only residents included; names link via `entityRouteKey`.
6. **Zero total** — **Billable total** label + attention hint when exceptions exist.
7. **Generation** — Distinct blocked vs ready panels; disabled primary button when blocked.
8. **Dates** — Display uses `displayDate` and `→` in period summary (API dates unchanged).
9. **Billing summary** — Compact panel: period title, scope · eligible count, billable total, exception count.
10. **Visual hierarchy** — Neutral surfaces; removed gradient hero; no new colored icon set.
11. **Buttons** — Existing Material flat/stroked patterns preserved.
12. **Loading** — `isPreviewing` / `isGenerating` with loading state on generate.
13. **Errors** — Unchanged API error handling; scope not cleared on failure.
14. **Responsive** — Existing table-container / mobile card patterns retained.

---

## Business logic preserved

- Preview and generate request bodies unchanged (`companyId`, `careHomeId`, `invoiceCategoryId`, `periodStart`, `periodEnd`, `clientIds`).
- No changes to backend billing services, eligibility, funding, rates, or invoice generation.
- Exception text and `canGenerate` still driven entirely by API response.

---

## API changes

**None.**

---

## QA result

See `BILLING_WORKSPACE_QA.md`. Build: **pass** (`npm run build`).

---

## Remaining limitations

- Client lookup for links uses the same in-memory list as before (paginated fetch, max 200); residents not in that set have no profile link.
- Stepper “blocked” on Generate is UX-only; active step remains **Review** until `canGenerate` is true, then **Generate** becomes current.
- Automated E2E and API integration tests were not re-run in this task.

---

## Operator outcomes

After preview, the screen answers:

1. **What am I billing?** — Read-only scope + period summary.
2. **Who is eligible?** — Eligible resident count and review rows.
3. **What blocks billing?** — Exception panel and **Cannot bill** rows with API reasons.
4. **Billable total?** — Explicit **Billable total** metric.
5. **What to fix?** — **View resident** / **Fix funding** where supported.
6. **When to generate?** — Step 4 ready vs blocked state and enabled **Generate invoices** button.

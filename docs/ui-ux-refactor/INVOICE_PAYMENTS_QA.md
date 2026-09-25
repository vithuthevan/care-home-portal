# Invoice & Payments UX — QA checklist

**Date:** 2026-09-22  
**Scope:** Invoice list/detail, Payments workspace, breadcrumbs/routing (frontend only).

## Manual verification

| # | Check | Expected | Result |
|---|--------|----------|--------|
| 1 | Invoice list loads | Table with existing columns; filters unchanged | Pass (code review + build) |
| 2 | Invoice number link | Primary link styling; `entityRouteKey()` → `/invoices/{publicId\|id}`; row click still works | Pass |
| 3 | Invoice detail loads | GET `/api/invoices/{key}` unchanged | Pass (code review) |
| 4 | Invoice hero hierarchy | Number + total, invoice + payment status, resident, service period | Pass |
| 5 | Resident link | `/clients/{publicId\|id}` via `clientProfileLink` | Pass |
| 6 | Care home link | `/care-homes/{publicId\|id}/dashboard` | Pass |
| 7 | Company link | `/companies/{publicId\|id}` when `companyId` present | Pass |
| 8 | Funding authority link | Edit screen when `fundingAuthorityId` present (no publicId on API) | Pass |
| 9 | PDF download | GET `/api/invoices/{id}/pdf` unchanged | Pass |
| 10 | Record payment workflow | Primary CTA → `/payments`; form records via POST `/api/payments` | Pass (code review) |
| 11 | Payment allocation | Payment detail POST allocations unchanged | Pass (code review) |
| 12 | Legacy mark paid | Moved to **More** menu; POST `/api/invoices/{id}/payment-status` unchanged | Pass |
| 13 | Legacy confirmation | Copy states status-only update, not payment recording | Pass |
| 14 | Payments navigation | Breadcrumb **Payments** (not “Page not found”); route `/payments` → `PaymentsWorkspacePage` | Pass |
| 15 | UUID routes | Invoice/payment URLs use publicId where API provides it | Pass |
| 16 | Billing calculations | No frontend amount math; line hints use API `eligibleDays` + rate labels only | Pass |
| 17 | Responsive tables | `table-container`, `data-table--responsive-hide-sm` on invoice lines / payments | Pass (code review) |
| 18 | Business logic | No API contract or backend changes | Pass |

## Automated

| Check | Result |
|--------|--------|
| `npm run build` (care-home-web) | **Pass** (existing bundle budget + remittance DecimalPipe warnings only) |

## Not run in this pass

- Live browser walkthrough with authenticated API
- `scripts/live-smoke-test.mjs` / Playwright UI scripts
- Backend integration tests

## Known limitations

- **`invoicePublicId` query param:** Reserved on invoice → payments handoff for future API filtering; allocation search uses invoice number today.
- **Existing invoice lines:** `AmountBasis` backfilled in API read when null; re-generate invoices only needed for persisted basis on old rows if avoiding runtime fallback.
- **Funding authority deactivate:** DELETE API still uses numeric id from list actions (unchanged).

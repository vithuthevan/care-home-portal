# Invoice & Payments UX — implementation report

**Date:** 2026-09-22

## Summary

Refined invoice and payments UI for clearer financial hierarchy and navigation, without changing billing, payment recording, allocation, PDF, or API behavior. The main functional fix was breadcrumb routing for `/payments` (and related revenue routes), which previously fell through to **Page not found** in the shell header while the page component was already registered.

## Files changed

| File | Change |
|------|--------|
| `frontend/care-home-web/src/app/shared/ui/breadcrumb.service.ts` | Payments/revenue breadcrumb cases; invoice detail URL matches non-numeric public ids |
| `frontend/care-home-web/src/app/features/invoices/pages/invoice-list/invoice-list.html` | Invoice number as primary table link |
| `frontend/care-home-web/src/app/features/invoices/pages/invoice-detail/invoice-detail.html` | Hero hierarchy, collection panel, entity links, line hints, legacy action in overflow |
| `frontend/care-home-web/src/app/features/invoices/pages/invoice-detail/invoice-detail.ts` | Legacy confirm copy; company/funding links; line amount hint helper |
| `frontend/care-home-web/src/app/features/payments/pages/payments-workspace/payments-workspace.html` | `.page` shell, record-payment form hierarchy, responsive table |
| `frontend/care-home-web/src/app/features/payments/pages/payments-workspace/payments-workspace.ts` | Breadcrumb label on init |
| `frontend/care-home-web/src/styles.scss` | Invoice hero, collection panel, table links, entity link styling |
| `docs/ui-ux-refactor/INVOICE_PAYMENTS_QA.md` | QA checklist |
| `docs/ui-ux-refactor/INVOICE_PAYMENTS_IMPLEMENTATION_REPORT.md` | This report |

## UI changes

### Invoice list
- Invoice number is a dedicated link (stops row-click propagation on that cell) using existing `entityRouteKey`.
- Mobile cards use the same link styling.

### Invoice detail
- **Hero:** Invoice number and total on one row; invoice status + collection/payment status; resident name/reference; service period.
- **Collection status:** Primary panel with payment status, outstanding, paid, and **Record payment & allocate** CTA plus short workflow hint.
- **Legacy mark as paid:** Secondary **More** menu item with clarified confirmation (status flag only, no payment record).
- **Resident / funding cards:** Linked resident, care home, company; funding authority links to edit when id is known.
- **Invoice lines:** Per-line hint from API `eligibleDays` and rate metadata; footer note on service vs line period (no recalculation).

### Payments workspace
- Wrapped in `.page` layout consistent with other modules.
- **Record payment** section with required received date, amount, payment reference, and **Record payment** button.
- Table uses shared `table-container` / responsive column hiding.

## Routing changes

- **No `app.routes.ts` changes** — `/payments` and `/payments/:id` were already defined.
- **BreadcrumbService:** `/payments`, `/payments/:id`, and sibling revenue paths now resolve correct labels instead of **Page not found**.
- Invoice detail breadcrumbs already set in component; URL fallback now supports UUID invoice keys.

## Business logic preserved

- Invoice generation, snapshots, credit notes, PDF, void/send, payment-status POST, bulk actions unchanged.
- Payment create, allocate, reverse flows unchanged.
- No new financial fields or client-side total calculations.

## API changes

None.

## QA result

See [INVOICE_PAYMENTS_QA.md](./INVOICE_PAYMENTS_QA.md). Build: **pass**.

## Build result

```
npm run build (care-home-web) — success
Warnings: pre-existing bundle budget and RemittanceWorkspacePage DecimalPipe unused import
```

## Follow-up (limitations 1–4)

| Item | Resolution |
|------|------------|
| Company / funding public ids on invoice | `CompanyPublicId`, `FundingAuthorityPublicId` on `InvoiceDetailDto`; mapped in `InvoicesController.Get` |
| Payment handoff | Invoice CTA → `/payments?search={invoiceNumber}`; after POST payment → `/payments/{publicId}?search=…`; payment detail pre-fills search |
| Funding authority UUID routes | `PublicId` on `FundingAuthority` + migration; GET/PUT `{key}`; Angular list/edit use `entityRouteKey` |
| Line amount basis | `AmountBasis` on `InvoiceLine` (set at generate); `InvoiceLineDto.AmountBasis`; API fallback for legacy lines |

**Migration:** `20260922151354_AddFundingAuthorityPublicIdAndInvoiceLineAmountBasis` — apply before deploy.

## Remaining limitations

- Allocation still uses candidate **search** by invoice number (not `invoicePublicId` filter on API).
- Funding authority **deactivate** still calls DELETE with numeric id.

# Invoice PDF — implementation report

**Date:** 2026-09-22

## Summary

Refined the QuestPDF invoice layout for clearer hierarchy, spacing, and table readability while rendering **only** fields already on the invoice snapshot (`Invoice` / `InvoiceLine`). No billing, payment, funding, API, or database logic was changed.

## Files changed

| File | Change |
|------|--------|
| `backend/CareHome.Api/Documents/InvoicePdfService.cs` | Professional layout: header with prominent invoice number and total, resident/funding panels, readable line table, totals panel, bank details block, footer contact + page numbers |
| `CareHome.Api.Tests/InvoicePdfRenderingTests.cs` | Unit test asserting PDF embeds snapshot values unchanged |
| `docs/ui-ux-refactor/INVOICE_PDF_QA.md` | QA checklist and results |
| `docs/ui-ux-refactor/INVOICE_PDF_IMPLEMENTATION_REPORT.md` | This report |

## PDF layout improvements

- **Header:** Logo and organisation block (tenant, company, care home, template header text) on the left; **INVOICE**, large invoice number, prominent **total**, and date meta (invoice date, due date, service period) on the right; horizontal rule separator.
- **Information panels:** Side-by-side bordered **Resident** (client, reference, Sage ID) and **Funding & billing** (authority, funding code, category, care home, company).
- **Line table:** Larger base typography (9–10pt), shaded header row, increased cell padding, natural wrapping in the service/description column (period, description, optional `AmountBasis`).
- **Totals:** Right-aligned bordered panel showing **Total** from `invoice.TotalAmount` only (no subtotal/VAT — not on model).
- **Bank details:** Bordered section from snapshot fields only; omitted when all bank fields are empty.
- **Footer:** Template footer text, contact line (name, job title, email, phone), page numbers.
- **Dates/currency:** UK display dates (`d MMM yyyy`) and `en-GB` currency formatting for amounts and rates.

## Data fields preserved

All values are read from existing snapshot properties, for example:

- `SnapshotTenantName`, `SnapshotCompanyName`, `SnapshotCareHomeName`, header/footer text
- `InvoiceNumber`, `InvoiceDate`, `DueDate`, `PeriodStart`, `PeriodEnd`, `TotalAmount`
- `SnapshotFundingAuthorityName`, `SnapshotFundingAuthorityCode`, `SnapshotInvoiceCategoryName` (+ code when set)
- Line: `SnapshotClientName`, `SnapshotClientReferenceNumber`, `SnapshotSageId`, service period, `Description`, `EligibleDays`, `RateAmount`, `RateFrequency`, `SnapshotNominalCode`, `LineAmount`, `AmountBasis`
- Bank: `SnapshotBankAccountName`, `SnapshotSortCode`, `SnapshotAccountNumber`

## Financial calculations preserved

- **No** changes to invoice generation, line amount calculation, eligible days, or totals.
- PDF does **not** sum lines independently for display; total shown is `invoice.TotalAmount`.
- Optional line explanation uses stored `AmountBasis` only when populated by billing.

## Behaviour note

- Invoice PDF download **always re-renders** from the stored invoice record and overwrites the saved file path, so layout improvements appear on the next download (avoids serving stale cached PDF bytes after template code changes).

## QA result

See [INVOICE_PDF_QA.md](./INVOICE_PDF_QA.md). Build and unit test: **pass**.

## Remaining limitations

- Multi-resident invoices: summary resident panel reflects the first line only.
- No payment/collection status on PDF (not part of invoice snapshot today).
- Credit note PDF layout unchanged except shared table cell styling helpers.

## Out of scope (unchanged)

- Invoice detail web UI
- Billing workspace, payments, email send, void, API DTOs
- Credit note document structure (beyond shared cell styles)

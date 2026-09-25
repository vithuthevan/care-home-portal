# V1 Finance UX Fix Report

**Date:** 2026-09-23  
**Commercial Revenue:** Disabled (unchanged)  
**Migrations:** None

---

## 1. Issues identified

From `docs/qa/V1_FINANCE_USER_SIMULATION_AUDIT.md` and the current UI/API:

1. Invoice detail had **Mark as paid** only (More menu, “Legacy” copy) and no **Mark as unpaid** after a paid status — list bulk was the only reversal path.
2. Payment wording used **Legacy**, **Not paid**, and **Mark paid** instead of finance-facing **Paid / Unpaid / Mark as paid / Mark as unpaid**.
3. Billing overlap exceptions included **Contract IDs**.
4. Sage Export subtitle talked about **provisional mapping** / stakeholder confirmation.
5. Sage **FileMissing** was easy to read as “export did not happen”, and the API treated a recorded export with a missing CSV as a hard failure that mentioned **batch id**.
6. Several user-visible errors used **client**, invoice **line ids**, or **client ids**.
7. Manual QA did not cover the detail payment reversal path, overlap wording, or Sage file-unavailable recovery in enough detail.

---

## 2. Issues fixed

1. Invoice detail **Payment status** is the authoritative control: Unpaid → **Mark as paid**; Paid → **Mark as unpaid**, with existing confirm, loading, double-submit guard, toast, and refresh. Invoice status stays separate. **PaidAt was not added.**
2. Invoice list filter/actions and status badges use **Payment status / Paid / Unpaid / Mark as paid / Mark as unpaid**. List bulk actions now confirm and disable while updating.
3. Overlap user messages use resident, authority, category, and dates. Contract ids stay in logs and on the exception DTO for diagnostics.
4. Sage page explains Sage 50 CSV export, success, and what to do if the file is missing. “Provisional mapping” removed.
5. File-unavailable batches show **Export recorded, but the CSV file is unavailable**, keep **Invoices exported**, and offer **Retry CSV**. A recorded export with a missing file now returns the batch (HTTP 200) instead of a 400 that sounded like export failure.
6. Sage validation, credit-note, funding-overlap, and misc-charge messages no longer expose integer line/contract/client ids.
7. Manual QA tests 23–31 added; tests 14 and 17 updated.
8. Technical wording classified in `docs/qa/V1_TECHNICAL_WORDING_AUDIT.md`.

---

## 3. Files changed

### Backend

- `backend/CareHome.Funding/FundingContractOverlap.cs`
- `backend/CareHome.Billing/Billing/BillingService.cs`
- `backend/CareHome.Billing/Billing/CreditNoteService.cs`
- `backend/CareHome.Api/Export/SageExportService.cs`
- `backend/CareHome.Api/Controllers/SageExportsController.cs`
- `backend/CareHome.Api/Controllers/InvoicesController.cs`
- `backend/CareHome.Api/Services/MiscChargeImportService.cs`

### Frontend

- `frontend/care-home-web/src/app/features/invoices/pages/invoice-detail/invoice-detail.html`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-detail/invoice-detail.ts`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-list/invoice-list.html`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-list/invoice-list.ts`
- `frontend/care-home-web/src/app/shared/ui/status-badge.ts`
- `frontend/care-home-web/src/app/shared/ui/billing-exception.ts`
- `frontend/care-home-web/src/app/features/sage/pages/sage-export/sage-export.html`
- `frontend/care-home-web/src/app/features/sage/pages/sage-export/sage-export.ts`
- `frontend/care-home-web/src/app/features/users/pages/user-list/user-list.html`
- `frontend/care-home-web/src/app/features/users/pages/user-form/user-form.html`
- `frontend/care-home-web/src/app/features/misc-charges/pages/misc-charges/misc-charges.html`

### Tests & docs

- `CareHome.Api.Tests/FundingContractOverlapTests.cs`
- `docs/qa/PHASE_A3_MANUAL_QA.md`
- `docs/qa/V1_TECHNICAL_WORDING_AUDIT.md`
- `docs/qa/V1_FINANCE_UX_FIX_REPORT.md` (this file)

---

## 4. Backend changes

- Overlap **user** message rewritten; **ContractIds** still populated on the billing exception DTO; ids written to **logs**.
- Funding create/update conflict copy uses **resident** / **funding arrangement**.
- Sage CSV write failure after commit: batch remains `FileMissing`, **batch is returned** so the API does not present a recorded export as a failed export. Batch id stays in logs only.
- Sage validation errors use invoice number + description; “Sage ID is missing.”
- Credit note over-credit errors use invoice number + description.
- Misc charge duplicate/unknown-reference errors use resident name/reference.
- Payment status validation copy: “paid or unpaid” (API values remain `Paid` / `NotPaid`).
- File download 404 body explains recorded export + Retry CSV.

No billing calculation, Sage50ColumnMap, snapshot, or invariant changes. No EF migrations.

---

## 5. Frontend changes

- Invoice detail payment panel is the status interaction (confirm, loading, toast, reload).
- Payment actions removed from the More menu.
- List terminology, confirmation, and bulk loading state aligned.
- Status badge: `NotPaid` → **Unpaid**.
- Sage Export copy, columns (invoices exported vs CSV availability), FileMissing explanation, Retry CSV loading, success/warning banners.
- Billing overlap fallback copy.
- Users and misc-charges subtitles no longer say “API” / “internal IDs”.

---

## 6. Tests

- Extended `FundingContractOverlapTests` for user-message content (no “Contract IDs”).
- Manual QA: tests 23–31; tests 14 and 17 rewritten.
- Regression: frontend `npm run build`; backend `dotnet restore` / `dotnet build`; existing overlap and Phase A tests.

---

## 7. Remaining limitations

- **PaidAt** and payment history are still absent (no domain field; stop condition).
- Credit note handoff URLs may still contain numeric `invoiceId` / `clientId` (not shown as labels).
- Nominal / category / template edit URLs remain integer ids (no PublicId; migration required).
- Sage still **commits invoice export marks before writing the CSV** (unchanged recovery model).
- Misc charge CSV header remains `ClientReference` (file contract).
- Commercial Revenue payment/allocation UI is unchanged and remains gated off.
- Manual QA script is not executed in CI.

---

## 8. Items deliberately not changed

- `BillingService` rate/pro-ration/generation logic
- `Sage50ColumnMap` and snapshot behaviour
- Funding overlap **rules** (only the user-facing message)
- `DefaultInvoiceCategories` / template resolver
- Invoice numbering / document sequences
- SQL app locks for billing and Sage
- Feature flags (`COMMERCIAL_REVENUE_ENABLED`)
- Payments, Banking, Collections, Remittances, Renewals, Revenue Assurance modules
- Database schema / EF migrations
- Backend property names (`NotPaid`, `FileMissing`, `ContractIds`)

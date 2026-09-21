# Phase 3 — Accounts Receivable

## Purpose

Phase 3 introduces an authoritative **Accounts Receivable** domain so operators can answer how much has been invoiced, collected, and remains outstanding, which debt is overdue, and where finance attention is required—without duplicating billing document generation.

## Domain ownership

| Area | Owner |
|------|--------|
| Invoice creation, lines, numbering, snapshots, void, credit notes | `CareHome.Billing` |
| Funding contracts and rates | `CareHome.Funding` |
| Outstanding balance, ageing, collection status, AR queries | `CareHome.Receivables` |
| Persistence (`Invoice`, `CreditNote`, `DueDate`) | `CareHome.Data` |

An unpaid, non-void invoice **is** the receivable. No separate `Receivables` table is introduced.

## Balance formula

All amounts use `decimal` via `Money.Round`.

```
GrossInvoiceAmount     = Invoice.TotalAmount (non-void invoices only in AR queries)
EffectiveCredits       = SUM(-CreditNote.TotalAmount) for Status != Void on that invoice
AllocatedPayments      = 0 in Phase 3 (Phase 4: PaymentAllocation)
LegacyPaidAdjustment   = if Invoice.PaymentStatus == Paid and AllocatedPayments == 0
                         then PaidAmount = min(net before payments, net before payments)
                         else PaidAmount = AllocatedPayments (capped)
OutstandingAmount      = max(0, Gross - EffectiveCredits - PaidAmount)
```

## Collection status (derived, not stored)

Separate from invoice **document** status (`Generated`, `Sent`, `Void`):

| Status | Rule |
|--------|------|
| Paid | Outstanding == 0 |
| PartiallyPaid | Outstanding > 0 and PaidAmount > 0 |
| Overdue | Outstanding > 0, PaidAmount == 0, DueDate < asOfDate |
| Unpaid | Outstanding > 0, PaidAmount == 0, DueDate >= asOfDate |

Credit notes reduce outstanding; they do **not** set payment status to “Credited”. Fully credited invoices show **Paid** collection status with PaidAmount = 0 (nothing left to collect).

Invoice UI continues to store legacy `NotPaid` / `Paid` on the invoice row for manual marking until Phase 4 allocations exist.

## Ageing rules

Buckets use explicit `asOfDate` (defaults to injectable `TimeProvider` UTC date):

- DueDate >= asOfDate → **Current**
- Else days overdue = asOfDate - DueDate (calendar days)
- 1–30, 31–60, 61–90, 90+ by overdue days

Only **outstanding > 0** amounts contribute to ageing totals.

## Credit notes

Non-void credit notes reduce receivable balance. Void credit notes have no AR effect.

## Voids

Void invoices are excluded from AR queries and totals. They remain in billing/history.

## Due dates

`Invoice.DueDate` is set at generation from tenant payment terms and is authoritative for ageing. Organisation default term changes do not alter historical invoices.

## Authorization

Policy `CanViewReceivables`: TenantAdmin, Administrator, LocationManager (scoped homes), ReadOnly (scoped read). Platform admin has no tenant financial access via tenant APIs.

Manual payment status changes remain under invoice/billing write paths (`CanManageBilling` / write guard).

## Architecture

```
CareHome.Api
  → CareHome.Receivables → CareHome.Data → CareHome.Abstractions
```

`ReportService` outstanding report and dashboard outstanding KPIs consume `IReceivablesService` (single calculation path).

## Phase 4 preparation

Future balance:

```
Outstanding = Gross - EffectiveCredits - SUM(PaymentAllocation.Amount)
```

`ReceivableBalance.Calculate` already accepts `allocatedPayments`. Phase 4 will populate that from `Payment` / `PaymentAllocation` without redesigning AR DTOs or dashboards.

## Legacy compatibility

- Existing invoices keep `PaymentStatus` values `NotPaid` and `Paid`.
- AR treats `Paid` as full settlement of the **net** receivable (after credits) when no allocations exist.
- Partial payments are not modeled until Phase 4; do not infer payment rows from status alone.

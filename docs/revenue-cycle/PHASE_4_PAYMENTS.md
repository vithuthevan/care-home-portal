# Phase 4 — Payments & Payment Allocation

## Domain ownership

| Module | Owns |
|--------|------|
| **Billing** | Invoice, lines, document status, credit notes, voiding, legal snapshots |
| **Payments** | Cash receipt, payer reference, allocations, reversals, unapplied cash |
| **Receivables** | Outstanding, ageing, collection status, AR summaries |

Payments does not generate invoices or store duplicate invoice balances.

## Payment model

- **Entity:** `Payment` (`Payments` table) with `PublicId`, tenant scope, optional `FundingAuthorityId` / `CareHomeId`, `Amount`, `Currency`, `ReceivedDate`, references, `Source`, `Status`, audit fields, `RowVersion`.
- **Sources:** `Manual` (operational in Phase 4), `BankImport`, `Remittance`, `PaymentGateway` (reserved).
- **Statuses:** `Received`, `PartiallyAllocated`, `Allocated`, `Reversed` — derived from active allocation totals unless reversed.

## Allocation model

- **Entity:** `PaymentAllocation` linking one payment to one invoice with `AllocatedAmount > 0`.
- **Reversal:** allocation-level `IsReversed` flags preserve history; payment-level reversal stops settlement effect.
- **Invariants (service + DB checks):** amount > 0; sum(allocations per payment) ≤ payment amount; sum(allocations per invoice) ≤ collectible balance; same tenant; no void invoices; no allocations on reversed payments.

## Balance formula (Receivables)

```
Outstanding = max(0, Gross − EffectiveCredits − ValidPaymentAllocations)
```

`ValidPaymentAllocations` = sum of non-reversed allocations on non-reversed payments (via `IAllocatedPaymentQuery` batch projection).

## Legacy Paid compatibility

`ReceivableBalance` + `LegacyInvoicePaymentCompatibility`:

- If `Invoice.PaymentStatus == Paid` **and** active allocated payments = 0 → treat net collectible as paid (Phase 3 behavior).
- Once any real allocation exists, paid amount comes **only** from allocations (no double-count with legacy flag).

No synthetic historical payments are created.

## Concurrency

Allocation and reversal mutations run inside **SQL Server `Serializable` transactions**. Combined with validation of payment remaining capacity and invoice collectible balance, concurrent allocation attempts cannot over-allocate (see integration test `Concurrent_over_allocation_on_invoice_blocked`).

`Payment.RowVersion` supports optimistic concurrency on the payment aggregate.

## Reversal behavior

- **Payment reversal:** status → `Reversed`; allocations remain for audit but are excluded from AR via payment status filter.
- **Allocation reversal:** marks allocation reversed; payment status re-derived.

## Authorization

| Policy | Roles |
|--------|-------|
| `CanViewPayments` | TenantAdmin, Administrator, LocationManager, ReadOnly |
| `CanManagePayments` | TenantAdmin, Administrator, LocationManager |

Cross-home payments (`CareHomeId` null) are limited to tenant-wide finance roles (unrestricted care-home scope). Location managers see payments tied to their homes or with allocations on accessible invoices.

Platform admins have no tenant payment API access (existing tenant API guards).

## Audit events

- `PAYMENT_CREATED`
- `PAYMENT_ALLOCATED`
- `PAYMENT_ALLOCATION_REVERSED`
- `PAYMENT_REVERSED`

## APIs

- `POST /api/payments` — manual payment (+ optional initial allocations, atomic)
- `GET /api/payments` — list (status / unapplied filters)
- `GET /api/payments/{publicId}`
- `GET /api/payments/{publicId}/allocation-suggestions` — deterministic hints only
- `POST /api/payments/{publicId}/allocations` — multi-invoice atomic
- `POST /api/payments/{publicId}/reverse`
- `POST /api/payments/{publicId}/allocations/{allocationPublicId}/reverse`

Invoice list/detail responses include `collectionStatus`, `paidAmount`, `outstandingAmount` from receivables enrichment.

## Phase 4 payment UX (finance UI)

- **Payment detail** (`/payments/:id`): reference, payer, dates, source, amounts, status, allocations, reversal.
- **Allocation workspace:** multi-invoice selection with default `min(outstanding, unapplied)`; server validates totals.
- **Legacy mark-as-paid:** hidden once allocated payments exist; users are directed to Payments.

## Phase 5 preparation

`Payment.Source = BankImport`, `ExternalReference`, and unapplied cash are modeled for bank import / remittance matching (see `PHASE_5_BANK_RECONCILIATION.md`).

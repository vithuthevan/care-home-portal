# Phase 5 — Bank Import & Payment Reconciliation

## Domain ownership

| Module | Owns |
|--------|------|
| **Payments** | `Payment`, `PaymentAllocation`, reversals |
| **Receivables** | Outstanding, ageing, collection status (derived from allocations) |
| **Reconciliation** | Bank accounts, import batches, bank transactions, match suggestions, reconciliation decisions |

Matching and bank import logic live in `CareHome.Reconciliation`, not in `ReceivablesService`.

## Bank import

- CSV upload with preview validation (invalid rows reported per row; valid rows can be committed).
- `BankImportBatch` tracks checksum, counts, and prevents duplicate file import per bank account.
- `BankTransaction` stores normalized reference, row hash, and status.

## Duplicate handling

- **File level:** unique `(TenantId, BankAccountId, ContentChecksum)`.
- **Row level:** unique `(TenantId, BankAccountId, RowHash)` using external transaction id when present, otherwise date + amount + reference + counterparty.

## Matching model

Deterministic scoring (`ReconciliationMatchScorer`):

| Factor | Points |
|--------|--------|
| Exact amount | +50 |
| Invoice reference in bank text | +30 |
| Funder / payer hint | +10 |
| Date proximity (≤7 days) | +5 |
| Historical payer pattern (reserved) | +5 |

Confidence bands: High ≥90, Medium 70–89, Low &lt;70. Auto-confirm is **not** enabled by default.

## Payment creation

On confirmation, `PaymentService.CreateBankImportPaymentAsync` creates `Source = BankImport` with `ExternalReference = bank-txn:{publicId}` (idempotent). Allocations use existing payment allocation APIs.

## Match groups

`ReconciliationMatchGroup` + lines support one bank receipt → many invoices. `PaymentReconciliation` links bank transaction, payment, optional match group, confidence, and explanation JSON.

## Reversal

`POST /api/banking/reconciliation/transactions/{id}/reverse` reverses active allocations on the linked payment, marks reconciliation reversed, and returns the bank transaction to matching.

## Authorization

| Policy | Roles |
|--------|-------|
| `CanViewBanking` | TenantAdmin, Administrator, LocationManager, ReadOnly |
| `CanManageBanking` | TenantAdmin, Administrator, LocationManager |
| `CanReconcilePayments` | TenantAdmin, Administrator, LocationManager |

## Concurrency

- Unique constraints on payment external reference and active reconciliation per bank transaction.
- Payment allocation continues to use serializable transactions from Phase 4.

## Legacy invoice “mark as paid” transition (Phase 4 UX)

- **Legacy invoices:** `PaymentStatus = Paid` with zero allocations still respected via `LegacyInvoicePaymentCompatibility`.
- **New workflow:** Record `Payment` + `PaymentAllocation` (or bank reconciliation). UI primary action is **Payments** / **Banking**.
- **Transition:** Legacy mark-as-paid remains available only when no allocated payments exist on the invoice (hidden once real allocations apply).

## Phase 6 preparation

Remittance processing should reuse:

- `PaymentService` (create + allocate)
- `ReconciliationMatchScorer` / candidate loading patterns
- `PaymentReconciliation` linkage model

Avoid hard-coding bank-only assumptions in scorer inputs (reference + amount + payer are generic).

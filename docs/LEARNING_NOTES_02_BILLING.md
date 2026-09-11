# Learning notes 02 — Billing preview and generate

Study of: `BillingController`, `BillingService`, `RateCalculator`, `DocumentSequenceService`, `InvoiceTemplateResolver`, `docs/BILLING_ENGINE.md`.

## WHAT

Billing turns eligible resident funding into invoices for a requested period. **Preview** is read-only math. **Generate** is a transactional write that creates immutable invoice headers + lines with snapshots.

## WHY

Month-end billing must never charge the same service day twice, must survive concurrent operators, and must keep historical amounts even if master data (names, rates) changes later.

## HOW — end-to-end

### UI

`BillingWorkspacePage` → `POST /api/billing/preview` then `POST /api/billing/generate` with company/home/period (and optional category).

### Controller

`BillingController` is thin: passes `tenantContext.TenantId` into the service. Body must not supply tenant.

### `GenerateAsync` critical path

1. `BeginTransactionAsync`
2. `AcquireBillingLockAsync` — SQL `sp_getapplock` resource `billing-generate-{tenantId}`, Exclusive, 30s timeout
3. Rebuild preview via `BuildPreviewAsync` (same rules as preview endpoint)
4. Persist `BillingExceptionLog` rows for warnings/errors
5. If critical errors or no lines → commit logs only / return error
6. Group lines by `Company + CareHome + FundingAuthority + InvoiceCategory`
7. Per group: re-check `HasFinalizedOverlapAsync`, resolve template, `DocumentSequenceService.NextAsync`, insert `Invoice` + `InvoiceLine` snapshots, mark misc charges invoiced
8. Audit + `CommitAsync`

### Eligibility (BuildPreviewAsync)

For each non-archived client in scope:

1. Occupancy overlaps requested period
2. Active funding contracts (optional category filter)
3. Contract date overlap with occupancy ∩ period
4. Defensive check: overlapping Active contracts on same Authority+Category → block stream (`OVERLAPPING_FUNDING_CONTRACTS`)
5. Rate-period overlap
6. Subtract already-billed (non-void) service periods for same client+contract+category
7. Remaining fragments → preview lines; misc unbilled charges can add MISC lines

### Rate formulas (`RateCalculator`) — provisional

- Daily: `amount × days`
- Weekly: `(amount / 7) × days`
- Monthly: per calendar month `(amount / daysInMonth) × eligible days`
- Rounding: `MidpointRounding.AwayFromZero` via `Money.Round`

### Sequences (`DocumentSequenceService`)

`SELECT … WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` then increment. Safe when called inside the billing transaction. Unique `(TenantId, InvoiceNumber)` is the last line of defence.

### Snapshots

Names, codes, rates, periods are copied onto invoice/line at generate time. PDF reads snapshots (and caches file path), not live client names.

## ALTERNATIVES

| Approach | Trade-off |
|---|---|
| Nightly batch job | Better for huge orgs; worse operator feedback |
| Store calculated “billable calendar” table | Faster generate; more complexity |
| MAX(number)+1 | Race conditions under concurrency |
| Mutable invoice edits | Breaks audit / Sage / PDF history |

## TRADE-OFF MADE

Synchronous generate under a **tenant-wide** applock. Correct and simple; serializes all generates for one org during month-end.

## WHAT CAN FAIL

- Lock timeout (30s) → exception / failed generate (other run in progress)
- Missing template → full rollback of that generate attempt
- Concurrent void of an invoice while generating is **not** covered by the same lock
- Crash after commit → invoices exist; PDF created lazily on first download

## SENIOR ARCHITECT VIEW

The hard problem is set-intersection of dates under concurrency, not CRUD. Keep provisional formulas isolated (`RateCalculator`) until business sign-off. Never re-price historical invoices from live rates.

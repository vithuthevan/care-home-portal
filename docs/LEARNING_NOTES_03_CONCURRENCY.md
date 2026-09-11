# Learning notes 03 — Money-path concurrency and idempotency

Study of: `CreditNoteService`, `SageExportService`, `MiscChargeImportService`, funding-contract overlap checks, billing applock.

## WHAT

Several financial workflows look “transactional” but still allow duplicate or conflicting outcomes when two requests run at once, or when a client retries after a timeout.

## WHY

HTTP is at-least-once from the user’s perspective (double-click, refresh, reverse-proxy retry). Money systems need either strong locks + re-validation or idempotency keys.

## Gap analysis

### Billing generate — relatively strong

- `sp_getapplock` + overlap re-check + sequence UPDLOCK inside one TX.
- Residual: void / credit / Sage / misc are outside that lock.

### Credit notes — high risk (over-credit)

1. `PreviewAsync` loads remaining balance **outside** the write transaction.
2. `GenerateAsync` opens a TX, allocates a number, writes lines using **stale** preview amounts.
3. No applock; no re-check of `RemainingCreditable` under the lock.

Two concurrent generates can each credit the full remaining amount.

### Funding contracts — TOCTOU overlap

`EnsureNoOverlappingContract` reads Active contracts, then inserts. Two parallel creates for the same client+authority+category can both pass the check. Billing later blocks with `OVERLAPPING_FUNDING_CONTRACTS`, but bad data is already stored.

### Misc import confirm — duplicate rows

~~Duplicate detection runs in **preview** only. `CommitAsync` does not re-check and has no unique index on `(TenantId, ClientId, UsedDate, Description, Amount)`.~~

**Done:** `CommitAsync` re-resolves every row from Raw (ignores client-supplied ClientId/Amount), re-checks duplicates, and DB enforces unique `(TenantId, ClientId, UsedDate, Description, Amount)`.

### Sage export — double export / orphan file

~~No TX wrapping file + DB. Concurrent exports can both see unexported invoices. Crash after `SaveAsync` file but before DB mark → orphan CSV. Two `SaveChanges` calls (batch, then invoice marks) can leave a batch without links.~~

**Done:** `sage-export-{tenantId}` applock + re-query under TX; invoices marked and committed **before** CSV write. File write failure leaves batch with `FileMissing` rather than unmarked invoices.

### Email send — duplicate mail

`POST /api/invoices/{id}/send` has no idempotency key; retries resend. **Partial fix:** status update uses conditional `ExecuteUpdate` so a concurrent void is not resurrected to `Sent`.

### Idempotency — systemic

No `Idempotency-Key` header or dedupe store anywhere.

## HOW a senior would harden (Priority order)

1. Credit: applock + recalculate remaining inside TX before insert. **Done** (`CreditNoteService.GenerateAsync` + `SqlAppLock`).
2. Funding contracts: TX + applock per stream key + re-check. **Done** (`FundingContractsController` Create/Update).
3. JWT: validate SecurityStamp so password change kills old tokens. **Done** (`JwtSecurityStamp` + claim on login).
4. Tenant filter regression tests for `ForTenant` / `ITenantOwned`. **Done** (`TenantIsolationTests`).
5. Misc unique constraint + commit re-validation. **Done** (`UniqueMiscChargeDedupeIndex` + `MiscChargeImportService.CommitAsync`).
6. Sage applock + DB-before-file. **Done** (`SageExportService.ExportAsync`).
7. Later: idempotency middleware; email send dedupe keys.

## SENIOR ARCHITECT VIEW

Locks without re-validation are theatre. Re-validation without locks races. You need both for money. Idempotency is the client-facing contract when networks are unreliable.

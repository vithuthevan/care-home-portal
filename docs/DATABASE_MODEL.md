# Database model

```
Tenant (Organisation)
 └── TenantSettings (1:1)
 └── Company
      └── CareHomeLocation
           └── Client
                └── ClientFundingContract
                     └── FundingRate

FundingAuthority ──┐
InvoiceCategory ───┼── ClientFundingContract
NominalCode ───────┘
InvoiceTemplate ────┘ (optional)

Invoice  (NO ClientId on the header; SnapshotTenantName)
 └── InvoiceLine   (Client + snapshots + service period)
      └── CreditNoteLine

Invoice
 └── CreditNote
      └── CreditNoteLine

MiscChargeImportBatch
 └── MiscCharge  ──► later InvoiceLine.MiscChargeId

SageExportBatch  ◄── Invoice.SageExportBatchId

ApplicationUser (Identity, TenantId nullable for PlatformAdmin)
 └── UserCareHomeAccess ──► CareHomeLocation


AuditLog
BillingExceptionLog
EmailSendLog
DocumentSequence
```

## Notes

- Calendar/business dates use SQL `date` / .NET `DateOnly`.
- Event timestamps use `DateTimeOffset`.
- Money uses `decimal(18,2)`.
- Unique indexes are tenant-scoped: `(TenantId, Name)` / `(TenantId, Code)` / `(TenantId, SageId)` / `(TenantId, ReferenceNumber)` / `(TenantId, InvoiceNumber)` / `(TenantId, CreditNoteNumber)` / `(TenantId, DocumentType)` / `(TenantId, ClientId, UsedDate, Description, Amount)` for misc charges.
- `FundingRate` has no `TenantId`; it is scoped only via `ClientFundingContract`. Always join through the contract for tenant filtering.
- Financial FKs use `Restrict` so history cannot be cascade-deleted.
- Invoice numbering uses per-tenant `DocumentSequences` with `UPDLOCK, ROWLOCK, HOLDLOCK` — not `MAX(number)+1`.
- Audit, billing exceptions, and email logs are tenant-owned. PlatformAdmin does not list all tenants from `/api/audit`.

## Migration

Existing history is unchanged. Tenancy is `20260829072440_AddMultiTenancy`. See `docs/EXISTING_CUSTOMER_MIGRATION.md`. Do not edit older migrations.

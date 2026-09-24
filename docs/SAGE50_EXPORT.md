# Sage50 export

File-based CSV only. The API never writes to a Sage database.

## Flow

1. Operator chooses date range (optional company / care home / status).
2. `POST /api/sage-exports/preview` validates Sage ID and nominal code on **invoice line snapshots**.
3. `POST /api/sage-exports` writes a CSV via `IDocumentStore` under `tenants/{publicId}/sage-exports/` and records `SageExportBatch`. Export is tenant-scoped; download requires the same tenant plus care-home access.
4. Invoices are marked with `SageExportBatchId` / `SageExportedAt` to reduce accidental re-export.

## Column map

**PROVISIONAL** — lives in one class: `Export/Sage50ColumnMap.cs`

| Column | Source |
|---|---|
| AccountRef | Snapshot Sage ID |
| NominalCode | Snapshot nominal |
| InvoiceNumber | Invoice number |
| InvoiceDate | yyyy-MM-dd |
| Details | Line description |
| NetAmount | Net line amount after non-void credit note lines (fully credited lines omitted from CSV) |
| TaxCode | `T0` (placeholder — VAT not agreed) |
| Department | Care home code snapshot |

Final Sage50 import specification **requires stakeholder confirmation**.

## Spreadsheet formula injection

Report CSV/XLSX exports neutralize user-controlled text that starts with `=`, `+`, `-`, or `@` by prefixing `'`.

Sage machine-target fields are **not** prefixed, so Sage 50 import semantics stay intact:

| Field | Neutralized? |
|---|---|
| AccountRef, NominalCode, InvoiceNumber, InvoiceDate, NetAmount, TaxCode, Department | No |
| Details (line description) | Yes |

If a Sage ID or care-home code itself began with `=`, it would be exported unchanged. Those values are operator-controlled identifiers, not free text.

## Sign-off checklist (finance user)

Do not mark Sage as production-ready until every box is confirmed against the **target Sage 50** company.

- [ ] Sample CSV opened in Sage 50 import (not only Excel)
- [ ] AccountRef matches customer/account records
- [ ] NominalCode posts to the intended nominal
- [ ] InvoiceNumber and InvoiceDate accepted
- [ ] Details text acceptable
- [ ] NetAmount sign and decimals correct (credits if exported)
- [ ] TaxCode `T0` accepted (VAT still provisional)
- [ ] Department = care home code accepted by Sage department map
- [ ] Re-export without `includeAlreadyExported` skips already exported invoices
- [ ] Signed off by: _______________ Date: _______________


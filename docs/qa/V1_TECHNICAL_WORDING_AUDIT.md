# V1 Technical Wording Audit

**Date:** 2026-09-23  
**Scope:** Finance-user-visible copy in V1 navigation (`COMMERCIAL_REVENUE_ENABLED` = false).  
**Method:** Search of Angular templates and finance-facing API error strings for `ID` / `Id` / `UUID` / `PublicId` / `legacy` / `provisional` / `DTO` / `API` / `database` / `internal` / `technical` / `implementation` / `contractId` / `clientId` / `invoiceId` / `careHomeId`.

Classification:

- **A** — legitimate business terminology
- **B** — technical/admin terminology that a trained operator may still need
- **C** — accidental implementation leakage (changed in this batch, or remaining and called out)

Hidden Commercial Revenue modules (Payments, Banking, Collections, Remittances, Renewals, Revenue Assurance) were searched but not rewritten.

---

## Changed (category C)

| Text (before) | Where | Why C | After |
|---------------|--------|-------|--------|
| Legacy: mark as paid (status only) | Invoice detail More menu | Internal “legacy flag” language | Removed from menu; **Mark as paid** / **Mark as unpaid** on Payment status |
| Legacy: payment status flag | Invoice confirm dialog | Implementation name for a status update | **Update payment status** |
| Not paid / Not Paid | Invoice list filter, status badge | Enum-shaped label | **Unpaid** |
| Mark paid / Mark unpaid | Invoice list bulk actions | Incomplete business verbs | **Mark as paid** / **Mark as unpaid** |
| Provisional CSV mapping. Final Sage import layout needs stakeholder confirmation. | Sage Export subtitle | Engineering/stakeholder note | Explains Sage 50 CSV export |
| FileMissing | Sage batch status column | Raw enum | **Unavailable** plus “Export recorded, but the CSV file is unavailable.” |
| Contact support with the batch id | Sage export write-failure API message | Internal batch identifier | User message no longer includes batch id (id remains in server logs) |
| Contract IDs: n, m | Billing overlap exception | Database contract keys | Resident, authority, category, overlapping dates |
| This client already has an overlapping funding contract… | Funding contract save | “Client” is API language | **This resident already has an overlapping funding arrangement…** |
| Invoice {number} line {id} | Sage validation errors | Invoice line PK | Invoice number and line description |
| Sage client ID is missing | Sage validation | UI field is Sage ID | **Sage ID is missing.** |
| Credit for invoice line {id} | Credit note errors | Line PK | Invoice number and description |
| Narrow the client or period | Credit note errors | API “client” | **resident** |
| Duplicate charge … client {id} | Misc charge import | Client PK | Resident name/reference |
| Roles are enforced in the API | Users list/form | Implementation word | Roles control what the person can do |
| Use resident reference, not internal IDs | Misc charges subtitle | “internal IDs” | Use the resident reference from the resident profile |

---

## Left unchanged (A — business)

| Text | Where | Notes |
|------|--------|--------|
| Sage ID | Resident profile, invoice detail, resident form | Operator field used for Sage export |
| Invoice number | Invoices, reports, Sage | Business reference |
| Funding authority / Invoice category / Nominal code | Billing setup, funding, invoices | Domain language already used in the UI |
| Payment status | Invoices, reports | Required V1 term |
| Paid / Unpaid | Status badges after this batch | Business values; API still stores `NotPaid` |
| Resident reference | Resident profile, misc charges CSV | Operator identifier |
| Contract (dropdown label) | Add rate form | Options show authority, category, dates — not numeric ids |
| Configuration source / System default | Billing setup lists | Explains who owns the record |
| ClientReference (CSV header) | Misc charge import errors that quote expected columns | Actual file column name operators must use |

---

## Left unchanged (B — admin / operational)

| Text | Where | Notes |
|------|--------|--------|
| SMTP credentials stay in server configuration | Organisation settings | Honest ops note; not a finance posting term |
| Development note: delivery is simulated | Invoice send in dev | Dev-only |
| UsedDate must be yyyy-MM-dd | Misc charge import | CSV format the file must use |
| Expected columns: ClientReference, … | Misc charge import parse error | File contract; renaming the column would break imports |
| UUID in browser address bar | Company, care home, resident, invoice routes | Navigation key, not labelled “UUID” on screen |
| Numeric edit URLs for nominal/category/template | Billing setup | No PublicId in domain; deferred (would need migration) |
| `invoiceId` / `clientId` query params | Credit note handoff from invoice detail | Not labelled on screen; still in the URL (see remaining) |

---

## Remaining category C (not changed this batch)

| Text / surface | Why not changed |
|----------------|-----------------|
| Credit note create URL may include numeric `invoiceId` and `clientId` | Workspace still resolves numeric keys; public-id-only handoff needs a wider change than this UX batch |
| Nominal / category / template edit paths `/…/{int}` | No `PublicId` on those entities; migration required |
| Retry CSV fallback filename `sage50-{batch.Id}.csv` | Only when the stored file name is empty; filename, not a UI sentence |
| Renewals “Contract #id” | Module is gated off in V1; out of scope |

---

## Search notes

- TypeScript property names (`clientId`, `publicId`, `invoiceIds`) in component code are not user-visible unless interpolated into templates. Template interpolations of those fields were checked; none displayed raw integer contract/invoice/client ids as labels in V1 finance screens after this batch.
- Status badge maps API `NotPaid` → **Unpaid** without renaming the backend property.
- Sage batch status `FileMissing` remains the API/database value; the UI no longer prints the enum as the only explanation.

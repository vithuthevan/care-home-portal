# Identifier and UUID migration audit

## Identifier types (do not conflate)

| Type | Purpose | Example |
|------|---------|---------|
| **Internal PK** | DB joins, FK | `Company.Id` int |
| **PublicId (UUID)** | Stable external route/API key | `550e8400-e29b-41d4-a716-446655440000` |
| **Business reference** | Human/report/PDF | `RVH-001`, `INV-2026-0001` |
| **External system ID** | Sage export | `SageId` on client |

---

## Inventory

| Entity | DB PK | Business ref | API today | Route today | PublicId today |
|--------|-------|--------------|-----------|-------------|----------------|
| Tenant / Organisation | int | Name | `TenantPublicId` in JWT | platform tenants `:id` int | **Yes** `Tenant.PublicId` |
| Company | int | Name (unique/tenant) | int | `/companies/:id` | **No** → add |
| Care home | int | `Code` | int | `/care-homes/:id/...` | **No** → add |
| Resident (Client) | int | `ReferenceNumber`, `SageId` | int | `/clients/:id` | **No** → add |
| Funding authority | int | `Code` | int | edit route | No |
| Funding contract | int | — | int via client | nested API | No |
| Funding rate | int | — | int | nested API | No |
| Invoice | int | `InvoiceNumber` | int | `/invoices/:id` | **No** → add |
| Credit note | int | Number from sequence | int | query params | No |
| Invoice category | int | `Code` | int | edit | No |
| Nominal code | int | `Code` | int | edit | No |
| Invoice template | int | Name | int | edit | No |
| Payment | — | — | via invoice status | — | N/A |
| User | string (Identity) | Email | id string | users list | Identity id |
| Audit log | int | Entity id string | int | — | EntityId is string of int |
| Sage export batch | int | Batch metadata | int | — | No |
| Misc charge | int | ClientReference on row | — | — | No |

---

## Business reference generation (existing)

| Entity | Mechanism | Format |
|--------|-----------|--------|
| Resident | `ClientIdentifierService.GenerateReferenceNumberAsync` | `{CARE_HOME_CODE}-{seq}` e.g. `RVH-001` |
| Invoice | `DocumentSequence` + tenant `InvoicePrefix`, `NumberLength` | e.g. `INV-2026-0001` |
| Credit note | Same pattern with `CreditNotePrefix` | e.g. `CN-2026-0001` |
| Care home | User-entered **Code** at create | e.g. `RVH` |
| Company | **None** — do not invent `COMP-001` without schema |

---

## UUID migration strategy

### Phase A (non-destructive)

1. Add `PublicId uniqueidentifier NOT NULL DEFAULT NEWID()` to: `Company`, `CareHomes`, `Clients`, `Invoices`.
2. Unique index: `(TenantId, PublicId)`.
3. Expose `publicId` on list/detail DTOs.
4. API: `{key}` routes — if `Guid.TryParse` → lookup by `PublicId`; else int `Id`. **Always** `TenantId` filter.
5. Frontend: `entityRouteKey(dto)` → prefer `publicId` in `routerLink`; services use same key in URL.

### Phase B

- Remaining setup entities (funding authority, category, nominal, template, credit note).
- Redirect old int URLs (optional).

### Phase C

- Stop accepting int in routes (breaking — coordinate release).

### Not migrating to UUID

- Internal FK fields in request bodies (`careHomeId`, `companyId`) remain int unless API v2.
- Audit `EntityId` remains stringified int for historical rows.

---

## Dependencies

- EF migration + deploy before frontend-only UUID URLs.
- PDFs and emails already use `InvoiceNumber` / `ReferenceNumber` — unchanged.
- Sage export uses `SageId` / `ClientReference` — unchanged.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Duplicate PublicId | DB default NEWID + unique index |
| Leaking sequential ids | Stop displaying raw id in UI; show business refs |
| Platform tenant routes | Keep int or use `Tenant.PublicId` already in JWT |

# Business reference strategy

**Date:** 2026-09-21  
**Principle:** Do not collapse **technical PK**, **PublicId (route)**, **business reference**, and **external system id**.

## Identity layers (target architecture)

```
DATABASE PRIMARY KEY (int)     → joins, FKs in API bodies, bulk ids
PUBLIC UUID (PublicId)         → URLs, external stable links
BUSINESS REFERENCE             → human-facing, PDFs, reports
EXTERNAL ID                    → Sage / third-party systems
```

---

## Entity-by-entity

| Entity | Technical PK | Public route id | Business reference | External id | Recommendation |
|--------|--------------|-----------------|--------------------|-------------|----------------|
| **Organisation / Tenant** | `Tenant.Id` | `Tenant.PublicId` (JWT) | Name | — | Keep; platform routes may adopt `PublicId` later |
| **Company** | `Company.Id` | `Company.PublicId` | **Name** (unique per tenant) | — | **Do not** add `COMP-001` without product/schema approval; display **name** in UI; optional future `CompanyCode` if finance needs it |
| **Care home** | `CareHome.Id` | `PublicId` | **`Code`** (user-defined, unique/tenant) e.g. `RVH` | — | Keep **Code** as business ref; show in lists/detail; URLs use **PublicId** |
| **Resident (Client)** | `Client.Id` | `PublicId` | **`ReferenceNumber`** e.g. `{HOME_CODE}-001` | **`SageId`** | Keep generation via `ClientIdentifierService`; display reference in profile header; Sage id for export only |
| **Invoice** | `Invoice.Id` | `PublicId` | **`InvoiceNumber`** (tenant prefix + sequence) | — | Never replace with UUID in UI; URLs use PublicId |
| **Credit note** | `CreditNote.Id` | — (Phase B) | **`CreditNoteNumber`** | — | Same pattern as invoice |
| **Funding authority** | int | Phase B | **`Code`** | — | Keep code in forms/lists |
| **Invoice category** | int | Phase B | **`Code`** | — | Keep |
| **Nominal code** | int | Phase B | **`Code`** | — | Keep |
| **Invoice template** | int | Phase B | Name (+ internal id) | — | Display name; code optional future |
| **User** | Identity string | — | **Email** | — | No UUID migration required |
| **Audit** | int | — | `EntityType` + `EntityId` (stringified int) | — | Historical; do not rewrite |

---

## Display guidelines (UI)

1. **Lists:** Primary label = business reference or name (`InvoiceNumber`, `ReferenceNumber`, care home `Code`, company **name**).
2. **URLs:** `entityRouteKey(dto)` → `publicId` when present; never show raw int PK in user-visible chrome unless debugging.
3. **PDFs / emails:** Continue `InvoiceNumber`, `ReferenceNumber`, care home name/code — unchanged.
4. **Sage export:** Continue `SageId` / client reference fields — unchanged.

---

## Format examples (illustrative only — not implemented)

| Entity | Example format | Status |
|--------|----------------|--------|
| Care home | User enters `RVH` | **Live** |
| Resident | `RVH-001` | **Live** (server-generated) |
| Invoice | `INV-2026-0001` | **Live** (tenant settings) |
| Credit note | `CN-2026-0001` | **Live** |
| Company | — | **No dedicated code**; use name |

---

## Product decisions needed

1. **Company business code** — required for multi-company reporting labels, or is unique name sufficient?
2. **Show PublicId in UI** — generally **no** (technical); copy-to-clipboard only for support tools if ever needed.
3. **Audit entity links** — future: resolve int `EntityId` to business ref for display without changing stored audit rows.

---

## Anti-patterns to avoid

- Using `InvoiceNumber` as API route key (not globally unique across tenants without prefix; collision risk).
- Replacing `SageId` with UUID for export.
- Storing PublicId in Sage or PDF footers instead of business numbers.

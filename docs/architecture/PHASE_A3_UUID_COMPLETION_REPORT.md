# Phase A3 — UUID route completion audit

**Date:** 2026-09-23  
**Scope:** V1 frontend navigation (`frontend/care-home-web`)  
**Convention:** `entityRouteKey()` in `shared/routing/entity-route.ts`; route param name remains `:id`.

---

## Summary

Primary and secondary navigation for **companies, care homes, residents (clients), invoices, and funding authorities** uses `publicId` when present. **Nominal codes, invoice categories, invoice templates, and platform tenants** remain on numeric edit routes (no `PublicId` on domain entities).

Internal API calls and billing/credit-note payloads continue to use integer FKs where required.

---

## Route changes (this phase)

| Old / risky pattern | New / expected pattern | Source file | Navigation | API change | QA |
|---------------------|------------------------|-------------|------------|------------|-----|
| `/invoices/{invoiceId}` on credit note list | `/invoices/{invoicePublicId}` | `credit-note-workspace.html`, `.ts` | Credit note → invoice | `CreditNoteDto.invoicePublicId` added | Build + manual script |
| `/clients?careHomeId={int}` from care home dashboard | `/clients?careHome={uuid}` | `care-home-dashboard.ts`, `client-list.ts` | Residents filter from home | No (resolve via `GET /api/care-homes/{key}`) | Manual script |
| `/invoices?careHomeId={int}` from care home dashboard | `/invoices?careHome={uuid}` | `care-home-dashboard.ts`, `invoice-list.ts` | Invoices filter from home | No | Manual script |
| `/billing?companyId&careHomeId&clientId` from resident | `/billing?company&careHome&client` (UUID keys) | `client-profile.ts`, `billing-workspace.ts` | Start billing | No (dual-key GET) | Manual script |
| After create care home → `/care-homes` list | `/care-homes/{publicId}/dashboard` | `care-home-form.ts` | Create care home success | No | Manual script |
| Add care home from company (no company context) | `/care-homes/new?company={uuid}` | `company-detail.html` | Company → add home | No | Manual script |
| Dashboard outstanding KPI → `/invoices?paymentStatus=NotPaid` | `/reports?report=outstanding` | `dashboard.ts` | Finance KPI | No | Manual script |

---

## Already correct (Phase A / A2.5 / prior P0 fixes)

| Area | Pattern | Files |
|------|---------|-------|
| Company list/detail/edit | `entityRouteKey(company)` | `company-list`, `company-detail`, `company-form` |
| Care home list/dashboard/edit/settings | UUID segment | `care-home-list`, `care-home-dashboard`, `care-home-form`, `care-home-portal-settings` |
| Resident list/profile/edit | `getClient(key)` string route | `client-list`, `client-profile`, `client-form` |
| Invoice list/detail links | `entityRouteKey(invoice)` | `invoice-list`, `invoice-detail`, `dashboard` |
| Funding authority edit links | `entityRouteKey(authority)` | `funding-authority-list` |
| Invoice detail cross-links | `clientPublicId`, `careHomePublicId`, etc. | `invoice-detail.ts` |
| Resident profile company/home links | `companyPublicId`, `careHomePublicId` | `client-profile` |

---

## Intentionally numeric (not user-facing route segments)

| Usage | Reason |
|-------|--------|
| `clientId`, `invoiceId` in credit-note **query** params from invoice detail | API preview/generate uses `ClientId`; `InvoiceId` not on preview contract |
| `GET /api/invoices/{id}/pdf`, payment, void | Mutation sub-routes remain int (documented) |
| `GET /api/dashboard/care-homes/{int}` | Dashboard aggregation endpoint |
| `GET /api/clients/{id}/funding-contracts` | Funding sub-resource |
| Master data edit: nominal, category, template | No `PublicId` column |
| Platform tenant routes | `Tenant.PublicId` optional; admin-only |

---

## Breadcrumb fallback (`setFromUrl`)

Updated regexes in `breadcrumb.service.ts` to treat **non-numeric** `:id` segments for clients and care homes (UUID-safe fallbacks when a page does not set custom crumbs).

---

## Regression tests

- `entity-route.spec.ts` — `entityRouteKey` / `isUuidRouteKey`
- `client-profile.spec.ts` — load by UUID route key

---

PHASE A3 UUID AUDIT COMPLETE

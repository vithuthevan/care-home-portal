# Billing configuration — usage in the UI

**Phase:** A2.5  
**Date:** 2026-09-23

This document describes how master billing configuration lists expose **usage** from the API and how deactivate actions behave.

---

## Data source

All usage counts come from `MasterDataUsageService` and are returned on list/detail DTOs as `usage` (`MasterDataUsageDto`):

| Field | Meaning |
|-------|---------|
| `fundingContractCount` | `ClientFundingContracts` referencing the entity |
| `invoiceCount` | `Invoices` referencing the entity |
| `invoiceLineSnapshotCount` | Invoice lines with matching nominal snapshot (nominal codes only) |
| `miscChargeCount` | Misc charges with nominal FK (nominal codes only) |
| `invoiceTemplateCount` | Invoice templates for category (categories only) |
| `pinnedContractCount` | Contracts with explicit `InvoiceTemplateId` (templates only) |

Frontend formatters live in `frontend/care-home-web/src/app/shared/format/master-data-usage.ts`. Labels use only fields present on the DTO — no invented metrics.

---

## Funding authorities

**Route:** `/funding-authorities`

| Column | Content |
|--------|---------|
| Funding authority | Name |
| Code | Authority code |
| Type | NHS / Council / Private / Other |
| Billing frequency | From entity |
| Usage | `fundingAuthorityUsageLabel` — e.g. “Used by 12 contracts and 3 invoices” |
| Status | Active / Inactive |
| Actions | Edit, Deactivate (when active and user can write) |

**Deactivate:** Confirmation uses `deactivateFundingAuthorityMessage`, which references contract and invoice counts when non-zero. API performs soft deactivate only (no hard delete).

---

## Invoice categories

**Route:** `/invoice-categories`

| Column | Content |
|--------|---------|
| Category | Name, code, **configuration source badge** |
| Usage | `invoiceCategoryUsageLabel` — prefers invoice count, then contracts, then templates |
| Status | Active / Inactive |
| Actions | Edit; Deactivate hidden for `configurationSource === 'SystemDefault'` |

**Badges:**

- `SystemDefault` → “System default”
- Otherwise → “Organisation”

**Edit form:** System default categories have read-only code and cannot be deactivated from the UI (matches API rules).

---

## Invoice templates

**Route:** `/invoice-templates`

| Column | Content |
|--------|---------|
| Template | Name (+ category on secondary line) |
| Category | `invoiceCategoryName` |
| Usage | `invoiceTemplateUsageLabel` — pinned contracts + generated invoices |
| Status | Active / Inactive |
| Actions | Edit, Deactivate |

**Last updated:** Not shown — `InvoiceTemplate` has no audit timestamp column in the domain; historical usage is via invoice/contract FKs only.

**Deactivate:** Confirmation references invoice and pinned-contract usage. API `DELETE` sets `IsActive = false` (no physical delete).

---

## Nominal codes

Unchanged from Phase A: usage column uses `masterDataUsageLabel` (aggregated financial references).

---

## QA checklist (manual)

1. List pages show usage consistent with API JSON.
2. Deactivate confirm text mentions usage when counts &gt; 0.
3. System default categories: no deactivate icon; edit blocks code change.
4. Template with invoices: deactivate still allowed (soft); message warns about historical invoices.

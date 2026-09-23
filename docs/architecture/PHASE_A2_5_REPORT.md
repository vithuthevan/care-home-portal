# Phase A2.5 — Billing configuration usage + form semantics

**Date:** 2026-09-23  
**Commercial Revenue:** Disabled (unchanged)  
**Billing calculations / Sage / migrations:** Unchanged per scope

---

## Summary

Phase A2.5 exposes **real usage** on funding authority, invoice category, and invoice template lists; improves **configuration source** presentation for categories; adds an **A11 form field audit**; and applies a **light form semantics pass** (required/optional hints, credit note client validation, system-default category protection in the edit form).

**Status: COMPLETE** for scoped deliverables. Full A10 visual redesign and A12 UUID link audit remain deferred.

---

## Files changed

### Backend

- `backend/CareHome.Api/Services/MasterDataUsageService.cs` — `GetInvoiceTemplateUsagesAsync`
- `backend/CareHome.Api/Dtos/InvoiceTemplates/InvoiceTemplateDtos.cs` — `Usage` on DTO
- `backend/CareHome.Api/Controllers/InvoiceTemplatesController.cs` — usage on list/get/create/update

### Frontend

- `frontend/care-home-web/src/app/shared/format/master-data-usage.ts` — entity-specific usage labels + deactivate messages
- `frontend/care-home-web/src/app/shared/ui/configuration-source-badge.ts` (new)
- `frontend/care-home-web/src/styles.scss` — `.config-source-badge`
- Funding authorities: model, list (columns + usage + deactivate confirm), form optional hints
- Invoice categories: list (usage, badge, hide deactivate for system defaults), form (system default UX)
- Invoice templates: model, list (usage column), deactivate confirm
- Credit notes: workspace validation + required markers
- Nominal code form: optional description hint
- Organisation settings: required-field legend

### Tests

- `CareHome.Api.Tests/PhaseAHardeningTests.cs` — pinned contract count in `TotalFinancialReferences`

### Docs

- `docs/ux/FORM_FIELD_REQUIREMENTS_FINAL.md`
- `docs/ux/BILLING_CONFIGURATION_USAGE_UI.md`
- `docs/architecture/PHASE_A2_5_REPORT.md`

---

## API changes

| Endpoint | Change |
|----------|--------|
| `GET /api/invoice-templates` | Each item includes `usage` (`pinnedContractCount`, `invoiceCount`) |
| `GET/POST/PUT /api/invoice-templates/{id}` | `usage` on response |

Existing funding authority and invoice category usage endpoints unchanged (UI now consumes them on lists).

---

## UI changes

- Funding authority list: Code, Type, Billing frequency, **Usage**, Status, Actions; usage-aware deactivate dialog.
- Invoice category list: **Usage**, **System default / Organisation** badge; deactivate hidden for system defaults.
- Invoice template list: **Usage** column; usage-aware deactivate dialog.
- Forms: consistent `*` legend where missing; optional hints on selected fields; category edit protects system defaults.
- Credit note create: required period/reason validation before preview/generate.

---

## Validation changes

- Credit note workspace: client-side checks for period start/end, date order, and non-empty reason (aligned with `CreditNoteService`).
- Invoice category edit: disabled code control for system defaults (submitted via `getRawValue()`).
- Existing Angular validators on funding authority, residents, templates, etc. retained.

---

## Tests

- `MasterDataUsageDtoTests` / `DefaultInvoiceCategoriesTests` — pass (including new pinned-contract aggregation test).
- Manual QA scenarios documented in `BILLING_CONFIGURATION_USAGE_UI.md` (not automated E2E in this batch).

---

## Deferred issues

- Invoice template **Last updated** column (no domain field).
- Full A10 spacing/theme consistency across every V1 screen.
- A12 UUID route audit for all secondary links.
- E2E/browser automation for form QA checklist.
- Active-only contract counts for funding authorities (API reports all contracts, not status-filtered).

---

## Regression risks

- Invoice template list API payload slightly larger (usage queries on list).
- `configurationSourceLabel` text changed to “System default” / “Organisation” (was “Default setup (organisation)” for system rows).
- Credit note preview now blocked until reason and dates are filled (stricter UX; matches server).

---

## Commercial Revenue

**Disabled.** No changes to feature flags or gates.

---

PHASE A2.5 COMPLETE

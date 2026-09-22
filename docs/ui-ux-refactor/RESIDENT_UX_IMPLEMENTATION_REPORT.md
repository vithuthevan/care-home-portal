# Resident module — UX implementation report

**Date:** 2026-09-22

## Summary

Polished the resident (clients) module for clearer funding → billing → invoices workflow: dedicated funding forms, improved summary and empty states, business-language billing handoff, UUID invoice links, and form/breadcrumb consistency — without changing billing, funding, or payment APIs.

---

## Files changed

### Documentation
- `docs/ui-ux-refactor/RESIDENT_UX_AUDIT.md` (new)
- `docs/ui-ux-refactor/RESIDENT_UX_QA.md` (new)
- `docs/ui-ux-refactor/RESIDENT_UX_IMPLEMENTATION_REPORT.md` (this file)

### Frontend — new
- `frontend/care-home-web/src/app/features/clients/pages/client-funding-contract-form/client-funding-contract-form.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-funding-contract-form/client-funding-contract-form.html`
- `frontend/care-home-web/src/app/features/clients/pages/client-funding-rate-form/client-funding-rate-form.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-funding-rate-form/client-funding-rate-form.html`

### Frontend — updated
- `frontend/care-home-web/src/app/app.routes.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-profile/client-profile.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-profile/client-profile.html`
- `frontend/care-home-web/src/app/features/clients/pages/client-profile/client-profile.scss`
- `frontend/care-home-web/src/app/features/clients/pages/client-form/client-form.ts`
- `frontend/care-home-web/src/app/features/clients/pages/client-form/client-form.html`
- `frontend/care-home-web/src/app/features/billing/pages/billing-workspace/billing-workspace.html`

---

## Form changes

| Form | Change |
|------|--------|
| **Resident create/edit** | Split subtitles; required legend; sections (Placement, Identification, Resident information, Contact, Placement details, Status); optional labels on reference/Sage; edit breadcrumbs; cancel/save → profile on edit |
| **Funding contract** | New page with required/optional labels aligned to `CreateFundingContractRequest`; helper text on open-ended end date |
| **Funding rate** | New page with contract selector, effective dates, frequency, amount, optional notes; backend rules reflected in hints |

Inline **Add contract** / **Add rate** panels removed from profile **Funding** tab.

---

## Funding workflow

```
Resident profile → Funding tab → Add funding contract (/clients/{publicId}/funding/new)
  → Save → profile ?tab=funding
  → Add rate (/clients/{publicId}/funding/rates/new)
  → Save → profile ?tab=funding
```

**Limitation:** `FundingContractDto` has no `PublicId`; contract identity is chosen in the rate form, not in the URL.

---

## Billing handoff

- Profile **Start billing** / workspace links pass `companyId`, `careHomeId`, `clientId`, `clientName`, `periodStart`, `periodEnd`.
- Billing workspace banner uses **Billing for {Resident}**, care home · period, and preselection copy (no technical “opened from profile” wording).

---

## Invoice navigation

- Resident **Invoices** tab links use `entityRouteKey({ id, publicId })`.
- Table columns: Invoice, Period, Amount, Status, Payment status.

---

## Breadcrumbs

| Page | Trail |
|------|--------|
| Profile | Home / Residents / {Name} |
| Edit | Home / Residents / {Name} / Edit |
| Add contract | Home / Residents / {Name} / Add funding contract |
| Add rate | Home / Residents / {Name} / Add rate |

---

## Profile UX

- Header subtitle: `Resident · {reference} · {care home}`.
- Summary strip: funding-not-configured state, **Current rate**, **Current billing period**, semantic outstanding tone.
- Funding tab answers payer, contract period, category, nominal, status, current rate; empty state blocks billing until configured.
- Details tab business labels; optional Sage/reference shown as — when empty.

---

## Accessibility

- Table headers use `scope="col"` on profile invoice and rate tables.
- Required-field legend on forms; icon actions unchanged on list (existing aria-labels).
- Header actions remain text buttons with clear primary/secondary styling.

---

## Responsive

- Existing panel/grid/table patterns retained; profile hero and tabs use established responsive utilities (no new horizontal scroll introduced in changed templates).

---

## Build result

```
npm run build — success (exit 0)
```

Pre-existing bundle budget and unrelated `DecimalPipe` warnings only.

---

## Remaining issues

| Priority | Item |
|----------|------|
| P2 | Optional `invoiceTemplateId` on funding contracts not exposed in UI (API supports it; was not on inline form) |
| P2 | Funding contract detail/edit still API-only (no dedicated view page) |
| P2 | Route `/clients` label “Residents” vs path name unchanged for compatibility |
| P3 | Reorder resident form sections to strict spec order (Identification before Placement block) — functionally equivalent today |

---

## Business narrative

The module now reads as one story: **who is the resident → where placed → who funds → what rate → can we bill → what invoices → are they paid**, with empty states explaining blockers and next actions.

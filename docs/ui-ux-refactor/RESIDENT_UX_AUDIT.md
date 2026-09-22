# Resident module — UX audit

**Date:** 2026-09-22  
**Scope:** Residents list (`/clients`), profile, create/edit, funding, billing handoff, invoices tab, breadcrumbs, shared UI.

UI label **Resident**; route segment remains `/clients` (unchanged).

---

## 1. Current UX snapshot

| Area | Current behavior | Issues |
|------|------------------|--------|
| **List** | Search, care-home filter, pagination, icon actions (view/edit/archive), UUID row links | Solid; terminology mixed “client” in toasts/API errors |
| **Profile header** | Name; subtitle `{ref} · {care home}`; Edit (stroked) + Start billing (primary) | Missing “Resident ·” prefix; reference in breadcrumb redundant |
| **Summary strip** | Funding / Weekly rate / Current period / Outstanding | “Not set” unclear; “Weekly rate” wrong when frequency ≠ weekly; funding uses loud `finance` tone when empty |
| **Hero panels** | Placement + funding summary duplicate tab content | Acceptable but funding empty copy weak |
| **Funding tab** | Contract cards + **large inline** Add contract / Add rate forms | Violates dedicated-page workflow; no field legend for optional template |
| **Billing tab** | Copy mostly aligned | Minor wording tweaks |
| **Invoices tab** | Empty state OK | Links use **numeric** `/invoices/{id}`; columns Number/Date not Period; message could mention resident |
| **Edit form** | Single subtitle for create **and** edit | Edit shows “Create a resident record…”; no breadcrumbs; cancel → list not profile |
| **Breadcrumbs** | Residents → `{Name} — {ref}` | Spec: `{Name}` only; edit route has no trail |
| **Billing workspace** | Banner: “Opened from resident profile…” | Technical; should be business language + period |
| **Billing handoff** | `careHomeId`, `clientId`, `clientName`, period | Missing explicit `companyId` (often inferred from home) |

---

## 2. Terminology

| Context | Inconsistent | Target |
|---------|--------------|--------|
| Module | “Client” in errors/toasts | “Resident” in user-facing copy |
| Summary | “Not set”, “Weekly rate” | “Funding not configured”, “Current rate” |
| Form subtitle | Create copy on edit | Split create vs edit subtitles |
| Details tab | “Identity & placement” | Align with grouped form labels |

---

## 3. Required vs optional fields (backend)

### Resident (`CreateClientRequest` / update)

| Field | Create | Edit |
|-------|--------|------|
| careHomeId | R | R |
| firstName, lastName | R | R |
| careType, admissionDate | R | R |
| referenceNumber, sageId | O (auto-generated if blank) | R on form (existing values) |
| title, dateOfBirth, email, phone, notes | O | O |
| status, discharge*, isArchived | — | R / conditional |

### Funding contract (`CreateFundingContractRequest`)

| Field | Required |
|-------|----------|
| fundingAuthorityId, invoiceCategoryId, nominalCodeId | R |
| contractStartDate | R |
| contractEndDate, invoiceTemplateId | O |

### Funding rate (`CreateFundingRateRequest`)

| Field | Required |
|-------|----------|
| effectiveFrom, frequency, amount (> 0) | R |
| effectiveTo, notes | O |

---

## 4. Navigation & routing

| Item | Status |
|------|--------|
| Resident profile URL | PublicId via `entityRouteKey()` — OK |
| Invoice links from profile | **Numeric ID** — fix |
| Funding contract/rate | Inline on profile — move to `/clients/:id/funding/new` and `/clients/:id/funding/rates/new` |
| Contract PublicId | **Not in API** — contract routes cannot use UUID; document limitation |

---

## 5. Empty states

| Location | Gap |
|----------|-----|
| Funding (no contracts) | Message should state billing blocked + CTA |
| Funding summary (profile) | Same |
| Invoices | Add “for this resident” wording |

---

## 6. Accessibility

| Item | Gap |
|------|-----|
| Icon actions on list | Already have aria-label — OK |
| Required fields on funding forms | Add legend + `(optional)` labels |
| Tab panels | Ensure focus order after navigation from dedicated forms |

---

## 7. Planned changes (this implementation)

1. Profile header, summary strip, funding-not-configured copy, tab content polish.
2. Dedicated funding contract and rate pages; remove inline forms from profile.
3. Resident form subtitles, sections, breadcrumbs, edit navigation to profile.
4. Invoice UUID links + period column; billing handoff includes `companyId`.
5. Billing workspace resident context banner (business language).
6. QA doc + implementation report; `npm run build`.

---

## 8. Limitations

- **Funding contracts** have no `PublicId` in `FundingContractDto`; rate/contract admin uses resident PublicId routes only; contract identity is selected in-form, not in URL.
- **Invoice template** on contracts is optional API field — omitted from first-pass UI unless already present (not on inline form today).

# Care Home module — UX audit

**Date:** 2026-09-22  
**Scope:** Care home list, dashboard, create/edit, navigation to residents, billing, invoices, breadcrumbs, empty states.

---

## 1. Current Care Home UX

| Area | Current behavior |
|------|------------------|
| **List** | Page header; company-scoped variant with “Back to company”. Search + pagination. Table: Code, Care Home, Company, Capacity, Manager, Status, icon actions (dashboard, edit, deactivate). Mobile cards. Empty states for search, company, and global. |
| **Dashboard** | Loads home via `GET /api/care-homes/{key}` (PublicId). Dashboard metrics via `GET /api/dashboard/care-homes/{numericId}`. KPI strip (capacity, occupied, available, outstanding). Location panel. Residents table links to **`/clients/{numericId}`** (UUID regression). Recent invoices link to **`/invoices/{numericId}`**. “All residents” uses `?careHomeId=` (numeric query — acceptable for API). Billing handoff: `?careHomeId=` only (company not passed). Breadcrumb: Care Homes → name. Actions: Portal settings, Edit home, Preview billing (primary). |
| **Create** | Flat single-grid form. Success → **`/care-homes/{publicId}/dashboard`** (spec wants list). No form reset. No breadcrumbs on form. |
| **Edit** | Same flat form + Active checkbox. Success → `/care-homes` (spec prefers dashboard). Cancel → list. No breadcrumbs. |
| **Portal settings** | Breadcrumb includes home name link to dashboard (good). |

---

## 2. Existing fields (Care home)

**API / DB (`CareHomeLocation`):** `Id`, `PublicId`, `CompanyId`, `Code`, `Name`, `BedCapacity`, optional address/phone/email/manager fields, `IsActive`, portal theme.

**Dashboard DTO (`CareHomeDashboardDto`):** `CareHomeId`, `Name`, `Capacity`, `Occupied`, `Available`, `ManagerName`, `CurrentClients` (names only), `RecentInvoices`, `OutstandingCount`, `OutstandingAmount`.

**Recent invoice on dashboard (`RecentInvoiceDto` today):** `Id`, `PublicId`, `InvoiceNumber`, `CareHomeName`, `TotalAmount`, `Status`, `PaymentStatus` — **no** resident name or billing period (available on `Invoice` entity).

**Residents on dashboard:** Loaded separately via `ClientService.getClients` — `publicId`, names, `referenceNumber`, `status`, `careType` (no funding rate on list DTO).

---

## 3. Routes

| Route | Component | Key in URL |
|-------|-----------|------------|
| `/care-homes` | `CareHomeList` | — |
| `/companies/:id/care-homes` | `CareHomeList` | Company PublicId |
| `/care-homes/new` | `CareHomeForm` | — |
| `/care-homes/:id/dashboard` | `CareHomeDashboardPage` | PublicId |
| `/care-homes/:id/edit` | `CareHomeForm` | PublicId |
| `/care-homes/:id/settings` | `CareHomePortalSettingsPage` | PublicId |

**Preserved:** `entityRouteKey()`, Batch A/B UUID resolution on care-home API.

---

## 4. Navigation gaps

- Dashboard resident and invoice rows use numeric IDs in paths.
- Billing from dashboard omits `companyId` query (workspace can infer company from home once homes load).
- Invoice “View all” from dashboard goes to unfiltered `/clients` / no invoice filter.
- Form lacks section grouping used on resident form (`form-section`).
- Empty states on dashboard are plain text, not `app-empty-state`.

---

## 5. Improvements without backend changes

- KPI hint copy and semantic tones (neutral vs attention).
- Contact & location section labelling.
- Header action hierarchy (Preview billing primary).
- Breadcrumbs on care-home form (edit/create).
- Create redirect → `/care-homes`, reset form.
- Edit save redirect → dashboard; cancel → dashboard.
- UUID links for residents/invoices on dashboard.
- Empty states + CTAs (Add resident, View billing).
- Billing context banner when care home scoped.
- Form sections + required-field legend.
- Invoice list: honour `careHomeId` query param (API already supports filter).

---

## 6. Backend changes that unlock preferred UI

| Change | Why |
|--------|-----|
| Extend `RecentInvoiceDto` with `PeriodStart`, `PeriodEnd`, `ClientName` (from invoice lines snapshot) | Dashboard “Recent invoices” columns: Invoice, Resident, Period, Amount, Payment status — data exists on invoice, not projected today. |

No new business references or invented metrics.

---

## 7. Do not change (this initiative)

Billing calculation, funding, invoice generation, payments, auth, PublicId migration, unrelated modules.

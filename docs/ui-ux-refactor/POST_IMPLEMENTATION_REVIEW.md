# Post-implementation review

**Date:** 2026-09-21  
**Scope:** First UI/UX refactor batch (theme, PublicId, routes, forms, invoice payment).  
**Method:** Read prior audits in `docs/ui-ux-refactor/`, then inspect backend + `frontend/care-home-web` (no automated test run).

## Summary verdict

| Area | Status | Notes |
|------|--------|-------|
| 1. Theme implementation | **PASS** (with caveats) | Global accent removed; care-home portal theme via API + `CareHomePortalThemeService`. |
| 2. UUID / PublicId | **PARTIAL** | Models, migration, DTOs, list GET dual-key largely in place; EF snapshot drift; sub-resource gaps. |
| 3. Route handling (Angular) | **PARTIAL** | Primary list → detail links use `entityRouteKey()`; many secondary surfaces still use numeric `id`. |
| 4. EntityRouteKey (API) | **PARTIAL** | Parser works; update duplicate checks use parsed `id` which stays `0` for GUID routes. |
| 5. Company creation | **PASS** | Create → `/companies`; `PublicId` from model default / DB. |
| 6. Resident creation | **PASS** | Create → `/clients`; optional ref/Sage → server generation. |
| 7. Invoice routing | **PARTIAL** | Detail GET accepts UUID; list links UUID; mutations/PDF still `{id:int}`. |
| 8. Care Home routing | **PARTIAL** | List/dashboard/settings use string key; edit form uses `Number(id)`; create redirect uses numeric id. |
| 9. Payment UI | **PARTIAL** | Single panel, confirm, loading; no paid date; hero no duplicate payment badge. |
| 10. Form reset / redirect | **PARTIAL** | Company/resident create → list OK; care home create → dashboard with numeric URL; edit update → list. |

---

## 1. Theme implementation

### Implemented

- `ThemeService` no longer persists accent in `localStorage`; default green on init (`theme.service.ts`).
- Global toolbar and user menu have **no** accent picker (`app.html`).
- `CareHomePortalSettingsPage` at `/care-homes/:id/settings` with `APP_THEME_OPTIONS` and `updatePortalAppearance` API.
- `CareHomePortalThemeService` applies accent only on `/care-homes/:key/dashboard|settings`; resets to default elsewhere.
- `CareHomeLocation.PortalAccentTheme` on model + `CareHomesController` PATCH/PUT appearance endpoint.

### Gaps / risks

- Accent is applied on `document.documentElement` (global DOM). **Last-loaded care home wins** across browser tabs — not tab-isolated.
- Organisation settings still has **Primary colour** for invoice branding metadata (correct per audit); not SPA theme — **PASS** for scope, ensure users do not confuse with portal accent.

**Status: PASS** (product caveat: multi-tab).

---

## 2. UUID / PublicId implementation

### Database & models

- `Company`, `CareHomeLocation`, `Client`, `Invoice` have `Guid PublicId` with `= Guid.NewGuid()` on entity (`Models/*.cs`).
- `CareHomeDbContext` configures unique `(TenantId, PublicId)` for those entities.
- Migration `20260921100000_AddEntityPublicIdsAndPortalTheme.cs`:
  - Adds `PublicId` NOT NULL with `defaultValueSql: "NEWID()"` on all four tables.
  - Adds unique indexes `IX_*_TenantId_PublicId`.
  - Adds `PortalAccentTheme` on `CareHomes`.

### Existing data backfill

SQL Server assigns a **distinct** `NEWID()` per row when the column is added with a default. Existing demo/production rows receive `PublicId` in one migration step — **no null PublicId** after successful `Up()`.

**Status: PASS** for backfill design (assuming migration applied).

### Critical EF tooling gap

- `CareHomeDbContextModelSnapshot.cs` **does not** include `PublicId` on `Company`, `CareHomeLocation`, `Client`, or `Invoice`, or `PortalAccentTheme`.
- Migration has **no** `.Designer.cs` sibling (unlike prior migrations).

**Risk:** `dotnet ef migrations add` may generate a duplicate migration or fail until snapshot is aligned. **Status: FAIL** for migration hygiene (runtime may still work if manual SQL migration was applied).

### API

- `CompaniesController`, `CareHomesController`, `ClientsController`, `InvoicesController`: GET/PUT/DELETE by `{key}` with `EntityRouteKey.TryParse`.
- List/detail DTOs expose `PublicId`.
- `InvoicesController` sub-routes remain **`{id:int}`** only: `pdf`, `send`, `payment-status`, `void`, bulk operations.
- `FundingContractsController` uses **`clientId:int`** only — resident profile always uses numeric client id from loaded DTO (works today; inconsistent with UUID URLs).

### Frontend

- `entity-route.ts`: `entityRouteKey()` prefers `publicId`.
- Models: optional `publicId` on company, care home, client; invoices use loose typing / API camelCase.
- **Not wired:** `client-profile` loads `Number(params.get('id'))` — **breaks UUID URLs**.
- **Not wired:** `care-home-form` edit uses `Number(id)` — **breaks UUID edit URLs**.

**Status: PARTIAL**

---

## 3. Route handling

- Route patterns remain `:id` (string param) in `app.routes.ts` — acceptable during migration.
- Primary list pages updated: companies, care homes, clients, invoices (partial).
- Secondary navigation still numeric: see `UUID_ROUTE_COVERAGE.md`.

**Status: PARTIAL**

---

## 4. EntityRouteKey

```csharp
// Guid route → id stays 0
EntityRouteKey.TryParse(guidString, out publicId, out id); // id == 0
```

Used correctly for **lookup** (`publicId != default ? ... : Id == id`).

**Bug:** After lookup, several **update** paths still use route `id` for duplicate exclusion and deactivation helpers:

| Controller | Issue |
|------------|--------|
| `CompaniesController.UpdateCompany` | `existingCompany.Id != id` when `id==0` → false duplicate positives on rename. |
| `CareHomesController` update | `x.Id != id` for code uniqueness; `RejectIfDeactivatingWithCurrentClients(id)` with `id==0`. |
| `ClientsController` update | `x.Id != id` for Sage/reference duplicates → **self-match** when updating via UUID without changing Sage/ref. |

**Status: PARTIAL** (GUID updates unsafe).

---

## 5. Company creation

- `company-form.ts`: success → `router.navigate(['/companies'])`.
- API create sets name; `PublicId` from EF default on insert.
- `CreatedAtAction` still returns `new { id = company.Id }` (numeric) — cosmetic.

**Status: PASS**

---

## 6. Resident creation

- `client-form.ts`: sends `referenceNumber` / `sageId` as `undefined` when blank.
- `ClientsController` uses `ClientIdentifierService.ResolveCreateIdentifiersAsync`.
- UI hints on create for auto-generation (`client-form.html`).
- Success → `/clients`.

**Status: PASS**

---

## 7. Invoice routing

- List/detail **navigation** uses `entityRouteKey` where updated.
- `invoice-detail` load: `GET /api/invoices/${key}` — UUID OK.
- PDF, send, payment, void: `current.id` (int) — OK while user arrived via UUID detail (DTO has `id`).
- Cross-links in detail template use `clientId`, `careHomeId` (numeric) — work via API dual-key on GET only if user navigates to profile with UUID — **profile broken for UUID**.

**Status: PARTIAL**

---

## 8. Care Home routing

- List: `entityRouteKey(careHome)` for dashboard/edit.
- Dashboard/settings: `params.get('id')` as string key — **PASS**.
- Create redirect: `navigate(['/care-homes', careHome.id, 'dashboard'])` — numeric URL.
- Edit form: `Number(id)` — **FAIL** for UUID edit links from list.

**Status: PARTIAL**

---

## 9. Payment UI

- Single `payment-status-panel` on `invoice-detail.html`.
- Invoice status in hero via `labeled-status` only (no duplicate payment badge).
- Confirm dialog + `isPaying` + optimistic local update on success.
- **Missing:** “Paid on &lt;date&gt;” — no `PaidAt` (or similar) on invoice model/DTO.
- Payment API still int-only (acceptable if detail always has `id`).

**Status: PARTIAL**

---

## 10. Form reset / redirect behaviour

| Flow | Redirect | Reset / state |
|------|----------|----------------|
| Company create | `/companies` | Navigates away (form destroyed) — OK |
| Resident create | `/clients` | OK |
| Care home create | `/care-homes/:id/dashboard` | Numeric id; OK functionally |
| Resident edit | `/clients` | OK |
| Company edit | `/companies` | OK |
| Setup forms (FA, category, nominal, template, user) | respective lists | OK |

Create flows generally use `isSaving` + `finalize`; company submit not gated on `form.invalid` (only `isSaving` / `canWrite`).

**Status: PARTIAL** (validation gating minor).

---

## Phase 7 — Required field review (spot check)

| Form | vs `FORM_FIELD_REQUIREMENTS_AUDIT.md` |
|------|--------------------------------------|
| Company | Name `*` and validation — **PASS** |
| Care home | Required fields present; check submit disabled — verify `care-home-form.html` (mostly aligned) |
| Resident | Create optional Sage/ref hints — **PASS**; edit requires both — **PASS** |
| Organisation settings | No browser accent — **PASS**; `primaryColour` remains branding — **PASS** |
| Invoice / credit note / user / funding | Not fully re-audited line-by-line; no regressions observed in theme/UUID work |

Embedded funding contract/rate in `client-profile` — unchanged inline pattern per restructure audit — **PASS**.

---

## Phase 8 — Create flow review

Aligned with audit targets for **company** and **resident**. Care home intentionally goes to **dashboard** (not list) — document as product choice; list link still available.

---

## Phase 9 — Theme scope

| Check | Result |
|-------|--------|
| Portal theme only under care home settings | **PASS** |
| Platform admin no global picker | **PASS** |
| Org settings no SPA accent | **PASS** |
| Per-home persistence | **PASS** (DB field) |
| Switching homes | **PASS** (reload from API on route change) |
| Multi-tab | **PARTIAL** (shared `data-app-theme`) |

---

## Phase 10 — Payment UI

See §9. Coherent single section — **PASS**; paid date — **NOT IMPLEMENTED**.

---

## Phase 11 — Pagination

See `PAGINATION_COVERAGE.md`.

---

## Phase 12 — UI quality (remaining issues only)

- **UUID inconsistency:** mixed numeric links on dashboard, company detail, client profile, invoice detail — confusing URLs, profile breakage.
- **Care home list:** with search active, loads **full** care home collection then client-side slice — scalability smell (documented in pagination doc).
- **Invoice detail:** resident links show numeric ids in URL.
- Shell: `overflow-x: hidden` and table-shell patterns from prior audit — no new regressions verified in code read.
- Icon tooltips: improved on many action buttons; not exhaustively re-verified on every list.

---

## Phase 3–4 — Database / UUID safety (consolidated)

| Check | Result |
|-------|--------|
| PublicId unique per tenant | **PASS** (index + NEWID) |
| Indexed | **PASS** `(TenantId, PublicId)` |
| Generated on insert | **PASS** (C# default + DB default) |
| Backfill existing rows | **PASS** (migration default) |
| Never null after migration | **PASS** |
| Safe for routes | **PARTIAL** (FE/API edge cases) |
| Int PKs unchanged | **PASS** |
| Concepts not collapsed | **PASS** (PK vs PublicId vs business ref) |

---

## References

- Source-of-truth requirements: `UI_UX_REFACTOR_AUDIT.md`, `FORM_FIELD_REQUIREMENTS_AUDIT.md`, `IDENTIFIER_AND_UUID_MIGRATION_AUDIT.md`, `FORM_PAGE_RESTRUCTURE_AUDIT.md`, `UI_UX_REFACTOR_QA.md`.

# UUID route coverage audit

**Date:** 2026-09-21  
**Helper:** `frontend/care-home-web/src/app/shared/routing/entity-route.ts` → `entityRouteKey()`, `isUuidRouteKey()`.  
**Route param name:** Still `:id` in `app.routes.ts` (value may be GUID or int string).

## Convention

| Entity | Expected path segment | API GET by key |
|--------|----------------------|----------------|
| Company | `publicId` (fallback `id`) | `{key}` dual |
| Care home | `publicId` (fallback `id`) | `{key}` dual |
| Resident | `publicId` (fallback `id`) | `{key}` dual |
| Invoice | `publicId` (fallback `id`) | `{key}` dual |

---

## Coverage matrix

| Entity | Current route pattern | Expected route | UUID in primary lists? | Remaining numeric / raw id usage | Files affected |
|--------|----------------------|----------------|------------------------|-----------------------------------|----------------|
| **Company** | `/companies/:id` | `/companies/:publicId` | **Yes** (list, detail edit button) | Detail care-home table `home.id`; query `companyId: co.id` | `company-list.html`, `company-detail.html` |
| **Care home** | `/care-homes/:id/dashboard\|settings\|edit` | UUID segment | **Yes** (list) | Create redirect `careHome.id`; edit form `Number(id)`; dashboard residents/invoices `resident.id`, `row.id` | `care-home-list.*`, `care-home-form.ts`, `care-home-dashboard.html`, `dashboard.html` (`careHomeId`) |
| **Resident** | `/clients/:id`, `.../edit` | UUID segment | **Yes** (list) | Profile `Number(params.get('id'))`; edit link `client.id`; company/care home links `companyId`, `careHomeId` | `client-list.*`, `client-profile.*`, `client-form.ts` (edit key OK) |
| **Invoice** | `/invoices/:id` | UUID segment | **Yes** (list) | Detail cross-links `clientId`, `careHomeId`; dashboard/credit-note `invoice.id` / `invoiceId`; API mutations use int | `invoice-list.*`, `invoice-detail.html`, `invoice-detail.ts`, `dashboard.html`, `care-home-dashboard.html`, `client-profile.html`, `credit-note-workspace.html` |
| **Funding authority** | `/funding-authorities/:id/edit` | int (Phase B) | **No** | All edit links use `authority.id` | `funding-authority-list.html` |
| **Invoice category** | `/invoice-categories/:id/edit` | int (Phase B) | **No** | `category.id` | `invoice-category-list.html` |
| **Nominal code** | `/nominal-codes/:id/edit` | int (Phase B) | **No** | `nominalCode.id` | `nominal-code-list.html` |
| **Platform tenant** | `/platform/tenants/:id` | `Tenant.PublicId` optional | **No** | `tenant.id` | `platform-tenant-list.html` |

---

## Navigation surfaces checked

| Mechanism | UUID-aware for core four? |
|-----------|---------------------------|
| `routerLink` on primary list rows | **Mostly yes** (companies, care homes, clients, invoices) |
| `router.navigate` after create | **Partial** (care home → numeric dashboard) |
| Breadcrumbs | Static list links only — **N/A** |
| Query params | `companyId`, `careHomeId`, `clientId`, `invoiceId` remain **int** (by design for APIs) |
| Service HTTP GET | `ClientService.getClient(string\|number)` OK; profile passes **number only** |
| Invoice PDF/send/pay/void | **`/api/invoices/{int}/...`** only |
| Funding contracts | **`/api/clients/{clientId:int}/funding-contracts`** |
| Dashboard API | **`/api/dashboard/care-homes/{int}`** |

---

## Broken or risky paths (P0)

1. **`client-profile.ts`** — `Number(params.get('id'))` then `getClient(id)` → **NaN** when list links use UUID.
2. **`care-home-form.ts`** edit — `Number(id)` → **NaN** when opening edit from list with UUID.
3. **API updates by UUID** — duplicate checks use `id==0` from `EntityRouteKey` (see `POST_IMPLEMENTATION_REVIEW.md`).

---

## Legacy numeric URLs

- API dual-key GET/PUT/DELETE for companies, care homes, clients, invoices: **supported**.
- Frontend list links prefer UUID when `publicId` present in DTO: **supported** after migration + API deploy.
- Bookmarks with numeric ids: **still work** for GET if user navigates to detail pages that pass string key through (except profile/form bugs above).

---

## Recommended completion order (plan only)

1. Fix profile + care-home edit param parsing (string key end-to-end).
2. Replace remaining `routerLink` numeric segments with `entityRouteKey()` where DTO includes `publicId`.
3. Extend invoice sub-routes to `{key}` or use `publicId` in detail mutations.
4. Phase B entities (funding authority, categories, nominal, templates, credit notes).

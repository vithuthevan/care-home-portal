# Batch A implementation report

**Date:** 2026-09-21  
**Scope:** P0 only — no Batch B UX work.

## Implemented

### P0-1 — Client profile UUID routing

- `client-profile.ts` loads resident using route param **string key** (`getClient(key)`), not `Number(id)`.
- Profile **Edit** link uses `entityRouteKey(client)`.

### P0-2 — Care home edit UUID routing

- `care-home-form.ts` uses `careHomeRouteKey` string for load and update.
- `CareHomesController` **PUT** and **DELETE** changed from `{id:int}` to `{key}` with `EntityRouteKey` + tenant + care-home access checks (aligned with GET).

### P0-3 — Duplicate validation on PUT

- **Companies:** name duplicate check and deactivation helper use `company.Id` after entity resolve.
- **Clients:** Sage and reference duplicate checks use `client.Id`.
- **Care homes:** code duplicate and deactivation use `careHome.Id` after resolve.

### P0-4 — Care home create redirect

- `care-home-form.ts` navigates with `entityRouteKey(careHome)` after create.
- `CareHomesController` `CreatedAtAction` uses `key = careHome.PublicId`.

### P0-5 — EF snapshot alignment

- Removed duplicate migration `20260921044810_BatchA_AlignPublicIdSnapshot` (would have re-applied same schema with unsafe `Guid.Empty` defaults).
- Added `20260921100000_AddEntityPublicIdsAndPortalTheme.Designer.cs` linked to existing migration.
- `CareHomeDbContextModelSnapshot.cs` includes `PublicId`, indexes, and `PortalAccentTheme`.

### Minor compile fix (routing-related)

- `BreadcrumbItem.routerLink` accepts `string | readonly (string | number)[]` for UUID path breadcrumbs.

### Care home list deactivate

- Uses `entityRouteKey(careHome)` for DELETE API (supports UUID).

---

## Files changed

| Area | Files |
|------|--------|
| API | `CompaniesController.cs`, `ClientsController.cs`, `CareHomesController.cs` |
| Migrations | `20260921100000_AddEntityPublicIdsAndPortalTheme.Designer.cs` (added); removed duplicate `20260921044810_*` migration `.cs` |
| Snapshot | `CareHomeDbContextModelSnapshot.cs` (aligned via EF tooling, retained) |
| Web | `client-profile.ts`, `client-profile.html`, `care-home-form.ts`, `care-home.service.ts`, `care-home-list.ts`, `breadcrumb.service.ts` |
| Docs | `BATCH_A_QA.md`, `BATCH_A_IMPLEMENTATION_REPORT.md` |

---

## UUID routing fixed

| Flow | Status |
|------|--------|
| `/clients/{uuid}` profile load | Fixed |
| `/clients/{uuid}/edit` (form already used string key) | OK |
| `/care-homes/{uuid}/edit` load + save | Fixed |
| `/care-homes/{uuid}/dashboard` | Was OK |
| Create → `/care-homes/{uuid}/dashboard` | Fixed |

---

## Duplicate validation fixed

After resolving entity by `PublicId` or int `key`, uniqueness checks exclude **`entity.Id`**, not parsed route `id` (which is `0` for GUID keys).

---

## EF snapshot status

- Snapshot reflects `PublicId` on Company, CareHome, Client, Invoice and `PortalAccentTheme` on CareHome.
- Designer attached to `20260921100000_AddEntityPublicIdsAndPortalTheme`.
- **Do not** apply a second migration that adds the same columns.

---

## Migration status

- **Authoritative migration:** `20260921100000_AddEntityPublicIdsAndPortalTheme.cs` (uses `NEWID()` for backfill).
- If DB was created before this migration, run `dotnet ef database update` once.
- If duplicate BatchA migration was never applied to DB, no action needed (file removed).

---

## Manual QA

See `BATCH_A_QA.md`. Not executed in this implementation session.

---

## Remaining UUID gaps (documented, not Batch A)

- `company-detail.html`, `dashboard.html`, `care-home-dashboard.html`, `invoice-detail.html`, `client-profile.html` (company/care home/invoice links) — still numeric ids in many `routerLink`s.
- Invoice sub-routes API: PDF/send/payment/void remain `{id:int}` (detail uses DTO `id` after load).
- Funding contracts: `/api/clients/{clientId:int}/...` only.
- Phase B entities unchanged.

---

## Risks

- Databases that accidentally applied the removed duplicate migration would need manual reconciliation (unlikely if never deployed).
- Multi-tab portal theme still shares global `data-app-theme` (P2).

---

## Build / test status

- **Frontend:** `npm run build` succeeded (budget warnings only).
- **Backend:** `dotnet build` succeeded earlier in session; a later restore/build was slow/hung in environment — re-run locally to confirm.

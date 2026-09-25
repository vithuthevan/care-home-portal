# Next implementation plan

**Date:** 2026-09-21  
**Prerequisite:** Review `POST_IMPLEMENTATION_REVIEW.md` — **do not** start broad refactors until P0 items are agreed.

---

## P0 — Blocks safe demo / data integrity

### P0-1 — Align EF model snapshot with migration

| | |
|--|--|
| **Problem** | `20260921100000_AddEntityPublicIdsAndPortalTheme` exists but `CareHomeDbContextModelSnapshot.cs` lacks `PublicId` / `PortalAccentTheme`; no migration Designer file. |
| **Current** | Runtime DB may be correct if migration applied manually; future `ef migrations add` is unsafe. |
| **Required change** | Regenerate or hand-update snapshot + add Designer; verify single migration history. |
| **Files** | `Migrations/CareHomeDbContextModelSnapshot.cs`, `Migrations/20260921100000_*.Designer.cs` |
| **Backend** | Yes |
| **Database** | Metadata only |
| **Risk** | Low if snapshot-only fix |
| **Complexity** | S |

### P0-2 — Fix client profile route param (UUID)

| | |
|--|--|
| **Problem** | `client-profile.ts` uses `Number(params.get('id'))` — UUID list links break load. |
| **Current** | List uses `entityRouteKey(client)`; profile expects int. |
| **Required change** | Use string `key` from route; `getClient(key)`. |
| **Files** | `client-profile.ts`, optionally `client-profile.html` links to use `entityRouteKey(client)` |
| **Backend** | Already dual-key GET |
| **Database** | No |
| **Risk** | Low |
| **Complexity** | S |

### P0-3 — Fix care home edit form route param (UUID)

| | |
|--|--|
| **Problem** | `care-home-form.ts` `Number(id)` on edit. |
| **Current** | List edit links use UUID. |
| **Required change** | Store `careHomeRouteKey: string`; `getCareHome(key)`. |
| **Files** | `care-home-form.ts` |
| **Backend** | No |
| **Database** | No |
| **Risk** | Low |
| **Complexity** | S |

### P0-4 — Fix API update duplicate checks when key is GUID

| | |
|--|--|
| **Problem** | `EntityRouteKey` leaves `id=0` for GUID; `x.Id != id` excludes no row → false duplicate errors. |
| **Current** | `CompaniesController`, `CareHomesController`, `ClientsController` update paths. |
| **Required change** | After entity load, use `entity.Id` for duplicate exclusion and deactivation helpers. |
| **Files** | `CompaniesController.cs`, `CareHomesController.cs`, `ClientsController.cs` |
| **Backend** | Yes |
| **Database** | No |
| **Risk** | Medium (regression on int routes if wrong) |
| **Complexity** | S |

---

## P1 — Important product UX

### P1-1 — Secondary navigation UUID links

| | |
|--|--|
| **Problem** | Dashboard, company detail, client profile, invoice detail still link with numeric ids. |
| **Current** | Mixed URLs; bookmarks inconsistent. |
| **Required change** | Pass `publicId` where DTO available; extend invoice line DTOs with `clientPublicId` / `careHomePublicId` if needed. |
| **Files** | `dashboard.html`, `company-detail.html`, `client-profile.html`, `invoice-detail.html`, `care-home-dashboard.html`, `credit-note-workspace.html` |
| **Backend** | Optional DTO fields on invoice detail |
| **Database** | No |
| **Risk** | Low |
| **Complexity** | M |

### P1-2 — Care home create redirect uses PublicId

| | |
|--|--|
| **Problem** | After create, navigates to numeric `/care-homes/{id}/dashboard`. |
| **Required change** | `entityRouteKey(careHome)` in navigate. |
| **Files** | `care-home-form.ts` |
| **Backend** | No |
| **Database** | No |
| **Risk** | Low |
| **Complexity** | S |

### P1-3 — Invoice sub-routes accept route key (optional dual-key)

| | |
|--|--|
| **Problem** | PDF/send/payment/void use `{id:int}` only. |
| **Current** | Detail uses `current.id` from DTO — works but inconsistent. |
| **Required change** | Either document int-only for mutations or add `{key}` overloads mirroring GET. |
| **Files** | `InvoicesController.cs`, optionally `invoice-detail.ts` to use `publicId` in URLs |
| **Backend** | Yes |
| **Database** | No |
| **Risk** | Medium |
| **Complexity** | M |

### P1-4 — Payment panel: paid date

| | |
|--|--|
| **Problem** | Spec asks “Paid on &lt;date&gt;”; model has only `PaymentStatus` string. |
| **Required change** | Product decision: add `PaidAt` column + audit, or derive from audit log, or drop spec text. |
| **Files** | `Invoice.cs`, migration, `InvoiceDtos.cs`, `invoice-detail.html` |
| **Backend** | Yes |
| **Database** | Maybe |
| **Risk** | Medium (business rules) |
| **Complexity** | M |

### P1-5 — Care home list search pagination

| | |
|--|--|
| **Problem** | Search loads all care homes then slices in browser. |
| **Required change** | API search + page; remove unpaged path. |
| **Files** | `CareHomesController.cs`, `care-home-list.ts`, `care-home.service.ts` |
| **Backend** | Yes |
| **Database** | No |
| **Risk** | Low |
| **Complexity** | M |

---

## P2 — Polish

### P2-1 — Form submit disabled when invalid

| | |
|--|--|
| **Problem** | e.g. company form submit not tied to `form.invalid`. |
| **Files** | `company-form.html`, similar forms |
| **Complexity** | S |

### P2-2 — Multi-tab portal theme isolation

| | |
|--|--|
| **Problem** | `data-app-theme` on `documentElement` is global per tab. |
| **Required change** | Scope CSS variables to portal layout container, or accept limitation. |
| **Files** | `care-home-portal-theme.service.ts`, `styles.scss`, portal page templates |
| **Complexity** | M–L |

### P2-3 — UUID coverage Phase B entities

| | |
|--|--|
| **Problem** | Funding authority, category, nominal, template, credit note still int routes. |
| **Files** | Per `IDENTIFIER_AND_UUID_MIGRATION_AUDIT.md` Phase B |
| **Complexity** | L |

### P2-4 — `CreatedAtAction` use publicId in Location header

| | |
|--|--|
| **Files** | Create actions on core controllers |
| **Complexity** | S |

---

## P3 — Optional

- Redirect numeric URLs to UUID canonical URL (301-style in router guard).
- Company business code field (`COMP-001`) after product sign-off.
- Audit list: display business reference for `EntityId`.
- Reports pagination / export limits documentation in UI.

---

## Suggested batch order

1. **Batch A (P0):** Snapshot + profile + care-home edit + API duplicate id fix — no schema change except snapshot.
2. **Batch B (P1):** Secondary links + care home redirect + care home search API.
3. **Batch C (P1/P2):** Payment date decision + invoice sub-route keys.
4. **Batch D (P2+):** Phase B PublicId entities, theme scoping.

**Explicitly out of scope for next batches:** billing/funding/credit calculations, auth, tenant isolation, Sage export logic, PDF generation semantics.

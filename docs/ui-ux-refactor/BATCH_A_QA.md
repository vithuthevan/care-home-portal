# Batch A — manual QA checklist

**Scope:** P0 UUID routing, duplicate validation, EF snapshot, care home create redirect.  
**Environment:** API + web running; migration `20260921100000_AddEntityPublicIdsAndPortalTheme` applied.

---

## TEST 1 — Existing Care Home

1. Open **Care Homes** list.
2. Open an existing care home (dashboard).
3. Confirm URL segment is a **UUID** (not a small integer), when `publicId` is returned by API.
4. Click **Edit**.
5. Confirm edit page loads (no “Unable to load care home”).
6. Change a harmless field (e.g. phone).
7. **Save**.
8. Confirm success toast and return to list.
9. Confirm **no** false “code already exists” error when code unchanged.

**Expected:** Care home loads and updates through UUID route key.

---

## TEST 2 — Existing Resident

1. Open **Residents** list.
2. Open a resident (profile).
3. Confirm URL contains **UUID**.
4. **Refresh** the browser.
5. Confirm profile still loads.
6. Click **Edit**, change a harmless field, **Save**.
7. Confirm no false duplicate Sage/reference error when values unchanged.

**Expected:** Profile and update work via UUID.

---

## TEST 3 — Care Home Create

1. **Care Homes** → **Add**.
2. Complete required fields (company, code, name, bed capacity).
3. Submit.
4. Confirm redirect to **`/care-homes/{uuid}/dashboard`** (not `/care-homes/123/...`).
5. Refresh dashboard.

**Expected:** New home opens with UUID in URL.

---

## TEST 4 — Cross-tenant safety

*(Multi-tenant environment only.)*

1. Login as **Tenant A**.
2. Copy a care home UUID from Tenant B (if known).
3. Navigate to `/care-homes/{tenant-b-uuid}/dashboard`.
4. Confirm **404** or empty error — not Tenant B data.

**Expected:** No cross-tenant leakage (`TenantId` filter + access checks).

---

## TEST 5 — Legacy numeric compatibility

1. Open `/care-homes/1/dashboard` (or known int id).
2. Confirm dashboard still loads if that id exists for your tenant.
3. Open `/clients/1` if applicable.
4. API should resolve int keys where dual-key is implemented.

**Expected:** Numeric keys still work; no automatic redirect to UUID (not implemented in Batch A).

---

## TEST 6 — API duplicate validation (smoke)

1. Edit company via UUID URL; save without renaming — should succeed.
2. Edit resident via UUID; save without changing Sage/ref — should succeed.

**Expected:** No false positives from `id == 0` duplicate logic.

---

## TEST 7 — EF / database

1. Confirm `__EFMigrationsHistory` contains `20260921100000_AddEntityPublicIdsAndPortalTheme`.
2. Confirm no pending duplicate migration that re-adds `PublicId` columns.
3. Spot-check: `SELECT TOP 1 PublicId FROM Companies` — non-null unique GUIDs.

**Expected:** Schema matches model snapshot; existing rows have PublicId.

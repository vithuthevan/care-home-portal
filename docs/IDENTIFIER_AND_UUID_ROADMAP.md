# Identifier and UUID roadmap

This document describes how the Care Home Back Office handles entity identifiers today and a phased path toward UUID-style public identifiers **without committing to a database migration in UX Phase 2**.

## Current state (Phase 0 — display)

- **Database:** Operational entities (companies, care homes, residents/clients, invoices, etc.) use `int` primary keys.
- **Tenants:** Platform tenants already expose a `PublicId` (`Guid`) where multi-tenancy requires it.
- **API routes:** URLs remain numeric, e.g. `/clients/27`, `/invoices/1042`.
- **UI:** User-visible labels prefer **business references** where they exist:
  - Resident: `referenceNumber` (e.g. `RVH-001`)
  - Care home: `code`
  - Invoice: `invoiceNumber`
  - Company: name (detail route uses numeric id internally)
- **Breadcrumbs:** Pages set human labels via `BreadcrumbService.set()` (e.g. resident profile: `Alex Morgan — RVH-001`; invoice detail: invoice number).

Additive API fields (no schema change): e.g. `CompanyId` on `ClientDto` so the resident profile can link to `/companies/{id}` without exposing raw ids in copy.

## Phase 1 — PublicId column (future)

For each entity type that appears in URLs or external integrations:

1. Add nullable `PublicId` (`uniqueidentifier`) column with default `NEWSEQUENTIALID()` on insert.
2. Backfill existing rows in a controlled migration.
3. Expose `publicId` on DTOs; keep `id` for backward compatibility.

No route changes yet; clients may start storing `publicId` for new integrations.

## Phase 2 — Dual routes (future)

- Accept **either** numeric id **or** `publicId` in API and Angular routes, e.g. `/clients/{idOrPublicId}`.
- Resolver tries Guid parse first, then int.
- UI links gradually switch to `publicId` while old bookmarks keep working.

## Phase 3 — Route cutover (future)

- Default links and API documentation use `publicId` only.
- Deprecation window for numeric routes; telemetry on remaining int-route usage.
- Optional: remove int from public URLs (internal joins still use int PK).

## Out of scope for UX Phase 2

- No EF migrations for UUID columns.
- No change to billing snapshots, invoice immutability, or credit-note identifiers.
- CSV/import formats keep existing column names (e.g. misc charges `ClientReference`) while UI copy says “resident reference”.

## Manual verification hints

- Resident profile breadcrumb shows name + reference, not database id.
- Invoice and credit-note screens show invoice numbers in links and confirms.
- Numeric ids should not appear in page titles, breadcrumbs, or empty-state copy unless required for support tooling (admin-only).

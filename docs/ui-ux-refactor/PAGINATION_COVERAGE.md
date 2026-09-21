# Pagination coverage audit

**Date:** 2026-09-21  
**Legend:** **SUPPORTED BY API** | **NEEDS API PAGINATION** | **FRONTEND ONLY** | **NOT REQUIRED**

---

## Operational lists

| Page | Frontend | Backend API | Classification | Notes |
|------|----------|-------------|----------------|-------|
| **Companies** | `table-pagination`, `page`/`pageSize` | `GET /api/companies` optional paging | **SUPPORTED BY API** | |
| **Care homes** | `table-pagination` | `GET /api/care-homes` paging when no search | **SUPPORTED BY API** | With **search** text: loads **full** list via `getCareHomes()` then client slice — **FRONTEND ONLY** / **NEEDS API** search+page |
| **Residents** | `table-pagination` | `GET /api/clients` always paged | **SUPPORTED BY API** | |
| **Invoices** | `table-pagination` | `GET /api/invoices` paged | **SUPPORTED BY API** | |
| **Users** | `table-pagination` | `GET /api/users` paged | **SUPPORTED BY API** | |
| **Audit** | `table-pagination` | `GET /api/audit` paged | **SUPPORTED BY API** | |
| **Funding authorities** | `table-pagination` | Paged endpoint used | **SUPPORTED BY API** | |
| **Credit notes** | `table-pagination` in workspace | Credit notes list API with `pageSize` | **SUPPORTED BY API** | Workspace not a classic list page |
| **Sage exports** | `table-pagination` | Paged imports/batches | **SUPPORTED BY API** | |
| **Misc charges** | Paged import history | `GET /api/misc-charges/imports` | **SUPPORTED BY API** | |

---

## Setup / reference lists

| Page | Frontend pagination | Backend | Classification |
|------|---------------------|---------|----------------|
| **Invoice categories** | None | Full list | **NOT REQUIRED** (small) / document as unpaged |
| **Nominal codes** | None | Full list | **NOT REQUIRED** (small) |
| **Invoice templates** | None | Full list | **NOT REQUIRED** (small) |

---

## Reports & dashboard

| Page | Pagination | Classification | Notes |
|------|------------|----------------|-------|
| **Reports** | Filter-driven result sets; no `table-pagination` on all report types | **NEEDS API PAGINATION** for large exports | Server generates datasets; UI loads result in memory — acceptable for demo scale |
| **Dashboard** | Top-N widgets (fixed limits) | **NOT REQUIRED** | Invoice rows link out; not a full ledger |

---

## Risks

1. **Care home search mode** — unbounded download as tenant grows → prioritize server-side search + page.
2. **Client list page size** — dashboard loads up to **200** residents client-side for care home dashboard — acceptable for demo; not a list page pagination issue.

---

## Recommendations (plan only)

| Priority | Item |
|----------|------|
| P1 | Care home list: never call unpaged `getCareHomes()` for search; add API `search` + `page`/`pageSize` |
| P2 | Reports: document max rows; add paging or export-only for production |
| P3 | Setup lists: optional client-side slice if collections exceed ~100 rows |

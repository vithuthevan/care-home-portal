# Revenue module — API investigation

Date: 22 September 2026  
Scope: Accounts receivable, collections, revenue assurance (findings), contract renewals.

## Summary

| Symptom | Typical HTTP status | Root cause class |
|--------|---------------------|------------------|
| “An unexpected error occurred…” on receivables / collections | **500** JSON from `ApiExceptionHandler` | Unhandled backend exception (most often **SQL schema drift** or receivables query failure) |
| Raw **502 Bad Gateway** + nginx HTML in the UI | **502** HTML from reverse proxy | Upstream API unreachable, timeout, or proxy error — **not** application JSON |
| Payments / banking / remittances OK while AR/collections fail | Mixed | Newer revenue-cycle code paths or tables (`CollectionPolicies`, allocations join) hitting missing columns/tables |

Production does **not** apply EF migrations automatically (`Database__ApplyMigrations` defaults to `false` in `docker-compose.prod.yml`). Deploying a new API without migrating SQL produces `Invalid column name` / `Invalid object name` exceptions → generic **500** message.

---

## 1. Accounts receivable

| Item | Detail |
|------|--------|
| **Frontend route** | `/receivables` → `ReceivablesWorkspacePage` |
| **Frontend calls** | `GET /api/receivables/summary`, `GET /api/receivables/invoices`, `GET /api/receivables/funders`, `GET /api/receivables/care-homes` |
| **API controller** | `ReceivablesController` (`backend/CareHome.Api/Controllers/ReceivablesController.cs`) |
| **Service** | `ReceivablesService` (`backend/CareHome.Receivables/ReceivablesService.cs`) |
| **Auth** | Policy `CanViewReceivables` |
| **Observed failure** | HTTP **500**, body `{ message: "An unexpected error occurred. Please try again or contact support.", correlationId }` |
| **Root cause** | Unhandled exception in `ReceivablesService.LoadComputedRowsAsync` (invoice + credit note + payment allocation queries). On environments where SQL is behind the app, common exceptions include missing `PaymentAllocations` columns/tables or missing `InvoiceLines.AmountBasis` / `FundingAuthorities.PublicId` from migration `20260922151354`. Users with **no scoped care homes** previously risked empty `IN ()` filters; guarded to return an empty ledger. |
| **Fix applied** | (1) Early return when scoped care home list is empty. (2) Startup log of **pending EF migrations** (`DatabaseMigrationStartupLogger`). (3) Frontend maps generic 500 text to module copy + Retry (`getApiErrorMessage`, receivables workspace). (4) Empty invoice table only when load succeeded (`!errorMessage()`). |
| **Verification** | Local: routes respond **401** without auth on `https://129-225-96-208.sslip.io/api/receivables/summary` (route registered). Full **200** requires tenant login + SQL migrated; apply pending migrations on the API database, then retry with correlationId in API logs if 500 persists. |

---

## 2. Collections

| Item | Detail |
|------|--------|
| **Frontend route** | `/collections` |
| **API endpoint** | `GET /api/collections/dashboard` |
| **Controller / service** | `CollectionsController` → `CollectionsWorkflowService.GetDashboardAsync` |
| **Auth** | `CanViewReceivables` |
| **Observed failure** | HTTP **500** (same generic JSON message) |
| **Root cause** | Dashboard loads/creates `CollectionPolicies` then calls `IReceivablesService.GetTenantSummaryAsync`. Fails if table **`CollectionPolicies`** is missing (migration `20260921163828_CommercialRevenueCycleExtensions`) or if receivables query throws (see above). |
| **Fix applied** | Pending-migration startup log; collections workspace loading/error/empty UX; smoke script corrected from wrong path `/api/collections` → `/api/collections/dashboard`. |
| **Verification** | Live probe: `/api/collections/dashboard` → **401** without token (route exists). After migration + auth: expect **200** with policy + overdue totals (possibly zero). |

---

## 3. Revenue assurance / findings

| Item | Detail |
|------|--------|
| **Frontend route** | `/revenue-assurance` |
| **API endpoints** | `GET /api/revenue-assurance/dashboard`, `GET /api/revenue-assurance/findings?status=Open` |
| **Controller / service** | `RevenueAssuranceController` → `RevenueAssuranceService` |
| **Auth** | `CanViewFinancialReports` (list/findings), scan requires `CanManageBilling` |
| **Observed failure** | UI showed raw **502** + nginx HTML |
| **Root cause (502)** | Reverse proxy could not obtain a valid JSON response from Kestrel (upstream down, timeout, or connection reset). Angular `getApiErrorMessage` previously surfaced HTML `error.error` string verbatim. |
| **Root cause (500, when API reachable)** | Missing **`RevenueAssuranceFindings`** table or receivables/invoice line schema drift on scan/list queries. |
| **Fix applied** | `getApiErrorMessage` rejects HTML/markup and 502/503/504; findings page error card + Retry; empty state when `200` with zero rows. Smoke script path fixed (`/api/revenue-assurance/dashboard`). |
| **Verification** | Live: `/api/revenue-assurance/findings?status=Open` → **401** unauthenticated (route present). Authenticated **200** after commercial revenue migration applied. |

---

## 4. Contract renewals

| Item | Detail |
|------|--------|
| **Frontend route** | `/contract-renewals` |
| **API endpoints** | `GET /api/contract-renewals/dashboard`, `GET /api/contract-renewals` |
| **Controller / service** | `ContractRenewalsController` → `ContractRenewalWorkflowService` |
| **Auth** | `CanManageFunding` |
| **Observed failure** | Raw **502** + nginx HTML (same class as findings) |
| **Root cause (502)** | Proxy/upstream (see above). |
| **Root cause (500)** | Missing **`FundingContractRenewals`** table (migration `20260921163828_CommercialRevenueCycleExtensions`). |
| **Fix applied** | HTML-safe error handling; renewals workspace loading/error/empty states + Retry; pending-migration startup log. |
| **Verification** | Live: `/api/contract-renewals` → **401** without auth. Expect **200** + empty list when no renewal rows after migration. |

---

## Required database migrations (revenue module)

Ensure these exist in `__EFMigrationsHistory` on the **same database** the API uses:

1. `20260921152558_AddPayments` — payment allocations (receivables enrichment)
2. `20260921163828_CommercialRevenueCycleExtensions` — `CollectionPolicies`, `RevenueAssuranceFindings`, `FundingContractRenewals`, disputes, etc.
3. `20260922151354_AddFundingAuthorityPublicIdAndInvoiceLineAmountBasis` — funder `PublicId`, line `AmountBasis`

Apply with `dotnet ef database update` (project `CareHome.Data`, startup `CareHome.Api`) or set `Database__ApplyMigrations=true` once during a controlled deploy.

---

## Frontend error standard (this change)

- **`getApiErrorMessage`**: Never returns HTML, nginx/proxy text, or generic server `"An unexpected error occurred…"` — uses module fallback instead.
- **`app-api-error`**: Optional title, message, **Retry** button.
- **Empty states**: Shown only after successful API responses with zero rows, not when `errorMessage` is set.

---

## Ops checklist when 500 persists

1. Copy `correlationId` from the error response (browser Network tab).
2. On the API host, search logs: `Unhandled exception for GET /api/... CorrelationId=...`
3. Run:

   ```sql
   SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
   ```

4. Apply pending migrations, restart API, retest.

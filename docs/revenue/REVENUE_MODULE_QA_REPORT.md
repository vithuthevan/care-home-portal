# Revenue module — QA report

Date: 22 September 2026  
Environment notes: Local API build verification; live route probe on `https://129-225-96-208.sslip.io` (unauthenticated). Full authenticated UI pass requires tenant credentials on the target environment.

## Results table

| Module | Route | Primary API | Result | Notes |
|--------|-------|-------------|--------|-------|
| Accounts receivable | `/receivables` | `GET /api/receivables/summary`, `GET /api/receivables/invoices` | **Fix applied — retest with tenant login** | Generic 500 mapped to module error + Retry; empty state gated on success. Backend: scoped-home guard + migration startup log. Live route returns **401** without auth (registered). |
| Payments | `/payments` | `GET /api/payments` | **Pass (reported)** | No change in this task; user confirmed working. |
| Banking | `/banking` | `GET /api/banking/accounts` | **Pass (reported)** | User confirmed working. |
| Remittances | `/remittances` | `GET /api/remittances` | **Pass (reported)** | User confirmed working. |
| Collections | `/collections` | `GET /api/collections/dashboard` | **Fix applied — retest with tenant login** | Loading/error/empty UX added. Live route **401** unauthenticated. Depends on `CollectionPolicies` + receivables SQL. |
| Disputes | `/disputes` | `GET /api/disputes` | **Pass (reported)** | User confirmed empty table OK. |
| Revenue assurance / findings | `/revenue-assurance` | `GET /api/revenue-assurance/findings?status=Open` | **Fix applied — retest with tenant login** | HTML/502 no longer shown in UI. Error card + Retry. Live route **401** unauthenticated. |
| Contract renewals | `/contract-renewals` | `GET /api/contract-renewals` | **Fix applied — retest with tenant login** | HTML/502 no longer shown in UI. Loading/error/empty + Retry. Live route **401** unauthenticated. |

## Regression scope (unchanged by design)

- Billing generate/preview, invoice calculations, payment allocation, credit notes, banking import/reconciliation, remittance upload, dispute workflows, revenue assurance rule logic — **not modified** except shared error handling and receivables scoped-home guard.

## Automated / script checks

| Check | Outcome |
|-------|---------|
| `dotnet build` CareHome.Api | Run after changes (local) |
| `scripts/live-smoke-test.mjs` paths for collections & revenue assurance | **Updated** to correct dashboard routes |
| Live `/health/ready` | **200 Healthy** (22 Sep 2026 probe) |

## Recommended manual retest (tenant admin)

1. Sign in as **TenantAdmin** (not PlatformAdmin).
2. Visit all eight routes above.
3. Confirm: no raw HTML in error banners; Retry reloads data; zero-data pages show empty states, not errors.
4. Confirm billing → invoice → payment flow still works.

## Blockers for “Pass” on failing modules in production

If authenticated calls still return **500**, apply pending EF migrations on production SQL (see `REVENUE_MODULE_API_INVESTIGATION.md`) before re-testing. If **502** persists with Retry, inspect reverse proxy → Kestrel connectivity and API process health during the request.

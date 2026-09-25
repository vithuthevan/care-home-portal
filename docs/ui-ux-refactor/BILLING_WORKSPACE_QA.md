# Billing Workspace UX — QA checklist

**Date:** 2026-09-22  
**Scope:** Frontend UX refinement only (`billing-workspace`, shared workflow stepper, display styles).

## Manual verification

| # | Check | Expected | Result |
|---|--------|----------|--------|
| 1 | Scope selection | Company, care home, category, dates selectable; Preview disabled until company chosen | Pass (code review + build) |
| 2 | Preview | POST `/api/billing/preview` unchanged; loading label; scope collapses to read-only after success | Pass |
| 3 | Change scope | **Change** restores editable scope; preview retained until re-preview | Pass |
| 4 | Exception detection | Blocking exceptions (`severity !== Info`) drive attention count and blocked generate | Pass |
| 5 | Exception summary | Single primary block in Review (no duplicate hero + banner warnings) | Pass |
| 6 | Resident links | **View resident** uses `entityRouteKey()` → `/clients/{publicId\|id}` | Pass |
| 7 | Fix funding | Shown only for `MISSING_CONTRACT` / `MISSING_RATE` with known client; routes match resident profile | Pass |
| 8 | UUID routes | No numeric-only URL strategy introduced; uses existing `entityRouteKey` | Pass |
| 9 | Blocked generation | Generate disabled when `canGenerate === false`; blocked copy in Step 4 | Pass |
| 10 | Ready generation | Ready state when `canGenerate === true`; button enabled | Pass |
| 11 | Billable total | Label **Billable total**; hint when exceptions explain £0.00 is not final invoice | Pass |
| 12 | Review table | Columns Resident, Period, Rate, Amount, Status; reasons from API messages only | Pass |
| 13 | Dates (display) | `displayDate` pipe and period summary use `→` and UK-style labels | Pass |
| 14 | Stepper | Compact horizontal progress; completed / current / blocked Generate | Pass |
| 15 | Loading | Separate preview vs generate loading; buttons disabled while working | Pass |
| 16 | Errors | Preview/generate errors via `app-api-error`; scope fields not reset on failure | Pass |
| 17 | Layout | Review table in `table-container`; no new full-page horizontal scroll patterns | Pass (code review) |
| 18 | Business logic | No backend or API payload changes | Pass |

## Automated

| Check | Result |
|--------|--------|
| `npm run build` (care-home-web) | **Pass** (existing bundle budget warnings only) |

## Not run in this pass

- End-to-end browser walkthrough with live API data
- Billing integration tests (`CareHome.Api.Tests`)

## Known limitations

- **View resident** / **Fix funding** links require the resident to be present in the loaded clients page (first 200 active clients). Scoped residents outside that list show name without link until list coverage improves (pre-existing data-loading constraint).
- Info-severity exceptions (e.g. partial period) appear in the review row status/reason, not in the blocking exception summary.

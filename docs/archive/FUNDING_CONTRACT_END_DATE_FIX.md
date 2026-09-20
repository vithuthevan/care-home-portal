# Funding Contract End Date Fix

## Root Cause

The client profile **Save contract** action posted the full `newContract` object unchanged. When the End Date input is left blank, Angular binds `contractEndDate` to an empty string (`""`). ASP.NET Core’s JSON binder cannot deserialize `""` into `DateOnly?`, so the API returns HTTP 400 before controller validation or persistence runs.

## Fix

In `saveContract()`, the POST body now mirrors `addRate()` for optional end dates: spread `newContract` and set `contractEndDate: this.newContract.contractEndDate || null` so a blank End Date is sent as JSON `null` instead of `""`. When a date is entered, behaviour is unchanged.

## Backend Changes

None.

## Database Changes

None.

## Verification

- [ ] Blank End Date contract saved — **Pending:** requires TenantAdmin login (`demo-admin@example.com`) in your environment; fix deployed to source and copied into running `carehome-web` container for hot reload.
- [ ] Alex contract created — **Pending:** same (Alex Morgan + Anytown Council / General Care / nominal 4000 / start 2026-04-01 / blank end).
- [ ] Contract is open-ended — **Pending:** list should show **Open** for End after save.
- [ ] £575/week rate created — **Pending:** existing `addRate()` already sends `effectiveTo: null` when blank.
- [ ] Blank Effective To accepted — **Expected yes** (unchanged rate path).
- [ ] August billing can recognise the contract/rate — **Pending:** billing preview for August 2026 after contract + rate exist (no invoice generation).

**Code-level request shape (blank End):** payload includes `"contractEndDate": null`, not `"contractEndDate": ""`.

## Files Changed

- `frontend/care-home-web/src/app/features/clients/pages/client-profile/client-profile.ts`

# Authorization

## Roles

| Role | TenantId | Access |
|---|---|---|
| PlatformAdmin | null | `/api/platform/tenants` only. Operational APIs return 403. |
| TenantAdmin | required | Full tenant operations and users. Cannot create PlatformAdmin or other tenants. |
| Administrator | required | Same as today within the tenant (compatibility). |
| LocationManager | required | Assigned care homes **in this tenant**. |
| ReadOnly | required | Tenant-scoped read. Mutating HTTP methods return 403. |

`SuperAdmin` is a legacy alias mapped to PlatformAdmin on login and in `AddMultiTenancy`.

Hidden Angular buttons are **not** security. API tenant filters, `[RequireTenant]`, care-home checks, and `ReadOnlyGuardFilter` enforce rules.

## JWT

Claims: `sub`, roles, `tenant_id` (omitted if null), `tenant_public_id`, `tenant_name`, and `security_stamp` (ASP.NET Identity stamp). Angular may display the organisation name from `/api/auth/me`. Normal APIs must not accept `tenantId` from the client.

On each authenticated request, JWT Bearer `OnTokenValidated` reloads the user and rejects the token when the stamp no longer matches (password change / stamp reset / **role or deactivate updates**) or the user is inactive. Tokens issued before this claim existed must sign in again. Tenant admins creating or resetting users set `MustChangePassword = true` so the recipient must change the temporary password before other APIs.

Configured in `appsettings.json` (`Jwt:Issuer`, `Jwt:Audience`). `Jwt:Key` is empty in the shared file. Development may set a placeholder in `appsettings.Development.json`. Production **must** set `Jwt__Key` (environment, user secrets, or secret store). Starting Production with a missing key, a weak key, or the development placeholder fails fast.

Token lifetime defaults to **8 hours** (`Jwt:ExpiryHours`). Clock skew defaults to 2 minutes. Lifetime is validated.

## Location lookups

Lists omit homes the LocationManager cannot access. Resource-by-id lookups for a same-tenant home, client, invoice, credit note, or care-home dashboard that is outside the assignment return **404**, not 403, so the API does not confirm that the object exists.

Hidden Angular buttons are **not** security. API tenant filters, `[RequireTenant]`, care-home checks, and `ReadOnlyGuardFilter` enforce rules. Read-only users receive **403** on all write HTTP methods.

## Inactive organisation

Deactivating a tenant (`IsActive = false`) does not delete data. Tenant users cannot log in (generic 401). Existing JWTs receive 403 on APIs. PlatformAdmin can still list/update tenants and reactivate.

## Development platform admin

`appsettings.Development.json`:

- Email: `admin@localhost`
- Password: `DevAdmin!12345`

Seeded only when `Seed:AdminEmail` and `Seed:AdminPassword` are set. Production `appsettings.json` leaves them empty. This user is PlatformAdmin with no organisation.

## Location Managers

`UserAccessService.GetAllowedCareHomeIdsAsync()` returns `null` for tenant-wide roles (all homes **in the tenant**) or the assigned home IDs. Every list still applies `TenantId`. Dashboard, clients, invoices, billing, reports, **credit-note preview/generate**, **funding contracts and rates**, **Sage export**, and **misc charge import** honour that list. Out-of-scope homes return **404** (or empty results), not 403.

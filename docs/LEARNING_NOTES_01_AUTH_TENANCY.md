# Learning notes 01 — Auth and multi-tenancy

Study of: `AuthController`, `HttpTenantContext`, `RequireTenantAttribute`, `UserAccessService`, `ReadOnlyGuardFilter`, Angular `AuthService` / guards / interceptor.

## WHAT

The system authenticates users with ASP.NET Identity passwords and issues a JWT. Every operational API call then derives the organisation (`TenantId`) from that token—not from the request body. Authorization is role-based, with an extra care-home scope for Location Managers.

## WHY

Care-home billing data is confidential and multi-organisation. If the client could send `tenantId=2` while logged into org 1, that would be a classic IDOR / tenant-escape bug. Putting tenancy in the signed JWT means the server decides the scope.

## HOW (login → request)

1. Browser calls `GET /api/auth/login-key` and encrypts the password with RSA-OAEP (`LoginPasswordCipher`).
2. `POST /api/auth/login` (rate-limited 10/min/IP):
   - Resolve password (cipher or legacy plaintext for tests).
   - Reject unknown/inactive user, locked account, bad password, inactive tenant — all with the **same** generic 401.
   - Build JWT claims: `sub`, roles, optional `tenant_id` / `tenant_public_id` / `tenant_name`, optional `must_change_password`.
3. Angular stores `{ token, roles, … }` in `localStorage` (`carehome.auth`).
4. `authInterceptor` adds `Authorization: Bearer …` on every HTTP call.
5. Pipeline: JWT validation → `[Authorize]` → `InactiveTenantMiddleware` → `MustChangePasswordMiddleware` → controller.
6. Operational controllers use `[RequireTenant]`; services read `ITenantContext.TenantId` and filter with `ForTenant` / `Id + TenantId`.

### `HttpTenantContext`

Reads claims from `HttpContext.User`. `TenantId` throws if missing—callers must check `HasTenant` or use `[RequireTenant]` first.

### `RequireTenantAttribute`

Returns **403** when there is no tenant claim (PlatformAdmin on operational APIs). This is deliberate: PlatformAdmin must not see unfiltered org data.

### `UserAccessService`

- Tenant-wide roles (`TenantAdmin`, `Administrator`, `ReadOnly`): `GetAllowedCareHomeIdsAsync()` returns `null` (= all homes in tenant).
- LocationManager: returns assigned IDs from `UserCareHomeAccess`.
- Resource lookups outside assignment return **404**, not 403, so the API does not confirm existence.

### `ReadOnlyGuardFilter`

Blocks non-GET/HEAD/OPTIONS for pure ReadOnly users (except change-password). UI hiding is not security.

## ALTERNATIVES

| Approach | Trade-off |
|---|---|
| Cookie sessions + antiforgery | Better XSS stance; harder for SPA/mobile; needs CSRF |
| External IdP (Entra/Auth0) | Better enterprise SSO; more ops complexity |
| EF global tenant query filters | Safer by default; painful for platform admin and design-time migrations |
| Pass `tenantId` in body | Convenient; **unsafe** |

## TRADE-OFF MADE

Explicit `TenantId` filters (no global query filters) + JWT in `localStorage`. Simpler migrations and platform routes; every query must remember the tenant. XSS can steal tokens—CSP and short lifetime mitigate, revocation is weak.

## WHAT CAN FAIL

- Missed `TenantId` filter → cross-tenant data leak.
- Password change updates `SecurityStamp` but JWT is not re-validated against stamp → old token works until expiry.
- LocationManager with empty assignments sees empty lists (correct) but buggy code that skips scope checks could over-expose.

## SENIOR ARCHITECT VIEW

Treat tenant isolation as a **security invariant**, not a filter convenience. Prefer tests that attempt cross-tenant GETs by ID. Prefer stamp/version checks on JWT if tokens live for hours.

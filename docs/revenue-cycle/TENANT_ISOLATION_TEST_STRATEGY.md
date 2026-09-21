# Tenant isolation testing strategy

**Phase 1.5** — systematic assurance that organisation boundaries hold under real HTTP and database paths.

## Principles

1. **Never rely on Angular guards** for security proofs; integration tests hit the API with JWTs.
2. **Prefer 404 over 403** for cross-tenant or out-of-scope resource IDs (existing API convention).
3. **Do not add EF global query filters** without a dedicated platform/admin bypass design; current explicit `TenantId` filters remain the source of truth.
4. **Every new financial workflow** (payments, remittance, disputes) must add at least one cross-tenant negative test before release.

## Layers

| Layer | What it proves | Location |
|-------|----------------|----------|
| Unit | `ForTenant()` and `ITenantOwned` coverage | `TenantIsolationTests` |
| Integration | HTTP + SQL Server + Identity + JWT | `CareHome.Api.Tests/Integration/*` |
| Manual / UAT | End-to-end scripts | `scripts/uat-*.ps1`, `docs/UAT_CHECKLIST.md` |

## Mandatory integration scenarios (maintain and extend)

| Scenario | Expected |
|----------|----------|
| Tenant A token → Tenant B invoice by id | **404** |
| LocationManager → unassigned care home client | **404** |
| ReadOnly → `POST /api/billing/generate` | **403** |
| PlatformAdmin → `GET /api/clients` | **403** (no tenant context) |
| Tenant user → `GET /api/platform/tenants` | **403** |
| Billing generate twice same period | Second blocked (`ALREADY_FULLY_BILLED` or zero lines) |
| Invoice PDF other tenant | **404** |

## EF global filters (future)

If global filters are introduced:

- Scope only to `ITenantOwned` entities.
- Provide `IgnoreQueryFilters()` only in audited platform jobs with explicit `tenantId` parameter.
- Add regression tests for PlatformAdmin provisioning and break-glass support flows.

## CI

Integration tests run against SQL Server in GitHub Actions (`CAREHOME_TEST_SQL` connection string). Local runs use the same variable or LocalDB fallback; tests skip when SQL is unreachable.

## Checklist for new features

- [ ] All queries filter by `tenantId` from `ITenantContext`, not request body.
- [ ] Care-home scoped roles use `UserAccessService`.
- [ ] Integration test added for cross-tenant access.
- [ ] Audit log written for mutating financial operations.

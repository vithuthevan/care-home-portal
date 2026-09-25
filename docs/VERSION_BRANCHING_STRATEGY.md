# Version branching strategy

This repository maintains two long-lived product lines from a single GitHub repository. They must be deployed and operated as separate environments.

## Branches

### `main` — Basic Care Home platform (V1)

- Scope: core care home operations (clients, billing, invoicing, funding contracts, Sage export, multi-tenancy) without the commercial Revenue Cycle expansion.
- Production: **Website 1** — existing/basic Care Home application.
- Change policy: essential maintenance, security fixes, and small V1-specific improvements only.
- Do **not** merge `revenue-cycle-v2` wholesale into `main`.

### `revenue-cycle-v2` — Revenue Cycle platform (V2)

- Scope: modular backend (`CareHome.Data`, `CareHome.Billing`, `CareHome.Funding`, `CareHome.Receivables`, `CareHome.Payments`, banking/reconciliation, remittance, revenue assurance, disputes, collections, funding renewals, finance attention), extended EF migrations, Angular revenue features, tests, and related documentation.
- Production: **Website 2** — Care Provider Revenue Cycle / commercial pilot.
- Change policy: all new financial-operations and revenue-cycle work lands here first.

## Bug fixes across versions

When a defect exists in shared code paths:

1. Fix on the branch where the bug was reported (or on the older branch if the fix is clearly V1-only).
2. Cherry-pick to the other branch when the same code exists and the fix still applies.
3. Avoid large merges between branches; prefer targeted cherry-picks and small shared patches.

## Future

If V2 eventually becomes the primary product, that should be an explicit product and release decision (default branch, deployment cutover, customer communication). This document does not change the default branch.

## Deployment mapping

| Site | Git branch | API / web deployment | Purpose |
|------|------------|----------------------|---------|
| Website 1 | `main` | Basic web + API | Basic Care Home application |
| Website 2 | `revenue-cycle-v2` | V2 web + API | Revenue Cycle platform |

No second GitHub repository is required; isolation is by **branch + environment configuration**, not by repo split.

## Environment isolation (required)

The two production sites **must not share one SQL database** or document storage bucket.

| Concern | V1 (`main`) | V2 (`revenue-cycle-v2`) |
|---------|-------------|---------------------------|
| SQL database | Dedicated Basic production database | Dedicated V2 production database |
| Document storage | Dedicated V1 storage root / account | Dedicated V2 storage root / account |
| Connection strings | V1-only | V2-only |
| JWT signing keys | V1-only | V2-only |
| SMTP / email | V1-only | V2-only |
| Frontend URL / API URL | V1 URLs | V2 URLs |
| CORS allowed origins | V1 frontends | V2 frontends |
| Monitoring / alerts | V1 workspace | V2 workspace |
| Backups | V1 backup policy & retention | V2 backup policy & retention |

Configure secrets and connection strings in each host’s environment or secret store — never commit production secrets to Git.

## Database migration warning

V2 introduces additional Entity Framework migrations (under `backend/CareHome.Data/Migrations/`).

- **Never** point the V2 application at the Basic (V1) production database for testing or piloting. EF could apply Revenue Cycle migrations to the V1 schema and break the basic site.
- **Never** point the V1 application at a database that has only been migrated with V2 migrations unless you have explicitly planned a coordinated schema strategy (not the default).

Treat V1 and V2 database schemas as **separate product environments**, each migrated only by the application version that owns that branch.

## Tags

- `v1-basic` (on `main`): frozen reference to the Basic platform commit before the Revenue Cycle baseline on `revenue-cycle-v2`.

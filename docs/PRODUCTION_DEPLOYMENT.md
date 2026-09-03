# Production deployment

Usable by another engineer for a **controlled pilot**. This product is a modular monolith: one ASP.NET Core API and one Angular SPA. It supports both multi-tenant SaaS in one database and a dedicated single-customer deployment with one tenant. Do not introduce subscriptions, Stripe, or custom domains yet.

Do **not** deploy from this document automatically. Do **not** run `migration Down()` on a live financial database.

## Prerequisites

- Windows or Linux host that can run .NET 10
- SQL Server (not LocalDB) with a dedicated application login
- TLS terminator (IIS, nginx, App Gateway, or equivalent) presenting a valid certificate
- Disk for document storage (invoice PDFs, credit-note PDFs, Sage CSVs)
- SMTP mailbox **or** an explicit decision to run simulated email (logged as a Production warning)
- Secrets store for `Jwt__Key`, SQL password, SMTP password, first-admin password
- Ability to take a SQL backup **before** applying migrations

## SQL Server

1. Create an empty database, for example `CareHome`.
2. Create an application login with `db_datareader`, `db_datawriter`, `db_ddladmin` **or** apply migrations with a separate elevated account and give the runtime login data-only rights.
3. Set `ConnectionStrings__DefaultConnection` as in `docs/PRODUCTION_CONFIGURATION.md`.

## API hosting

Publish:

```powershell
cd backend\CareHome.Api
dotnet publish -c Release -o C:\apps\carehome-api
```

Run behind IIS (ASP.NET Core Module), systemd, or a container. Set `ASPNETCORE_ENVIRONMENT=Production`. Listen on HTTP locally and terminate HTTPS at the reverse proxy. Forwarded headers (`X-Forwarded-For`, `X-Forwarded-Proto`) are honoured.

Health:

- `GET /health/live` — process is up (anonymous, no diagnostics)
- `GET /health/ready` — SQL connectivity (anonymous, `{ "status": "Healthy"|"Unhealthy" }`)

## Angular hosting

```powershell
cd frontend\care-home-web
npm ci
npm run build
```

Serve `dist/care-home-web/browser` (or the generated browser folder) as static files.

Preferred: same origin as the API, reverse-proxy `/api` to the ASP.NET app. The production bundle uses relative `/api` paths and does **not** require the development proxy.

If the SPA is on another host, set `Cors__AllowedOrigins__0` to that HTTPS origin.

SPA host headers should include clickjacking protection and a CSP as documented in `docs/PRODUCTION_CONFIGURATION.md`.

## HTTPS / DNS

- Public URL should be HTTPS only.
- API `Https:Redirect` defaults to on in Production.
- Certificate expiry is an operational concern (`docs/OPERATIONS_CHECKLIST.md`).
- Do not hard-code a customer domain in source.

## Configuration / secrets

See `docs/PRODUCTION_CONFIGURATION.md`. Minimum Production secrets:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key` (≥32 mixed characters, not the development placeholder)
- SMTP password if `Email__Mode=Smtp`

## Initial PlatformAdmin bootstrap

Development `admin@localhost` / `DevAdmin!12345` is **never** created in Production.

On a fresh Production database, after migrations:

```text
Seed__AdminEmail=platform.admin@example.org
Seed__AdminPassword=<unique strong password>
```

Start the API once. Confirm login. Then **remove** those environment variables so a known bootstrap password does not remain on the host.

Alternatively, create the first PlatformAdmin through a one-off operational process (SQL + Identity hash is not documented here on purpose). Empty `Seed:AdminEmail` / `Seed:AdminPassword` means no admin is created.

Then create the real organisation with `POST /api/platform/tenants` (or the Organisations screen). New tenants receive generic invoice categories and sequences only — no demo residents, no customer trading names, no invoices.

## Database backup (before every release)

See `docs/BACKUP_RESTORE.md`. Take a full backup of the target database **and** copy the document storage root.

## Migration (do not auto-apply at startup)

The API **does not** run EF migrations on startup.

Release validation on a copy, then production:

```powershell
cd backend\CareHome.Api
dotnet ef migrations has-pending-model-changes
# Expected: No changes have been made to the model since the last migration.

# After backup:
dotnet ef database update --connection "<production-or-slot-connection>"
```

Confirm `__EFMigrationsHistory` contains the latest migration. Then start/restart the API.

## Application deployment sequence

1. Backup database
2. Backup document storage
3. Deploy the new API package and Angular static files (keep the previous package)
4. Apply reviewed EF migrations
5. Verify migration (`__EFMigrationsHistory`, smoke query)
6. Start/restart the API
7. Health check (`/health/live`, `/health/ready`)
8. Smoke test (`docs/PRODUCTION_SMOKE_TEST.md`) — use a dedicated test tenant; do not generate live funder invoices on every deploy

## Document storage

Set `DocumentStorage__RootPath` to a dedicated volume. Layout:

```text
{root}/tenants/{tenant-public-id}/invoices/
{root}/tenants/{tenant-public-id}/credit-notes/
{root}/tenants/{tenant-public-id}/sage-exports/
```

Filenames are sanitized. Paths containing `..` are rejected. Database backup **does not** include these files.

## SMTP

Set `Email__Mode=Smtp` plus host, from address, TLS, and credentials. Incomplete Smtp configuration fails fast in Production.

If the pilot must run without mail, leave Mode off `Smtp`. Logs will say **PRODUCTION EMAIL IS SIMULATED**. Invoice generate still succeeds; send records failure/simulation without changing invoice totals.

## Rollback

See the Rollback section below and `docs/PRODUCTION_DEPLOYMENT.md` (this file).

### Application rollback

Redeploy the previous API publish folder and the previous Angular `dist`. Restart. No database change if migrations were **not** applied.

### Database rollback

If a migration was applied, **restore the pre-deployment backup**. Do not run `dotnet ef database update <previous>` / `migration Down()` on a live financial database. Restore procedure: `docs/BACKUP_RESTORE.md`.

### Documents rollback

Restore the document storage copy taken with the database backup so PDF paths on invoices still resolve.

## Azure (recommended managed host)

Same-origin App Service + Azure SQL + Azure Files for documents. Automated provision/migrate/deploy:

```powershell
.\scripts\Deploy-Azure.ps1 -ResourceGroup rg-carehome -Location uksouth -AppName carehome-pilot
```

Details: `docs/AZURE_HOSTING.md`. Bicep: `infra/azure/main.bicep`. Publish package (SPA into API `wwwroot`): `scripts/Publish-CareHome.ps1`.

## Deployment models

**A. Multi-tenant SaaS** — one API, one database, many `Tenant` rows. Isolation is `TenantId` + JWT `tenant_id`. PlatformAdmin has no organisation context.

**B. Dedicated single-customer** — same binaries, one tenant provisioned, optionally a dedicated database. No code change is required.

## What is not in this release

Stripe, subscriptions, licensing, custom domains, tenant usage billing, per-tenant SMTP secrets beyond from-name/from-address.

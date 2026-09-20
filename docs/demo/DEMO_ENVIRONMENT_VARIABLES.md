# Demo Environment Variables

**Date:** 15 September 2026  
**Scope:** Local Docker Compose + `ASPNETCORE_ENVIRONMENT=Development` client demo only.  
**Not in scope:** Azure deployment, production configuration, or application code changes.

This document lists every configuration value traced through `docker-compose.yml`, `.env` files, `appsettings*.json`, `Program.cs`, configuration classes, and direct `IConfiguration` usage in the API. The Angular frontend does **not** read runtime environment variables; it proxies `/api` via `proxy.conf.docker.json` (Docker) or `proxy.conf.json` (local `npm start`).

---

## How configuration is loaded (Docker demo)

| Layer | Source | Notes |
|-------|--------|-------|
| SQL container | `docker-compose.yml` `environment:` | `ACCEPT_EULA`, `MSSQL_PID`, `MSSQL_SA_PASSWORD` |
| API container | `docker-compose.yml` `environment:` | Connection string, migrations, document path, public URL |
| API defaults | `appsettings.json` + `appsettings.Development.json` | Baked into the image at build time |
| Optional overrides | Repository-root `.env` | Docker Compose substitutes `${MSSQL_SA_PASSWORD}` only |
| Production validator | `ProductionStartupValidator` | **Skipped** when `ASPNETCORE_ENVIRONMENT=Development` |

Environment variable names use ASP.NET Core’s `__` convention (e.g. `Jwt:Key` → `Jwt__Key`).

---

## Variable reference

### A. Required for demo

Variables without which the demo **cannot** run when using the repository’s Docker Compose stack **with built-in defaults**.

| Variable | Required for Demo? | Example / Placeholder | Secret? | Where Used | Purpose |
|----------|-------------------|------------------------|---------|------------|---------|
| *(none)* | No manual supply needed | — | — | — | `docker-compose.yml` and `appsettings.Development.json` provide all required values for a standard local demo. |

**Operator note:** Copy `.env.demo.example` to `.env` only if you need to override the SQL SA password. The default `CareHomeDevSql2022@` is acceptable for an isolated local demo machine.

---

### B. Required but already defaulted

Configuration the application needs; **already set** for Docker Compose Development — do not supply manually unless overriding.

#### Docker Compose — SQL service (`carehome-sql`)

| Variable | Required for Demo? | Example / Placeholder | Secret? | Where Used | Purpose |
|----------|-------------------|------------------------|---------|------------|---------|
| `ACCEPT_EULA` | Yes (auto) | `Y` | No | `docker-compose.yml` → SQL Server image | Accept SQL Server license |
| `MSSQL_PID` | Yes (auto) | `Developer` | No | `docker-compose.yml` | SQL Server edition |
| `MSSQL_SA_PASSWORD` | Yes (auto) | `CareHomeDevSql2022@` or `<DEMO_DATABASE_PASSWORD>` | **Yes** (if customized) | `docker-compose.yml` SQL + API connection string; healthcheck | SQL Server `sa` password; must match in API connection string |

#### Docker Compose — API service (`carehome-api`)

| Variable | Required for Demo? | Example / Placeholder | Secret? | Where Used | Purpose |
|----------|-------------------|------------------------|---------|------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Yes (auto) | `Development` | No | `Program.cs`, `ProductionStartupValidator`, `ConfigurableEmailSender`, `IdentitySeeder`, `JwtSigningKey`, middleware | Enables Development profile, simulated email, dev JWT placeholder, skips production validation |
| `ASPNETCORE_URLS` | Yes (auto) | `http://+:8080` | No | `Dockerfile`, `docker-compose.yml` | API listen URL inside container |
| `ConnectionStrings__DefaultConnection` | Yes (auto) | `Server=sql,1433;Database=CareHomeDb;User Id=sa;Password=<DEMO_DATABASE_PASSWORD>;…` | **Yes** (contains password) | `Program.cs` → EF Core `CareHomeDbContext` | Database connectivity |
| `Database__ApplyMigrations` | Yes (auto) | `true` | No | `Program.cs` startup | Auto-applies EF Core migrations on empty volume |
| `DocumentStorage__RootPath` | Yes (auto) | `/app/App_Data/documents` | No | `LocalDocumentStore.cs` | Invoice/credit-note PDF and Sage export storage (Docker volume) |
| `App__PublicUrl` | Yes (auto) | `http://localhost:4200` | No | `TenantProvisioningService.cs` welcome email body | Sign-in link text when provisioning organisations |

#### `appsettings.Development.json` (loaded when `ASPNETCORE_ENVIRONMENT=Development`)

| Variable (config key) | Env override | Example / Placeholder | Secret? | Where Used | Purpose |
|-----------------------|--------------|------------------------|---------|------------|---------|
| `Jwt:Key` | `Jwt__Key` | `DEVELOPMENT-ONLY-CHANGE-ME-TO-A-LONG-SECRET-KEY` | **Yes** (in non-Dev) | `Program.cs`, `AuthController.cs`, `JwtSigningKey.cs` | JWT signing key |
| `Jwt:Issuer` | `Jwt__Issuer` | `CareHomeApi` | No | `Program.cs`, `AuthController.cs` | JWT issuer claim |
| `Jwt:Audience` | `Jwt__Audience` | `CareHomeWeb` | No | `Program.cs`, `AuthController.cs` | JWT audience claim |
| `Jwt:ExpiryHours` | `Jwt__ExpiryHours` | `8` | No | `AuthController.cs` | Token lifetime (1–12 hours) |
| `Jwt:ClockSkewMinutes` | `Jwt__ClockSkewMinutes` | `2` | No | `Program.cs` | Clock skew for token validation (0–5) |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, … | `http://localhost:4200`, `http://127.0.0.1:4200` | No | `Program.cs`, `ProductionStartupValidator.ResolveOrigins` | Allowed browser origins |
| `Https:Redirect` | `Https__Redirect` | `false` | No | `Program.cs` | Disable HTTPS redirect in Development |
| `Email:Mode` | `Email__Mode` | `Development` | No | `EmailOptions.cs`, `ConfigurableEmailSender.cs` | Simulated email (no SMTP) |
| `Email:FromAddress` | `Email__FromAddress` | `noreply@localhost` | No | `appsettings.json` default; SMTP only if `Mode=Smtp` | Sender address |
| `Email:FromName` | `Email__FromName` | `Care Home Billing` | No | `ConfigurableEmailSender.cs` | Sender display name |
| `Seed:AdminEmail` | `Seed__AdminEmail` | `admin@localhost` | No | `IdentitySeeder.cs` | PlatformAdmin bootstrap email |
| `Seed:AdminPassword` | `Seed__AdminPassword` | `<DEMO_PLATFORM_ADMIN_PASSWORD>` | **Yes** | `IdentitySeeder.cs` | PlatformAdmin bootstrap password |
| `App:PublicUrl` | `App__PublicUrl` | `http://localhost:4200` | No | `TenantProvisioningService.cs` | Overridden by Compose for Docker |

#### `appsettings.json` fallbacks (when not overridden)

| Variable (config key) | Env override | Example / Placeholder | Secret? | Where Used | Purpose |
|-----------------------|--------------|------------------------|---------|------------|---------|
| `DocumentStorage:RootPath` | `DocumentStorage__RootPath` | *(empty → `App_Data/documents`)* | No | `LocalDocumentStore.cs` | Document root fallback |
| `ForwardedHeaders:KnownProxies` | `ForwardedHeaders__KnownProxies__0` | `[]` | No | `Program.cs` | Trusted reverse proxies (non-Development) |
| `AllowedHosts` | — | `*` | No | ASP.NET Core host filtering | Host allow list |

#### Docker Compose — Web service (`carehome-web`)

| Variable | Required for Demo? | Example / Placeholder | Secret? | Where Used | Purpose |
|----------|-------------------|------------------------|---------|------------|---------|
| *(none)* | — | — | — | `proxy.conf.docker.json` | Proxies `/api` and `/health` to `http://api:8080` |

#### Host ports (not env vars)

| Endpoint | Value |
|----------|-------|
| Frontend | http://localhost:4200 |
| API | http://localhost:5092 |
| SQL (external tools) | `localhost,14333` / database `CareHomeDb` / user `sa` |

---

### C. Optional

Features or overrides that **do not** need to be configured for the standard demo.

| Variable | Required for Demo? | Example / Placeholder | Secret? | Where Used | Purpose |
|----------|-------------------|------------------------|---------|------------|---------|
| `Email:Smtp:Host` | No | `<SMTP_HOST>` | No | `ConfigurableEmailSender.cs` | Real SMTP (not used in Development demo) |
| `Email:Smtp:Port` | No | `587` | No | `EmailOptions` / SMTP client | SMTP port |
| `Email:Smtp:User` | No | `<SMTP_USER>` | No | `ConfigurableEmailSender.cs` | SMTP authentication |
| `Email:Smtp:Password` | No | `<SMTP_PASSWORD>` | **Yes** | `ConfigurableEmailSender.cs` | SMTP password |
| `Email:Smtp:EnableSsl` | No | `true` | No | `ConfigurableEmailSender.cs` | SMTP TLS |
| `Seed__AdminEmail` / `Seed__AdminPassword` override | No | Custom platform admin | **Yes** (password) | `IdentitySeeder.cs` | Replace default `admin@localhost` if desired |
| `Jwt__Key` override | No | `<JWT_SECRET>` (≥32 chars) | **Yes** | `JwtSigningKey.cs` | Replace dev placeholder |
| `Jwt__ExpiryHours` override | No | `8` | No | `AuthController.cs` | Adjust session length |
| `Cors__AllowedOrigins__*` override | No | Custom origin | No | `Program.cs` | Only if not using default localhost ports |
| `Logging:LogLevel:*` | No | `Information` | No | ASP.NET Core logging | Verbose diagnostics |

**TenantAdmin credentials** are created in the UI when provisioning an organisation — they are **not** environment variables.

---

### D. Production only

Do **not** set these for the local client demo.

| Variable | Example / Placeholder | Where Used | Why not for demo |
|----------|------------------------|------------|------------------|
| `ASPNETCORE_ENVIRONMENT=Production` | — | Entire host | Triggers `ProductionStartupValidator`; blocks dev credentials and placeholder JWT |
| `ConnectionStrings__DefaultConnection` (Azure SQL) | Azure SQL connection string | `ProductionStartupValidator`, EF Core | Demo uses local Docker SQL |
| `Jwt__Key` (production secret) | `<JWT_SECRET>` | `JwtSigningKey.cs` | Dev placeholder is allowed only in Development |
| `Email__Mode=Smtp` | `Smtp` | `ProductionStartupValidator`, `ConfigurableEmailSender` | Demo uses simulated email |
| `Email__AllowSimulationInProduction` | `true` | `ProductionStartupValidator` | Weakens production controls |
| `Email__Smtp__*` | SMTP settings | `ConfigurableEmailSender` | Real mail delivery — not for demo |
| `Cors__AllowedOrigins__*` (production HTTPS) | `https://app.example.com` | `ProductionStartupValidator` | Production CORS rules |
| `Https__Redirect=true` | `true` | `Program.cs` | Default in Production |
| `ForwardedHeaders__KnownProxies__*` | Proxy IPs | `Program.cs` | Azure/reverse-proxy hosting |
| `Seed__AdminEmail` / `Seed__AdminPassword` (production bootstrap) | Unique values | `IdentitySeeder`, `ProductionStartupValidator` | Dev seed blocked outside Development |
| `Database__ApplyMigrations=true` on shared/production DB | — | `Program.cs` | Production migration discipline |
| Azure / deploy script settings | See `infra/azure/`, `scripts/Deploy-Azure.ps1` | Azure App Service | Post-demo deployment only |

---

### E. Secrets

Values that must **never** be committed to Git. Use placeholders in documentation and `.env.demo.example`.

| Secret | Config / env name | Demo handling |
|--------|-------------------|---------------|
| SQL SA password | `MSSQL_SA_PASSWORD` | Default in `.env.example` is local-only; override via `.env` if needed |
| Platform admin password | `Seed__AdminPassword` / `Seed:AdminPassword` | Default `DevAdmin!12345` in Development config — **do not show on screen** |
| JWT signing key | `Jwt__Key` | Development placeholder in repo is acceptable locally only |
| SMTP password | `Email__Smtp__Password` | Not used in demo |
| Tenant admin password | *(UI only)* | Temporary password from organisation create screen; change before demo |
| Production bootstrap admin | `Seed__AdminPassword` | Not used in demo |

**Never commit:** `.env` with real passwords, user secrets, Key Vault exports, or production connection strings.

---

## `.env` files in this repository

| File | Purpose |
|------|---------|
| `.env.example` | Minimal local Docker override (`MSSQL_SA_PASSWORD` only) |
| `.env.demo.example` | Demo-oriented template with placeholders and comments (copy to `.env` if overriding) |
| `.env` | Local overrides (gitignored) — create from example files |

Docker Compose automatically loads `.env` from the repository root. Only `MSSQL_SA_PASSWORD` is substituted in `docker-compose.yml` today; other API settings come from the image’s Development appsettings and Compose `environment:` block.

---

## Quick answer: what must I provide manually?

For the **standard Docker Compose demo with repository defaults:**

| Provide manually? | Variable |
|-------------------|----------|
| **No** | All required configuration is pre-set |

Optional:

| Provide manually? | Variable | When |
|-------------------|----------|------|
| Optional | `MSSQL_SA_PASSWORD` | Only if the default SA password is unacceptable on your machine |

All demo **business** setup (organisation, TenantAdmin, residents, invoices) is done through the UI after startup — not via environment variables.

---

## Related documents

| Document | Use |
|----------|-----|
| `CLIENT_DEMO_SCRIPT.md` | Startup procedure and demo narrative |
| `DEMO_PRE_FLIGHT_CHECKLIST.md` | Pre-demo and recovery checklists |
| `CLIENT_DEMO_ENVIRONMENT_SETUP.md` | Detailed operator runbook (data setup, troubleshooting) |
| `.env.demo.example` | Placeholder env file for local demo |

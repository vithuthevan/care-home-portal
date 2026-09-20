# Production configuration

Do **not** commit real secrets. Values below are placeholders.

Development may use `appsettings.Development.json`. Production must supply secrets via environment variables, the host secret store, or a secret manager. Double-underscore environment names map to nested JSON (`Jwt__Key` → `Jwt:Key`).

Array indexes use `__0`, `__1`, …

## Settings

| Name | Required? | Secret? | Example placeholder | Description |
|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Yes | No | `Production` | `Development` allows LocalDB, the JWT placeholder, and the demo admin seed. |
| `ASPNETCORE_URLS` | Hosting-specific | No | `http://127.0.0.1:8080` | Listen URLs behind the TLS terminator. |
| `ConnectionStrings__DefaultConnection` | **Yes in Production** | **Yes** | `Server=sql.example.internal;Database=CareHome;User Id=app;Password=<secret>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True` | SQL Server. LocalDB / `(localdb)\MSSQLLocalDB` is **rejected** outside Development. |
| `Jwt__Key` | **Yes in Production** | **Yes** | (do not commit; ≥32 mixed characters) | HMAC-SHA256 signing key. Missing, weak, or the development placeholder fails fast outside Development. |
| `Jwt__Issuer` | No (default `CareHomeApi`) | No | `CareHomeApi` | Token issuer. |
| `Jwt__Audience` | No (default `CareHomeWeb`) | No | `CareHomeWeb` | Token audience. |
| `Jwt__ExpiryHours` | No (default `8`, max `12`) | No | `8` | Access-token lifetime in hours. |
| `Jwt__ClockSkewMinutes` | No (default `2`, max `5`) | No | `2` | JWT lifetime clock skew. |
| `Cors__AllowedOrigins__0` | Same-origin: no. Split SPA: **yes** | No | `https://app.example.com` | Trusted Angular origins. Do not use `*`. Do not include localhost in Production. Additional origins: `__1`, `__2`. |
| `Https__Redirect` | No (default `true` outside Development) | No | `true` | Enables HTTPS redirection and HSTS. Set `false` only for a local Production-config experiment on HTTP. |
| `Email__Mode` | **Yes in Production** | No | `Smtp` | `Smtp` sends mail via SMTP. Any other value simulates in Development only. Production **refuses to start** unless `Email__Mode=Smtp` or `Email__AllowSimulationInProduction=true`. |
| `Email__AllowSimulationInProduction` | Path B interim only | No | `false` | When `true`, API starts without SMTP but sends **fail visibly** (invoice not marked Sent). Requires business sign-off. See [PRODUCTION_EMAIL_SETUP.md](operations/PRODUCTION_EMAIL_SETUP.md). |
| `Email__FromAddress` | **Yes if Mode=Smtp** | No | `billing@example.org` | Envelope/from address. |
| `Email__FromName` | No | No | `Care Home Billing` | From display name. |
| `Email__Smtp__Host` | **Yes if Mode=Smtp** | No | `smtp.example.org` | SMTP host. Missing host in Smtp mode fails fast in Production. |
| `Email__Smtp__Port` | No (default `587`) | No | `587` | SMTP port. |
| `Email__Smtp__User` | If the server requires auth | **Yes** | `smtp-user` | SMTP username. |
| `Email__Smtp__Password` | If the server requires auth | **Yes** | (secret store) | SMTP password. Never commit. |
| `Email__Smtp__EnableSsl` | No (default `true`) | No | `true` | SMTP TLS. |
| `DocumentStorage__RootPath` | Recommended | No | `D:\carehome-documents` | Root for PDFs and Sage CSVs. Empty → `{contentRoot}/App_Data/documents`. |
| `Seed__AdminEmail` | First bootstrap only | No | `platform.admin@example.org` | Creates a PlatformAdmin **only if both email and password are set**. `admin@localhost` is rejected outside Development. |
| `Seed__AdminPassword` | First bootstrap only | **Yes** | (secret store, ≥12 chars, mixed) | Must not be `DevAdmin!12345`. Leave empty after the first admin exists. |
| `Logging__LogLevel__Default` | No | No | `Information` | Default log level. |

## How to supply `Jwt__Key`

```text
# Environment (IIS / systemd / container)
Jwt__Key=<at least 32 mixed characters>

# User secrets (developer machines only)
dotnet user-secrets set "Jwt:Key" "<value>" --project backend/CareHome.Api
```

A deployment secret manager (Azure Key Vault, Windows DPAPI, Kubernetes Secret, etc.) is preferred. The repository must never contain a production key.

### Azure Key Vault (production deploy)

Azure deployments store secrets in Key Vault and reference them from App Service app settings:

```text
ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/ConnectionStrings-DefaultConnection/)
Jwt__Key=@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/Jwt-Key/)
```

Key Vault secret names use hyphens (not `__`). See [PRODUCTION_SECRETS_SETUP.md](operations/PRODUCTION_SECRETS_SETUP.md) for the full inventory, deploy flow, and rotation steps.

## CORS

- Same-origin (Angular reverse-proxied to `/api` on the API host): leave `Cors:AllowedOrigins` empty.
- Split hosts: set one or more HTTPS origins. Example: `Cors__AllowedOrigins__0=https://app.example.com`.
- Production refuses `*` and localhost origins.

## Identity (not environment variables)

Configured in code for Production-safe defaults:

| Setting | Value |
|---|---|
| Password minimum length | 12 |
| Digit / upper / lower / non-alphanumeric | required |
| Lockout | 5 failed attempts, 15-minute lock |
| Login rate limit | 10 requests / minute / IP |
| Unique email | required globally |

## SQL Server connection-string format

```text
Server=<host>,1433;Database=CareHome;User Id=<app-login>;Password=<secret>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True
```

Windows authentication (if used):

```text
Server=<host>;Database=CareHome;Trusted_Connection=True;Encrypt=True;MultipleActiveResultSets=True
```

Do not use LocalDB in Production.

## Time zone

Tenant settings default to `Europe/London`. Business dates are `DateOnly`. Event timestamps are `DateTimeOffset`. There is no process-wide `TZ` override required.

## API security headers

The API sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy`, and an **API-only** CSP:

```text
default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'
```

That CSP is for JSON/file API responses. It is **not** intended for the Angular host. The static-file host should set its own CSP, typically:

```text
default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'
```

`style-src 'unsafe-inline'` is required for this Angular build (component styles). Do not add `unsafe-eval` unless a future change proves it necessary.

## Correlation ID

Incoming `X-Correlation-ID` is used if it is ≤64 characters of `[A-Za-z0-9_-]`. Otherwise a new id is created. The same value is returned on the response and included in logs and 500 bodies.

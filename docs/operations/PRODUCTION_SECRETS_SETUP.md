# Production secrets setup

**Purpose:** Configure production secrets outside source control, with Azure Key Vault for Azure deployments and environment variables for other hosts.

**Related docs:** [docs/PRODUCTION_CONFIGURATION.md](docs/PRODUCTION_CONFIGURATION.md), [docs/AZURE_HOSTING.md](docs/AZURE_HOSTING.md), [P0_EXECUTION_ROADMAP.md](P0_EXECUTION_ROADMAP.md)

---

## Principles

1. **No production secrets in git** — connection strings, JWT keys, SMTP passwords, and bootstrap passwords never belong in `appsettings.json`, Bicep parameters committed to the repo, or deployment scripts checked into version control.
2. **Development vs Production separation** — local dev uses `appsettings.Development.json`, user secrets, or Docker `.env`. Production uses host environment variables or Key Vault references.
3. **Fail fast** — the API refuses to start in Production with LocalDB, a missing/weak JWT key, dev bootstrap credentials, missing `Email__Mode=Smtp` (unless `Email__AllowSimulationInProduction=true`), or incomplete SMTP when `Email__Mode=Smtp`.

---

## Required production secrets

| App setting (env var) | Key Vault secret name | Secret? | Purpose | When required |
|---|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings-DefaultConnection` | **Yes** | SQL Server runtime connection | Always in Production |
| `Jwt__Key` | `Jwt-Key` | **Yes** | JWT HMAC signing (≥32 mixed chars) | Always in Production |
| `Email__Smtp__Password` | `Email-Smtp-Password` | **Yes** | SMTP authentication | When `Email__Mode=Smtp` and server requires auth |
| `Seed__AdminPassword` | `Seed-AdminPassword` | **Yes** | One-time PlatformAdmin bootstrap | First deploy only; remove after login |

### Non-secret production settings (not in Key Vault)

| App setting | Example | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Required |
| `DocumentStorage__RootPath` | `/home/carehome-documents` | Azure Files mount on App Service |
| `Email__Mode` | `Smtp` | **Required in Production** for live email (see [PRODUCTION_EMAIL_SETUP.md](PRODUCTION_EMAIL_SETUP.md)) |
| `Email__AllowSimulationInProduction` | `true` | Interim manual-send workflow only; sends still fail visibly |
| `Email__Smtp__Host`, `Email__FromAddress`, etc. | Provider-specific | Required when `Email__Mode=Smtp` |
| `Cors__AllowedOrigins__0` | `https://app.example.com` | Leave empty for same-origin Azure deploy |
| `Https__Redirect` | `true` | Default in Production |

---

## Where secrets are stored

### Azure (recommended)

| Component | Role |
|---|---|
| **Azure Key Vault** | Authoritative store for SQL connection string, JWT key, SMTP password, bootstrap password |
| **App Service app settings** | Key Vault **references** only (not plain secret values) |
| **App Service managed identity** | Reads secrets from Key Vault at runtime (`Key Vault Secrets User` RBAC) |
| **Deploy operator** | `Key Vault Secrets Officer` during deploy (granted via Bicep `deployerObjectId`) |

Infrastructure is defined in [infra/azure/main.bicep](infra/azure/main.bicep):

- Key Vault with RBAC authorization and soft delete
- Linux App Service with system-assigned managed identity
- `keyVaultReferenceIdentity: SystemAssigned` for App Service Key Vault references

### Non-Azure hosts (IIS, systemd, Kubernetes, on-prem VM)

Store secrets in the host's secret mechanism and map to environment variables:

- Windows: environment variables, Windows Credential Manager, or DPAPI-protected config
- Linux: systemd `EnvironmentFile` with `0600` permissions, or Kubernetes Secrets
- Containers: orchestrator secrets mounted as env vars

The application reads configuration via standard ASP.NET Core env var binding (`Jwt__Key` → `Jwt:Key`). No Key Vault SDK is required on non-Azure hosts.

### Local development (not production)

| Mechanism | Location |
|---|---|
| `appsettings.Development.json` | `backend/CareHome.Api/appsettings.Development.json` — dev JWT placeholder and demo admin only |
| User secrets | `dotnet user-secrets set "Jwt:Key" "..." --project backend/CareHome.Api` |
| Docker Compose | Copy `.env.example` to `.env` (gitignored) for `MSSQL_SA_PASSWORD` |

---

## How deployment retrieves secrets

### Automated Azure deploy (`scripts/Deploy-Azure.ps1`)

1. **Provision** — Bicep creates Key Vault, App Service (managed identity), SQL, storage.
2. **Generate** — SQL admin password and JWT key are generated (or passed as `-SqlAdminPassword` / `-JwtKey`).
3. **Write to Key Vault** — `az keyvault secret set` stores:
   - `ConnectionStrings-DefaultConnection`
   - `Jwt-Key`
   - `Seed-AdminPassword` (bootstrap only)
4. **Configure App Service** — app settings use Key Vault references:
   ```text
   @Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/Jwt-Key/)
   ```
5. **Migrate** — EF migrations use the connection string **locally in the deploy script** (never written as plain text to App Service).
6. **Bootstrap** — on first start, `Seed__AdminEmail` (plain) + `Seed__AdminPassword` (Key Vault ref) create PlatformAdmin.
7. **Cleanup** — deploy script removes `Seed__*` app settings and deletes `Seed-AdminPassword` from Key Vault.

A gitignored inventory file is written to `secrets-inventory/<app>-<date>.md` listing secret names and rotation guidance. Copy it to your password manager and delete the local file.

### Legacy plain app settings (not recommended)

Pass `-UsePlainAppSettings` to `Deploy-Azure.ps1` only for emergency debugging. Production pilots should use Key Vault references.

### Manual Key Vault update (rotation)

```powershell
# Update secret value (creates new version)
az keyvault secret set --vault-name <vault> --name Jwt-Key --value "<new-key>"

# Restart App Service to pick up latest version
az webapp restart --resource-group <rg> --name <app>
```

App Service Key Vault references resolve the **latest** secret version on restart.

---

## Developer setup

### Run API locally (Development)

```powershell
cd backend\CareHome.Api
dotnet run
```

Uses `appsettings.Development.json` automatically when `ASPNETCORE_ENVIRONMENT=Development` (default in `launchSettings.json`).

Optional: override JWT via user secrets:

```powershell
dotnet user-secrets set "Jwt:Key" "your-local-dev-key-at-least-32-chars" --project backend/CareHome.Api
```

### Run via Docker Compose

```powershell
copy .env.example .env
# Edit .env if needed — never commit .env
docker compose up --build
```

Docker uses `ASPNETCORE_ENVIRONMENT=Development` and dev SQL credentials from `.env`.

### Test Production configuration locally (optional)

Set environment variables without committing values:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=...;..."
$env:Jwt__Key = "<at-least-32-mixed-characters>"
dotnet run --project backend\CareHome.Api
```

Expect startup failure if JWT is missing, weak, or the development placeholder.

---

## Secrets inventory (operator register)

Maintain a register outside git (spreadsheet, password manager, or ops wiki). Minimum columns:

| Secret | Key Vault name | Owner | Rotation frequency | Last rotated | Who can read |
|---|---|---|---|---|---|
| SQL connection string | `ConnectionStrings-DefaultConnection` | DevOps | 90 days | | DevOps, on-call |
| JWT signing key | `Jwt-Key` | DevOps | Annually or on compromise | | DevOps |
| SMTP password | `Email-Smtp-Password` | DevOps | Per provider policy | | DevOps |
| PlatformAdmin bootstrap | `Seed-AdminPassword` | DevOps | One-time (delete after use) | | Deploy operator only |

The deploy script generates a starter inventory under `secrets-inventory/` (gitignored).

---

## Manual steps after implementation

These are **not** automated in the repository and must be completed by operators:

1. **Save generated secrets** — when `Deploy-Azure.ps1` prints SQL password, JWT key, and bootstrap password, store them in a password manager before closing the terminal.
2. **Create dedicated SQL application login** (recommended) — the deploy script currently uses the SQL admin account in the runtime connection string. For GA, create a least-privilege login (`db_datareader`, `db_datawriter`) and update the Key Vault secret; keep admin credentials for migrations only.
3. **Configure SMTP** (P0-5) — see [PRODUCTION_EMAIL_SETUP.md](PRODUCTION_EMAIL_SETUP.md) for full steps, provider notes, and troubleshooting.
4. **Verify bootstrap cleanup** — confirm `Seed__AdminEmail` and `Seed__AdminPassword` are absent from App Service settings and `Seed-AdminPassword` is deleted from Key Vault.
5. **Tighten SQL firewall** — remove temporary deploy-client firewall rules; restrict to Azure services and known operator IPs.
6. **Schedule rotation** — add calendar reminders per the inventory register.

---

## Verification checklist

- [ ] No `.env`, `secrets-inventory/`, or production keys committed to git (`git status` clean of secrets)
- [ ] App Service settings show `@Microsoft.KeyVault(...)` for `ConnectionStrings__DefaultConnection` and `Jwt__Key`
- [ ] `GET /health/ready` returns `Healthy` on the production host
- [ ] API refuses to start if `Jwt__Key` is removed or set to the development placeholder
- [ ] `Seed__*` settings removed after PlatformAdmin bootstrap
- [ ] Secrets inventory register exists with owner and rotation dates

---

## File reference

| File | Purpose |
|---|---|
| `backend/CareHome.Api/appsettings.json` | Base config; empty secrets |
| `backend/CareHome.Api/appsettings.Development.json` | Dev-only JWT and bootstrap (never used in Production) |
| `backend/CareHome.Api/appsettings.Production.json` | Non-secret Production defaults; secrets supplied via env/Key Vault |
| `infra/azure/main.bicep` | Key Vault, managed identity, RBAC |
| `scripts/Deploy-Azure.ps1` | Key Vault secret writes + App Service references |
| `docs/PRODUCTION_CONFIGURATION.md` | Full environment variable reference |
| `.env.example` | Docker dev defaults only |
| `.gitignore` | Excludes `.env`, `secrets-inventory/` |

# Azure hosting (App Service + Azure SQL)

Same-origin deploy: one App Service hosts the Angular SPA (static files in API `wwwroot`) and the ASP.NET Core API (`/api`, `/health`). Azure SQL is the Production database. Document PDFs/CSVs mount to Azure Files at `/home/carehome-documents`.

## Prerequisites

- Azure subscription
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (`winget install --exact --id Microsoft.AzureCLI`)
- .NET 10 SDK, Node.js 20+, EF tools (`dotnet tool install -g dotnet-ef` if needed)
- `az login` completed

## One-command deploy

**1. Sign in to Azure** (required once on this machine):

```powershell
az login
```

**2. Provision + migrate + deploy** from the repo root:

```powershell
.\scripts\Deploy-Azure.ps1 `
  -ResourceGroup rg-carehome `
  -Location uksouth `
  -AppName carehome-pilot
```

**3. Verify**:

```powershell
.\scripts\Verify-AzureDeploy.ps1 -BaseUrl https://carehome-pilot.azurewebsites.net
```

The deploy script will:

1. Create resource group + Bicep stack ([infra/azure/main.bicep](../infra/azure/main.bicep)): Linux App Service (B1), Azure SQL Basic, Storage account + Azure Files share
2. Open a temporary SQL firewall rule for your client IP
3. Run `dotnet ef database update` against Azure SQL
4. Build Angular into API `wwwroot` and publish (`scripts/Publish-CareHome.ps1`)
5. Set Production app settings (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, document path, simulated email)
6. ZIP-deploy to App Service and restart
7. Wait for `/health/live` and `/health/ready`
8. Remove `Seed__AdminEmail` / `Seed__AdminPassword` after first boot

Generated SQL password, JWT key, and PlatformAdmin password are printed **once** — store them in a password manager / Key Vault.

## Publish only (no Azure)

```powershell
.\scripts\Publish-CareHome.ps1
# Output: artifacts/carehome-api
```

## Manual configuration reference

See [PRODUCTION_CONFIGURATION.md](PRODUCTION_CONFIGURATION.md). Same-origin: leave `Cors__AllowedOrigins` empty.

Minimum App Settings:

| Setting | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string |
| `Jwt__Key` | ≥32 mixed characters |
| `DocumentStorage__RootPath` | `/home/carehome-documents` |
| `Email__Mode` | `Development` (simulated) or `Smtp` + SMTP settings |

## After deploy

1. Open `https://<app-name>.azurewebsites.net`
2. Sign in as the seeded PlatformAdmin
3. Create an organisation (`POST /api/platform/tenants` or Organisations UI)
4. Run [PRODUCTION_SMOKE_TEST.md](PRODUCTION_SMOKE_TEST.md) on a **test** tenant

## SQL firewall

- `AllowAzureServices` (0.0.0.0–0.0.0.0) lets App Service reach Azure SQL
- Deploy script adds a temporary rule for your public IP so migrations can run from your machine
- Tighten firewall for production pilots (remove broad rules; prefer private endpoints later)

## Cost (starting point)

App Service B1 + Azure SQL Basic + Storage LRS — suitable for Dev/Test and small pilots. Scale SKUs in Bicep parameters as needed.

## Rollback

Redeploy the previous ZIP (`az webapp deploy`). If a migration was applied, restore the Azure SQL backup — do not run EF `Down()` on live financial data. See [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md) and [BACKUP_RESTORE.md](BACKUP_RESTORE.md).

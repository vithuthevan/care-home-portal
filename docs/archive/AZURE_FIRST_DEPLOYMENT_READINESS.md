# Azure First Deployment Readiness

**Date:** 15 September 2026  
**Scope:** Pre-Azure deployment audit and preparation only. No resources were provisioned, no production commands were run, and no application code was changed.  
**Intended use:** First Azure deployment after the client demo, so the deploy is predictable and safe.

---

## 1. Deployment Status

Current readiness:

**🟡 Requires preparation**

The application has **not** been deployed to Azure. Templates, deploy scripts, production fail-fast validation, Key Vault wiring, and health checks exist and can support a first deploy. The default one-command path is **not** safe to run as-is.

| Area | Assessment |
|------|------------|
| Application production configuration | Ready, with secrets and SMTP supplied at deploy time |
| Azure Bicep / deploy script | Ready as a controlled-pilot stack |
| Database first-create + EF migrations | Ready, with leftover historical tenant data (see §7) |
| Operator / client inputs | **Not complete** — SMTP, Azure subscription, unique names, admin mailbox |
| Default `Deploy-Azure.ps1` with no email flags | **Will fail** — Production refuses to start without SMTP or an explicit simulation opt-in |
| Live funder invoicing | Separate gate — billing/Sage rules remain PENDING business sign-off |

Do **not** treat this as 🟢 until the preparation checklist in §4 step 1 is complete. Do **not** treat this as 🔴: the first Azure deploy can succeed if those inputs and permissions are in place.

**What “ready” means here:** a same-origin App Service + Azure SQL pilot that boots, authenticates, stores documents, and can create a test organisation. It does **not** mean unrestricted live invoicing.

---

## 2. Required Azure Resources

Provisioned by `infra/azure/main.bicep` via `scripts/Deploy-Azure.ps1` (unless `-SkipProvision`).

| Resource | Purpose | Status | Required Action |
|----------|---------|--------|-----------------|
| Resource group | Container for all pilot resources | Not created | Create via deploy script (`az group create`) in chosen region (default `uksouth`) |
| Linux App Service plan (B1 Basic) | Host API + Angular SPA | Defined in Bicep; not deployed | Confirm B1 is acceptable for a single-instance pilot; raise SKU later if month-end load needs it |
| Linux Web App (`DOTNETCORE\|10.0`) | Runtime for published API; SPA in `wwwroot` | Defined in Bicep; not deployed | Confirm .NET 10 stack is available in the target region; App name must be globally unique (`^[a-z0-9-]{3,40}$`) |
| System-assigned managed identity | Read Key Vault secrets at runtime | Defined in Bicep | No extra action if Bicep RBAC succeeds |
| Azure SQL Server | SQL host (TLS 1.2, public access) | Defined in Bicep; not deployed | SQL server name must be globally unique (script default `sql-<appName>`) |
| Azure SQL Database `CareHome` (Standard S0) | Application database; 35-day PITR + long-term retention defaults | Defined in Bicep; not deployed | Keep Standard (not Basic) so PITR/LTR settings apply |
| SQL firewall `AllowAzureServices` (`0.0.0.0`) | Lets App Service reach Azure SQL | Defined in Bicep | Required for first deploy; tighten later (do not remove before VNet/private endpoint exists) |
| Temporary SQL firewall rule for operator IP | Lets `dotnet ef database update` run from the deploy machine | Created by deploy script | **Manual cleanup after deploy** — script does not delete the rule |
| Azure Key Vault (RBAC, soft-delete 7 days) | Store connection string, JWT key, SMTP password, bootstrap password | Defined in Bicep; not deployed | Vault name is derived and truncated to 24 characters; must be globally unique |
| Key Vault Secrets User on Web App identity | App Service Key Vault references | Defined in Bicep | Requires permission to create role assignments (see §9) |
| Key Vault Secrets Officer on deployer | Script can `az keyvault secret set` | Defined in Bicep when `deployerObjectId` is resolved | Deploy as an Azure AD **user**; service principal deploys will fail (`principalType: 'User'`) |
| Storage account (Standard LRS, HTTPS, no public blobs) | Azure Files backend for PDFs/Sage CSVs | Defined in Bicep; not deployed | Account name derived + truncated to 24 characters; must be globally unique |
| Azure Files share `carehome-documents` (50 GB) | Document root mounted at `/home/carehome-documents` | Defined in Bicep | After deploy, confirm the mount is writable (see §7) |
| Recovery Services Vault + daily Azure Files policy | Document backup policy object | Defined in Bicep (`enableDocumentBackup=true`) | Policy is **not** attached automatically — run `Enable-AzureFileBackup.ps1` once |
| Custom domain / App Gateway / WAF | Public hostname beyond `*.azurewebsites.net` | **Not in Bicep** | Not required for first deploy; default HTTPS hostname is enough |
| Application Insights / Log Analytics | Centralised logs and alerts | **Not in Bicep** | Not required to boot; debugging first-deploy failures will rely on App Service log stream |
| Staging slot | Blue/green swap | **Not in Bicep** | First-deploy rollback is ZIP redeploy + SQL recreate/PITR, not a slot swap |
| Private endpoints / VNet | Lock down SQL, storage, Key Vault | **Not in Bicep** | Public network access is enabled on SQL, Key Vault, and Recovery Vault |

Docker Compose and the API/frontend Dockerfiles are **development-only**. First Azure deploy uses ZIP deploy of `scripts/Publish-CareHome.ps1` output, not containers.

---

## 3. Required Configuration

Environment separation is real:

| Environment | How it is selected | What it allows |
|-------------|--------------------|----------------|
| Development | `ASPNETCORE_ENVIRONMENT=Development` (local / Docker Compose) | LocalDB, JWT placeholder, `admin@localhost`, demo org seed, simulated email success, `Database__ApplyMigrations` in Compose only |
| Production | Bicep sets `ASPNETCORE_ENVIRONMENT=Production` | LocalDB rejected; weak/placeholder JWT rejected; dev bootstrap credentials rejected; SMTP required unless explicit simulation flag; demo master-data seeder **does not run** |

`appsettings.Production.json` leaves secrets empty. Runtime values must come from App Service settings / Key Vault references. `Database:ApplyMigrations` is **not** set in Production (defaults `false`). Migrations are applied from the operator machine, not at app startup.

| Configuration | Source | Required Before Deployment |
|---------------|--------|----------------------------|
| `ASPNETCORE_ENVIRONMENT=Production` | Bicep app setting | Yes — already set by template |
| `WEBSITES_PORT=8080` | Bicep app setting | Yes — already set by template |
| `Https__Redirect=true` | Bicep + App Service `httpsOnly` | Yes — already set |
| `DocumentStorage__RootPath=/home/carehome-documents` | Bicep + deploy script | Yes — already set; verify mount after deploy |
| `ConnectionStrings__DefaultConnection` | Generated by deploy script → Key Vault `ConnectionStrings-DefaultConnection` | Yes — script generates SQL admin password if omitted |
| `Jwt__Key` (≥32 mixed characters, not the Development placeholder) | Generated by deploy script → Key Vault `Jwt-Key` | Yes — script generates if omitted |
| `Email__Mode=Smtp` **or** `Email__AllowSimulationInProduction=true` | Deploy script flags; **not** in Bicep | **Yes — first deploy fails without one of these** |
| `Email__Smtp__Host`, `Email__FromAddress` | Client mailbox / provider | **Yes if Path A (live SMTP)** |
| `Email__Smtp__User` + `Email__Smtp__Password` | Client mailbox; password → Key Vault `Email-Smtp-Password` | **Yes if the SMTP server requires auth** (almost always) |
| `Email__Smtp__Port` / `EnableSsl` | Defaults 587 / true | Only if the provider differs |
| `Seed__AdminEmail` / `Seed__AdminPassword` | Deploy script (bootstrap only) | Yes for first boot so a PlatformAdmin exists; script generates if omitted; **removed after first successful start** |
| `Cors__AllowedOrigins` | Empty in Production appsettings | Leave empty (same-origin SPA+API). Do not add localhost |
| `App__PublicUrl` | **Not set by Bicep or deploy script** | Strongly recommended before creating organisations (welcome-email sign-in link) |
| `ForwardedHeaders__KnownProxies` | Empty | Optional; login rate-limit may see App Service front-end IPs, not clients |
| `Database__ApplyMigrations` | Must stay unset/`false` on App Service | Do **not** enable on Azure |
| `Jwt__Issuer` / `Jwt__Audience` | Defaults `CareHomeApi` / `CareHomeWeb` | No |
| Angular API base URL | Relative `/api` in the SPA | No extra frontend env file for same-origin Azure |

### Secrets that must be provided or generated

| Secret | Who supplies it | Where it must live |
|--------|-----------------|-------------------|
| Azure SQL admin password | Generated by script, or `-SqlAdminPassword` | Password manager + Key Vault (inside the connection string) |
| JWT signing key | Generated by script, or `-JwtKey` | Password manager + Key Vault `Jwt-Key` |
| PlatformAdmin bootstrap password | Generated by script, or `-SeedAdminPassword` | Password manager **immediately** (console prints once; Key Vault copy is deleted after bootstrap) |
| SMTP password | **Client / business** | Key Vault `Email-Smtp-Password` |
| Azure login (operator) | Deploying engineer | `az login` — not stored in the app |

Never commit `.env`, `secrets-inventory/`, or a filled `main.bicepparam`. The example param file has empty `appName` / SQL names / password on purpose.

### Configuration that requires client or business input

| Input | Why |
|-------|-----|
| Azure subscription and region | Billing owner; default region `uksouth` |
| Globally unique `AppName` | App Service, SQL, storage, and Key Vault names derive from it |
| Billing mailbox + SMTP host/user/password | Production startup **and** organisation creation |
| Whether Path B (simulated email) is acceptable | Path B lets the API start but **blocks organisation creation** (see §7) |
| PlatformAdmin email (optional) | Default `platform.admin@<appName>.local` is not a real mailbox |
| Test organisation name and tenant-admin email | Needed after login; welcome email goes to this address |
| Finance sign-off of proration / Sage mapping | Not required to **deploy**; required before invoicing real funders |

---

## 4. Deployment Steps

Exact order for the **first** Azure deployment. Do not skip step 1.

1. **Complete operator prerequisites (local machine)**  
   Azure CLI, `az login`, **Owner or Contributor + User Access Administrator** on the target subscription (see §9), .NET 10 SDK, Node.js 20+, `dotnet tool install -g dotnet-ef`, password manager open.

2. **Collect client SMTP decision (blocking)**  
   - **Path A (required for a usable pilot):** dedicated billing mailbox, SMTP host, port 587, username, password, From address. Confirm SMTP AUTH is allowed (Microsoft 365 often needs it enabled per mailbox).  
   - **Path B (interim only):** business sign-off for `Email__AllowSimulationInProduction=true`. Understand that organisation create will fail until SMTP is configured.

3. **Choose names and record them**  
   Resource group (e.g. `rg-carehome`), location (`uksouth`), `AppName` (globally unique), SQL admin login (default `carehomeadmin`). Confirm the names are unused in Azure.

4. **Do not copy Development settings**  
   Do not set `ASPNETCORE_ENVIRONMENT=Development` on App Service. Do not use `admin@localhost` / `DevAdmin!12345`. Do not set `Database__ApplyMigrations=true`. Do not use Docker Compose as the Azure host.

5. **Provision + migrate + deploy (Path A example)**  
   From the repo root, after `az login`:

   ```powershell
   .\scripts\Deploy-Azure.ps1 `
     -ResourceGroup rg-carehome `
     -Location uksouth `
     -AppName <globally-unique-name> `
     -EmailMode Smtp `
     -SmtpHost <smtp-host> `
     -SmtpUser <smtp-user> `
     -SmtpFromAddress <billing@client-domain> `
     -SmtpFromName "Care Home Billing" `
     -SmtpPassword (ConvertTo-SecureString '<smtp-password>' -AsPlainText -Force) `
     -SeedAdminEmail <operator-or-agreed-admin@domain>
   ```

   Omit `-SmtpPassword` / `-SeedAdminEmail` only if you pass them as `SecureString` / explicit values another way. If SMTP is deliberately deferred (Path B):

   ```powershell
   .\scripts\Deploy-Azure.ps1 `
     -ResourceGroup rg-carehome `
     -Location uksouth `
     -AppName <globally-unique-name> `
     -AllowSimulatedEmail
   ```

   The script will, in order: create the resource group; deploy Bicep; open a SQL firewall rule for the operator public IP; run `dotnet ef database update` against Azure SQL; build SPA + publish API (`Publish-CareHome.ps1`); write secrets to Key Vault; set App Service Key Vault references; ZIP-deploy; restart; wait up to 5 minutes for `/health/ready`; remove `Seed__*` app settings and delete `Seed-AdminPassword` from Key Vault.

6. **Save generated secrets immediately**  
   SQL password, JWT key, and PlatformAdmin password are printed **once**. Copy them to a password manager before closing the terminal. Move `secrets-inventory/<app>-<date>.md` out of the repo (it is gitignored).

7. **Set `App__PublicUrl` (manual; not in the script)**  

   ```text
   App__PublicUrl=https://<app-name>.azurewebsites.net
   ```

   Restart the web app so welcome emails include the real sign-in URL.

8. **Enable Azure Files backup (manual; Bicep only creates the vault and policy)**  

   ```powershell
   .\scripts\Enable-AzureFileBackup.ps1 `
     -ResourceGroup rg-carehome `
     -RecoveryVaultName <output recoveryVaultName> `
     -StorageAccountName <output storageAccountName>
   ```

9. **Remove the temporary SQL firewall rule** created as `DeployClient-<timestamp>`. Keep `AllowAzureServices` until a private network design exists.

10. **Verify** with `.\scripts\Verify-AzureDeploy.ps1 -BaseUrl https://<app-name>.azurewebsites.net`, then the checks in §5. Use a **test** organisation only.

11. **Do not invoice real funders** until `docs/PRODUCTION_BUSINESS_SIGNOFF.md` is APPROVED.

`-PreMigrationBackup` is unnecessary on a brand-new empty database. Use it on every **later** release that applies migrations.

---

## 5. Post Deployment Verification

Run in this order. Capture `X-Correlation-ID` on failures.

### Application starts

- App Service state is Running.
- Log stream shows Production startup, **not** Development.
- Expected email log: `Email mode is Smtp. Host=... From=...`  
  If Path B: warning `PRODUCTION EMAIL SIMULATION EXPLICITLY ALLOWED`.
- No fail-fast exceptions for JWT, LocalDB, CORS, or missing SMTP.

### Health checks

| Check | Expected |
|-------|----------|
| `GET /health/live` | 200, `{ "status": "Healthy" }` |
| `GET /health/ready` | 200, `{ "status": "Healthy" }` (SQL reachable) |
| `GET /` | 200 HTML (SPA from `wwwroot`) |
| Script | `.\scripts\Verify-AzureDeploy.ps1 -BaseUrl https://<app>.azurewebsites.net` |

If live is healthy and ready is not, Key Vault connection-string resolution or SQL firewall is the usual cause.

### Login

- Sign in as the **seeded PlatformAdmin** (email from deploy output). Do **not** use `admin@localhost`.
- Confirm JWT is issued, organisation name handling is empty for PlatformAdmin, and `/api/companies` returns **403** (platform users have no tenant).
- Confirm `Seed__AdminEmail` and `Seed__AdminPassword` are **gone** from App Service settings and `Seed-AdminPassword` is deleted from Key Vault.
- Change the bootstrap password after first login if the generated value was used.

### User creation

- Create a **test** organisation with a real admin email (Organisations UI / `POST /api/platform/tenants`). Administrator email is **required**.
- Path A: welcome email arrives; tenant admin must change password before other APIs (`MustChangePassword`).
- Path B: this step is expected to **fail** with an email-not-configured message; do not treat that as a random bug.
- After tenant-admin login, create a second user (Administrator / LocationManager / ReadOnly) from Users. Tenant user create sets the password in-app and does **not** send email.

### Invoice workflow

Use the **test tenant only**. Do not generate a live funder invoice.

- Enter company, care home, funding authority, nominals, invoice template (bank details), client, funding contract and rate.
- Billing **preview**, then generate on the test tenant.
- Open invoice detail; download PDF (`GET /api/invoices/{id}/pdf` body starts `%PDF`).
- Confirm invoice numbering on the new tenant (not on leftover tenant 1 — see §7).

### Email verification

- Send the test invoice (`POST /api/invoices/{id}/send`).
- Path A: recipient mailbox receives PDF; invoice marked Sent; `EmailSendLogs.Success = 1`, `Simulated = 0`.
- Path A failure (auth/firewall): API returns 400; invoice is **not** marked Sent. Check outbound TCP 587 from App Service and provider SMTP AUTH policy.
- Path B: send fails visibly; invoice stays unsent. That is by design.

### Document upload / file storage

- Invoice/credit-note PDFs write under `{DocumentStorage__RootPath}/tenants/{tenant-public-id}/...`.
- Confirm files appear on the Azure Files share `carehome-documents`, not only in App Service ephemeral disk.
- Multipart limit is 2 MB (misc CSV / uploads). Oversized files must fail cleanly.
- If PDF generate returns 500, check App Service Linux fonts/native dependencies (QuestPDF) and that the Files mount is writable.

### Permissions

| Actor | Expected |
|-------|----------|
| Anonymous `/api/companies` | 401 |
| PlatformAdmin on tenant APIs | 403 |
| TenantAdmin on another tenant’s invoice id | 404 (not 403) |
| ReadOnly | GET 200; writes 403; PDF/report read allowed |
| LocationManager | Only assigned care homes; others 404 |
| Inactive tenant | Users cannot log in; existing tokens 403 |
| Login brute force | Lockout after 5 failures; HTTP 429 after 10 login posts/minute/IP |

Also run the minimum table in `docs/PRODUCTION_SMOKE_TEST.md` against the test tenant.

---

## 6. Rollback Plan

There is **no previous production ZIP and no PITR restore point at the moment of first create**. Rollback strategy for the first deploy is different from later releases.

### If Bicep / resource provision fails

- The resource group may be empty or partial.
- Safe action: inspect the failed deployment in the portal, fix the cause (name collision, missing RBAC, region quota, .NET 10 stack), and **re-run** `Deploy-Azure.ps1`.
- If the group is a broken experiment with no data: delete the resource group and start clean.
- Do not leave a half-created Key Vault/SQL server with unknown admin passwords.

### If EF migrations fail (before or during first `database update`)

- Azure SQL automated PITR is **not** reliable until the first full backup completes (can take hours on a new database).
- EF Core `database update` is forward-only and can be retried after a transient error.
- **Do not** run `dotnet ef database update <previous>` / migration `Down()` — several migrations (including `AddMultiTenancy`) are not reversible.
- Acceptable first-deploy recovery: drop and recreate the `CareHome` database (or delete the SQL resource and re-provision), then re-run migrations. There must be **no** real client data yet.
- After the first successful backup exists, later migration failures use PITR + document share restore per `BACKUP_AND_RECOVERY_RUNBOOK.md`.

### If App Service deploy or startup fails after migrations succeeded

- Database schema may already be current; Identity roles and PlatformAdmin may already exist.
- Fix configuration (SMTP, Key Vault references, JWT) and **redeploy the ZIP** / restart. Do not re-run bootstrap with Development credentials.
- If Key Vault references are unresolved, wait for RBAC propagation (often a few minutes) and restart. The deploy script already retries Key Vault writes for up to 3 minutes and health for up to 5 minutes.
- If PlatformAdmin seed failed (weak password, unresolved secret) and the script already deleted `Seed__*`, the database has no admin. Recovery: temporarily set new `Seed__AdminEmail` / `Seed__AdminPassword` (unique, ≥12 mixed characters), restart once, then remove them again. Do not use `admin@localhost`.

### If the application starts but is the wrong package or a bad build

- Redeploy a known-good publish folder with `az webapp deploy`.
- First deploy has no prior package; keep the `artifacts/carehome-api` folder from a successful publish as the rollback ZIP.

### If SMTP is wrong after a successful start

- App is up; organisation create or invoice send fails.
- Update SMTP app settings / Key Vault `Email-Smtp-Password`, restart. No database rollback.

### If you must abandon the entire first environment

1. Confirm there is no real resident/financial data.  
2. Delete the resource group.  
3. Rotate any secrets that were printed to the console (treat them as exposed to that operator workstation).  
4. Re-run §4 from step 3 with new names if old names are still reserved.

### What does *not* roll back automatically

- Azure Files content (PDFs, Sage CSVs) is not in SQL.  
- File-share backup is not active until `Enable-AzureFileBackup.ps1` succeeds.  
- SQL admin password and JWT key printed to the console remain valid until rotated.  
- Temporary SQL firewall rules remain until deleted.

---

## 7. First Deployment Risks

Things most likely to fail or surprise on the **first** Azure deployment.

| Risk | Effect | Mitigation |
|------|--------|------------|
| Deploy without `-EmailMode Smtp` or `-AllowSimulatedEmail` | `ProductionStartupValidator` throws; `/health/ready` never becomes Healthy; script times out | Decide Path A or Path B **before** running the script |
| Path B (simulated email) | API starts; `POST /api/platform/tenants` **fails** because admin welcome email must succeed and Production simulation returns `Success=false` | Use Path A for a usable pilot; Path B is start-only |
| Key Vault RBAC not granted | Bicep role assignments fail, or App Service cannot resolve `@Microsoft.KeyVault(...)` | Deploying identity needs `Microsoft.Authorization/roleAssignments/write` (§9) |
| Key Vault RBAC propagation delay | First start uses unresolved JWT/connection string; app crash-loops | Wait and restart; script already retries |
| Globally unique names | App, SQL, storage, Key Vault deploy fails with conflict | Pick a unique `AppName` |
| `.NET 10` Linux stack missing in region | Web App provision fails on `linuxFxVersion: DOTNETCORE\|10.0` | Confirm stack in `uksouth` (or chosen region) before deploy day |
| `dotnet-ef` missing on operator PC | Migration step throws | Install global `dotnet-ef` matching the SDK |
| Corporate network blocks TCP 1433 | Migrations cannot reach Azure SQL even with firewall rule | Run deploy from a network that can reach `*.database.windows.net,1433` |
| SQL admin used as runtime login | Broader than `db_datareader`/`db_datawriter` | Acceptable for first pilot; create a least-privilege login before GA |
| Historical tenant **Existing Organisation** | Fresh migration chain inserts tenant Id=1 plus default invoice categories; companies “Sovereign Care Homes” / “Care Pro” are inserted then deleted if unused | After migrate, in Organisations UI: deactivate or ignore tenant 1; create a **new** test organisation for smoke tests |
| Azure Files mount under `/home/carehome-documents` | Linux App Service custom mounts are typically documented under `/mounts/...`; `/home` is also the persistent home | After deploy, generate a test PDF and confirm the file is on the share |
| QuestPDF on App Service Linux | Docker image installs fonts; ZIP deploy does not | Verify PDF download in smoke test |
| App Service outbound SMTP | Port 25 blocked; 587 usually allowed; many providers block datacentre IPs | Use a transactional SMTP mailbox with AUTH; test send immediately |
| `App__PublicUrl` unset | Welcome email has no Azure URL | Set it in step 7 |
| Login rate limit uses `RemoteIpAddress` | Without trusted forwarded headers, many users may share the App Service front-end IP (10 logins/min) | Monitor 429s; configure `ForwardedHeaders:KnownProxies` only if needed |
| HTTPS redirect vs Azure TLS offload | Platform `httpsOnly=true` should prevent loops | If a redirect loop appears, check forwarded proto / `Https__Redirect` |
| File backup not enabled | Recovery vault exists but share is unprotected | Run `Enable-AzureFileBackup.ps1` |
| No Application Insights | First-fail diagnosis is log stream only | Use `az webapp log tail` during first start |
| Uncommitted local WIP | Pilot might not match the demo build | Deploy from a tagged/known commit, not mixed working-tree changes |
| Billing/Sage PENDING sign-off | Correct deploy can still produce unapproved invoice amounts | Test tenant only until `docs/PRODUCTION_BUSINESS_SIGNOFF.md` is signed |

### Migration safety (first empty database)

- Latest migration: `20260911053954_UniqueMiscChargeDedupeIndex` (12-step chain from `InitialCreate`).
- Startup does **not** auto-migrate in Production.
- `DevelopmentMasterDataSeeder` (Demo Care Group / Sunrise House) **does not run** in Production.
- `IdentitySeeder` **does** run: creates roles and, if `Seed__*` is set, one PlatformAdmin.
- Historical `InsertData` in early migrations **does** run on a greenfield database (see Existing Organisation above). That is leftover schema history, not the Development demo seeder.
- `Down()` is not a production rollback tool.

---

## 8. Azure Permissions Required

The operator who runs `Deploy-Azure.ps1` needs:

| Permission | Why |
|------------|-----|
| Azure subscription access and `az login` | All ARM/Bicep operations |
| **Owner**, or **Contributor + User Access Administrator**, on the subscription or resource group | Bicep creates Key Vault role assignments. **Contributor alone cannot assign RBAC** and will fail at `webAppSecretsUserRole` / `deployerSecretsOfficerRole` |
| Microsoft.Resources/deployments/write | `az deployment group create` |
| Microsoft.Web/* | App Service plan, web app, ZIP deploy, app settings |
| Microsoft.Sql/* | SQL server, database, firewall rules |
| Microsoft.KeyVault/* | Vault create + secret set |
| Microsoft.Storage/* | Storage account + file share |
| Microsoft.RecoveryServices/* | Vault + backup policy; later `Enable-AzureFileBackup.ps1` |
| Microsoft.Authorization/roleAssignments/write | Managed identity → Key Vault Secrets User |
| Directory permission to read signed-in user object ID | `az ad signed-in-user show` for `deployerObjectId`. Guest/SPN accounts may return empty; Key Vault secret writes then fail |

The **Web App managed identity** needs Key Vault Secrets User (created by Bicep). It does not need Azure AD admin on SQL for the current connection-string design.

---

## 9. Missing Manual Steps (not automated)

These are not done by Bicep or `Deploy-Azure.ps1`:

1. Obtain Azure subscription/owner approval and unique resource names.  
2. Obtain SMTP mailbox and credentials (or written Path B sign-off).  
3. Save console-printed secrets to a password manager.  
4. Set `App__PublicUrl`.  
5. Run `Enable-AzureFileBackup.ps1`.  
6. Delete the temporary SQL firewall rule.  
7. Confirm leftover **Existing Organisation** (tenant Id=1) is not used as the client tenant.  
8. Create the real/test organisation and tenant admin **after** SMTP works.  
9. Remove bootstrap PlatformAdmin password from chat logs / terminal scrollback.  
10. Optional before GA: dedicated SQL app login (data-only), Application Insights, private endpoints, custom domain, staging slot.  
11. Restore drill on **this** Azure SQL database after the first backup exists (`RESTORE_DRILL_PROCEDURE.md`) — not required to boot, required before live data.

---

## 10. Related documents

| Document | Use |
|----------|-----|
| `docs/AZURE_HOSTING.md` | One-command Azure flow |
| `docs/PRODUCTION_CONFIGURATION.md` | Full setting reference |
| `PRODUCTION_SECRETS_SETUP.md` | Key Vault names and rotation |
| `PRODUCTION_EMAIL_SETUP.md` | SMTP Path A / Path B |
| `docs/PRODUCTION_DEPLOYMENT.md` | General production sequence (on-prem and Azure) |
| `docs/PRODUCTION_SMOKE_TEST.md` | Post-deploy functional checks |
| `BACKUP_AND_RECOVERY_RUNBOOK.md` | Later-release backup/restore |
| `docs/PRODUCTION_BUSINESS_SIGNOFF.md` | Gate before live funder invoices |
| `infra/azure/main.bicep` | Infrastructure source of truth |
| `scripts/Deploy-Azure.ps1` | First-deploy orchestrator |

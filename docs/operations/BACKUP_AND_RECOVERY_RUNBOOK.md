# Backup and Recovery Runbook

**Application:** Care Home Back-Office Management System  
**Last updated:** 15 September 2026  
**Scope:** Production database and document storage backup readiness (P0-3)  
**Related:** `docs/BACKUP_RESTORE.md` (technical restore steps), `P0_EXECUTION_ROADMAP.md` (P0-3 / P0-4)

---

## 1. What must be backed up

| Component | Contents | Backup owner | Storage location |
|-----------|----------|--------------|------------------|
| **Azure SQL database** (`CareHome`) | Tenants, users, clients, contracts, invoices, credits, audit, sequences | **DevOps** | Azure-managed (platform storage, geo-redundant per region) |
| **Document storage** (`/home/carehome-documents` on Azure; configurable path on-prem) | Invoice PDFs, credit-note PDFs, Sage export CSVs, tenant logos | **DevOps** | Azure Files share + Recovery Services Vault (Azure) or mirrored volume (on-prem) |

A database-only backup is **not** sufficient. Invoice rows store relative `PdfPath` values; PDFs can regenerate from snapshots, but Sage CSVs and logo files exist only on disk.

---

## 2. Automated backup strategy

### 2.1 Azure SQL (recommended production host)

| Setting | Production default | Notes |
|---------|-------------------|-------|
| **Backup type** | Automated full + differential + transaction log (platform-managed) | No SQL Agent job required |
| **Frequency** | Full: weekly; Differential: every 12–24 h; Log: every 5–10 min | Controlled by Azure SQL service |
| **Point-in-time restore (PITR)** | Continuous within retention window | Restore to any second within window |
| **Short-term retention** | **35 days** (IaC default) | Configured in `infra/azure/main.bicep` via `sqlBackupRetentionDays` |
| **Long-term retention (LTR)** | Weekly: 4 weeks; Monthly: 12 months; Yearly: 5 years | Requires Standard tier or higher; enabled by default in IaC |
| **Storage location** | Azure-managed backup storage (same region; geo-redundant per Azure SQL SLA) | Not customer-accessible `.bak` files |
| **Ownership** | **DevOps** provisions and monitors; **Business/Finance** approves RPO/RTO and legal retention |

**Tier requirement:** `Standard S0` or higher (IaC default). `Basic` tier provides only **7 days** PITR and cannot configure 35-day retention or LTR.

### 2.2 Document storage (Azure Files)

| Setting | Production default | Notes |
|---------|-------------------|-------|
| **Backup type** | Azure Backup for Azure Files (daily) + on-demand share snapshot | Vault provisioned by Bicep; protection enabled via `scripts/Enable-AzureFileBackup.ps1` |
| **Frequency** | Daily at **02:00 UTC** | Policy name: `DailyAzureFiles` |
| **Retention** | **30 days** (IaC default, `documentBackupRetentionDays`) | Adjust in Bicep for legal requirements |
| **Storage location** | Recovery Services Vault in the same resource group | Geo-redundant vault storage (Azure-managed) |
| **Coupling** | Run document backup in the **same window** as pre-migration SQL checkpoint (±15 min) | Use `scripts/Backup-CareHome.ps1` before migrations |

### 2.3 On-premises / self-hosted SQL Server

| Setting | Recommendation | Notes |
|---------|----------------|-------|
| **Backup type** | `BACKUP DATABASE ... WITH CHECKSUM` | Full backup daily; transaction-log backups hourly if FULL recovery model |
| **Frequency** | Daily full + hourly log (business hours) + **before every migration** | SQL Agent or Task Scheduler + `sqlcmd` |
| **Retention** | Operator/legal decision (minimum 30 days on-site, 90 days off-site suggested) | Document in ops register |
| **Storage location** | Dedicated backup volume + off-site copy | Encrypt at rest |
| **Documents** | `robocopy` mirror of `DocumentStorage__RootPath` | Same schedule as SQL job |

Use `scripts/Backup-CareHome.ps1 -Target OnPrem` for coordinated on-prem backups.

### 2.4 RPO / RTO (agree with Business / Finance)

| Metric | Suggested starting point | Owner |
|--------|-------------------------|-------|
| **RPO** (max acceptable data loss) | **1 hour** (Azure SQL PITR) / **24 hours** (documents on daily schedule) | Business + DevOps |
| **RTO** (max acceptable downtime) | **4 hours** (restore + verification) | Business + DevOps |

Record agreed values in the operator wiki when legal/finance sign off. These are starting points, not legal retention rules.

---

## 3. Infrastructure readiness (IaC)

### 3.1 Configured in `infra/azure/main.bicep`

| Resource | Backup configuration |
|----------|---------------------|
| `sqlDatabase` | SKU defaults to **Standard S0**; `shortTermRetentionPolicy` (35 days, 12 h interval); `longTermRetentionPolicy` (weekly/monthly/yearly) |
| `recoveryVault` | Recovery Services Vault for document backups |
| `documentBackupPolicy` | Daily Azure Files policy (`DailyAzureFiles`, 30-day retention) |

### 3.2 Parameters (see `infra/azure/main.bicepparam.example`)

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `sqlDatabaseSku` | `S0` | SQL compute tier |
| `sqlDatabaseSkuTier` | `Standard` | Enables configurable PITR |
| `sqlBackupRetentionDays` | `35` | Short-term PITR retention |
| `enableLongTermRetention` | `true` | Weekly/monthly/yearly backups |
| `enableDocumentBackup` | `true` | Provision Recovery Services Vault |
| `documentBackupRetentionDays` | `30` | File share backup retention |

### 3.3 Post-deploy manual step (one-time)

After first Bicep deploy, enable Azure Files protection:

```powershell
.\scripts\Enable-AzureFileBackup.ps1 `
  -ResourceGroup rg-carehome `
  -RecoveryVaultName <output recoveryVaultName> `
  -StorageAccountName <output storageAccountName>
```

Verify:

```powershell
.\scripts\Verify-BackupReadiness.ps1 `
  -ResourceGroup rg-carehome `
  -SqlServerName sql-carehome-pilot `
  -RecoveryVaultName <output recoveryVaultName>
```

---

## 4. Backup process

### 4.1 Daily automated (Azure)

No operator action required for Azure SQL — platform backups run continuously.

Document storage: Azure Backup runs daily per policy. Monitor via `Verify-BackupReadiness.ps1` or Azure Portal → Recovery Services Vault → Backup items.

### 4.2 Before every EF migration (mandatory)

**Azure:**

```powershell
.\scripts\Backup-CareHome.ps1 `
  -Target Azure `
  -ResourceGroup rg-carehome `
  -SqlServerName sql-carehome-pilot `
  -SqlDatabaseName CareHome `
  -Purpose PreMigration
```

This records the current PITR window and creates an Azure Files share snapshot. Then apply migrations per `docs/PRODUCTION_DEPLOYMENT.md`.

**On-prem:**

```powershell
.\scripts\Backup-CareHome.ps1 `
  -Target OnPrem `
  -SqlServerInstance localhost `
  -SqlDatabaseName CareHome `
  -DocumentRoot D:\carehome-documents `
  -BackupRoot D:\backups\carehome `
  -Purpose PreMigration
```

### 4.3 Deploy script integration

`scripts/Deploy-Azure.ps1` supports `-PreMigrationBackup` to run the Azure backup step automatically before `dotnet ef database update`.

```powershell
.\scripts\Deploy-Azure.ps1 `
  -ResourceGroup rg-carehome `
  -Location uksouth `
  -AppName carehome-pilot `
  -PreMigrationBackup
```

### 4.4 Off-site copy

| Host | Action | Owner |
|------|--------|-------|
| Azure SQL | Platform geo-redundancy (region-dependent) | Azure (verify SLA with subscription) |
| Azure Files | Recovery Services Vault storage is geo-redundant | DevOps verifies vault region |
| On-prem | Copy `.bak` + document mirror to secondary site or cloud storage daily | DevOps |

Encrypt all off-site copies. Treat backups as confidential (same PII/financial data as production). See `docs/DATA_PROTECTION.md`.

---

## 5. Recovery procedure

**Never restore over the live production database to "test" a backup.** Always restore to an isolated database name first (P0-4 drill).

### 5.1 Azure SQL — point-in-time restore

Restore to a **new** database (e.g. `CareHomeRestoreCheck`):

```powershell
# List available restore window
az sql db list-restore-windows `
  --resource-group rg-carehome `
  --server sql-carehome-pilot `
  --name CareHome

# Restore to a point in time (UTC)
az sql db restore `
  --dest-name CareHomeRestoreCheck `
  --name CareHome `
  --resource-group rg-carehome `
  --server sql-carehome-pilot `
  --time "2026-09-15T10:30:00Z"
```

Or use Azure Portal: SQL database → **Restore** → select timestamp → new database name.

### 5.2 Azure SQL — production cutover (after verification)

Only after a successful restore drill (P0-4):

1. Stop the App Service (or enable maintenance page).
2. Restore `CareHome` to the required point in time **as a new database** (e.g. `CareHomeRestored`).
3. Update Key Vault secret `ConnectionStrings-DefaultConnection` to point at `CareHomeRestored` (or rename databases per your DBA procedure).
4. Restart App Service.
5. Run verification checklist (section 6).

**Alternative:** Rename databases (`CareHome` → `CareHomeCorrupt`; `CareHomeRestored` → `CareHome`) during a maintenance window if your process requires keeping the same database name.

### 5.3 On-premises SQL restore

See `docs/BACKUP_RESTORE.md` for `RESTORE DATABASE ... WITH MOVE` steps. Restore to `CareHomeRestoreCheck`, not production.

### 5.4 Document storage restore

**Azure Files — from Azure Backup:**

Azure Portal → Recovery Services Vault → Backup items → Azure Files → select share → **Restore**. Target a **separate** share or storage account (e.g. `carehome-documents-restore`).

**Azure Files — from share snapshot:**

```powershell
az storage file copy start-batch `
  --source-account-name <account> `
  --source-share carehome-documents `
  --pattern '*' `
  --destination-account-name <account> `
  --destination-share carehome-documents-restore
```

Use the snapshot URL as the source if restoring a specific snapshot (see Azure Files snapshot docs).

**On-prem:**

```powershell
robocopy D:\backups\carehome-documents-predeploy D:\carehome-documents-restore /MIR
```

### 5.5 Reconnect application

Point a **non-production** API instance (or production after cutover approval) at the restored resources:

| Setting | Value |
|---------|-------|
| `ConnectionStrings__DefaultConnection` | `...Database=CareHomeRestoreCheck...` (drill) or restored production DB |
| `DocumentStorage__RootPath` | Restored share mount (e.g. `/home/carehome-documents-restore`) or restored local path |

On Azure App Service, update the Key Vault connection string secret and remount or swap the Azure Files share. Restart the web app.

### 5.6 Rollback after failed release

If a migration was applied and must be abandoned:

1. Restore the **pre-deployment** database backup (PITR timestamp or `.bak` file).
2. Restore the **matching** document storage copy from the same window.
3. Redeploy the previous application package if needed.
4. **Do not** run EF `migration Down()` on live financial data.

---

## 6. Verification checklist

Execute after every restore drill or production recovery. Full detail in `docs/BACKUP_RESTORE.md`.

| # | Check | Expected | Owner |
|---|-------|----------|-------|
| 1 | `GET /health/ready` | `Healthy` | Developer |
| 2 | Login as known tenant user | Success | Developer |
| 3 | Open client, funding contract, invoice (header + lines), credit note, audit | Data matches pre-backup | Developer |
| 4 | Download invoice PDF | File present or regenerated from snapshots | Developer |
| 5 | Cross-tenant invoice access | HTTP 404 (isolation intact) | Developer |
| 6 | Invoice total spot-check | Matches pre-backup value (e.g. finance reference invoice) | Finance |
| 7 | Sage export file present (if applicable) | File exists in restored document root | DevOps |

Record results in a restore drill report (date, backup used, duration, participants, outcome). Schedule quarterly re-drills (minimum: before pilot go-live, P0-4).

---

## 7. Responsibilities

| Role | Responsibility |
|------|----------------|
| **DevOps** | Provision IaC backup settings; enable Azure Files backup; run/monitor daily jobs; execute restores; maintain off-site copies; configure failure alerts |
| **Developer** | Pre-migration backup gate; application smoke verification after restore; update connection strings during drills |
| **Business / Finance** | Approve RPO/RTO and legal retention periods; spot-check invoice totals after restore drills |
| **Legal / Privacy** | Approve backup retention and subprocessor list (Azure Backup storage) |

---

## 8. Monitoring and alerts

### Daily checks (`docs/OPERATIONS_CHECKLIST.md`)

- Run `scripts/Verify-BackupReadiness.ps1` (or equivalent Azure Monitor alert).
- Confirm SQL PITR window spans at least 24 hours.
- Confirm document backup completed within 48 hours.

### Recommended Azure Monitor alerts (manual DevOps setup)

| Alert | Condition |
|-------|-----------|
| SQL backup health | Azure SQL automated backup failure (if available in region/tier) |
| Recovery Services backup job failed | Job status = Failed |
| Recovery Services backup not run | Last backup older than 36 hours |

Route alerts to an operations channel (email, Teams, PagerDuty).

---

## 9. Scripts reference

| Script | Purpose |
|--------|---------|
| `scripts/Backup-CareHome.ps1` | Coordinated pre-migration / on-demand backup |
| `scripts/Enable-AzureFileBackup.ps1` | One-time Azure Files protection registration |
| `scripts/Verify-BackupReadiness.ps1` | Daily backup health check |
| `scripts/Deploy-Azure.ps1 -PreMigrationBackup` | Deploy with mandatory pre-migration backup |

---

## 10. P0-3 completion checklist

- [ ] Azure SQL on Standard S0+ with 35-day PITR configured (verify via `Verify-BackupReadiness.ps1`)
- [ ] Long-term retention enabled (if required by legal/finance)
- [ ] Azure Files backup enabled via `Enable-AzureFileBackup.ps1`
- [ ] At least one successful document backup job recorded
- [ ] Pre-migration backup executed before first production migration
- [ ] RPO/RTO agreed with business and recorded
- [ ] Backup failure alert configured
- [ ] Off-site / geo-redundant copy verified for subscription region
- [ ] P0-4 restore drill scheduled on production-tier SQL (separate task)

---

## References

| Document | Purpose |
|----------|---------|
| `docs/BACKUP_RESTORE.md` | Technical SQL restore commands and LocalDB evidence |
| `docs/PRODUCTION_DEPLOYMENT.md` | Deploy sequence with backup gate |
| `docs/OPERATIONS_CHECKLIST.md` | Daily operational checks |
| `docs/AZURE_HOSTING.md` | Azure deploy overview |
| `infra/azure/main.bicep` | Backup-related IaC parameters |
| `P0_EXECUTION_ROADMAP.md` | P0-3 and P0-4 requirements |

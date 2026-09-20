<#
.SYNOPSIS
  Verifies CarehomeSystem backup readiness (Azure SQL PITR + document backups).

.EXAMPLE
  .\scripts\Verify-BackupReadiness.ps1 -ResourceGroup rg-carehome -SqlServerName sql-carehome-pilot -SqlDatabaseName CareHome -RecoveryVaultName carehomepilotrsv
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,

    [string]$SqlDatabaseName = 'CareHome',

    [string]$RecoveryVaultName = '',

    [string]$DocumentsShareName = 'carehome-documents',

    [int]$MaxPitrAgeHours = 24,

    [int]$MaxDocumentBackupAgeHours = 48
)

$ErrorActionPreference = 'Stop'
$issues = @()

$az = Get-Command az -ErrorAction SilentlyContinue
if (-not $az) {
    throw "Azure CLI (az) not found."
}

Write-Host "Checking Azure SQL backup readiness for $SqlDatabaseName..."

$db = az sql db show `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $SqlDatabaseName `
    --output json | ConvertFrom-Json

$tier = $db.sku.tier
Write-Host "  Tier: $tier / $($db.sku.name)"

if ($tier -eq 'Basic') {
    $issues += 'SQL tier is Basic (7-day PITR only). Upgrade to Standard S0+ for 35-day retention per docs/operations/BACKUP_AND_RECOVERY_RUNBOOK.md.'
}

$restoreWindow = az sql db list-restore-windows `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $SqlDatabaseName `
    --output json | ConvertFrom-Json

$earliest = [datetimeoffset]::Parse($restoreWindow.value[0].earliestRestoreDate)
$latest = [datetimeoffset]::Parse($restoreWindow.value[0].latestRestoreDate)
$pitrSpanHours = ($latest - $earliest).TotalHours
Write-Host "  PITR window: $($earliest.UtcDateTime.ToString('u')) -> $($latest.UtcDateTime.ToString('u')) ($([math]::Round($pitrSpanHours, 1)) h span)"

if ($pitrSpanHours -lt 1) {
    $issues += 'PITR restore window is less than 1 hour — automated backups may not be active yet.'
}

$shortTerm = az sql db str-policy show `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --database-name $SqlDatabaseName `
    --output json 2>$null | ConvertFrom-Json

if ($shortTerm) {
    Write-Host "  Short-term retention: $($shortTerm.retentionDays) days (interval $($shortTerm.backupIntervalInHours) h)"
    if ($tier -ne 'Basic' -and $shortTerm.retentionDays -lt 7) {
        $issues += "Short-term retention is $($shortTerm.retentionDays) days — recommend >= 7 (35 for production)."
    }
}

if ($RecoveryVaultName) {
    Write-Host "Checking Azure Files backup in vault $RecoveryVaultName..."

    $items = az backup item list `
        --resource-group $ResourceGroup `
        --vault-name $RecoveryVaultName `
        --output json 2>$null | ConvertFrom-Json

    $fileItem = $items | Where-Object { $_.properties.friendlyName -eq $DocumentsShareName -or $_.name -match $DocumentsShareName } | Select-Object -First 1

    if (-not $fileItem) {
        $issues += "No protected Azure Files item found for share '$DocumentsShareName'. Run scripts/Enable-AzureFileBackup.ps1."
    }
    else {
        $lastBackup = $fileItem.properties.lastBackupTime
        Write-Host "  Last file share backup: $lastBackup"
        if ($lastBackup) {
            $age = ((Get-Date).ToUniversalTime() - [datetimeoffset]::Parse($lastBackup).UtcDateTime).TotalHours
            if ($age -gt $MaxDocumentBackupAgeHours) {
                $issues += "Document backup is $([math]::Round($age, 1)) hours old (threshold $MaxDocumentBackupAgeHours h)."
            }
        }
        else {
            $issues += 'Document backup has never completed. Wait for first scheduled run or trigger on-demand backup.'
        }
    }
}
else {
    Write-Warning 'RecoveryVaultName not provided — skipping document backup check.'
}

Write-Host ""
if ($issues.Count -eq 0) {
    Write-Host 'PASS: Backup readiness checks passed.'
    exit 0
}

Write-Host 'FAIL: Backup readiness issues found:'
foreach ($issue in $issues) {
    Write-Host "  - $issue"
}
exit 1

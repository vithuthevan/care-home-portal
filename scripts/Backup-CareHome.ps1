<#
.SYNOPSIS
  Creates a coordinated SQL + document storage backup for CarehomeSystem.

.DESCRIPTION
  Azure SQL: records the current PITR window (automated backups are platform-managed).
  On-prem SQL: runs BACKUP DATABASE ... WITH CHECKSUM.
  Documents: Azure Files snapshot or robocopy mirror for on-prem paths.

  Run before every EF migration and on a daily schedule. See docs/operations/BACKUP_AND_RECOVERY_RUNBOOK.md.

.EXAMPLE
  .\scripts\Backup-CareHome.ps1 -Target Azure -ResourceGroup rg-carehome -SqlServerName sql-carehome-pilot -SqlDatabaseName CareHome

.EXAMPLE
  .\scripts\Backup-CareHome.ps1 -Target OnPrem -SqlServerInstance localhost -SqlDatabaseName CareHome -DocumentRoot D:\carehome-documents -BackupRoot D:\backups\carehome
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Azure', 'OnPrem')]
    [string]$Target,

    [string]$ResourceGroup = '',

    [string]$SqlServerName = '',

    [string]$SqlDatabaseName = 'CareHome',

    [string]$StorageAccountName = '',

    [string]$DocumentsShareName = 'carehome-documents',

    [string]$SqlServerInstance = 'localhost',

    [string]$DocumentRoot = '',

    [string]$BackupRoot = '',

    [string]$SqlLogin = '',

    [SecureString]$SqlPassword,

    [ValidateSet('PreMigration', 'Daily', 'Manual')]
    [string]$Purpose = 'Manual'
)

$ErrorActionPreference = 'Stop'
if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

function Require-Az {
    $az = Get-Command az -ErrorAction SilentlyContinue
    if (-not $az) {
        throw "Azure CLI (az) not found. Install: winget install --exact --id Microsoft.AzureCLI"
    }
}

function ConvertFrom-SecureStringPlain([SecureString]$Value) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function New-BackupManifest {
    param(
        [string]$OutputPath,
        [hashtable]$Data
    )
    $payload = [ordered]@{
        createdUtc   = (Get-Date).ToUniversalTime().ToString('o')
        purpose      = $Purpose
        target       = $Target
        database     = $SqlDatabaseName
        entries      = $Data
    }
    $json = $payload | ConvertTo-Json -Depth 6
    $dir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    Set-Content -Path $OutputPath -Value $json -Encoding UTF8
    return $OutputPath
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$manifestEntries = @{}

if ($Target -eq 'Azure') {
    Require-Az
    if ([string]::IsNullOrWhiteSpace($ResourceGroup) -or [string]::IsNullOrWhiteSpace($SqlServerName)) {
        throw 'Azure target requires -ResourceGroup and -SqlServerName.'
    }

    Write-Host "Recording Azure SQL PITR window for $SqlDatabaseName on $SqlServerName..."
    $db = az sql db show `
        --resource-group $ResourceGroup `
        --server $SqlServerName `
        --name $SqlDatabaseName `
        --output json | ConvertFrom-Json

    $restoreWindow = az sql db list-restore-windows `
        --resource-group $ResourceGroup `
        --server $SqlServerName `
        --name $SqlDatabaseName `
        --output json | ConvertFrom-Json

    $earliest = $restoreWindow.value[0].earliestRestoreDate
    $latest = $restoreWindow.value[0].latestRestoreDate
    Write-Host "  PITR window: $earliest -> $latest"
    Write-Host "  SKU: $($db.sku.tier) / $($db.sku.name)"

    $manifestEntries.Sql = @{
        type              = 'AzureSqlPitr'
        server            = $SqlServerName
        database          = $SqlDatabaseName
        skuTier           = $db.sku.tier
        skuName           = $db.sku.name
        earliestRestoreUtc = $earliest
        latestRestoreUtc  = $latest
        note              = 'Platform-managed automated backups. Use az sql db restore for recovery.'
    }

    if (-not $StorageAccountName) {
        $StorageAccountName = (az storage account list `
            --resource-group $ResourceGroup `
            --query "[?ends_with(name, 'docs')].name | [0]" -o tsv).Trim()
    }

    if ($StorageAccountName) {
        Write-Host "Creating Azure Files share snapshot for $DocumentsShareName..."
        $snapshotName = "backup-$Purpose-$timestamp".ToLowerInvariant()
        $snapshot = az storage share snapshot `
            --account-name $StorageAccountName `
            --name $DocumentsShareName `
            --metadata "purpose=$Purpose" "createdUtc=$(Get-Date -Format o)" `
            --output json | ConvertFrom-Json

        Write-Host "  Snapshot: $($snapshot.snapshot)"
        $manifestEntries.Documents = @{
            type           = 'AzureFilesSnapshot'
            storageAccount = $StorageAccountName
            shareName      = $DocumentsShareName
            snapshot       = $snapshot.snapshot
            snapshotTime   = $snapshot.snapshotTime
        }
    }
    else {
        Write-Warning 'Storage account not found. Document snapshot skipped — run Enable-AzureFileBackup.ps1 or pass -StorageAccountName.'
    }

    $manifestPath = New-BackupManifest `
        -OutputPath (Join-Path $env:TEMP "carehome-backup-$timestamp.json") `
        -Data $manifestEntries
}
else {
    if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
        throw 'OnPrem target requires -BackupRoot.'
    }
    if ([string]::IsNullOrWhiteSpace($DocumentRoot)) {
        throw 'OnPrem target requires -DocumentRoot.'
    }

    $sqlBackupDir = Join-Path $BackupRoot 'sql'
    $docBackupDir = Join-Path $BackupRoot "documents-$Purpose-$timestamp"
    New-Item -ItemType Directory -Path $sqlBackupDir -Force | Out-Null

    $bakFile = Join-Path $sqlBackupDir "CareHome_${Purpose}_$timestamp.bak"
    $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        throw 'sqlcmd not found. Install SQL Server command-line tools.'
    }

    Write-Host "Backing up SQL database $SqlDatabaseName to $bakFile..."
    $backupSql = @"
BACKUP DATABASE [$SqlDatabaseName]
TO DISK = N'$($bakFile.Replace("'", "''"))'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;
"@
    if ($SqlLogin -and $SqlPassword) {
        $pwd = ConvertFrom-SecureStringPlain $SqlPassword
        & sqlcmd -S $SqlServerInstance -U $SqlLogin -P $pwd -Q $backupSql -b
    }
    else {
        & sqlcmd -S $SqlServerInstance -E -Q $backupSql -b
    }

    $bakInfo = Get-Item $bakFile
    Write-Host "  Backup size: $($bakInfo.Length) bytes"
    $manifestEntries.Sql = @{
        type     = 'SqlServerBak'
        instance = $SqlServerInstance
        database = $SqlDatabaseName
        file     = $bakFile
        sizeBytes = $bakInfo.Length
    }

    Write-Host "Mirroring document storage $DocumentRoot -> $docBackupDir..."
    robocopy $DocumentRoot $docBackupDir /MIR /NFL /NDL /NJH /NJS /NC /NS | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed with exit code $LASTEXITCODE"
    }

    $manifestEntries.Documents = @{
        type   = 'RobocopyMirror'
        source = $DocumentRoot
        target = $docBackupDir
    }

    $manifestPath = New-BackupManifest `
        -OutputPath (Join-Path $BackupRoot "manifest-$Purpose-$timestamp.json") `
        -Data $manifestEntries
}

Write-Host ""
Write-Host "Backup complete ($Purpose)."
Write-Host "  Manifest: $manifestPath"
Write-Host "Next: copy manifest and off-site copies per docs/operations/BACKUP_AND_RECOVERY_RUNBOOK.md"

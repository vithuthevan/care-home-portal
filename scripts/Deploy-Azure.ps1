<#
.SYNOPSIS
  Provisions Azure resources (Bicep), applies EF migrations, deploys the published API+SPA,
  stores secrets in Azure Key Vault, and configures App Service Key Vault references.

.NOTES
  Requires: Azure CLI (az), .NET 10 SDK, Node.js, logged-in az account with a subscription.
  Secrets are written to Key Vault and referenced from App Service — never committed to git.
  Plain-text secrets are shown once in the console for operator backup; save them to a password manager.

.EXAMPLE
  .\scripts\Deploy-Azure.ps1 -ResourceGroup rg-carehome -Location uksouth -AppName carehome-pilot
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [string]$Location = 'uksouth',

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9-]{3,40}$')]
    [string]$AppName,

    [string]$SqlServerName = '',

    [string]$SqlAdminLogin = 'carehomeadmin',

    [SecureString]$SqlAdminPassword,

    [SecureString]$JwtKey,

    [string]$SeedAdminEmail = '',

    [SecureString]$SeedAdminPassword,

    [string]$SqlDatabaseName = 'CareHome',

    [string]$PublishDir = '',

    [switch]$SkipProvision,

    [switch]$SkipMigrate,

    [switch]$SkipDeploy,

    [switch]$SkipBootstrap,

    [switch]$UsePlainAppSettings,

    [ValidateSet('', 'Smtp')]
    [string]$EmailMode = '',

    [string]$SmtpHost = '',

    [string]$SmtpUser = '',

    [string]$SmtpFromAddress = '',

    [string]$SmtpFromName = 'Care Home Billing',

    [int]$SmtpPort = 587,

    [SecureString]$SmtpPassword,

    [switch]$AllowSimulatedEmail,

    [switch]$PreMigrationBackup,

    [string]$SqlDatabaseSku = 'S0',

    [string]$SqlDatabaseSkuTier = 'Standard',

    [int]$SqlBackupRetentionDays = 35
)

$ErrorActionPreference = 'Stop'
if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$bicep = Join-Path (Join-Path (Join-Path $repoRoot 'infra') 'azure') 'main.bicep'

# Key Vault secret names (alphanumeric and hyphen only). Map to app settings via Deploy-Azure.ps1.
$SecretNames = @{
    ConnectionString = 'ConnectionStrings-DefaultConnection'
    JwtKey           = 'Jwt-Key'
    SeedPassword     = 'Seed-AdminPassword'
    SmtpPassword     = 'Email-Smtp-Password'
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

function New-StrongSecret([int]$Length = 40) {
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*-_=+'
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] $Length
    $rng.GetBytes($bytes)
    -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
}

function Get-DeployerObjectId {
    $id = az ad signed-in-user show --query id -o tsv 2>$null
    if (-not $id) {
        Write-Warning "Could not resolve signed-in user object ID. Key Vault secret writes may fail unless you have Key Vault Secrets Officer via another role."
        return ''
    }
    return $id.Trim()
}

function Set-KeyVaultSecret {
    param(
        [string]$VaultName,
        [string]$SecretName,
        [string]$Value
    )
    $deadline = (Get-Date).AddMinutes(3)
    do {
        try {
            az keyvault secret set `
                --vault-name $VaultName `
                --name $SecretName `
                --value $Value `
                --output none
            return
        }
        catch {
            if ((Get-Date) -ge $deadline) {
                throw
            }
            Write-Host "Key Vault write not ready yet (RBAC propagation); retrying in 15s..."
            Start-Sleep -Seconds 15
        }
    } while ($true)
}

function New-KeyVaultAppSettingReference {
    param(
        [string]$VaultUri,
        [string]$SecretName
    )
    $uri = "$($VaultUri.TrimEnd('/'))/secrets/$SecretName/"
    return "@Microsoft.KeyVault(SecretUri=$uri)"
}

function Get-EmailAppSettings {
    param(
        [string]$VaultUri,
        [bool]$UsePlain,
        [string]$SmtpPasswordPlain
    )

    $emailSettings = @()

    if ($AllowSimulatedEmail) {
        $emailSettings += 'Email__AllowSimulationInProduction=true'
        Write-Warning 'AllowSimulatedEmail: API will start but email sends fail visibly until SMTP is configured (manual-send workflow only).'
        return $emailSettings
    }

    if ($EmailMode -ne 'Smtp') {
        Write-Warning 'Email not configured. Production API will refuse to start until Email__Mode=Smtp is set (see docs/operations/PRODUCTION_EMAIL_SETUP.md).'
        return $emailSettings
    }

    if ([string]::IsNullOrWhiteSpace($SmtpHost) -or [string]::IsNullOrWhiteSpace($SmtpFromAddress)) {
        throw 'EmailMode Smtp requires -SmtpHost and -SmtpFromAddress.'
    }

    $emailSettings += 'Email__Mode=Smtp'
    $emailSettings += "Email__Smtp__Host=$SmtpHost"
    $emailSettings += "Email__Smtp__Port=$SmtpPort"
    $emailSettings += "Email__FromAddress=$SmtpFromAddress"
    $emailSettings += "Email__FromName=$SmtpFromName"
    $emailSettings += 'Email__Smtp__EnableSsl=true'

    if (-not [string]::IsNullOrWhiteSpace($SmtpUser)) {
        $emailSettings += "Email__Smtp__User=$SmtpUser"
        if ([string]::IsNullOrWhiteSpace($SmtpPasswordPlain)) {
            throw 'SmtpUser is set but -SmtpPassword was not provided.'
        }
        if ($UsePlain) {
            $emailSettings += "Email__Smtp__Password=$SmtpPasswordPlain"
        }
        else {
            $emailSettings += "Email__Smtp__Password=$(New-KeyVaultAppSettingReference -VaultUri $VaultUri -SecretName $SecretNames.SmtpPassword)"
        }
    }

    return $emailSettings
}

function Write-SecretsInventory {
    param(
        [string]$OutputPath,
        [hashtable]$Entries
    )
    $lines = @(
        '# CarehomeSystem production secrets inventory (generated — DO NOT COMMIT)',
        "# Created: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')",
        '# Store this file in a password manager or secure ops vault, then delete the local copy.',
        ''
    )
    foreach ($key in ($Entries.Keys | Sort-Object)) {
        $entry = $Entries[$key]
        $lines += "## $($entry.Purpose)"
        $lines += "Key Vault secret: $($entry.SecretName)"
        $lines += "App setting: $($entry.AppSetting)"
        $lines += "Rotation: $($entry.Rotation)"
        $lines += ''
    }
    $dir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir | Out-Null
    }
    Set-Content -Path $OutputPath -Value $lines -Encoding UTF8
    Write-Host "Secrets inventory written to $OutputPath (gitignored — move to password manager)"
}

Require-Az

$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in. Opening az login (device code)..."
    az login --use-device-code | Out-Null
    $account = az account show | ConvertFrom-Json
}
Write-Host "Subscription: $($account.name) ($($account.id))"

if (-not $SqlServerName) {
    $SqlServerName = ("sql-$AppName").ToLowerInvariant()
}

if (-not $SqlAdminPassword) {
    $plain = New-StrongSecret 28
    $plain = 'Aa1!' + $plain
    $SqlAdminPassword = ConvertTo-SecureString $plain -AsPlainText -Force
    Write-Host "Generated SQL admin password (store securely; shown once): $plain"
}

if (-not $JwtKey) {
    $plainJwt = New-StrongSecret 48
    $JwtKey = ConvertTo-SecureString $plainJwt -AsPlainText -Force
    Write-Host "Generated Jwt__Key (store securely; shown once): $plainJwt"
}

$sqlPasswordPlain = ConvertFrom-SecureStringPlain $SqlAdminPassword
$jwtPlain = ConvertFrom-SecureStringPlain $JwtKey
$keyVaultName = ''
$keyVaultUri = ''
$recoveryVaultName = ''
$storageAccountName = ''

if (-not $SkipProvision) {
    Write-Host "Ensuring resource group $ResourceGroup in $Location..."
    az group create --name $ResourceGroup --location $Location | Out-Null

    $deployerObjectId = Get-DeployerObjectId
    if ($deployerObjectId) {
        Write-Host "Deployer object ID: $deployerObjectId (Key Vault Secrets Officer)"
    }

    Write-Host "Deploying Bicep infrastructure (App Service, SQL, Key Vault, backup policies)..."
    $deployment = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file $bicep `
        --parameters `
            appName=$AppName `
            sqlServerName=$SqlServerName `
            sqlAdminLogin=$SqlAdminLogin `
            sqlAdminPassword=$sqlPasswordPlain `
            sqlDatabaseName=$SqlDatabaseName `
            sqlDatabaseSku=$SqlDatabaseSku `
            sqlDatabaseSkuTier=$SqlDatabaseSkuTier `
            sqlBackupRetentionDays=$SqlBackupRetentionDays `
            location=$Location `
            deployerObjectId=$deployerObjectId `
        --query properties.outputs `
        --output json | ConvertFrom-Json

    $webAppUrl = $deployment.webAppUrl.value
    $sqlFqdn = $deployment.sqlServerFqdn.value
    $keyVaultName = $deployment.keyVaultName.value
    $keyVaultUri = $deployment.keyVaultUri.value
    $recoveryVaultName = $deployment.recoveryVaultName.value
    $storageAccountName = $deployment.storageAccountName.value
    Write-Host "Web app: $webAppUrl"
    Write-Host "SQL: $sqlFqdn (tier $SqlDatabaseSkuTier / $SqlDatabaseSku, PITR $SqlBackupRetentionDays days)"
    Write-Host "Key Vault: $keyVaultName ($keyVaultUri)"
    if ($recoveryVaultName) {
        Write-Host "Recovery vault: $recoveryVaultName (enable file backup: scripts/Enable-AzureFileBackup.ps1)"
    }
}
else {
    $webAppUrl = "https://$AppName.azurewebsites.net"
    $sqlFqdn = "$SqlServerName.database.windows.net"
    $keyVaultName = (az keyvault list --resource-group $ResourceGroup --query "[?starts_with(name, '$($AppName -replace '-',''))].name | [0]" -o tsv).Trim()
    if (-not $keyVaultName) {
        $keyVaultName = (az keyvault list --resource-group $ResourceGroup --query "[0].name" -o tsv).Trim()
    }
    if ($keyVaultName) {
        $keyVaultUri = (az keyvault show --name $keyVaultName --query properties.vaultUri -o tsv).Trim()
        Write-Host "Key Vault: $keyVaultName ($keyVaultUri)"
    }
    elseif (-not $UsePlainAppSettings) {
        throw "Key Vault not found in resource group $ResourceGroup. Re-run without -SkipProvision or pass -UsePlainAppSettings for legacy plain app settings."
    }

    $recoveryVaultName = (az backup vault list --resource-group $ResourceGroup --query "[0].name" -o tsv 2>$null)
    if ($recoveryVaultName) { $recoveryVaultName = $recoveryVaultName.Trim() }
    $storageAccountName = (az storage account list --resource-group $ResourceGroup --query "[?ends_with(name, 'docs')].name | [0]" -o tsv 2>$null)
    if ($storageAccountName) { $storageAccountName = $storageAccountName.Trim() }
}

$connectionString = "Server=tcp:$sqlFqdn,1433;Database=$SqlDatabaseName;User Id=$SqlAdminLogin;Password=$sqlPasswordPlain;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True"

Write-Host "Adding client IP to SQL firewall for migrations..."
$clientIp = (Invoke-RestMethod -Uri 'https://api.ipify.org').Trim()
$ruleName = "DeployClient-$(Get-Date -Format 'yyyyMMddHHmmss')"
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $SqlServerName `
    --name $ruleName `
    --start-ip-address $clientIp `
    --end-ip-address $clientIp | Out-Null

if (-not $SkipMigrate) {
    if ($PreMigrationBackup) {
        Write-Host "Pre-migration backup (mandatory gate)..."
        & (Join-Path $PSScriptRoot 'Backup-CareHome.ps1') `
            -Target Azure `
            -ResourceGroup $ResourceGroup `
            -SqlServerName $SqlServerName `
            -SqlDatabaseName $SqlDatabaseName `
            -StorageAccountName $storageAccountName `
            -Purpose PreMigration
    }
    else {
        Write-Warning 'PreMigrationBackup not set. Take a backup before migrations (scripts/Backup-CareHome.ps1 or -PreMigrationBackup). See docs/operations/BACKUP_AND_RECOVERY_RUNBOOK.md.'
    }

    Write-Host "Applying EF migrations to Azure SQL..."
    $apiDir = Join-Path (Join-Path $repoRoot 'backend') 'CareHome.Api'
    Push-Location $apiDir
    try {
        dotnet ef database update --connection $connectionString
    }
    finally {
        Pop-Location
    }
}

if (-not $PublishDir) {
    Write-Host "Building publish package (SPA + API)..."
    & (Join-Path $PSScriptRoot 'Publish-CareHome.ps1')
    $PublishDir = Join-Path (Join-Path $repoRoot 'artifacts') 'carehome-api'
}

$seedPasswordPlain = $null
if (-not $SkipBootstrap) {
    if (-not $SeedAdminEmail) {
        $SeedAdminEmail = "platform.admin@$AppName.local"
        Write-Warning "Using bootstrap email $SeedAdminEmail — change after first login if needed."
    }
    if (-not $SeedAdminPassword) {
        $seedPlain = 'Ch!' + (New-StrongSecret 16)
        $SeedAdminPassword = ConvertTo-SecureString $seedPlain -AsPlainText -Force
        Write-Host "Generated Seed__AdminPassword (store securely; shown once): $seedPlain"
    }
    $seedPasswordPlain = ConvertFrom-SecureStringPlain $SeedAdminPassword
}

$smtpPasswordPlain = $null
if ($SmtpPassword) {
    $smtpPasswordPlain = ConvertFrom-SecureStringPlain $SmtpPassword
}

if (-not $SkipDeploy) {
    if ($UsePlainAppSettings) {
        Write-Warning "UsePlainAppSettings: storing secrets as plain App Service settings (not recommended for production)."
        $settings = @(
            "ASPNETCORE_ENVIRONMENT=Production"
            "ConnectionStrings__DefaultConnection=$connectionString"
            "Jwt__Key=$jwtPlain"
            "DocumentStorage__RootPath=/home/carehome-documents"
            "Https__Redirect=true"
        )
        $settings += Get-EmailAppSettings -VaultUri $keyVaultUri -UsePlain $true -SmtpPasswordPlain $smtpPasswordPlain
        if (-not $SkipBootstrap) {
            $settings += "Seed__AdminEmail=$SeedAdminEmail"
            $settings += "Seed__AdminPassword=$seedPasswordPlain"
        }
    }
    else {
        Write-Host "Writing secrets to Key Vault..."
        Set-KeyVaultSecret -VaultName $keyVaultName -SecretName $SecretNames.ConnectionString -Value $connectionString
        Set-KeyVaultSecret -VaultName $keyVaultName -SecretName $SecretNames.JwtKey -Value $jwtPlain
        if (-not $SkipBootstrap) {
            Set-KeyVaultSecret -VaultName $keyVaultName -SecretName $SecretNames.SeedPassword -Value $seedPasswordPlain
        }
        if ($EmailMode -eq 'Smtp' -and -not [string]::IsNullOrWhiteSpace($smtpPasswordPlain)) {
            Set-KeyVaultSecret -VaultName $keyVaultName -SecretName $SecretNames.SmtpPassword -Value $smtpPasswordPlain
        }

        Write-Host "Configuring App Service Key Vault references..."
        $settings = @(
            "ASPNETCORE_ENVIRONMENT=Production"
            "ConnectionStrings__DefaultConnection=$(New-KeyVaultAppSettingReference -VaultUri $keyVaultUri -SecretName $SecretNames.ConnectionString)"
            "Jwt__Key=$(New-KeyVaultAppSettingReference -VaultUri $keyVaultUri -SecretName $SecretNames.JwtKey)"
            "DocumentStorage__RootPath=/home/carehome-documents"
            "Https__Redirect=true"
        )
        $settings += Get-EmailAppSettings -VaultUri $keyVaultUri -UsePlain $false -SmtpPasswordPlain $smtpPasswordPlain
        if (-not $SkipBootstrap) {
            $settings += "Seed__AdminEmail=$SeedAdminEmail"
            $settings += "Seed__AdminPassword=$(New-KeyVaultAppSettingReference -VaultUri $keyVaultUri -SecretName $SecretNames.SeedPassword)"
        }

        $inventoryPath = Join-Path (Join-Path $repoRoot 'secrets-inventory') "$AppName-$(Get-Date -Format 'yyyyMMdd').md"
        $inventory = @{
            SqlConnection = @{
                Purpose    = 'Azure SQL connection string (runtime)'
                SecretName = $SecretNames.ConnectionString
                AppSetting = 'ConnectionStrings__DefaultConnection'
                Rotation   = 'On SQL password rotation (recommend 90 days)'
            }
            JwtKey = @{
                Purpose    = 'JWT HMAC signing key'
                SecretName = $SecretNames.JwtKey
                AppSetting = 'Jwt__Key'
                Rotation   = 'On compromise or annually (invalidates active tokens)'
            }
        }
        if (-not $SkipBootstrap) {
            $inventory.SeedPassword = @{
                Purpose    = 'PlatformAdmin bootstrap password (remove after first login)'
                SecretName = $SecretNames.SeedPassword
                AppSetting = 'Seed__AdminPassword'
                Rotation   = 'One-time; delete from Key Vault after bootstrap'
            }
        }
        if ($EmailMode -eq 'Smtp' -and -not [string]::IsNullOrWhiteSpace($smtpPasswordPlain)) {
            $inventory.SmtpPassword = @{
                Purpose    = 'SMTP authentication password'
                SecretName = $SecretNames.SmtpPassword
                AppSetting = 'Email__Smtp__Password'
                Rotation   = 'Per mailbox provider policy'
            }
        }
        Write-SecretsInventory -OutputPath $inventoryPath -Entries $inventory
    }

    az webapp config appsettings set `
        --resource-group $ResourceGroup `
        --name $AppName `
        --settings @settings | Out-Null

    Write-Host "Deploying package to App Service..."
    $artifactsDir = Join-Path $repoRoot 'artifacts'
    if (-not (Test-Path $artifactsDir)) {
        New-Item -ItemType Directory -Path $artifactsDir | Out-Null
    }
    $zipPath = Join-Path $artifactsDir 'carehome-api.zip'
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $PublishDir '*') -DestinationPath $zipPath -Force

    az webapp deploy `
        --resource-group $ResourceGroup `
        --name $AppName `
        --src-path $zipPath `
        --type zip `
        --async false | Out-Null

    Write-Host "Restarting web app..."
    az webapp restart --resource-group $ResourceGroup --name $AppName | Out-Null
}

Write-Host "Waiting for health endpoints..."
$liveUrl = "$webAppUrl/health/live"
$readyUrl = "$webAppUrl/health/ready"
$deadline = (Get-Date).AddMinutes(5)
$readyOk = $false
do {
    Start-Sleep -Seconds 8
    try {
        $live = Invoke-RestMethod -Uri $liveUrl -TimeoutSec 30
        $ready = Invoke-RestMethod -Uri $readyUrl -TimeoutSec 30
        Write-Host "live=$($live.status) ready=$($ready.status)"
        if ($ready.status -eq 'Healthy') {
            $readyOk = $true
            break
        }
    }
    catch {
        Write-Host "Health not ready yet: $($_.Exception.Message)"
    }
} while ((Get-Date) -lt $deadline)

if (-not $readyOk) {
    throw "Timed out waiting for /health/ready. Check App Service logs, Key Vault access, and SQL firewall."
}

if (-not $SkipBootstrap -and -not $SkipDeploy) {
    Write-Host "Removing bootstrap Seed__* app settings (admin already created on first start)..."
    az webapp config appsettings delete `
        --resource-group $ResourceGroup `
        --name $AppName `
        --setting-names Seed__AdminEmail Seed__AdminPassword | Out-Null

    if (-not $UsePlainAppSettings -and $keyVaultName) {
        Write-Host "Removing bootstrap secret from Key Vault..."
        az keyvault secret delete `
            --vault-name $keyVaultName `
            --name $SecretNames.SeedPassword `
            --output none 2>$null
    }
}

Write-Host ""
Write-Host "Deploy complete."
Write-Host "  URL:       $webAppUrl"
Write-Host "  Live:      $liveUrl"
Write-Host "  Ready:     $readyUrl"
if ($keyVaultName) {
    Write-Host "  Key Vault: $keyVaultName"
}
if (-not $SkipBootstrap) {
    Write-Host "  Login:     $SeedAdminEmail (password shown above if generated)"
}
Write-Host "Next: create a tenant via Organisations, then run docs/PRODUCTION_SMOKE_TEST.md"
Write-Host "Secrets documentation: docs/operations/PRODUCTION_SECRETS_SETUP.md"
Write-Host "Email setup: docs/operations/PRODUCTION_EMAIL_SETUP.md"
Write-Host "Backup runbook: docs/operations/BACKUP_AND_RECOVERY_RUNBOOK.md"
if ($recoveryVaultName -and $storageAccountName) {
    Write-Host "Enable document backup: .\scripts\Enable-AzureFileBackup.ps1 -ResourceGroup $ResourceGroup -RecoveryVaultName $recoveryVaultName -StorageAccountName $storageAccountName"
}

<#
.SYNOPSIS
  Provisions Azure resources (Bicep), applies EF migrations, deploys the published API+SPA,
  configures Production app settings, and bootstraps PlatformAdmin once.

.NOTES
  Requires: Azure CLI (az), .NET 10 SDK, Node.js, logged-in az account with a subscription.
  Secrets are prompted or passed as parameters — never committed.

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

    [switch]$SkipBootstrap
)

$ErrorActionPreference = 'Stop'
if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$bicep = Join-Path (Join-Path (Join-Path $repoRoot 'infra') 'azure') 'main.bicep'

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
    # Azure SQL requires complexity: mix of categories
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

if (-not $SkipProvision) {
    Write-Host "Ensuring resource group $ResourceGroup in $Location..."
    az group create --name $ResourceGroup --location $Location | Out-Null

    Write-Host "Deploying Bicep infrastructure..."
    $deployment = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file $bicep `
        --parameters `
            appName=$AppName `
            sqlServerName=$SqlServerName `
            sqlAdminLogin=$SqlAdminLogin `
            sqlAdminPassword=$sqlPasswordPlain `
            sqlDatabaseName=$SqlDatabaseName `
            location=$Location `
        --query properties.outputs `
        --output json | ConvertFrom-Json

    $webAppUrl = $deployment.webAppUrl.value
    $sqlFqdn = $deployment.sqlServerFqdn.value
    Write-Host "Web app: $webAppUrl"
    Write-Host "SQL: $sqlFqdn"
}
else {
    $webAppUrl = "https://$AppName.azurewebsites.net"
    $sqlFqdn = "$SqlServerName.database.windows.net"
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

if (-not $SkipDeploy) {
    Write-Host "Configuring App Service settings..."
    $settings = @(
        "ASPNETCORE_ENVIRONMENT=Production"
        "ConnectionStrings__DefaultConnection=$connectionString"
        "Jwt__Key=$jwtPlain"
        "DocumentStorage__RootPath=/home/carehome-documents"
        "Email__Mode=Development"
        "Https__Redirect=true"
    )

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
        $settings += "Seed__AdminEmail=$SeedAdminEmail"
        $settings += "Seed__AdminPassword=$seedPasswordPlain"
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
    throw "Timed out waiting for /health/ready. Check App Service logs and SQL firewall."
}

if (-not $SkipBootstrap -and -not $SkipDeploy) {
    Write-Host "Removing bootstrap Seed__* app settings (admin already created on first start)..."
    az webapp config appsettings delete `
        --resource-group $ResourceGroup `
        --name $AppName `
        --setting-names Seed__AdminEmail Seed__AdminPassword | Out-Null
}

Write-Host ""
Write-Host "Deploy complete."
Write-Host "  URL:   $webAppUrl"
Write-Host "  Live:  $liveUrl"
Write-Host "  Ready: $readyUrl"
if (-not $SkipBootstrap) {
    Write-Host "  Login: $SeedAdminEmail (password shown above if generated)"
}
Write-Host "Next: create a tenant via Organisations, then run docs/PRODUCTION_SMOKE_TEST.md"

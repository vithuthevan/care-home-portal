<#
.SYNOPSIS
  Post-deploy health check for CarehomeSystem on Azure App Service.

.EXAMPLE
  .\scripts\Verify-AzureDeploy.ps1 -BaseUrl https://carehome-pilot.azurewebsites.net
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl
)

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')

Write-Host "Checking $BaseUrl/health/live ..."
$live = Invoke-RestMethod -Uri "$BaseUrl/health/live" -TimeoutSec 60
if ($live.status -ne 'Healthy') {
    throw "live status was $($live.status)"
}
Write-Host "live=Healthy"

Write-Host "Checking $BaseUrl/health/ready ..."
$ready = Invoke-RestMethod -Uri "$BaseUrl/health/ready" -TimeoutSec 60
if ($ready.status -ne 'Healthy') {
    throw "ready status was $($ready.status) — check SQL connection string and firewall"
}
Write-Host "ready=Healthy"

Write-Host "Checking SPA index ..."
$index = Invoke-WebRequest -Uri $BaseUrl -TimeoutSec 60
if ($index.StatusCode -ne 200) {
    throw "SPA root returned $($index.StatusCode)"
}
if ($index.Content -notmatch 'html') {
    throw "SPA root did not look like HTML"
}
Write-Host "SPA root OK"

Write-Host ""
Write-Host "Verify complete. Sign in as PlatformAdmin, create a test tenant, then run docs/PRODUCTION_SMOKE_TEST.md"

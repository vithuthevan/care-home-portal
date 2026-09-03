<#
.SYNOPSIS
  Builds Angular SPA into API wwwroot and publishes CareHome.Api for Azure App Service.

.EXAMPLE
  .\scripts\Publish-CareHome.ps1
  .\scripts\Publish-CareHome.ps1 -OutputDir C:\apps\carehome-api
#>
[CmdletBinding()]
param(
    [string]$OutputDir = '',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
if (-not $PSScriptRoot) {
    $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
if (-not $OutputDir) {
    $OutputDir = Join-Path (Join-Path $repoRoot 'artifacts') 'carehome-api'
}
$frontend = Join-Path (Join-Path $repoRoot 'frontend') 'care-home-web'
$api = Join-Path (Join-Path $repoRoot 'backend') 'CareHome.Api'
$wwwroot = Join-Path $api 'wwwroot'
$browserDist = Join-Path (Join-Path (Join-Path $frontend 'dist') 'care-home-web') 'browser'

Write-Host "Building Angular SPA..."
Push-Location $frontend
try {
    if (-not (Test-Path (Join-Path $frontend 'node_modules'))) {
        npm ci
        if ($LASTEXITCODE -ne 0) { throw "npm ci failed with exit code $LASTEXITCODE" }
    }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

if (-not (Test-Path $browserDist)) {
    throw "Angular build output not found at $browserDist"
}

Write-Host "Copying SPA into API wwwroot..."
if (Test-Path $wwwroot) {
    Get-ChildItem -Path $wwwroot -Force |
        Where-Object { $_.Name -ne '.gitignore' } |
        Remove-Item -Recurse -Force
}
else {
    New-Item -ItemType Directory -Path $wwwroot | Out-Null
}

Copy-Item -Path (Join-Path $browserDist '*') -Destination $wwwroot -Recurse -Force

Write-Host "Publishing API ($Configuration) -> $OutputDir"
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

dotnet publish (Join-Path $api 'CareHome.Api.csproj') -c $Configuration -o $OutputDir

Write-Host "Publish complete: $OutputDir"
Write-Host "Next: .\scripts\Deploy-Azure.ps1 (or az webapp deploy)"

<#
.SYNOPSIS
  Captures pre-restore baseline counts or validates a restored drill database (read-only).

.DESCRIPTION
  P0-4 restore drill preparation helper. This script NEVER restores, deletes, or modifies databases.
  - Baseline mode: read-only SELECT counts from production (or any database) for later comparison.
  - Validate mode: compare restored database counts to baseline; optional health/login/PDF checks.

.EXAMPLE
  # Capture baseline before drill (Azure SQL)
  .\scripts\Verify-RestoreDrill.ps1 -Mode Baseline `
    -ResourceGroup rg-carehome -SqlServerName sql-carehome-pilot -SqlDatabaseName CareHome `
    -BaselineOutputPath .\artifacts\restore-drill-baseline.json

.EXAMPLE
  # Validate after restore (point at CareHomeRestoreCheck)
  .\scripts\Verify-RestoreDrill.ps1 -Mode Validate `
    -ResourceGroup rg-carehome -SqlServerName sql-carehome-pilot -SqlDatabaseName CareHomeRestoreCheck `
    -BaselineInputPath .\artifacts\restore-drill-baseline.json `
    -ApiBaseUrl https://carehome-drill.example.com `
    -TestUserEmail tenant.admin@example.org -TestUserPassword (Read-Host -AsSecureString) `
    -DocumentRoot D:\backups\carehome-documents-restore -SampleInvoiceId 1 `
    -ReportOutputPath .\artifacts\restore-drill-report.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Baseline', 'Validate')]
    [string]$Mode,

    [string]$ResourceGroup = '',

    [string]$SqlServerName = '',

    [string]$SqlServerInstance = '',

    [string]$SqlDatabaseName = 'CareHome',

    [string]$SqlLogin = '',

    [SecureString]$SqlPassword,

    [bool]$UseIntegratedSecurity,

    [string]$BaselineOutputPath = '',

    [string]$BaselineInputPath = '',

    [string]$ApiBaseUrl = '',

    [string]$TestUserEmail = '',

    [SecureString]$TestUserPassword,

    [string]$DocumentRoot = '',

    [int]$SampleInvoiceId = 0,

    [string]$ReportOutputPath = ''
)

$ErrorActionPreference = 'Stop'

$requiredRoles = @(
    'PlatformAdmin',
    'TenantAdmin',
    'Administrator',
    'LocationManager',
    'ReadOnly'
)

$results = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param(
        [string]$Id,
        [string]$Name,
        [bool]$Passed,
        [string]$Detail
    )
    $results.Add([pscustomobject]@{
            Id     = $Id
            Name   = $Name
            Result = $(if ($Passed) { 'PASS' } else { 'FAIL' })
            Detail = $Detail
        })
}

function Add-Skip {
    param(
        [string]$Id,
        [string]$Name,
        [string]$Detail
    )
    $results.Add([pscustomobject]@{
            Id     = $Id
            Name   = $Name
            Result = 'SKIP'
            Detail = $Detail
        })
}

function ConvertFrom-SecureStringPlain([SecureString]$Value) {
    if (-not $Value) { return '' }
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Resolve-SqlServerTarget {
    if ($SqlServerName) {
        if ($ResourceGroup) {
            $fqdn = az sql server show --resource-group $ResourceGroup --name $SqlServerName --query fullyQualifiedDomainName -o tsv 2>$null
            if ($fqdn) { return $fqdn }
        }
        return $SqlServerName
    }
    if ($SqlServerInstance) { return $SqlServerInstance }
    throw 'Specify -SqlServerName (Azure) or -SqlServerInstance (on-prem).'
}

function Invoke-SqlScalar {
    param(
        [string]$Database,
        [string]$Query
    )

    $server = Resolve-SqlServerTarget
    $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        throw 'sqlcmd not found. Install SQL Server command-line tools.'
    }

    $args = @('-S', $server, '-d', $Database, '-Q', $Query, '-h', '-1', '-W')
    if ($UseIntegratedSecurity) {
        $args += '-E'
    }
    elseif ($SqlLogin) {
        $args += @('-U', $SqlLogin, '-P', (ConvertFrom-SecureStringPlain $SqlPassword))
    }
    else {
        $args += '-E'
    }

    $out = & sqlcmd @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed: $out"
    }
    return ($out | Where-Object { $_ -and $_.Trim() } | Select-Object -First 1)
}

function Get-EntityCounts {
    $db = $SqlDatabaseName
    $counts = [ordered]@{
        capturedUtc          = (Get-Date).ToUniversalTime().ToString('o')
        database             = $db
        server               = (Resolve-SqlServerTarget)
        users                = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM AspNetUsers;')
        roles                = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM AspNetRoles;')
        userRoleAssignments  = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM AspNetUserRoles;')
        tenants              = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM Tenants;')
        companies            = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM Companies;')
        careHomes            = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM CareHomes;')
        clients              = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM Clients;')
        contracts            = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM ClientFundingContracts;')
        invoices             = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM Invoices;')
        invoiceLines         = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM InvoiceLines;')
        invoicesPaid         = [int](Invoke-SqlScalar $db "SELECT COUNT(*) FROM Invoices WHERE PaymentStatus = 'Paid';")
        invoicesNotPaid      = [int](Invoke-SqlScalar $db "SELECT COUNT(*) FROM Invoices WHERE PaymentStatus = 'NotPaid';")
        creditNotes          = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM CreditNotes;')
        auditLogs            = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM AuditLogs;')
        sageExportBatches    = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM SageExportBatches;')
        invoicesWithPdfPath  = [int](Invoke-SqlScalar $db 'SELECT COUNT(*) FROM Invoices WHERE PdfPath IS NOT NULL AND PdfPath <> '''';')
    }

    foreach ($role in $requiredRoles) {
        $safeRole = $role.Replace("'", "''")
        $counts["role_$role"] = [int](Invoke-SqlScalar $db "SELECT COUNT(*) FROM AspNetRoles WHERE Name = N'$safeRole';")
    }

    return $counts
}

function Test-RoleChecks {
    param($Counts)
    $allOk = $true
    foreach ($role in $requiredRoles) {
        $key = "role_$role"
        $ok = $Counts[$key] -ge 1
        if (-not $ok) { $allOk = $false }
        Add-Check "ROLE-$role" "Role exists: $role" $ok "count=$($Counts[$key])"
    }
    return $allOk
}

function Compare-Counts {
    param($Baseline, $Current)

    $keys = @(
        @{ Key = 'users'; Name = 'Users exist' },
        @{ Key = 'roles'; Name = 'Roles exist' },
        @{ Key = 'companies'; Name = 'Companies exist' },
        @{ Key = 'clients'; Name = 'Residents (clients) exist' },
        @{ Key = 'contracts'; Name = 'Funding contracts exist' },
        @{ Key = 'invoices'; Name = 'Invoices exist' },
        @{ Key = 'invoiceLines'; Name = 'Invoice lines exist' },
        @{ Key = 'invoicesPaid'; Name = 'Paid invoices exist' },
        @{ Key = 'creditNotes'; Name = 'Credit notes exist' },
        @{ Key = 'auditLogs'; Name = 'Audit log rows exist' }
    )

    $allOk = $true
    foreach ($item in $keys) {
        $expected = [int]$Baseline[$item.Key]
        $actual = [int]$Current[$item.Key]
        $ok = $actual -eq $expected
        if (-not $ok) { $allOk = $false }
        Add-Check $item.Key $item.Name $ok "baseline=$expected actual=$actual"
    }
    return $allOk
}

function Invoke-ApiJson {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body = $null,
        [string]$Token = $null
    )

    $url = "$($ApiBaseUrl.TrimEnd('/'))$Path"
    $headers = @{ 'Content-Type' = 'application/json; charset=utf-8' }
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }

    $params = @{
        Uri         = $url
        Method      = $Method
        Headers     = $headers
        ErrorAction = 'Stop'
    }
    if ($Body) { $params['Body'] = $Body }

    try {
        $response = Invoke-WebRequest @params -UseBasicParsing
        $json = $null
        if ($response.Content) {
            try { $json = $response.Content | ConvertFrom-Json } catch { }
        }
        return @{ Status = [int]$response.StatusCode; Json = $json; Content = $response.Content }
    }
    catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
            return @{ Status = $status; Json = $null; Content = $_.Exception.Message }
        }
        throw
    }
}

function Test-ApiChecks {
    param($Counts)

    if (-not $ApiBaseUrl) {
        Add-Skip 'API-HEALTH' 'Application health (/health/ready)' 'ApiBaseUrl not provided'
        Add-Skip 'API-LOGIN' 'Login as known tenant user' 'ApiBaseUrl not provided'
        Add-Skip 'API-PDF' 'Invoice PDF accessible' 'ApiBaseUrl not provided'
        return
    }

    $ready = Invoke-ApiJson GET '/health/ready'
    $healthOk = $ready.Status -eq 200 -and $ready.Json.status -eq 'Healthy'
    Add-Check 'API-HEALTH' 'Application starts; GET /health/ready' $healthOk "status=$($ready.Status) body=$($ready.Content)"

    if (-not $TestUserEmail -or -not $TestUserPassword) {
        Add-Skip 'API-LOGIN' 'Login as known tenant user' 'TestUserEmail or TestUserPassword not provided'
        Add-Skip 'API-PDF' 'Invoice PDF accessible' 'Login credentials not provided'
        return
    }

    $pwd = ConvertFrom-SecureStringPlain $TestUserPassword
    $loginBody = (@{ email = $TestUserEmail; password = $pwd } | ConvertTo-Json -Compress)
    $login = Invoke-ApiJson POST '/api/auth/login' $loginBody
    $token = $login.Json.token
    $loginOk = $login.Status -eq 200 -and $token
    Add-Check 'API-LOGIN' 'Login as known tenant user' $loginOk "status=$($login.Status)"

    if ($SampleInvoiceId -gt 0 -and $loginOk) {
        $pdf = Invoke-ApiJson GET "/api/invoices/$SampleInvoiceId/pdf" $null $token
        $pdfOk = $pdf.Status -eq 200
        Add-Check 'API-PDF' 'Invoice PDF downloadable via API' $pdfOk "invoiceId=$SampleInvoiceId status=$($pdf.Status)"
    }
    elseif ($SampleInvoiceId -le 0) {
        Add-Skip 'API-PDF' 'Invoice PDF downloadable via API' 'SampleInvoiceId not provided'
    }
}

function Test-DocumentChecks {
    if (-not $DocumentRoot) {
        Add-Skip 'DOC-ROOT' 'Document storage root accessible' 'DocumentRoot not provided'
        return
    }

    $rootOk = Test-Path $DocumentRoot
    Add-Check 'DOC-ROOT' 'Document storage root exists' $rootOk "path=$DocumentRoot"

    if (-not $rootOk) { return }

    $tenantDirs = @(Get-ChildItem -Path $DocumentRoot -Directory -ErrorAction SilentlyContinue)
    $tenantOk = $tenantDirs.Count -gt 0
    Add-Check 'DOC-TENANTS' 'Tenant document folders present' $tenantOk "folderCount=$($tenantDirs.Count)"

    if ($SampleInvoiceId -gt 0) {
        $db = $SqlDatabaseName
        $pdfPath = Invoke-SqlScalar $db "SELECT TOP 1 PdfPath FROM Invoices WHERE Id = $SampleInvoiceId;"
        if ($pdfPath) {
            $fullPath = Join-Path $DocumentRoot ($pdfPath -replace '/', '\')
            $fileOk = Test-Path $fullPath
            Add-Check 'DOC-PDF' 'Invoice PDF file on disk' $fileOk "relativePath=$pdfPath"
        }
        else {
            Add-Skip 'DOC-PDF' 'Invoice PDF file on disk' "Invoice $SampleInvoiceId has no PdfPath (may regenerate from snapshots)"
        }
    }
}

# --- Main ---

Write-Host "Verify-RestoreDrill.ps1 — Mode: $Mode (read-only; no restore/delete)"
Write-Host "  Database: $SqlDatabaseName on $(Resolve-SqlServerTarget)"
Write-Host ''

if ($Mode -eq 'Baseline') {
    if (-not $BaselineOutputPath) {
        throw 'Baseline mode requires -BaselineOutputPath.'
    }

    $counts = Get-EntityCounts
    $dir = Split-Path -Parent $BaselineOutputPath
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $counts | ConvertTo-Json -Depth 4 | Set-Content -Path $BaselineOutputPath -Encoding UTF8

    Add-Check 'BASELINE' 'Baseline counts captured' $true "output=$BaselineOutputPath"
    Write-Host "Baseline written to $BaselineOutputPath"
    Write-Host "  users=$($counts.users) companies=$($counts.companies) clients=$($counts.clients)"
    Write-Host "  contracts=$($counts.contracts) invoices=$($counts.invoices) paid=$($counts.invoicesPaid)"
}
else {
    if (-not $BaselineInputPath) {
        throw 'Validate mode requires -BaselineInputPath from a prior Baseline run.'
    }
    if (-not (Test-Path $BaselineInputPath)) {
        throw "Baseline file not found: $BaselineInputPath"
    }

    $baseline = Get-Content $BaselineInputPath -Raw | ConvertFrom-Json
    $current = Get-EntityCounts

    $countsOk = Compare-Counts $baseline $current
    Test-RoleChecks $current | Out-Null
    Test-ApiChecks $current
    Test-DocumentChecks

    if ($SqlDatabaseName -eq 'CareHome') {
        Add-Check 'SAFETY-DB-NAME' 'Restored database uses isolated name' $false 'SqlDatabaseName is CareHome — use CareHomeRestoreCheck for drills'
    }
    else {
        Add-Check 'SAFETY-DB-NAME' 'Restored database uses isolated name' $true "database=$SqlDatabaseName"
    }
}

$failures = @($results | Where-Object { $_.Result -eq 'FAIL' })
$pass = $failures.Count -eq 0

Write-Host ''
Write-Host '--- Restore drill validation checklist ---'
foreach ($r in $results) {
    Write-Host ("  [{0}] {1} — {2}" -f $r.Result, $r.Name, $r.Detail)
}

if ($ReportOutputPath -and $Mode -eq 'Validate') {
    $dir = Split-Path -Parent $ReportOutputPath
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $report = [ordered]@{
        generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
        mode         = $Mode
        database     = $SqlDatabaseName
        overall      = $(if ($pass) { 'PASS' } else { 'FAIL' })
        checks       = $results
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -Path $ReportOutputPath -Encoding UTF8
    Write-Host ''
    Write-Host "Report written to $ReportOutputPath"
}

Write-Host ''
if ($pass) {
    Write-Host 'PASS: All restore drill checks passed.'
    exit 0
}

Write-Host "FAIL: $($failures.Count) check(s) failed."
exit 1

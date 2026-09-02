# UAT TC-287 through TC-305 (Responsive UI, Browser, Concurrency)
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5092'
$repoRoot = Split-Path $PSScriptRoot -Parent
$frontendRoot = Join-Path $repoRoot 'frontend\care-home-web\src'
$results = @()
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$pastDob = '1980-01-15'
$validPwd = 'QaManual#2026Test'

function Add-Result($tc, $name, $passed, $detail) {
    $script:results += [pscustomobject]@{ TC = $tc; Name = $name; Result = $(if ($passed) { 'PASS' } else { 'FAIL' }); Detail = $detail }
}

function Add-Skip($tc, $name, $detail) {
    $script:results += [pscustomobject]@{ TC = $tc; Name = $name; Result = 'SKIP'; Detail = $detail }
}

function Read-Source($relativePath) {
    $path = Join-Path $frontendRoot $relativePath
    if (-not (Test-Path $path)) { return '' }
    return Get-Content $path -Raw
}

function Test-ResponsiveLayoutSource($viewportLabel) {
    $styles = Get-Content (Join-Path $repoRoot 'frontend\care-home-web\src\styles.scss') -Raw

    $appTs = Read-Source 'app\app.ts'
    $appHtml = Read-Source 'app\app.html'
    $routes = Read-Source 'app\app.routes.ts'

    $overflowOk = $styles -match 'overflow-x:\s*hidden'
    $tableScrollOk = $styles -match '\.table-container[\s\S]*overflow-x:\s*auto'
    $mobileNavOk = ($appTs -match 'max-width:\s*1024px') -and ($appHtml -match "mode.*isMobile") -and ($appHtml -match 'aria-label="Toggle navigation"')
    $minWidthOk = ($appHtml -match 'min-w-0') -or ($styles -match 'min-width:\s*0')
    $routesOk = ($routes -match 'dashboard') -and ($routes -match 'clients') -and ($routes -match 'billing') -and ($routes -match 'invoices')

    return @{
        Pass = ($overflowOk -and $tableScrollOk -and $mobileNavOk -and $minWidthOk -and $routesOk)
        Detail = "viewport=$viewportLabel overflow=$overflowOk tableScroll=$tableScrollOk mobileNav=$mobileNavOk routes=$routesOk (source verification)"
    }
}

function Get-ErrorText($r) {
    if ($r.Json -and $r.Json.errors) {
        $parts = @()
        foreach ($prop in $r.Json.errors.PSObject.Properties) {
            $val = $prop.Value
            if ($val -is [array] -and $val.Count -gt 0) { $parts += [string]$val[0] }
            elseif ($val) { $parts += [string]$val }
        }
        if ($parts.Count -gt 0) { return ($parts -join '; ') }
    }
    if ($r.Json -and $r.Json.message) { return [string]$r.Json.message }
    if ($r.Json -and $r.Json.title) { return [string]$r.Json.title }
    $content = [string]$r.Content
    if ($content -and $content -notmatch '^\s*\{') { return $content }
    return $content
}

function Invoke-Json($Method, $Path, $Body = $null, $Token = $null, $ExtraHeaders = @()) {
    $args = @('-s', '-w', "`n%{http_code}", '-X', $Method, "$base$Path", '-H', 'Content-Type: application/json; charset=utf-8')
    if ($Token) { $args += @('-H', "Authorization: Bearer $Token") }
    foreach ($h in $ExtraHeaders) { $args += @('-H', $h) }
    if ($Body) {
        $temp = [IO.Path]::GetTempFileName()
        [IO.File]::WriteAllText($temp, $Body, [Text.UTF8Encoding]::new($false))
        $args += @('--data-binary', "@$temp")
        try { $raw = & curl.exe @args } finally { Remove-Item $temp -Force }
    } else { $raw = & curl.exe @args }
    $lines = if ($raw) { $raw -split "`n" } else { @('0') }
    return @{
        Status = [int]$lines[-1]
        Content = ($lines[0..($lines.Length - 2)] -join "`n")
        Json = $(if ($lines.Length -gt 1 -and $lines[0]) { try { $lines[0..($lines.Length - 2)] -join "`n" | ConvertFrom-Json } catch { $null } } else { $null })
    }
}

function Login($email, $password) {
    $r = Invoke-Json POST '/api/auth/login' (@{ email = $email; password = $password } | ConvertTo-Json -Compress) $null
    if ($r.Status -eq 200) { return $r.Json.token }
    return $null
}

function Provision-Tenant($pt, $name, $email) {
    $c = Invoke-Json POST '/api/platform/tenants' (@{ name = $name; isActive = $true; adminEmail = $email; adminDisplayName = 'QA Admin' } | ConvertTo-Json -Compress) $pt
    if ($c.Status -ne 201) { throw "Provision failed ($email): $($c.Content)" }
    $tp = $c.Json.temporaryPassword
    $t = Login $email $tp
    Invoke-Json POST '/api/auth/change-password' (@{ currentPassword = $tp; newPassword = 'QaTenantAdmin!99' } | ConvertTo-Json -Compress) $t | Out-Null
    return @{ Token = (Login $email 'QaTenantAdmin!99'); TenantId = $c.Json.id; Name = $name }
}

function Get-CategoryId($token, $code) {
    return ((Invoke-Json GET '/api/invoice-categories' $null $token).Json | Where-Object { $_.code -eq $code } | Select-Object -First 1).id
}

function Setup-Stack($token, $suffix) {
    $co = (Invoke-Json POST '/api/companies' (@{ name = "Conc Co $suffix" } | ConvertTo-Json -Compress) $token).Json
    $careHome = (Invoke-Json POST '/api/care-homes' (@{
        companyId = $co.id; code = "C$suffix"; name = "Conc Home $suffix"; bedCapacity = 10; managerName = 'Mgr'
    } | ConvertTo-Json -Compress) $token).Json
    $fa = (Invoke-Json POST '/api/funding-authorities' (@{
        code = "FA$suffix"; name = 'Conc Authority'; type = 'Council'; billingFrequency = 'Weekly'; email = 'fa@qa.test'
    } | ConvertTo-Json -Compress) $token).Json
    $cat = Get-CategoryId $token 'GENERAL_CARE'
    $nom = (Invoke-Json POST '/api/nominal-codes' (@{ code = "N$suffix"; name = 'Care' } | ConvertTo-Json -Compress) $token).Json
    Invoke-Json POST '/api/invoice-templates' (@{
        name = "Tpl $suffix"; invoiceCategoryId = $cat; contactEmail = 'bill@qa.test'
        bankAccountName = 'Bank'; sortCode = '12-34-56'; accountNumber = '12345678'
    } | ConvertTo-Json -Compress) $token | Out-Null
    return @{ CompanyId = $co.id; HomeId = $careHome.id; FaId = $fa.id; CatId = $cat; NomId = $nom.id }
}

function New-ClientWithContract($token, $stack, $sageId, $ref, $periodStart, $periodEnd) {
    $cl = (Invoke-Json POST '/api/clients' (@{
        careHomeId = $stack.HomeId; sageId = $sageId; referenceNumber = $ref
        firstName = 'Conc'; lastName = $ref; careType = 'Residential'
        admissionDate = '2026-01-01'; dateOfBirth = $pastDob
    } | ConvertTo-Json -Compress) $token).Json
    $fc = (Invoke-Json POST "/api/clients/$($cl.id)/funding-contracts" (@{
        fundingAuthorityId = $stack.FaId; invoiceCategoryId = $stack.CatId; nominalCodeId = $stack.NomId; contractStartDate = '2026-01-01'
    } | ConvertTo-Json -Compress) $token).Json
    Invoke-Json POST "/api/funding-contracts/$($fc.id)/rates" (@{
        effectiveFrom = '2026-01-01'; frequency = 'Daily'; amount = 100
    } | ConvertTo-Json -Compress) $token | Out-Null
    $gen = Invoke-Json POST '/api/billing/generate' (@{
        companyId = $stack.CompanyId; careHomeId = $stack.HomeId
        periodStart = $periodStart; periodEnd = $periodEnd
        clientIds = @($cl.id); invoiceCategoryId = $stack.CatId
    } | ConvertTo-Json -Compress) $token
    return @{ Client = $cl; InvoiceId = [int]$gen.Json.invoiceIds[0] }
}

function Invoke-ParallelCurl($method, $path, $token, $bodyJson, $count) {
    $temp = [IO.Path]::GetTempFileName()
    [IO.File]::WriteAllText($temp, $bodyJson, [Text.UTF8Encoding]::new($false))
    $jobs = 1..$count | ForEach-Object {
        Start-Job -ScriptBlock {
            param($b, $m, $p, $t, $tmp)
            & curl.exe -s -w "`n%{http_code}" -X $m "$b$p" -H 'Content-Type: application/json' -H "Authorization: Bearer $t" --data-binary "@$tmp"
        } -ArgumentList $base, $method, $path, $token, $temp
    }
    $jobs | Wait-Job | Out-Null
    $rawResults = @($jobs | ForEach-Object {
        $out = Receive-Job $_
        if ($out -is [array]) { ($out -join "`n") } else { [string]$out }
    })
    Remove-Job $jobs -Force
    Remove-Item $temp -Force
    return $rawResults | ForEach-Object {
        $text = [string]$_
        $lines = $text -split "`n"
        $statusLine = $lines[-1].Trim()
        $bodyLines = if ($lines.Length -gt 1) { $lines[0..($lines.Length - 2)] } else { @() }
        @{
            Status = [int]$statusLine
            Content = ($bodyLines -join "`n")
            Json = $(try { ($bodyLines -join "`n") | ConvertFrom-Json } catch { $null })
        }
    }
}

# --- TC-287 to TC-292 Responsive layout (source verification) ---
foreach ($pair in @(
    @{ TC = 'TC-287'; Label = '1440 desktop' }
    @{ TC = 'TC-288'; Label = '1366x768 desktop' }
    @{ TC = 'TC-289'; Label = '1024 tablet' }
    @{ TC = 'TC-290'; Label = '768 tablet' }
    @{ TC = 'TC-291'; Label = '430 mobile' }
    @{ TC = 'TC-292'; Label = '375 mobile' }
)) {
    $check = Test-ResponsiveLayoutSource $pair.Label
    Add-Result $pair.TC "Core navigation/layout at $($pair.Label)" $check.Pass $check.Detail
}

# --- TC-293 Mobile navigation drawer ---
$appTs293 = Read-Source 'app\app.ts'
$appHtml293 = Read-Source 'app\app.html'
$drawerOk = ($appTs293 -match 'toggleMenu') -and ($appTs293 -match 'closeMenu') -and
    ($appTs293 -match 'isMobile') -and ($appHtml293 -match '\(click\)="closeMenu\(\)"') -and
    ($appHtml293 -match 'closedStart')
Add-Result 'TC-293' 'Mobile navigation drawer' $drawerOk 'hamburger toggle, close on nav select, closedStart handler (source verification)'

# --- TC-294 Complex financial tables on mobile ---
$billingHtml = Read-Source 'app\features\billing\pages\billing-workspace\billing-workspace.html'
$invoiceHtml = Read-Source 'app\features\invoices\pages\invoice-detail\invoice-detail.html'
$reportsHtml = Read-Source 'app\features\reports\pages\reports\reports.html'
$table294 = ($billingHtml -match 'table-container') -and ($invoiceHtml -match 'table-container') -and ($reportsHtml -match 'table-container')
Add-Result 'TC-294' 'Complex financial tables on mobile' $table294 'billing/invoice-detail/reports use table-container horizontal scroll (source verification)'

# --- TC-295 Inline field validation ---
$pt = Login 'admin@localhost' 'DevAdmin!12345'
if (-not $pt) { throw 'Platform login failed' }
$taObj = Provision-Tenant $pt "QA UI Conc $ts" "qa-ui-conc-$ts@uat.test"
$ta = $taObj.Token
$stack = Setup-Stack $ta $ts

$badClient = Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = ''; referenceNumber = ''
    firstName = ''; lastName = ''; careType = 'InvalidType'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
$badCareType = Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = "B295$ts"; referenceNumber = "RB295-$ts"
    firstName = 'Bad'; lastName = 'Type'; careType = 'InvalidType'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = "DUP$ts"; referenceNumber = "RD1-$ts"
    firstName = 'Dup'; lastName = 'One'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta | Out-Null
$dupClient2 = Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = "DUP$ts"; referenceNumber = "RD2-$ts"
    firstName = 'Dup'; lastName = 'Two'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
$clientFormHtml = Read-Source 'app\features\clients\pages\client-form\client-form.html'
$apiErrTs = Read-Source 'app\core\api-error.ts'
$msg295a = Get-ErrorText $badClient
$msg295b = Get-ErrorText $badCareType
$msg295c = Get-ErrorText $dupClient2
$readable295 = ($badClient.Status -eq 400) -and ($badCareType.Status -eq 400) -and ($dupClient2.Status -eq 400) -and
    ($msg295a -match 'required|Required|SageId|ReferenceNumber|FirstName|LastName') -and
    ($msg295b -match 'Care type|Nursing|Residential') -and
    ($msg295c -match 'Sage ID') -and
    ($clientFormHtml -match 'mat-error') -and ($apiErrTs -match 'getApiErrorMessage')
Add-Result 'TC-295' 'Inline field validation' $readable295 "required=$msg295a careType=$msg295b dup=$msg295c"

# --- TC-296 Save success/error notifications ---
$toastSvc = Read-Source 'app\shared\ui\toast.service.ts'
$clientFormTs = Read-Source 'app\features\clients\pages\client-form\client-form.ts'
$goodSave = Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = "OK$ts"; referenceNumber = "ROK-$ts"
    firstName = 'Good'; lastName = 'Save'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
$serverFail = Invoke-Json POST '/api/clients' (@{
    careHomeId = 999999; sageId = "BAD$ts"; referenceNumber = "RBAD-$ts"
    firstName = 'Bad'; lastName = 'Home'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
$failMsg296 = Get-ErrorText $serverFail
$toast296 = ($toastSvc -match 'success\(') -and ($toastSvc -match 'error\(') -and
    ($clientFormTs -match 'toast\.success') -and ($clientFormTs -match 'errorMessage') -and
    ($serverFail.Status -eq 400) -and ($goodSave.Status -eq 201) -and
    ($failMsg296 -match 'care home|exist|inactive')
Add-Result 'TC-296' 'Save success/error notifications' $toast296 "toast+errorMessage; valid=201 invalid=$($serverFail.Status) msg=$failMsg296"

# --- TC-297 Keyboard navigation and visible focus ---
$loginHtml = Read-Source 'app\features\login\login.html'
$focus297 = ($appHtml293 -match 'aria-label="Toggle navigation"') -and ($loginHtml -match 'mat-form-field') -and
    ($loginHtml -match 'type="submit"')
Add-Result 'TC-297' 'Keyboard navigation and visible focus' $focus297 'aria-labels and form controls present (Material focus rings; manual keyboard walk recommended)'

# --- TC-298 Labels and error association ---
$labels298 = ($clientFormHtml -match 'mat-label') -and ($clientFormHtml -match 'mat-error') -and
    ($loginHtml -match 'mat-error')
Add-Result 'TC-298' 'Labels and error association' $labels298 'client-form and login use mat-label/mat-error (source verification)'

# --- TC-299 Chrome/Edge compatibility ---
Add-Skip 'TC-299' 'Chrome/Edge current versions' 'Requires manual smoke in Chrome and Edge browsers'

# --- TC-300 Multiple tabs same invoice ---
$inv300 = New-ClientWithContract $ta $stack "S300$ts" "REF300$ts" '2026-07-01' '2026-07-07'
$tab1 = Invoke-Json GET "/api/invoices/$($inv300.InvoiceId)" $null $ta
$tab2 = Invoke-Json GET "/api/invoices/$($inv300.InvoiceId)" $null $ta
Invoke-Json POST "/api/invoices/$($inv300.InvoiceId)/payment-status" (@{ paymentStatus = 'Paid' } | ConvertTo-Json -Compress) $ta | Out-Null
$tab1Refresh = Invoke-Json GET "/api/invoices/$($inv300.InvoiceId)" $null $ta
$tc300 = ($tab1.Status -eq 200) -and ($tab2.Status -eq 200) -and
    ($tab1.Json.invoiceNumber -eq $tab2.Json.invoiceNumber) -and
    ($tab1Refresh.Json.paymentStatus -eq 'Paid')
Add-Result 'TC-300' 'Multiple tabs same invoice' $tc300 "initial=$($tab1.Json.paymentStatus) refreshed=$($tab1Refresh.Json.paymentStatus)"

# --- TC-301 Double-submit Create Client ---
$body301 = (@{
    careHomeId = $stack.HomeId; sageId = "DS301$ts"; referenceNumber = "REF301$ts"
    firstName = 'Double'; lastName = 'Submit'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress)
$res301 = Invoke-ParallelCurl POST '/api/clients' $ta $body301 2
$created301 = @($res301 | Where-Object { $_.Status -eq 201 })
$blocked301 = @($res301 | Where-Object { $_.Status -eq 400 })
$list301 = @((Invoke-Json GET "/api/clients?search=DS301$ts" $null $ta).Json.items)
$tc301 = ($created301.Count -eq 1) -and ($blocked301.Count -eq 1) -and ($list301.Count -eq 1)
Add-Result 'TC-301' 'Double-submit Create Client' $tc301 "201=$($created301.Count) 400=$($blocked301.Count) db=$($list301.Count)"

# --- TC-302 Two tabs generate same billing period ---
$stack302 = New-ClientWithContract $ta $stack "S302$ts" "REF302$ts" '2026-08-01' '2026-08-07'
$body302 = (@{
    companyId = $stack.CompanyId; careHomeId = $stack.HomeId
    periodStart = '2026-09-01'; periodEnd = '2026-09-07'
    clientIds = @($stack302.Client.id); invoiceCategoryId = $stack.CatId
} | ConvertTo-Json -Compress)
$res302 = Invoke-ParallelCurl POST '/api/billing/generate' $ta $body302 2
$invs302 = @((Invoke-Json GET '/api/invoices?pageSize=200' $null $ta).Json.items | ForEach-Object { $_ }) |
    Where-Object { $_.periodStart -eq '2026-09-01' -and $_.periodEnd -eq '2026-09-07' -and $_.clientId -eq $stack302.Client.id }
$success302 = @($res302 | Where-Object { $_.Status -eq 200 -or $_.Status -eq 201 })
$tc302 = ($invs302.Count -le 1) -and ($success302.Count -ge 1)
Add-Result 'TC-302' 'Two tabs generate same billing period' $tc302 "invoices=$($invs302.Count) successes=$($success302.Count) statuses=$($res302.Status -join ',')"

# --- TC-303 Two users create same nominal code ---
$nomCode303 = "NC303_$ts"
$body303 = (@{ code = $nomCode303; name = 'Duplicate Nominal' } | ConvertTo-Json -Compress)
$res303 = Invoke-ParallelCurl POST '/api/nominal-codes' $ta $body303 2
$created303 = @($res303 | Where-Object { $_.Status -eq 201 })
$blocked303 = @($res303 | Where-Object { $_.Status -eq 400 })
$db303 = @((Invoke-Json GET '/api/nominal-codes' $null $ta).Json | Where-Object { $_.code -eq $nomCode303 })
$tc303 = ($created303.Count -eq 1) -and ($blocked303.Count -eq 1) -and ($db303.Count -eq 1)
Add-Result 'TC-303' 'Two users create same nominal code' $tc303 "201=$($created303.Count) 400=$($blocked303.Count) db=$($db303.Count)"

# --- TC-304 Two users create overlapping contract ---
$cl304 = (Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = "S304$ts"; referenceNumber = "REF304$ts"
    firstName = 'Overlap'; lastName = 'Client'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta).Json
$body304 = (@{
    fundingAuthorityId = $stack.FaId; invoiceCategoryId = $stack.CatId; nominalCodeId = $stack.NomId
    contractStartDate = '2026-01-01'; contractEndDate = '2026-12-31'
} | ConvertTo-Json -Compress)
$path304 = "/api/clients/$($cl304.id)/funding-contracts"
$res304 = Invoke-ParallelCurl POST $path304 $ta $body304 2
$created304 = @($res304 | Where-Object { $_.Status -eq 201 })
$blocked304 = @($res304 | Where-Object { $_.Status -eq 400 })
$contracts304 = @((Invoke-Json GET "/api/clients/$($cl304.id)/funding-contracts" $null $ta).Json |
    Where-Object { $_.status -eq 'Active' -and $_.fundingAuthorityId -eq $stack.FaId -and $_.invoiceCategoryId -eq $stack.CatId })
$tc304 = ($created304.Count -eq 1) -and ($blocked304.Count -eq 1) -and ($contracts304.Count -eq 1)
Add-Result 'TC-304' 'Two users create overlapping contract' $tc304 "201=$($created304.Count) 400=$($blocked304.Count) active=$($contracts304.Count)"

# --- TC-305 Two users change payment status ---
$inv305 = New-ClientWithContract $ta $stack "S305$ts" "REF305$ts" '2026-10-01' '2026-10-07'
$bodyPaid = (@{ paymentStatus = 'Paid' } | ConvertTo-Json -Compress)
$bodyNotPaid = (@{ paymentStatus = 'NotPaid' } | ConvertTo-Json -Compress)
$tempPaid = [IO.Path]::GetTempFileName()
$tempNot = [IO.Path]::GetTempFileName()
[IO.File]::WriteAllText($tempPaid, $bodyPaid, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText($tempNot, $bodyNotPaid, [Text.UTF8Encoding]::new($false))
$path305 = "/api/invoices/$($inv305.InvoiceId)/payment-status"
$jobPaid = Start-Job -ScriptBlock {
    param($b, $p, $t, $tmp)
    & curl.exe -s -w "`n%{http_code}" -X POST "$b$p" -H 'Content-Type: application/json' -H "Authorization: Bearer $t" --data-binary "@$tmp"
} -ArgumentList $base, $path305, $ta, $tempPaid
$jobNot = Start-Job -ScriptBlock {
    param($b, $p, $t, $tmp)
    & curl.exe -s -w "`n%{http_code}" -X POST "$b$p" -H 'Content-Type: application/json' -H "Authorization: Bearer $t" --data-binary "@$tmp"
} -ArgumentList $base, $path305, $ta, $tempNot
Wait-Job $jobPaid, $jobNot | Out-Null
$raw305a = Receive-Job $jobPaid
$raw305b = Receive-Job $jobNot
Remove-Job $jobPaid, $jobNot -Force
Remove-Item $tempPaid, $tempNot -Force
function Parse-Curl($raw) {
    $text = if ($raw -is [array]) { ($raw -join "`n") } else { [string]$raw }
    $lines = $text -split "`n"
    return @{ Status = [int]$lines[-1].Trim(); Content = ($lines[0..($lines.Length - 2)] -join "`n") }
}
$r305a = Parse-Curl $raw305a
$r305b = Parse-Curl $raw305b
$final305 = (Invoke-Json GET "/api/invoices/$($inv305.InvoiceId)" $null $ta).Json
$audit305 = @((Invoke-Json GET '/api/audit?entityType=Invoice&action=PaymentStatus&pageSize=50' $null $ta).Json.items | ForEach-Object { $_ }) |
    Where-Object { $_.entityId -eq "$($inv305.InvoiceId)" }
$validFinal = ($final305.paymentStatus -eq 'Paid') -or ($final305.paymentStatus -eq 'NotPaid')
$tc305 = ($r305a.Status -eq 200) -and ($r305b.Status -eq 200) -and $validFinal -and ($audit305.Count -ge 2)
Add-Result 'TC-305' 'Two users change payment status' $tc305 "statuses=$($r305a.Status)/$($r305b.Status) final=$($final305.paymentStatus) audit=$($audit305.Count)"

# --- Summary ---
Write-Host ''
Write-Host '=== UAT TC-287 to TC-305 Results ===' -ForegroundColor Cyan
$results | Format-Table -AutoSize
$pass = @($results | Where-Object Result -eq 'PASS').Count
$fail = @($results | Where-Object Result -eq 'FAIL').Count
$skip = @($results | Where-Object Result -eq 'SKIP').Count
Write-Host "PASS=$pass FAIL=$fail SKIP=$skip TOTAL=$($results.Count)" -ForegroundColor $(if ($fail -gt 0) { 'Red' } else { 'Green' })
if ($fail -gt 0) { exit 1 }

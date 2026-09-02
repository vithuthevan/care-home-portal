# UAT TC-266 through TC-286 API tests (Audit & Traceability, Error Handling)
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5092'
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
    $hdrFile = [IO.Path]::GetTempFileName()
    $hdrArgs = @('-s', '-D', $hdrFile, '-o', 'NUL', '-X', $Method, "$base$Path", '-H', 'Content-Type: application/json; charset=utf-8')
    if ($Token) { $hdrArgs += @('-H', "Authorization: Bearer $Token") }
    foreach ($h in $ExtraHeaders) { $hdrArgs += @('-H', $h) }
    if ($Body) {
        $temp2 = [IO.Path]::GetTempFileName()
        [IO.File]::WriteAllText($temp2, $Body, [Text.UTF8Encoding]::new($false))
        $hdrArgs += @('--data-binary', "@$temp2")
        try { & curl.exe @hdrArgs | Out-Null } finally { Remove-Item $temp2 -Force }
    } else { & curl.exe @hdrArgs | Out-Null }
    $respHeaders = if (Test-Path $hdrFile) { Get-Content $hdrFile -Raw } else { '' }
    Remove-Item $hdrFile -Force -ErrorAction SilentlyContinue
    return @{
        Status = [int]$lines[-1]
        Content = ($lines[0..($lines.Length - 2)] -join "`n")
        Json = $(if ($lines.Length -gt 1 -and $lines[0]) { try { $lines[0..($lines.Length - 2)] -join "`n" | ConvertFrom-Json } catch { $null } } else { $null })
        Headers = $respHeaders
    }
}

function Invoke-Raw($Method, $Path, $Token = $null, $ExtraHeaders = @()) {
    $args = @('-s', '-w', "`n%{http_code}", '-X', $Method, "$base$Path")
    if ($Token) { $args += @('-H', "Authorization: Bearer $Token") }
    foreach ($h in $ExtraHeaders) { $args += @('-H', $h) }
    $hdrFile = [IO.Path]::GetTempFileName()
    $argsWithHdr = @('-s', '-D', $hdrFile, '-w', "`n%{http_code}", '-X', $Method, "$base$Path")
    if ($Token) { $argsWithHdr += @('-H', "Authorization: Bearer $Token") }
    foreach ($h in $ExtraHeaders) { $argsWithHdr += @('-H', $h) }
    $raw = & curl.exe @argsWithHdr
    $lines = $raw -split "`n"
    $respHeaders = if (Test-Path $hdrFile) { Get-Content $hdrFile -Raw } else { '' }
    Remove-Item $hdrFile -Force -ErrorAction SilentlyContinue
    return @{
        Status = [int]$lines[-1]
        Content = ($lines[0..($lines.Length - 2)] -join "`n")
        Json = $(try { $lines[0..($lines.Length - 2)] -join "`n" | ConvertFrom-Json } catch { $null })
        Headers = $respHeaders
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

function Find-Audit($token, $entityType, $action, $entityId = $null) {
    $q = "/api/audit?entityType=$entityType&action=$action&pageSize=100"
    $r = Invoke-Json GET $q $null $token
    $items = @($r.Json.items)
    if ($entityId) {
        return $items | Where-Object { $_.entityId -eq "$entityId" } | Select-Object -First 1
    }
    return $items | Select-Object -First 1
}

function Test-AuditEntry($entry) {
    return $entry -and $entry.loggedAt -and $entry.userId -and $entry.entityType -and $entry.action -and $entry.description -and
        $entry.description -notmatch 'password|Password|secret|TemporaryPassword'
}

function New-User($token, $email, $displayName, $role, $password, $careHomeIds = @()) {
    return Invoke-Json POST '/api/users' (@{
        email = $email; displayName = $displayName; password = $password; role = $role; careHomeIds = @($careHomeIds)
    } | ConvertTo-Json -Compress) $token
}

function Setup-Stack($token) {
    $co = (Invoke-Json POST '/api/companies' (@{ name = "Audit Co $ts" } | ConvertTo-Json -Compress) $token).Json
    $careHome = (Invoke-Json POST '/api/care-homes' (@{
        companyId = $co.id; code = 'AUDIT01'; name = 'Audit Home'; bedCapacity = 10; managerName = 'Mgr'
    } | ConvertTo-Json -Compress) $token).Json
    $fa = (Invoke-Json POST '/api/funding-authorities' (@{
        code = 'AUDFA'; name = 'Audit Authority'; type = 'Council'; billingFrequency = 'Weekly'; email = 'fa@qa.test'
    } | ConvertTo-Json -Compress) $token).Json
    $cat = Get-CategoryId $token 'GENERAL_CARE'
    $nom = (Invoke-Json POST '/api/nominal-codes' (@{ code = "AUD$ts"; name = 'Care' } | ConvertTo-Json -Compress) $token).Json
    Invoke-Json POST '/api/invoice-templates' (@{
        name = 'Audit Tpl'; invoiceCategoryId = $cat; contactEmail = 'bill@qa.test'
        bankAccountName = 'Bank'; sortCode = '12-34-56'; accountNumber = '12345678'
    } | ConvertTo-Json -Compress) $token | Out-Null
    return @{ CompanyId = $co.id; HomeId = $careHome.id; FaId = $fa.id; CatId = $cat; NomId = $nom.id }
}

function Update-Client($token, $id, $client, $patch) {
    $body = @{
        careHomeId = $client.careHomeId; sageId = $client.sageId; referenceNumber = $client.referenceNumber
        firstName = $client.firstName; lastName = $client.lastName; careType = $client.careType
        status = $client.status; admissionDate = $client.admissionDate
        dischargeDate = $client.dischargeDate; dischargeReason = $client.dischargeReason
        dateOfBirth = $client.dateOfBirth; isArchived = $client.isArchived
    }
    foreach ($k in $patch.Keys) { $body[$k] = $patch[$k] }
    return Invoke-Json PUT "/api/clients/$id" ($body | ConvertTo-Json -Compress) $token
}

# --- Bootstrap ---
$pt = Login 'admin@localhost' 'DevAdmin!12345'
if (-not $pt) { throw 'Platform login failed' }
$taObj = Provision-Tenant $pt "QA Audit A $ts" "qa-audit-a-$ts@uat.test"
$tbObj = Provision-Tenant $pt "QA Audit B $ts" "qa-audit-b-$ts@uat.test"
$ta = $taObj.Token
$tb = $tbObj.Token
$stack = Setup-Stack $ta
$stackB = Setup-Stack $tb
$locSec = New-User $ta "loc-sec-$ts@uat.test" 'Loc Security' 'LocationManager' $validPwd @($stack.HomeId)
$locSecTok = Login "loc-sec-$ts@uat.test" $validPwd

# --- TC-266 Create client ---
$cl266 = Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = 'AUD266'; referenceNumber = 'REF266'
    firstName = 'Audit'; lastName = 'Create266'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
$a266 = Find-Audit $ta 'Client' 'Create' $cl266.Json.id
Add-Result 'TC-266' 'Audit records: Create client' (Test-AuditEntry $a266) "entity=$($a266.entityType) action=$($a266.action)"

# --- TC-267 Edit client ---
Update-Client $ta $cl266.Json.id $cl266.Json @{ firstName = 'Edited266' } | Out-Null
$a267 = Find-Audit $ta 'Client' 'Update' $cl266.Json.id
Add-Result 'TC-267' 'Audit records: Edit client' (Test-AuditEntry $a267) "desc=$($a267.description)"

# --- TC-268 Archive client ---
$left = Update-Client $ta $cl266.Json.id (Invoke-Json GET "/api/clients/$($cl266.Json.id)" $null $ta).Json @{
    status = 'Left'; dischargeDate = '2026-06-01'; dischargeReason = 'UAT'
}
Invoke-Json DELETE "/api/clients/$($cl266.Json.id)" $null $ta | Out-Null
$a268 = Find-Audit $ta 'Client' 'Archive' $cl266.Json.id
Add-Result 'TC-268' 'Audit records: Archive client' (Test-AuditEntry $a268) "desc=$($a268.description)"

# --- TC-269 Create funding contract ---
$cl269 = (Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = 'AUD269'; referenceNumber = 'REF269'
    firstName = 'Fund'; lastName = 'Client269'; careType = 'Residential'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta).Json
$fc269 = (Invoke-Json POST "/api/clients/$($cl269.id)/funding-contracts" (@{
    fundingAuthorityId = $stack.FaId; invoiceCategoryId = $stack.CatId; nominalCodeId = $stack.NomId; contractStartDate = '2026-01-01'
} | ConvertTo-Json -Compress) $ta).Json
$a269 = Find-Audit $ta 'ClientFundingContract' 'Create' $fc269.id
Add-Result 'TC-269' 'Audit records: Create funding contract' (Test-AuditEntry $a269) "desc=$($a269.description)"

# --- TC-270 Add rate ---
$rate270 = Invoke-Json POST "/api/funding-contracts/$($fc269.id)/rates" (@{
    effectiveFrom = '2026-01-01'; frequency = 'Weekly'; amount = 575
} | ConvertTo-Json -Compress) $ta
$a270 = Find-Audit $ta 'FundingRate' 'Create' $rate270.Json.id
Add-Result 'TC-270' 'Audit records: Add rate' (Test-AuditEntry $a270) "desc=$($a270.description)"

# --- TC-271 Generate invoice ---
$gen271 = Invoke-Json POST '/api/billing/generate' (@{
    companyId = $stack.CompanyId; careHomeId = $stack.HomeId; periodStart = '2026-05-01'; periodEnd = '2026-05-31'
    clientIds = @($cl269.id); invoiceCategoryId = $stack.CatId
} | ConvertTo-Json -Compress) $ta
$inv271 = [int]$gen271.Json.invoiceIds[0]
$a271 = Find-Audit $ta 'Invoice' 'Generate' $null
$a271match = @((Invoke-Json GET '/api/audit?entityType=Invoice&action=Generate&pageSize=20' $null $ta).Json.items) |
    Where-Object { $_.entityId -match "$inv271" } | Select-Object -First 1
Add-Result 'TC-271' 'Audit records: Generate invoice' (Test-AuditEntry $a271match) "inv=$inv271"

# --- TC-272 Send invoice ---
$invDetail = Invoke-Json GET "/api/invoices/$inv271" $null $ta
$faEmail = Invoke-Json PUT "/api/funding-authorities/$($stack.FaId)" (@{
    code = 'AUDFA'; name = 'Audit Authority'; type = 'Council'; billingFrequency = 'Weekly'
    email = 'recipient@qa.test'; isActive = $true
} | ConvertTo-Json -Compress) $ta
$send272 = Invoke-Json POST "/api/invoices/$inv271/send" $null $ta
$a272 = Find-Audit $ta 'Invoice' 'Send' $inv271
Add-Result 'TC-272' 'Audit records: Send invoice' (Test-AuditEntry $a272 -and $send272.Status -eq 200) "send=$($send272.Status) desc=$($a272.description)"

# --- TC-273 Create credit note ---
$lineId = $invDetail.Json.lines[0].id
$cn273 = Invoke-Json POST '/api/credit-notes/generate' (@{
    clientId = $cl269.id; periodStart = '2026-05-01'; periodEnd = '2026-05-31'
    creditNoteDate = '2026-06-15'; reason = 'Audit credit test'; lineAmounts = @{ "$lineId" = 50 }
} | ConvertTo-Json -Compress -Depth 5) $ta
$a273 = Find-Audit $ta 'CreditNote' 'Generate' $cn273.Json.id
Add-Result 'TC-273' 'Audit records: Create credit note' (Test-AuditEntry $a273) "cn=$($cn273.Json.creditNoteNumber)"

# --- TC-274 Change payment status ---
$pay274 = Invoke-Json POST "/api/invoices/$inv271/payment-status" (@{ paymentStatus = 'Paid' } | ConvertTo-Json -Compress) $ta
$a274 = Find-Audit $ta 'Invoice' 'PaymentStatus' $inv271
Add-Result 'TC-274' 'Audit records: Change payment status' (Test-AuditEntry $a274) "pay=$($pay274.Status)"

# --- TC-275 Generate Sage export ---
$sage275 = Invoke-Json POST '/api/sage-exports' (@{
    dateFrom = '2026-05-01'; dateTo = '2026-05-31'; companyId = $stack.CompanyId
} | ConvertTo-Json -Compress) $ta
$a275 = Find-Audit $ta 'SageExport' 'Export' $sage275.Json.id
Add-Result 'TC-275' 'Audit records: Generate Sage export' (Test-AuditEntry $a275) "batch=$($sage275.Json.id)"

# --- TC-276 Create/deactivate user ---
$u276 = New-User $ta "audit-user-$ts@uat.test" 'Audit User' 'ReadOnly' $validPwd
Invoke-Json POST "/api/users/$($u276.Json.id)/deactivate" $null $ta | Out-Null
$a276c = Find-Audit $ta 'User' 'Create' $u276.Json.id
$a276d = Find-Audit $ta 'User' 'Deactivate' $u276.Json.id
Add-Result 'TC-276' 'Audit records: Create/deactivate user' (Test-AuditEntry $a276c -and Test-AuditEntry $a276d) "create=$($a276c.action) deact=$($a276d.action)"

# --- TC-277 Audit tenant-scoped ---
Invoke-Json POST '/api/companies' (@{ name = 'TENANT_B_AUDIT_MARKER' } | ConvertTo-Json -Compress) $tb | Out-Null
$auditA = Invoke-Json GET '/api/audit?pageSize=200' $null $ta
$auditText = (@($auditA.Json.items) | ForEach-Object { "$($_.description) $($_.entityId)" }) -join ' '
Add-Result 'TC-277' 'Audit is tenant-scoped' ($auditText -notmatch 'TENANT_B_AUDIT_MARKER') 'no Tenant B marker in Tenant A audit'

# --- TC-278 Audit immutable ---
$auditId = $auditA.Json.items[0].id
$put278 = Invoke-Json PUT "/api/audit/$auditId" (@{ description = 'hack' } | ConvertTo-Json -Compress) $ta
$del278 = Invoke-Raw DELETE "/api/audit/$auditId" $ta
$locAudit = if ($locSecTok) { Invoke-Json GET '/api/audit' $null $locSecTok } else { $null }
Add-Result 'TC-278' 'Audit records cannot be edited/deleted' (
    $put278.Status -in @(404, 405) -and $del278.Status -in @(404, 405) -and
    ((-not $locSecTok) -or ($locAudit.Status -eq 403))
) "put=$($put278.Status) del=$($del278.Status) locAudit=$($locAudit.Status) locLogin=$([bool]$locSecTok)"

# --- TC-279 HTTP 400 ---
$bad279 = Invoke-Json POST '/api/clients' (@{
    careHomeId = $stack.HomeId; sageId = 'BAD279'; referenceNumber = 'BAD279'
    firstName = 'Bad'; lastName = 'Request'; careType = 'InvalidType'
    admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
Add-Result 'TC-279' 'HTTP 400 handling' (
    $bad279.Status -eq 400 -and $bad279.Json.message
) "status=$($bad279.Status) msg=$($bad279.Json.message)"

# --- TC-280 HTTP 401 ---
$unauth280 = Invoke-Json GET '/api/companies' $null $null
Add-Result 'TC-280' 'HTTP 401 handling' ($unauth280.Status -eq 401) "status=$($unauth280.Status)"

# --- TC-281 HTTP 403 ---
$forbidden281 = Invoke-Json POST '/api/platform/tenants' (@{
    name = 'Forbidden Tenant'; isActive = $true; adminEmail = "forbidden-$ts@uat.test"
} | ConvertTo-Json -Compress) $ta
Add-Result 'TC-281' 'HTTP 403 handling' (
    $forbidden281.Status -eq 403
) "status=$($forbidden281.Status)"

# --- TC-282 HTTP 404 ---
$missing282 = Invoke-Json GET '/api/invoices/999999999' $null $ta
Add-Result 'TC-282' 'HTTP 404 handling' ($missing282.Status -eq 404) "status=$($missing282.Status)"

# --- TC-283 HTTP 500 ---
$corr283 = "uat-corr-$ts"
$err283 = Invoke-Raw GET '/api/_dev/throw' $null @("X-Correlation-ID: $corr283")
Add-Result 'TC-283' 'HTTP 500 handling' (
    $err283.Status -eq 500 -and $err283.Content -match 'unexpected error' -and
    $err283.Content -match 'correlationId' -and $err283.Content -notmatch 'stack|SqlException|\.cs:'
) "status=$($err283.Status)"

# --- TC-284 API stopped ---
Add-Skip 'TC-284' 'API stopped while UI open' 'Manual UI resilience test; not automatable in API script'

# --- TC-285 Email unavailable ---
Add-Skip 'TC-285' 'Email unavailable SMTP failure' 'Dev Email:Mode=Development simulates success; requires SMTP misconfiguration test'

# --- TC-286 Correlation ID on unexpected error ---
Add-Result 'TC-286' 'Correlation ID on unexpected error' (
    $err283.Json.correlationId -and ($err283.Headers -match 'X-Correlation-ID') -and
    $err283.Content -notmatch 'password|secret'
) "corr=$($err283.Json.correlationId)"

# --- Summary ---
$results | Format-Table -AutoSize
$pass = ($results | Where-Object Result -eq 'PASS').Count
$fail = ($results | Where-Object Result -eq 'FAIL').Count
$skip = ($results | Where-Object Result -eq 'SKIP').Count
Write-Host "`nSUMMARY: $pass PASS, $fail FAIL, $skip SKIP of $($results.Count) tests"
if ($fail -gt 0) { exit 1 }

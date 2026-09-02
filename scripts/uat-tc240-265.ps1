# UAT TC-240 through TC-265 API tests (Users & Roles, Multi-Tenancy Security)
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5092'
$results = @()
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$pastDob = '1980-01-15'
$validPwd = 'QaManual#2026Test'

function Add-Result($tc, $name, $passed, $detail) {
    $script:results += [pscustomobject]@{ TC = $tc; Name = $name; Result = $(if ($passed) { 'PASS' } else { 'FAIL' }); Detail = $detail }
}

function Invoke-Json($Method, $Path, $Body = $null, $Token = $null) {
    $args = @('-s', '-w', "`n%{http_code}", '-X', $Method, "$base$Path", '-H', 'Content-Type: application/json; charset=utf-8')
    if ($Token) { $args += @('-H', "Authorization: Bearer $Token") }
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

function Invoke-Binary($Path, $Token) {
    $tmp = [IO.Path]::GetTempFileName()
    $raw = & curl.exe -s -w "`n%{http_code}" -o $tmp "$base$Path" -H "Authorization: Bearer $Token"
    $lines = $raw -split "`n"
    $bytes = if (Test-Path $tmp) { [IO.File]::ReadAllBytes($tmp) } else { [byte[]]@() }
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    return @{ Status = [int]$lines[-1]; Bytes = $bytes; Text = if ($bytes.Length) { [System.Text.Encoding]::UTF8.GetString($bytes) } else { '' } }
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

function New-User($token, $email, $displayName, $role, $password, $careHomeIds = @()) {
    return Invoke-Json POST '/api/users' (@{
        email = $email; displayName = $displayName; password = $password; role = $role; careHomeIds = @($careHomeIds)
    } | ConvertTo-Json -Compress) $token
}

function Get-CategoryId($token, $code) {
    return ((Invoke-Json GET '/api/invoice-categories' $null $token).Json | Where-Object { $_.code -eq $code } | Select-Object -First 1).id
}

function Setup-TenantData($token, $marker) {
    $co = (Invoke-Json POST '/api/companies' (@{ name = "SharedCo $marker" } | ConvertTo-Json -Compress) $token).Json
    $home1 = (Invoke-Json POST '/api/care-homes' (@{
        companyId = $co.id; code = 'HOME01'; name = "Home01 $marker"; bedCapacity = 20; managerName = 'Mgr'
    } | ConvertTo-Json -Compress) $token).Json
    $home2 = (Invoke-Json POST '/api/care-homes' (@{
        companyId = $co.id; code = 'HOME02'; name = "Home02 $marker"; bedCapacity = 10; managerName = 'Mgr2'
    } | ConvertTo-Json -Compress) $token).Json
    $fa = (Invoke-Json POST '/api/funding-authorities' (@{
        code = 'AUTH01'; name = "Authority $marker"; type = 'Council'; billingFrequency = 'Weekly'; email = 'fa@qa.test'
    } | ConvertTo-Json -Compress) $token).Json
    $cat = Get-CategoryId $token 'GENERAL_CARE'
    $nom = (Invoke-Json POST '/api/nominal-codes' (@{ code = '4000'; name = 'Care Fees' } | ConvertTo-Json -Compress) $token).Json
    Invoke-Json POST '/api/invoice-templates' (@{
        name = 'Tpl'; invoiceCategoryId = $cat; contactEmail = 'f@qa.test'
        bankAccountName = 'Bank'; sortCode = '12-34-56'; accountNumber = '12345678'
    } | ConvertTo-Json -Compress) $token | Out-Null
    $cl = (Invoke-Json POST '/api/clients' (@{
        careHomeId = $home1.id; sageId = 'SAGE001'; referenceNumber = 'CLIENT001'
        firstName = 'Tenant'; lastName = $marker; careType = 'Residential'
        admissionDate = '2026-01-01'; dateOfBirth = $pastDob
    } | ConvertTo-Json -Compress) $token).Json
    $fc = (Invoke-Json POST "/api/clients/$($cl.id)/funding-contracts" (@{
        fundingAuthorityId = $fa.id; invoiceCategoryId = $cat; nominalCodeId = $nom.id; contractStartDate = '2026-01-01'
    } | ConvertTo-Json -Compress) $token).Json
    Invoke-Json POST "/api/funding-contracts/$($fc.id)/rates" (@{
        effectiveFrom = '2026-01-01'; frequency = 'Weekly'; amount = 575
    } | ConvertTo-Json -Compress) $token | Out-Null
    $gen = Invoke-Json POST '/api/billing/generate' (@{
        companyId = $co.id; careHomeId = $home1.id; periodStart = '2026-05-01'; periodEnd = '2026-05-31'
        clientIds = @($cl.id); invoiceCategoryId = $cat
    } | ConvertTo-Json -Compress) $token
    $invId = [int]$gen.Json.invoiceIds[0]
    $inv = (Invoke-Json GET "/api/invoices/$invId" $null $token).Json
    $lineId = $inv.lines[0].id
    $cn = Invoke-Json POST '/api/credit-notes/generate' (@{
        clientId = $cl.id; periodStart = '2026-05-01'; periodEnd = '2026-05-31'
        creditNoteDate = '2026-06-01'; reason = 'UAT credit'; lineAmounts = @{ "$lineId" = 100 }
    } | ConvertTo-Json -Compress -Depth 5) $token
    $sage = Invoke-Json POST '/api/sage-exports' (@{
        dateFrom = '2026-05-01'; dateTo = '2026-05-31'; companyId = $co.id
    } | ConvertTo-Json -Compress) $token
    $audit = Invoke-Json GET '/api/audit?pageSize=5' $null $token
    $auditId = $audit.Json.items[0].id
    $settings = Invoke-Json GET '/api/settings/organisation' $null $token
    $user = New-User $token "marker-user-$marker-$ts@uat.test" "Marker $marker" 'ReadOnly' 'ReadOnlyPass!12345'
    if ($user.Status -ne 201) { throw "Marker user create failed for $marker : $($user.Content)" }
    return @{
        CompanyId = $co.id; Home1Id = $home1.id; Home2Id = $home2.id
        FaId = $fa.id; NomId = $nom.id; ContractId = $fc.id
        ClientId = $cl.id; InvoiceId = $invId; CreditNoteId = [int]$cn.Json.id
        SageBatchId = [int]$sage.Json.id; AuditLogId = $auditId
        SettingsName = $settings.Json.name; MarkerUserId = $user.Json.id
        Marker = $marker
    }
}

# --- Bootstrap ---
$pt = Login 'admin@localhost' 'DevAdmin!12345'
if (-not $pt) { throw 'Platform login failed' }
$taObj = Provision-Tenant $pt "QA Users A $ts" "qa-users-a-$ts@uat.test"
$tbObj = Provision-Tenant $pt "QA Users B $ts" "qa-users-b-$ts@uat.test"
$ta = $taObj.Token
$tb = $tbObj.Token

$dataA = Setup-TenantData $ta 'TenantA'
$dataB = Setup-TenantData $tb 'TENANT_B_MARKER'

$adminEmail = "admin.qa-$ts@test.local"
$mgrEmail = "manager.qa-$ts@test.local"
$roEmail = "readonly.qa-$ts@test.local"
$dupEmail = "dup.qa-$ts@test.local"
$deactEmail = "deact.qa-$ts@test.local"
$freshEmail = "fresh.qa-$ts@test.local"

# --- TC-240 Create Administrator ---
$c240 = New-User $ta $adminEmail 'QA Administrator' 'Administrator' $validPwd
$adminTok = Login $adminEmail $validPwd
Add-Result 'TC-240' 'Create Administrator' ($c240.Status -eq 201 -and $adminTok) "status=$($c240.Status) login=$([bool]$adminTok)"

# --- TC-241 Create LocationManager ---
$c241 = New-User $ta $mgrEmail 'QA Location Manager' 'LocationManager' $validPwd @($dataA.Home1Id)
$mgrTok = Login $mgrEmail $validPwd
$mgrOk = Invoke-Json GET "/api/care-homes/$($dataA.Home1Id)" $null $mgrTok
$mgrDeny = Invoke-Json GET "/api/care-homes/$($dataA.Home2Id)" $null $mgrTok
Add-Result 'TC-241' 'Create LocationManager and assign home' (
    $c241.Status -eq 201 -and @($c241.Json.careHomeIds) -contains $dataA.Home1Id -and
    $mgrOk.Status -eq 200 -and $mgrDeny.Status -eq 404
) "create=$($c241.Status) home1=$($mgrOk.Status) home2=$($mgrDeny.Status)"

# --- TC-242 Create ReadOnly ---
$c242 = New-User $ta $roEmail 'QA Read Only' 'ReadOnly' $validPwd
$roTok = Login $roEmail $validPwd
$roRead = Invoke-Json GET '/api/companies' $null $roTok
$roWrite = Invoke-Json POST '/api/companies' (@{ name = 'RO Blocked Co' } | ConvertTo-Json -Compress) $roTok
Add-Result 'TC-242' 'Create ReadOnly' (
    $c242.Status -eq 201 -and $roTok -and $roRead.Status -eq 200 -and $roWrite.Status -eq 403
) "read=$($roRead.Status) write=$($roWrite.Status)"

# --- TC-243 Duplicate email ---
$c243a = New-User $ta $dupEmail 'Dup User' 'ReadOnly' $validPwd
$c243b = New-User $ta $dupEmail 'Dup User 2' 'ReadOnly' $validPwd
Add-Result 'TC-243' 'Duplicate email rejected' ($c243a.Status -eq 201 -and $c243b.Status -eq 400) "first=$($c243a.Status) second=$($c243b.Status)"

# --- TC-244 Weak password ---
$c244 = New-User $ta "weak-$ts@test.local" 'Weak Pass' 'ReadOnly' 'short'
Add-Result 'TC-244' 'Weak password rejected' ($c244.Status -eq 400) "status=$($c244.Status)"

# --- TC-245 New user login ---
$c245 = New-User $ta $freshEmail 'Fresh User' 'Administrator' $validPwd
$freshTok = Login $freshEmail $validPwd
Add-Result 'TC-245' 'Newly created user password works' ($c245.Status -eq 201 -and $freshTok) "login=$([bool]$freshTok)"

# --- TC-246 Deactivate user blocks access ---
$c246 = New-User $ta $deactEmail 'Deact User' 'ReadOnly' $validPwd
$deactTok = Login $deactEmail $validPwd
$deact = Invoke-Json POST "/api/users/$($c246.Json.id)/deactivate" $null $ta
Start-Sleep -Seconds 2
$loginAfter = Invoke-Json POST '/api/auth/login' (@{ email = $deactEmail; password = $validPwd } | ConvertTo-Json -Compress) $null
$meAfter = Invoke-Json GET '/api/auth/me' $null $deactTok
$getAfter = Invoke-Json GET "/api/users/$($c246.Json.id)" $null $ta
Add-Result 'TC-246' 'Deactivate user blocks access' (
    $deact.Status -eq 204 -and $loginAfter.Status -in @(401, 429) -and $meAfter.Status -eq 401 -and $getAfter.Json.isActive -eq $false
) "deact=$($deact.Status) login=$($loginAfter.Status) me=$($meAfter.Status)"

# --- TC-247 TenantAdmin cannot assign PlatformAdmin ---
$c247 = New-User $ta "plat-$ts@test.local" 'Platform Attempt' 'PlatformAdmin' $validPwd
Add-Result 'TC-247' 'TenantAdmin cannot assign PlatformAdmin' ($c247.Status -eq 400) "status=$($c247.Status)"

# --- TC-248 Cross-tenant care home assignment ---
$c248 = New-User $ta "crosshome-$ts@test.local" 'Cross Home' 'LocationManager' $validPwd @($dataB.Home1Id)
Add-Result 'TC-248' 'Cannot assign care home from another tenant' ($c248.Status -eq 400) "status=$($c248.Status)"

# --- TC-249 ReadOnly write actions ---
$roCoW = Invoke-Json POST '/api/companies' (@{ name = 'RO Co' } | ConvertTo-Json -Compress) $roTok
$roChW = Invoke-Json POST '/api/care-homes' (@{
    companyId = $dataA.CompanyId; code = 'ROHOME'; name = 'RO Home'; bedCapacity = 5; managerName = 'X'
} | ConvertTo-Json -Compress) $roTok
$roClW = Invoke-Json POST '/api/clients' (@{
    careHomeId = $dataA.Home1Id; sageId = 'RO-SAGE'; referenceNumber = 'RO-REF'
    firstName = 'RO'; lastName = 'Client'; careType = 'Residential'; admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $roTok
$roBillW = Invoke-Json POST '/api/billing/generate' (@{
    companyId = $dataA.CompanyId; careHomeId = $dataA.Home1Id; periodStart = '2026-06-01'; periodEnd = '2026-06-30'
    clientIds = @($dataA.ClientId); invoiceCategoryId = (Get-CategoryId $roTok 'GENERAL_CARE')
} | ConvertTo-Json -Compress) $roTok
$roInvW = Invoke-Json POST "/api/invoices/$($dataA.InvoiceId)/void" $null $roTok
Add-Result 'TC-249' 'ReadOnly write actions blocked' (
    $roCoW.Status -eq 403 -and $roChW.Status -eq 403 -and $roClW.Status -eq 403 -and $roBillW.Status -eq 403 -and $roInvW.Status -eq 403
) "co=$($roCoW.Status) home=$($roChW.Status) client=$($roClW.Status) bill=$($roBillW.Status) inv=$($roInvW.Status)"

# --- TC-250 LocationManager assigned vs unassigned ---
$mgrDash = Invoke-Json GET '/api/dashboard' $null $mgrTok
$mgrCl1 = Invoke-Json GET "/api/clients?careHomeId=$($dataA.Home1Id)" $null $mgrTok
$mgrCl2 = Invoke-Json GET "/api/clients?careHomeId=$($dataA.Home2Id)" $null $mgrTok
$mgrClientB = Invoke-Json GET "/api/clients/$($dataA.ClientId)" $null $mgrTok
Add-Result 'TC-250' 'LocationManager assigned vs unassigned' (
    $mgrDash.Status -eq 200 -and $mgrDash.Json.totalCareHomes -eq 1 -and
    $mgrCl1.Status -eq 200 -and $mgrCl2.Json.items.Count -eq 0 -and $mgrClientB.Status -eq 200
) "homes=$($mgrDash.Json.totalCareHomes) cl2=$($mgrCl2.Json.items.Count)"

# --- TC-251 Company isolation ---
$x251 = Invoke-Json GET "/api/companies/$($dataB.CompanyId)" $null $ta
Add-Result 'TC-251' 'Tenant A cannot access Tenant B Company' ($x251.Status -eq 404) "status=$($x251.Status)"

# --- TC-252 Care Home isolation ---
$x252 = Invoke-Json GET "/api/care-homes/$($dataB.Home1Id)" $null $ta
Add-Result 'TC-252' 'Tenant A cannot access Tenant B Care Home' ($x252.Status -eq 404) "status=$($x252.Status)"

# --- TC-253 Client isolation ---
$x253 = Invoke-Json GET "/api/clients/$($dataB.ClientId)" $null $ta
Add-Result 'TC-253' 'Tenant A cannot access Tenant B Client' ($x253.Status -eq 404) "status=$($x253.Status)"

# --- TC-254 Funding Authority isolation ---
$x254 = Invoke-Json GET "/api/funding-authorities/$($dataB.FaId)" $null $ta
Add-Result 'TC-254' 'Tenant A cannot access Tenant B Funding Authority' ($x254.Status -eq 404) "status=$($x254.Status)"

# --- TC-255 Nominal Code isolation ---
$x255 = Invoke-Json GET "/api/nominal-codes/$($dataB.NomId)" $null $ta
Add-Result 'TC-255' 'Tenant A cannot access Tenant B Nominal Code' ($x255.Status -eq 404) "status=$($x255.Status)"

# --- TC-256 Funding Contract isolation ---
$x256 = Invoke-Json GET "/api/funding-contracts/$($dataB.ContractId)" $null $ta
Add-Result 'TC-256' 'Tenant A cannot access Tenant B Funding Contract' ($x256.Status -eq 404) "status=$($x256.Status)"

# --- TC-257 Invoice isolation ---
$x257 = Invoke-Json GET "/api/invoices/$($dataB.InvoiceId)" $null $ta
Add-Result 'TC-257' 'Tenant A cannot access Tenant B Invoice' ($x257.Status -eq 404) "status=$($x257.Status)"

# --- TC-258 Invoice PDF isolation ---
$x258 = Invoke-Binary "/api/invoices/$($dataB.InvoiceId)/pdf" $ta
Add-Result 'TC-258' 'Tenant A cannot access Tenant B Invoice PDF' ($x258.Status -eq 404) "status=$($x258.Status)"

# --- TC-259 Credit Note isolation ---
$x259 = Invoke-Json GET "/api/credit-notes/$($dataB.CreditNoteId)" $null $ta
$x259p = Invoke-Binary "/api/credit-notes/$($dataB.CreditNoteId)/pdf" $ta
Add-Result 'TC-259' 'Tenant A cannot access Tenant B Credit Note' ($x259.Status -eq 404 -and $x259p.Status -eq 404) "get=$($x259.Status) pdf=$($x259p.Status)"

# --- TC-260 Report isolation ---
$rep260 = Invoke-Json GET '/api/reports/client-census' $null $ta
Add-Result 'TC-260' 'Tenant A cannot access Tenant B Report data' (
    (@($rep260.Json) | Where-Object { $_.clientName -match 'TENANT_B_MARKER' }).Count -eq 0
) "rows=$(@($rep260.Json).Count)"

# --- TC-261 Sage Export isolation ---
$x261 = Invoke-Binary "/api/sage-exports/$($dataB.SageBatchId)/file" $ta
Add-Result 'TC-261' 'Tenant A cannot access Tenant B Sage Export' ($x261.Status -eq 404) "status=$($x261.Status)"

# --- TC-262 User isolation ---
$x262 = Invoke-Json GET "/api/users/$($dataB.MarkerUserId)" $null $ta
Add-Result 'TC-262' 'Tenant A cannot access Tenant B User' ($x262.Status -eq 404) "status=$($x262.Status) targetId=$($dataB.MarkerUserId)"

# --- TC-263 Audit Log isolation ---
$auditA = Invoke-Json GET '/api/audit?pageSize=200' $null $ta
$auditEntityIds = @($auditA.Json.items | ForEach-Object { $_.entityId })
Add-Result 'TC-263' 'Tenant A cannot access Tenant B Audit Log' (
    -not ($auditEntityIds -contains $dataB.CompanyId.ToString()) -and
    -not ($auditEntityIds -contains $dataB.MarkerUserId)
) "tenantBCompanyInA=$($auditEntityIds -contains $dataB.CompanyId.ToString())"

# --- TC-264 Organisation Settings isolation ---
$setA = Invoke-Json GET '/api/settings/organisation' $null $ta
$setB = Invoke-Json GET '/api/settings/organisation' $null $tb
Add-Result 'TC-264' 'Tenant A cannot access Tenant B Organisation Settings' (
    $setA.Json.name -ne $setB.Json.name -and $setA.Json.name -match 'QA Users A'
) "a=$($setA.Json.name) b=$($setB.Json.name)"

# --- TC-265 Tenant-scoped uniqueness ---
$uniCoA = Invoke-Json POST '/api/companies' (@{ name = 'SharedCo UNI' } | ConvertTo-Json -Compress) $ta
$uniCoB = Invoke-Json POST '/api/companies' (@{ name = 'SharedCo UNI' } | ConvertTo-Json -Compress) $tb
$uniHomeA = Invoke-Json POST '/api/care-homes' (@{
    companyId = $uniCoA.Json.id; code = 'UNI01'; name = 'Uni Home'; bedCapacity = 5; managerName = 'M'
} | ConvertTo-Json -Compress) $ta
$uniHomeB = Invoke-Json POST '/api/care-homes' (@{
    companyId = $uniCoB.Json.id; code = 'UNI01'; name = 'Uni Home'; bedCapacity = 5; managerName = 'M'
} | ConvertTo-Json -Compress) $tb
$uniFaA = Invoke-Json POST '/api/funding-authorities' (@{
    code = 'UNIAUTH'; name = 'Uni Auth'; type = 'Council'; billingFrequency = 'Weekly'
} | ConvertTo-Json -Compress) $ta
$uniFaB = Invoke-Json POST '/api/funding-authorities' (@{
    code = 'UNIAUTH'; name = 'Uni Auth'; type = 'Council'; billingFrequency = 'Weekly'
} | ConvertTo-Json -Compress) $tb
$uniNomA = Invoke-Json POST '/api/nominal-codes' (@{ code = 'UNI4000'; name = 'Uni Nom' } | ConvertTo-Json -Compress) $ta
$uniNomB = Invoke-Json POST '/api/nominal-codes' (@{ code = 'UNI4000'; name = 'Uni Nom' } | ConvertTo-Json -Compress) $tb
$uniClA = Invoke-Json POST '/api/clients' (@{
    careHomeId = $uniHomeA.Json.id; sageId = 'UNISAGE'; referenceNumber = 'UNIREF'
    firstName = 'Uni'; lastName = 'Client'; careType = 'Residential'; admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta
$uniClB = Invoke-Json POST '/api/clients' (@{
    careHomeId = $uniHomeB.Json.id; sageId = 'UNISAGE'; referenceNumber = 'UNIREF'
    firstName = 'Uni'; lastName = 'Client'; careType = 'Residential'; admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $tb
Add-Result 'TC-265' 'Tenant-scoped uniqueness' (
    $uniCoA.Status -eq 201 -and $uniCoB.Status -eq 201 -and
    $uniHomeA.Status -eq 201 -and $uniHomeB.Status -eq 201 -and
    $uniFaA.Status -eq 201 -and $uniFaB.Status -eq 201 -and
    $uniNomA.Status -eq 201 -and $uniNomB.Status -eq 201 -and
    $uniClA.Status -eq 201 -and $uniClB.Status -eq 201
) "allCreated=true"

# --- Summary ---
$results | Format-Table -AutoSize
$pass = ($results | Where-Object Result -eq 'PASS').Count
$fail = ($results | Where-Object Result -eq 'FAIL').Count
Write-Host "`nSUMMARY: $pass PASS, $fail FAIL of $($results.Count) tests"
if ($fail -gt 0) { exit 1 }

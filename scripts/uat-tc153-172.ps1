# UAT TC-153 through TC-172 API tests (Invoices, Detail, Snapshot, Payment)
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5092'
$results = @()
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$pastDob = '1980-01-15'

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
    $lines = $raw -split "`n"
    return @{ Status = [int]$lines[-1]; Content = ($lines[0..($lines.Length - 2)] -join "`n"); Json = $(if ($lines.Length -gt 1 -and $lines[0]) { try { $lines[0..($lines.Length - 2)] -join "`n" | ConvertFrom-Json } catch { $null } } else { $null }) }
}

function Invoke-Raw($Method, $Path, $Token = $null) {
    $args = @('-s', '-w', "`n%{http_code}", '-X', $Method, "$base$Path")
    if ($Token) { $args += @('-H', "Authorization: Bearer $Token") }
    $raw = & curl.exe @args
    $lines = $raw -split "`n"
    return @{ Status = [int]$lines[-1]; Content = ($lines[0..($lines.Length - 2)] -join "`n") }
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
    return @{ Token = (Login $email 'QaTenantAdmin!99'); TenantId = $c.Json.id }
}

function Get-CategoryId($token, $code) {
    return ((Invoke-Json GET '/api/invoice-categories' $null $token).Json | Where-Object { $_.code -eq $code } | Select-Object -First 1).id
}

function New-Company($token, $name) {
    return (Invoke-Json POST '/api/companies' (@{ name = $name } | ConvertTo-Json -Compress) $token).Json
}

function New-CareHome($token, $companyId, $code, $name) {
    return (Invoke-Json POST '/api/care-homes' (@{
        companyId = $companyId; code = $code; name = $name; bedCapacity = 20; managerName = 'QA Manager'
    } | ConvertTo-Json -Compress) $token).Json
}

function New-Client($token, $careHomeId, $sageId, $ref, $first, $last) {
    return (Invoke-Json POST '/api/clients' (@{
        careHomeId = $careHomeId; sageId = $sageId; referenceNumber = $ref
        firstName = $first; lastName = $last; careType = 'Residential'
        admissionDate = '2026-01-01'; dateOfBirth = $pastDob
    } | ConvertTo-Json -Compress) $token).Json
}

function New-FA($token, $code, $name) {
    return (Invoke-Json POST '/api/funding-authorities' (@{ code = $code; name = $name; type = 'Council'; billingFrequency = 'Weekly' } | ConvertTo-Json -Compress) $token).Json
}

function New-Nominal($token, $code, $name) {
    return (Invoke-Json POST '/api/nominal-codes' (@{ code = $code; name = $name } | ConvertTo-Json -Compress) $token).Json
}

function New-Template($token, $catId, $bankAccount = 'QA Bank', $accountNumber = '12345678') {
    return (Invoke-Json POST '/api/invoice-templates' (@{
        name = 'Invoice Template'; invoiceCategoryId = $catId
        bankAccountName = $bankAccount; sortCode = '12-34-56'; accountNumber = $accountNumber
        contactEmail = 'finance@qa.test'
    } | ConvertTo-Json -Compress) $token).Json
}

function New-Contract($token, $clientId, $faId, $catId, $nomId) {
    return (Invoke-Json POST "/api/clients/$clientId/funding-contracts" (@{
        fundingAuthorityId = $faId; invoiceCategoryId = $catId; nominalCodeId = $nomId; contractStartDate = '2026-01-01'
    } | ConvertTo-Json -Compress) $token).Json
}

function Add-Rate($token, $contractId, $amount) {
    Invoke-Json POST "/api/funding-contracts/$contractId/rates" (@{
        effectiveFrom = '2026-01-01'; frequency = 'Weekly'; amount = $amount
    } | ConvertTo-Json -Compress) $token | Out-Null
}

function Billing-Generate($token, $companyId, $careHomeId, $start, $end, $clientIds, $categoryId) {
    return Invoke-Json POST '/api/billing/generate' (@{
        companyId = $companyId; careHomeId = $careHomeId; periodStart = $start; periodEnd = $end
        clientIds = @($clientIds); invoiceCategoryId = $categoryId
    } | ConvertTo-Json -Compress) $token
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

function Invoke-Sql($query) {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { return $null }
    $out = & sqlcmd -S '(localdb)\MSSQLLocalDB' -d CareHomeDb -Q $query -h -1 -W 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return ($out | Where-Object { $_.Trim() -ne '' } | Select-Object -First 1)
}

function Setup-BillableClient($token, $homeId, $faId, $catId, $nom, $sageId, $ref, $first, $last) {
    $cl = New-Client $token $homeId $sageId $ref $first $last
    $fc = New-Contract $token $cl.id $faId $catId $nom.id
    Add-Rate $token $fc.id 575
    return $cl
}

# --- Bootstrap ---
$pt = Login 'admin@localhost' 'DevAdmin!12345'
if (-not $pt) { throw 'Platform login failed' }
$tenA = Provision-Tenant $pt "QA Invoices A $ts" "qa-inv-a-$ts@uat.test"
$tenB = Provision-Tenant $pt "QA Invoices B $ts" "qa-inv-b-$ts@uat.test"
$ta = $tenA.Token
$tb = $tenB.Token

$co = New-Company $ta 'QA Care Ltd'
$coId = $co.id
$careHome = New-CareHome $ta $coId 'HOME01' 'Oak Lodge'
$homeId = $careHome.id
$faEast = New-FA $ta 'EASTCC' 'East County Council'
$faWest = New-FA $ta 'WESTCC' 'West County Council'
$catGeneral = Get-CategoryId $ta 'GENERAL_CARE'
$nom = New-Nominal $ta '4000' 'Care Revenue'
$tpl = New-Template $ta $catGeneral 'QA Bank Account' '12345678'

$clients = @()
foreach ($n in 1..3) {
    $clients += Setup-BillableClient $ta $homeId $faEast.id $catGeneral $nom "SAGE00$n" "CLIENT00$n" "Client$n" "Line"
}

# May 2026 grouped invoice (3 lines)
$genMay = Billing-Generate $ta $coId $homeId '2026-05-01' '2026-05-31' ($clients | ForEach-Object { $_.id }) $catGeneral
$invMayId = [int]$genMay.Json.invoiceIds[0]

# Jun 2026 invoice with different authority for filter variety
$clJun = Setup-BillableClient $ta $homeId $faWest.id $catGeneral $nom 'SAGE-JUN' 'CLIENT-JUN' 'June' 'Client'
$genJun = Billing-Generate $ta $coId $homeId '2026-06-01' '2026-06-30' @($clJun.id) $catGeneral
$invJunId = [int]$genJun.Json.invoiceIds[0]
$payUpd = Invoke-Json POST "/api/invoices/$invJunId/payment-status" (@{ paymentStatus = 'Paid' } | ConvertTo-Json -Compress) $ta

$listAll = Invoke-Json GET '/api/invoices?pageSize=50' $null $ta
$invMay = Invoke-Json GET "/api/invoices/$invMayId" $null $ta
$invMayId = [int]$invMay.Json.id
$invNum = $invMay.Json.invoiceNumber

# --- TC-153 List loads ---
Add-Result 'TC-153' 'Invoice list loads' ($listAll.Status -eq 200 -and $listAll.Json.items.Count -ge 2) "status=$($listAll.Status); count=$($listAll.Json.items.Count)"

# --- TC-154 Search invoice number ---
$f154 = Invoke-Json GET "/api/invoices?invoiceNumber=$invNum" $null $ta
$f154b = Invoke-Json GET "/api/invoices?invoiceNumber=$invNum" $null $tb
Add-Result 'TC-154' 'Filter invoice number' (
    $f154.Json.items.Count -eq 1 -and $f154.Json.items[0].invoiceNumber -eq $invNum -and $f154b.Json.items.Count -eq 0
) "A=$($f154.Json.items.Count) B=$($f154b.Json.items.Count)"

# --- TC-155 Filter company ---
$f155 = Invoke-Json GET "/api/invoices?companyId=$coId" $null $ta
Add-Result 'TC-155' 'Filter company' (
    $f155.Json.items.Count -ge 2 -and ($f155.Json.items | Where-Object { $_.companyName -ne 'QA Care Ltd' }).Count -eq 0
) "count=$($f155.Json.items.Count)"

# --- TC-156 Filter care home ---
$f156 = Invoke-Json GET "/api/invoices?careHomeId=$homeId" $null $ta
Add-Result 'TC-156' 'Filter care home' (
    $f156.Json.items.Count -ge 2 -and ($f156.Json.items | Where-Object { $_.careHomeName -ne 'Oak Lodge' }).Count -eq 0
) "count=$($f156.Json.items.Count)"

# --- TC-157 Filter authority ---
$f157 = Invoke-Json GET "/api/invoices?fundingAuthorityId=$($faEast.id)" $null $ta
Add-Result 'TC-157' 'Filter authority' (
    $f157.Json.items.Count -eq 1 -and $f157.Json.items[0].fundingAuthorityName -eq 'East County Council'
) "count=$($f157.Json.items.Count)"

# --- TC-158 Filter category ---
$f158 = Invoke-Json GET "/api/invoices?categoryId=$catGeneral" $null $ta
Add-Result 'TC-158' 'Filter category' (
    $f158.Json.items.Count -ge 2 -and ($f158.Json.items | Where-Object { $_.invoiceCategoryName -notmatch 'General' }).Count -eq 0
) "count=$($f158.Json.items.Count)"

# --- TC-159 Filter status ---
$f159 = Invoke-Json GET '/api/invoices?status=Generated' $null $ta
Add-Result 'TC-159' 'Filter status Generated' (
    $f159.Json.items.Count -ge 1 -and ($f159.Json.items | Where-Object { $_.status -ne 'Generated' }).Count -eq 0
) "count=$($f159.Json.items.Count)"

# --- TC-160 Filter payment status ---
$f160 = Invoke-Json GET '/api/invoices?paymentStatus=NotPaid' $null $ta
$mayNotPaid = @($f160.Json.items | Where-Object { $_.id -eq $invMayId })
$junNotPaid = @($f160.Json.items | Where-Object { $_.id -eq $invJunId })
Add-Result 'TC-160' 'Filter payment status NotPaid' (
    $payUpd.Status -eq 200 -and $mayNotPaid.Count -eq 1 -and $junNotPaid.Count -eq 0
) "payUpd=$($payUpd.Status) mayListed=$($mayNotPaid.Count) junListed=$($junNotPaid.Count)"

# --- TC-161 Filter date range ---
$f161 = Invoke-Json GET '/api/invoices?from=2026-05-01&to=2026-05-31' $null $ta
$mayInRange = @($f161.Json.items | Where-Object { $_.id -eq $invMayId })
$junInRange = @($f161.Json.items | Where-Object { $_.id -eq $invJunId })
Add-Result 'TC-161' 'Filter date range May 2026' (
    $mayInRange.Count -eq 1 -and $junInRange.Count -eq 0
) "count=$($f161.Json.items.Count) may=$($mayInRange.Count) jun=$($junInRange.Count)"

# --- TC-162 Invoice header fields ---
$hdr = $invMay.Json
$hdrOk = $hdr.invoiceNumber -and $hdr.invoiceDate -and $hdr.dueDate -and $hdr.periodStart -and $hdr.periodEnd `
    -and $hdr.companyName -eq 'QA Care Ltd' -and $hdr.careHomeName -eq 'Oak Lodge' `
    -and $hdr.fundingAuthorityName -eq 'East County Council' -and $hdr.invoiceCategoryName `
    -and $hdr.status -and $hdr.paymentStatus -and $null -ne $hdr.totalAmount
Add-Result 'TC-162' 'Invoice header fields complete' $hdrOk "number=$($hdr.invoiceNumber) total=$($hdr.totalAmount)"

# --- TC-163 Invoice line fields ---
$ln = $hdr.lines[0]
$lnOk = $ln.clientName -and $ln.clientReference -and $ln.sageId -and $ln.description `
    -and $ln.servicePeriodStart -and $ln.servicePeriodEnd -and $ln.eligibleDays -gt 0 `
    -and $ln.rateAmount -gt 0 -and $ln.rateFrequency -and $ln.nominalCode -and $ln.lineAmount -gt 0
Add-Result 'TC-163' 'Invoice line fields complete' ($hdr.lines.Count -eq 3 -and $lnOk) "lines=$($hdr.lines.Count)"

# --- TC-164 Total equals line sum ---
$lineSum = [Math]::Round(($hdr.lines | ForEach-Object { [decimal]$_.lineAmount } | Measure-Object -Sum).Sum, 2)
Add-Result 'TC-164' 'Invoice total equals line sum' ($lineSum -eq [decimal]$hdr.totalAmount) "total=$($hdr.totalAmount) sum=$lineSum"

# --- TC-165 Non-existent invoice ---
$missing = Invoke-Json GET '/api/invoices/999999' $null $ta
Add-Result 'TC-165' 'Non-existent invoice 404' ($missing.Status -eq 404) "status=$($missing.Status)"

# --- TC-166/167 ReadOnly security ---
$roCreate = Invoke-Json POST '/api/users' (@{
    email = "ro-$ts@uat.test"; displayName = 'Read Only'; password = 'ReadOnlyPass!12345'
    role = 'ReadOnly'
} | ConvertTo-Json -Compress) $ta
$ro = Login "ro-$ts@uat.test" 'ReadOnlyPass!12345'
$roGet = Invoke-Json GET "/api/invoices/$invMayId" $null $ro
$roPdf = Invoke-Raw GET "/api/invoices/$invMayId/pdf" $ro
$roVoid = Invoke-Json POST "/api/invoices/$invJunId/void" $null $ro
$roPay = Invoke-Json POST "/api/invoices/$invMayId/payment-status" (@{ paymentStatus = 'Paid' } | ConvertTo-Json -Compress) $ro
$roSend = Invoke-Json POST "/api/invoices/$invMayId/send" $null $ro
$roGen = Invoke-Json POST '/api/billing/generate' (@{
    companyId = $coId; careHomeId = $homeId; periodStart = '2026-07-01'; periodEnd = '2026-07-07'
    clientIds = @($clients[0].id); invoiceCategoryId = $catGeneral
} | ConvertTo-Json -Compress) $ro
Add-Result 'TC-166' 'ReadOnly read access allowed (API)' (
    $roCreate.Status -eq 201 -and $ro -and $roGet.Status -eq 200 -and $roPdf.Status -eq 200 -and $roPdf.Content.StartsWith('%PDF')
) "create=$($roCreate.Status) get=$($roGet.Status) pdf=$($roPdf.Status)"
Add-Result 'TC-167' 'ReadOnly write APIs rejected' (
    $roVoid.Status -eq 403 -and $roPay.Status -eq 403 -and $roSend.Status -eq 403 -and $roGen.Status -eq 403
) "void=$($roVoid.Status) pay=$($roPay.Status) send=$($roSend.Status) gen=$($roGen.Status)"

# --- TC-168 Client rename snapshot ---
$origName = $hdr.lines[0].clientName
$origSage = $hdr.lines[0].sageId
$client0 = Invoke-Json GET "/api/clients/$($clients[0].id)" $null $ta
Update-Client $ta $clients[0].id $client0.Json @{ firstName = 'Renamed'; lastName = 'Client' } | Out-Null
$after168 = Invoke-Json GET "/api/invoices/$invMayId" $null $ta
Add-Result 'TC-168' 'Client rename does not alter invoice' (
    $after168.Json.lines[0].clientName -eq $origName
) "snapshot=$($after168.Json.lines[0].clientName) live=Renamed Client"

# --- TC-169 Sage ID change snapshot ---
Update-Client $ta $clients[0].id (Invoke-Json GET "/api/clients/$($clients[0].id)" $null $ta).Json @{ sageId = 'SAGE999' } | Out-Null
$after169 = Invoke-Json GET "/api/invoices/$invMayId" $null $ta
Add-Result 'TC-169' 'Sage ID change does not alter invoice' (
    $after169.Json.lines[0].sageId -eq $origSage
) "snapshot=$($after169.Json.lines[0].sageId) live=SAGE999"

# --- TC-170 Master data rename snapshot ---
Invoke-Json PUT "/api/companies/$coId" (@{ name = 'Renamed Company Ltd'; isActive = $true } | ConvertTo-Json -Compress) $ta | Out-Null
$homeGet = Invoke-Json GET "/api/care-homes/$homeId" $null $ta
Invoke-Json PUT "/api/care-homes/$homeId" (@{
    companyId = $coId; code = $homeGet.Json.code; name = 'Renamed Lodge'; bedCapacity = 20; isActive = $true
} | ConvertTo-Json -Compress) $ta | Out-Null
$faGet = Invoke-Json GET "/api/funding-authorities/$($faEast.id)" $null $ta
Invoke-Json PUT "/api/funding-authorities/$($faEast.id)" (@{
    code = $faGet.Json.code; name = 'Renamed Council'; type = $faGet.Json.type
    billingFrequency = $faGet.Json.billingFrequency; billingIntervalDays = $faGet.Json.billingIntervalDays
    isActive = $true
} | ConvertTo-Json -Compress) $ta | Out-Null
$catGet = (Invoke-Json GET '/api/invoice-categories' $null $ta).Json | Where-Object { $_.id -eq $catGeneral } | Select-Object -First 1
Invoke-Json PUT "/api/invoice-categories/$catGeneral" (@{
    code = $catGet.code; name = 'Renamed Category'; description = $catGet.description; isActive = $true
} | ConvertTo-Json -Compress) $ta | Out-Null
$after170 = Invoke-Json GET "/api/invoices/$invMayId" $null $ta
Add-Result 'TC-170' 'Master data rename does not alter invoice' (
    $after170.Json.companyName -eq 'QA Care Ltd' -and $after170.Json.careHomeName -eq 'Oak Lodge' `
    -and $after170.Json.fundingAuthorityName -eq 'East County Council' -and $after170.Json.invoiceCategoryName -match 'General'
) "company=$($after170.Json.companyName) home=$($after170.Json.careHomeName)"

# --- TC-171 Template/bank snapshot ---
$bankBefore = Invoke-Sql "SELECT SnapshotAccountNumber FROM Invoices WHERE Id=$invMayId"
Invoke-Json PUT "/api/invoice-templates/$($tpl.id)" (@{
    name = 'Updated Template'; invoiceCategoryId = $catGeneral
    bankAccountName = 'Changed Bank'; sortCode = '99-99-99'; accountNumber = '99999999'
    contactEmail = 'changed@qa.test'; isActive = $true
} | ConvertTo-Json -Compress) $ta | Out-Null
$bankAfter = Invoke-Sql "SELECT SnapshotAccountNumber FROM Invoices WHERE Id=$invMayId"
$pdfAfter = Invoke-Raw GET "/api/invoices/$invMayId/pdf" $ta
Add-Result 'TC-171' 'Template bank change does not alter invoice snapshot' (
    $bankBefore -eq '12345678' -and $bankAfter -eq '12345678' -and $pdfAfter.Status -eq 200
) "before=$bankBefore after=$bankAfter pdf=$($pdfAfter.Status)"

# --- TC-172 Void invoice ---
$voidTarget = $invJunId
$beforeVoid = Invoke-Json GET "/api/invoices/$voidTarget" $null $ta
$voidRes = Invoke-Json POST "/api/invoices/$voidTarget/void" $null $ta
$afterVoid = Invoke-Json GET "/api/invoices/$voidTarget" $null $ta
$stillListed = @((Invoke-Json GET '/api/invoices?pageSize=50' $null $ta).Json.items | Where-Object { $_.id -eq $voidTarget }).Count -eq 1
Add-Result 'TC-172' 'Void finalized invoice retained in history' (
    $voidRes.Status -eq 200 -and $afterVoid.Json.status -eq 'Void' -and $stillListed -and $beforeVoid.Json.invoiceNumber
) "status=$($afterVoid.Json.status) listed=$stillListed"

# --- Summary ---
$results | Format-Table -AutoSize
$pass = ($results | Where-Object Result -eq 'PASS').Count
$fail = ($results | Where-Object Result -eq 'FAIL').Count
Write-Host "`nSUMMARY: $pass PASS, $fail FAIL of $($results.Count) tests"
if ($fail -gt 0) { exit 1 }

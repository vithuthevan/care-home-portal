# UAT TC-211 through TC-239 API tests (Reports, Sage Export, Dashboard)
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5092'
$results = @()
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$pastDob = '1980-01-15'

function Add-Result($tc, $name, $passed, $detail) {
    $script:results += [pscustomobject]@{ TC = $tc; Name = $name; Result = $(if ($passed) { 'PASS' } else { 'FAIL' }); Detail = $detail }
}

function Add-Skip($tc, $name, $detail) {
    $script:results += [pscustomobject]@{ TC = $tc; Name = $name; Result = 'SKIP'; Detail = $detail }
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
    return @{ Token = (Login $email 'QaTenantAdmin!99'); TenantId = $c.Json.id }
}

function Get-CategoryId($token, $code) {
    return ((Invoke-Json GET '/api/invoice-categories' $null $token).Json | Where-Object { $_.code -eq $code } | Select-Object -First 1).id
}

function New-Client($token, $careHomeId, $sageId, $ref, $first, $last) {
    return (Invoke-Json POST '/api/clients' (@{
        careHomeId = $careHomeId; sageId = $sageId; referenceNumber = $ref
        firstName = $first; lastName = $last; careType = 'Residential'
        admissionDate = '2026-01-01'; dateOfBirth = $pastDob
    } | ConvertTo-Json -Compress) $token).Json
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

function Billing-Generate($token, $companyId, $careHomeId, $start, $end, $clientIds, $categoryId) {
    return Invoke-Json POST '/api/billing/generate' (@{
        companyId = $companyId; careHomeId = $careHomeId
        periodStart = $start; periodEnd = $end; clientIds = @($clientIds); invoiceCategoryId = $categoryId
    } | ConvertTo-Json -Compress) $token
}

function Sage-Preview($token, $companyId, $from, $to, $includeExported = $false) {
    return Invoke-Json POST '/api/sage-exports/preview' (@{
        dateFrom = $from; dateTo = $to; companyId = $companyId; includeAlreadyExported = $includeExported
    } | ConvertTo-Json -Compress) $token
}

function Sage-Export($token, $companyId, $from, $to, $includeExported = $false) {
    return Invoke-Json POST '/api/sage-exports' (@{
        dateFrom = $from; dateTo = $to; companyId = $companyId; includeAlreadyExported = $includeExported
    } | ConvertTo-Json -Compress) $token
}

function Invoke-Sql($query) {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { return $null }
    $out = & sqlcmd -S '(localdb)\MSSQLLocalDB' -d CareHomeDb -Q $query -h -1 -W 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return ($out | Where-Object { $_.Trim() -ne '' })
}

function Count-CsvField($csvText, $fieldIndex, $value) {
    $lines = ($csvText -split "`r?`n") | Where-Object { $_.Trim() -ne '' }
    if ($lines.Count -le 1) { return 0 }
    $count = 0
    for ($i = 1; $i -lt $lines.Count; $i++) {
        $cols = $lines[$i] -split ','
        if ($cols.Length -gt $fieldIndex -and $cols[$fieldIndex].Trim('"') -eq $value) { $count++ }
    }
    return $count
}

# --- Bootstrap ---
$pt = Login 'admin@localhost' 'DevAdmin!12345'
if (-not $pt) { throw 'Platform login failed' }
$taObj = Provision-Tenant $pt "QA Reports A $ts" "qa-rpt-a-$ts@uat.test"
$tbObj = Provision-Tenant $pt "QA Reports B $ts" "qa-rpt-b-$ts@uat.test"
$ta = $taObj.Token
$tb = $tbObj.Token
$tenantAId = $taObj.TenantId

$co = (Invoke-Json POST '/api/companies' (@{ name = "Green Valley $ts" } | ConvertTo-Json -Compress) $ta).Json
$home1 = (Invoke-Json POST '/api/care-homes' (@{
    companyId = $co.id; code = 'HOME01'; name = 'Oak Lodge'; bedCapacity = 30; managerName = 'Mgr'
} | ConvertTo-Json -Compress) $ta).Json
$home2 = (Invoke-Json POST '/api/care-homes' (@{
    companyId = $co.id; code = 'HOME02'; name = 'Pine House'; bedCapacity = 12; managerName = 'Mgr2'
} | ConvertTo-Json -Compress) $ta).Json
$fa = (Invoke-Json POST '/api/funding-authorities' (@{
    code = 'COUNCIL01'; name = 'East County Council'; type = 'Council'; billingFrequency = 'Weekly'; email = 'council@qa.test'
} | ConvertTo-Json -Compress) $ta).Json
$cat = Get-CategoryId $ta 'GENERAL_CARE'
$nom = (Invoke-Json POST '/api/nominal-codes' (@{ code = '4000'; name = 'Care Fees' } | ConvertTo-Json -Compress) $ta).Json
Invoke-Json POST '/api/invoice-templates' (@{
    name = 'Default'; invoiceCategoryId = $cat; contactEmail = 'finance@qa.test'
    bankAccountName = 'Bank'; sortCode = '12-34-56'; accountNumber = '12345678'
} | ConvertTo-Json -Compress) $ta | Out-Null

$clientIds = @()
foreach ($pair in @(
    @('SAGE001', 'CLIENT001', 'Alice', 'Brown'),
    @('SAGE002', 'CLIENT002', 'David', 'Smith'),
    @('SAGE003', 'CLIENT003', 'Mary', 'Jones')
)) {
    $cl = New-Client $ta $home1.id $pair[0] $pair[1] $pair[2] $pair[3]
    $fc = (Invoke-Json POST "/api/clients/$($cl.id)/funding-contracts" (@{
        fundingAuthorityId = $fa.id; invoiceCategoryId = $cat; nominalCodeId = $nom.id; contractStartDate = '2026-01-01'
    } | ConvertTo-Json -Compress) $ta).Json
    Invoke-Json POST "/api/funding-contracts/$($fc.id)/rates" (@{
        effectiveFrom = '2026-01-01'; frequency = 'Weekly'; amount = 575
    } | ConvertTo-Json -Compress) $ta | Out-Null
    $clientIds += $cl.id
}

$home2Client = New-Client $ta $home2.id 'SAGE-H2' 'CLIENT-H2' 'Hidden' 'HomeTwo'
$fc2 = (Invoke-Json POST "/api/clients/$($home2Client.id)/funding-contracts" (@{
    fundingAuthorityId = $fa.id; invoiceCategoryId = $cat; nominalCodeId = $nom.id; contractStartDate = '2026-01-01'
} | ConvertTo-Json -Compress) $ta).Json
Invoke-Json POST "/api/funding-contracts/$($fc2.id)/rates" (@{
    effectiveFrom = '2026-01-01'; frequency = 'Weekly'; amount = 575
} | ConvertTo-Json -Compress) $ta | Out-Null

$genMay = Billing-Generate $ta $co.id $home1.id '2026-05-01' '2026-05-31' $clientIds $cat
if ($genMay.Status -ne 200) { throw "May billing failed: $($genMay.Content)" }
$invId = [int]$genMay.Json.invoiceIds[0]
$inv = (Invoke-Json GET "/api/invoices/$invId" $null $ta).Json
$contractId = (Invoke-Json GET "/api/clients/$($clientIds[0])/funding-contracts" $null $ta).Json[0].id

# Trigger billing exception log (exceptions persist on generate attempt)
$noContract = New-Client $ta $home1.id 'SAGE-NC' 'CLIENT-NC' 'No' 'Contract'
Billing-Generate $ta $co.id $home1.id '2026-06-01' '2026-06-30' @($noContract.id) $cat | Out-Null

# Tenant B marker
$coB = (Invoke-Json POST '/api/companies' (@{ name = "Tenant B Co $ts" } | ConvertTo-Json -Compress) $tb).Json
$homeB = (Invoke-Json POST '/api/care-homes' (@{
    companyId = $coB.id; code = 'TBHOME'; name = 'TENANT_B_MARKER_HOME'; bedCapacity = 5; managerName = 'B'
} | ConvertTo-Json -Compress) $tb).Json
New-Client $tb $homeB.id 'SAGE-B' 'TENANT_B_MARKER' 'Tenant' 'BOnly' | Out-Null

# --- TC-211 Occupancy ---
$occ = Invoke-Json GET "/api/reports/occupancy?companyId=$($co.id)" $null $ta
$occRow = $occ.Json | Where-Object { $_.careHomeName -eq 'Oak Lodge' } | Select-Object -First 1
Add-Result 'TC-211' 'Occupancy report loads' (
    $occ.Status -eq 200 -and $occRow -and $occRow.currentClients -eq 4 -and $occRow.capacity -eq 30
) "clients=$($occRow.currentClients) cap=$($occRow.capacity)"

# --- TC-212 Income by Category ---
$inc = Invoke-Json GET '/api/reports/income-by-category?from=2026-05-01&to=2026-05-31' $null $ta
Add-Result 'TC-212' 'Income by Category report loads' (
    $inc.Status -eq 200 -and $inc.Json.Count -ge 1 -and ([decimal]$inc.Json[0].amount -eq 7639.29)
) "amount=$($inc.Json[0].amount)"

# --- TC-213 Invoice Status / Outstanding ---
$out = Invoke-Json GET '/api/reports/outstanding' $null $ta
$outRows = @($out.Json)
Add-Result 'TC-213' 'Invoice Status report loads' (
    $out.Status -eq 200 -and $outRows.Count -ge 1 -and $outRows[0].paymentStatus -eq 'NotPaid' -and $outRows[0].amount -eq 7639.29
) "rows=$($outRows.Count) inv=$($outRows[0].invoiceNumber)"

# --- TC-214 Client Census ---
$census = Invoke-Json GET "/api/reports/client-census?careHomeId=$($home1.id)" $null $ta
Add-Result 'TC-214' 'Client Census report loads' (
    $census.Status -eq 200 -and ($census.Json | Where-Object { $_.careHomeName -eq 'Oak Lodge' }).Count -ge 4
) "rows=$($census.Json.Count)"

# --- TC-215 Funding Rate History ---
$rates = Invoke-Json GET "/api/reports/rate-history?contractId=$contractId" $null $ta
Add-Result 'TC-215' 'Funding Rate History report loads' (
    $rates.Status -eq 200 -and $rates.Json.Count -ge 1 -and [decimal]$rates.Json[0].amount -eq 575
) "rows=$($rates.Json.Count) amt=$($rates.Json[0].amount)"

# --- TC-216 Billing Exceptions ---
$exc = Invoke-Json GET '/api/reports/billing-exceptions' $null $ta
$excRows = @($exc.Json)
Add-Result 'TC-216' 'Billing Exceptions report loads' (
    $exc.Status -eq 200 -and $excRows.Count -ge 1 -and $excRows[0].message -match 'contract|billing|invoice'
) "rows=$($excRows.Count) code=$($excRows[0].code)"

# --- TC-217 Report totals reconcile ---
$manualTotal = [decimal]$inv.totalAmount
$reportTotal = ([decimal]($inc.Json | ForEach-Object { [decimal]$_.amount } | Measure-Object -Sum).Sum)
Add-Result 'TC-217' 'Report totals reconcile' ($manualTotal -eq 7639.29 -and $reportTotal -eq $manualTotal) "inv=$manualTotal report=$reportTotal"

# --- TC-218 Export Excel ---
$xlsx = Invoke-Binary '/api/reports/occupancy?format=xlsx' $ta
Add-Result 'TC-218' 'Export report to Excel' (
    $xlsx.Status -eq 200 -and $xlsx.Bytes.Length -gt 100 -and $xlsx.Bytes[0] -eq 0x50 -and $xlsx.Bytes[1] -eq 0x4B
) "bytes=$($xlsx.Bytes.Length)"

# --- TC-219 Export PDF ---
$pdf = Invoke-Binary '/api/reports/occupancy?format=pdf' $ta
Add-Result 'TC-219' 'Export report to PDF' (
    $pdf.Status -eq 200 -and $pdf.Text.StartsWith('%PDF')
) "bytes=$($pdf.Bytes.Length)"

# --- TC-220 Export CSV ---
$csv = Invoke-Binary '/api/reports/occupancy?format=csv' $ta
Add-Result 'TC-220' 'Export report to CSV' (
    $csv.Status -eq 200 -and $csv.Text -match 'CareHomeName' -and $csv.Text -match 'Oak Lodge'
) "bytes=$($csv.Bytes.Length)"

# --- TC-221 Tenant-scoped report ---
Add-Result 'TC-221' 'Tenant-scoped report' (
    ($occ.Json | Where-Object { $_.careHomeName -match 'TENANT_B_MARKER' }).Count -eq 0 -and
    ($census.Json | Where-Object { $_.referenceNumber -eq 'TENANT_B_MARKER' }).Count -eq 0
) 'no Tenant B markers in Tenant A reports'

# --- TC-222 ReadOnly report access ---
Invoke-Json POST '/api/users' (@{
    email = "ro-rpt-$ts@uat.test"; displayName = 'Read Only'; password = 'ReadOnlyPass!12345'; role = 'ReadOnly'
} | ConvertTo-Json -Compress) $ta | Out-Null
$ro = Login "ro-rpt-$ts@uat.test" 'ReadOnlyPass!12345'
$roOcc = Invoke-Json GET '/api/reports/occupancy' $null $ro
$roWrite = Invoke-Json POST '/api/billing/generate' (@{
    companyId = $co.id; careHomeId = $home1.id; periodStart = '2026-06-01'; periodEnd = '2026-06-30'
    clientIds = @($clientIds[0]); invoiceCategoryId = $cat
} | ConvertTo-Json -Compress) $ro
Add-Result 'TC-222' 'ReadOnly report access' (
    $roOcc.Status -eq 200 -and $roWrite.Status -eq 403
) "read=$($roOcc.Status) write=$($roWrite.Status)"

# --- TC-223 Sage preview ---
$sagePrev = Sage-Preview $ta $co.id '2026-05-01' '2026-05-31'
Add-Result 'TC-223' 'Sage preview' (
    $sagePrev.Status -eq 200 -and $sagePrev.Json.eligibleCount -eq 3 -and $sagePrev.Json.canExport -eq $true
) "eligible=$($sagePrev.Json.eligibleCount) canExport=$($sagePrev.Json.canExport)"

# --- TC-224 Missing Sage ID blocks export ---
$lineId = $inv.lines[0].id
$sqlOk = Invoke-Sql "UPDATE InvoiceLines SET SnapshotSageId='' WHERE Id=$lineId"
if ($sqlOk) {
    $sageBad = Sage-Preview $ta $co.id '2026-05-01' '2026-05-31'
    $sageBadExp = Sage-Export $ta $co.id '2026-05-01' '2026-05-31'
    Add-Result 'TC-224' 'Missing Sage ID blocks export' (
        $sageBad.Json.canExport -eq $false -and $sageBadExp.Status -eq 400
    ) "canExport=$($sageBad.Json.canExport) export=$($sageBadExp.Status)"
    Invoke-Sql "UPDATE InvoiceLines SET SnapshotSageId='SAGE001' WHERE Id=$lineId" | Out-Null
} else {
    Add-Skip 'TC-224' 'Missing Sage ID blocks export' 'sqlcmd unavailable'
}

# --- TC-225 Missing nominal blocks export ---
if ($sqlOk) {
    Invoke-Sql "UPDATE InvoiceLines SET SnapshotNominalCode='' WHERE Id=$lineId" | Out-Null
    $nomBad = Sage-Preview $ta $co.id '2026-05-01' '2026-05-31'
    $nomBadExp = Sage-Export $ta $co.id '2026-05-01' '2026-05-31'
    Add-Result 'TC-225' 'Missing nominal blocks export' (
        $nomBad.Json.canExport -eq $false -and $nomBadExp.Status -eq 400
    ) "canExport=$($nomBad.Json.canExport) export=$($nomBadExp.Status)"
    Invoke-Sql "UPDATE InvoiceLines SET SnapshotNominalCode='4000' WHERE Id=$lineId" | Out-Null
} else {
    Add-Skip 'TC-225' 'Missing nominal blocks export' 'sqlcmd unavailable'
}

# --- TC-226 Generate Sage CSV ---
$sageExp = Sage-Export $ta $co.id '2026-05-01' '2026-05-31'
$sageFile = Invoke-Binary "/api/sage-exports/$([int]$sageExp.Json.id)/file" $ta
Add-Result 'TC-226' 'Generate Sage CSV' (
    $sageExp.Status -eq 200 -and $sageExp.Json.recordCount -eq 3 -and
    $sageFile.Status -eq 200 -and $sageFile.Text -match 'AccountRef' -and $sageFile.Text -match 'SAGE001'
) "records=$($sageExp.Json.recordCount) fileBytes=$($sageFile.Bytes.Length)"

# --- TC-227 No duplicate Sage rows ---
$s1 = Count-CsvField $sageFile.Text 0 'SAGE001'
$s2 = Count-CsvField $sageFile.Text 0 'SAGE002'
$s3 = Count-CsvField $sageFile.Text 0 'SAGE003'
$csvAmt = 0.0
$lines = ($sageFile.Text -split "`r?`n") | Where-Object { $_.Trim() -ne '' }
for ($i = 1; $i -lt $lines.Count; $i++) {
    $cols = $lines[$i] -split ','
    if ($cols.Length -ge 6) { $csvAmt += [decimal]$cols[5].Trim('"') }
}
Add-Result 'TC-227' 'No duplicate Sage rows in grouped invoice' (
    $s1 -eq 1 -and $s2 -eq 1 -and $s3 -eq 1 -and [Math]::Round($csvAmt, 2) -eq 7639.29
) "S1=$s1 S2=$s2 S3=$s3 total=$([Math]::Round($csvAmt,2))"

# --- TC-228 Re-export behavior ---
$sageRe = Sage-Preview $ta $co.id '2026-05-01' '2026-05-31' $false
$sageReExp = Sage-Export $ta $co.id '2026-05-01' '2026-05-31' $false
Add-Result 'TC-228' 'Re-export blocked without includeAlreadyExported' (
    $sageRe.Json.eligibleCount -eq 0 -and $sageRe.Json.canExport -eq $false -and $sageReExp.Status -eq 400
) "eligible=$($sageRe.Json.eligibleCount) export=$($sageReExp.Status)"

# --- TC-229 Cross-tenant Sage batch denied ---
$crossSage = Invoke-Binary "/api/sage-exports/$([int]$sageExp.Json.id)/file" $tb
Add-Result 'TC-229' 'Cross-tenant Sage batch/file denied' ($crossSage.Status -eq 404) "status=$($crossSage.Status)"

# --- TC-230 Sage business sign-off ---
Add-Skip 'TC-230' 'Validate import mapping in Sage test environment' 'Requires manual finance review in Sage demo environment (Q-10)'

# --- Dashboard baseline ---
$dashBefore = (Invoke-Json GET '/api/dashboard' $null $ta).Json
$homesA = @((Invoke-Json GET '/api/care-homes' $null $ta).Json | Where-Object { $_.isActive -eq $true })
$clientsA = @((Invoke-Json GET '/api/clients?status=Current&pageSize=200' $null $ta).Json.items)
$capacityA = ($homesA | ForEach-Object { $_.bedCapacity } | Measure-Object -Sum).Sum
$occupiedA = $clientsA.Count
$outstandingResp = Invoke-Json GET '/api/invoices?paymentStatus=NotPaid&pageSize=200' $null $ta
$outstandingA = @($outstandingResp.Json.items | Where-Object { $_.status -ne 'Void' })
$generatedResp = Invoke-Json GET '/api/invoices?pageSize=200' $null $ta
$generatedA = @($generatedResp.Json.items | Where-Object { $_.status -ne 'Void' })

Add-Result 'TC-231' 'Care Homes metric accurate' (
    $dashBefore.totalCareHomes -eq $homesA.Count
) "dash=$($dashBefore.totalCareHomes) src=$($homesA.Count)"

Add-Result 'TC-232' 'Current Clients metric accurate' (
    $dashBefore.currentClients -eq $occupiedA
) "dash=$($dashBefore.currentClients) src=$($occupiedA)"

Add-Result 'TC-233' 'Available Beds metric accurate' (
    $dashBefore.availableBeds -eq ($capacityA - $occupiedA)
) "dash=$($dashBefore.availableBeds) expected=$($capacityA - $occupiedA)"

Add-Result 'TC-234' 'Outstanding Invoices metric accurate' (
    $dashBefore.outstandingInvoices -eq $outstandingA.Count
) "dash=$($dashBefore.outstandingInvoices) src=$($outstandingA.Count)"

Add-Result 'TC-235' 'Upcoming Billing metric accurate' (
    $dashBefore.upcomingBillingCount -eq $dashBefore.upcomingInvoices.Count
) "count=$($dashBefore.upcomingBillingCount)"

Add-Result 'TC-236' 'Generated Invoices metric accurate' (
    $dashBefore.invoicesGenerated -eq $generatedA.Count
) "dash=$($dashBefore.invoicesGenerated) src=$($generatedA.Count)"

# --- TC-237 LocationManager dashboard scope ---
Invoke-Json POST '/api/users' (@{
    email = "loc-rpt-$ts@uat.test"; displayName = 'Loc Mgr'; password = 'LocMgrPass!12345'
    role = 'LocationManager'; careHomeIds = @($home1.id)
} | ConvertTo-Json -Compress) $ta | Out-Null
$loc = Login "loc-rpt-$ts@uat.test" 'LocMgrPass!12345'
$locDash = (Invoke-Json GET '/api/dashboard' $null $loc).Json
Add-Result 'TC-237' 'Dashboard respects location scope' (
    $locDash.totalCareHomes -eq 1 -and $locDash.currentClients -eq 4 -and
    ($locDash.occupancyByHome | Where-Object { $_.careHomeName -eq 'Pine House' }).Count -eq 0
) "homes=$($locDash.totalCareHomes) clients=$($locDash.currentClients)"

# --- TC-238 Dashboard exception links ---
Add-Skip 'TC-238' 'Dashboard exception links' 'UI navigation only; API exposes billingExceptions list on dashboard'

# --- TC-239 Dashboard updates after lifecycle change ---
$pay = Invoke-Json POST "/api/invoices/$invId/payment-status" (@{ paymentStatus = 'Paid' } | ConvertTo-Json -Compress) $ta
$dashAfter = (Invoke-Json GET '/api/dashboard' $null $ta).Json
Add-Result 'TC-239' 'Dashboard updates after lifecycle change' (
    $pay.Status -eq 200 -and $dashAfter.outstandingInvoices -lt $dashBefore.outstandingInvoices
) "pay=$($pay.Status) before=$($dashBefore.outstandingInvoices) after=$($dashAfter.outstandingInvoices)"

# --- Summary ---
$results | Format-Table -AutoSize
$pass = ($results | Where-Object Result -eq 'PASS').Count
$fail = ($results | Where-Object Result -eq 'FAIL').Count
$skip = ($results | Where-Object Result -eq 'SKIP').Count
Write-Host "`nSUMMARY: $pass PASS, $fail FAIL, $skip SKIP of $($results.Count) tests"
if ($fail -gt 0) { exit 1 }

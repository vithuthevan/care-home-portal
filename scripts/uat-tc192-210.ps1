# UAT TC-192 through TC-210 API tests (Credit Notes & Misc CSV Import)
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5092'
$results = @()
$ts = Get-Date -Format 'yyyyMMddHHmmss'
$pastDob = '1980-01-15'
$csvDir = Join-Path $env:TEMP "uat-misc-$ts"
New-Item -ItemType Directory -Path $csvDir -Force | Out-Null

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
    return @{ Status = [int]$lines[-1]; Content = ($lines[0..($lines.Length - 2)] -join "`n"); Json = $(if ($lines.Length -gt 1 -and $lines[0]) { try { $lines[0..($lines.Length - 2)] -join "`n" | ConvertFrom-Json } catch { $null } } else { $null }) }
}

function Invoke-CsvPreview($token, $filePath) {
    $args = @('-s', '-w', "`n%{http_code}", '-X', 'POST', "$base/api/misc-charges/import/preview", '-H', "Authorization: Bearer $token", '-F', "file=@$filePath;type=text/csv")
    $raw = & curl.exe @args
    $lines = $raw -split "`n"
    return @{ Status = [int]$lines[-1]; Content = ($lines[0..($lines.Length - 2)] -join "`n"); Json = $(try { $lines[0..($lines.Length - 2)] -join "`n" | ConvertFrom-Json } catch { $null }) }
}

function Invoke-Pdf($path, $token) {
    $tmp = [IO.Path]::GetTempFileName() + '.pdf'
    $raw = & curl.exe -s -w "`n%{http_code}" -o $tmp "$base$path" -H "Authorization: Bearer $token"
    $lines = $raw -split "`n"
    $bytes = if (Test-Path $tmp) { [IO.File]::ReadAllBytes($tmp) } else { [byte[]]@() }
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    return @{ Status = [int]$lines[-1]; Bytes = $bytes; Text = if ($bytes.Length) { [System.Text.Encoding]::ASCII.GetString($bytes) } else { '' } }
}

function Login($email, $password) {
    $r = Invoke-Json POST '/api/auth/login' (@{ email = $email; password = $password } | ConvertTo-Json -Compress) $null
    if ($r.Status -eq 200) { return $r.Json.token }
    return $null
}

function Provision-Tenant($pt, $name, $email) {
    $c = Invoke-Json POST '/api/platform/tenants' (@{ name = $name; isActive = $true; adminEmail = $email; adminDisplayName = 'QA Admin' } | ConvertTo-Json -Compress) $pt
    if ($c.Status -ne 201) { throw "Provision failed: $($c.Content)" }
    $tp = $c.Json.temporaryPassword
    $t = Login $email $tp
    Invoke-Json POST '/api/auth/change-password' (@{ currentPassword = $tp; newPassword = 'QaTenantAdmin!99' } | ConvertTo-Json -Compress) $t | Out-Null
    return Login $email 'QaTenantAdmin!99'
}

function Get-CategoryId($token, $code) {
    return ((Invoke-Json GET '/api/invoice-categories' $null $token).Json | Where-Object { $_.code -eq $code } | Select-Object -First 1).id
}

$script:nomSeq = 0
function Setup-Invoice($token, $sageId, $ref, $first, $last, $periodStart, $periodEnd) {
    $script:nomSeq++
    $co = (Invoke-Json POST '/api/companies' (@{ name = "Co $ts $sageId" } | ConvertTo-Json -Compress) $token).Json
    $ch = (Invoke-Json POST '/api/care-homes' (@{ companyId = $co.id; code = "H$sageId"; name = "Home $sageId"; bedCapacity = 10; managerName = 'Mgr' } | ConvertTo-Json -Compress) $token).Json
    $fa = (Invoke-Json POST '/api/funding-authorities' (@{ code = "FA$sageId"; name = "Auth $sageId"; type = 'Council'; billingFrequency = 'Weekly'; email = 'council@qa.test' } | ConvertTo-Json -Compress) $token).Json
    $cat = Get-CategoryId $token 'GENERAL_CARE'
    $nomResp = Invoke-Json POST '/api/nominal-codes' (@{ code = "N${ts}_$($script:nomSeq)"; name = 'Care' } | ConvertTo-Json -Compress) $token
    if ($nomResp.Status -ne 201) { throw "Nominal code failed: $($nomResp.Content)" }
    $nom = $nomResp.Json
    Invoke-Json POST '/api/invoice-templates' (@{ name = "Tpl $sageId"; invoiceCategoryId = $cat; contactEmail = 'finance@qa.test'; bankAccountName = 'Bank'; sortCode = '12-34-56'; accountNumber = '12345678' } | ConvertTo-Json -Compress) $token | Out-Null
    $cl = (Invoke-Json POST '/api/clients' (@{ careHomeId = $ch.id; sageId = $sageId; referenceNumber = $ref; firstName = $first; lastName = $last; careType = 'Residential'; admissionDate = '2026-01-01'; dateOfBirth = $pastDob } | ConvertTo-Json -Compress) $token).Json
    $fcResp = Invoke-Json POST "/api/clients/$($cl.id)/funding-contracts" (@{ fundingAuthorityId = $fa.id; invoiceCategoryId = $cat; nominalCodeId = $nom.id; contractStartDate = '2026-01-01' } | ConvertTo-Json -Compress) $token
    if ($fcResp.Status -ne 201) { throw "Funding contract failed: $($fcResp.Content)" }
    $fc = $fcResp.Json
    $rateResp = Invoke-Json POST "/api/funding-contracts/$($fc.id)/rates" (@{ effectiveFrom = '2026-01-01'; frequency = 'Weekly'; amount = 575 } | ConvertTo-Json -Compress) $token
    if ($rateResp.Status -ne 200) { throw "Rate failed: $($rateResp.Content)" }
    $gen = Invoke-Json POST '/api/billing/generate' (@{ companyId = $co.id; careHomeId = $ch.id; periodStart = $periodStart; periodEnd = $periodEnd; clientIds = @($cl.id); invoiceCategoryId = $cat } | ConvertTo-Json -Compress) $token
    if ($gen.Status -ne 200) { throw "Generate failed: $($gen.Content)" }
    $invId = [int]$gen.Json.invoiceIds[0]
    $inv = (Invoke-Json GET "/api/invoices/$invId" $null $token).Json
    return @{ ClientId = $cl.id; InvoiceId = $invId; LineId = $inv.lines[0].id; LineAmount = [decimal]$inv.lines[0].lineAmount; Invoice = $inv; FA = $fa.id; Category = $cat }
}

function Credit-Preview($token, $clientId, $start, $end, $reason, $lineAmounts = $null) {
    $body = @{ clientId = $clientId; periodStart = $start; periodEnd = $end; creditNoteDate = '2026-06-15'; reason = $reason }
    if ($lineAmounts) { $body.lineAmounts = $lineAmounts }
    return Invoke-Json POST '/api/credit-notes/preview' ($body | ConvertTo-Json -Compress -Depth 5) $token
}

function Credit-Generate($token, $clientId, $start, $end, $reason, $lineAmounts = $null) {
    $body = @{ clientId = $clientId; periodStart = $start; periodEnd = $end; creditNoteDate = '2026-06-15'; reason = $reason }
    if ($lineAmounts) { $body.lineAmounts = $lineAmounts }
    return Invoke-Json POST '/api/credit-notes/generate' ($body | ConvertTo-Json -Compress -Depth 5) $token
}

function Write-CsvFile($name, $content) {
    $path = Join-Path $csvDir $name
    [IO.File]::WriteAllText($path, $content, [Text.UTF8Encoding]::new($false))
    return $path
}

function Invoke-Sql($query) {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { return $null }
    $out = & sqlcmd -S '(localdb)\MSSQLLocalDB' -d CareHomeDb -Q $query -h -1 -W 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return ($out | Where-Object { $_.Trim() -ne '' })
}

# --- Bootstrap ---
$pt = Login 'admin@localhost' 'DevAdmin!12345'
if (-not $pt) { throw 'Platform login failed' }
$ta = Provision-Tenant $pt "QA Credit A $ts" "qa-cn-a-$ts@uat.test"
$tb = Provision-Tenant $pt "QA Credit B $ts" "qa-cn-b-$ts@uat.test"

$invMay = Setup-Invoice $ta 'SAGE001' 'CLIENT001' 'Alice' 'Brown' '2026-05-01' '2026-05-31'
$invJun = Setup-Invoice $ta 'SAGE002' 'CLIENT002' 'David' 'Smith' '2026-06-01' '2026-06-30'
$invMay2 = Setup-Invoice $ta 'SAGE003' 'CLIENT003' 'Mary' 'Jones' '2026-05-01' '2026-05-31'

# --- TC-192 Partial credit ---
$partialAmt = [Math]::Round($invMay.LineAmount / 2, 2)
$cn192 = Credit-Generate $ta $invMay.ClientId '2026-05-01' '2026-05-31' 'Partial service adjustment' @{ "$($invMay.LineId)" = $partialAmt }
Add-Result 'TC-192' 'Create partial credit' (
    $cn192.Status -eq 201 -and $cn192.Json.creditNoteNumber -like 'CN-*' -and $cn192.Json.totalAmount -lt 0
) "number=$($cn192.Json.creditNoteNumber) total=$($cn192.Json.totalAmount)"

# --- TC-193 Full remaining credit ---
$cn193 = Credit-Generate $ta $invJun.ClientId '2026-06-01' '2026-06-30' 'Full period credit' $null
Add-Result 'TC-193' 'Create full remaining credit' (
    $cn193.Status -eq 201 -and [Math]::Abs([decimal]$cn193.Json.totalAmount) -eq $invJun.LineAmount
) "total=$($cn193.Json.totalAmount) line=$($invJun.LineAmount)"

# --- TC-194 Reason required ---
$p194 = Credit-Preview $ta $invMay2.ClientId '2026-05-01' '2026-05-31' '   '
Add-Result 'TC-194' 'Reason required' ($p194.Json.canGenerate -eq $false -and ($p194.Json.exceptions -join ' ') -match 'reason') "canGen=$($p194.Json.canGenerate)"

# --- TC-195 Over-credit rejected ---
$p195 = Credit-Preview $ta $invMay2.ClientId '2026-05-01' '2026-05-31' 'Over credit test' @{ "$($invMay2.LineId)" = ($invMay2.LineAmount + 100) }
$g195 = Credit-Generate $ta $invMay2.ClientId '2026-05-01' '2026-05-31' 'Over credit test' @{ "$($invMay2.LineId)" = ($invMay2.LineAmount + 100) }
Add-Result 'TC-195' 'Over-credit rejected' ($p195.Json.canGenerate -eq $false -and $g195.Status -eq 400) "preview=$($p195.Json.canGenerate) gen=$($g195.Status)"

# --- TC-196 Cannot span multiple invoices ---
$stackClient = $invMay.ClientId
$coId = (Invoke-Json GET "/api/invoices/$($invMay.InvoiceId)" $null $ta).Json.companyId
$homeId = (Invoke-Json GET "/api/invoices/$($invMay.InvoiceId)" $null $ta).Json.careHomeId
$catId = $invMay.Category
$genA = Invoke-Json POST '/api/billing/generate' (@{ companyId = $coId; careHomeId = $homeId; periodStart = '2026-07-01'; periodEnd = '2026-07-31'; clientIds = @($stackClient); invoiceCategoryId = $catId } | ConvertTo-Json -Compress) $ta
$genB = Invoke-Json POST '/api/billing/generate' (@{ companyId = $coId; careHomeId = $homeId; periodStart = '2026-08-01'; periodEnd = '2026-08-31'; clientIds = @($stackClient); invoiceCategoryId = $catId } | ConvertTo-Json -Compress) $ta
$p196 = Credit-Preview $ta $stackClient '2026-07-01' '2026-08-31' 'Span two invoices' $null
Add-Result 'TC-196' 'Credit cannot span multiple invoices' (
    $p196.Json.canGenerate -eq $false -and ($p196.Json.exceptions -join ' ') -match 'one invoice'
) "invoices=$($genA.Status)/$($genB.Status) canGen=$($p196.Json.canGenerate)"

# --- TC-197 Credit-note PDF ---
$cnId = [int]$cn192.Json.id
$cnDetail = Invoke-Json GET "/api/credit-notes/$cnId" $null $ta
$cnPdf = Invoke-Pdf "/api/credit-notes/$cnId/pdf" $ta
Add-Result 'TC-197' 'Credit-note PDF readable' (
    $cnPdf.Status -eq 200 -and $cnPdf.Text.StartsWith('%PDF') -and $cnDetail.Json.creditNoteNumber -and $cnDetail.Json.reason -and $cnDetail.Json.totalAmount -lt 0
) "number=$($cnDetail.Json.creditNoteNumber) bytes=$($cnPdf.Bytes.Length)"

# --- TC-198 Send credit note ---
$invBefore = (Invoke-Json GET "/api/invoices/$($invJun.InvoiceId)" $null $ta).Json
$send198 = Invoke-Json POST "/api/credit-notes/$([int]$cn193.Json.id)/send" $null $ta
$invAfter = (Invoke-Json GET "/api/invoices/$($invJun.InvoiceId)" $null $ta).Json
$emailLog = Invoke-Sql "SELECT TOP 1 CAST(Simulated AS int) FROM EmailSendLogs WHERE DocumentType='CreditNote' AND DocumentId=$([int]$cn193.Json.id) ORDER BY Id DESC"
Add-Result 'TC-198' 'Send credit note simulation' (
    $send198.Status -eq 200 -and $send198.Json.simulated -eq $true -and $invAfter.totalAmount -eq $invBefore.totalAmount
) "simulated=$($send198.Json.simulated) invTotal=$($invAfter.totalAmount)"

# --- TC-199 Cross-tenant credit denied ---
$invB = Setup-Invoice $tb 'SAGEB' 'CLIENTB' 'Other' 'Tenant' '2026-05-01' '2026-05-31'
$cnB = Credit-Generate $tb $invB.ClientId '2026-05-01' '2026-05-31' 'Tenant B credit' @{ "$($invB.LineId)" = 100 }
$crossGet = Invoke-Json GET "/api/credit-notes/$([int]$cnB.Json.id)" $null $ta
$crossPdf = Invoke-Pdf "/api/credit-notes/$([int]$cnB.Json.id)/pdf" $ta
Add-Result 'TC-199' 'Cross-tenant credit denied' ($crossGet.Status -eq 404 -and $crossPdf.Status -eq 404) "get=$($crossGet.Status) pdf=$($crossPdf.Status)"

# --- Misc CSV: setup client + nominal on tenant A ---
$miscClient = (Invoke-Json POST '/api/clients' (@{
    careHomeId = $homeId; sageId = 'SAGEMISC'; referenceNumber = 'MISC001'; firstName = 'Misc'; lastName = 'Client'
    careType = 'Residential'; admissionDate = '2026-01-01'; dateOfBirth = $pastDob
} | ConvertTo-Json -Compress) $ta).Json
$miscNom = (Invoke-Json POST '/api/nominal-codes' (@{ code = 'MISC4000'; name = 'Misc Revenue' } | ConvertTo-Json -Compress) $ta).Json
$miscClientB = Setup-Invoice $tb 'SAGEB2' 'MISCB' 'Tenant' 'BClient' '2026-05-01' '2026-05-07'

# --- TC-200 Valid CSV import ---
$validCsv = Write-CsvFile 'valid.csv' @"
ClientReference,UsedDate,Description,Amount,NominalCode
MISC001,2026-05-15,Haircut charge,25.50,MISC4000
"@
$p200 = Invoke-CsvPreview $ta $validCsv
$c200 = Invoke-Json POST '/api/misc-charges/import/confirm' ($p200.Content) $ta
$imports200 = Invoke-Json GET '/api/misc-charges/imports' $null $ta
Add-Result 'TC-200' 'Valid CSV preview and commit' (
    $p200.Status -eq 200 -and $p200.Json.validCount -eq 1 -and $c200.Status -eq 200 -and $c200.Json.acceptedRows -eq 1
) "valid=$($p200.Json.validCount) accepted=$($c200.Json.acceptedRows)"

# --- TC-201 Unknown client ---
$unkCsv = Write-CsvFile 'unknown.csv' "ClientReference,UsedDate,Description,Amount`nUNKNOWN001,2026-05-16,Bad client,10.00"
$p201 = Invoke-CsvPreview $ta $unkCsv
Add-Result 'TC-201' 'Unknown client reference rejected' (
    $p201.Json.invalidCount -eq 1 -and ($p201.Json.rows[0].error -match 'Unknown client')
) "error=$($p201.Json.rows[0].error)"

# --- TC-202 Invalid amount ---
$badAmt = Write-CsvFile 'badamt.csv' "ClientReference,UsedDate,Description,Amount`nMISC001,2026-05-17,Bad amount,abc"
$p202 = Invoke-CsvPreview $ta $badAmt
Add-Result 'TC-202' 'Invalid amount rejected' ($p202.Json.rows[0].isValid -eq $false) "error=$($p202.Json.rows[0].error)"

# --- TC-203 Invalid date ---
$badDate = Write-CsvFile 'baddate.csv' "ClientReference,UsedDate,Description,Amount`nMISC001,31/99/2026,Bad date,10.00"
$p203 = Invoke-CsvPreview $ta $badDate
Add-Result 'TC-203' 'Invalid date rejected' ($p203.Json.rows[0].isValid -eq $false) "error=$($p203.Json.rows[0].error)"

# --- TC-204 Missing required column ---
$missCol = Write-CsvFile 'misscol.csv' "UsedDate,Description,Amount`n2026-05-18,Missing ref,10.00"
$p204 = Invoke-CsvPreview $ta $missCol
Add-Result 'TC-204' 'Missing column rejected' ($p204.Json.rows[0].isValid -eq $false) "error=$($p204.Json.rows[0].error)"

# --- TC-205 Extra column tolerated ---
$extraCol = Write-CsvFile 'extracol.csv' "ClientReference,UsedDate,Description,Amount,NominalCode,ExtraNotes`nMISC001,2026-05-19,Extra col test,15.00,MISC4000,ignored"
$p205 = Invoke-CsvPreview $ta $extraCol
Add-Result 'TC-205' 'Extra column handled safely' ($p205.Json.rows[0].isValid -eq $true -and $p205.Json.rows[0].amount -eq 15) "valid=$($p205.Json.rows[0].isValid)"

# --- TC-206 Empty CSV ---
$emptyCsv = Write-CsvFile 'empty.csv' 'ClientReference,UsedDate,Description,Amount'
$p206 = Invoke-CsvPreview $ta $emptyCsv
$c206 = Invoke-Json POST '/api/misc-charges/import/confirm' ($p206.Content) $ta
Add-Result 'TC-206' 'Empty CSV no import' ($p206.Json.validCount -eq 0 -and $c206.Status -eq 400) "valid=$($p206.Json.validCount) confirm=$($c206.Status)"

# --- TC-207 Wrong extension ---
$txtPath = Join-Path $csvDir 'charges.txt'
[IO.File]::WriteAllText($txtPath, "ClientReference,UsedDate,Description,Amount`nMISC001,2026-05-20,Txt,10", [Text.UTF8Encoding]::new($false))
$p207 = Invoke-CsvPreview $ta $txtPath
Add-Result 'TC-207' 'Wrong extension rejected' ($p207.Status -eq 400) "status=$($p207.Status)"

# --- TC-208 Oversized file ---
$bigPath = Join-Path $csvDir 'big.csv'
$header = "ClientReference,UsedDate,Description,Amount`n"
[IO.File]::WriteAllText($bigPath, $header + ('X' * (2 * 1024 * 1024 + 1)), [Text.UTF8Encoding]::new($false))
$p208 = Invoke-CsvPreview $ta $bigPath
Add-Result 'TC-208' 'Oversized file rejected' ($p208.Status -eq 400) "status=$($p208.Status)"

# --- TC-209 Duplicate row ---
$dupCsv = Write-CsvFile 'dup.csv' "ClientReference,UsedDate,Description,Amount`nMISC001,2026-05-15,Haircut charge,25.50"
$p209 = Invoke-CsvPreview $ta $dupCsv
Add-Result 'TC-209' 'Duplicate row rejected' ($p209.Json.rows[0].isValid -eq $false -and $p209.Json.rows[0].error -match 'Duplicate') "error=$($p209.Json.rows[0].error)"

# --- TC-210 Cross-tenant client reference ---
$crossCsv = Write-CsvFile 'crosstenant.csv' "ClientReference,UsedDate,Description,Amount`nMISCB,2026-05-21,Cross tenant,12.00"
$p210 = Invoke-CsvPreview $ta $crossCsv
Add-Result 'TC-210' 'Tenant A cannot bind Tenant B client' ($p210.Json.rows[0].isValid -eq $false) "error=$($p210.Json.rows[0].error)"

# --- Cleanup & summary ---
Remove-Item $csvDir -Recurse -Force -ErrorAction SilentlyContinue
$results | Format-Table -AutoSize
$pass = ($results | Where-Object Result -eq 'PASS').Count
$fail = ($results | Where-Object Result -eq 'FAIL').Count
Write-Host "`nSUMMARY: $pass PASS, $fail FAIL of $($results.Count) tests"
if ($fail -gt 0) { exit 1 }

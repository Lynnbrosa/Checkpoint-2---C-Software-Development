[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ImageTag,
    [string]$OutputDirectory = "artifacts/cp3-experiments"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$evidencePath = Join-Path $OutputDirectory "EVIDENCIAS-E01-E05.md"
$excelPath = Join-Path $OutputDirectory "report-e01.xlsx"
$lines = [System.Collections.Generic.List[string]]::new()

function Write-Section([string]$title) {
    $lines.Add("")
    $lines.Add("## $title")
    $lines.Add("")
    Write-Host "=== $title ==="
}

function Write-Block([string]$lang, [string]$content) {
    $lines.Add("``````$lang")
    $lines.Add($content.TrimEnd())
    $lines.Add("``````")
    $lines.Add("")
}

$adminKey = "Adm#" + [Guid]::NewGuid().ToString("N").Substring(0, 10) + "9z"
$empKey = "Emp#" + [Guid]::NewGuid().ToString("N").Substring(0, 10) + "9z"
$rabbitKey = "Rmq#" + [Guid]::NewGuid().ToString("N").Substring(0, 10) + "9z"

@"
EXPENSEHUB_WEB_IMAGE=ghcr.io/lynnbrosa/expensehub-web:$ImageTag
EXPENSEHUB_WORKER_IMAGE=ghcr.io/lynnbrosa/expensehub-worker:$ImageTag
WEB_PORT=5245
ASPNETCORE_ENVIRONMENT=Development
SEED_ADMIN_EMAIL=admin@expensehub.local
SEED_ADMIN_PASSWORD=$adminKey
RABBITMQ_USER=guest
RABBITMQ_PASS=$rabbitKey
"@ | Set-Content -Path ".env" -Encoding utf8

$lines.Add("# Evidências Operacionais Executadas — Checkpoint 3 (E01 a E05)")
$lines.Add("")
$lines.Add("- **Commit / Tag das imagens:** ``$ImageTag``")
$lines.Add("- **Imagem Web:** ``ghcr.io/lynnbrosa/expensehub-web:$ImageTag``")
$lines.Add("- **Imagem Worker:** ``ghcr.io/lynnbrosa/expensehub-worker:$ImageTag``")
$lines.Add("- **Data da execução (UTC):** $((Get-Date).ToUniversalTime().ToString('o'))")

try {
    Write-Section "Inicialização do Ambiente (CP3-I05)"
    $pullOut = (docker compose pull 2>&1) -join "`n"
    $upOut = (docker compose up -d 2>&1) -join "`n"
    Write-Block "text" "$pullOut`n$upOut"

    $baseUrl = "http://localhost:5245"
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Seconds 1
        try {
            $h = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 2
            if ($h.status -eq "ok") { break }
        }
        catch {}
    }

    $psOut = (docker compose ps 2>&1) -join "`n"
    Write-Block "text" $psOut

    # Preparação de usuários e reembolsos do exemplo verificável
    $adminLogin = Invoke-RestMethod -Method Post -Uri "$baseUrl/login" -ContentType "application/json" -Body (@{ email = "admin@expensehub.local"; password = $adminKey } | ConvertTo-Json)
    $adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }

    $emp1 = Invoke-RestMethod -Method Post -Uri "$baseUrl/register" -ContentType "application/json" -Body (@{ email = "emp1@expensehub.local"; password = $empKey; fullName = "Funcionário 1" } | ConvertTo-Json)
    $emp2 = Invoke-RestMethod -Method Post -Uri "$baseUrl/register" -ContentType "application/json" -Body (@{ email = "emp2@expensehub.local"; password = $empKey; fullName = "Funcionário 2" } | ConvertTo-Json)
    $appr = Invoke-RestMethod -Method Post -Uri "$baseUrl/register" -ContentType "application/json" -Body (@{ email = "approver@expensehub.local"; password = $empKey; fullName = "Aprovador" } | ConvertTo-Json)
    $fin  = Invoke-RestMethod -Method Post -Uri "$baseUrl/register" -ContentType "application/json" -Body (@{ email = "finance@expensehub.local"; password = $empKey; fullName = "Financeiro" } | ConvertTo-Json)
    $aud  = Invoke-RestMethod -Method Post -Uri "$baseUrl/register" -ContentType "application/json" -Body (@{ email = "auditor@expensehub.local"; password = $empKey; fullName = "Auditor" } | ConvertTo-Json)

    Invoke-RestMethod -Method Put -Uri "$baseUrl/api/admin/users/$($emp1.id)/roles" -Headers $adminHeaders -ContentType "application/json" -Body '{"roles":["Employee"]}' | Out-Null
    Invoke-RestMethod -Method Put -Uri "$baseUrl/api/admin/users/$($emp2.id)/roles" -Headers $adminHeaders -ContentType "application/json" -Body '{"roles":["Employee"]}' | Out-Null
    Invoke-RestMethod -Method Put -Uri "$baseUrl/api/admin/users/$($appr.id)/roles" -Headers $adminHeaders -ContentType "application/json" -Body '{"roles":["Approver"]}' | Out-Null
    Invoke-RestMethod -Method Put -Uri "$baseUrl/api/admin/users/$($fin.id)/roles"  -Headers $adminHeaders -ContentType "application/json" -Body '{"roles":["Finance"]}'  | Out-Null
    Invoke-RestMethod -Method Put -Uri "$baseUrl/api/admin/users/$($aud.id)/roles"  -Headers $adminHeaders -ContentType "application/json" -Body '{"roles":["Auditor"]}'  | Out-Null

    $emp1Headers = @{ Authorization = "Bearer $((Invoke-RestMethod -Method Post -Uri "$baseUrl/login" -ContentType "application/json" -Body (@{ email = "emp1@expensehub.local"; password = $empKey } | ConvertTo-Json)).accessToken)" }
    $emp2Headers = @{ Authorization = "Bearer $((Invoke-RestMethod -Method Post -Uri "$baseUrl/login" -ContentType "application/json" -Body (@{ email = "emp2@expensehub.local"; password = $empKey } | ConvertTo-Json)).accessToken)" }
    $apprHeaders = @{ Authorization = "Bearer $((Invoke-RestMethod -Method Post -Uri "$baseUrl/login" -ContentType "application/json" -Body (@{ email = "approver@expensehub.local"; password = $empKey } | ConvertTo-Json)).accessToken)" }
    $finHeaders  = @{ Authorization = "Bearer $((Invoke-RestMethod -Method Post -Uri "$baseUrl/login" -ContentType "application/json" -Body (@{ email = "finance@expensehub.local"; password = $empKey } | ConvertTo-Json)).accessToken)" }
    $audHeaders  = @{ Authorization = "Bearer $((Invoke-RestMethod -Method Post -Uri "$baseUrl/login" -ContentType "application/json" -Body (@{ email = "auditor@expensehub.local"; password = $empKey } | ConvertTo-Json)).accessToken)" }

    # 5 reembolsos da tabela do capítulo 4 da apostila
    $ex1 = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses" -Headers $emp1Headers -ContentType "application/json" -Body '{"description":"Reembolso 1 dentro do período","amount":100.00,"expenseDate":"2026-09-05","categoryId":1}'
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex1.id)/submit" -Headers $emp1Headers | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex1.id)/approve" -Headers $apprHeaders | Out-Null

    $ex2 = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses" -Headers $emp1Headers -ContentType "application/json" -Body '{"description":"Reembolso 2 dentro do período","amount":50.00,"expenseDate":"2026-09-15","categoryId":1}'
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex2.id)/submit" -Headers $emp1Headers | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex2.id)/approve" -Headers $apprHeaders | Out-Null

    $ex3 = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses" -Headers $emp1Headers -ContentType "application/json" -Body '{"description":"Reembolso 3 pago no período","amount":25.00,"expenseDate":"2026-09-20","categoryId":1}'
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex3.id)/submit" -Headers $emp1Headers | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex3.id)/approve" -Headers $apprHeaders | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex3.id)/pay" -Headers $finHeaders | Out-Null

    $ex4 = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses" -Headers $emp2Headers -ContentType "application/json" -Body '{"description":"Reembolso de outro funcionário","amount":200.00,"expenseDate":"2026-09-18","categoryId":1}'
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex4.id)/submit" -Headers $emp2Headers | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex4.id)/approve" -Headers $apprHeaders | Out-Null

    $ex5 = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses" -Headers $emp1Headers -ContentType "application/json" -Body '{"description":"Reembolso fora do período","amount":500.00,"expenseDate":"2026-08-25","categoryId":1}'
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex5.id)/submit" -Headers $emp1Headers | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex5.id)/approve" -Headers $apprHeaders | Out-Null
    Invoke-RestMethod -Method Post -Uri "$baseUrl/api/expenses/$($ex5.id)/pay" -Headers $finHeaders | Out-Null

    # ============================================================
    # E01 — Fluxo normal com download Excel
    # ============================================================
    Write-Section "E01 — Fluxo normal e download Excel"
    $e01Req = Invoke-WebRequest -Method Post -Uri "$baseUrl/api/reports" -Headers $emp1Headers -ContentType "application/json" -Body '{"fromDate":"2026-09-01","toDate":"2026-09-30","scope":"Mine"}'
    $e01Created = $e01Req.Content | ConvertFrom-Json
    $e01Id = $e01Created.id
    $lines.Add("- **Status HTTP inicial:** ``$($e01Req.StatusCode)`` | **Location:** ``$($e01Req.Headers['Location'])`` | **ReportId:** ``$e01Id``")
    Write-Block "json" ($e01Created | ConvertTo-Json -Depth 10)

    $e01Detail = $null
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 500
        $e01Detail = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/reports/$e01Id" -Headers $emp1Headers
        if ($e01Detail.status -eq "Completed") { break }
    }
    $lines.Add("Resultado após processamento pelo worker:")
    Write-Block "json" ($e01Detail | ConvertTo-Json -Depth 10)

    $excelResp = Invoke-WebRequest -Method Get -Uri "$baseUrl/api/reports/$e01Id/excel" -Headers $emp1Headers -OutFile $excelPath -PassThru
    $lines.Add("- **Download Excel:** Status ``$($excelResp.StatusCode)``, Content-Type ``$($excelResp.Headers['Content-Type'])``, Content-Disposition ``$($excelResp.Headers['Content-Disposition'])``, Tamanho ``$((Get-Item $excelPath).Length) bytes``.")

    # ============================================================
    # E02 — Worker parado e retomada
    # ============================================================
    Write-Section "E02 — Worker parado e retomada da mesma solicitação"
    docker compose stop worker | Out-Null
    $e02Created = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/reports" -Headers $emp1Headers -ContentType "application/json" -Body '{"fromDate":"2026-09-01","toDate":"2026-09-30","scope":"Mine"}'
    $e02Id = $e02Created.id
    Start-Sleep -Seconds 2
    $e02WhileStopped = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/reports/$e02Id" -Headers $emp1Headers
    $rmqQueues = (docker compose exec -T rabbitmq rabbitmqctl list_queues name messages_ready messages_unacknowledged 2>&1) -join "`n"
    $lines.Add("Solicitação ``$e02Id`` com worker parado permanece em ``$($e02WhileStopped.status)`` e mensagem aguarda na fila:")
    Write-Block "text" $rmqQueues
    Write-Block "json" ($e02WhileStopped | ConvertTo-Json -Depth 10)

    docker compose up -d worker | Out-Null
    $e02AfterResume = $null
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 500
        $e02AfterResume = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/reports/$e02Id" -Headers $emp1Headers
        if ($e02AfterResume.status -eq "Completed") { break }
    }
    $lines.Add("Mesmo identificador ``$e02Id`` após retomar o worker:")
    Write-Block "json" ($e02AfterResume | ConvertTo-Json -Depth 10)

    # ============================================================
    # E03 — Duplicata após reinício do worker
    # ============================================================
    Write-Section "E03 — Duplicata após reinício do worker (Idempotência persistente)"
    docker compose restart worker | Out-Null
    Start-Sleep -Seconds 3
    $rmqAuth = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:$rabbitKey"))
    $publishDuplicateBody = @{
        properties = @{ delivery_mode = 2; content_type = "application/json" }
        routing_key = "expensehub.reports"
        payload = "{`"reportId`":`"$e01Id`"}"
        payload_encoding = "string"
    } | ConvertTo-Json -Compress
    $pubResult = Invoke-RestMethod -Method Post -Uri "http://localhost:15672/api/exchanges/%2F/amq.default/publish" -Headers @{ Authorization = $rmqAuth } -ContentType "application/json" -Body $publishDuplicateBody
    Start-Sleep -Seconds 2
    $e01AfterDup = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/reports/$e01Id" -Headers $emp1Headers
    $workerLogs = (docker compose logs --tail 60 worker 2>&1) -join "`n"
    $lines.Add("- **Republicação na fila via RabbitMQ Management API:** ``routed = $($pubResult.routed)``")
    $lines.Add("- **completedAt antes:** ``$($e01Detail.completedAt)`` | **completedAt depois:** ``$($e01AfterDup.completedAt)``")
    Write-Block "text" $workerLogs

    # ============================================================
    # E04 — Isolamento e Autorização
    # ============================================================
    Write-Section "E04 — Isolamento e Autorização"
    $empGlobalStatus = try { (Invoke-WebRequest -Method Post -Uri "$baseUrl/api/reports" -Headers $emp1Headers -ContentType "application/json" -Body '{"fromDate":"2026-09-01","toDate":"2026-09-30","scope":"Global"}').StatusCode } catch { [int]$_.Exception.Response.StatusCode }
    $emp2GetEmp1Status = try { (Invoke-WebRequest -Method Get -Uri "$baseUrl/api/reports/$e01Id" -Headers $emp2Headers).StatusCode } catch { [int]$_.Exception.Response.StatusCode }
    $audGetEmp1Status = try { (Invoke-WebRequest -Method Get -Uri "$baseUrl/api/reports/$e01Id" -Headers $audHeaders).StatusCode } catch { [int]$_.Exception.Response.StatusCode }
    $audExcelEmp1Status = try { (Invoke-WebRequest -Method Get -Uri "$baseUrl/api/reports/$e01Id/excel" -Headers $audHeaders).StatusCode } catch { [int]$_.Exception.Response.StatusCode }

    $audGlobalCreated = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/reports" -Headers $audHeaders -ContentType "application/json" -Body '{"fromDate":"2026-09-01","toDate":"2026-09-30","scope":"Global"}'
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 500
        $audGlobalDetail = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/reports/$($audGlobalCreated.id)" -Headers $audHeaders
        if ($audGlobalDetail.status -eq "Completed") { break }
    }

    $lines.Add("- Employee solicita ``Global``: **$empGlobalStatus** (esperado 403)")
    $lines.Add("- Outro Employee consulta relatório alheio: **$emp2GetEmp1Status** (esperado 404)")
    $lines.Add("- Auditor consulta relatório de Employee: **$audGetEmp1Status** (esperado 404)")
    $lines.Add("- Auditor baixa Excel de Employee: **$audExcelEmp1Status** (esperado 404)")
    $lines.Add("- Auditor solicita e conclui relatório ``Global`` próprio (``$($audGlobalCreated.id)``):")
    Write-Block "json" ($audGlobalDetail | ConvertTo-Json -Depth 10)

    # ============================================================
    # E05 — Persistência e versão após recriar Web e Worker
    # ============================================================
    Write-Section "E05 — Persistência e versão após recriar Web e Worker"
    $recreateOut = (docker compose up -d --force-recreate web worker 2>&1) -join "`n"
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        try {
            $h = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 2
            if ($h.status -eq "ok") { break }
        }
        catch {}
    }
    $imagesOut = (docker compose images 2>&1) -join "`n"
    $e01AfterRecreate = Invoke-RestMethod -Method Get -Uri "$baseUrl/api/reports/$e01Id" -Headers $emp1Headers
    Write-Block "text" "$recreateOut`n$imagesOut"
    $lines.Add("Consulta ao relatório anterior ``$e01Id`` após recriar contêineres (token e dados preservados):")
    Write-Block "json" ($e01AfterRecreate | ConvertTo-Json -Depth 10)
}
finally {
    $lines | Set-Content -Path $evidencePath -Encoding utf8
    docker compose down
    Remove-Item ".env" -Force -ErrorAction SilentlyContinue
}

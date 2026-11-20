[CmdletBinding()]
param(
    [string]$Image = "engenharia-dotnet-observabilidade:smoke",
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dockerfile = Join-Path $repoRoot "exemplos/c27-c28-observabilidade/Dockerfile"
$suffix = [guid]::NewGuid().ToString("N").Substring(0, 12)
$containers = [System.Collections.Generic.List[string]]::new()
$httpClient = $null

function Invoke-Docker {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker $($Arguments -join ' ') falhou com código $LASTEXITCODE."
    }
}

function Start-SmokeContainer {
    param(
        [Parameter(Mandatory)][string]$Name,
        [string[]]$ExtraArguments = @()
    )

    $arguments = @(
        "run", "--detach",
        "--name", $Name,
        "--label", "engenharia-dotnet.smoke=$suffix",
        "--publish", "127.0.0.1::8080"
    ) + $ExtraArguments + @($Image)

    Invoke-Docker $arguments | Out-Null
    $containers.Add($Name)

    $mapping = Invoke-Docker @("port", $Name, "8080/tcp") | Select-Object -First 1
    if ($mapping -notmatch ":(?<port>[0-9]+)$") {
        throw "Não foi possível descobrir a porta publicada de ${Name}: $mapping"
    }

    return [int]$Matches.port
}

function Get-HttpStatus {
    param([Parameter(Mandatory)][string]$Uri)

    $timeout = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(3))
    try {
        $response = $script:httpClient.GetAsync($Uri, $timeout.Token).GetAwaiter().GetResult()
        try {
            return [int]$response.StatusCode
        }
        finally {
            $response.Dispose()
        }
    }
    catch {
        return 0
    }
    finally {
        $timeout.Dispose()
    }
}

function Wait-HttpStatus {
    param(
        [Parameter(Mandatory)][string]$Uri,
        [Parameter(Mandatory)][int]$ExpectedStatus
    )

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $status = Get-HttpStatus $Uri
        if ($status -eq $ExpectedStatus) {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "$Uri não retornou HTTP $ExpectedStatus dentro do tempo esperado; último status: $status."
}

function Get-ContainerLogs {
    param([Parameter(Mandatory)][string]$Name)

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $logs = (& docker logs $Name 2>&1) | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "Não foi possível ler os logs de $Name."
        }

        return $logs
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Wait-LogEvent {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][int]$EventId
    )

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $logs = Get-ContainerLogs $Name
        if ($logs -match ('"EventId":' + $EventId)) {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "O evento $EventId não apareceu nos logs de $Name."
}

try {
    Invoke-Docker @("info", "--format", "{{.ServerVersion}}") | Out-Null

    if (-not ("System.Net.Http.HttpClient" -as [type])) {
        Add-Type -AssemblyName System.Net.Http
    }
    $httpClient = [System.Net.Http.HttpClient]::new()

    if (-not $SkipBuild) {
        Write-Host "Construindo $Image..."
        Invoke-Docker @("build", "--file", $dockerfile, "--tag", $Image, $repoRoot)
    }

    Write-Host "Validando container saudável e limites de recursos..."
    $healthyName = "engenharia-dotnet-smoke-healthy-$suffix"
    $healthyPort = Start-SmokeContainer $healthyName @(
        "--memory", "256m",
        "--cpus", "1",
        "--stop-timeout", "10"
    )
    $healthyBaseUri = "http://127.0.0.1:$healthyPort"
    Wait-HttpStatus "$healthyBaseUri/health/live" 200
    Wait-HttpStatus "$healthyBaseUri/health/ready" 200

    $user = Invoke-Docker @("inspect", $healthyName, "--format", "{{.Config.User}}")
    $stopSignal = Invoke-Docker @("inspect", $healthyName, "--format", "{{.Config.StopSignal}}")
    $memory = [long](Invoke-Docker @("inspect", $healthyName, "--format", "{{.HostConfig.Memory}}"))
    $nanoCpus = [long](Invoke-Docker @("inspect", $healthyName, "--format", "{{.HostConfig.NanoCpus}}"))
    if ($user -eq "0" -or [string]::IsNullOrWhiteSpace($user)) {
        throw "O container está configurado com usuário privilegiado ou indefinido."
    }
    if ($stopSignal -ne "SIGTERM") {
        throw "STOPSIGNAL inesperado: $stopSignal."
    }
    if ($memory -ne 268435456 -or $nanoCpus -ne 1000000000) {
        throw "Limites inesperados: memória=$memory, NanoCPUs=$nanoCpus."
    }

    Write-Host "Validando diferença entre liveness e readiness..."
    $degradedName = "engenharia-dotnet-smoke-degraded-$suffix"
    $degradedPort = Start-SmokeContainer $degradedName @(
        "--env", "Dependencias__Catalogo=http://127.0.0.1:1"
    )
    $degradedBaseUri = "http://127.0.0.1:$degradedPort"
    Wait-HttpStatus "$degradedBaseUri/health/live" 200
    Wait-HttpStatus "$degradedBaseUri/health/ready" 503
    Wait-HttpStatus "$degradedBaseUri/pedidos/11111111-1111-1111-1111-111111111111?atrasoMs=0" 503
    Wait-LogEvent $degradedName 2703

    Write-Host "Validando propagação de SIGTERM e cancelamento..."
    $shutdownName = "engenharia-dotnet-smoke-shutdown-$suffix"
    $shutdownPort = Start-SmokeContainer $shutdownName @("--stop-timeout", "10")
    $shutdownBaseUri = "http://127.0.0.1:$shutdownPort"
    Wait-HttpStatus "$shutdownBaseUri/health/live" 200

    $requestTask = $httpClient.GetAsync("$shutdownBaseUri/operacoes/demorada?duracaoMs=30000")
    Wait-LogEvent $shutdownName 2720

    Invoke-Docker @("stop", "--time", "10", $shutdownName) | Out-Null
    try {
        $requestTask.Wait(5000) | Out-Null
    }
    catch {
        # A conexão pode terminar antes de entregar a resposta durante o shutdown.
    }

    $shutdownLogs = Get-ContainerLogs $shutdownName
    foreach ($eventId in @(2710, 2722, 2711)) {
        if ($shutdownLogs -notmatch ('"EventId":' + $eventId)) {
            throw "O evento de shutdown $eventId não foi observado."
        }
    }
    if ($shutdownLogs -notmatch '"ApplicationStopping":true') {
        throw "A operação cancelada não registrou ApplicationStopping=true."
    }

    Write-Host "Smoke test Docker concluído com sucesso."
    Write-Host "Imagem: $Image"
    Write-Host "Health normal: live=200, ready=200"
    Write-Host "Dependência indisponível: live=200, ready=503, pedido=503"
    Write-Host "Shutdown: SIGTERM propagado, operação cancelada e ApplicationStopped observado"
}
finally {
    if ($null -ne $httpClient) {
        $httpClient.Dispose()
    }

    foreach ($container in $containers) {
        & docker rm --force $container 2>$null | Out-Null
    }
}

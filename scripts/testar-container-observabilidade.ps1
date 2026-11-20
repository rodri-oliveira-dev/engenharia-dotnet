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
$cleanupFailures = [System.Collections.Generic.List[string]]::new()
$httpClient = $null
$scriptFailure = $null

function Invoke-Docker {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker $($Arguments -join ' ') falhou com código $LASTEXITCODE."
    }
}

function Invoke-SmokeContainer {
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

function Get-ContainerLog {
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
        $logs = Get-ContainerLog $Name
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
        Write-Information "Construindo $Image..." -InformationAction Continue
        Invoke-Docker @("build", "--file", $dockerfile, "--tag", $Image, $repoRoot)
    }

    Write-Information "Validando container saudável e limites de recursos..." -InformationAction Continue
    $healthyName = "engenharia-dotnet-smoke-healthy-$suffix"
    $healthyPort = Invoke-SmokeContainer $healthyName @(
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
    $userComponent = ([string]$user -split ":", 2)[0].Trim()
    [uint32]$userId = 0
    $isNumericUser = [uint32]::TryParse($userComponent, [ref]$userId)
    if (
        [string]::IsNullOrWhiteSpace($userComponent) -or
        $userComponent -ieq "root" -or
        ($isNumericUser -and $userId -eq 0)
    ) {
        throw "O container está configurado com usuário privilegiado ou indefinido."
    }
    if ($stopSignal -ne "SIGTERM") {
        throw "STOPSIGNAL inesperado: $stopSignal."
    }
    if ($memory -ne 268435456 -or $nanoCpus -ne 1000000000) {
        throw "Limites inesperados: memória=$memory, NanoCPUs=$nanoCpus."
    }

    Write-Information "Validando diferença entre liveness e readiness..." -InformationAction Continue
    $degradedName = "engenharia-dotnet-smoke-degraded-$suffix"
    $degradedPort = Invoke-SmokeContainer $degradedName @(
        "--env", "Dependencias__Catalogo=http://127.0.0.1:1"
    )
    $degradedBaseUri = "http://127.0.0.1:$degradedPort"
    Wait-HttpStatus "$degradedBaseUri/health/live" 200
    Wait-HttpStatus "$degradedBaseUri/health/ready" 503
    Wait-HttpStatus "$degradedBaseUri/pedidos/11111111-1111-1111-1111-111111111111?atrasoMs=0" 503
    Wait-LogEvent $degradedName 2703

    Write-Information "Validando propagação de SIGTERM e cancelamento..." -InformationAction Continue
    $shutdownName = "engenharia-dotnet-smoke-shutdown-$suffix"
    $shutdownPort = Invoke-SmokeContainer $shutdownName @("--stop-timeout", "10")
    $shutdownBaseUri = "http://127.0.0.1:$shutdownPort"
    Wait-HttpStatus "$shutdownBaseUri/health/live" 200

    $requestTask = $httpClient.GetAsync("$shutdownBaseUri/operacoes/demorada?duracaoMs=30000")
    Wait-LogEvent $shutdownName 2720

    Invoke-Docker @("stop", "--time", "10", $shutdownName) | Out-Null
    try {
        $requestTask.Wait(5000) | Out-Null
    }
    catch {
        Write-Verbose "A conexão terminou durante o shutdown: $($_.Exception.Message)"
    }

    $shutdownLogs = Get-ContainerLog $shutdownName
    foreach ($eventId in @(2710, 2722, 2711)) {
        if ($shutdownLogs -notmatch ('"EventId":' + $eventId)) {
            throw "O evento de shutdown $eventId não foi observado."
        }
    }
    if ($shutdownLogs -notmatch '"ApplicationStopping":true') {
        throw "A operação cancelada não registrou ApplicationStopping=true."
    }

    Write-Information "Smoke test Docker concluído com sucesso." -InformationAction Continue
    Write-Information "Imagem: $Image" -InformationAction Continue
    Write-Information "Health normal: live=200, ready=200" -InformationAction Continue
    Write-Information "Dependência indisponível: live=200, ready=503, pedido=503" -InformationAction Continue
    Write-Information "Shutdown: SIGTERM propagado, operação cancelada e ApplicationStopped observado" -InformationAction Continue
}
catch {
    $scriptFailure = $_
}
finally {
    if ($null -ne $httpClient) {
        $httpClient.Dispose()
    }

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        foreach ($container in $containers) {
            try {
                $cleanupOutput = (& docker rm --force $container 2>&1) | Out-String
                if ($LASTEXITCODE -ne 0) {
                    $cleanupFailures.Add(
                        "docker rm --force $container falhou com código $LASTEXITCODE`: $($cleanupOutput.Trim())"
                    )
                }
            }
            catch {
                $cleanupFailures.Add(
                    "docker rm --force $container lançou uma exceção: $($_.Exception.Message)"
                )
            }
        }
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

if ($null -ne $scriptFailure) {
    if ($cleanupFailures.Count -gt 0) {
        Write-Warning ("Falhas adicionais durante a limpeza: " + ($cleanupFailures -join " | "))
    }

    throw $scriptFailure
}

if ($cleanupFailures.Count -gt 0) {
    throw ("A limpeza dos containers falhou: " + ($cleanupFailures -join " | "))
}

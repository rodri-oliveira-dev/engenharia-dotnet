#requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Python
)

$ErrorActionPreference = "Stop"

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solucao = Join-Path $raiz "exemplos/EngenhariaDotnet.slnx"
$verificador = Join-Path $raiz "scripts/verificar-vulnerabilidades-nuget.py"
$relatorio = Join-Path ([IO.Path]::GetTempPath()) (
    "engenharia-dotnet-nuget-{0}.json" -f [Guid]::NewGuid().ToString("N"))

try {
    & dotnet restore $solucao `
        "-p:NuGetAudit=true" `
        "-p:NuGetAuditMode=all" `
        "-p:NuGetAuditLevel=low"
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    [string[]]$json = & dotnet package list `
        --project $solucao `
        --vulnerable `
        --include-transitive `
        --no-restore `
        --format json `
        --output-version 1
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    [IO.File]::WriteAllLines(
        $relatorio,
        $json,
        [Text.UTF8Encoding]::new($false))

    & $Python $verificador $relatorio
    exit $LASTEXITCODE
}
finally {
    Remove-Item -LiteralPath $relatorio -Force -ErrorAction SilentlyContinue
}

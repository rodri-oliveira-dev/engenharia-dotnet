$ErrorActionPreference = "Stop"

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $raiz

try {
    $padrao = @(
        "-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----",
        "gh[pousr]_[A-Za-z0-9]{30,}",
        "github_pat_[A-Za-z0-9_]{20,}",
        "AKIA[0-9A-Z]{16}",
        "AIza[0-9A-Za-z_-]{35}",
        "(^|[^A-Za-z0-9])sk-(proj-)?[A-Za-z0-9_-]{20,}"
    ) -join "|"

    $resultados = & git grep -n -I -E -- $padrao -- . ":!scripts/verificar-segredos.ps1"
    $codigo = $LASTEXITCODE

    if ($codigo -eq 0) {
        $resultados | Write-Output
        throw "Possíveis segredos foram encontrados em arquivos versionados."
    }

    if ($codigo -gt 1) {
        throw "A verificação de segredos falhou com o código $codigo."
    }

    Write-Output "Verificação básica de segredos concluída sem ocorrências."
}
finally {
    Pop-Location
}

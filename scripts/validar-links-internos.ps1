$ErrorActionPreference = "Stop"

$raiz = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$codificacaoAnterior = [Console]::OutputEncoding
try {
    [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
    $saida = & git -C $raiz ls-files -z --cached --others --exclude-standard -- "*.md"
    $codigoGit = $LASTEXITCODE
}
finally {
    [Console]::OutputEncoding = $codificacaoAnterior
}
if ($codigoGit -ne 0) {
    throw "Não foi possível listar os arquivos Markdown pelo Git."
}
$caminhos = (($saida -join "") -split "`0") | Where-Object { $_ }
$arquivos = $caminhos | ForEach-Object {
    $caminho = Join-Path $raiz $_
    if (Test-Path -LiteralPath $caminho -PathType Leaf) {
        Get-Item -LiteralPath $caminho
    }
}
$falhas = [System.Collections.Generic.List[string]]::new()
$verificados = 0

foreach ($arquivo in $arquivos) {
    $conteudo = Get-Content -LiteralPath $arquivo.FullName -Raw -Encoding UTF8
    $links = [regex]::Matches($conteudo, "!?\[[^\]]*\]\((?<destino>[^)]+)\)")

    foreach ($link in $links) {
        $destino = $link.Groups["destino"].Value.Trim()
        if ($destino.StartsWith("<") -and $destino.EndsWith(">")) {
            $destino = $destino.Substring(1, $destino.Length - 2)
        }

        if ($destino -match "^(https?://|mailto:|#)") {
            continue
        }

        if ($destino -match "\s+[`"']") {
            $destino = ($destino -split "\s+[`"']")[0]
        }

        $caminhoRelativo = ($destino -split "#", 2)[0]
        if ([string]::IsNullOrWhiteSpace($caminhoRelativo)) {
            continue
        }

        $caminhoRelativo = [System.Uri]::UnescapeDataString($caminhoRelativo)
        $caminhoResolvido = [System.IO.Path]::GetFullPath(
            (Join-Path $arquivo.DirectoryName $caminhoRelativo))
        $verificados++

        if (-not (Test-Path -LiteralPath $caminhoResolvido)) {
            $origem = [System.IO.Path]::GetRelativePath($raiz, $arquivo.FullName)
            $falhas.Add("${origem}: destino inexistente: ${destino}")
        }
    }
}

if ($falhas.Count -gt 0) {
    $falhas | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Links internos verificados: $verificados; falhas: 0."

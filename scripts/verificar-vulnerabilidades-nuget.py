#!/usr/bin/env python3
"""Transforma o relatório NuGet JSON v1 em aprovação/falha do GitHub Actions.

O comando dotnet package list --vulnerable pode retornar exit code 0 mesmo
quando identifica vulnerabilidades. Auditar ambos os tipos de dependência.
"""

import json
import sys
from pathlib import Path


class RelatorioInvalido(ValueError):
    """Falha explícita quando não é possível confiar no resultado da auditoria."""


def encontrar_vulnerabilidades(relatorio):
    if not isinstance(relatorio, dict) or relatorio.get("version") != 1:
        raise RelatorioInvalido("Relatório inválido ou versão diferente de 1.")
    projetos = relatorio.get("projects")
    if not isinstance(projetos, list) or not projetos:
        raise RelatorioInvalido("Relatório sem projetos verificáveis.")

    achados = []
    for projeto in projetos:
        if not isinstance(projeto, dict) or not isinstance(projeto.get("path"), str):
            raise RelatorioInvalido("Projeto inválido.")
        frameworks = projeto.get("frameworks", [])
        if not isinstance(frameworks, list):
            raise RelatorioInvalido("Lista de frameworks inválida.")
        for framework in frameworks:
            if not isinstance(framework, dict):
                raise RelatorioInvalido("Framework inválido.")
            for tipo, campo in (("direta", "topLevelPackages"), ("transitiva", "transitivePackages")):
                pacotes = framework.get(campo, [])
                if not isinstance(pacotes, list):
                    raise RelatorioInvalido("Lista de pacotes inválida.")
                for pacote in pacotes:
                    if not isinstance(pacote, dict):
                        raise RelatorioInvalido("Pacote inválido.")
                    vulnerabilidades = pacote.get("vulnerabilities", [])
                    if not isinstance(vulnerabilidades, list):
                        raise RelatorioInvalido("Vulnerabilidades inválidas.")
                    for vulnerabilidade in vulnerabilidades:
                        if not isinstance(vulnerabilidade, dict):
                            raise RelatorioInvalido("Detalhes da vulnerabilidade inválidos.")
                        achados.append(
                            f"{projeto['path']} [{framework.get('framework', '?')}] "
                            f"{tipo}: {pacote.get('id', '?')} {pacote.get('resolvedVersion', '?')} "
                            f"| {vulnerabilidade.get('severity', '?')} "
                            f"| {vulnerabilidade.get('advisoryurl', '?')}"
                        )
    return achados


def executar(caminho):
    try:
        dados = json.loads(Path(caminho).read_text(encoding="utf-8-sig"))
        achados = encontrar_vulnerabilidades(dados)
    except (OSError, UnicodeError, json.JSONDecodeError, RelatorioInvalido) as erro:
        print(f"::error::Auditoria NuGet inconclusiva: {erro}", file=sys.stderr)
        return 2
    if achados:
        print(f"::error::Foram encontradas {len(achados)} vulnerabilidades NuGet.", file=sys.stderr)
        for achado in achados:
            print(f"  - {achado}", file=sys.stderr)
        return 1
    print("Auditoria NuGet aprovada: nenhuma vulnerabilidade direta ou transitiva.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Uso: python scripts/verificar-vulnerabilidades-nuget.py RELATORIO.json", file=sys.stderr)
        sys.exit(2)
    sys.exit(executar(sys.argv[1]))

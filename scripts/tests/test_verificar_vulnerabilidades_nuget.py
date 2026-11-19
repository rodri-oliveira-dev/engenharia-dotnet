"""Testes da política de segurança com relatórios simulados."""

import importlib.util
import io
from contextlib import redirect_stderr, redirect_stdout
import json
import tempfile
import unittest
from pathlib import Path

ARQUIVO = Path(__file__).resolve().parents[1] / "verificar-vulnerabilidades-nuget.py"
spec = importlib.util.spec_from_file_location("verificador_nuget", ARQUIVO)
assert spec is not None and spec.loader is not None
verificador = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verificador)


def relatorio(diretos=None, transitivos=None):
    return {"version": 1, "projects": [
        {"path": "exemplos/Projeto.csproj", "frameworks": [
            {"framework": "net11.0", "topLevelPackages": diretos or [],
             "transitivePackages": transitivos or []}
        ]}
    ]}


def vulneravel():
    return {"id": "Pacote.Exemplo", "resolvedVersion": "1.0.0",
            "vulnerabilities": [{"severity": "High",
                                 "advisoryurl": "https://github.com/advisories/GHSA-test"}]}


class VerificadorNuGetTests(unittest.TestCase):
    def executar(self, dados):
        with tempfile.TemporaryDirectory() as pasta:
            caminho = Path(pasta) / "audit.json"
            caminho.write_text(json.dumps(dados), encoding="utf-8")
            # Casos de falha são intencionais: não gerar ::error:: no job verde.
            with redirect_stderr(io.StringIO()), redirect_stdout(io.StringIO()):
                return verificador.executar(caminho)

    def test_relatorio_limpo_aprova(self):
        self.assertEqual(0, self.executar(relatorio()))

    def test_pacote_direto_vulneravel_reprova(self):
        self.assertEqual(1, self.executar(relatorio(diretos=[vulneravel()])))

    def test_pacote_transitivo_vulneravel_reprova(self):
        self.assertEqual(1, self.executar(relatorio(transitivos=[vulneravel()])))

    def test_pacote_vulneravel_em_outro_framework_reprova(self):
        dados = relatorio()
        dados["projects"].append({"path": "exemplos/Outro.csproj",
                                  "frameworks": [
                                      {"framework": "net10.0"},
                                      {"framework": "net11.0",
                                       "transitivePackages": [vulneravel()]}]})
        self.assertEqual(1, self.executar(dados))

    def test_sem_projetos_reprova(self):
        self.assertEqual(2, self.executar({"version": 1, "projects": []}))

    def test_versao_desconhecida_reprova(self):
        self.assertEqual(2, self.executar({"version": 2, "projects": []}))

    def test_pacotes_malformados_reprovam(self):
        dados = relatorio()
        dados["projects"][0]["frameworks"][0]["topLevelPackages"] = "inválido"
        self.assertEqual(2, self.executar(dados))

    def test_json_invalido_reprova(self):
        with tempfile.TemporaryDirectory() as pasta:
            caminho = Path(pasta) / "audit.json"
            caminho.write_text("{", encoding="utf-8")
            with redirect_stderr(io.StringIO()), redirect_stdout(io.StringIO()):
                self.assertEqual(2, verificador.executar(caminho))


if __name__ == "__main__":
    unittest.main()

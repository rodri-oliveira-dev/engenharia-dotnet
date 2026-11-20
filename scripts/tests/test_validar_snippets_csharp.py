import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "validar-snippets-csharp.py"
SPEC = importlib.util.spec_from_file_location("validar_snippets_csharp", SCRIPT_PATH)
VALIDATOR = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = VALIDATOR
SPEC.loader.exec_module(VALIDATOR)


class SnippetValidatorTests(unittest.TestCase):
    def create_root(self, markdown: str) -> Path:
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        (root / "docs").mkdir()
        (root / "docs" / "exemplo.md").write_text(markdown, encoding="utf-8")
        (root / "global.json").write_text(
            json.dumps({"sdk": {"version": "11.0.100"}}), encoding="utf-8"
        )
        return root

    @staticmethod
    def manifest(snippets=None):
        return {
            "schema_version": 1,
            "target_framework": "net11.0",
            "language_version": "15.0",
            "snippets": snippets or {},
        }

    def test_extracts_code_and_original_line(self):
        root = self.create_root(
            "# Exemplo\n\n"
            "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n"
            "```csharp\n"
            "Console.WriteLine(42);\n"
            "```\n"
        )

        snippets = VALIDATOR.extract_snippets(root)

        self.assertEqual(["teste-01"], list(snippets))
        self.assertEqual(5, snippets["teste-01"].code_line)
        self.assertEqual("Console.WriteLine(42);\n", snippets["teste-01"].code)

    def test_extracts_tilde_and_four_backtick_fences(self):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n"
            "~~~csharp\nConsole.WriteLine(1);\n~~~\n\n"
            "<!-- snippet-csharp: id=teste-02; categoria=isolado -->\n"
            "````cs\nConsole.WriteLine(2);\n````\n"
        )

        snippets = VALIDATOR.extract_snippets(root)

        self.assertEqual(["teste-01", "teste-02"], list(snippets))
        self.assertEqual("Console.WriteLine(1);\n", snippets["teste-01"].code)
        self.assertEqual("Console.WriteLine(2);\n", snippets["teste-02"].code)

    def test_four_backticks_require_matching_closure(self):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n"
            "````csharp\n"
            "Console.WriteLine(1);\n"
            "```\n"
            "Console.WriteLine(2);\n"
            "````\n\n"
            "<!-- snippet-csharp: id=teste-02; categoria=isolado -->\n"
            "~~~c#\nConsole.WriteLine(3);\n~~~~\n"
        )

        snippets = VALIDATOR.extract_snippets(root)

        self.assertIn("```\nConsole.WriteLine(2);", snippets["teste-01"].code)
        self.assertEqual("Console.WriteLine(3);\n", snippets["teste-02"].code)

    def test_ignores_metadata_example_inside_another_fence(self):
        root = self.create_root(
            "```markdown\n"
            "<!-- snippet-csharp: id=exemplo-01; categoria=isolado -->\n"
            "```\n"
        )

        self.assertEqual({}, VALIDATOR.extract_snippets(root))

    def test_rejects_missing_or_malformed_metadata(self):
        cases = {
            "ausente": "```csharp\nvar valor = 1;\n```\n",
            "ausente_til": "~~~csharp\nvar valor = 1;\n~~~\n",
            "ausente_quatro_crases": "````csharp\nvar valor = 1;\n````\n",
            "malformado": (
                "<!-- snippet-csharp: id=TESTE; categoria=isolado -->\n"
                "```csharp\nvar valor = 1;\n```\n"
            ),
            "orfao": (
                "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n\n"
                "```csharp\nvar valor = 1;\n```\n"
            ),
        }

        for name, markdown in cases.items():
            with self.subTest(name=name):
                root = self.create_root(markdown)
                with self.assertRaises(VALIDATOR.ValidationError):
                    VALIDATOR.extract_snippets(root)

    def test_rejects_duplicate_identifier(self):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n"
            "```csharp\nvar primeiro = 1;\n```\n\n"
            "<!-- snippet-csharp: id=teste-01; categoria=contexto -->\n"
            "```csharp\nvar segundo = 2;\n```\n"
        )

        with self.assertRaisesRegex(VALIDATOR.ValidationError, "duplicado"):
            VALIDATOR.extract_snippets(root)

    def test_excludes_fragment_and_invalid_code(self):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=fragmento -->\n"
            "```csharp\nvar fragmento = ;\n```\n\n"
            "<!-- snippet-csharp: id=teste-02; categoria=invalido -->\n"
            "```csharp\nvar invalido = ;\n```\n"
        )
        discovered = VALIDATOR.extract_snippets(root)

        configured, pending, excluded = VALIDATOR.select_snippets(
            discovered, self.manifest()
        )

        self.assertEqual([], configured)
        self.assertEqual([], pending)
        self.assertEqual(["teste-01", "teste-02"], [item.identifier for item in excluded])

    def test_generates_isolated_project_with_imports_and_auxiliary_types(self):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=contexto -->\n"
            "```csharp\n"
            "public string Obter() => Auxiliar.Valor;\n"
            "```\n"
        )
        discovered = VALIDATOR.extract_snippets(root)
        manifest = self.manifest(
            {
                "teste-01": {
                    "output_type": "Library",
                    "imports": ["System"],
                    "prefix": "public sealed class Exemplo\n{\n",
                    "suffix": "}\n",
                    "auxiliary_sources": [
                        {
                            "file": "Auxiliar.cs",
                            "content": (
                                "public static class Auxiliar\n"
                                "{\n"
                                "    public const string Valor = \"ok\";\n"
                                "}\n"
                            ),
                        }
                    ],
                }
            }
        )
        configured, _, _ = VALIDATOR.select_snippets(discovered, manifest)
        observed_temporary_roots = []

        def successful_runner(command, **kwargs):
            temporary_root = Path(kwargs["cwd"])
            if command[1] == "--version":
                self.assertTrue((temporary_root / "global.json").exists())
                return subprocess.CompletedProcess(command, 0, "11.0.100\n", "")

            observed_temporary_roots.append(temporary_root)
            project = temporary_root / "teste-01"
            source = (project / "Snippet.cs").read_text(encoding="utf-8")
            csproj = (project / "Snippet.csproj").read_text(encoding="utf-8")
            self.assertTrue((temporary_root / "global.json").exists())
            self.assertIn("using System;", source)
            self.assertIn('#line 3 "docs/exemplo.md"', source)
            self.assertIn("public sealed class Exemplo", source)
            self.assertTrue((project / "Auxiliar.cs").exists())
            self.assertIn("<Nullable>enable</Nullable>", csproj)
            self.assertIn("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", csproj)
            self.assertIn("<LangVersion>15.0</LangVersion>", csproj)
            self.assertEqual("dotnet", command[0])
            return subprocess.CompletedProcess(command, 0, "Build succeeded.", "")

        results = VALIDATOR.compile_snippets(
            root, configured, manifest, runner=successful_runner
        )

        self.assertTrue(results[0].succeeded)
        self.assertEqual(1, len(observed_temporary_roots))
        self.assertFalse(observed_temporary_roots[0].exists())

    def test_reports_compiler_failure_with_original_origin(self):
        root = self.create_root(
            "# Título\n\n"
            "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n"
            "```csharp\n"
            "int valor = \"texto\";\n"
            "```\n"
        )
        discovered = VALIDATOR.extract_snippets(root)
        manifest = self.manifest({"teste-01": {"output_type": "Exe"}})
        configured, _, _ = VALIDATOR.select_snippets(discovered, manifest)
        observed_temporary_roots = []

        def failing_runner(command, **kwargs):
            if command[1] == "--version":
                return subprocess.CompletedProcess(command, 0, "11.0.100\n", "")
            observed_temporary_roots.append(Path(kwargs["cwd"]))
            diagnostic = (
                "docs/exemplo.md(5,13): error CS0029: "
                "Cannot implicitly convert type 'string' to 'int'"
            )
            return subprocess.CompletedProcess(command, 1, diagnostic, "")

        results = VALIDATOR.compile_snippets(
            root, configured, manifest, runner=failing_runner
        )

        self.assertFalse(results[0].succeeded)
        self.assertIn("docs/exemplo.md(5,13)", results[0].diagnostics)
        self.assertEqual(5, results[0].snippet.code_line)
        self.assertFalse(observed_temporary_roots[0].exists())

    def compile_executable(self, execution, execution_results):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=isolado -->\n"
            "```csharp\nConsole.WriteLine(\"resultado\");\n```\n"
        )
        manifest = self.manifest(
            {
                "teste-01": {
                    "output_type": "Exe",
                    "execution": execution,
                }
            }
        )
        discovered = VALIDATOR.extract_snippets(root)
        configured, _, _ = VALIDATOR.select_snippets(discovered, manifest)
        calls = []

        def runner(command, **kwargs):
            if command[1] == "--version":
                return subprocess.CompletedProcess(command, 0, "11.0.100\n", "")
            if command[1] == "build":
                return subprocess.CompletedProcess(command, 0, "Build succeeded.", "")

            calls.append((command, kwargs))
            result = execution_results[len(calls) - 1]
            if isinstance(result, Exception):
                raise result
            return subprocess.CompletedProcess(command, *result)

        with mock.patch.dict("os.environ", {"SEGREDO_TESTE": "nao-propagar"}):
            results = VALIDATOR.compile_snippets(
                root, configured, manifest, runner=runner
            )
        return results[0], calls

    def test_validates_expected_stdout_twice_in_isolated_processes(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_stdout": "resultado\n",
            "timeout_seconds": 3,
        }

        result, calls = self.compile_executable(
            execution,
            [(0, "resultado\r\n", ""), (0, "resultado\n", "")],
        )

        self.assertTrue(result.succeeded)
        self.assertTrue(result.execution.succeeded)
        self.assertEqual(2, len(calls))
        self.assertNotEqual(calls[0][1]["cwd"], calls[1][1]["cwd"])
        self.assertNotIn("SEGREDO_TESTE", calls[0][1]["env"])
        self.assertIs(subprocess.DEVNULL, calls[0][1]["stdin"])
        self.assertEqual(3, calls[0][1]["timeout"])
        isolation_key = (
            "creationflags" if VALIDATOR.os.name == "nt" else "start_new_session"
        )
        self.assertIn(isolation_key, calls[0][1])
        self.assertFalse(Path(calls[0][1]["cwd"]).parents[2].exists())

    def test_reports_stdout_difference(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_stdout": "esperado\n",
        }

        result, _ = self.compile_executable(
            execution,
            [(0, "real\n", ""), (0, "real\n", "")],
        )

        self.assertFalse(result.succeeded)
        self.assertIn("saída padrão divergente", result.diagnostics)
        self.assertIn("--- esperado", result.diagnostics)
        self.assertIn("+++ real", result.diagnostics)

    def test_rejects_non_deterministic_result(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_stdout": "resultado\n",
        }

        result, _ = self.compile_executable(
            execution,
            [(0, "primeiro\n", ""), (0, "segundo\n", "")],
        )

        self.assertFalse(result.succeeded)
        self.assertIn("resultado não determinístico", result.diagnostics)

    def test_validates_expected_exception(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_exception": "System.InvalidOperationException",
        }
        stderr = "__SNIPPET_EXCEPTION_TYPE__:System.InvalidOperationException\n"

        result, calls = self.compile_executable(
            execution,
            [(1, "", stderr), (1, "", stderr)],
        )

        self.assertTrue(result.succeeded)
        self.assertEqual(2, len(calls))

    def test_rejects_expected_name_printed_by_wrong_exception(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_exception": "System.NullReferenceException",
        }
        stderr = "__SNIPPET_EXCEPTION_TYPE__:System.InvalidOperationException\n"

        result, _ = self.compile_executable(
            execution,
            [
                (1, "System.NullReferenceException\n", stderr),
                (1, "System.NullReferenceException\n", stderr),
            ],
        )

        self.assertFalse(result.succeeded)
        self.assertIn("System.InvalidOperationException", result.diagnostics)

    def test_reports_execution_timeout(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_stdout": "",
            "timeout_seconds": 1,
        }
        timeout = subprocess.TimeoutExpired(["dotnet", "Snippet.dll"], 1)

        result, calls = self.compile_executable(execution, [timeout])

        self.assertFalse(result.succeeded)
        self.assertEqual(1, len(calls))
        self.assertIn("excedeu o timeout de 1 segundo", result.diagnostics)
        self.assertFalse(Path(calls[0][1]["cwd"]).parents[2].exists())

    def test_reports_missing_expected_exception(self):
        execution = {
            "classification": "deterministic-safe",
            "expected_exception": "System.InvalidOperationException",
        }

        result, _ = self.compile_executable(
            execution,
            [(0, "resultado\n", ""), (0, "resultado\n", "")],
        )

        self.assertFalse(result.succeeded)
        self.assertIn("era esperada a exceção", result.diagnostics)

    def test_requires_explicit_safe_execution_classification(self):
        execution = {"expected_stdout": "resultado\n"}

        with self.assertRaisesRegex(
            VALIDATOR.ValidationError, "classification deve ser deterministic-safe"
        ):
            self.compile_executable(execution, [])

    def test_rejects_manifest_that_selects_fragment(self):
        root = self.create_root(
            "<!-- snippet-csharp: id=teste-01; categoria=fragmento -->\n"
            "```csharp\nvar valor = ;\n```\n"
        )
        discovered = VALIDATOR.extract_snippets(root)
        manifest = self.manifest({"teste-01": {"output_type": "Exe"}})

        with self.assertRaisesRegex(VALIDATOR.ValidationError, "não pode ser compilado"):
            VALIDATOR.select_snippets(discovered, manifest)


if __name__ == "__main__":
    unittest.main()

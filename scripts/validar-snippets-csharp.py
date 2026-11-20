#!/usr/bin/env python3
"""Extrai, compila e valida snippets C# classificados em Markdown."""

from __future__ import annotations

import argparse
import difflib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Sequence


METADATA_RE = re.compile(
    r"<!-- snippet-csharp: id=(?P<id>[a-z][a-z0-9]*(?:-[a-z0-9]+)+); "
    r"categoria=(?P<category>isolado|contexto|fragmento|invalido) -->"
)
CSHARP_FENCE_RE = re.compile(r"\s*```(?:csharp|cs|c#)\s*")
OPENING_FENCE_RE = re.compile(r"\s*```.*")
CLOSING_FENCE_RE = re.compile(r"\s*```\s*")
ELIGIBLE_CATEGORIES = frozenset({"isolado", "contexto"})
EXCEPTION_MARKER = "__SNIPPET_EXCEPTION_TYPE__:"


class ValidationError(Exception):
    """Erro de entrada que impede uma validação confiável."""


@dataclass(frozen=True)
class Snippet:
    identifier: str
    category: str
    path: Path
    relative_path: str
    fence_line: int
    code_line: int
    code: str


@dataclass(frozen=True)
class ExecutionResult:
    succeeded: bool
    diagnostics: str


@dataclass(frozen=True)
class CompilationResult:
    snippet: Snippet
    succeeded: bool
    diagnostics: str
    execution: ExecutionResult | None = None


def _relative_path(root: Path, path: Path) -> str:
    return path.relative_to(root).as_posix()


def extract_snippets(root: Path) -> dict[str, Snippet]:
    """Extrai fences C# e valida metadados, IDs e fechamento dos blocos."""

    snippets: dict[str, Snippet] = {}
    errors: list[str] = []

    markdown_files = sorted(
        path
        for path in root.rglob("*.md")
        if ".git" not in path.relative_to(root).parts
    )

    for path in markdown_files:
        relative = _relative_path(root, path)
        lines = path.read_text(encoding="utf-8").splitlines()
        index = 0

        while index < len(lines):
            line = lines[index]

            if line.startswith("<!-- snippet-csharp:"):
                metadata = METADATA_RE.fullmatch(line)
                if metadata is None:
                    errors.append(f"{relative}:{index + 1}: metadado inválido")
                    index += 1
                    continue

                if index + 1 >= len(lines) or not CSHARP_FENCE_RE.fullmatch(
                    lines[index + 1]
                ):
                    errors.append(
                        f"{relative}:{index + 1}: metadado não precede um fence C#"
                    )
                    index += 1
                    continue

                fence_index = index + 1
                closing_index = fence_index + 1
                while closing_index < len(lines) and not CLOSING_FENCE_RE.fullmatch(
                    lines[closing_index]
                ):
                    closing_index += 1

                if closing_index == len(lines):
                    errors.append(
                        f"{relative}:{fence_index + 1}: fence C# sem fechamento"
                    )
                    break

                identifier = metadata.group("id")
                if identifier in snippets:
                    first = snippets[identifier]
                    errors.append(
                        f"{relative}:{index + 1}: ID '{identifier}' duplicado; "
                        f"primeira ocorrência em {first.relative_path}:"
                        f"{first.fence_line - 1}"
                    )
                else:
                    snippets[identifier] = Snippet(
                        identifier=identifier,
                        category=metadata.group("category"),
                        path=path,
                        relative_path=relative,
                        fence_line=fence_index + 1,
                        code_line=fence_index + 2,
                        code="\n".join(lines[fence_index + 1 : closing_index]) + "\n",
                    )

                index = closing_index + 1
                continue

            if CSHARP_FENCE_RE.fullmatch(line):
                errors.append(f"{relative}:{index + 1}: fence C# sem metadado")

            if OPENING_FENCE_RE.fullmatch(line):
                closing_index = index + 1
                while closing_index < len(lines) and not CLOSING_FENCE_RE.fullmatch(
                    lines[closing_index]
                ):
                    closing_index += 1
                index = min(closing_index + 1, len(lines))
                continue

            index += 1

    if errors:
        raise ValidationError("\n".join(errors))

    return snippets


def load_manifest(path: Path, global_json_path: Path) -> dict:
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
        global_json = json.loads(global_json_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise ValidationError(f"Não foi possível ler a configuração: {error}") from error

    if manifest.get("schema_version") != 1:
        raise ValidationError("schema_version do manifesto deve ser 1")

    snippets = manifest.get("snippets")
    if not isinstance(snippets, dict):
        raise ValidationError("snippets deve ser um objeto no manifesto")

    sdk_version = global_json.get("sdk", {}).get("version")
    if not isinstance(sdk_version, str):
        raise ValidationError("global.json não declara sdk.version")

    target_framework = manifest.get("target_framework")
    expected_framework = f"net{sdk_version.split('.', 1)[0]}.0"
    if target_framework != expected_framework:
        raise ValidationError(
            f"target_framework deve ser {expected_framework} para o SDK {sdk_version}"
        )

    if manifest.get("language_version") != "15.0":
        raise ValidationError("language_version deve ser 15.0")

    return manifest


def select_snippets(
    discovered: dict[str, Snippet], manifest: dict
) -> tuple[list[tuple[Snippet, dict]], list[Snippet], list[Snippet]]:
    configured: list[tuple[Snippet, dict]] = []
    manifest_entries = manifest["snippets"]

    for identifier, configuration in manifest_entries.items():
        if identifier not in discovered:
            raise ValidationError(f"Snippet configurado não encontrado: {identifier}")
        if not isinstance(configuration, dict):
            raise ValidationError(f"Configuração de {identifier} deve ser um objeto")

        snippet = discovered[identifier]
        if snippet.category not in ELIGIBLE_CATEGORIES:
            raise ValidationError(
                f"{identifier} tem categoria {snippet.category} e não pode ser compilado"
            )
        _validate_configuration(identifier, configuration)
        configured.append((snippet, configuration))

    configured.sort(key=lambda item: item[0].identifier)
    configured_ids = set(manifest_entries)
    pending = sorted(
        (
            snippet
            for snippet in discovered.values()
            if snippet.category in ELIGIBLE_CATEGORIES
            and snippet.identifier not in configured_ids
        ),
        key=lambda snippet: snippet.identifier,
    )
    excluded = sorted(
        (
            snippet
            for snippet in discovered.values()
            if snippet.category not in ELIGIBLE_CATEGORIES
        ),
        key=lambda snippet: snippet.identifier,
    )
    return configured, pending, excluded


def _validate_configuration(identifier: str, configuration: dict) -> None:
    output_type = configuration.get("output_type")
    if output_type not in {"Exe", "Library"}:
        raise ValidationError(f"{identifier}: output_type deve ser Exe ou Library")

    imports = configuration.get("imports", [])
    if not isinstance(imports, list) or not all(
        isinstance(item, str) and item for item in imports
    ):
        raise ValidationError(f"{identifier}: imports deve ser uma lista de strings")

    for field in ("prefix", "suffix"):
        value = configuration.get(field, "")
        if not isinstance(value, str):
            raise ValidationError(f"{identifier}: {field} deve ser uma string")

    auxiliary_sources = configuration.get("auxiliary_sources", [])
    if not isinstance(auxiliary_sources, list):
        raise ValidationError(
            f"{identifier}: auxiliary_sources deve ser uma lista"
        )

    names: set[str] = {"Snippet.cs"}
    for source in auxiliary_sources:
        if not isinstance(source, dict):
            raise ValidationError(
                f"{identifier}: cada fonte auxiliar deve ser um objeto"
            )
        name = source.get("file")
        content = source.get("content")
        if (
            not isinstance(name, str)
            or Path(name).name != name
            or not name.endswith(".cs")
            or name in names
        ):
            raise ValidationError(f"{identifier}: nome de fonte auxiliar inválido")
        if not isinstance(content, str):
            raise ValidationError(
                f"{identifier}: conteúdo da fonte auxiliar deve ser uma string"
            )
        names.add(name)

    execution = configuration.get("execution")
    if execution is None:
        return
    if output_type != "Exe":
        raise ValidationError(
            f"{identifier}: somente projetos Exe podem declarar execution"
        )
    if not isinstance(execution, dict):
        raise ValidationError(f"{identifier}: execution deve ser um objeto")
    if execution.get("classification") != "deterministic-safe":
        raise ValidationError(
            f"{identifier}: execution.classification deve ser deterministic-safe"
        )

    timeout_seconds = execution.get("timeout_seconds", 5)
    if (
        isinstance(timeout_seconds, bool)
        or not isinstance(timeout_seconds, (int, float))
        or not 0 < timeout_seconds <= 30
    ):
        raise ValidationError(
            f"{identifier}: timeout_seconds deve estar entre 0 e 30"
        )

    has_stdout = "expected_stdout" in execution
    has_exception = "expected_exception" in execution
    if has_stdout == has_exception:
        raise ValidationError(
            f"{identifier}: declare exatamente expected_stdout ou expected_exception"
        )
    if has_stdout and not isinstance(execution["expected_stdout"], str):
        raise ValidationError(f"{identifier}: expected_stdout deve ser uma string")
    if has_exception and (
        not isinstance(execution["expected_exception"], str)
        or not re.fullmatch(
            r"[A-Za-z_][A-Za-z0-9_.]+", execution["expected_exception"]
        )
    ):
        raise ValidationError(
            f"{identifier}: expected_exception deve ser um nome de tipo qualificado"
        )


def _source_for(snippet: Snippet, configuration: dict) -> str:
    imports = "".join(f"using {item};\n" for item in configuration.get("imports", []))
    prefix = configuration.get("prefix", "")
    suffix = configuration.get("suffix", "")
    mapped_path = snippet.relative_path.replace('"', '\\"')
    return (
        imports
        + prefix
        + f'#line {snippet.code_line} "{mapped_path}"\n'
        + snippet.code
        + "#line default\n"
        + suffix
    )


def _project_for(manifest: dict, configuration: dict) -> str:
    return f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>{configuration["output_type"]}</OutputType>
    <TargetFramework>{manifest["target_framework"]}</TargetFramework>
    <LangVersion>{manifest["language_version"]}</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
"""


Runner = Callable[..., subprocess.CompletedProcess[str]]


def _normalize_output(value: str) -> str:
    return value.replace("\r\n", "\n").replace("\r", "\n")


def _isolated_environment(runtime_directory: Path) -> dict[str, str]:
    allowed_keys = (
        "COMSPEC",
        "DOTNET_ROOT",
        "DOTNET_ROOT(x86)",
        "LANG",
        "LC_ALL",
        "PATH",
        "PATHEXT",
        "SYSTEMROOT",
        "WINDIR",
    )
    environment = {
        key: os.environ[key] for key in allowed_keys if key in os.environ
    }
    home = runtime_directory / "home"
    temporary = runtime_directory / "tmp"
    home.mkdir()
    temporary.mkdir()
    environment.update(
        {
            "COMPlus_EnableDiagnostics": "0",
            "DOTNET_BUNDLE_EXTRACT_BASE_DIR": str(runtime_directory / "bundle"),
            "DOTNET_CLI_HOME": str(home),
            "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
            "DOTNET_EnableDiagnostics": "0",
            "DOTNET_NOLOGO": "1",
            "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
            "HOME": str(home),
            "TEMP": str(temporary),
            "TMP": str(temporary),
            "TMPDIR": str(temporary),
            "USERPROFILE": str(home),
        }
    )
    return environment


def _execution_diff(expected: str, actual: str) -> str:
    lines = difflib.unified_diff(
        expected.splitlines(keepends=True),
        actual.splitlines(keepends=True),
        fromfile="esperado",
        tofile="real",
    )
    difference = "".join(lines)
    if difference:
        return difference
    return f"esperado={expected!r}\nreal={actual!r}"


def _validate_execution(
    snippet: Snippet,
    configuration: dict,
    project_directory: Path,
    manifest: dict,
    dotnet: str,
    runner: Runner,
) -> ExecutionResult:
    expectation = configuration["execution"]
    timeout_seconds = expectation.get("timeout_seconds", 5)
    assembly = (
        project_directory
        / "bin"
        / "Release"
        / manifest["target_framework"]
        / "Snippet.dll"
    )
    observations: list[tuple[int, str, str]] = []

    for attempt in range(1, 3):
        runtime_directory = project_directory / "runtime" / f"run-{attempt}"
        runtime_directory.mkdir(parents=True)
        isolation: dict = {"start_new_session": True}
        if os.name == "nt":
            isolation = {"creationflags": subprocess.CREATE_NEW_PROCESS_GROUP}

        try:
            process = runner(
                [dotnet, str(assembly)],
                cwd=runtime_directory,
                env=_isolated_environment(runtime_directory),
                text=True,
                stdin=subprocess.DEVNULL,
                capture_output=True,
                timeout=timeout_seconds,
                check=False,
                **isolation,
            )
        except subprocess.TimeoutExpired:
            return ExecutionResult(
                succeeded=False,
                diagnostics=(
                    f"{snippet.relative_path}:{snippet.code_line}: execução {attempt} "
                    f"excedeu o timeout de {timeout_seconds} segundo(s)"
                ),
            )
        except FileNotFoundError as error:
            return ExecutionResult(
                succeeded=False,
                diagnostics=(
                    f"{snippet.relative_path}:{snippet.code_line}: "
                    f"não foi possível iniciar o processo: {error}"
                ),
            )

        observations.append(
            (
                process.returncode,
                _normalize_output(process.stdout or ""),
                _normalize_output(process.stderr or ""),
            )
        )

    if observations[0] != observations[1]:
        return ExecutionResult(
            succeeded=False,
            diagnostics=(
                f"{snippet.relative_path}:{snippet.code_line}: resultado não "
                "determinístico entre duas execuções\n"
                f"execução 1={observations[0]!r}\n"
                f"execução 2={observations[1]!r}"
            ),
        )

    return_code, standard_output, standard_error = observations[0]
    if "expected_stdout" in expectation:
        expected_output = _normalize_output(expectation["expected_stdout"])
        if return_code != 0:
            return ExecutionResult(
                succeeded=False,
                diagnostics=(
                    f"{snippet.relative_path}:{snippet.code_line}: saída esperada, "
                    f"mas o processo terminou com código {return_code}\n"
                    f"stderr:\n{standard_error}"
                ),
            )
        if standard_error:
            return ExecutionResult(
                succeeded=False,
                diagnostics=(
                    f"{snippet.relative_path}:{snippet.code_line}: o processo "
                    f"escreveu em stderr:\n{standard_error}"
                ),
            )
        if standard_output != expected_output:
            return ExecutionResult(
                succeeded=False,
                diagnostics=(
                    f"{snippet.relative_path}:{snippet.code_line}: saída padrão "
                    "divergente\n"
                    + _execution_diff(expected_output, standard_output)
                ),
            )
        return ExecutionResult(succeeded=True, diagnostics="")

    expected_exception = expectation["expected_exception"]
    if return_code == 0:
        return ExecutionResult(
            succeeded=False,
            diagnostics=(
                f"{snippet.relative_path}:{snippet.code_line}: era esperada a "
                f"exceção {expected_exception}, mas o processo terminou com sucesso"
            ),
        )
    reported_exceptions = [
        line.removeprefix(EXCEPTION_MARKER)
        for line in standard_error.splitlines()
        if line.startswith(EXCEPTION_MARKER)
    ]
    if reported_exceptions != [expected_exception]:
        return ExecutionResult(
            succeeded=False,
            diagnostics=(
                f"{snippet.relative_path}:{snippet.code_line}: exceção esperada "
                f"{expected_exception}; tipos registrados: {reported_exceptions!r}\n"
                f"stdout:\n{standard_output}\nstderr:\n{standard_error}"
            ),
        )
    return ExecutionResult(succeeded=True, diagnostics="")


def compile_snippets(
    root: Path,
    configured: Sequence[tuple[Snippet, dict]],
    manifest: dict,
    *,
    dotnet: str = "dotnet",
    runner: Runner = subprocess.run,
    temporary_parent: Path | None = None,
) -> list[CompilationResult]:
    """Compila, executa os opt-ins e remove todo o diretório temporário."""

    global_json_path = root / "global.json"
    results: list[CompilationResult] = []

    with tempfile.TemporaryDirectory(
        prefix="engenharia-dotnet-snippets-",
        dir=temporary_parent,
    ) as temporary_name:
        temporary_root = Path(temporary_name)
        shutil.copyfile(global_json_path, temporary_root / "global.json")
        environment = os.environ.copy()
        environment.update(
            {
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
                "DOTNET_NOLOGO": "1",
                "NUGET_PACKAGES": str(temporary_root / ".nuget" / "packages"),
            }
        )

        try:
            sdk_process = runner(
                [dotnet, "--version"],
                cwd=temporary_root,
                env=environment,
                text=True,
                capture_output=True,
                timeout=30,
                check=False,
            )
        except (FileNotFoundError, subprocess.TimeoutExpired) as error:
            raise ValidationError(f"Não foi possível iniciar o SDK .NET: {error}") from error

        if sdk_process.returncode != 0:
            diagnostics = "\n".join(
                part.strip()
                for part in (sdk_process.stdout, sdk_process.stderr)
                if part and part.strip()
            )
            raise ValidationError(
                "O SDK definido em global.json não está disponível.\n" + diagnostics
            )

        requested_sdk = json.loads(global_json_path.read_text(encoding="utf-8"))["sdk"][
            "version"
        ]
        resolved_sdk = sdk_process.stdout.strip()
        if resolved_sdk.split(".", 1)[0] != requested_sdk.split(".", 1)[0]:
            raise ValidationError(
                f"O SDK resolvido ({resolved_sdk}) não atende ao solicitado "
                f"({requested_sdk})."
            )

        for snippet, configuration in configured:
            _validate_configuration(snippet.identifier, configuration)
            project_directory = temporary_root / snippet.identifier
            project_directory.mkdir()
            project_path = project_directory / "Snippet.csproj"
            project_path.write_text(
                _project_for(manifest, configuration), encoding="utf-8"
            )
            (project_directory / "Snippet.cs").write_text(
                _source_for(snippet, configuration), encoding="utf-8"
            )

            for source in configuration.get("auxiliary_sources", []):
                (project_directory / source["file"]).write_text(
                    source["content"], encoding="utf-8"
                )

            try:
                process = runner(
                    [
                        dotnet,
                        "build",
                        str(project_path),
                        "--configuration",
                        "Release",
                        "--nologo",
                        "--verbosity",
                        "minimal",
                    ],
                    cwd=temporary_root,
                    env=environment,
                    text=True,
                    capture_output=True,
                    timeout=120,
                    check=False,
                )
                diagnostics = "\n".join(
                    part.strip()
                    for part in (process.stdout, process.stderr)
                    if part and part.strip()
                )
                execution_result = None
                succeeded = process.returncode == 0
                if succeeded and "execution" in configuration:
                    execution_result = _validate_execution(
                        snippet,
                        configuration,
                        project_directory,
                        manifest,
                        dotnet,
                        runner,
                    )
                    succeeded = execution_result.succeeded
                    if not succeeded:
                        diagnostics = execution_result.diagnostics
                results.append(
                    CompilationResult(
                        snippet=snippet,
                        succeeded=succeeded,
                        diagnostics=diagnostics,
                        execution=execution_result,
                    )
                )
            except (FileNotFoundError, subprocess.TimeoutExpired) as error:
                results.append(
                    CompilationResult(
                        snippet=snippet,
                        succeeded=False,
                        diagnostics=str(error),
                    )
                )

    return results


def _print_identifiers(title: str, snippets: Sequence[Snippet]) -> None:
    identifiers = ", ".join(snippet.identifier for snippet in snippets) or "nenhum"
    print(f"{title} ({len(snippets)}): {identifiers}")


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parent.parent,
        help="raiz do repositório",
    )
    parser.add_argument(
        "--manifest",
        type=Path,
        help="manifesto; por padrão, scripts/snippets-csharp.json",
    )
    parser.add_argument("--dotnet", default="dotnet", help="comando dotnet")
    parser.add_argument(
        "--list-only",
        action="store_true",
        help="lista cobertura sem compilar",
    )
    arguments = parser.parse_args(argv)
    root = arguments.root.resolve()
    manifest_path = arguments.manifest or root / "scripts" / "snippets-csharp.json"

    try:
        discovered = extract_snippets(root)
        manifest = load_manifest(manifest_path, root / "global.json")
        configured, pending, excluded = select_snippets(discovered, manifest)
    except ValidationError as error:
        print(str(error), file=sys.stderr)
        return 2

    print(f"SDK solicitado: {json.loads((root / 'global.json').read_text())['sdk']['version']}")
    print(
        f"Configuração: {manifest['target_framework']}, "
        f"C# {manifest['language_version']}, nullable e warnings como erros"
    )
    _print_identifiers("Selecionados", [item[0] for item in configured])
    _print_identifiers(
        "Execução determinística habilitada",
        [item[0] for item in configured if "execution" in item[1]],
    )
    _print_identifiers("Ainda não validados", pending)
    _print_identifiers("Excluídos por classificação", excluded)

    if arguments.list_only:
        return 0

    try:
        results = compile_snippets(
            root,
            configured,
            manifest,
            dotnet=arguments.dotnet,
        )
    except ValidationError as error:
        print(str(error), file=sys.stderr)
        return 2
    failures = [result for result in results if not result.succeeded]
    executed = [result for result in results if result.execution is not None]

    for result in results:
        origin = f"{result.snippet.relative_path}:{result.snippet.code_line}"
        if result.succeeded:
            validated = (
                "compilação e comportamento"
                if result.execution is not None
                else "compilação"
            )
            print(f"[OK] {result.snippet.identifier}: {validated} ({origin})")
        else:
            print(f"[FALHA] {result.snippet.identifier} ({origin})", file=sys.stderr)
            if result.diagnostics:
                print(result.diagnostics, file=sys.stderr)

    print(
        f"Validados: {len(results) - len(failures)}; "
        f"executados: {len(executed)}; falhas: {len(failures)}"
    )
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())

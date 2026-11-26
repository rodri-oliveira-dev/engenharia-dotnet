#!/usr/bin/env python3
"""Executa CSpell apenas sobre prosa Markdown adicionada/modificada em diff Git.

Mantém os números de linha originais para localizar as ocorrências.
Ignora blocos de código, código inline, URLs e metadados HTML.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
from pathlib import Path

HUNK = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@")
FENCE = re.compile(r"^\s{0,3}("+r"`{3,}|~{3,})")
INLINE_CODE = re.compile(r"("+r"`+)(.*?)\1")
LINK = re.compile(r"(?<!!)\[([^\]]+)\]\([^\n)]*\)")
REF_LINK = re.compile(r"\[([^\]]+)\]\[[^\]]*\]")
URL = re.compile(r"(?:https?://|mailto:)\S+", re.IGNORECASE)
AUTO_LINK = re.compile(r"<(?:(?:https?://|mailto:)[^>]+)>", re.IGNORECASE)
HTML_TAG = re.compile(r"</?[A-Za-z][^>]*>")


def changed_markdown_ranges(diff: str) -> dict[str, set[int]]:
    """Lê cabeçalhos de diff -U0, usando o número de linha do arquivo novo."""
    current: str | None = None
    ranges: dict[str, set[int]] = {}
    for line in diff.splitlines():
        if line.startswith("+++ "):
            name = line[4:]
            current = name[2:] if name.startswith("b/") and name.endswith(".md") else None
            if current:
                ranges.setdefault(current, set())
        match = HUNK.match(line)
        if match and current:
            first = int(match.group(1))
            count = int(match.group(2)) if match.group(2) is not None else 1
            ranges[current].update(range(first, first + count))
    return {path: positions for path, positions in ranges.items() if positions}


def prose_lines(markdown: str, changed_lines: set[int]) -> tuple[str, int]:
    """Mascara linhas antigas e partes técnicas sem perder posição original."""
    fence_mark: str | None = None
    in_comment = False
    result: list[str] = []
    checked = 0
    for number, original in enumerate(markdown.splitlines(), 1):
        fence = FENCE.match(original)
        if fence:
            marker = fence.group(1)
            if fence_mark is None:
                fence_mark = marker
            elif marker[0] == fence_mark[0] and len(marker) >= len(fence_mark):
                fence_mark = None
            result.append("")
            continue
        if fence_mark is not None:
            result.append("")
            continue

        # Comentários HTML podem começar antes das linhas alteradas.
        cleaned: list[str] = []
        cursor = 0
        while cursor < len(original):
            if in_comment:
                end = original.find("-->", cursor)
                if end < 0:
                    break
                cursor = end + 3
                in_comment = False
            else:
                start = original.find("<!--", cursor)
                if start < 0:
                    cleaned.append(original[cursor:])
                    break
                cleaned.append(original[cursor:start])
                cursor = start + 4
                in_comment = True
        line = "".join(cleaned)
        line = INLINE_CODE.sub(" ", line)
        line = AUTO_LINK.sub(" ", line)
        line = LINK.sub(
            lambda match: " " if match.group(1).lower().endswith(
                (".md", ".cs", ".yml", ".yaml", ".json", ".py", ".csproj", ".slnx")
            ) else match.group(1),
            line,
        )
        line = REF_LINK.sub(r"\1", line)
        line = URL.sub(" ", line)
        line = HTML_TAG.sub(" ", line)
        line = re.sub(r"(?<!\w)(?:[\w.-]+/)+[\w.-]+", " ", line)
        if number not in changed_lines:
            result.append("")
            continue
        result.append(line)
        if re.search(r"[^\W\d_]", line):
            checked += 1
    return "\n".join(result) + "\n", checked


def cspell_binary(root: Path, platform: str = sys.platform) -> Path:
    """Seleciona o shim executável do npm para a plataforma atual."""
    name = "cspell.cmd" if platform == "win32" else "cspell"
    return root / "node_modules" / ".bin" / name


def run(base: str, head: str, root: Path) -> int:
    result = subprocess.run(
        ["git", "-c", "core.quotePath=false", "diff", "--no-ext-diff",
         "--no-color", "--unified=0", "--diff-filter=ACMRT",
         f"{base}...{head}", "--", "*.md"],
        cwd=root, text=True, capture_output=True, check=False,
    )
    if result.returncode:
        print(f"Falha ao obter diff: {result.stderr}", file=sys.stderr)
        return 2

    ranges = changed_markdown_ranges(result.stdout)
    binary = cspell_binary(root)
    if not binary.is_file():
        print("CSpell não instalado: execute npm install.", file=sys.stderr)
        return 2

    with tempfile.TemporaryDirectory(prefix=".cspell-mudancas-", dir=root) as folder:
        files: list[str] = []
        lines_checked = 0
        for path, positions in sorted(ranges.items()):
            source = root / path
            if not source.is_file() or source.is_symlink():
                continue
            if not source.resolve().is_relative_to(root.resolve()):
                continue
            content, count = prose_lines(source.read_text(encoding="utf-8"), positions)
            if not count:
                continue
            dest = Path(folder) / path
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_text(content, encoding="utf-8")
            files.append(str(dest))
            lines_checked += count

        print(f"Ortografia pt-BR: {len(files)} arquivo(s), {lines_checked} linhas de prosa nova.", flush=True)
        if not files:
            return 0
        return subprocess.run(
            [str(binary), "lint", "--config", str(root / "cspell.json"),
             "--no-progress", "--no-summary", *files],
            cwd=root, check=False,
        ).returncode


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, help="Commit base do diff")
    parser.add_argument("--head", default="HEAD", help="Commit com mudanças")
    args = parser.parse_args()
    return run(args.base, args.head, Path(__file__).resolve().parents[1])


if __name__ == "__main__":
    raise SystemExit(main())

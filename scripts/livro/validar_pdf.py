"""Verifica conteúdo editorial e marcação estrutural do PDF gerado."""

import os
import re
import subprocess
import sys
import unicodedata
from pathlib import Path


def normalize(text):
    text = unicodedata.normalize("NFKC", text)
    text = text.replace("\u00ad", "").replace("\u200b", "")
    return " ".join(text.split())


def missing_text(text, expected):
    content = normalize(text)
    return [item for item in expected if normalize(item) not in content]


def extract(pdf, *args):
    return subprocess.run(
        ["pdftotext", *args, str(pdf), "-"],
        check=True, capture_output=True, text=True, encoding="utf-8",
    ).stdout


def validate(pdf):
    checks = (
        ("Capa (página 1)", extract(pdf, "-f", "1", "-l", "1"), (
            "Engenharia .NET",
            "Distribuição gratuita",
            "Rodrigo de Oliveira",
            "https://github.com/rodri-oliveira-dev/engenharia-dotnet",
        )),
        ("Direitos autorais (página 2)", extract(pdf, "-f", "2", "-l", "2"), (
            "Conhecimento livre, acesso gratuito",
            "distribuição gratuita",
            "CC BY-NC 4.0",
        )),
        ("Dedicatória (página 3)", extract(pdf, "-f", "3", "-l", "3"), (
            "À minha esposa, Rafaela, e aos nossos gatos",
            "mais leve, afetuosa e especial.",
        )),
        ("Epígrafe (página 4)", extract(pdf, "-f", "4", "-l", "4"), (
            "Todos os seres humanos desejam, por natureza, conhecer.",
            "Aristóteles", "980a21",
        )),
        ("Sumário (página 5)", extract(pdf, "-f", "5", "-l", "5"), ("Sumário",)),
        ("Conteúdo integral", extract(pdf), (
            "MIT License",
            "Como utilizar este livro",
            "Histórico de edições",
            "Sobre o autor",
            "Conhecimento compartilhado",
            "Arquiteto de Software Sênior",
        )),
    )
    errors = []
    for label, text, expected in checks:
        for item in missing_text(text, expected):
            errors.append(f"{label}: texto ausente: {item!r}")
    info = subprocess.run(
        ["pdfinfo", str(pdf)], check=True, capture_output=True,
        text=True, encoding="utf-8", env={**os.environ, "LC_ALL": "C"},
    ).stdout
    if not re.search(r"^Tagged:[ \t]+yes\s*$", info, re.MULTILINE):
        errors.append("PDF sem marcação estrutural.")
    return errors


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Uso: validar_pdf.py arquivo.pdf")
    problems = validate(Path(sys.argv[1]))
    for problem in problems:
        print(problem, file=sys.stderr)
    if problems:
        sys.exit(1)
    print("Verificações editoriais e marcação estrutural aprovadas.")

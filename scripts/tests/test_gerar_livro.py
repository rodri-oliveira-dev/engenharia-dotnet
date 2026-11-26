import importlib.util
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "gerar-livro.py"
SPEC = importlib.util.spec_from_file_location("gerar_livro", SCRIPT)
book = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(book)


class LivroTests(unittest.TestCase):
    def test_ordem_e_cobertura_reais(self):
        root = SCRIPT.parents[1]
        _, areas, appendices = book.select(root)
        self.assertGreaterEqual(len(areas), 8)
        chapters = [p for _, group in areas for p in group]
        self.assertGreaterEqual(len(chapters), 35)
        self.assertTrue(appendices)
        self.assertEqual(len(chapters), len(set(chapters)))
        self.assertEqual(chapters[0].name, "c01-tipos-identidade-modelagem.md")
        self.assertLess(chapters.index(next(p for p in chapters if p.name.startswith("c05-"))),
                        chapters.index(next(p for p in chapters if p.name.startswith("c04-"))))

    def test_pagina_de_direitos_antes_do_sumario(self):
        source = SCRIPT.parents[1] / "scripts/livro/direitos-autorais.md"
        page = '<header>Capa</header><nav id="TOC">Sumário</nav>'
        result = book.insert_rights(page, source)
        self.assertLess(result.index("</header>"), result.index('id="direitos-autorais"'))
        self.assertLess(result.index('id="direitos-autorais"'), result.index('id="TOC"'))
        self.assertIn("CC BY-NC 4.0", result)
        self.assertIn("MIT", result)
        with self.assertRaises(ValueError):
            book.insert_rights("<html></html>", source)

    def test_metadados_e_avisos_editoriais(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "a.md"
            path.write_text("# Título\n\n> **Nível:** Fundamental\n\n"
                            "> Atenção: verifique o comportamento.\n\n"
                            "> Uma citação comum.\n", encoding="utf-8")
            blocks = book.editorial_blocks(book.parse(path))
            self.assertEqual(blocks[1]["c"][0][1], ["chapter-meta"])
            self.assertEqual(blocks[2]["c"][0][1], ["editorial-note"])
            self.assertEqual(blocks[3]["t"], "BlockQuote")

    def test_validacao_textual_tolera_quebras_sem_ignorar_ausencias(self):
        path = SCRIPT.parent / "livro/validar_pdf.py"
        spec = importlib.util.spec_from_file_location("validar_pdf", path)
        validator = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(validator)
        text = "Conhecimento livre, acesso\ngratuito\nCC BY-NC\u00a04.0"
        self.assertEqual(validator.missing_text(text, (
            "Conhecimento livre, acesso gratuito", "CC BY-NC 4.0",
        )), [])
        self.assertEqual(validator.missing_text(text, (
            "CC BY-NC 4.0", "distribuição gratuita",
        )), ["distribuição gratuita"])

    def test_selecao_com_caminho_relativo_e_codificado(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp).resolve()
            area = root / "docs/area"
            area.mkdir(parents=True)
            (root / "docs/indice.md").write_text(
                "# Índice\n\n## Áreas de conhecimento\n\n"
                "- [Área](area/indice.md)\n\n"
                "## Material complementar\n\n- [Guia](guia.md)\n",
                encoding="utf-8")
            (root / "docs/guia.md").write_text("# Guia\n", encoding="utf-8")
            chapter = area / "c01-titulo.md"
            chapter.write_text("# Capítulo\n", encoding="utf-8")
            for target in ("c01-titulo.md", "./c01-titulo.md", "./%6301-titulo.md"):
                with self.subTest(target=target):
                    (area / "indice.md").write_text(
                        f"# Área\n\n- [Capítulo]({target})\n", encoding="utf-8")
                    _, areas, _ = book.select(root)
                    self.assertEqual(areas[0][1], [chapter])

    def test_erro_na_escrita_preserva_pdf_anterior(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = Path(tmp) / "livro.pdf"
            output.write_bytes(b"anterior")
            errors = []

            class Document:
                def write_pdf(self, path, **kwargs):
                    path.write_bytes(b"novo")
                    errors.append("recurso ausente")

            with self.assertRaisesRegex(ValueError, "recurso ausente"):
                book.write_checked_pdf(Document(), output, errors)
            self.assertEqual(output.read_bytes(), b"anterior")
            self.assertFalse(output.with_suffix(".tmp.pdf").exists())

    def test_apresentacao_do_autor(self):
        source = SCRIPT.parent / "livro/sobre-o-autor.md"
        document = book.parse(source)
        self.assertEqual(document["blocks"][0]["t"], "Header")
        self.assertEqual(document["blocks"][0]["c"][0], 1)
        self.assertEqual(sum(h["c"][0] == 1 for h in book.nodes(
            document["blocks"], "Header")), 1)
        self.assertIn("https://rodri-oliveira-dev.github.io/", list(
            book.links(document["blocks"])))

    def test_links_e_ancoras(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp).resolve()
            a, b, c = [root / f"{n}.md" for n in "abc"]
            for path in (a, b, c):
                path.write_text("# Título\n\n## Seção\n", encoding="utf-8")
            maps = {p: book.anchor_map(p, book.parse(p)) for p in (a, b)}
            self.assertEqual(book.resolve_link(root, a, "b.md#se%C3%A7%C3%A3o", maps, "sha"),
                             "#doc-b--seção")
            self.assertEqual(book.resolve_link(root, a, "#seção", maps, "sha"),
                             "#doc-a--seção")
            self.assertEqual(book.resolve_link(root, a, "b.md", maps, "sha"), "#doc-b")
            self.assertEqual(book.resolve_link(root, a, "c.md", maps, "sha"),
                             f"{book.REPO}/blob/sha/c.md")
            for target in ("b.md#ausente", "inexistente.md", "../", "file:///etc/passwd"):
                with self.subTest(target=target), self.assertRaises(ValueError):
                    book.resolve_link(root, a, target, maps, "sha")

    def test_preserva_codigo_e_remove_comentarios(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp).resolve()
            path = root / "a.md"
            path.write_text("# Título\n\n<!-- interno -->\n\n" +
                            chr(96) * 3 + "csharp\nConsole.WriteLine(1);\n" + chr(96) * 3,
                            encoding="utf-8")
            doc = book.parse(path)
            maps = {path: book.anchor_map(path, doc)}
            blocks = book.convert(doc["blocks"], root, path, maps, "sha", root, [])
            self.assertEqual(list(book.nodes(blocks, "Header"))[0]["c"][0], 2)
            self.assertFalse(list(book.nodes(blocks, "RawBlock")))
            self.assertEqual(list(book.nodes(blocks, "CodeBlock"))[0]["c"][1],
                             "Console.WriteLine(1);")


if __name__ == "__main__":
    unittest.main()

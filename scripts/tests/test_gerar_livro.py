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

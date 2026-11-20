"""Testes do seletor de prosa nova, sem dependência de Node.js."""

import importlib.util
import unittest
from pathlib import Path

MODULE = Path(__file__).resolve().parents[1] / "validar-portugues-pr.py"
spec = importlib.util.spec_from_file_location("validar_portugues_pr", MODULE)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class SelecionarProsaTests(unittest.TestCase):
    def test_hunks_de_adicao_e_modificacao(self):
        diff = """--- a/docs/a.md
+++ b/docs/a.md
@@ -1,0 +2,2 @@
+primeira
+segunda
@@ -8 +10 @@
-antigo
+novo
"""
        self.assertEqual(module.changed_markdown_ranges(diff), {"docs/a.md": {2, 3, 10}})

    def test_remocao_nao_gera_linhas_novas(self):
        diff = """--- a/a.md
+++ b/a.md
@@ -3,2 +2,0 @@
-a
-b
"""
        self.assertEqual(module.changed_markdown_ranges(diff), {})

    def test_ignora_codigo_fora_de_markdown(self):
        self.assertEqual(module.changed_markdown_ranges("+++ b/code.cs\n@@ -0,0 +1,2 @@\n"), {})

    def test_linhas_nao_modificadas_ficam_em_branco(self):
        text, count = module.prose_lines("Antes\nNo meio\nDepois\n", {3})
        self.assertEqual(text.splitlines(), ["", "", "Depois"])
        self.assertEqual(count, 1)

    def test_codigo_fenced_e_ignorado(self):
        content = "# Título\n```csharp\nvar errro = 2;\n```\nTexto correto.\n"
        text, count = module.prose_lines(content.replace("\\`", "`"), {3, 5})
        self.assertNotIn("errro", text)
        self.assertIn("Texto correto", text)
        self.assertEqual(count, 1)

    def test_codigo_inline_e_ignorado(self):
        text, count = module.prose_lines("Corrigir o `HttpClinent` do serviço.\n".replace("\\`", "`"), {1})
        self.assertIn("Corrigir", text)
        self.assertNotIn("HttpClinent", text)
        self.assertEqual(count, 1)

    def test_link_preserva_rotulo_descarta_url(self):
        text, _ = module.prose_lines("Veja [arquitettura](https://site.dev/rota) agora.\n", {1})
        self.assertIn("arquitettura", text)
        self.assertNotIn("site.dev", text)

    def test_comentario_html_nao_conta_como_texto(self):
        text, count = module.prose_lines("<!-- snippet-csharp: id=x; categoria=fragmento -->\n", {1})
        self.assertEqual(count, 0)
        self.assertNotIn("fragmento", text)

    def test_comentario_aberto_antes_de_modificacao(self):
        doc = "<!-- comentário\nparte invisível\n-->\nTexto correto.\n"
        text, count = module.prose_lines(doc, {2, 3, 4})
        self.assertNotIn("invisível", text)
        self.assertIn("Texto correto.", text)
        self.assertEqual(count, 1)


if __name__ == "__main__":
    unittest.main()

"""Gera a edição PDF a partir dos índices editoriais, sem alterar os Markdown."""
import argparse
import html
import json
import logging
import os
from pathlib import Path
import re
import subprocess
import tempfile
from urllib.parse import quote, unquote, urlsplit

REPO = "https://github.com/rodri-oliveira-dev/engenharia-dotnet"
CHAPTER = re.compile(r"c\d{2,}-.*\.md$")
APPENDICES = {
    "material-complementar",
    "compatibilidade-e-evolução-de-versões",
    "trilha-de-estudo-e-leitura",
}
IMAGE_TYPES = {".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif"}


def run(*args, input=None):
    return subprocess.run(args, input=input, text=True, encoding="utf-8",
                          check=True, stdout=subprocess.PIPE).stdout


def parse(path):
    return json.loads(run("pandoc", "--from=gfm", "--to=json", str(path)))


def nodes(value, kind):
    if isinstance(value, dict):
        if value.get("t") == kind:
            yield value
        for item in value.values():
            yield from nodes(item, kind)
    elif isinstance(value, list):
        for item in value:
            yield from nodes(item, kind)


def local_path(root, source, target):
    path = (source.parent / unquote(target)).resolve() if target else source
    if not path.is_relative_to(root) or not path.exists():
        raise ValueError(f"Referência local inválida: {source}: {target}")
    return path


def links(blocks):
    return (node["c"][-1][0] for node in nodes(blocks, "Link"))


def select(root):
    central = root / "docs/indice.md"
    doc = parse(central)
    areas, appendices = [], []
    section = ""
    for block in doc["blocks"]:
        if block["t"] == "Header":
            section = block["c"][1][0]
        for target in links(block):
            url = urlsplit(target)
            if url.scheme or url.netloc or not url.path:
                continue
            if section == "áreas-de-conhecimento":
                index = local_path(root, central, url.path)
                area = parse(index)
                titles = list(nodes(area["blocks"], "Header"))
                chapters = []
                for dest in links(area["blocks"]):
                    ref = urlsplit(dest)
                    if not ref.scheme and not ref.netloc and CHAPTER.fullmatch(ref.path):
                        chapter = local_path(root, index, ref.path)
                        if chapter not in chapters:
                            chapters.append(chapter)
                if not chapters:
                    raise ValueError(f"Área sem capítulos: {index}")
                areas.append((titles[0]["c"][2], chapters))
            elif section in APPENDICES and url.path.endswith(".md"):
                appendix = local_path(root, central, url.path)
                if appendix.name != "indice.md" and appendix not in appendices:
                    appendices.append(appendix)
    ordered = [chapter for _, group in areas for chapter in group]
    expected = {p.resolve() for p in (root / "docs").rglob("*.md")
                if CHAPTER.fullmatch(p.name)}
    if not ordered or len(ordered) != len(set(ordered)) or set(ordered) != expected:
        missing = sorted(str(p.relative_to(root)) for p in expected - set(ordered))
        raise ValueError(f"Capítulos ausentes, duplicados ou fora dos índices: {missing}")
    if not appendices or set(ordered) & set(appendices):
        raise ValueError("Apêndices ausentes ou duplicados.")
    return doc["pandoc-api-version"], areas, appendices


def anchor_map(path, document):
    headers = list(nodes(document["blocks"], "Header"))
    if not headers or headers[0]["c"][0] != 1:
        raise ValueError(f"Documento sem título principal: {path}")
    if sum(h["c"][0] == 1 for h in headers) != 1:
        raise ValueError(f"Mais de um título principal: {path}")
    prefix = "doc-" + path.stem
    anchors = {"": prefix}
    for header in headers:
        level, attr, _ = header["c"]
        old = attr[0]
        if old in anchors:
            raise ValueError(f"Identificador repetido: {path}#{old}")
        anchors[old] = prefix if level == 1 else f"{prefix}--{old}"
    return anchors


def resolve_link(root, source, target, maps, commit):
    url = urlsplit(target)
    if url.scheme in {"http", "https", "mailto"}:
        return target
    if url.scheme or url.netloc:
        raise ValueError(f"Esquema de link não permitido: {target}")
    path = local_path(root, source, url.path)
    fragment = unquote(url.fragment)
    if path in maps and not url.query:
        if fragment not in maps[path]:
            raise ValueError(f"Âncora inexistente: {source}: {target}")
        return "#" + maps[path][fragment]
    kind = "tree" if path.is_dir() else "blob"
    suffix = ("?" + url.query if url.query else "") + ("#" + url.fragment if url.fragment else "")
    return f"{REPO}/{kind}/{commit}/{quote(path.relative_to(root).as_posix())}{suffix}"


def render_diagram(code, directory, counter):
    name = f"diagram-{counter}"
    source, image = directory / f"{name}.mmd", directory / f"{name}.png"
    source.write_text(code, encoding="utf-8")
    config = directory / "mermaid.json"
    config.write_text(json.dumps({"securityLevel": "strict", "theme": "neutral",
                                  "flowchart": {"htmlLabels": False}}), encoding="utf-8")
    browser = directory / "puppeteer.json"
    settings = {"args": ["--no-sandbox", "--disable-setuid-sandbox"]}
    if os.environ.get("CHROME_BIN"):
        settings["executablePath"] = os.environ["CHROME_BIN"]
    browser.write_text(json.dumps(settings), encoding="utf-8")
    run("mmdc", "-i", str(source), "-o", str(image), "-c", str(config),
        "-p", str(browser), "-b", "white", "-s", "2")
    if not image.is_file() or image.stat().st_size == 0:
        raise ValueError(f"Diagrama sem imagem: {name}")
    return image.name


def convert(value, root, source, maps, commit, directory, assets):
    if isinstance(value, list):
        return [convert(v, root, source, maps, commit, directory, assets) for v in value]
    if not isinstance(value, dict):
        return value
    kind = value.get("t")
    if kind == "Header":
        level, attr, inline = value["c"]
        attr[0] = maps[source][attr[0]]
        if level == 1:
            attr[1].append("chapter")
        value["c"][0] = min(level + 1, 6)
    elif kind == "Link":
        value["c"][-1][0] = resolve_link(root, source, value["c"][-1][0], maps, commit)
    elif kind == "Image":
        dest = urlsplit(value["c"][-1][0])
        if dest.scheme or dest.netloc or dest.query or dest.fragment:
            raise ValueError(f"Imagem externa ou inválida: {source}: {dest.geturl()}")
        path = local_path(root, source, dest.path)
        if not path.is_file() or path.suffix.lower() not in IMAGE_TYPES:
            raise ValueError(f"Formato de imagem não permitido: {path}")
        assets.append(path)
        name = f"image-{len(assets)}{path.suffix.lower()}"
        (directory / name).write_bytes(path.read_bytes())
        value["c"][-1][0] = name
    elif kind == "CodeBlock" and "mermaid" in value["c"][0][1]:
        assets.append(source)
        name = render_diagram(value["c"][1], directory, len(assets))
        return {"t": "Para", "c": [{"t": "Image", "c": [
            ["", ["diagram"], []], [{"t": "Str", "c": "Diagrama Mermaid"}], [name, ""]]}]}
    elif kind in {"RawInline", "RawBlock"}:
        if re.fullmatch(r"\s*<!--[\s\S]*?-->\s*", value["c"][1]):
            return {"t": "Str", "c": ""} if kind == "RawInline" else {"t": "Plain", "c": []}
        raise ValueError(f"HTML bruto não suportado no livro: {source}")
    return {k: convert(v, root, source, maps, commit, directory, assets)
            for k, v in value.items()}


def header(level, ident, inline):
    return {"t": "Header", "c": [level, [ident, ["part"], []], inline]}


def text_inline(text):
    return [{"t": "Str", "c": text}]


def insert_rights(page, source):
    """Insere a página editorial após a capa e antes do sumário."""
    if page.count("</header>") != 1 or '<nav id="TOC"' not in page:
        raise ValueError("Capa ou sumário não encontrado no livro.")
    rights = run("pandoc", "--from=gfm-raw_html", "--to=html5", str(source))
    return page.replace("</header>", '</header><section class="rights" id="direitos-autorais">'
                        + rights + "</section>", 1)


def build(root, output, directory, commit):
    from weasyprint import HTML, default_url_fetcher

    api, areas, appendices = select(root)
    paths = [p for _, group in areas for p in group] + appendices
    documents = {p: parse(p) for p in paths}
    maps = {p: anchor_map(p, d) for p, d in documents.items()}
    if len({m[""] for m in maps.values()}) != len(maps):
        raise ValueError("Nomes de documentos repetidos.")
    blocks, assets = [], []
    for i, (title, group) in enumerate(areas + [(text_inline("Apêndices"), appendices)], 1):
        blocks.append(header(1, f"parte-{i}", title))
        for path in group:
            print(f"Preparando {path.relative_to(root)}", flush=True)
            blocks.extend(convert(documents[path]["blocks"], root, path, maps,
                                  commit, directory, assets))
    credits = parse(root / "CONTRIBUTORS.md")["blocks"]
    for item in credits:
        if item["t"] == "Header" and item["c"][0] == 1:
            item["c"][1][0] = "creditos-colaboradores"
            item["c"][1][1].append("part")
    blocks.extend(credits)
    blocks.append(header(1, "licenca", text_inline("Licença MIT dos exemplos de código")))
    for paragraph in (root / "LICENSE-CODE").read_text(encoding="utf-8").strip().split("\n\n"):
        blocks.append({"t": "Para", "c": text_inline(" ".join(paragraph.splitlines()))})
    metadata = {key: {"t": "MetaString", "c": value} for key, value in {
        "title": "Engenharia .NET",
        "subtitle": "Da linguagem à arquitetura de software",
        "author": "Rodrigo de Oliveira",
        "date": run("git", "-C", str(root), "show", "-s", "--format=%cs", commit).strip(),
        "lang": "pt-BR",
    }.items()}
    payload = {"pandoc-api-version": api, "meta": metadata, "blocks": blocks}
    page = run("pandoc", "--from=json", "--to=html5", "--standalone", "--toc",
               "--toc-depth=2", "--highlight-style=tango", input=json.dumps(payload))
    css = (root / "scripts/livro/estilo.css").read_text(encoding="utf-8")
    page = page.replace("</head>", "<style>" + css + "</style></head>", 1)
    page = page.replace("</header>", f'<p class="edition">Commit: {html.escape(commit[:12])}</p>'
                        f'<p><a href="{REPO}/tree/{commit}">Código e documentação</a></p>'
                        '<p>Conteúdo editorial: CC BY-NC 4.0 | Código: MIT</p></header>', 1)
    page = insert_rights(page, root / "scripts/livro/direitos-autorais.md")
    page = page.replace('<nav id="TOC"', '<h1 class="toc-title">Sumário</h1><nav id="TOC"', 1)

    def fetch(url):
        parsed = urlsplit(url)
        path = Path(unquote(parsed.path)).resolve()
        if parsed.scheme != "file" or parsed.netloc or not path.is_relative_to(directory):
            raise ValueError(f"Recurso externo bloqueado: {url}")
        return default_url_fetcher(url)

    errors = []
    class Capture(logging.Handler):
        def emit(self, record):
            errors.append(record.getMessage())

    logger = logging.getLogger("weasyprint")
    handler = Capture(logging.ERROR)
    logger.addHandler(handler)
    try:
        document = HTML(string=page, base_url=directory.as_uri() + "/",
                        url_fetcher=fetch).render()
        anchors = {a for p in document.pages for a in p.anchors}
        internal = {link[1] for p in document.pages for link in p.links
                    if link[0] == "internal"}
        required = {a for m in maps.values() for a in m.values()} | {"direitos-autorais", "creditos-colaboradores"}
        if len(document.pages) < 3 or "direitos-autorais" not in document.pages[1].anchors:
            raise ValueError("Página de direitos autorais fora da posição editorial.")
        if not required <= anchors or not internal <= anchors or len(document.pages) < len(paths):
            raise ValueError("Âncoras, sumário ou paginação incompletos.")
        if errors:
            raise ValueError("Falhas de renderização: " + "; ".join(errors))
        output.parent.mkdir(parents=True, exist_ok=True)
        temporary = output.with_suffix(".tmp.pdf")
        try:
            document.write_pdf(temporary)
            temporary.replace(output)
        finally:
            temporary.unlink(missing_ok=True)
    finally:
        logger.removeHandler(handler)
    print(f"{len(paths)} documentos, {len(document.pages)} páginas: {output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path("dist/livro/engenharia-dotnet.pdf"))
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    commit = run("git", "-C", str(root), "rev-parse", "HEAD").strip()
    with tempfile.TemporaryDirectory(prefix="engenharia-livro-") as temporary:
        build(root, args.output.resolve(), Path(temporary).resolve(), commit)


if __name__ == "__main__":
    main()

"""Gera as edições PDF e EPUB a partir dos índices editoriais."""
import argparse
import copy
import html
import json
import logging
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import tempfile
from urllib.parse import quote, unquote, urlsplit
import zipfile
import xml.etree.ElementTree as ET

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
                    if (not ref.scheme and not ref.netloc
                            and CHAPTER.fullmatch(Path(unquote(ref.path)).name)):
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
    settings = {}
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
        try:
            name = render_diagram(value["c"][1], directory, len(assets))
        except subprocess.CalledProcessError as error:
            raise ValueError(f"Falha no diagrama {len(assets)} de {source.relative_to(root)}") from error
        return {"t": "Para", "c": [{"t": "Image", "c": [
            ["", ["diagram"], []], [{"t": "Str", "c": "Diagrama Mermaid"}], [name, ""]]}]}
    elif kind in {"RawInline", "RawBlock"}:
        if re.fullmatch(r"\s*<!--[\s\S]*?-->\s*", value["c"][1]):
            return {"t": "Str", "c": ""} if kind == "RawInline" else {"t": "Plain", "c": []}
        raise ValueError(f"HTML bruto não suportado no livro: {source}")
    return {k: convert(v, root, source, maps, commit, directory, assets)
            for k, v in value.items()}


def editorial_blocks(document):
    """Identifica metadados iniciais e avisos sem modificar o conteúdo."""
    blocks = document["blocks"]
    for index, block in enumerate(blocks):
        if block["t"] != "BlockQuote":
            continue
        words = " ".join(item["c"] for item in nodes(block, "Str"))
        if index == 1 and ("Nível:" in words or "Objetivo:" in words):
            css_class = "chapter-meta"
        elif re.match(r"^(Atenção|Importante|Nota|Recomendação|Cuidado|Dica)\b", words, re.I):
            css_class = "editorial-note"
        else:
            continue
        blocks[index] = {"t": "Div", "c": [["", [css_class], []], [block]]}
    return blocks


def part_intro_blocks(document):
    """Seleciona a introdução do índice de área, antes da primeira seção."""
    blocks = document["blocks"]
    if not blocks or blocks[0]["t"] != "Header" or blocks[0]["c"][0] != 1:
        raise ValueError("Índice de área sem título principal.")
    intro = []
    for block in blocks[1:]:
        if block["t"] == "Header":
            break
        intro.append(block)
    if not intro:
        raise ValueError("Índice de área sem introdução editorial.")
    return intro


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


FRONT_MATTER = (
    ("direitos-autorais", "rights"),
    ("dedicatoria", "dedication"),
    ("epigrafe", "epigraph"),
)


def front_blocks(root, ident, css_class):
    blocks = parse(root / f"scripts/livro/{ident}.md")["blocks"]
    if not blocks or blocks[0]["t"] != "Header" or blocks[0]["c"][0] != 1:
        raise ValueError(f"Seção editorial sem título: {ident}")
    blocks[0]["c"][1][0] = ident
    blocks[0]["c"][1][1].append(css_class)
    return blocks


def insert_front_matter(page, root):
    page = insert_rights(page, root / "scripts/livro/direitos-autorais.md")
    sections = []
    for ident, css_class in FRONT_MATTER[1:]:
        source = root / f"scripts/livro/{ident}.md"
        content = run("pandoc", "--from=gfm-raw_html", "--to=html5", str(source))
        # O identificador do título é removido para não duplicar a âncora.
        content = re.sub(r'<h1 id="[^"]+">', "<h1>", content, count=1)
        sections.append(f'<section class="{css_class} front-matter" id="{ident}">'
                        + content + "</section>")
    return page.replace('<nav id="TOC"', "".join(sections) + '<nav id="TOC"', 1)


def official_editions(pages):
    """Considera somente releases públicas do livro com os dois arquivos."""
    editions = {}
    for page in pages:
        for release in page:
            tag = release.get("tag_name", "")
            published = release.get("published_at") or ""
            assets = {a.get("name") for a in release.get("assets", [])}
            if (re.fullmatch(r"livro-[0-9a-f]{40}", tag)
                    and not release.get("draft") and not release.get("prerelease")
                    and re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", published)
                    and {"engenharia-dotnet.pdf", "engenharia-dotnet.epub"} <= assets):
                editions[tag] = published
    return sorted(editions.items(), key=lambda item: (item[1], item[0]))


def history_blocks(root, commit, release_history):
    blocks = front_blocks(root, "historico-edicoes", "part")
    blocks.append({"t": "Para", "c": text_inline(f"Revisão desta edição: {commit[:12]}.")})
    if release_history is None:
        notice = "Histórico oficial não consultado nesta geração local."
        blocks.append({"t": "Para", "c": text_inline(notice)})
    elif not release_history:
        blocks.append({"t": "Para", "c": text_inline(
            "Nenhuma edição oficial anterior registrada na data da geração.")})
    else:
        for number, (tag, published) in enumerate(release_history, 1):
            day = published[:10].split("-")
            label = f"{number}ª edição · {day[2]}/{day[1]}/{day[0]} · revisão {tag[6:18]}"
            link = {"t": "Link", "c": [["", [], []], text_inline(label),
                    [f"{REPO}/releases/tag/{tag}", ""]]}
            blocks.append({"t": "Para", "c": [link]})
    return blocks


def write_checked_pdf(document, output, errors):
    """Só promove a saída se a escrita final não registrar erros."""
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(
            dir=output.parent, prefix=output.stem + ".", suffix=".tmp.pdf",
            delete=False) as handle:
        temporary = Path(handle.name)
    try:
        document.write_pdf(temporary, pdf_tags=True)
        if errors:
            raise ValueError("Falhas de renderização: " + "; ".join(errors))
        temporary.replace(output)
    finally:
        temporary.unlink(missing_ok=True)


def prepare(root, directory, commit, release_history=None):
    """Consolida a fonte editorial e renderiza diagramas uma única vez."""

    api, areas, appendices = select(root)
    paths = [p for _, group in areas for p in group] + appendices
    documents = {p: parse(p) for p in paths}
    maps = {p: anchor_map(p, d) for p, d in documents.items()}
    if len({m[""] for m in maps.values()}) != len(maps):
        raise ValueError("Nomes de documentos repetidos.")
    intro = parse(root / "scripts/livro/como-utilizar.md")["blocks"]
    intro[0]["c"][1][0] = "como-utilizar"
    intro[0]["c"][1][1].append("part")
    author = parse(root / "scripts/livro/sobre-o-autor.md")["blocks"]
    if not author or author[0]["t"] != "Header" or author[0]["c"][0] != 1:
        raise ValueError("Página de apresentação sem título principal.")
    author[0]["c"][1][0] = "sobre-o-autor"
    author[0]["c"][1][1].extend(["part", "author-title"])
    blocks = author[:1] + [{"t": "Div", "c": [["", ["author-profile"], []], author[1:]]}] + intro
    assets = []
    for i, (title, group) in enumerate(areas + [(text_inline("Apêndices"), appendices)], 1):
        blocks.append(header(1, f"parte-{i}", title))
        if i <= len(areas):
            index = group[0].parent / "indice.md"
            intro = part_intro_blocks(parse(index))
            converted = convert(intro, root, index, maps, commit, directory, assets)
            blocks.append({"t": "Div", "c": [["", ["part-intro"], []], converted]})
        for path in group:
            print(f"Preparando {path.relative_to(root)}", flush=True)
            blocks.extend(convert(editorial_blocks(documents[path]), root, path, maps,
                                  commit, directory, assets))
    credits = parse(root / "CONTRIBUTORS.md")["blocks"]
    for item in credits:
        if item["t"] == "Header" and item["c"][0] == 1:
            item["c"][1][0] = "creditos-colaboradores"
            item["c"][1][1].append("part")
    blocks.extend(credits)
    blocks.extend(history_blocks(root, commit, release_history))
    blocks.extend(front_blocks(root, "autenticidade", "part"))
    blocks.append(header(1, "licenca", text_inline("Licença MIT dos exemplos de código")))
    for paragraph in (root / "LICENSE-CODE").read_text(encoding="utf-8").strip().split("\n\n"):
        blocks.append({"t": "Para", "c": text_inline(" ".join(paragraph.splitlines()))})
    edition_date = run("git", "-C", str(root), "show", "-s", "--format=%cs", commit).strip()
    metadata = {key: {"t": "MetaString", "c": value} for key, value in {
        "title": "Engenharia .NET",
        "subtitle": "Da linguagem à arquitetura de software",
        "author": "Rodrigo de Oliveira",
        "date": "Edição " + edition_date[:4],
        "lang": "pt-BR",
    }.items()}
    payload = {"pandoc-api-version": api, "meta": metadata, "blocks": blocks}
    return payload, paths, maps, edition_date


def build_pdf(root, output, directory, commit, payload, paths, maps):
    from weasyprint import HTML, default_url_fetcher

    page = run("pandoc", "--from=json", "--to=html5", "--standalone", "--toc",
               "--toc-depth=2", "--highlight-style=tango", input=json.dumps(payload))
    css = (root / "scripts/livro/estilo.css").read_text(encoding="utf-8")
    page = page.replace("</head>", "<style>" + css + "</style></head>", 1)
    cover = root / "scripts/livro/capa-arquitetura.svg"
    (directory / cover.name).write_bytes(cover.read_bytes())
    if '<header id="title-block-header">' not in page or '<p class="author">' not in page:
        raise ValueError("Estrutura da capa não encontrada.")
    page = page.replace('<header id="title-block-header">',
                        '<header id="title-block-header"><p class="cover-label">'
                        'PUBLICAÇÃO TÉCNICA INDEPENDENTE</p>', 1)
    page = page.replace('<p class="author">',
                        '<img class="cover-art" src="capa-arquitetura.svg" alt="Ilustração vetorial de camadas arquiteturais interligadas sobre uma grade técnica.">'
                        '<p class="author">', 1)
    page = page.replace("</header>", '<p class="cover-free">Distribuição gratuita</p>'
                        f'<p class="edition">Revisão: {html.escape(commit[:12])}</p>'
                        f'<p><a href="{REPO}/tree/{commit}">Código e documentação</a></p>'
                        '<p>Conteúdo editorial: CC BY-NC 4.0 | Código: MIT</p></header>', 1)
    page = insert_front_matter(page, root)
    page = page.replace('<nav id="TOC"', '<h1 class="toc-title" id="sumario">Sumário</h1><nav id="TOC"', 1)

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
        required = {a for m in maps.values() for a in m.values()} | {"direitos-autorais", "dedicatoria", "epigrafe", "historico-edicoes", "autenticidade", "creditos-colaboradores", "como-utilizar", "sumario", "sobre-o-autor"}
        for index, ident in enumerate(("direitos-autorais", "dedicatoria", "epigrafe", "sumario"), 1):
            if len(document.pages) <= index or ident not in document.pages[index].anchors:
                raise ValueError(f"Seção editorial fora da posição: {ident}")
        if not required <= anchors or not internal <= anchors or len(document.pages) < len(paths):
            raise ValueError("Âncoras, sumário ou paginação incompletos.")
        author_pages = [i for i, p in enumerate(document.pages) if "sobre-o-autor" in p.anchors]
        intro_pages = [i for i, p in enumerate(document.pages) if "como-utilizar" in p.anchors]
        if len(author_pages) != 1 or len(intro_pages) != 1 or intro_pages[0] != author_pages[0] + 1:
            raise ValueError("A apresentação do autor deve ocupar uma única página antes da introdução.")
        if errors:
            raise ValueError("Falhas de renderização: " + "; ".join(errors))
        write_checked_pdf(document, output, errors)
    finally:
        logger.removeHandler(handler)
    print(f"{len(paths)} documentos, {len(document.pages)} páginas: {output}")


def epub_payload(root, payload, commit, edition_date):
    """Adiciona os direitos ao mesmo conteúdo usado no PDF."""
    result = copy.deepcopy(payload)
    front = []
    for ident, css_class in FRONT_MATTER:
        front.extend(front_blocks(root, ident, css_class))
    result["blocks"] = front + result["blocks"]
    for key, value in {
        "date": edition_date,
        "rights": "Conteúdo editorial: CC BY-NC 4.0; código: MIT.",
        "identifier": f"{REPO}/tree/{commit}",
        "toc-title": "Sumário",
    }.items():
        result["meta"][key] = {"t": "MetaString", "c": value}
    return result


def epub_cover(root, directory, edition_date):
    """Reutiliza a arte da capa em uma composição vertical."""
    artwork = (root / "scripts/livro/capa-arquitetura.svg").read_text(encoding="utf-8")
    art = ET.fromstring(artwork)
    art.set("x", "60")
    art.set("y", "480")
    art.set("width", "1080")
    art.set("height", "590")
    svg = directory / "capa-epub.svg"
    png = directory / "capa-epub.png"
    svg.write_text(
        '<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="1800" '
        'viewBox="0 0 1200 1800">'
        '<rect width="1200" height="1800" fill="#10223d"/>'
        '<g font-family="DejaVu Sans" fill="#f0f6ff">'
        '<path d="M90 130H1110" stroke="#8cadd4" stroke-width="3"/>'
        '<text x="90" y="195" font-size="31" fill="#b7cce5">PUBLICAÇÃO TÉCNICA INDEPENDENTE</text>'
        '<text x="90" y="345" font-size="92" font-weight="bold">Engenharia .NET</text>'
        '<text x="90" y="425" font-size="40" fill="#d7e5f7">Da linguagem à arquitetura de software</text>'
        + ET.tostring(art, encoding="unicode") +
        '<text x="90" y="1250" font-size="57" font-weight="bold">Rodrigo de Oliveira</text>'
        f'<text x="90" y="1335" font-size="36">Edição {html.escape(edition_date[:4])}</text>'
        '<text x="90" y="1455" font-size="40" fill="#d6c8ff">Distribuição gratuita</text>'
        '<text x="90" y="1630" font-size="28" fill="#d7e5f7">Conteúdo editorial: CC BY-NC 4.0 | Código: MIT</text>'
        '</g></svg>', encoding="utf-8")
    run("rsvg-convert", "--output", str(png), str(svg))
    return png


def check_epub_structure(path, expected):
    """Confere âncoras editoriais, capa e navegação do EPUB."""
    with zipfile.ZipFile(path) as archive:
        if archive.read("mimetype") != b"application/epub+zip":
            raise ValueError("Formato EPUB inválido.")
        package = ET.fromstring(archive.read("META-INF/container.xml"))
        rootfile = package.find(".//{*}rootfile")
        if rootfile is None:
            raise ValueError("Pacote EPUB sem manifesto.")
        opf_path = rootfile.attrib["full-path"]
        opf = ET.fromstring(archive.read(opf_path))
        if not opf.get("version", "").startswith("3."):
            raise ValueError("A edição precisa usar EPUB 3.")
        manifest = opf.findall(".//{*}manifest/{*}item")
        if not any("cover-image" in item.get("properties", "").split() for item in manifest):
            raise ValueError("Capa EPUB ausente.")
        nav = [item for item in manifest if "nav" in item.get("properties", "").split()]
        if len(nav) != 1:
            raise ValueError("Documento de navegação EPUB ausente ou duplicado.")
        anchors = set()
        for item in manifest:
            if item.get("media-type") == "application/xhtml+xml":
                name = str(PurePosixPath(opf_path).parent /
                           unquote(item.attrib["href"]))
                page = ET.fromstring(archive.read(name))
                anchors.update(e.get("id") for e in page.iter() if e.get("id"))
        missing = set(expected) - anchors
        if missing:
            raise ValueError(f"Âncoras ausentes no EPUB: {sorted(missing)}")


def reorder_epub_nav(path):
    """Posiciona o sumário visível depois dos três capítulos pré-textuais."""
    rewritten = path.with_suffix(".ordered.epub")
    try:
        with zipfile.ZipFile(path) as source:
            container = ET.fromstring(source.read("META-INF/container.xml"))
            opf_path = container.find(".//{*}rootfile").attrib["full-path"]
            data = source.read(opf_path).decode("utf-8")
            opf = ET.fromstring(data)
            items = opf.findall(".//{*}manifest/{*}item")
            nav = next(i.attrib["id"] for i in items
                       if "nav" in i.get("properties", "").split())
            spine = opf.findall(".//{*}spine/{*}itemref")
            order = [i.attrib["idref"] for i in spine]
            front = {}
            for item in items:
                if item.get("media-type") != "application/xhtml+xml":
                    continue
                name = str(PurePosixPath(opf_path).parent /
                           unquote(item.attrib["href"]))
                page = ET.fromstring(source.read(name))
                for node in page.iter():
                    ident = node.get("id")
                    if ident in {i for i, _ in FRONT_MATTER} | {"sobre-o-autor"}:
                        front[ident] = item.attrib["id"]
            expected = [front[i] for i, _ in FRONT_MATTER]
            content_order = [item for item in order if item != nav]
            if (len(set(expected)) != 3
                    or content_order[1:1 + len(expected)] != expected):
                raise ValueError("Ordem pré-textual EPUB inválida.")
            if order.index(nav) < order.index(expected[-1]):
                # Pandoc emite itemref vazio; mova somente esse elemento, sem
                # reserializar namespaces ou metadados do manifesto.
                pattern = r'<itemref\b(?=[^>]*\bidref="' + re.escape(nav) + r'")[^>]*/>'
                match = re.search(pattern, data)
                if match is None:
                    raise ValueError("Referência de navegação EPUB não encontrada.")
                itemref = match.group()
                data = data[:match.start()] + data[match.end():]
                anchor = re.compile(r'<itemref\b(?=[^>]*\bidref="'
                                    + re.escape(expected[-1]) + r'")[^>]*/>')
                data, count = anchor.subn(lambda m: m.group() + itemref, data)
                if count != 1:
                    raise ValueError("Não foi possível posicionar o sumário EPUB.")
            ordered = [i.attrib["idref"] for i in
                       ET.fromstring(data).findall(".//{*}spine/{*}itemref")]
            sequence = expected + [nav, front["sobre-o-autor"]]
            if ordered[1:1 + len(sequence)] != sequence:
                raise ValueError("Sumário EPUB fora da posição editorial.")
            with zipfile.ZipFile(rewritten, "w") as target:
                for info in source.infolist():
                    target.writestr(info, data.encode("utf-8") if info.filename == opf_path
                                    else source.read(info.filename))
        rewritten.replace(path)
    finally:
        rewritten.unlink(missing_ok=True)


def build_epub(root, output, directory, commit, payload, maps, edition_date):
    edition = epub_payload(root, payload, commit, edition_date)
    for image in nodes(edition, "Image"):
        name = image["c"][-1][0]
        if Path(name).name != name or not (directory / name).is_file():
            raise ValueError(f"Imagem EPUB não preparada: {name}")
    if list(nodes(edition, "RawBlock")) or list(nodes(edition, "RawInline")):
        raise ValueError("Conteúdo bruto não permitido no EPUB.")
    cover = epub_cover(root, directory, edition_date)
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_suffix(".tmp.epub")
    try:
        run("pandoc", "--from=json", "--to=epub3", "--toc", "--toc-depth=2",
            "--split-level=2", "--highlight-style=tango",
            "--resource-path=" + str(directory),
            "--css=" + str(root / "scripts/livro/estilo-epub.css"),
            "--epub-title-page=false",
            "--epub-cover-image=" + str(cover),
            "--output=" + str(temporary), input=json.dumps(edition))
        required = {a for m in maps.values() for a in m.values()} | {
            "direitos-autorais", "dedicatoria", "epigrafe", "historico-edicoes",
            "autenticidade", "creditos-colaboradores", "sobre-o-autor", "como-utilizar"}
        reorder_epub_nav(temporary)
        check_epub_structure(temporary, required)
        print(run("epubcheck", str(temporary)), end="")
        temporary.replace(output)
    finally:
        temporary.unlink(missing_ok=True)
    print(f"EPUB 3 validado: {output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path("dist/livro/engenharia-dotnet.pdf"))
    parser.add_argument("--format", choices=("pdf", "epub", "both"), default="pdf")
    parser.add_argument("--epub-output", type=Path, default=Path("dist/livro/engenharia-dotnet.epub"))
    parser.add_argument("--release-history", type=Path, help="JSON paginado de releases oficiais")
    args = parser.parse_args()
    pdf_output = args.output.resolve()
    epub_output = args.epub_output.resolve()
    if args.format == "both" and pdf_output == epub_output:
        parser.error("--output e --epub-output devem apontar para arquivos diferentes")
    root = Path(__file__).resolve().parents[1]
    commit = run("git", "-C", str(root), "rev-parse", "HEAD").strip()
    with tempfile.TemporaryDirectory(prefix="engenharia-livro-") as temporary:
        directory = Path(temporary).resolve()
        history = (official_editions(json.loads(args.release_history.read_text(encoding="utf-8")))
                   if args.release_history else None)
        payload, paths, maps, edition_date = prepare(root, directory, commit, history)
        if args.format in ("pdf", "both"):
            build_pdf(root, pdf_output, directory, commit, payload, paths, maps)
        if args.format in ("epub", "both"):
            build_epub(root, epub_output, directory, commit, payload, maps, edition_date)


if __name__ == "__main__":
    main()

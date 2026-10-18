"""Manutenção explícita do manifest; não faz downloads nem integra o runtime.

Recebe os três ZIPs oficiais já baixados em tmp/official. Preserva bytes e
seleciona apenas o fechamento das dependências dos doze entrypoints (seis da
NFC-e, três de evento e três de inutilização).
Qualquer mudança de pacote/hash exige revisão deste script e dos testes.
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path
from lxml import etree

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "schemas/nfe/4.00"
SOURCES = [
    dict(id="PL009n", archive="PL_009n_NT2023_004_v101_e_NT2019_001_v162.zip",
         sha256="8e269cd5d5d60532b709c8a88bf590323b94002efd3a49935fb5fd2db5957a62",
         origin="https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=2&nomeArquivo=PL_009n_NT2023_004_v101_e_NT2019_001_v162.zip",
         catalog="https://dfe-portal.svrs.rs.gov.br/Nfe/Documentos",
         publishedOn="2024-03-19", subtree="PL_009n_NT2023_004_v101_e_NT2019_001_v162/"),
    # Mesmo ZIP/hash do PL010d, subárvore Evento/ (leiaute de eventos 1.00, que
    # inclui tiposBasico_v1.03 — nome exclusivo). Precede NFe/ e PL010f para que
    # xmldsig/tiposBasico_v4.00 continuem vindo das fontes já fixadas.
    dict(id="PL010dEvento", archive="PL_010d_v1.03.zip",
         sha256="45ceefe4dfbbfec93958283b650a2f1e1734784f4770d070b9907754de081d9b",
         origin="https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=%2BpBOYTXBtbk%3D",
         catalog="https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=BMPFMBoln3w%3D",
         publishedOn="2026-07-10", subtree="PL_010d_v1.03/Evento/"),
    dict(id="PL010d", archive="PL_010d_v1.03.zip",
         sha256="45ceefe4dfbbfec93958283b650a2f1e1734784f4770d070b9907754de081d9b",
         origin="https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=%2BpBOYTXBtbk%3D",
         catalog="https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=BMPFMBoln3w%3D",
         publishedOn="2026-07-10", subtree="PL_010d_v1.03/NFe/"),
    dict(id="PL010f", archive="PL_010f_v1.04.zip",
         sha256="b8589490a58a09a993a80e6ac4d7ed10f20892061ecfc56719337098d4b95998",
         origin="https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=8ITFuBLltXs%3D",
         catalog="https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=BMPFMBoln3w%3D",
         publishedOn="2026-08-31", subtree="PL_010f_v1.04/"),
]
ENTRYPOINTS = {name: f"{name}_v4.00.xsd" for name in (
    "nfe", "enviNFe", "retEnviNFe", "consSitNFe", "retConsSitNFe", "procNFe",
    # EF-17/EF-18 (18/10/2026): inutilização no leiaute 4.00 do mesmo pacote.
    "inutNFe", "retInutNFe", "procInutNFe")}
# Eventos genéricos (cancelamento 110111) permanecem no leiaute 1.00 dentro dos
# mesmos pacotes oficiais; versão distinta do arquivo não é versão distinta do PL.
ENTRYPOINTS.update({name: f"{name}_v1.00.xsd" for name in ("envEvento", "retEnvEvento", "procEventoNFe")})
selected = {}
history = {}
for source in SOURCES:
    data = (ROOT / "tmp/official" / source["archive"]).read_bytes()
    if hashlib.sha256(data).hexdigest() != source["sha256"]:
        raise ValueError(f"Hash do ZIP divergente: {source['id']}")
    with zipfile.ZipFile(ROOT / "tmp/official" / source["archive"]) as archive:
        for entry in archive.infolist():
            if not entry.filename.startswith(source["subtree"]) or not entry.filename.endswith(".xsd"):
                continue
            name = entry.filename[len(source["subtree"]):]
            if not re.fullmatch(r"[A-Za-z0-9_.-]+\.xsd", name):
                raise ValueError("Subdiretório inesperado na subárvore NFe")
            content = archive.read(entry)
            metadata = dict(name=name, path=f"files/{name}", sha256=hashlib.sha256(content).hexdigest(),
                            packageId=source["id"], archiveEntry=entry.filename)
            history.setdefault(name, []).append(metadata)
            selected[name] = (metadata, content)

files = {}
def visit(name):
    if name in files:
        return
    metadata, content = selected[name]
    tree = etree.fromstring(content, etree.XMLParser(resolve_entities=False, no_network=True, load_dtd=False))
    if tree.getroottree().docinfo.doctype:
        raise ValueError("DTD proibido")
    dependencies = sorted({element.get("schemaLocation") for element in tree
                           if element.tag in ("{http://www.w3.org/2001/XMLSchema}include",
                                              "{http://www.w3.org/2001/XMLSchema}import")})
    if any(not dep or not re.fullmatch(r"[A-Za-z0-9_.-]+\.xsd", dep) for dep in dependencies):
        raise ValueError("Dependência externa ou ausente")
    files[name] = dict(metadata, dependencies=dependencies, supersedes=history[name][:-1])
    for dep in dependencies:
        visit(dep)

for name in ENTRYPOINTS.values():
    visit(name)
TARGET.mkdir(parents=True, exist_ok=True)
(TARGET / "files").mkdir(exist_ok=True)
for name in files:
    (TARGET / "files" / name).write_bytes(selected[name][1])
manifest = dict(manifestVersion=1, baselineId="nfe-4.00-offline-2026-10-04",
                reviewedOn="2026-10-04", precedence=[s["id"] for s in SOURCES],
                packages=SOURCES, entrypoints=ENTRYPOINTS,
                files=[files[name] for name in sorted(files)])
(TARGET / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"{len(files)} XSDs oficiais, bytes preservados; precedência PL009n < PL010d/Evento < PL010d/NFe < PL010f.")

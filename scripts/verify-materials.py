"""Auditoria complementar de material local; NÃO executa nem substitui testes .NET.

Valida somente hashes, dependências e estrutura XSD com libxml2/lxml.
Assinaturas XML de placeholder existem exclusivamente em memória para XSD:
não são provas criptográficas, certificados válidos ou autorização fiscal.
Sem I/O de rede. Os testes obrigatórios estão na solução .NET 10.
"""
import hashlib
import json
from pathlib import Path
from lxml import etree

ROOT = Path(__file__).resolve().parents[1]
SCHEMAS = ROOT / "schemas/nfe/4.00"
manifest = json.loads((SCHEMAS / "manifest.json").read_text(encoding="utf-8"))
contents = {}
for file in manifest["files"]:
    path = SCHEMAS / file["path"]
    assert path.resolve().is_relative_to(SCHEMAS.resolve()), "Caminho externo"
    data = path.read_bytes()
    assert hashlib.sha256(data).hexdigest() == file["sha256"], f"Hash: {file['name']}"
    contents[file["name"]] = data

BASE = "https://schemas.offline.invalid/nfe4/"
class LocalOnly(etree.Resolver):
    def resolve(self, url, public_id, context):
        name = url.removeprefix(BASE)
        if not url.startswith(BASE) or name not in contents:
            raise OSError("Resolução externa ou não manifestada proibida")
        return self.resolve_string(contents[name], context, base_url=BASE + name)

def parse(data, base_url=None):
    parser = etree.XMLParser(load_dtd=False, no_network=True, resolve_entities=False)
    parser.resolvers.add(LocalOnly())
    tree = etree.fromstring(data, parser, base_url=base_url)
    if tree.getroottree().docinfo.doctype:
        raise ValueError("DTD proibido")
    return tree

xs = "{http://www.w3.org/2001/XMLSchema}"
for file in manifest["files"]:
    tree = parse(contents[file["name"]])
    deps = {child.get("schemaLocation") for child in tree if child.tag in (xs + "import", xs + "include")}
    assert deps == set(file["dependencies"]), f"Dependências: {file['name']}"
    assert all(dep in contents for dep in deps), "Dependência ausente"
sets = {name: etree.XMLSchema(parse(contents[file], BASE + file)) for name, file in manifest["entrypoints"].items()}

N = "http://www.portalfiscal.inf.br/nfe"
DS = "http://www.w3.org/2000/09/xmldsig#"
key = "53261012345678000195650010000000011123456782"
invoice = parse((ROOT / "tests/EmissorFiscal.Core.Tests/Fixtures/nfce-unsigned.xml").read_bytes())
supplement = etree.SubElement(invoice, "{" + N + "}infNFeSupl")
etree.SubElement(supplement, "{" + N + "}qrCode").text = f"http://www.fazenda.df.gov.br/nfce/qrcode?p={key}|3|2"
etree.SubElement(supplement, "{" + N + "}urlChave").text = "http://www.fazenda.df.gov.br/nfce/consulta"
digest = "AAAAAAAAAAAAAAAAAAAAAAAAAAA="  # PLACEHOLDER XSD, nunca correlacionar como prova.
signature = f'''<Signature xmlns="{DS}"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315"/><SignatureMethod Algorithm="{DS}rsa-sha1"/><Reference URI="#NFe{key}"><Transforms><Transform Algorithm="{DS}enveloped-signature"/><Transform Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315"/></Transforms><DigestMethod Algorithm="{DS}sha1"/><DigestValue>{digest}</DigestValue></Reference></SignedInfo><SignatureValue>AA==</SignatureValue><KeyInfo><X509Data><X509Certificate>AA==</X509Certificate></X509Data></KeyInfo></Signature>'''
invoice.append(parse(signature.encode()))
invoice_xml = etree.tostring(invoice, encoding="unicode")
protocol = f'''<protNFe versao="4.00"><infProt><tpAmb>2</tpAmb><verAplic>OFFLINE-1</verAplic><chNFe>{key}</chNFe><dhRecbto>2026-10-04T12:00:01-03:00</dhRecbto><nProt>15326000000000001</nProt><digVal>{digest}</digVal><cStat>100</cStat><xMotivo>SINTETICO XSD</xMotivo></infProt></protNFe>'''
prefix = '<tpAmb>2</tpAmb><verAplic>OFFLINE-1</verAplic>'
middle = '<xMotivo>SINTETICO XSD</xMotivo><cUF>53</cUF><dhRecbto>2026-10-04T12:00:01-03:00</dhRecbto>'
documents = {
    "nfe": invoice_xml,
    "enviNFe": f'<enviNFe xmlns="{N}" versao="4.00"><idLote>1</idLote><indSinc>1</indSinc>{invoice_xml}</enviNFe>',
    "consSitNFe": f'<consSitNFe xmlns="{N}" versao="4.00"><tpAmb>2</tpAmb><xServ>CONSULTAR</xServ><chNFe>{key}</chNFe></consSitNFe>',
    "retEnviNFe": f'<retEnviNFe xmlns="{N}" versao="4.00">{prefix}<cStat>104</cStat>{middle}{protocol}</retEnviNFe>',
    "retConsSitNFe": f'<retConsSitNFe xmlns="{N}" versao="4.00">{prefix}<cStat>100</cStat>{middle}<chNFe>{key}</chNFe>{protocol}</retConsSitNFe>',
    "procNFe": f'<nfeProc xmlns="{N}" versao="4.00">{invoice_xml}{protocol}</nfeProc>'
}
for name, xml in documents.items():
    sets[name].assertValid(parse(xml.encode()))

negatives = {
    "nfe": invoice_xml.replace("<qCom>1.0000</qCom>", "<qCom>invalid</qCom>"),
    "enviNFe": documents["enviNFe"].replace("<indSinc>1</indSinc>", "<indSinc>2</indSinc>"),
    "consSitNFe": documents["consSitNFe"].replace("CONSULTAR", "invalid"),
    "retEnviNFe": documents["retEnviNFe"].replace("<tpAmb>2</tpAmb>", "<tpAmb>9</tpAmb>"),
    "retConsSitNFe": documents["retConsSitNFe"].replace("</xMotivo></infProt>", "</xMotivo><cMsg>1</cMsg><xMsg>SINTETICO</xMsg></infProt>"),
    "procNFe": documents["procNFe"].replace(protocol, "")
}
for name, xml in negatives.items():
    assert not sets[name].validate(parse(xml.encode())), f"Negativo aceito: {name}"
alerts = documents["retEnviNFe"].replace("<cStat>100</cStat>", "<cStat>120</cStat>").replace(
    "</xMotivo></infProt>", "</xMotivo><cMsg>1</cMsg><xMsg>SINTETICO</xMsg></infProt>")
sets["retEnviNFe"].assertValid(parse(alerts.encode()))
alpha = "53261012ABC34501DE35650010000000011123456782"
sets["nfe"].assertValid(parse(invoice_xml.replace(key, alpha).replace("12345678000195", "12ABC34501DE35").encode()))
scope_gap = []
for label, before, after in [
    ("modelo55", "<mod>65</mod>", "<mod>55</mod>"),
    ("uf35", "<cUF>53</cUF>", "<cUF>35</cUF>"),
    ("ambiente1", "<tpAmb>2</tpAmb>", "<tpAmb>1</tpAmb>"),
    ("tpEmis9", "<tpEmis>1</tpEmis>", "<tpEmis>9</tpEmis>")
]:
    sets["nfe"].assertValid(parse(invoice_xml.replace(before, after).encode()))
    scope_gap.append(label)
second = invoice_xml.replace(key, alpha).replace("12345678000195", "12ABC34501DE35")
sets["enviNFe"].assertValid(parse(documents["enviNFe"].replace("</enviNFe>", second + "</enviNFe>").encode()))
scope_gap.append("maisDeUmaNfce")
for name in ("xxe.xml", "truncated-response.xml"):
    try:
        parse((ROOT / "tests/EmissorFiscal.Core.Tests/Fixtures" / name).read_bytes())
    except (ValueError, etree.XMLSyntaxError):
        pass
    else:
        raise AssertionError(f"XML perigoso/incompleto aceito: {name}")
try:
    parser = etree.XMLParser(no_network=True, load_dtd=False, resolve_entities=False)
    parser.resolvers.add(LocalOnly())
    etree.XMLSchema(etree.fromstring(contents["nfe_v4.00.xsd"].replace(
        b'leiauteNFe_v4.00.xsd', b'https://invalid.example/external.xsd'), parser, base_url=BASE + "nfe_v4.00.xsd"))
except (OSError, etree.XMLSchemaParseError):
    pass
else:
    raise AssertionError("Resolver aceitou dependência externa")

report = dict(kind="SUPPLEMENTAL_MATERIAL_AUDIT_NOT_DOTNET", reviewedOn="2026-10-04",
              manifestSha256=hashlib.sha256((SCHEMAS / "manifest.json").read_bytes()).hexdigest(),
              xsdFilesVerified=len(contents), compiledWith="lxml/libxml2", compiledEntrypoints=sorted(sets),
              positiveXsdFixtures=6, negativeXsdFixtures=6,
              additionalChecks=["cStat120-alertas-protocolo17", "CNPJ-chave-alfanumericos", "DTD-proibido", "XML-truncado", "resolver-externo-proibido"],
              xsdAcceptsOutsideRuntimeScope=scope_gap, cryptographicProof=False, dotnetTestsExecuted=False,
              signatures="PLACEHOLDERS ONLY, IN MEMORY, XSD STRUCTURE", fiscalIo=False)
output = ROOT / ".validation"
output.mkdir(exist_ok=True)
(output / "materials.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))

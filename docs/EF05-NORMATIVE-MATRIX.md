# EF-05 — matriz normativa e diagnóstico de schemas

Pesquisa de 05/10/2026, sobre main 4a475c4edc96b7b262b5087145b3bdfd99b3dd89. Incorporação documental e regressões da baseline existente; issue #2 aberta e compatibilidade bloqueada. As seções abaixo preservam os resultados históricos do levantamento, não a execução deste incremento.

**Nota de 17/10/2026:** vários prazos anunciados nesta matriz já venceram ou estão próximos (ex.: tabelas da IT 2025.002 até 16/10/2026; produção das NTs 2026.007/2026.008 e correções VC02 em 03/11/2026; RVs da NT 2026.002 prorrogadas para 14/12/2026). Nenhuma reconferência foi feita nesta data; **reconferir todas as datas e vigências nas fontes oficiais antes de qualquer incremento com transmissão** (EF-14), conforme a issue #2. Esta nota não atribui novas vigências nem altera a baseline.

Evidências compactas: [fontes](evidence/ef05/FONTES.md), [índice](evidence/ef05/source-index.json), [inventário](evidence/ef05/zip-inventory.json), [bytes](evidence/ef05/baseline-byte-verification.json), [dependências](evidence/ef05/dependency-closures.json). Scripts, PDFs, XMLs placeholder, TRX e logs citados como históricos permanecem em tmp/ef05-research e não são requisitos para executar a suíte versionada.

## 1. Resultado e recomendação

1. **Não promover uma nova baseline agora.** Nos catálogos Nacional e SVRS consultados e nos nove ZIPs examinados, não foi identificado leiaute oficial de consulta que admita os alertas previstos na NT 2026.002. Isso delimita a pesquisa; não afirma inexistência em toda fonte possível.
2. **Manter a rejeição segura atual.** A consulta com `cMsg/xMsg` deve continuar resultando em `InvalidResponse`, sem autorização e sem construção de `nfeProc`. Não remover o teste da limitação, editar XSD oficial, copiar o tipo de autorização para a consulta ou introduzir fallback.
3. **A norma e o pacote divergem de forma demonstrável.** A NT cita `NFeConsultaProtocolo`, mas `TProtNFe` em `leiauteConsSitNFe_v4.00.xsd` não contém o grupo de mensagens. Os protocolos de 17 posições já são admitidos pela baseline; o bloqueio não é o tamanho do protocolo.
4. **O anexo DF foi localizado integralmente no DODF.** A lacuna de acesso ao documento foi resolvida. Permanecem a normalização/reconciliação integral de suas linhas e a aplicação por operação/regime, dependente de EF-06. A tabela HTML do AD 2/2025 está desatualizada em relação ao DODF consultado.
5. **Incremento entregue:** diagnóstico e matriz incorporados à documentação versionada, com evidências compactas e regressões adicionais da baseline existente. A migração de schemas fica condicionada a um pacote oficial verificável; as decisões do piloto continuam em EF-06.

## 2. Diagnóstico reproduzível

Baseline preservada: `nfe-4.00-offline-2026-10-04`; manifest SHA-256 `9a3d723f602816bc5188ea9b5e01fe324fbc8165f479d06dac8261a6d570e72c`.

| Aspecto | Norma/documento | Bytes oficiais na baseline | Consequência |
| --- | --- | --- | --- |
| Serviços de protocolo | NT 2026.002 v1.11, seção 2.3, pp.5–6: autorização, retorno de autorização e consulta | `retConsSitNFe` inclui `leiauteConsSitNFe`; envio/processado incluem `leiauteNFe` | São dois tipos homônimos `TProtNFe`, com conteúdo diferente. Compilar em conjuntos separados. |
| Mensagens em `infProt` | PR13: sequência 0–5; PR14 `cMsg`: 0–1, 1–4 dígitos; PR15 `xMsg`: 1–1, 1–255 caracteres | `leiauteNFe` linhas 6739–6763: sequência 0–5 de pares; cada `cMsg` e `xMsg` obrigatório dentro do par. `leiauteConsSitNFe` linhas 100–160: nenhuma mensagem | Há também divergência PDF/XSD sobre opcionalidade de `cMsg`. Não ampliar o parser com base apenas na tabela do PDF. |
| Número do protocolo | PR09, p.6: 0–1, 15 ou 17 posições | Ambos referenciam `TProt`; `tiposBasico_v4.00.xsd` linhas 59–67 contém `[0-9]{15}\|[0-9]{17}` e comprimento máximo 17 | 15/17 aceitos nos três documentos; 16/18 rejeitados na prova estrutural. O núcleo exige protocolo completo para autorizar, embora o XSD permita sua ausência. |
| `cStat` | P.4 separa 100, 120 e 150; p.5 determina 120 quando há alerta mesmo fora do prazo | `TStat` é formato, não uma enumeração semântica desses estados | Schema válido não prova coerência fiscal do código com as mensagens. Fixtures 100/150 com mensagens são testes sintéticos da implementação, não exemplos de resposta oficial. |
| Digest, chave e ambiente | Protocolo deve corresponder ao documento | `NfceProtocol` valida schema antes de interpretar; compara chave, ambiente e digest; só depois monta o processado | Erro XSD de consulta impede que um status aparentemente positivo autorize. |
| Nova baseline | Exige pacote/hash/proveniência revisados | `SchemaManifest.cs` fixa identificador, data, precedência, quatro fontes (três ZIPs; PL010d em duas subárvores), doze entrypoints e 20 arquivos | Não basta trocar o manifest ou baixar um ZIP no runtime. |

**Provas atuais, distintas:**

- **Proveniência:** todos os 20 XSDs locais são idênticos aos bytes de suas entradas nos ZIPs oficiais (`baseline-byte-verification.json`, regenerado em 18/10/2026 após novo download dos três ZIPs com os mesmos SHA-256; os 11 originais não mudaram de byte).
- **Compilação .NET histórica:** CLI `schemas` compilou separadamente os seis entrypoints então existentes, usando o loader/resolver (`dotnet-schemas.json`). A ampliação para doze entrypoints está descrita em [VALIDATION](VALIDATION.md#incremento-ef-17ef-18-núcleo--cancelamento-e-inutilização-offline--18102026).
- **Prova estrutural independente:** `probe.py`, lxml/libxml2, parser sem DTD/entidades/rede e resolver limitado aos bytes verificados. Seis conjuntos compilados; 150 avaliações com resultados esperados, além de bloqueios de DTD, truncamento e dependência externa (`probe-results.json`). Assinatura e digest são placeholders para estrutura; não são provas criptográficas.
- **Regressão do núcleo atual:** 40/40 testes, zero falhas/ignorados, nas classes `ProtocolTests`, `ManifestTests` e `XsdAndWireTests`. TRX confirma `AlertExtensionMissingInPinnedConsultationSchemaFailsClosed=Passed`. Não é integração de schemas novos nem nova execução integral do CI.

| Resposta sintética, nos três códigos 100/120/150 | `retEnviNFe` | `retConsSitNFe` | `nfeProc` |
| --- | --- | --- | --- |
| Protocolo 15/17, sem mensagens | XSD aceita | XSD aceita | XSD aceita |
| Protocolo 15/17, 1 ou 5 pares | XSD aceita | XSD rejeita | XSD aceita |
| 6 pares | XSD rejeita | XSD rejeita | XSD rejeita |
| Protocolo 16/18 | XSD rejeita | XSD rejeita | XSD rejeita |
| `xMsg` sem `cMsg`, código 120 e protocolo 17 | XSD rejeita | XSD rejeita | XSD rejeita |

O fato de o XSD aceitar 120 sem mensagens ou 100/150 com mensagens não estabelece autorização real nem aderência semântica à NT. Os XMLs `*-120-alert17.xml` são apenas reproduções estruturais, sem validade fiscal.

## 3. Pacotes, hashes, diferenças e dependências

Catálogo [Nacional](https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=BMPFMBoln3w%3D), [SVRS NF-e](https://dfe-portal.svrs.rs.gov.br/Nfe/Documentos) e [SVRS NFC-e](https://dfe-portal.svrs.rs.gov.br/Nfce/Documentos), baixados em 05/10. O Nacional ainda lista PL010f, publicado em 31/08/2026, como pacote atual. As versões históricas foram conferidas para evitar a suposição de que o mais recente contém todos os serviços.

| ZIP/local ou identificação do catálogo | Publicação | XSDs examinados | Consulta/resultado | SHA-256 do ZIP |
| --- | --- | ---: | --- | --- |
| PL009n NT2023.004/NT2019.001 | 19/03/2024 | 19 | Consulta antiga, sem mensagens | `8e269cd5d5d60532b709c8a88bf590323b94002efd3a49935fb5fd2db5957a62` |
| PL010b NT2025.002 v1.30, SVRS | 07/10/2025 | 5 | Não inclui consulta | `2deaa8d430d0acb47deae06b9b6d1202dbb436baf3d2d3aa834a534a3d6fa1b8` |
| PL010c, NT2022.002 v1.30 | 26/03/2026 | 5 | Não inclui consulta | `aceb8df3ba5235aea4dcac2502a68ef00c6cd34f1d26cb3f6eebae9d675cd0fc` |
| PL010d v1.01 (`PL010d-v101.zip`) | 08/06/2026 | 22, dentro de três ZIPs | Consulta idêntica à baseline, sem mensagens | `cc50170b276c23bdab88650e1c68a46fdc707c8e810664452bba974cf744e7db` |
| PL010d v1.02 | 26/06/2026 | 22 | Consulta idêntica à baseline, sem mensagens | `13c51a8e3a98ed5a63b64a2312bc6d057f98dd07de6d3c346f8e07352f1bbc12` |
| PL010d v1.03 | 10/07/2026 | 22 | Consulta da baseline, sem mensagens | `45ceefe4dfbbfec93958283b650a2f1e1734784f4770d070b9907754de081d9b` |
| PL010e v1.01 | 26/06/2026 | 5 | Não inclui consulta | `8d4d6c102cc458fb945c287542cf6f7077f159fdbfab4511954b8c0b94dedca5` |
| PL010e v1.02 | 10/07/2026 | 5 | Não inclui consulta | `d44ae5aa6a0d1cabf6235d2d2d47b75be5dd87bc6b90a7ec3dcec99c3d41bda1` |
| PL010f v1.04 | 31/08/2026 | 5 | Autorização com mensagens; não inclui consulta | `b8589490a58a09a993a80e6ac4d7ed10f20892061ecfc56719337098d4b95998` |

O inventário de **cada entrada original**, SHA-256 por XSD/ZIP interno, dependências e igualdade byte a byte com o arquivo homônimo da baseline está em `zip-inventory.json`. URLs exatas, horários, status, tamanho, tipo de conteúdo e redirecionamentos estão em `source-index.json` e no [índice legível de fontes](evidence/ef05/FONTES.md). Nomes curtos de downloads históricos são aliases locais; as entradas originais do ZIP não foram renomeadas nem reserializadas.

Precedência da composição atual: **PL009n < PL010d v1.03 / NFe < PL010f v1.04**. Não aplicar indiscriminadamente as subárvores `Evento` ou `CadConsultaCadastro` do PL010d; há nomes de tipos/arquivos comuns. Os ZIPs PL010b/c/e/f têm só cinco arquivos de leiaute/tipos e não formam sozinhos o conjunto de seis serviços.

`leiauteConsSitNFe_v4.00.xsd` do PL010d v1.01, v1.02 e v1.03 é idêntico: SHA-256 `646e3a6bd26720b12a1322d5d25181504d82175c3650e14a1120dfb65895b618`. O equivalente PL009n tem hash `5d8826f29ff1b0f2fd3c1ccda808fdc2ebac73f6d4abf716cb9049eb5169c535` e também não contém mensagens. O leiaute de autorização atual tem hash `2bace939973916d54184ff3e2740041a932de5d79772f3363504504160f22542`.

Fechamento das dependências, registrado integralmente em `dependency-closures.json`:

- `nfe`, `enviNFe`, `retEnviNFe`, `procNFe`: o entrypoint específico + `leiauteNFe`, `DFeTiposBasicos`, `tiposBasico`, `xmldsig-core-schema` — cinco arquivos em cada conjunto.
- `consSitNFe`, `retConsSitNFe`: o entrypoint específico + `leiauteConsSitNFe`, `tiposBasico`, `xmldsig-core-schema` — quatro arquivos em cada conjunto.

**Não há composição candidata recomendada.** Reintroduzir PL010e/c/b não acrescenta o serviço ausente e pode regredir outros tipos. Nenhuma dessas alternativas foi integrada ao núcleo. A compilação e os testes apresentados são controles da baseline existente.

## 4. Matriz normativa e de aplicabilidade

As datas abaixo são **cronogramas anunciados** nos PDFs. Não foi contatado autorizador nem obtida prova de implantação por ambiente. Catálogo, revisão interna, efeito legal e ativação técnica são registros distintos. A ressalva da SVRS sobre possíveis revisões de datas deve ser preservada. Fontes e hashes completos: `FONTES.md`.

| Fonte e revisão interna | Publicação do catálogo | Homologação anunciada | Produção anunciada | Aplicabilidade e diferença frente ao schema/código |
| --- | --- | --- | --- | --- |
| NT 2026.002 v1.11, set/2026, pp.3–6 e 24; v1.10a também conferida | 01/10/2026; v1.10a 25/08 | Alertas 01/09/2026 | Alertas 05/10/2026; três RVs prorrogadas para 14/12 | **Aplicável parcialmente ao 65.** Consulta com mensagens bloqueada; 5E17-65 é alerta 172 de situação do destinatário. Prorrogação não é adiamento geral dos alertas. DANFE tipo 2/modelo 55/contingência continuam excluídos. |
| RTC NT 2025.002 v1.52, set/2026, pp.6 e 70 | 01/10/2026 | Correção VC02-50 até 05/10 | VC02-50 até 03/11; VC02-14 em 03/11 | **Delta v1.52: regras VC02-14/50 são modelo 55**, fora do recorte. A NT acumulada contém regras 55/65, condicionadas à tributação/EF-06. PL010f representa leiaute v1.50; compilação não prova aplicação das RVs de v1.51/v1.52. |
| NT 2026.009 v1.00, set/2026, pp.3–5 | 09/09/2026 | Até 17/09/2026 | Até 17/09/2026 | **Fora do modelo 65.** I08-140/modelo 55 amplia a exceção de CFOP 1.949/2.949; texto afirma não alterar leiaute/XSD. Trecho riscado foi conferido visualmente. Não exige mudança desta baseline. |
| NT 2026.010 v1.00, out/2026, pp.3–4 | 01/10/2026 | Sem data indicada (`-`) | 01/12/2026 | **Fora do modelo 65.** Impressão DANFE modelo 55 com RTC; não é manual do DANFE NFC-e, nem solução para consulta. |
| NT 2026.007 v1.10, set/2026, pp.3–4, 7–11 | 01/10/2026 | v1.00 01/09; v1.10 até 05/10 | 03/11/2026 | **Parcialmente aplicável.** Regras LCC/CCC/CRT para 55/65; C17-42 veda NFC-e sem IE do emitente, com exceção anunciada a partir de 2033. Permissão de NF-e sem IE é modelo 55. IE opcional no XSD não autoriza remover a restrição fiscal de NFC-e. Regime/cadastro do piloto TBD. |
| NT 2026.008 v1.00, set/2026, pp.3–8 | 01/10/2026 | Campos/RVs 05/10/2026; NB01-30 01/02/2027 | 03/11/2026; NB01-30 01/03/2027 | **Parcialmente aplicável.** `vUnComLiq`, `vProdLiq`, `vProdLiqTot` ausentes no PL010f. I11c-10 diz implementação futura, apesar da tabela geral. NB01-10 proíbe `ICMSPrevistoPagtoAntecip` em modelo 65. Não assumir todos os campos obrigatórios hoje ou adicionar o grupo à NFC-e. P.6 tem cabeçalho residual “NT 2026.006”; preservar e registrar a inconsistência. |
| NT 2026.006 v1.00, ago/2026, p.3 e grupo YC | 25/08/2026 | 05/10/2026 | Preparação 03/11/2026 | **Preparatória para 55/65.** PDF expressamente dispensa uso dos campos de split payment em produção em 2026; ativação prevista para 2027. Grupo/evento não implementados; não inferir obrigação atual. |
| IT 2025.002 v1.70, set/2026, pp.1/3/12 | 01/10/2026 | Tabelas até 16/10/2026 | Tabelas até 16/10/2026 | **TBD por classificação/operação/regime.** Não é XSD; manter versão, validade e indicador de modelo de cada código. O PDF possui rodapé residual “Versão 1.00”; capa/controle identificam v1.70. Tabelas completas não foram incorporadas ou certificadas para o piloto. |
| NT 2026.004 v1.01, CNPJ/chaves alfanuméricas | 08/06/2026 | Até 15/06/2026 | 01/07/2026 | **Aplicável ao formato.** Incorporada via tipos PL010d/f e núcleo. Algoritmos/testes não provam existência ou habilitação cadastral. |
| NT 2025.001 v1.03 corrigida, QR3 | 29/09/2025 | Ajustes até 20/10/2025 | 03/11/2025 | **Aplicável ao QR3 online** já fixado; QR3 inicial tinha datas 02/06 e 01/09/2025. Não abrir contingência ou alterar URL DF neste levantamento. |
| NT 2023.002 v1.01 | 18/11/2025 | 19/01/2026 | Até 02/02/2026 | **Aplicável à evolução NFC-e.** A supressão de denegação/lote fiscal não elimina por si só o envelope XSD de uma nota. Emitente CPF/avulsa fora do núcleo atual. |
| NT 2021.003 v1.50, set/2026, pp.3–5 | 25/09/2026 | Grupo V 01/04/2027 | Grupo V 01/09/2027 | **Delta CCG/Grupo V restrito a NF-e 55**, conforme evolução da NT. Não confundir com os requisitos gerais cEAN/cEANTrib que também alcançam NFC-e quando houver GTIN. Produto/NCM/GTIN do piloto TBD. |
| NT 2023.003 v1.40, set/2026, pp.3–4 | Nacional 25/09; SVRS 24/09 | 28/09/2026 | 05/10/2026 | **Delta exclusivo AM**, fora do DF. Não habilitar CFOP 5.949 no DF por analogia com exceções de AM/SP/ES. |
| NT 2026.001 v1.02b, PAA, pp.1/3/4 | 31/07/2026 | Base v1.02 03/08/2026 | Base v1.02 05/10/2026; a/b sem nova data | **Fora da arquitetura autorizada:** PAA não foi escolhido; integração direta com A1 permanece o recorte. Publicação não transforma esta API em PAA habilitado. |
| NT 2026.003 v1.00, mai/2026, pp.3–4 | 25/05/2026 | 01/07/2026 | 03/08/2026 | **Fora:** DANFE Simplificado Tipo 2 para modelo 55. Não é solução de protocolo nem ampliação automática da NFC-e. |
| NT 2019.001 v1.70, ago/2025, pp.20–22; PDF DF v1.20 | 18/08/2025; anexo DF histórico sem data atual comprovada | DF conforme linha D6/D4: 05/10/2020 | D6/65: 02/09/2024; D4: 01/02/2021 | **Aplicável por regra/CST/EF-06**, com divergência documental concreta descrita abaixo. Não usar só o “Sim” do anexo antigo como configuração atual. |
| Ajuste SINIEF 19/16 compilado, Portaria DF 387/2019 compilada | Textos consultados em 05/10/2026 | Não são calendários XSD | Efeitos por dispositivo/alteração | Base jurídica do 65/DF; credenciamento, identidade e guarda permanecem exigências operacionais futuras. Portaria inclui 998/2025 e retificação; art.9 revogado. Não define os regimes/operações aceitos pelo produto. |
| AD DF Assuntos Econômicos 1/2026 + AD 2/2025 e DODF 182 | Ato 29/09; DODF 30/09/2026 | Não se aplica | Art.4: efeitos 30/09/2026 | **Aplicável à matriz cBenef, condicionado à operação.** Fonte primária completa localizada; HTML e DODF divergem. Não criar enquadramento automático. |

MOC 7/anexos e a prova EF-04 permanecem referências da baseline já documentadas em `TECHNICAL-SCOPE.md`/`VALIDATION.md`; não foram tratados como novas revisões nem como novas evidências de homologação. SDK 10.0.401, locks, auditoria NuGet e CI foram preservados.

## 5. Reconciliação DF e lacunas delimitadas

### Regras de validação

O PDF estadual `Nota_Tecnica_2019.001_regras_adotadas_pelo_DF.pdf`, baixado novamente, identifica **v1.20** e marca N12-90/934 como adotada para 55/65. A NT nacional **v1.70**, p.21, marca **DF/N12-90 como D\***, cuja legenda diz que a regra não será aplicada. Essa é uma divergência de fonte/versão a registrar, não algo a resolver por suposição.

Na mesma matriz nacional, DF/N12-85, N12-86 e N12-94 têm D6, modelos 55/65 e exceções E2/E3/E4; para NFC-e, D6 aponta produção em 02/09/2024 e homologação em 05/10/2020. N12-98 tem D4, 55/65, produção 01/02/2021 e homologação 05/10/2020. P.22 descreve E2 devolução, E3 ajuste e E4 entrada. Isso não torna tais operações admissíveis no piloto NFC-e; a aplicação exige cruzamento do modelo, CST/regime e operação aprovados.

**Recomendação:** usar a revisão nacional atual para a análise técnica e manter o conflito do anexo estadual explicitamente pendente de confirmação oficial do DF antes de fixar comportamento tributário operacional. Não houve consulta ao autorizador para provar quais RVs estão ativas.

### Anexo de benefícios

O serviço público usado pelo portal DF foi consultado sem autenticação: `POST /WPI/api/legislacao/obter-legislacao`, apenas busca normativa. Payloads e hashes das respostas estão em [df-requests.json](evidence/ef05/df-requests.json); respostas integrais históricas em `tmp/ef05-research/df-results.json`. O corpo do AD 1/2026 continua avisando que a consolidação está em atualização e remete ao DODF.

Foi obtido o **DODF nº 182, 30/09/2026, íntegra**, 231 páginas, SHA-256 `5751bdac761ddf79a9fa46c7c258f164a252e2999a99704e3eeec596b0e8981a`. Ato na p.44; anexo nas pp.45–48, **índices 1–270**, conferidos visualmente. As tabelas são imagens; a busca textual retorna quase só cabeçalhos nessas páginas e não serve como prova de ausência do anexo.

- Art.2 e p.46, linhas 104/138: **DF814104 e DF814151 encerram utilização em 30/09/2026**.
- Art.1 e p.48: 13 códigos distintos acrescentados — DF817010, DF817014, DF817504, DF817505, DF817506, DF817507, DF817508, DF817509, DF817510, DF817511, DF817512, DF827001, DF827002. O art.1 repete DF817512; registrar a duplicação literal, sem inventar um código adicional.
- DF817512 aparece na linha 257, início 30/09/2026. Os demais possuem datas próprias no anexo; a data de publicação não deve substituir automaticamente as datas de cada linha.
- A tabela HTML do AD 2/2025 possui **257 códigos distintos**, não contém esses 13 novos e mantém vazia a data final das duas linhas encerradas. Apesar de haver nota sobre o AD 1/2026, o corpo tabular não está reconciliado. Evidência: `df-benefits-html-rows.json`, `df-benefits-selected-rows.json`, imagens `dodf-p45.png` a `dodf-p48.png` e detalhe `dodf-p48-beneficios-detalhe.png`.

**Limite:** localizar o anexo completo e comparar as alterações identificadas não equivale a validar cada célula das 270 linhas, seus fundamentos legais ou o direito de um contribuinte a um benefício. A conversão integral em tabela versionada, revisão independente de transcrição/CST/datas e vínculo com os casos EF-06 permanecem pendentes. Não preencher campos vazios do anexo por inferência.


## Decisões do piloto — EF-06

O [contrato EF-06/0.2](PILOT-CONTRACT.md) confirma presencial/retirada/consumo próprio, regime conforme empresa escolhida, série exclusiva validada, operações próprias por cliente e operador central delegado. Empresa/CRT/grupos, autoridades/delegações, série/histórico e casos tributários comprovados ainda são `TBD` na [ficha](PILOT-REGISTRATION.md). [Pesquisa 0.2](evidence/ef06/FONTES-v0.2.md) atualiza as fontes jurídicas sem resolver condições específicas DF, consulta com alertas ou ativação técnica. Fixtures não definem enquadramento. Migração exige pacote oficial/proveniência/seis conjuntos compilados, preservando bytes/baseline anteriores; transporte/replay fiscais reais permanecem futuros.

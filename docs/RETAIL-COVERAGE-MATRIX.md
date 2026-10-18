# Matriz de cobertura tributária do varejo — NFC-e 65/DF

Levantamento documental preparado em 17/10/2026 (America/Sao_Paulo). Mapeia as
**variantes tributárias** que os segmentos de varejo pretendidos tendem a exigir,
o estado de cada variante no preparador `nfce-preparation/0.1` e uma proposta de
priorização de implementação. Complementa o [modelo de entrada](NFCE-INPUT-MODEL.md)
e o [dicionário do piloto](PILOT-DATA-DICTIONARY.md); não altera contratos,
não habilita variantes no código por si e não substitui o caso fiscal aprovado da
[ficha do piloto](PILOT-REGISTRATION.md). **Atualização de 17/10/2026:** o
incremento P1 (ST retido) foi implementado em código pela issue
[#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24); ver
seções 3 e 4.

**Princípio do levantamento:** não se mapeiam produtos, que são infinitos;
mapeiam-se variantes tributárias, que são finitas. A classificação de cada
mercadoria real (NCM, CEST, CST/CSOSN, CFOP, benefício) permanece responsabilidade
do sistema cliente/responsável tributário, como o dicionário já fixa. A decisão
de produto deste repositório é **quais grupos o preparador representa e valida**
— e em que ordem. **Atualização de 17/10/2026 (2):** o incremento P2
(isenções/não incidência) foi implementado pela issue
[#26](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/26); ver
seções 3 e 4-A.

As associações segmento→variante abaixo são **hipóteses de produto para
priorização**, baseadas em perfis usuais de sortimento; não são determinações
fiscais. Cada variante só entra em operação com a matriz de validação da
seção 3 do dicionário preenchida e fontes conferidas para o caso aprovado.
**Atualização de 17/10/2026 (3):** o incremento P3 (redução de base de
cálculo) foi implementado pela issue
[#28](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/28); ver
seções 3 e 4-B.

## 1. Estado do preparador no levantamento original

Fatos verificados no código em 17/10/2026, **antes do incremento P1** (a tabela
é o retrato daquele levantamento; o estado vigente das variantes está na
seção 3):

| Capacidade | Estado | Evidência |
| --- | --- | --- |
| Grupos ICMS aceitos | Somente `ICMSSN102` com CSOSN `102` (CRT 1) e `ICMS00` com CST `00` (CRT 3); qualquer outro grupo é recusado com "Grupo ICMS não mapeado" | [PreparationJson.cs](../src/EmissorFiscal.Core/Preparation/PreparationJson.cs), validações de CRT e de grupo |
| PIS/COFINS aceitos | `PISAliq`/`COFINSAliq` (CST 01–02) e `PISNT`/`COFINSNT` (CST 04–09); demais grupos recusados | [PreparationJson.cs](../src/EmissorFiscal.Core/Preparation/PreparationJson.cs), `Contribution` |
| CEST e cBenef | Campos opcionais já parseados, validados (CEST 7 dígitos; cBenef 8–10) e serializados em `prod/CEST`/`prod/cBenef`. Não há exigência condicional (ex.: CEST obrigatório quando ST) | [PreparationModels.cs](../src/EmissorFiscal.Core/Preparation/PreparationModels.cs), [NfcePreparation.cs](../src/EmissorFiscal.Core/Preparation/NfcePreparation.cs) |
| Campos ST no item | O record `PreparationIcms` não representa `vBCSTRet`, `pST`, `vICMSSubstituto`, `vICMSSTRet`, FCP-ST nem base/alíquota efetivas | [PreparationModels.cs](../src/EmissorFiscal.Core/Preparation/PreparationModels.cs), `PreparationIcms` |
| Totais ST | `ICMSTot` já exige `vBCST`, `vST`, `vFCPST` e `vFCPSTRet` declarados pelo cliente | [PreparationModels.cs](../src/EmissorFiscal.Core/Preparation/PreparationModels.cs), `PreparationIcmsTotals` |
| RTC (IBS/CBS) | Somente CST `000` com `cClassTrib` `000001` (regular); demais classificações/grupos recusados | [NFCE-INPUT-MODEL.md](NFCE-INPUT-MODEL.md), seção 3 |

Consequência prática: uma venda de varejo típica com mercadoria em substituição
tributária já retida — por exemplo, uma caixa de cerveja — era **recusada** pelo
subconjunto da data do levantamento, pois exigiria `ICMSSN500` (Simples) ou
`ICMS60` (regime normal). A recusa é o comportamento projetado para variante sem
suporte; este documento organiza a ordem de ampliação. O incremento P1 da seção 4
foi implementado em 17/10/2026 (ver status na seção 3); a tabela acima permanece
como o retrato do levantamento original.

## 2. Matriz segmento × variantes esperadas (hipóteses de priorização)

| Segmento | Perfil usual do sortimento | Variantes ICMS esperadas | PIS/COFINS esperados | Observações |
| --- | --- | --- | --- | --- |
| Mercado/minimercado | Mix amplo: bebidas, cigarros, higiene/limpeza, alimentos, hortifrúti | ST retido (CSOSN 500 / CST 60) em parte relevante; tributado integral (102 / 00); isenções/reduções de cesta básica conforme legislação DF (40/41/20; CSOSN 103/300/400) | Alíquota básica (01), monofásico de bebidas frias (04), alíquota zero de cesta básica (06) | Segmento que exige a maior largura de variantes; concentra P1 e P2 |
| Padaria | Produção própria + revenda de bebidas/industrializados | Tributado integral; ST retido na revenda | 01 e 04 | Produção própria tende ao grupo tributado; revenda espelha o mercado |
| Distribuidora de bebidas | Quase todo o sortimento em bebidas | **Dominância de ST retido** (CSOSN 500 / CST 60) | Monofásico (04) dominante | Sem P1, o segmento é inviável no emissor |
| Lanchonete | Preparo próprio + bebidas | Tributado integral no preparo; ST retido nas bebidas | 01 e 04 | Semelhante à padaria |
| Armarinho/papelaria | Mercadorias gerais de baixo giro ST | Tributado integral dominante; ST pontual | 01 | Segmento mais próximo do recorte já implementado |
| Agropecuária | Rações, insumos, defensivos, utensílios | Isenções/reduções de insumos (40/41/20; CSOSN correspondentes) conforme convênios; tributado integral; ST pontual | 01; regimes específicos de insumos a confirmar | Variantes de benefício exigem reconciliação DF (pendência 2 do [roadmap](ROADMAP.md)) |

A coluna de variantes é hipótese a confirmar com o responsável tributário de cada
empresa e com as fontes da seção 5. Nenhuma linha autoriza enquadramento de
mercadoria real nem comprova aplicabilidade no DF.

## 3. Variante × estrutura × status × prioridade

Grupos e campos conferidos no [leiauteNFe_v4.00.xsd](../schemas/nfe/4.00/files/leiauteNFe_v4.00.xsd)
da baseline local (`nfe-4.00-offline-2026-10-04`); a presença no XSD não comprova
exigência ou dispensa fiscal, conforme a regra de leitura do dicionário.

| Prioridade | Variante | Grupo XSD e campos além de `orig`+CST/CSOSN | Status no preparador | Esforço estrutural |
| --- | --- | --- | --- | --- |
| Entregue | CSOSN 102 (Simples, tributado sem crédito) | `ICMSSN102` | Implementado | — |
| Entregue | CST 00 (normal, tributado integral) | `ICMS00`: `modBC`, `vBC`, `pICMS`, `vICMS` | Implementado | — |
| **P1** | CSOSN 500 (Simples, ST retido/antecipação) | `ICMSSN500`: sequências opcionais `vBCSTRet`+`pST`+`vICMSSubstituto`?+`vICMSSTRet`, FCP-ST (`vBCFCPSTRet`/`pFCPSTRet`/`vFCPSTRet`) e efetivos (`pRedBCEfet`/`vBCEfet`/`pICMSEfet`/`vICMSEfet`) | Implementado em 17/10/2026 ([#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24)): campos representados, validados e serializados; exemplo [sale-st-crt1.json](../samples/preparation/sale-st-crt1.json) | Entregue; totais já existiam |
| **P1** | CST 60 (normal, ST retido) | `ICMS60`: mesmas sequências do `ICMSSN500` | Implementado em 17/10/2026 ([#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24)), compartilhando os campos do `ICMSSN500`; coberto por teste com XSD | Entregue |
| **P1** | CEST condicional ao ST | `prod/CEST` | Implementado em 17/10/2026 ([#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24)): CEST exigido quando o grupo declarado é `ICMSSN500`/`ICMS60`; fonte baixada e hasheada em [FONTES.md](evidence/retail-coverage/FONTES.md) | Entregue; Convênio ICMS 142/18, cláusula vigésima, I |
| **P2** | CSOSN 103/300/400 (isenção de faixa, imune, não tributada) | Mesmo grupo `ICMSSN102`, enumeração já admite os quatro códigos | Implementado em 17/10/2026 ([#26](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/26)): enumeração estendida, exemplo [sale-exempt-crt1.json](../samples/preparation/sale-exempt-crt1.json); fonte Anexo III-A hasheada em [FONTES.md](evidence/retail-coverage/FONTES.md) | Entregue; sem campos novos |
| **P2** | CST 40/41 (isenta, não tributada) | `ICMS40` (admite 40/41/50): opcionais `vICMSDeson`+`motDesICMS` | Implementado em 17/10/2026 ([#26](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/26)): `orig`+CST 40/41 aceitos e cobertos por teste com XSD; CST 50 e desoneração recusados | Entregue; desoneração fora do recorte |
| **P3** | CST 20 (redução de base) | `ICMS20`: `modBC`, `pRedBC`, `vBC`, `pICMS`, `vICMS` + FCP/desoneração opcionais | Implementado em 17/10/2026 ([#28](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/28)): campos obrigatórios representados, validados e serializados; FCP e desoneração recusados; exemplo [sale-red-crt3.json](../samples/preparation/sale-red-crt3.json) | Entregue; percentual real por caso segue exigindo fonte (RICMS-DF pendente) |
| **P4** | CST 90 / CSOSN 900 (outros) | `ICMS90`/`ICMSSN900`: combinações amplas de campos opcionais | Sem representação; grupos recusados | Genéricos; implementar somente com caso aprovado que os exija |
| **P4** | PIS/COFINS `Outr` (CST 49+) e `Qtde` | `PISOutr`/`PISQtde` e equivalentes COFINS | Sem representação | Conforme caso; monofásico de bebidas (CST 04) já é representável via `PISNT`/`COFINSNT` |
| Transversal | RTC além de CST 000/`cClassTrib` 000001 | `IBSCBS` com outras classificações | Somente o par regular; consulta CFF do P2 (17/10/2026) confirmou que isenção/não incidência de ICMS **não** tem correspondência genérica nos CST RTC 400/410, e que cesta básica nacional é CST 200/`cClassTrib` 200003 (alíquota zero, art. 199 da LC 214/2025) — grupo não representado, **pendência impeditiva** registrada em [FONTES.md](evidence/retail-coverage/FONTES.md) | Cada variante nova de ICMS exige conferir a classificação IBS/CBS correspondente na tabela CFF antes da implementação |

Gate comum a todas as linhas: preencher a matriz de validação (seção 3 do
[dicionário](PILOT-DATA-DICTIONARY.md)) com fonte oficial, regra, equações e
tolerâncias **quando a fonte as estabelecer**, exemplo aprovado e responsável.
Sem isso, a variante permanece recusada, que é o comportamento correto.

## 4. Incremento P1 — proposto e entregue

O escopo abaixo foi proposto neste levantamento e **implementado em 17/10/2026**
pela issue [#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24)
(commit `94fb267`), com 152/152 testes no CI; evidências na
[validação](VALIDATION.md). O texto permanece como registro do escopo aprovado:

- Representar os campos ST-retido em `PreparationIcms` (ou record próprio por
  grupo), com validação de representação decimal nos tipos `TDec_1302`/`TDec_0302a04Opc`.
- Aceitar `ICMSSN500` (CRT 1) e `ICMS60` (CRT 3), mantendo a recusa das demais
  combinações e a regra de não recalcular/completar valores declarados.
- Exigir CEST quando o grupo declarado for ST, com fonte registrada.
- Exemplo sintético novo "caixa de cerveja": unidades CX/UN, CFOP de ST a
  confirmar na fonte, PIS/COFINS CST 04 via grupos NT existentes, destinatário
  com os textos de homologação vigentes.
- Testes positivos/negativos e validação XSD via CLI `prepare`.

Os valores do exemplo serão sintéticos e declarados, como nos casos CRT 1/3
atuais; nenhuma alíquota ou MVA real é embutida.

## 4-A. Incremento P2 — entregue

Escopo implementado em 17/10/2026 pela issue
[#26](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/26),
seguindo a convenção do P1:

- Enumeração do grupo `ICMSSN102` estendida para CSOSN `102|103|300|400`
  (Anexo III-A do Convênio s/nº de 1970, redação do Ajuste SINIEF 39/23).
- Grupo `ICMS40` representado com `orig`+CST `40|41` (Tabela B, mesma redação);
  CST `50` (suspensão) e a sequência de desoneração (`vICMSDeson`/`motDesICMS`)
  permanecem recusados neste recorte.
- Exemplo sintético [sale-exempt-crt1.json](../samples/preparation/sale-exempt-crt1.json)
  com três itens (CSOSN 103/300/400) e o caso `ICMS40` coberto por teste
  derivado do exemplo CRT 3; validação XSD via CLI `prepare` incluída em
  `scripts/verify.ps1`.
- Gate transversal RTC consultado na tabela CFF: o par regular `000`/`000001`
  segue único aceito; CST 200/`cClassTrib` 200003 (cesta básica nacional) e os
  CST RTC 400/410 ficam como pendência impeditiva com fonte registrada em
  [FONTES.md](evidence/retail-coverage/FONTES.md).

Nenhuma isenção, imunidade ou benefício real foi aprovado; os códigos dos
exemplos continuam hipóteses de teste e o enquadramento permanece com o
responsável tributário do caso aprovado.

## 4-B. Incremento P3 — entregue

Escopo implementado em 17/10/2026 pela issue
[#28](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/28),
seguindo a convenção dos incrementos P1/P2:

- Grupo `ICMS20` representado com `orig`+CST `20` e os campos obrigatórios
  `modBC`, `pRedBC`, `vBC`, `pICMS`, `vICMS` (Tabela B do Convênio s/nº de 1970,
  redação do Ajuste SINIEF 39/23), aceito somente sob CRT 3; valores declarados,
  nunca recalculados. As sequências opcionais de FCP (`vBCFCP`/`pFCP`/`vFCP`) e
  de desoneração (`vICMSDeson`/`motDesICMS`/`indDeduzDeson`) permanecem
  recusadas neste recorte; redução de base no Simples (CSOSN 900) segue P4.
- Exemplo sintético [sale-red-crt3.json](../samples/preparation/sale-red-crt3.json)
  com percentual de redução declarado como valor de teste; validação XSD via
  CLI `prepare` incluída em `scripts/verify.ps1`.
- Gate transversal RTC consultado na tabela CFF: o CST RTC `222` (Redução de
  Base de Cálculo) tem classificação única `222001`, restrita a transporte
  internacional de passageiros, sem correspondência genérica com a redução de
  base do ICMS; o par regular `000`/`000001` segue único aceito, com fonte
  registrada em [FONTES.md](evidence/retail-coverage/FONTES.md).

Nenhum percentual de redução real foi aprovado; a aplicabilidade no DF depende
do RICMS-DF (pendência da seção 5) e do responsável tributário do caso aprovado.

## 5. Fontes a levantar

Na entrega original da matriz nenhuma fonte externa havia sido baixada. O
incremento P1 coletou o Convênio ICMS 142/18, registrado com URL, data/hora UTC
e SHA-256 em [evidence/retail-coverage/FONTES.md](evidence/retail-coverage/FONTES.md);
as demais fontes seguem pendentes e devem ser registradas no mesmo padrão
(evidências EF-05/EF-06) quando a coleta ocorrer.

| Fonte | Para quê | Status |
| --- | --- | --- |
| [Convênio ICMS 142/18 — CONFAZ](https://www.confaz.fazenda.gov.br/legislacao/convenios/2018/CV142_18) e anexos (tabela CEST) | Regime geral de ST, segmentos/CEST e obrigatoriedade do código | Obtido em 17/10/2026 (incremento P1), hasheado em [FONTES.md](evidence/retail-coverage/FONTES.md); vigência de item/MVA/pauta e aplicabilidade no DF seguem com o responsável tributário |
| Regulamento do ICMS do DF e anexos de ST/benefícios vigentes | Quais segmentos/mercadorias têm ST ou benefício no DF; conecta-se à pendência 2 do roadmap (anexo DF não reconciliado) | `TBD` — identificar ato vigente; não usar tabela histórica como cobertura atual |
| Base legal do regime monofásico de PIS/COFINS para bebidas frias | Confirmar CST aplicável na revenda (hipótese: 04) | `TBD` — identificar lei/ato e vigência |
| [Convênio ICMS 100/97 — CONFAZ](https://www.confaz.fazenda.gov.br/legislacao/convenios/1997/CV100_97) | Reduções/isenções de insumos agropecuários e vigência | `TBD` — confirmar prorrogações e aplicação no DF |
| [Tabela de Classificação Tributária IBS/CBS — CFF](https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria) | CST/`cClassTrib` RTC correspondentes a cada variante nova | Consultada e hasheada em 17/10/2026 (incremento P2) para isenção/não incidência; demais variantes seguem exigindo consulta própria |

## 6. Limites desta entrega

- Documento de priorização: não escolhe enquadramento de mercadoria real nem
  altera contratos existentes. Na entrega original não habilitava variantes no
  código; P1, P2 e P3 foram implementados depois, com issues próprias
  ([#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24),
  [#26](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/26),
  [#28](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/28)),
  conforme a convenção. P4 continua exigindo issue, matriz de validação do
  dicionário e fontes antes de qualquer implementação.
- As frequências por segmento são hipóteses de produto; a cobertura efetiva do
  piloto continua determinada pela empresa selecionada e seus casos aprovados.
- Nenhuma alíquota, MVA, pauta ou benefício é afirmado; nenhum autorizador foi
  contatado; nada aqui comprova ativação de regra em homologação ou produção.
- A transmissão permanece no gate EF-14 do [roadmap](ROADMAP.md); este documento
  não a antecipa.

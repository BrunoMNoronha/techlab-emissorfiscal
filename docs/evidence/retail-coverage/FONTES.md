# Fontes — matriz de cobertura tributária do varejo

Criado em 17/10/2026. Na entrega original da
[matriz de cobertura](../../RETAIL-COVERAGE-MATRIX.md) nenhuma fonte externa foi
baixada; a priorização usou fatos estruturais conferidos somente em material
local. A coleta do Convênio ICMS 142/18, feita para o incremento P1, está
registrada na seção abaixo.

| Material local | Uso |
| --- | --- |
| [leiauteNFe_v4.00.xsd](../../../schemas/nfe/4.00/files/leiauteNFe_v4.00.xsd) da baseline `nfe-4.00-offline-2026-10-04` | Grupos `ICMSSN102` (CSOSN 102/103/300/400), `ICMSSN500`, `ICMS00`, `ICMS20`, `ICMS40` (CST 40/41/50), `ICMS60`, `ICMS90`, `ICMSSN900` e seus campos/sequências |
| [PreparationModels.cs](../../../src/EmissorFiscal.Core/Preparation/PreparationModels.cs), [PreparationJson.cs](../../../src/EmissorFiscal.Core/Preparation/PreparationJson.cs), [NfcePreparation.cs](../../../src/EmissorFiscal.Core/Preparation/NfcePreparation.cs) | Estado do preparador: whitelist de grupos, CEST/cBenef, totais ST |

Quando a coleta das fontes externas da seção 5 da matriz ocorrer, registrar aqui
cada download com URL final, data/hora UTC, SHA-256 dos bytes e limitações,
no mesmo padrão de [ef05/FONTES.md](../ef05/FONTES.md). Acesso a catálogo ou
texto legal não comprova ativação de regra em autorizador.

## Coleta de 17/10/2026 — incremento P1 (ST retido)

| Arquivo local | Origem consultada (URL final) | Data/hora UTC | SHA-256 dos bytes | Resultado |
| --- | --- | --- | --- | --- |
| CV142_18.html (não versionado; apenas hash) | <https://www.confaz.fazenda.gov.br/legislacao/convenios/2018/CV142_18> | 2026-10-17T21:18:07Z | `7c2bdc9db1ceb11faaa56b2582e77e5a1bcb66e51cd62b6de0f155a78359c45a` | Obtido (HTTP 200, 933.142 bytes) |

Fatos conferidos no texto baixado, usados pelo incremento P1:

- **Cláusula vigésima, I**: o documento fiscal das operações com bens dos Anexos
  II a XXVI contém "o CEST de cada bem e mercadoria, ainda que a operação não
  esteja sujeita ao regime de substituição tributária". É a fonte da exigência
  condicional de CEST quando o grupo ICMS declarado é `ICMSSN500`/`ICMS60`.
- **Anexo IV** (cervejas, chopes, refrigerantes, águas e outras bebidas): item
  21.3, CEST 03.021.03, NCM 2203.00.00, "Cerveja em lata" — usado como hipótese
  de teste no exemplo sintético `sale-st-crt1.json`, sem enquadrar mercadoria real.

Limitações: a página do CONFAZ consolida redações com alterações posteriores;
o hash identifica os bytes na data da coleta, não certifica vigência de item,
MVA, pauta ou aplicabilidade no DF, que seguem com o responsável tributário e
com o regulamento do DF ainda pendente abaixo.

## Coleta de 17/10/2026 — incremento P2 (isenções/não incidência)

| Arquivo local | Origem consultada (URL final) | Data/hora UTC | SHA-256 dos bytes | Resultado |
| --- | --- | --- | --- | --- |
| CVSN_70.html (não versionado; apenas hash) | <https://www.confaz.fazenda.gov.br/legislacao/ajustes/sinief/cvsn_70> | 2026-10-18T00:05:09Z | `7eeb4e45976812c2951d190c158b6d2da77c282e115376f620e2a74c5527d190` | Obtido (HTTP 200, 452.001 bytes) |
| cff-page.html (não versionado; apenas hash) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | 2026-10-18T00:06:13Z | `0c2d7b19379f605e9d9a98b1045984e2da8c2ce7a57ba677e4057b401207cace` | Obtido (HTTP 200, 4.189.528 bytes; página dinâmica com a tabela embutida) |

Fatos conferidos nos textos baixados, usados pelo incremento P2:

- **Convênio s/nº de 15/12/1970, Anexo III-A** (acrescido pelo Ajuste SINIEF
  39/23, efeitos a partir de 01.10.24): CSOSN `103` "Isenção do ICMS no Simples
  Nacional para faixa de receita bruta", `300` "Imune" e `400` "Não tributada
  pelo Simples Nacional". Nota explicativa: CSOSN usado na NF-e exclusivamente
  quando CRT for 1 ou 4.
- **Convênio s/nº de 15/12/1970, Anexo I, Tabela B** (nova redação do Ajuste
  SINIEF 39/23, efeitos a partir de 01.12.23): CST `40` "Isenta — operações e
  prestações isentas" e `41` "Não tributada — operações e prestações imunes ou
  não sujeitas à incidência do ICMS". CST `50` (suspensão) permanece fora do
  recorte P2.
- **Tabela de Classificação Tributária IBS/CBS (CFF)**, gate transversal da
  matriz: não existe correspondência genérica entre isenção/não incidência de
  ICMS e os CST RTC `400` (Isenção — restrito a transporte coletivo, bagagens e
  remessas internacionais, códigos `400001`–`400004`) ou `410` (Imunidade e não
  incidência, códigos `410001`–`410999`). A cesta básica nacional é
  **CST `200`/cClassTrib `200003`** (alíquota reduzida a zero, art. 199 da
  LC 214/2025), grupo com campos próprios ainda **não representado** pelo
  preparador. Conclusão aplicada: o par regular `000`/`000001` permanece o único
  aceito; variantes RTC de alíquota reduzida/isenção/imunidade seguem recusadas
  como **pendência impeditiva** até incremento próprio com fonte e campos do
  leiaute.

Limitações: as páginas consolidam redações e dados dinâmicos; os hashes
identificam os bytes na data da coleta, não certificam vigência, enquadramento
de mercadoria real ou aplicabilidade no DF, que seguem com o responsável
tributário. Nenhum benefício, isenção ou imunidade real foi aprovado.

## Coleta de 17/10/2026 — incremento P3 (redução de base de cálculo)

Recoleta das mesmas duas fontes do P2, com bytes idênticos (mesmos SHA-256),
feita para conferir os fatos específicos do CST 20 e o gate transversal RTC:

| Arquivo local | Origem consultada (URL final) | Data/hora UTC | SHA-256 dos bytes | Resultado |
| --- | --- | --- | --- | --- |
| CVSN_70.html (não versionado; apenas hash) | <https://www.confaz.fazenda.gov.br/legislacao/ajustes/sinief/cvsn_70> | 2026-10-18T00:30:59Z | `7eeb4e45976812c2951d190c158b6d2da77c282e115376f620e2a74c5527d190` | Obtido (HTTP 200, 452.001 bytes; idêntico à coleta P2) |
| cff-page.html (não versionado; apenas hash) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | 2026-10-18T00:31:52Z | `0c2d7b19379f605e9d9a98b1045984e2da8c2ce7a57ba677e4057b401207cace` | Obtido (HTTP 200, 4.189.528 bytes; idêntico à coleta P2) |

Fatos conferidos nos textos baixados, usados pelo incremento P3:

- **Convênio s/nº de 15/12/1970, Anexo I, Tabela B** (nova redação do Ajuste
  SINIEF 39/23, efeitos a partir de 01.12.23): CST `20` "Tributada com redução
  de base de cálculo — classificam-se neste código as operações e prestações
  contempladas com redução de base de cálculo do imposto". A redação verde
  posterior na mesma página ("Tributada com redução de base de cálculo ou
  redução do imposto") é a Tabela B do Ajuste SINIEF 11/19, marcada
  **sem efeitos**, e não foi usada.
- **Tabela de Classificação Tributária IBS/CBS (CFF)**, gate transversal da
  matriz: o CST RTC `222` "Redução de Base de Cálculo" (`IndReducaoBc`) possui
  **uma única classificação**, `222001`, restrita a transporte internacional de
  passageiros com trechos de ida e volta vendidos em conjunto — sem
  correspondência genérica com a redução de base do ICMS no varejo. O CST `200`
  "Alíquota reduzida" lista hipóteses fechadas da LC 214/2025 (anexos/artigos
  específicos), que também não espelham benefício de ICMS. Conclusão aplicada:
  o par regular `000`/`000001` permanece o único aceito pelo preparador; a
  classificação RTC de cada caso real de redução segue com o responsável
  tributário, e nenhum par novo foi habilitado neste incremento.

Limitações: as páginas consolidam redações e dados dinâmicos; os hashes
identificam os bytes na data da coleta, não certificam vigência, percentual de
redução aplicável ou enquadramento de mercadoria real no DF, que seguem com o
responsável tributário (RICMS-DF permanece pendente abaixo). Nenhum benefício
real foi aprovado; o percentual do exemplo sintético é valor de teste.

## Coleta de 18/10/2026 — RTC cesta básica nacional (issue #30)

Recoleta da tabela CFF, com bytes idênticos aos das coletas P2/P3 (a primeira
tentativa, às 01:21:34Z, recebeu HTTP 503 "The service is unavailable." e foi
descartada):

| Arquivo local | Origem consultada (URL final) | Data/hora UTC | SHA-256 dos bytes | Resultado |
| --- | --- | --- | --- | --- |
| cff-page.html (não versionado; apenas hash) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | 2026-10-18T01:22:04Z | `0c2d7b19379f605e9d9a98b1045984e2da8c2ce7a57ba677e4057b401207cace` | Obtido (HTTP 200, 4.189.528 bytes; idêntico às coletas P2/P3) |

Fatos conferidos, usados pela issue
[#30](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/30):

- **Tabela CFF, CST `200`** "Alíquota reduzida": `IndReducaoAliq` verdadeiro e
  `IndReducaoBc` falso — redução de alíquota, não de base.
- **Tabela CFF, `cClassTrib` `200003`** (CST `200`): vendas de produtos
  destinados à alimentação humana do Anexo I da LC 214/2025 que compõem a Cesta
  Básica Nacional de Alimentos; `PercRedIbs` e `PercRedCbs` 100, `IndNfce`
  verdadeiro, `TipoAliq` 2, `NroAnexo` 1, texto regulatório do art. 199 da
  LC 214/2025 (alíquotas de IBS e CBS reduzidas a zero), início de vigência
  2025-05-05 e publicação 2026-06-22.
- **XSD local da baseline** ([DFeTiposBasicos_v1.00.xsd](../../../schemas/nfe/4.00/files/DFeTiposBasicos_v1.00.xsd)):
  tipo `TRed` com a sequência `pRedAliq`, `pAliqEfet` (ambos `TDec_0302_04RTC`);
  em `TCIBS_NFe`, o elemento opcional `gRed` aparece em `gIBSUF`, `gIBSMun` e
  `gCBS`, depois de `gDif`/`gDevTrib` e antes do valor do subgrupo (em `gCBS`,
  antes de `gALCZFMCBS`).

Conclusão aplicada: o preparador aceita o par `200`/`200003` exigindo `gRed`
declarado nos três subgrupos e mantém `000`/`000001` sem `gRed`; demais pares
seguem recusados. Limitações: o registro CFF não comprova enquadramento de
mercadoria real nos NCM do Anexo I, nem aplicação em autorizador; os valores do
exemplo sintético (redução 100, alíquota efetiva 0) são hipóteses de teste.

| Fonte externa | URL de partida | Status |
| --- | --- | --- |
| Convênio ICMS 142/18 + tabela CEST | <https://www.confaz.fazenda.gov.br/legislacao/convenios/2018/CV142_18> | Obtido em 17/10/2026 (P1) |
| Convênio s/nº de 1970 (Tabela B CST e Anexo III-A CSOSN) | <https://www.confaz.fazenda.gov.br/legislacao/ajustes/sinief/cvsn_70> | Obtido em 17/10/2026 (P2; reconferido no P3, tabela acima) |
| Tabela de Classificação Tributária IBS/CBS (CFF) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | Obtido em 17/10/2026 (P2; reconferido no P3 para redução de base e em 18/10/2026 para `200003`) |
| Regulamento do ICMS do DF e anexos ST/benefícios | A identificar (ato vigente) | Pendente |
| Base legal do monofásico PIS/COFINS de bebidas frias | A identificar | Pendente |
| Convênio ICMS 100/97 (insumos agropecuários) | <https://www.confaz.fazenda.gov.br/legislacao/convenios/1997/CV100_97> | Pendente |

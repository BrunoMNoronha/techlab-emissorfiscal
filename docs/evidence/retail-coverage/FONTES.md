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

| Fonte externa | URL de partida | Status |
| --- | --- | --- |
| Convênio ICMS 142/18 + tabela CEST | <https://www.confaz.fazenda.gov.br/legislacao/convenios/2018/CV142_18> | Obtido em 17/10/2026 (P1) |
| Convênio s/nº de 1970 (Tabela B CST e Anexo III-A CSOSN) | <https://www.confaz.fazenda.gov.br/legislacao/ajustes/sinief/cvsn_70> | Obtido em 17/10/2026 (P2, tabela acima) |
| Tabela de Classificação Tributária IBS/CBS (CFF) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | Obtido em 17/10/2026 (P2, tabela acima); antes consultada somente para 000001/CST 000 |
| Regulamento do ICMS do DF e anexos ST/benefícios | A identificar (ato vigente) | Pendente |
| Base legal do monofásico PIS/COFINS de bebidas frias | A identificar | Pendente |
| Convênio ICMS 100/97 (insumos agropecuários) | <https://www.confaz.fazenda.gov.br/legislacao/convenios/1997/CV100_97> | Pendente |

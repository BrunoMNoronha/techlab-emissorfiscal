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

| Fonte externa | URL de partida | Status |
| --- | --- | --- |
| Convênio ICMS 142/18 + tabela CEST | <https://www.confaz.fazenda.gov.br/legislacao/convenios/2018/CV142_18> | Obtido em 17/10/2026 (tabela acima) |
| Regulamento do ICMS do DF e anexos ST/benefícios | A identificar (ato vigente) | Pendente |
| Base legal do monofásico PIS/COFINS de bebidas frias | A identificar | Pendente |
| Convênio ICMS 100/97 (insumos agropecuários) | <https://www.confaz.fazenda.gov.br/legislacao/convenios/1997/CV100_97> | Pendente |
| Tabela de Classificação Tributária IBS/CBS (CFF) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | Pendente (consultada antes somente para 000001/CST 000) |

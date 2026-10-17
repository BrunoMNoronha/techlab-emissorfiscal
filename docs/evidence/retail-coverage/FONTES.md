# Fontes — matriz de cobertura tributária do varejo

Criado em 17/10/2026. Nenhuma fonte externa foi baixada para a
[matriz de cobertura](../../RETAIL-COVERAGE-MATRIX.md); a entrega é de
priorização, com fatos estruturais conferidos somente em material local:

| Material local | Uso |
| --- | --- |
| [leiauteNFe_v4.00.xsd](../../../schemas/nfe/4.00/files/leiauteNFe_v4.00.xsd) da baseline `nfe-4.00-offline-2026-10-04` | Grupos `ICMSSN102` (CSOSN 102/103/300/400), `ICMSSN500`, `ICMS00`, `ICMS20`, `ICMS40` (CST 40/41/50), `ICMS60`, `ICMS90`, `ICMSSN900` e seus campos/sequências |
| [PreparationModels.cs](../../../src/EmissorFiscal.Core/Preparation/PreparationModels.cs), [PreparationJson.cs](../../../src/EmissorFiscal.Core/Preparation/PreparationJson.cs), [NfcePreparation.cs](../../../src/EmissorFiscal.Core/Preparation/NfcePreparation.cs) | Estado do preparador: whitelist de grupos, CEST/cBenef, totais ST |

Quando a coleta das fontes externas da seção 5 da matriz ocorrer, registrar aqui
cada download com URL final, data/hora UTC, SHA-256 dos bytes e limitações,
no mesmo padrão de [ef05/FONTES.md](../ef05/FONTES.md). Acesso a catálogo ou
texto legal não comprova ativação de regra em autorizador.

| Fonte externa | URL de partida | Status |
| --- | --- | --- |
| Convênio ICMS 142/18 + tabela CEST | <https://www.confaz.fazenda.gov.br/legislacao/convenios/2018/CV142_18> | Pendente |
| Regulamento do ICMS do DF e anexos ST/benefícios | A identificar (ato vigente) | Pendente |
| Base legal do monofásico PIS/COFINS de bebidas frias | A identificar | Pendente |
| Convênio ICMS 100/97 (insumos agropecuários) | <https://www.confaz.fazenda.gov.br/legislacao/convenios/1997/CV100_97> | Pendente |
| Tabela de Classificação Tributária IBS/CBS (CFF) | <https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria> | Pendente (consultada antes somente para 000001/CST 000) |

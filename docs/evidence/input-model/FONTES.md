# Fontes oficiais do modelo de dados NFC-e

Revisão em **05/10/2026**, horário do cliente (America/Sao_Paulo). Os acessos UTC, métodos, hashes e recortes estão em [sources.json](sources.json). Levantamento público para **modelo 65, DF, homologação, emissão normal e venda presencial/retirada**. Sem transmissão, credenciais, cadastro real ou aprovação tributária.

## Dados e limites verificados

| Tema | Fato sustentado | Fonte / consequência para o modelo |
| --- | --- | --- |
| Emitente | CNPJ, razão social, endereço fiscal, IE e CRT provêm do estabelecimento real. Homologação não dispensa credenciamento DF. | Portaria DF 387/2019, arts. 2º–3º; NT 2026.007. Números fictícios servem apenas ao teste offline. |
| Homologação | A constante do primeiro `det/prod/xProd` é `NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL`. Quando informado, `dest/xNome` usa `NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL`. | MOC E04-20/I04-10. Não substituir a razão social do emitente. |
| Identificação | O grupo destinatário admite CPF, CNPJ ou identificação estrangeira. Identificação é exigida a partir de R$ 10.000, quando solicitada abaixo disso e nas operações não presenciais; UF pode reduzir limites. | SINIEF 19/16, cláusula quarta. Não generalizar a dispensa do grupo no XSD para toda operação. |
| Destinatário 65 | `indIEDest=9` é a codificação prevista pela RV E16a-10; XSD genérico também aceita 1/2. | MOC, rejeição 789. Essa codificação não decide por si a condição legal do comprador. |
| CNPJ comprador | SINIEF 12/26 revogou 11/25. Operações varejistas que permitem crédito de ICMS ao adquirente exigem modelo 55, com efeitos de 23/26 em 05/10/2026. | Sem proibição genérica baseada apenas em CNPJ; admissibilidade do caso concreto continua necessária. Condicionamentos específicos DF não foram integralmente confirmados. |
| CRT | CRT 1 representa Simples Nacional; CRT 3, regime normal. | Convênio SINIEF, Anexo III. Não implica automaticamente CSOSN 102, CST 00 ou alíquota de ICMS. |
| Endereços | Endereço do emitente é estruturalmente obrigatório, incluindo CEP; endereço do destinatário é grupo opcional no leiaute genérico, com campos obrigatórios quando presente. | XSD PL010f fixado. Cadastro real e regras da operação prevalecem sobre exemplos. |
| Pagamento | A tabela atual distingue dinheiro 01, PIX dinâmico 17, estático 20 e automático 23. | IT 2024.002 v1.11 / planilha 04/03/2026. Existência na tabela não prova admissão de toda modalidade em NFC-e; exemplo inicial em dinheiro. |
| Quantidades/valores | XML usa tipos decimais e limites próprios para quantidade, preço, valor e percentual. | XSD fixado. Modelo deve preservar precisão decimal e validar cálculos; não usar `number` como prova de exatidão fiscal. |
| Cadastro/IE | Dispensa de IE da NT 2026.007 atende modelo 55; a regra C17-42 impede o modelo 65 sem IE antes de 2033. | Cronograma homologação até 05/10/2026, produção 03/11/2026; anúncio não confirma implantação DF. A exceção de cadastro em homologação depende do indicador oficial de contribuinte de teste. |
| Leiaute posterior | NT 2026.008 cria valores líquidos ausentes do PL010f local. | Cronograma inicial homologação 05/10/2026 e produção 03/11/2026; algumas RVs são futuras. XSD local válido não comprova cobertura da atualização. |

## RTC no exemplo CRT 3 de 2026

NT 2025.002 v1.52, UB12-10: homologação CRT 3 desde **01/07/2026**. Na página 40, os antigos textos de produção 03/08/2026 e CRT 1/2/4 em 04/01/2027 estão **riscados**; a indicação vigente é implementação futura para produção. A extração textual isolada mistura versões. Página 40 foi conferida visualmente.

A classificação oficial CFF confirma **CST 000 / cClassTrib 000001**, utilização em NFC-e e tributação integral, sem redução, diferimento, monofasia, crédito presumido ou grupo de tributação regular. Isso sustenta uma **hipótese sintética**, não o enquadramento de mercadoria real.

UB18-10/UB37-10/UB56-10: percentuais de 2026 **IBS UF 0,1%; IBS município 0%; CBS 0,9%**. UB16-10 descreve a composição da base e declara sua validação futura, aguardando orientação normativa. Bases e tributos reais continuam sendo fornecidos pelo cliente e validados pelo responsável fiscal.

Hipótese numérica declarada: produto R$ 100,00; ICMS 00 com base R$ 100,00, 18%, valor R$ 18,00; PIS 01 com base R$ 82,00, 1,65%, valor R$ 1,35; COFINS 01 com base R$ 82,00, 7,6%, valor R$ 6,23. Esses códigos e percentuais ICMS/PIS/COFINS não são padrões reais aprovados.

| Grupo | Valores sintéticos |
| --- | --- |
| `IBSCBS` | `CST=000`, `cClassTrib=000001`, `gIBSCBS/vBC=74.42` |
| `gIBSUF` | `pIBSUF=0.1000`, `vIBSUF=0.07` |
| `gIBSMun` | `pIBSMun=0.0000`, `vIBSMun=0.00` |
| `gIBSCBS` | `vIBS=0.07` |
| `gCBS` | `pCBS=0.9000`, `vCBS=0.67` |
| `IBSCBSTot` | `vBCIBSCBS=74.42`; totais IBS UF 0.07, município 0.00, IBS 0.07 e CBS 0.67; campos estruturais de diferimento, devolução, crédito presumido e condição suspensiva em 0.00 |
| Item, nota e pagamento | `vItem=100.00`, `vNF=100.00`, `vNFTot=100.00`; dinheiro `tPag=01`, `vPag=100.00` |

VB01-10 excepciona 2025/2026 da soma de IBS/CBS/IS ao total do item; página 68 conferida visualmente. W34-20 exige totalização quando há IBS/CBS nos itens. RVs de obrigatoriedade/cálculo de `vItem` e `vNFTot` constam como futuras. Incluí-los no exemplo completo demonstra estrutura e consistência local.

## Referências primárias

- [MOC 7.00 — Anexo I](https://www.confaz.fazenda.gov.br/legislacao/arquivo-manuais/moc7-anexo-i-leiaute-e-rv.pdf).
- [NT 2025.002 RTC v1.52](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=3&nomeArquivo=NT_2025.002_v1.52_RTC_NF-e_IBS_CBS_IS.pdf) e [Classificação Tributária CFF](https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria).
- [NT 2026.007 v1.10](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=3&nomeArquivo=NT2026.007_v1.10%20-%20Emiss%C3%A3o%20NF-e%20sem%20IE%20e%20RV%20LCC.pdf) e [NT 2026.008 v1.00](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=3&nomeArquivo=NT_2026.008_v1.00_RTC_ValorLiquidoProduto.pdf).
- [IT 2024.002 v1.11](https://dfe-portal.svrs.rs.gov.br/DFE/DownloadArquivoEstatico/?sistema=DFE&tipoArquivo=16&nomeArquivo=IT2024.002v1.11%20-%20Atualiza%20Tabela%20Meios%20de%20Pagamento%20-%2004032026.pdf) e [tabela de pagamentos](https://dfe-portal.svrs.rs.gov.br/DFE/DownloadArquivoEstatico/?sistema=DFE&tipoArquivo=12&nomeArquivo=Tabela_de_meios_de_pagamento%20-%2004032026.xlsx).
- [CRT — Anexo III do Convênio SINIEF](https://www.confaz.fazenda.gov.br/legislacao/ajustes/sinief/cvsn_70).
- [SINIEF 19/16](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2016/AJ_019_16), [12/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ012_26) e [23/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ023_26).
- [Portaria DF 387/2019 — SINJ](https://www.sinj.df.gov.br/sinj/Norma/3efbac69483f4b74b65ffd53ba901b0a/Portaria_387_20_12_2019.html), com consulta pública alternativa de leitura registrada no JSON.
- [Manifesto do XSD fixado](../../../schemas/nfe/4.00/manifest.json).

Nenhum exemplo aprova emissão real. Faltam empresa e cadastros conferidos, regime real, casos tributários aprovados, admissibilidade da série, autorização operacional e revisão do leiaute/implantação vigente. As evidências históricas EF05/EF06 foram preservadas.

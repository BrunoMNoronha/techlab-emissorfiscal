# EF-06 v0.2 — reconferência dirigida de fontes

Data do cliente: **05/10/2026**, America/Sao_Paulo. Coletas em 06/10/2026 UTC correspondem a 05/10/2026 no fuso do cliente. Evidência estruturada: [source-review-v0.2.json](source-review-v0.2.json).

Esta revisão confirma fontes públicas para o contrato documental. Não escolhe enquadramento tributário, não habilita variantes e não demonstra implantação do autorizador. O recorte inicial é contratação presencial com retirada; demais variantes exigem revisão própria antes da habilitação.

## Fontes e alcance

| Fonte primária | Dispositivos / efeito conferido | Consequência delimitada |
| --- | --- | --- |
| [SINIEF 19/16 consolidado](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2016/AJ_019_16) | Cláusula quarta, VII(a–c), §§4º–5º e §8º; cláusula quinta, §3º, I. Alínea VII(c) pelo 9/26, efeitos em 03/08/2026 | Identificação nas hipóteses normativas e endereço em contratação não presencial. Limites nacionais sujeitos a redução estadual. Autorização formal não convalida tributação. |
| [SINIEF 12/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ012_26) | Cláusulas primeira e segunda; publicado e vigente em 09/04/2026 | Revoga 11/25. CNPJ isolado não sustenta aquela proibição geral antiga, nem garante elegibilidade atual. |
| [SINIEF 23/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ023_26) | Cláusula primeira e §§2º–3º; cláusula segunda. Publicação 09/07/2026; efeitos 05/10/2026 | Operação varejista que permita crédito ICMS ao adquirente exige NF-e 55. Condições específicas do DF ainda não confirmadas. |
| [Portaria DF 387/2019 — serviço público Receita DF](https://ww1.receita.fazenda.df.gov.br/WPI/api/legislacao/obter-legislacao) | Art. 1º e §2º, I; arts. 2º–3º; art. 5º, §§1º–2º; art. 11. Texto compilado inclui 998/2025 | Varejo ao consumidor final; 65 não substitui 55 obrigatório. Credenciamento real necessário. Em entrega, DANFE acompanha trânsito em papel ou apresentável em equipamento eletrônico; substituição da impressão tem concordância/condições. |
| [Convênio s/nº de 1970 consolidado](https://www.confaz.fazenda.gov.br/legislacao/ajustes/sinief/cvsn_70) | Art. 5º-A; Anexos III e III-A | CRT 1/2/3/4 representam situações distintas. Planejamento Simples/normal não determina CRT real nem aprova MEI/sublimite. |
| [MOC 7.0 — Anexo I](https://www.confaz.fazenda.gov.br/legislacao/arquivo-manuais/moc7-anexo-i-leiaute-e-rv.pdf) | Capa 7.00, nov/2020; controle 7.03, out/2020. Campos B25a/B25b/C21 e RVs B25b-20, E05-20 e X03-20 | Distingue consumidor final, presença e CRT. Documento histórico deve ser cruzado com NTs posteriores; não prova todas as regras atuais ou implantação. |

O serviço DF foi consultado por **POST de busca pública**, com `Ano=2019`, `Numero=387`, `CodigoTipo=7`, `Parte=.` e `Setor=null`; não é emissão, alteração cadastral ou acesso restrito. Payload integral, horário UTC, status HTTP, tamanho e SHA-256 constam no JSON. O [SINJ](https://www.sinj.df.gov.br/sinj/Norma/3efbac69483f4b74b65ffd53ba901b0a/Portaria_387_20_12_2019.html) apresentou timeout nas reconferências posteriores.

## Histórico e limites

- [source-status.json](source-status.json) permanece intacto: documenta a falha histórica de acesso ao 23/26. Esta revisão acrescenta GET bem-sucedido, sem reclassificar o passado.
- Todos os seis corpos desta rodada foram obtidos com HTTP 200 e hashes calculados dos bytes reais. Hash identifica resposta recebida; não comprova vigência, implantação técnica, credenciamento ou aprovação fiscal.
- Os excertos curtos ficam apenas no JSON. Não foram salvos corpos integrais, usados certificados ou contatados serviços de autorização.
- Não confirmar redução de limites ou condições de crédito neste recorte não significa provar ausência de outras normas DF.
- Consumo próprio, identificação, condição de contribuinte, crédito, presença e recebimento não devem ser deduzidos uns dos outros.
- Regime efetivo, códigos/valores tributários, benefícios e elegibilidade por operação dependem de fatos do emitente e validação fiscal; a decisão de fornecer tributação pelo cliente vem do contrato do produto.
- Série exclusiva exige admissibilidade e inventário reais. Esta pesquisa não escolhe série nem resolve numeração legada.
- O MOC consultado contém B25b-20 com `indPres` 1/4 para modelo 65 e regras de endereço/transportador em entrega. A regra de endereço do SINIEF 9/26 não habilita, isoladamente, compra remota com retirada ou entrega; compatibilidade atual com NTs posteriores permanece pendente.

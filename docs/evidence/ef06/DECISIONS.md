# EF-06 — origem e alcance das decisões

Data: 05/10/2026. Fonte das decisões: respostas do usuário neste chat. Fontes normativas em [PILOT-CONTRACT.md](../../PILOT-CONTRACT.md). Não há emitente, aprovador ou política tributária inventados.

| ID | Fonte e evidência | Alcance / limites |
| --- | --- | --- |
| H01 | PROJECT.md/briefing: respostas de Bruno em 04/10 | API multiempresa, DF primeiro, NFC-e antes de NF-e, tributação do cliente e A1. Sem infraestrutura ou uso real de credenciais definidos |
| H02 | 05/10: “Sistemas de vendas para usuários finais” | Perfil dos clientes; não presume venda ou regime |
| H03 | 05/10: “NFCe basicamente vai ser pra mercadinhos distribuidoreas, etc” | Segmentos iniciais; não elegibilidade universal |
| H04 | 05/10: “concordo”, em resposta a planejar ambos regimes e começar pelo regime do primeiro piloto | Simples e normal planejados. Primeiro CRT real, MEI/sublimite não definidos |
| H05 | 05/10: “vamos simular esses dados. você irá criar um agente com essa personaliade e ele será nosso PO” | Simulação e Rafael como PO; não contador ou autoridade real |
| H06 | 05/10: usuário exige analisar, pesquisar e refletir sem inventar informações | Invalida escolhas não fundamentadas do primeiro rascunho |
| H07 | 05/10: “para consumo proprio” | Finalidade da compra; não determina CPF/CNPJ/contribuinte/crédito/presença/entrega |
| R01 | 05/10: usuário pede agente para responder retirada/entrega; PO recomenda ambos, retirada primeiro | Recomendação para simulação, não operação real confirmada |

**Estado histórico da versão 0.1:** PILOTO-A/B são parâmetros para explorar regimes, não empresas. As políticas então propostas não representavam aprovação humana real. O rascunho inicial invalidado não integra a versão vigente; a revisão do PO não encerra #3.

## Decisões humanas da preparação 0.2 — 05/10/2026

Fonte: escolhas diretas do usuário no planejamento deste chat e pedido posterior “PLEASE IMPLEMENT THIS PLAN”, com o plano completo reproduzido. A aprovação é de produto/preparação documental; não declara fatos cadastrais, autoridade tributária ou liberação de transporte.

| ID | Resposta / autorização direta | Alcance / limite |
| --- | --- | --- |
| H08 | Escolheu “#3 — Contrato do piloto” e “Piloto real em homologação” | Preparar condições de piloto real; implementação/transmissão nas etapas dependentes |
| H09 | Escolheu “Definir antes do piloto” para empresa/regime conhecido/responsável tributário | Empresa e autoridades ainda não identificadas; ficha obrigatória e bloqueios sem nomes inventados |
| H10 | Escolheu “Presencial com retirada” | Primeiro piloto presencial/retirada/consumo próprio; entrega/remoto posteriores. Substitui R01 como alcance inicial aprovado |
| H11 | Escolheu “Série exclusiva” | Buscar série admissível após histórico/coordenação; não presume série/número inicial livres |
| H12 | Escolheu “Cada um vê as próprias” | Conteúdo isolado por sistema cliente, mesmo emitente autorizado; aliases compartilham identidade/pool sem ampliar visibilidade |
| H13 | Escolheu “Adiar correção” | Preservar rejeição/histórico; correção/reutilização de número indisponíveis até política fiscal futura |
| H14 | Escolheu “Emissão, consulta e DANFE” | XML/protocolo e QR no aceite; cancelamento/inutilização/contingência posteriores; DANFE futuro depende EF-16 |
| H15 | Escolheu “Operador central do serviço” | Concede/revoga por autorização documentada do emitente, ação/ambiente; pessoa/legitimidade/delegação ainda TBD |
| H16 | Escolheu “Regime da empresa escolhida” e autorizou implementar o plano documental | CRT comprovado define primeiro caso; Simples/normal planejados separadamente. Contrato 0.2 não altera API/DTO/banco/transporte; #3 permanece parcial |

Decisões de política de acesso/série/adiamentos acima estão aprovadas para o desenho. Cadastro real, empresa/CRT, pessoas/delegações, série/histórico, payloads/regras tributárias e condições de uso de A1 continuam `TBD`, com impactos na [ficha](../../PILOT-REGISTRATION.md). A simulação v1 não preenche esses fatos. Não transformar a aprovação deste plano em autorização de homologação/transmissão ou produção.

## Decisão técnica de preparação

T01 — A seleção EF-06 utiliza registro privado já autorizado, sob guarda do emitente e com acesso aprovado aos revisores; a ficha registra meio/guarda/referência sem conteúdo privado. Isso evita depender da arquitetura EF-07 para reunir os fatos que a precedem. Não escolhe/provisiona armazenamento operacional nem custódia A1; essas decisões continuam na EF-07. Fonte: revisão documental do contrato 0.2 em 05/10/2026, dentro do plano autorizado.

## Novo incremento — modelo técnico e dados de teste

H17 — Após reafirmar que o objetivo é emitir notas fiscais, o usuário pediu levantamento dos dados necessários, pesquisa e estruturação do modelo com dados de teste. Autoriza o modelo executável local `nfce-preparation/0.1`, exemplos completos e prova JSON → XML/assinatura/XSD, em incremento posterior à entrega documental 0.2. A empresa ainda não foi indicada: exemplos identificados como sintéticos não preenchem a ficha nem substituem cadastro, responsável tributário ou A1 reais. Fontes, capacidade e limites em [NFCE-INPUT-MODEL.md](../../NFCE-INPUT-MODEL.md). Nenhuma autorização de transmissão ou produção é inferida desse pedido.

## Decisões de 17/10/2026 — varejo amplo e trilha NFS-e

| ID | Fonte e evidência | Alcance / limites |
| --- | --- | --- |
| H18 | Decisão de atender varejo amplo organizada na [matriz de cobertura](../../RETAIL-COVERAGE-MATRIX.md) (17/10/2026) e entrega do incremento P1 pela issue [#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24), commit `94fb267`, 152/152 testes no CI | Prioriza P1–P4 por variante tributária; P1 (ST retido `ICMSSN500`/`ICMS60` + CEST condicional) implementado com hipóteses sintéticas. Não enquadra mercadoria real, não aprova caso tributário, não preenche a ficha; P2–P4 exigem issue, matriz de validação e fontes próprias |
| H19 | Resposta de Bruno neste chat, 17/10/2026: os documentos NFS-e ([TISS](../../TISS-NFSE-FIELD-MAP.md), [fisioterapia](../../FISIO-NFSE-REQUIREMENTS.md)) refletem interessado real a avaliar; formalizar como trilha exploratória paralela | NFS-e saúde acompanhada como exploração, fora do MVP NFC-e, sem issue, épico EF ou compromisso de implementação. Identidade do interessado permanece em registro privado (T01), fora do Git; confirmação formal, contador/responsável tributário, ambiente DF/Anexo I, granularidade e volume seguem `TBD` |
| H20 | Decisão técnica (não de negócio) do incremento 3 da sequência de núcleo aprovada por Bruno em 18/10/2026: DANFE NFC-e offline da EF-16 ([#13](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/13), PR [#38](https://github.com/BrunoMNoronha/techlab-emissorfiscal/pull/38)) | Saída mínima = texto monoespaçado de 80 mm (48 colunas) + QR Code em SVG, gerados por código próprio (ISO/IEC 18004, byte/M) a partir de um `nfeProc` autorizado, sem NuGet novo. PDF, impressora térmica (ESC/POS), DANFE simplificado tipo 2 e contingência ficam para quando houver cliente/hardware definidos; a decisão é reversível e não escolhe fornecedor, dispositivo ou leiaute final. Não é aceite da EF-16: falta homologação e documento autorizado real da EF-15 |

# EF-06 — preparação do piloto real de NFC-e no DF

Contrato **documental** `nfce-df-homologacao/0.2`, 05/10/2026 (America/Sao_Paulo). Base de código: `952c74c5adfd67f79f7cb746caefb4bb60b95888`. Preparação inicial no checkout com integração simulada local; entrega documental isolada em `codex/ef-06-piloto-real-docs`, preservando aquele trabalho. Incorpora as decisões humanas e o plano aprovado neste chat; não publica um DTO, não implementa transporte e não inicia homologação. **Issue #3 parcial e aberta.**

O contrato executável `SaleRequest` v1 da integração exclusivamente simulada existente no checkout de origem pertence a uma entrega separada. Seu CRT 1, códigos da fixture, limites de teste e configuração local não preenchem o contrato fiscal nem a ficha de empresa real. A versão conceitual 0.1 e sua [revisão do PO](evidence/ef06/PO-REVIEW.md) são históricas; Rafael permanece agente de produto, sem autoridade tributária ou operacional real.

## 1. Decisões vigentes e recorte

Origem: [registro de decisões H01–H16](evidence/ef06/DECISIONS.md). Decisões de produto não comprovam fatos cadastrais, adequação tributária, disponibilidade de série ou autorização de uso de credenciais.

| Tema | Decisão humana vigente | Fato necessário / limite |
| --- | --- | --- |
| Modelo e finalidade | API multiempresa; NFC-e 65 primeiro, DF/cUF 53, homologação/tpAmb 2, emissão normal/tpEmis 1; varejo presencial com retirada para consumo próprio | Elegibilidade, emitente e estabelecimento comprovados. Consumo próprio não define crédito, CPF/CNPJ ou condição de contribuinte |
| Regime | Simples e normal planejados separadamente; primeiro recorte segue o CRT comprovado da empresa selecionada | Empresa/CRT/grupos aprovados: `TBD`. Não assumir CRT 1 da fixture nem incluir MEI/sublimite implicitamente |
| Empresa e responsáveis | Selecionar e documentar antes do piloto | Empresa, sistema cliente, responsável tributário e operador central efetivos: `TBD`, sem autoridades fictícias |
| Entrega do piloto | Emissão, consulta, XML assinado/protocolo correlacionado, XML processado e DANFE/QR | Capacidades futuras; string de QR e processado sintético não provam esse aceite |
| Série | Exclusiva para o serviço no ambiente do piloto, após verificar admissibilidade, histórico e coordenação com outros emissores | Série/número inicial/evidências: `TBD`; exclusividade não demonstra disponibilidade |
| Visibilidade | Cada sistema acessa suas próprias operações; aliases usam a mesma identidade fiscal e pool | Clientes autorizados ao mesmo emitente não acessam automaticamente conteúdo um do outro. Cadastro/concessões reais auditáveis |
| Delegação | Operador central concede/revoga por ação, emitente e ambiente mediante autorização documentada do emitente | Operador, autorizante, escopo e documento: `TBD`; administração não deriva do body |
| Tributação | Sistema cliente fornece; emissor valida e preserva estrutura, consistência e regras suportadas | Casos com responsável/fundamento; não escolher CFOP/CST/CSOSN/NCM/cBenef/alíquotas por heurística |
| Adiados | Correção, cancelamento, inutilização, contingência, contratação remota e entrega | Indisponíveis inicialmente. Não criar revisão corretiva, reciclar número ou habilitar tpEmis 9; evolução exige novo recorte/aceite |
| Fora do recorte | Modelo 55, outras UFs e produção | Recusar sem converter modelo/ambiente automaticamente |

## 2. Elegibilidade e fontes oficiais

Registrar separadamente finalidade, destinatário, condição de contribuinte, direito a crédito, presença e recebimento. `Não determinado` não equivale a `não`: se a ausência impedir decidir elegibilidade ou obrigatoriedade de dados, bloquear o caso antes de preparar transmissão. CPF/CNPJ isolado não resolve o modelo.

| Fonte | Regra a confrontar com o caso | Limite |
| --- | --- | --- |
| [SINIEF 19/16](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2016/AJ_019_16), quarta VII/§§4º–5º e quinta §3º I | Identificação a partir de R$ 10 mil e abaixo quando solicitada; contratação não presencial exige identificação/endereço; modelo 55 a partir de R$ 200 mil, sujeitos a limites estaduais; autorização não convalida tributação | Limites nacionais não são configuração definitiva do DF; verificar dispositivos/UF por caso |
| [SINIEF 12/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ012_26) | Revogou 11/25; sua antiga exclusão geral por CNPJ não é regra vigente | Não autoriza toda venda a pessoa jurídica no modelo 65 |
| [SINIEF 23/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ023_26), cláusulas primeira/segunda | Operação varejista que permita crédito ICMS do adquirente exige modelo 55; efeitos anunciados em 05/10/2026 | Condições DF e classificação do caso por verificar; crédito não recebe booleano padrão |
| [Portaria DF 387/2019](https://www.sinj.df.gov.br/sinj/Norma/3efbac69483f4b74b65ffd53ba901b0a/Portaria_387_20_12_2019.html), arts. 1º/5º | Varejo/consumidor final, sem substituir modelo 55 obrigatório; condições de apresentação do DANFE | Retirada não dispensa DANFE; entrega futura deve observar representação em trânsito e permanece adiada |
| [Matriz EF-05](EF05-NORMATIVE-MATRIX.md) e [manifest fixado](../schemas/nfe/4.00/manifest.json) | Leiaute, dependências, precisão e regras/grupos; consulta com alertas incompatível com a baseline | XSD válido não comprova enquadramento, ativação de regra ou origem SEFAZ |

Pesquisa desta versão: [FONTES-v0.2](evidence/ef06/FONTES-v0.2.md) e [registro de acessos](evidence/ef06/source-review-v0.2.json). A [tentativa anterior](evidence/ef06/source-status.json) permanece histórica. Distinguir publicação, efeitos legais, cronograma técnico e ativação comprovada; falha de acesso não prova inexistência de norma.

## 3. Contrato de dados e políticas

O [dicionário documental](PILOT-DATA-DICTIONARY.md) define obrigatoriedade, origem, leiaute, precisão, totais, versionamento, idempotência e erros. Não altera rotas/serialização/DTOs da simulação. Cliente não fornece schema confiável, protocolo autorizador, chave privada ou identidade autenticada no body.

| Comportamento | Regra para as etapas dependentes |
| --- | --- |
| Acesso | Negação padrão, vínculo cliente autenticado/emitente/ação/ambiente; consulta/download exigem propriedade. Operador administra concessões dentro da delegação, sem acesso automático ao conteúdo fiscal |
| Aliases/pool | Identidade canônica do estabelecimento; sem pool por alias/cliente. Unicidade por identidade/modelo/ambiente/série/número; autorização e propriedade independentes |
| Idempotência | Referência estável no namespace cliente/identidade fiscal/ambiente. Mesma referência/conteúdo retorna operação e bytes existentes; conteúdo diferente conflita. Canonicalização/versionamento do hash definidos antes do contrato executável fiscal |
| Numeração | Reserva transacional futura/série exclusiva comprovada; sem número livre do cliente, série presumida ou contador deduzido apenas do maior número observado |
| Rejeição | Preservar XML/tentativas/motivo e acesso autorizado aos artefatos. Correção/reutilização indisponíveis; outra referência não é atalho para corrigir a mesma venda |
| Incerteza | Timeout/crash/retorno inválido ou desconhecido não são rejeição definitiva. Mesma revisão, número, chave, data, bytes e baseline; reconciliação sem reserva/regeneração/retransmissão automática. 217 isolado não libera envio |
| Resultado | Fase local e estado fiscal separados; recebimento não autoriza. Transporte confiável deve correlacionar tentativa/chave/ambiente/digest; nfeProc apenas com correlação válida |
| Auditoria | Registrar concessões/revogações/aliases/snapshots/tentativas/hashes, sem sobrescrever história. Custódia/retenção/backup reais: EF-07, ainda `TBD` |

## 4. Ficha e casos obrigatórios

Preencher a [ficha do piloto](PILOT-REGISTRATION.md), com evidências reais e aprovação documentada. Na seleção EF-06, usar registro privado já autorizado pelo emitente, sob sua guarda, com acesso dos revisores autorizado e referências publicáveis sem conteúdo privado. Isso não depende de implementar armazenamento do serviço: a custódia operacional fiscal será decidida na EF-07. Não colocar PFX, senhas, tokens, dados de compradores ou documentos privados no Git/chat; o arquivo versionado é um modelo sem dados reais.

Cada caso precisa do regime comprovado, finalidade, destinatário/crédito, itens/grupos/totais/pagamentos, fontes e responsável pela validação. Exemplos publicados serão sintéticos e vinculados ao caso revisado; códigos da fixture não demonstram adequação da operação da empresa. Dado/grupo sem suporte comprovado será recusado.

## 5. Cenários C01–C13 e evidência

Os treze cenários são **documentais**; nenhum foi executado em homologação nesta entrega. `Previsto` é aceite futuro, `adiado` é capacidade indisponível com recusa esperada, `regressão existente` é prova offline restrita. O [registro de validação](VALIDATION.md) e a simulação local separada não são execuções destes casos reais.

| ID | Cenário / resultado esperado | Estado e dependência |
| --- | --- | --- |
| C01 | Caso real validado, CRT comprovado, presencial/retirada/consumo próprio; preservar tributação, obter emissão/consulta/XML/protocolo/DANFE/QR coerentes | Previsto; ficha/casos, EF-05/07–15/16. Simples/normal separados |
| C02 | Remoto, mesmo com retirada, ou entrega: recusar no primeiro piloto. Manter identificação/endereço/DANFE aplicáveis na ampliação | Positivos adiados; recusa prevista. Presença/recebimento independentes |
| C03 | Caso que exige 55, inclusive crédito aplicável comprovado: recusar fluxo 65; crédito/contribuinte não determinados bloqueiam classificação quando indispensáveis | Previsto; caso validado/regras DF |
| C04 | A sem concessão tenta B, consulta, XML/protocolo/DANFE ou credencial: negar em todas as etapas/erros | Previsto; EF-08/11/12/13; evidência do simulador distinta |
| C05 | Aliases/clientes autorizados ao mesmo emitente: identidade/pool únicos sem colisão; conteúdo de outro cliente negado | Previsto; EF-08/09/10, delegação/aliases reais |
| C06 | Mesma referência/conteúdo recupera snapshot/bytes sem novo número/envio; diferente conteúdo conflita | Previsto; hash/contrato fiscal executável e EF-10/11 |
| C07 | Série com histórico/número ocupado: bloquear configuração/reserva incompatível e comprovar exclusividade admissível/coordenação | Previsto; inventário e EF-10; não inferir disponibilidade |
| C08 | Rejeição definitiva e pedido de correção: preservar histórico/motivo/artefatos e recusar correção/reutilização de número | Correção adiada; recusa prevista; revisão fiscal depende de política futura |
| C09 | Timeout/crash/resposta tardia ou 217 isolado: incerteza, mesma revisão/XML, reconciliação sem nova emissão automática | Previsto; EF-12/14/15. Fencing não cancela I/O em voo |
| C10 | Consulta com cMsg/xMsg incompatível: falhar de forma segura, sem autorização/processado por fallback | Regressão offline existente `AlertExtensionMissingInPinnedConsultationSchemaFailsClosed`; EF-05 bloqueada |
| C11 | 55/outra UF/produção/tpEmis 9/eventos/inutilização: recusar neste fluxo | Fronteira 65/53/2/1 tem regressões offline; demais recusas futuras. Capacidades adiadas/fora do recorte |
| C12 | Concessão revogada antes da tarefa/resposta tardia obsoleta: revalidar acesso, preservar evidência, negar transição obsoleta | Previsto; EF-08/12/14, operador/delegação documentados |
| C13 | CRT/grupo/código/precisão/total incompatível com caso: erro local, sem substituir, arredondar ou calcular tributo silenciosamente | Previsto; dicionário/caso/EF-05/11; fixture não define regra real |

## 6. Marcos e encerramento

| Marco | Condição verificável | Estado desta versão |
| --- | --- | --- |
| Preparação documental 0.2 | Decisões com fonte, ficha, dicionário, cenários/dependências e pesquisa sincronizados | Entrega documental; validação separada |
| EF-06 / issue #3 | Ficha sem `TBD` indispensável; empresa/CRT/sistema/autoridades/delegação/série/histórico comprovados; casos/regras validados e contrato suficiente às etapas dependentes | **Parcial, aberta**. Publicar 0.2 ou selecionar empresa isoladamente não conclui |
| Arquitetura | EF-07 decide autenticação, banco, worker, armazenamento, retenção/recuperação/custódia após pré-requisitos EF-06 | Não concluída por escolhas do simulador |
| Piloto homologado | EF-05 e base EF-08–12, A1/emitente EF-13, transporte EF-14, provas reais EF-15 **e DANFE/QR EF-16**, ficha/autoridades válidas | Bloqueado; nenhuma transmissão nesta entrega |
| Evoluções | Correção, eventos/contingência EF-17–19, novo regime/operação ou entrega/remoto exigem casos/contrato/aceites próprios | Adiados; produção EF-20 e expansão/modelo 55 EF-21 fora do piloto |

Preservar limitação EF-05 de consulta com alertas. Plano, fixture, verde de CI e autorização sintética não encerram dependência. Homologação dos casos e prontidão de produção são marcos distintos.

### Aceite da issue #3 nesta entrega

| Critério da issue | Evidência / estado |
| --- | --- |
| 1. Decisões indispensáveis com fonte e autoridade; lacunas não essenciais em `TBD` | H01–H16 e ficha registram decisões e lacunas. **Parcial:** empresa/CRT, autoridades/delegação, série/histórico e casos indispensáveis não foram comprovados |
| 2. Entrada/saída, obrigatoriedade, versão, tributação preservada e variantes recusadas | Dicionário 0.2 e C03/C11/C13 especificados documentalmente; grupos/regras dos casos reais e contrato executável fiscal continuam dependentes |
| 3. Cenários de acesso A/B, aliases, legado/série e correção com história preservada | C04–C09/C12 e políticas documentados; execução real prevista. Correção indisponível e recusa prevista no primeiro recorte |
| 4. Fronteira 65/DF explícita e exclusões | Seções 1–3, C02/C03/C11 e roadmap registram recorte e capacidades adiadas |
| 5. Não encerrar por recomendação ou publicação documental | **Issue aberta/parcial.** Seleção, provas e validação tributária indispensáveis permanecem requisitos de conclusão |

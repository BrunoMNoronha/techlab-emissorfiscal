# EF-06 — ficha obrigatória do piloto real

Modelo ligado ao contrato documental [nfce-df-homologacao/0.2](PILOT-CONTRACT.md). Data: 05/10/2026. **Não preenchido: empresa e responsáveis ainda não selecionados.** `TBD` não tem valor padrão. Regime, série e códigos da simulação não preenchem esta ficha.

Em 18/10/2026, Bruno confirmou que ainda não há empresa definida. O [roteiro de seleção](PILOT-SELECTION.md) organiza a triagem e os próximos passos sem preencher fatos por suposição.

Durante a seleção EF-06, as evidências ficam sob guarda do emitente em registro privado já autorizado, com acesso aprovado aos revisores e referência publicável sem conteúdo privado. Identificar esse meio na ficha, sem provisionar armazenamento do serviço; a custódia operacional fiscal será decidida na EF-07 depois. Versionar somente o modelo/referências publicáveis; documentos privados, dados de compradores, PFX/senhas/tokens e connection strings ficam fora do Git/chat. Preenchimento cadastral não autoriza credencial/transmissão.

## 1. Identificação, papéis e delegação

| Campo obrigatório | Valor atual | Evidência necessária | Impacto da ausência |
| --- | --- | --- | --- |
| Identificador/versão da ficha | `TBD` | Registro seguro, revisão e vínculo ao contrato 0.2 | Sem correlacionar aprovação e configuração |
| Meio privado de evidência da seleção | `TBD` | Emitente indica registro já autorizado, responsável por sua guarda e acesso dos revisores; referência publicável sem conteúdo privado | Sem comprovar fatos da seleção; não é escolha de armazenamento operacional EF-07 |
| Empresa/estabelecimento emitente | `TBD` | Cadastro, CNPJ/IE/endereço/UF e identidade fiscal canônica | Sem configurar identidade real |
| CRT efetivo e vigência cadastral | `TBD` | Cadastro comprovado e validação tributária | Não escolher grupo nem inferir CRT da fixture |
| Sistema(s) cliente(s) e proprietário do conteúdo | `TBD` | Identificação técnica e vínculo emitente/ambiente | Sem emitir/consultar/baixar por cliente |
| Pessoa que exerce operador central | `TBD` | Identidade, vínculo com serviço e alcance administrativo | Papel de produto não comprova pessoa/autoridade |
| Autorizante efetivo do emitente | `TBD` | Identidade e legitimidade de delegação | Sem administrar acesso em nome da empresa |
| Delegação ao operador central | `TBD` | Documento, ações/clientes/emitentes/ambiente, validade e revogação | Sem poderes administrativos comprovados |
| Responsável tributário | `TBD` | Identidade, vínculo e alcance de aprovação de cada caso | Sem aprovar elegibilidade/tributação |
| Autoridade operacional de homologação | `TBD` | Pessoa/autorização específica vinculadas a EF-07/13/14 | Sem A1/transmissão pelo simples preenchimento |
| Aliases/concessões por ação | `TBD` | Alias → identidade, cliente/emitente/ambiente/ação, origem/data da concessão | Alias não amplia permissão nem cria pool |

Acumular papéis somente com vínculo/autoridade documentados para cada função; não presumir pelo nome do solicitante. Cada cliente vê suas próprias operações. Administração de concessões não dá acesso automático ao conteúdo fiscal.

## 2. Série exclusiva e histórico

| Campo obrigatório | Valor atual | Evidência necessária | Impacto da ausência |
| --- | --- | --- | --- |
| Escopo do pool | Modelo `65`, UF `53`, ambiente `2`; identidade/série `TBD` | Configuração por estabelecimento/modelo/ambiente/série | Sem reserva fora de pool validado |
| Série exclusiva e admissibilidade | `TBD` | Série indicada pela empresa e regras aplicáveis | Não assumir série 1/disponibilidade |
| Histórico no ambiente | `TBD` | Emissões/reservas/cancelamentos/inutilizações/operações em andamento e lacunas | Não deduzir contador apenas pelo maior número |
| Emissores e coordenação | `TBD` | Inventário, não uso concorrente e transição quando necessária | Sem declarar exclusividade ou inicializar contador |
| Próximo número aprovado/fonte | `TBD` | Número justificado pelo histórico e aprovação responsável | Sem inicialização de pool real |
| Número após rejeição | Correção/reutilização indisponíveis inicialmente | Rejeição/histórico preservados; mudança futura fundamentada | Não reciclar número nem corrigir por nova referência |

Exclusividade deve ser comprovada. Reserva pertence ao serviço e ao pool canônico, sem pool por alias/cliente. Série escolhida não comprova credenciamento/liberação do transporte.

## 3. Caso tributário de referência — repetir por caso

Todos os campos obrigatórios abaixo começam `TBD`. Campo condicionado só recebe `não se aplica` com fundamento/revisão; ausência não vira `false`.

| Grupo | Conteúdo obrigatório / evidência |
| --- | --- |
| Identificação/revisão | ID/versão, contrato/baseline/regras, cliente e responsável pela preparação |
| Emitente/regime | Vínculo ao estabelecimento/CRT comprovados e grupos tributários do caso |
| Operação | Varejo presencial/retirada/consumo próprio; origem/destino/classificação verificados para DF/65 |
| Destinatário | CPF/CNPJ ou ausência permitida com fonte; condição de contribuinte/IE/crédito independentes; evidências seguras sem dados pessoais no exemplo público |
| Crédito/elegibilidade | `permite`, `não permite` ou `não determinado`, com fonte; confrontar SINIEF 23/26/DF e demais hipóteses de 55 sem inferir do CNPJ/consumo próprio |
| Valores/identificação | Valor da operação, pedido de identificação e limites/regras nacionais/DF verificados; dados condicionais conforme o caso |
| Itens/tributação | Produtos/NCM/CFOP/origem/CST/CSOSN, grupos/bases/alíquotas/valores, benefícios/fundamento/datas e RTC quando aplicáveis; entrada do cliente validada pelo responsável |
| Quantidades/totais/pagamento | Dados comerciais/tributáveis, precisão, descontos/frete/acréscimos/inclusão no total quando aplicáveis, totais/pagamentos/troco; equações/tolerâncias e exemplo numérico rastreáveis |
| Suporte/recusas | Grupos/variantes admitidos/recusados e motivos; decisão fiscal ausente impede habilitação |
| Exemplo publicável | Payload sintético completo vinculado ao caso revisado, substituição controlada de dados privados; separar prova estrutural e adequação tributária |
| Aprovação | Responsável efetivo/data/registro seguro/escopo/ressalvas; PO ou fixture não substituem autoridade |
| Resultado esperado futuro | Após gates: emissão/consulta correlacionadas, XML/protocolo/processado, DANFE/QR e artefatos ao cliente proprietário |

Crédito/elegibilidade não determinados bloqueiam o caso. Operação que exige 55 é negativa do fluxo 65, sem conversão automática. Simples/normal têm casos separados; grupo ou CRT não aprovado não entra por aceitação genérica do XSD.

## 4. Checklist por marco

| Marco | Condição | Situação atual |
| --- | --- | --- |
| EF-06 | Seções 1–3 completas, fatos comprovados, casos/decisões com autoridade/fontes, dicionário/cenários revisados | Pendente; issue #3 parcial |
| EF-07 | Arquitetura/ambientes/retenção/recuperação/custódia definidos; sem presumir escolhas do simulador | Pendente |
| EF-05/08–14 | Normas/schema aplicáveis e identidade/persistência/idempotência/worker/A1/transporte verificados | Pendente; roadmap por dependência |
| EF-15/16 | Provas reais dos casos em homologação e DANFE/QR consistentes | Não executado |

A autoridade de cada função indicada na seção 1 registra seu aceite quando houver evidência. Este modelo não atribui autoridade, não libera produção e não encerra #3. Correção/eventos/contingência/entrega/remoto e modelo 55 seguem indisponíveis inicialmente.

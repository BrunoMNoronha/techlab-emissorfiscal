# EF-06 — contrato conceitual do piloto simulado

Versão preparatória 0.1, 05/10/2026 (America/Sao_Paulo). Base: main e1b1b9e212423a6960c04c936d6037d0d12972db. Proposta documental, sem implementação e sem homologação. Os cenários deste documento ainda não foram executados. Issue #3 não concluída e #2 parcialmente bloqueada.

## 1. Estado vigente e autoridade das informações

Este documento é a proposta versionada vigente. O registro em evidence/ef06/DECISIONS.md identifica fatos humanos e recomendações; o primeiro rascunho empresarial do PO foi invalidado e não integra este contrato. Rafael é PO delegado para análise e recomendações, não pessoa/contador/autoridade reais.

| Classe | Conteúdo e fonte |
| --- | --- |
| CONFIRMADO — humano | API multiempresa, foco inicial DF/NFC-e, tributação fornecida pelo cliente, A1: PROJECT.md/briefing; segmentos mercadinhos/distribuidoras e sistemas de vendas para usuários finais: respostas no chat de 05/10 |
| CONFIRMADO — humano | Planejar Simples e regime normal; homologação futura começa pelo regime do primeiro piloto: resposta “concordo” à recomendação explícita registrada no registro de decisões |
| CONFIRMADO — humano | Distribuidora vende para consumo próprio do adquirente: resposta direta “para consumo proprio”; não determina CPF/CNPJ/crédito/CRT |
| AUTORIZADO — humano | Simulação e agente PO; análise/pesquisa/reflexão antes de decisões materiais, sem inventar fatos: respostas diretas registradas no registro de decisões |
| RECOMENDAÇÃO — PO | Retirada e entrega como cenários, começando pela venda presencial com retirada. Perfil real de delivery não informado |
| CAPACIDADE ATUAL | Core offline 65/cUF53/tpAmb2/tpEmis1, XSD/assinatura sintética; sem API/banco/controle de acesso/reserva durável/transporte. Código e TECHNICAL-SCOPE.md |
| NÃO VIGENTE | Nomes de empresas fictícias, regime inicial presumido e políticas do primeiro rascunho do PO foram invalidados. Não são fonte de aprovação |

Norma aplicável, capacidade de código e escolha de teste são registros distintos. Consumo próprio não implica que toda operação possa usar modelo 65. Revisão fiscal do caso e eventuais condições do adquirente permanecem necessárias.

## 2. Recorte e fontes normativas

Proposta de avaliação sintética: venda varejista ao consumidor final no DF. Retirada primeiro; entrega como variante posterior. Ambos os regimes são planejados, não cobertura fiscal integral. Revenda fora da finalidade informada; operação que exige NF-e 55 recebe recusa explícita neste fluxo, sem implementar o modelo 55. Produção e tpEmis 9 continuam fora do código atual.

Conferência de 05/10/2026 com Rafael e agente principal; matriz EF05-NORMATIVE-MATRIX.md preservada. Publicação, efeitos legais e ativação técnica no autorizador não são equivalentes.

| Fonte/dispositivo | Publicação e efeitos | Aplicação delimitada e limite da evidência |
| --- | --- | --- |
| [SINIEF 19/16](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2016/AJ_019_16), cláusula quarta VII(a–c), §§4º–5º; cláusula quinta §3º, I | DOU de 15/12/2016; redação do inciso VII(c) pelo Ajuste 9/26 com efeitos em 03/08/2026; texto consolidado conferido | Identificação a partir de R$ 10 mil, abaixo quando solicitada; venda não presencial exige endereço. A partir de R$ 200 mil, modelo 55 obrigatório. UF pode reduzir limites: redução específica DF não confirmada neste ciclo. Autorização não convalida tributação. Não usar limites como configuração do DF definitiva |
| [SINIEF 12/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ012_26), cláusulas1–2 | DOU de 09/04/2026; vigência na publicação | Revoga o Ajuste 11/25; texto antigo de exclusão geral por CNPJ não deve orientar contrato. Identificação CNPJ, isoladamente, não conclui elegibilidade |
| [SINIEF 23/26](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2026/AJ023_26), cláusula primeira e §§2–3; cláusula segunda | DOU de 09/07/2026; efeitos em 05/10/2026 | Crédito de ICMS do adquirente na operação varejista condiciona modelo 55, com condições estaduais. PO leu fonte primária anteriormente nesta mesma sessão; novo acesso falhou nesta rodada também no agente principal. Conteúdo reportado como evidência anterior; reconfirmação atual e análise específica do DF permanecem pendentes antes de codificar |
| [Portaria DF 387/2019](https://www.sinj.df.gov.br/sinj/Norma/3efbac69483f4b74b65ffd53ba901b0a/Portaria_387_20_12_2019.html), art. 1º, §2º, I; art. 5º, §§1º–2º; art. 11 | DODF de 23/12/2019, vigência na publicação; texto compilado conferido, inclui alteração pela Portaria 998/2025 | Varejo/consumidor final sem substituir o modelo 55 obrigatório. Em entrega, DANFE acompanha trânsito impresso ou apresentável em equipamento eletrônico. Substituição da impressão tem concordância/condições do §1º; não impor papel como única alternativa |

Dimensões independentes do caso: finalidade de aquisição, identificação, condição de contribuinte, direito a crédito, presença da contratação e retirada/entrega. Não deduzir uma da outra. Condição de crédito pode ser não determinada, com fonte/classificação preservada; nessa situação, não concluir elegibilidade por um booleano default. Texto compilado pode incluir redações revogadas: prevalência conferida pelos dispositivos e datas, sem prova de ativação técnica. Consulta normativa limitada; não é cobertura completa das operações.

## 3. Contrato conceitual — proposta de produto, não DTO publicado

Obrigatoriedade O = obrigatória na proposta; C = condicionada ao caso; S = determinada pelo servidor; P = política/especificação ainda pendente. Nomes são conceitos, sem rotas, linguagem de implementação, banco ou autenticação escolhidos. Dicionário fiscal completo, limites e exemplos tributários dependem do pacote/regras e casos verificados; lista abaixo não substitui o leiaute oficial.

| Conceito | Classe/origem | Regra proposta e pendência |
| --- | --- | --- |
| Versão de contrato | O, cliente | Versão suportada explícita; número público final TBD. Rejeitar desconhecida; mudanças não regeneram histórico |
| Solicitante | S, contexto autenticado futuro | Identidade do cliente e concessões vêm do servidor, não do tenant no body. Credencial técnica EF-07/EF-08 |
| Emitente/estabelecimento | O referência cliente + S resolução | Resolver vínculo autorizado e identidade fiscal canônica auditável; CNPJ/IE/cadastro real não inventados |
| Modelo/UF/ambiente/emissão | O referência/configuração + S allowlist | Código atual: 65/53/2/1; parâmetros não habilitam55/outra UF/produção/contingência |
| Referência da operação | O, cliente | Estável; namespace proposto inclui cliente autenticado, identidade fiscal canônica e ambiente. Forma/tamanho/canonicalização precisam ser fechados antes de implementar |
| ID/revisão da operação | S | Preservar cada revisão, vínculos e tentativas. Alteração de conteúdo com mesma referência não cria revisão silenciosa |
| Dados da venda | O, cliente | Itens, quantidades, unidades, valores, grupos fiscais e pagamentos do caso. Precisão, arredondamento, limites de itens/campos e validação de totais ainda precisam de especificação rastreável |
| Regime do emitente | S cadastro autorizado + dado declarado cliente conferido | CRT deve corresponder ao caso cadastrado. Simples/normal são escopo planejado; MEI/sublimite não aprovados implicitamente. Divergência deve ser recusada |
| Tributação | O/C, cliente | Preservar dados recebidos; grupos/códigos/valores condicionados ao regime/operação e versão. Não substituir código, benefício ou valor por heurística; validar estrutura/consistência/regras suportadas |
| Destinatário | C, cliente | Identificação, endereço e indicadores conforme dispositivos aplicáveis e caso; ausência só quando permitida. Não presumir comprador PF/não contribuinte pelo segmento |
| Presença e recebimento | O classificação cliente / C dados | Presença da contratação separada de retirada/entrega. Compra remota pode ter retirada. Identificação/endereço por dispositivo e caso; entrega envolve DANFE. Não preencher dados ausentes nem presumir indPres=1 pela retirada |
| Série/número | S configuração/reserva proposta; P legado | Seleção por pool autorizado; cliente não impõe número livre. Reserva atômica futura. Série fiscal real e reutilização após rejeição permanecem pendentes |
| Configuração e A1 | S | Referência interna autorizada e snapshot; sem PFX/senha/CSC em body público, resposta ou logs. Custódia EF-07/EF-13 |
| Baseline e requestHash | S | Versão/hash imutáveis na revisão; canonicalização versionada a especificar. Cliente não fornece schema confiável. SHA256 de XML e digest fiscal têm finalidades distintas |
| Metadados de negócio | C, cliente | Opcionais se definidos no contrato; limites e inclusão/exclusão no hash explícitos, nunca usados como permissão |

**Resultado proposto:** operação/revisão, fase local e estado fiscal separados; motivo local para variante/entrada inválida, códigos/motivos fiscais literais quando disponíveis, mensagens ordenadas, chave e nProt como strings quando comprovados, baseline e referências de artefatos autorizados. Recebimento da solicitação não significa autorização fiscal. Não definir códigos HTTP finais neste documento.

Consulta e download exigem vínculo por emitente/ação/ambiente e não iniciam nova emissão. Nunca tomar protocolo enviado pelo cliente como prova de autorização. Resposta inválida/truncada/código desconhecido não equivale a rejeição definitiva; conservar incerteza e evidência.

## 4. Políticas propostas e consequências

| Tema | Recomendação e motivo | Limite/ponto pendente |
| --- | --- | --- |
| Acesso | Negação padrão; concessão por cliente, identidade fiscal, ambiente e ação. Emitir, consultar, baixar, corrigir e administrar separados; reduz exposição cruzada | Quem concede/revoga e visibilidade compartilhada reais TBD; mecanismo EF-07 |
| Aliases/clientes | Alias resolve identidade/pool únicos sem conceder acesso. Proposta de teste: cliente CA e cliente CB podem compartilhar emitente A somente com concessão explícita; acesso ao conteúdo separado | Não inferir que mesmo CNPJ implica mesmo tenant/dono/visibilidade. Política real de propriedade TBD |
| Pool | Serviço futuro controla reserva por identidade fiscal/modelo/ambiente/série; unicidade e atomicidade. Preferir isolamento de cenário sem legado para testar primeiro | Rótulo POOL-A é só dado sintético; número de série não definido. Série real dedicada precisa de admissibilidade e inventário. Coexistência exige coordenação comprovada |
| Idempotência | Mesma referência/conteúdo retorna snapshot existente; diferente conteúdo gera conflito. Preserva identidade da operação | Namespace/hash/canonicalização/retenção da chave devem ser definidos antes de codificar; não confundir referência cliente com nNF |
| Correção | Nova revisão explícita após rejeição definitiva, preservando bytes/tentativas; cenário é histórico auditável | Quem corrige e regra manter/trocar número TBD. Não liberar continuidade fiscal enquanto política ausente; timeout/217 não habilitam correção/reemissão |
| Recuperação | Intenção/XML/tentativa duráveis antes de I/O futuro; resultado desconhecido inicia reconciliação | Fencing local não cancela envio em voo; bloqueio XSD de consulta não pode ser contornado por fallback |

## 5. Cenários e dados de teste — todos ainda não executados

PILOTO-A explora Simples; PILOTO-B explora normal, ambos por escolha explícita de parâmetro de teste, não regime de empresa presumida. Não há nome empresarial, CNPJ/IE real, contabilista fictício aprovando ou valor fiscal inventado. Os aliases não são documentos fiscais válidos. Payload fiscal completo fica TBD até seleção de exemplo suportado oficialmente; fixtures atuais podem demonstrar estrutura/assinatura, nunca enquadramento real.

| ID | Caso / dado sintético | Resultado esperado futuro / fonte |
| --- | --- | --- |
| C01 | PILOTO-A/PILOTO-B, retirada para consumo próprio, dados fiscais suportados ainda a completar | Avaliar geração/validação sem transmissão; tributação preservada. PROJECT + recorte técnico; nenhum CFOP/CST presumido |
| C02 | Venda não presencial com retirada: identificação/endereço omitidos; depois presentes. Variante remota com entrega; variante presencial com entrega | Recusar dados exigidos ausentes pelo inciso VII(c); avaliar casos completos. Na entrega, DANFE em trânsito em papel ou eletrônico conforme art. 5º, §2º; não inferir requisito de endereço para toda entrega somente pelo inciso VII(c). Regra adicional de produto exige justificativa própria |
| C03 | Entrada explicitamente classificada como exigindo modelo 55, por exemplo condição fiscal de crédito aplicável verificada | Recusar fluxo65; não trocar modelo automaticamente. Contexto não inferido de CNPJ isolado |
| C04 | ClienteCA sem concessão tenta emitente B, arquivos/credencial/consulta | Negar em todos os caminhos, inclusive trabalhador e erro. TECHNICAL-SCOPE/EF-08 |
| C05 | Dois aliases do emitente A e CA/CB explicitamente autorizados | Mesma identidade/pool, sem colisão; permissão de conteúdo não ampliada pelo alias. Proposta §4 |
| C06 | Mesma referência e mesmo conteúdo; depois conteúdo distinto | Repetição recupera snapshot sem novo número/envio; alteração gera conflito. EF-10 |
| C07 | Pool sintético com número marcado ocupado | Bloquear reserva conflitante; não presumir contador inicial livre ou adivinhar maior número usado. Política real de legado TBD |
| C08 | Rejeição definitiva comprovada e solicitação explícita de correção | Preservar revisões/artefatos, bloquear continuidade enquanto política fiscal de número estiver ausente. Não substituir original |
| C09 | Timeout/crash após tentativa; consulta com código 217 isolada | Resultado desconhecido, sem nova reserva/regeneração/retransmissão automática. EF-12 |
| C10 | Consulta com cMsg/xMsg não aceita pela baseline | InvalidResponse, sem autorização/processado por heurística. Teste existente e EF-05 |
| C11 | Modelo55/outra UF/ambiente1/tpEmis 9 | Recusar no núcleo atual: 65/53/2/1; schema genérico aceitar não amplia escopo |
| C12 | Permissão revogada antes de tarefa pendente; resposta tardia de trabalhador anterior | Revalidar concessão; histórico preservado, sem autorizar trabalhador obsoleto. Detalhes de corrida precisam EF-07/EF-08/12 |
| C13 | CRT declarado diverge do perfil configurado; grupos tributários não suportados | Motivo local explícito; não converter CST/CSOSN/calcular tributo silenciosamente. Dicionário concreto TBD |

Não há provas implementadas novas para C01–C13; são critérios documentais futuros. 84/84 testes da PR #20/CI são históricos distintos.

## 6. O que está definido e o que bloqueia

**Suficiente para desenho sintético:** finalidade consumo próprio; ambos regimes planejados; limites atuais; separação cliente/servidor; critérios de idempotência, isolamento, histórico e limites da consulta; matriz de variantes a explorar. Políticas propostas §4 não são aprovação humana de configuração real.

**Antes de implementar contrato fiscal completo:** campos/limites/totais/versionamento/canonicalização finais; casos fiscais rastreáveis por regime/operação; condição de destinatário/crédito e regra de classificação; política de identidade/delegação/série/correção. PO pode recomendar solução de produto com fontes, mas não fabricar política tributária.

**Antes de homologação real:** emitente/estabelecimento e regime efetivos, validação tributária dos casos e autoridade operacional, série/legado, credenciamento e custódia A1, armazenamento/recuperação, infra EF-07, transporte EF14 e limites EF-05 resolvidos ou tratados por recorte explicitamente aprovado. Não decidir produção aqui.

**TBDs adiáveis para esta proposta:** escolha de banco/hospedagem/framework/rotas/autenticação da EF-07, métricas de operação e expansão55/outrasUFs. Isso não dispensa fechá-los antes do marco operacional.

## 7. Revisão, evidência e status

[Revisão do PO](evidence/ef06/PO-REVIEW.md) e [registro das decisões](evidence/ef06/DECISIONS.md) preservam a origem desta proposta. Foram incorporadas as distinções entre presença/recebimento, limites nacionais/DF e crédito não determinado, além da representação do DANFE. A revisão aprova a entrega conceitual, não validação tributária ou configuração operacional.

Entrega documental suficiente para avaliação conceitual; não constitui contrato pronto para implementação fiscal nem aceite de conclusão da EF-06. Cenários futuros não são testes executados. Validação documental e CI deste incremento são registradas em [VALIDATION.md](VALIDATION.md); os 84 testes anteriores são evidência histórica distinta. Issues #2 e #3 continuam abertas.

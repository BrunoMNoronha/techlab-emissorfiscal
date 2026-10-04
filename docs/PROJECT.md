# Projeto — [RECOMENDAÇÃO] TechLab+ Emissor Fiscal — nome provisório baseado na pasta de trabalho. (Fonte: Diretório local verificado em 04/10/2026)

Use como **briefing e instrução permanente**. Preencha o conhecido, use `TBD` nas lacunas e não repita contexto já existente no repositório.

## 1. Briefing

- **Produto/problema/evidências:** [CONFIRMADO] Desenvolver emissor próprio de NF-e e NFC-e com integração direta à SEFAZ, usando documentação oficial. (Fonte: Pedido de Bruno neste chat, 04/10/2026) | [CONFIRMADO] MOC 7.00/anexos, PDFs técnicos, cronogramas e schemas oficiais foram analisados para o primeiro recorte NFC-e offline. Onze XSDs tiveram hashes conferidos; seis entrypoints compilaram em .NET. Restore, build sem avisos/erros, 70 testes e CLI offline passaram com PFX sintético em memória. Não houve transmissão ou homologação fiscal. (Fonte: [Matriz técnica](TECHNICAL-SCOPE.md), [relatório de validação](VALIDATION.md) e [Documentos NF-e/SVRS](https://dfe-portal.svrs.rs.gov.br/Nfe/Documentos), consulta em 04/10/2026)
- **Objetivo/métricas:** [CONFIRMADO] Gerar, validar, assinar, transmitir e consultar documentos fiscais, evoluindo para cancelamento, inutilização, documentos auxiliares e contingência. (Fonte: Sequência fornecida por Bruno) | [DECISÃO PENDENTE] TBD — volumes, latência, disponibilidade, custos e metas quantitativas. (Fonte: Lacuna no levantamento)
- **Público/plataformas/interface:** [CONFIRMADO] API compartilhada entre empresas, começando pelo Distrito Federal. (Fonte: Resposta de Bruno à pergunta de abrangência neste chat, 04/10/2026) | [DECISÃO PENDENTE] TBD — sistemas clientes, empresas piloto, UFs atendidas e necessidade de painel administrativo. (Fonte: Lacuna no levantamento)
- **Responsáveis/decisores:** [DECISÃO PENDENTE] TBD — responsáveis por produto, operação, decisões tributárias e liberação fiscal. Bruno é o solicitante; isso não define outros papéis. (Fonte: Pedido no chat e lacuna no levantamento)
- **Fluxo/perfis/permissões:** [RECOMENDAÇÃO] Sistema cliente autenticado solicita emissão; serviço verifica acesso ao emitente, registra operação idempotente, reserva numeração atomicamente, gera/valida/assina XML e transmite. Persistir respostas e reconciliar resultados incertos por consulta antes de repetir transmissão. Cliente consulta estado e recupera documentos conforme permissão. (Fonte: Proposta de arquitetura do agente, a detalhar após leitura técnica) | [DECISÃO PENDENTE] TBD — autenticação da API, administradores, delegação entre sistemas e empresas, permissões para emissão, eventos e acesso aos arquivos. (Fonte: Lacuna no levantamento)
- **MVP / fora do MVP:** [RECOMENDAÇÃO] Sequência proposta: (1) fechar recorte e matriz normativa; (2) núcleo de XML/XSD/assinatura com dados fictícios; (3) emissão e consulta em homologação com persistência, isolamento, numeração e idempotência; (4) cancelamento, inutilização, DANFE/QR Code e contingência aplicáveis; (5) validar recuperação e preparar operação. Confiabilidade começa na etapa 2, sem adiamento até o fim. (Fonte: Ordem de Bruno e proposta técnica do agente) | [CONFIRMADO] Primeiro fluxo completo em homologação: NFC-e (modelo 65). NF-e (modelo 55) será desenvolvida depois. O sistema cliente fornece a tributação; certificado A1. (Fonte: Respostas de Bruno às perguntas de modelo e tributação neste chat, 04/10/2026) | [DECISÃO PENDENTE] TBD — operações e regimes tributários iniciais, limites finais do MVP e marco de início da NF-e. (Fonte: Lacuna no levantamento) | [RECOMENDAÇÃO] Fora do recorte inicial proposto: ERP/PDV completo, NFS-e, outros DF-e, cobrança SaaS e operação em produção. Ampliação depende de necessidade expressa. (Fonte: Proposta de contenção do MVP pelo agente)
- **Regras de negócio:** [CONFIRMADO] Usar MOC, Notas Técnicas, schemas e regras do DF; acompanhar atualizações e datas de ativação por ambiente. (Fonte: Instruções de Bruno) | [RECOMENDAÇÃO] Não inferir autorização fiscal de resposta HTTP ou envio concluído. Distinguir erro local, rejeição fiscal, processamento pendente, resultado desconhecido e autorização comprovada. Proibir duplicidade de operação e colisão de numeração; especificar chave fiscal conforme modelo e norma. Versionar a matriz normativa e não confundir publicação com vigência. (Fonte: Proposta técnica do agente; regras detalhadas devem receber referência normativa) | [CONFIRMADO] O sistema cliente fornece os dados tributários. O emissor não assume cálculo ou escolha automática de enquadramento tributário. (Fonte: Resposta de Bruno à pergunta de tributação neste chat, 04/10/2026) | [RECOMENDAÇÃO] Validar estrutura, consistência e regras aplicáveis aos dados recebidos; recusar variantes fiscais não suportadas explicitamente. Dados fornecidos pelo cliente não dispensam validação. (Fonte: Proposta técnica do agente) | [DECISÃO PENDENTE] TBD — regimes e operações aceitos, regras fiscais aplicáveis e tratamento de séries/numeração já usadas por outros emissores. (Fonte: Lacuna no levantamento)
- **Dados/privacidade/retenção:** [RECOMENDAÇÃO] Modelar empresas/estabelecimentos, vínculo com sistemas clientes, configuração fiscal por ambiente, solicitações, XML, protocolos, eventos, tentativas, versão normativa e auditoria. Guardar arquivos e segredos com controles distintos; certificados, senhas e CSC nunca entram no Git, exemplos ou logs. (Fonte: Proposta de arquitetura do agente) | [DECISÃO PENDENTE] TBD — custódia de certificados/chaves, proteção de dados pessoais, retenção fiscal, backups, restauração e política de exclusão. A retenção precisa de fundamento aplicável, sem prazo inventado. (Fonte: Lacuna no levantamento)
- **Integrações/dependências:** [CONFIRMADO] Fontes indicadas e acessadas: catálogos de documentos NF-e/NFC-e, relações de serviços e Portal da Receita do DF. A página do DF referencia manuais e arquivo de regras adotadas. Acesso aos catálogos não comprova comunicação com os autorizadores. (Fonte: [Documentos NFC-e](https://dfe-portal.svrs.rs.gov.br/Nfce/Documentos); [Serviços NF-e](https://dfe-portal.svrs.rs.gov.br/Nfe/Servicos); [Serviços NFC-e](https://dfe-portal.svrs.rs.gov.br/Nfce/Servicos); [Receita DF](https://www.receita.fazenda.df.gov.br/aplicacoes/CartaServicos/servico.cfm?codCategoriaServico=96&codServico=772&codSubCategoria=218&codTipoPessoa=7), consulta em 04/10/2026) | [CONFIRMADO] Suportar certificado A1 no recorte inicial. (Fonte: Resposta de Bruno à pergunta de certificado neste chat, 04/10/2026) | [DECISÃO PENDENTE] TBD — acesso seguro ao A1, credenciamento do emitente, CSC quando aplicável, autorizador e endpoints efetivos por modelo/UF/ambiente. Confirmar dependências na documentação antes da transmissão; não solicitar segredos no chat. (Fonte: Lacuna no levantamento; dependências a verificar)
- **Segurança/performance/acessibilidade/observabilidade/disponibilidade:** [CONFIRMADO] Validar separação entre empresas, numeração, recuperação de falhas e prevenção de duplicidade. (Fonte: Item 5 da sequência de Bruno) | [RECOMENDAÇÃO] Isolamento em todas as consultas e ações; concorrência controlada e histórico durável; correlação das operações sem expor conteúdo sensível. XML externo exige parser seguro. Trabalhador de transmissão recuperável e segregação de homologação/produção; escolher transporte e persistência após prova técnica. (Fonte: Proposta técnica do agente) | [DECISÃO PENDENTE] TBD — SLA, RPO/RTO, limites de carga, alertas e requisitos de interface/acessibilidade caso exista painel. (Fonte: Lacuna no levantamento)
- **Restrições/prazo/custos:** [CONFIRMADO] pnpm obrigatório em Node.js; proibição de npm/npx e package-lock.json conforme instrução do projeto. Usar fontes oficiais indicadas. (Fonte: AGENTS.md fornecido no chat e pedido de Bruno) | [CONFIRMADO] A integração em homologação foi solicitada expressamente na sequência de trabalho. A dispensa genérica de homologação na seção 3 não elimina essa etapa fiscal. Produção exige alvo e condições de operação definidos. (Fonte: Item 3 da sequência de Bruno e precedência do pedido sobre o modelo da skill) | [DECISÃO PENDENTE] TBD — prazo, orçamento, hospedagem e autorização de uso operacional de certificados e empresas reais. (Fonte: Lacuna no levantamento)
- **Repositório/diretório/branch:** [CONFIRMADO] C:/Development/Projects/techlab-emissorfiscal está na branch main, com origin no repositório privado [BrunoMNoronha/techlab-emissorfiscal](https://github.com/BrunoMNoronha/techlab-emissorfiscal). Código offline inicial em d981b92 e revisão do roadmap em ba17162. As pendências EF-04 a EF-21 foram planejadas e convertidas em 18 issues abertas (#1 a #18), verificadas e vinculadas no [roadmap](ROADMAP.md). Não há workflow de CI implementado nesta etapa. (Fonte: git status/log/remote, gh repo/issue e conferência dos 18 corpos/URLs, 04/10/2026) | [DECISÃO PENDENTE] TBD — configuração de CI, ambientes e destinos de deploy. Publicação do código e criação de issues não definem infraestrutura nem autorizam transmissão fiscal ou produção. (Fonte: Lacuna no levantamento; recorte autorizado)
- **Stack/banco/infraestrutura:** [CONFIRMADO] .NET 10 foi escolhido no pedido para a prova local; global.json fixa SDK 10.0.401. Após instalação pelo usuário, essa versão foi confirmada e executada com runtime 10.0.12; restore/build/testes/CLI passaram. (Fonte: Pedido do incremento, global.json e [relatório](VALIDATION.md), 04/10/2026) | [DECISÃO PENDENTE] TBD — stack operacional completa, banco, armazenamento de arquivos e segredos, execução de trabalhadores e infraestrutura. (Fonte: Lacuna no levantamento; a prova não define esses componentes) | [RECOMENDAÇÃO] Validar a stack pela capacidade verificável de validar XSD, assinar XML corretamente e comunicar com os serviços fiscais, além de transações para numeração/idempotência. Começar com poucos componentes e usar bibliotecas especializadas quando adequadas; integração direta não implica reimplementar criptografia. (Fonte: Critério técnico proposto pelo agente; prova offline executada, comunicação fiscal permanece fora deste incremento)
- **Definição de pronto:** [RECOMENDAÇÃO] Para este briefing: 15 campos, seis seções preservadas, fontes acessíveis e TBD explícitos. Para o emissor: XML válido no pacote selecionado, assinatura verificável, autorização e consulta reais em homologação com protocolo/XML persistidos; isolamento e concorrência testados; recuperação de timeout/crash sem nova emissão indevida; eventos, documentos e contingência comprovados no recorte aprovado. Reportar separadamente validação local, homologação fiscal e prontidão de produção. (Fonte: Critérios propostos pelo agente, derivados do escopo informado) | [DECISÃO PENDENTE] TBD — aceite tributário/operacional, casos fiscais de referência e condições de liberação em produção. (Fonte: Lacuna no levantamento)

## 2. Operação e autonomia

- **ChatGPT:** arquiteta, pesquisa, decide, planeja e orquestra. Produz tarefas, critérios de aceite e prompts; revisa evidências e conduz a entrega.
- **Claude ou Antigravity:** executam os prompts. Antes de alterar, consultam repositório e documentação; ao final, sempre entregam o relatório obrigatório.
- O ChatGPT não repete a implementação, salvo para revisar, corrigir ou quando solicitado.

Antes de decisão relevante, analise o projeto e pesquise de forma direcionada em fontes confiáveis ou documentação oficial. Escolha a solução mais simples que atenda ao MVP com segurança, manutenção e custo adequados. Registre apenas decisões importantes.

Prossiga sem confirmação em escolhas reversíveis ou de baixo risco. Consulte o usuário somente diante de conflito de negócio, obrigação jurídica/financeira, credencial indispensável, risco grave de segurança ou ação irreversível com possível perda de dados.

Há autorização para editar arquivos, instalar dependências justificadas, criar branches, executar migrações seguras, fazer **commit, push, abrir/revisar/mesclar PR, deploy e publicação**. Preserve alterações existentes, valide antes de entregar e mantenha rollback quando aplicável. Nunca exponha segredos, reduza segurança ou execute ação destrutiva em dados reais sem proteção explícita.

Em Node.js, use **pnpm**; não use npm sem solicitação expressa.

## 3. Fluxo

1. Verificar estado do repositório e ler somente arquivos relevantes.
2. Pesquisar apenas incertezas materiais.
3. Definir objetivo, escopo, aceite, riscos e validações.
4. Gerar prompt curto e autocontido, referenciando arquivos em vez de copiar conteúdo extenso.
5. Receber relatório; revisar diff e evidências; concentrar correções em **um único prompt**.
6. Com os controles técnicos concluídos, finalizar Git, PR, merge e publicação sem nova confirmação.

Sem solicitação expressa, **ignore homologação**. Validação técnica permanece obrigatória.

## 4. Documentação, qualidade e revisão

- Repositório é a fonte oficial. Prefira `docs/PROJECT.md`; crie outros documentos somente quando necessários.
- Documente incrementalmente e atualize apenas o afetado.
- Revise pelo risco e pelo diff; não reanalise todo o projeto a cada ciclo.
- Execute controles aplicáveis: lint, tipagem, testes, build, migrações e segurança.
- Teste caminho principal, bordas, vazio e alto volume quando pertinente.
- Após correção, repita apenas verificações impactadas; use suíte completa antes de merge/deploy em mudança ampla ou crítica.
- Use segundo agente revisor apenas em autenticação, autorização, pagamentos, dados sensíveis, segurança, migração destrutiva ou arquitetura crítica.
- Não reabra decisão registrada sem nova evidência.

### Incremento local NFC-e — 04/10/2026

- **Status verificado:** concluído para a prova local offline. SDK 10.0.401/runtime 10.0.12 usados; `scripts/verify.ps1` terminou com exit 0: restore, build Release com zero avisos/erros, 70/70 testes e CLI schemas/demo. Assinatura/PKCS#12 sintética comprovada após serialização/envelope/nfeProc. Não declarar homologação fiscal.
- **Recorte autorizado do incremento offline:** modelo 65/DF/cUF 53/tpAmb 2/tpEmis 1, sem transmissão. A etapa original ficou somente local, sem Git/publicação. O pedido posterior de Bruno, “suba pro github”, autorizou inicialização, commit e push do projeto no repositório privado indicado acima. Não autoriza deploy, banco, migração ou transmissão fiscal.
- **Materiais conferidos:** hashes dos três ZIPs oficiais e fechamento de 11 XSDs; PL009n < PL010d v1.03/subárvore NFe < PL010f v1.04. Os seis entrypoints compilaram em .NET, com positivos numéricos/alfanuméricos e negativos aprovados. A auditoria lxml anterior permanece complementar; seus placeholders não são a prova criptográfica.
- **Limites normativos registrados:** consulta PL010d não aceita cMsg/xMsg em infProt; revisão futura de pacote/provas necessária. Portaria DF 387 compilada e corpo do AD 1/2026 Assuntos Econômicos foram consultados; anexo completo de benefícios e aplicação por operação ainda precisam de reconciliação. O arquivo DF de regras 2019.001 é histórico v1.20. Não há cobertura fiscal integral declarada.
- **TBDs preservados:** operações, regimes, permissões/autenticação, custódia real, retenção, credenciamento, empresas piloto, legado de numeração, infraestrutura e produção. Tributação da fixture não define o negócio. API, isolamento, numeração/idempotência e reconciliação futura estão somente documentados.
- **Correção técnica na compilação:** os dois Resolver=null de SignedXml causavam CS8625 no pacote fixado. Substituídos por RejectingXmlResolver, que proíbe GetEntity, sem supressão de nulabilidade ou liberação de acesso externo. Revisão independente da correção e das evidências não encontrou apontamentos acionáveis.
- **Evidência e reprodução:** [README](../README.md), [matriz/contrato técnico](TECHNICAL-SCOPE.md), [relatório](VALIDATION.md), `schemas/nfe/4.00/manifest.json`, `.validation/dotnet-validation.json`, `.validation/dotnet-validation-final.log`, `.validation/cli-*.json` e TRX em `TestResults/`. Tentativas anteriores ao SDK e auditoria lxml foram preservadas como histórico. Reproduzir com SDK fixado e `scripts/verify.ps1`.
- **Preparação da publicação:** `.gitattributes` preserva os bytes dos XSDs oficiais e do manifest, evitando mudanças de hashes por conversão de finais de linha no checkout. Downloads, caches, resultados de teste, binários e credenciais permanecem ignorados; fixtures/certificados são sintéticos.

### Roadmap e revisão do projeto — 04/10/2026

- **Documento de andamento:** [ROADMAP.md](ROADMAP.md) registra EF-01/EF-02 concluídos, a revisão EF-03 e os próximos incrementos EF-04 a EF-21, com dependências, aceite e decisões bloqueantes. Homologação, produção e modelo 55 continuam etapas futuras.
- **Revisão comprovada:** código da base d981b92 inalterado; contadores/hash do TRX e hashes dos 11 XSDs/manifest conferidos. Segunda revisão de segurança sem nova falha acionável no recorte offline. A suíte não foi executada novamente nesta revisão documental.
- **Próximo trabalho recomendado:** CI/reprodução de dependências (EF-04) e pesquisa normativa/schema de consulta com alertas (EF-05) podem começar independentemente. Fechar contrato do piloto (EF-06) e arquitetura/custódia/dados (EF-07) antes da base operacional. Os campos desatualizados do briefing JSON foram sincronizados, sem escolher regimes, banco, autenticação ou custódia.
- **Gates mantidos:** verificação local não comprova ICP-Brasil/titularidade nem origem SEFAZ. Retorno confiável precisa ser vinculado à tentativa durável. Fencing protege transições locais e não cancela I/O fiscal já em voo; a recuperação deve preservar XML/revisão e reconciliar respostas tardias/duplicidade. Esses requisitos foram esclarecidos no [contrato técnico](TECHNICAL-SCOPE.md).

### Planejamento das pendências em issues — 04/10/2026

- **Resultado:** 18 issues abertas, de [#1](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/1) a [#18](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/18), com escopo, exclusões, dependências, aceite e validações. Inventário anterior sem issues; conferência posterior com um vínculo único por EF-04 a EF-21, sem duplicatas. Nenhuma entrega concluída EF-01 a EF-03 foi reaberta.
- **Ordem recomendada:** CI offline [#1](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/1) e pesquisa de normas/schemas [#2](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/2) podem começar agora. Decisões do piloto [#3](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/3) precedem arquitetura [#4](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/4) e a base operacional. Dependências detalhadas estão no [ROADMAP.md](ROADMAP.md).
- **Limites:** assuntos TBD permanecem condicionados a decisões/acesso; EF-21/#18 é planejamento diferido de modelo 55/expansão. Sem implementação, testes .NET repetidos, acesso a A1 real, transmissão ou deploy nesta etapa. Responsáveis, prioridades, milestones e prazos não foram inventados.

## 5. Prompt e relatório

Todo prompt deve conter:

`contexto → objetivo → escopo/fora do escopo → arquivos → restrições/decisões → aceite → validações → Git/publicação → relatório`.

Todo agente deve retornar:

- **status:** concluído, parcial ou bloqueado;
- **resultado:** resumo e arquivos alterados;
- **decisões:** suposições e justificativas relevantes;
- **validação:** comandos, testes, resultados e evidências;
- **entrega:** branch, commit, PR, merge e deploy/publicação;
- **pendências:** falhas, riscos, dívida técnica e fora do escopo;
- **próxima ação:** somente se necessária.

## 6. Primeira resposta

Entregue: resumo; fatos, suposições e bloqueios; decisões/pesquisas iniciais; plano ou backlog mínimo priorizado; e primeiro prompt executor. Não aguarde confirmação sem bloqueio real. Preserve o MVP e elimine documentação, revisões e etapas que não contribuam diretamente para a entrega.

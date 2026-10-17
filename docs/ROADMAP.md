# Roadmap — TechLab+ Emissor Fiscal

## Preparação do piloto real — contrato documental 0.2

Plano de preparação aprovado em 05/10/2026. [Contrato](PILOT-CONTRACT.md), [ficha obrigatória](PILOT-REGISTRATION.md) e [dicionário](PILOT-DATA-DICTIONARY.md) definem presencial/retirada/consumo próprio, regime conforme empresa comprovada, série exclusiva validada, operações próprias por cliente e operador central com delegação do emitente. Empresa, pessoas/autoridades, série/histórico e casos tributários ainda são `TBD`; EF-06/#3 permanece parcial e EF-07 condicionada.

O primeiro piloto completo exige **EF-15 + EF-16**: emissão/consulta/XML/protocolo e DANFE/QR. A dependência EF-16 → EF-15 permanece, sem criar ciclo. Correção, eventos EF-17/18, contingência EF-19 e entrega/remoto ficam indisponíveis inicialmente, como evolução posterior. Não há transmissão ou mudança de API/banco/transporte nesta preparação; o contrato fiscal documental tem identificação própria, distinta dos exemplos sintéticos.

Preparação inicial em `codex/integracao-simulada-duravel`, base `952c74c`; extração somente documental na branch `codex/ef-06-piloto-real-docs`, a partir dessa base. Alterações locais de integração simulada foram preservadas no checkout original. O núcleo dessa base possui 84 testes da revisão EF-05; referências a 70 testes e à base inicial abaixo são históricas.

**Histórico da base offline, conferido em 05/10/2026:** revisão do código, testes,
documentação, evidências locais e estado do GitHub. Base inicial do código: commit
`d981b927907737515cfb5c2e59b94c84da4cde40`, branch `main`, no
[repositório privado](https://github.com/BrunoMNoronha/techlab-emissorfiscal).
EF-04 a EF-21 estão vinculados a **18 issues verificadas**. EF-04 está concluído
com CI real e locks NuGet; EF-01 a EF-03 permanecem concluídos e não foram reabertos.

O primeiro incremento está concluído como **prova técnica offline de NFC-e
65/DF/ambiente 2/emissão normal 1 em .NET 10**. O emissor operacional ainda não
está implementado. O primeiro fluxo fiscal previsto é NFC-e em homologação;
NF-e modelo 55 vem depois, com recorte próprio. O sistema cliente fornece a
tributação. Recorte presencial/retirada/consumo próprio definido em 0.2; empresa,
CRT efetivo, regras/grupos e casos tributários aprovados continuam `TBD`.

## Base verificada

| Área | Estado confirmado | Evidência e limite |
| --- | --- | --- |
| XML, XSD e assinatura | Concluído no recorte offline | Parser seguro, 11 XSDs oficiais, seis entrypoints, chave/CNPJ numéricos e alfanuméricos, A1 sintético em memória e assinatura após serialização/envelope/nfeProc. [Código e execução](VALIDATION.md). |
| QR e mensagens | Concluído no recorte offline | QR 3 online DF, envio síncrono unitário, consulta e interpretação correlacionada de protocolos simulados. Não há cliente SOAP ou transmissão. |
| Validação local e CI | 70/70 testes aprovados em checkout limpo e Actions | SDK 10.0.401/runtime 10.0.12, build Release sem avisos/erros, CLI schemas/demo e hashes dos XSDs/manifest aprovados. Cenários negativos de restore/build/testes/schema propagaram falhas. Evidências de EF-04 em [VALIDATION.md](VALIDATION.md). |
| Normas e schemas | Baseline fixada; cobertura fiscal parcial | Matriz com fontes e revisões em [TECHNICAL-SCOPE.md](TECHNICAL-SCOPE.md). Consulta com alertas é incompatível com o XSD fixado; anexo DF localizado integralmente; reconciliação das linhas e aplicação por operação permanecem pendentes. Ver [EF-05](EF05-NORMATIVE-MATRIX.md). |
| Publicação e planejamento | Código, roadmap e CI entregues; issues vinculadas | Código offline em `d981b92`, revisão original em `ba17162`, CI/locks em `a00361b` e [PR #19](https://github.com/BrunoMNoronha/techlab-emissorfiscal/pull/19). Sem API, banco ou transporte fiscal. |
| API e confiabilidade operacional | Somente propostas documentadas | Autenticação, autorização, identidade canônica, banco, isolamento, reserva, idempotência, trabalhadores e reconciliação ainda não têm implementação. |
| Homologação e produção | Não realizadas | Nenhum certificado de contribuinte, credenciamento comprovado, autorização fiscal real, evento fiscal ou deploy. |

O relatório da prova permanece histórico; evidências em `.validation/` e
`TestResults/` ficam locais e ignoradas. Os comandos de reprodução estão no
[README](../README.md). As conclusões acima foram confrontadas com o código
inalterado da base e com uma segunda revisão de segurança.

## Estados e ordem de trabalho

- **Concluído:** aceite atendido no recorte indicado, com evidência identificada.
- **Planejado:** trabalho ainda não implementado; dependências indicam quando pode começar.
- **Bloqueado:** depende de decisão ou acesso indispensável que continua `TBD`.
- **Em andamento:** usar somente durante execução comprovada; a abertura de issues não inicia implementação.
- **Diferido:** planejamento fora do recorte atual, dependente de novo recorte aprovado.

Os IDs são estáveis. A ordem abaixo é uma recomendação por dependência e risco,
sem datas de entrega, responsáveis, estimativas ou prioridades de negócio
acordadas. Cada pendência possui uma issue vinculada; o estado `OPEN` no GitHub
não substitui o estado de execução indicado neste roadmap. Preservar vínculos,
dependências e evidências ao atualizar ou concluir as issues.

### Etapa 0 — base entregue

| ID | Entrega | Estado | Evidência de conclusão |
| --- | --- | --- | --- |
| EF-01 | Núcleo NFC-e offline | Concluído | Core/CLI, manifest, fixtures sintéticas e 70 testes conforme [VALIDATION.md](VALIDATION.md). Não equivale a emissor homologado. |
| EF-02 | Publicação privada do código | Concluído | Commit `d981b92` publicado na `main`; schemas/manifest preservados byte a byte e caches/credenciais ignorados. |
| EF-03 | Revisão do projeto e roadmap | Concluído | Este documento, briefing sincronizado e contratos de origem do retorno/fencing esclarecidos. Sem mudança no código fiscal. |

### Etapa 1 — preparar a continuação

| ID | Trabalho | Estado | Dependências | Aceite mínimo | Issue |
| --- | --- | --- | --- | --- | --- |
| EF-04 | CI da prova offline e reprodução das dependências | Concluído | EF-01 | Windows 2025/SDK fixado, três locks e restore bloqueado; build, 70 testes e CLI aprovados no Actions, artefatos/hashes conferidos e falha controlada reprovada com diagnósticos publicados. [Evidência](VALIDATION.md#ef-04--ci-offline-e-locks-nuget--05102026). Sem segredo/transmissão fiscal. | [#1](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/1) |
| EF-05 | Revisão normativa e compatibilidade dos schemas para o próximo recorte | Parcial — pesquisa incorporada; compatibilidade bloqueada | EF-01 | Conferir fontes oficiais, versões, vigências por ambiente e regras aplicáveis ao piloto; reconciliar consulta com alertas por pacote oficial e regressões de envio/consulta/nfeProc. Nenhuma edição de XSD oficial ou troca silenciosa de baseline. Registrar limitações ainda sem solução. | [#2](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/2) |
| EF-06 | Definir o contrato e as decisões do piloto NFC-e/DF | Parcial — contrato documental 0.2; fatos/casos reais pendentes | Ficha, autoridades, cadastro/série/casos comprovados | [Contrato 0.2](PILOT-CONTRACT.md), [ficha](PILOT-REGISTRATION.md) e [dicionário](PILOT-DATA-DICTIONARY.md): presencial/retirada/consumo próprio, regime da empresa escolhida, série exclusiva validada, conteúdo próprio por cliente e operador central delegado. Preencher fatos/tributação/autoridades indispensáveis; publicar 0.2 não encerra. Correção/eventos/contingência adiados; DANFE/QR no aceite futuro. | [#3](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/3) |
| EF-07 | Definir arquitetura operacional e política de dados/credenciais | Bloqueado | EF-06; decisões de infraestrutura/custódia `TBD` | Decidir autenticação, banco/transações, armazenamento de artefatos e segredos, execução dos trabalhadores, ambientes, auditoria, retenção, backup/restauração e custódia do A1. Registrar custos/restrições e como validar cada componente antes de implementar a base operacional. | [#4](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/4) |

EF-04 está concluído. A pesquisa de EF-05 e o levantamento de EF-06
podem avançar agora; o fechamento das regras aplicáveis depende dessas
decisões. EF-07 usa o contrato do piloto. A conclusão do CI não escolhe serviços,
regimes ou custódia para a operação fiscal.

### Etapa 2 — base operacional antes de I/O fiscal

| ID | Trabalho | Estado | Dependências | Aceite mínimo | Issue |
| --- | --- | --- | --- | --- | --- |
| EF-08 | Autenticação, autorização e identidade fiscal canônica | Planejado | EF-06, EF-07 | Identidade do cliente do servidor; concessão por ação/emitente/ambiente via operador central delegado e operações próprias por cliente. Aliases compartilham identidade/pool sem ampliar conteúdo. Negar acesso cruzado em consulta/arquivos/credenciais, worker e erros; revalidar revogação. | [#5](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/5) |
| EF-09 | Persistência de operações, revisões, artefatos e auditoria | Planejado | EF-07 | XML assinado, hash de bytes, digest fiscal, baseline, tentativa, retorno, protocolo e alertas duráveis e vinculados à operação/revisão. Não sobrescrever histórico. Demonstrar integridade, isolamento e restauração no banco/armazenamento escolhidos. | [#6](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/6) |
| EF-10 | Numeração transacional e idempotência | Planejado | EF-06, EF-08, EF-09 | Unicidade por identidade fiscal canônica/modelo/ambiente/série/número; reserva atômica sob concorrência. Definir canonicalização/versionamento de requestHash; mesma operação/conteúdo retorna a mesma revisão, conteúdo diferente gera conflito. Aliases não criam pools independentes. | [#7](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/7) |
| EF-11 | API de emissão, estado, consulta e recuperação de artefatos | Planejado | EF-05, EF-08, EF-09, EF-10 | Contrato versionado e validado; dados tributários preservados dentro do recorte suportado. Estados locais/fiscais e erros distintos, isolamento em todas as rotas, sem aceitar resultado/protocolo do cliente como prova de autorização. Nesta etapa usar transporte simulado. | [#8](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/8) |
| EF-12 | Trabalhadores, tentativas duráveis e reconciliação | Planejado | EF-05, EF-09, EF-10 | Intenção e XML imutável registrados antes do envio simulado; lease/fencing e transições auditáveis. Provar crash, timeout, resposta tardia, perda de lease, código desconhecido, duplicidade e consulta não localizada sem nova reserva ou reenvio indevido. | [#9](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/9) |
| EF-13 | Preparar credencial A1 real e emitente de homologação | Bloqueado | EF-06, EF-07; acesso seguro/uso real `TBD` | Custódia e acesso autorizado definidos, titularidade/vínculo do emitente e política ICP-Brasil/cadeia/revogação/validade comprovados; credenciamento e configuração de homologação verificados. Segredos fora de Git, chat, exemplos e logs. A prova matemática atual não atende sozinha esse aceite. | [#10](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/10) |

EF-08 e EF-09 podem ser implementados em paralelo depois de EF-07, compartilhando
o contrato de identidade. EF-11 e EF-12 convergem sobre a mesma operação/revisão;
seus testes iniciais usam respostas e transporte sintéticos. EF-13 é uma
preparação operacional separada e não pode ser simulado como concluído.

### Etapa 3 — primeiro fluxo NFC-e em homologação

| ID | Trabalho | Estado | Dependências | Aceite mínimo | Issue |
| --- | --- | --- | --- | --- | --- |
| EF-14 | Transporte fiscal autenticado em homologação | Planejado | EF-05, EF-11, EF-12, EF-13 | Confirmar autorizador/endpoints/WSDL e contrato SOAP/TLS do DF/65/ambiente 2 em fontes oficiais. Registrar tentativa antes do I/O, correlacionar resposta confiável, limitar timeouts/tamanhos e manter produção inacessível no recorte. Sem retry automático de emissão por falha de transporte. | [#11](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/11) |
| EF-15 | Homologação integrada de emissão, consulta e recuperação | Planejado | EF-04, EF-14 | Obter autorização fiscal real, persistir resposta/protocolo/XML e construir nfeProc correlacionado; provar consulta, rejeição, alertas quando aplicáveis e recuperação de falhas sem duplicidade. Validar também isolamento e concorrência no fluxo completo. Evidência local/CI e fiscal são registradas separadamente. | [#12](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/12) |

Uma lista de URLs, HTTP 200, resposta positiva simulada ou teste TCP não conclui
EF-14/EF-15. Uso de empresa/certificado real requer as condições de EF-13 e
escopo operacional definido; nenhuma transmissão é realizada por esta revisão.

EF-15 comprova o fluxo fiscal integrado; o aceite do primeiro piloto escolhido
em EF-06 também exige DANFE/QR da EF-16. XML/protocolo isolados não encerram esse
marco. O adiamento de eventos/contingência é do piloto em homologação e não
dispensa revisão dessas obrigações antes de eventual operação em produção.

### Etapa 4 — completar o recorte NFC-e aprovado

| ID | Trabalho | Estado | Dependências | Aceite mínimo | Issue |
| --- | --- | --- | --- | --- | --- |
| EF-16 | DANFE NFC-e e apresentação do QR | Planejado | EF-05, EF-15 | Renderização conforme manual/NTs vigentes, vinculada ao documento e ao estado fiscal persistido. Provar leitura do QR e consistência de chave/ambiente; a string de QR já entregue não equivale a DANFE renderizado. | [#13](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/13) |
| EF-17 | Cancelamento e eventos necessários | Planejado | EF-05, EF-12, EF-15 | Recorte/regras/prazos definidos; XML assinado, idempotência do evento, protocolo e estado correlacionados, autorização por emitente e evidência fiscal em homologação. Não tratar erro de transporte como evento registrado. | [#14](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/14) |
| EF-18 | Inutilização de numeração | Planejado | EF-05, EF-10, EF-15 | Política aprovada para lacunas/legado, validação do intervalo, bloqueio concorrente, assinatura, resposta e auditoria duráveis; provas reais em homologação. Não reutilizar números ou inutilizar intervalos por heurística. | [#15](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/15) |
| EF-19 | Contingência NFC-e aplicável ao DF | Planejado | EF-05, EF-12, EF-15, EF-16 | Política normativa e operacional definida; emissão/QR/DANFE e posterior transmissão/reconciliação comprovados, com falhas de relógio, queda/retorno e duplicidade cobertas. A prova atual é sem transporte e não implementa tpEmis=9. | [#16](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/16) |

Cancelamento, inutilização e renderização podem ter desenvolvimento independente
após suas dependências, mantendo as mesmas garantias de identidade e auditoria.
Limites do primeiro piloto foram definidos na EF-06/0.2: EF-16 obrigatório para
esse marco; EF-17–19 e correção posteriores. As condições reais da ficha e os
casos fiscais ainda precisam ser resolvidos; não declarar etapa concluída por
este recorte documental.

### Etapa 5 — operação e expansão

| ID | Trabalho | Estado | Dependências | Aceite mínimo | Issue |
| --- | --- | --- | --- | --- | --- |
| EF-20 | Preparar operação e eventual liberação de produção | Bloqueado | EF-15 e itens EF-16 a EF-19 exigidos pelo MVP; decisões/autoridade de produção `TBD` | Definir volumes/limites, disponibilidade, SLA/RPO/RTO, alertas, retenção e resposta a incidentes; provar carga, backup/restauração, recuperação e segregação de ambientes. Alvo, aceite tributário/operacional, credenciais e autoridade de liberação definidos antes de qualquer deploy/operação em produção. | [#17](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/17) |
| EF-21 | NF-e modelo 55 e ampliação de UFs/operações | Diferido, fora do recorte atual | EF-15; novo recorte aprovado | Matriz/contratos/testes próprios para modelo 55 e operações aprovadas. Não liberar 55 removendo apenas o bloqueio de modelo do núcleo; rever eventos, DANFE, transporte, regimes e novas NTs aplicáveis. Marco de início e demais UFs seguem `TBD`. | [#18](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/18) |

## Pontos que a revisão exige resolver

1. **Consulta com alertas:** `ProtocolTests.AlertExtensionMissingInPinnedConsultationSchemaFailsClosed`
   demonstra que o PL010d de consulta não aceita cMsg/xMsg em infProt, enquanto
   envio/nfeProc do PL010f aceitam. EF-05 precisa de solução baseada em material
   oficial e regressões antes de usar consulta para reconciliar autorizações com alertas.
2. **Cobertura normativa:** reconciliar regras/benefícios DF e normas/tabelas RTC
   com operações/regimes decididos. O anexo completo de benefícios DF ainda não
   foi reconciliado; a tabela histórica de regras DF não comprova cobertura atual.
   O [catálogo NF-e/SVRS](https://dfe-portal.svrs.rs.gov.br/Nfe/Documentos),
   reconferido em 04/10/2026, também lista NT 2026.009 v1.00 e NT 2026.010 v1.00
   triadas na [matriz EF-05](EF05-NORMATIVE-MATRIX.md) como modelo 55, fora do recorte. Não inferir impacto de
   título; a descrição da 2026.010 se refere ao DANFE modelo 55. O catálogo alerta
   que cronogramas dos documentos de 01/10/2026 podem mudar. Não atribuir novas
   vigências ou alterar a baseline nesta revisão.
3. **A1 e origem da resposta:** `A1Signer.Verify` verifica assinatura local;
   `NfceProtocol.Interpret` interpreta XML recebido do chamador. EF-13/EF-14 devem
   provar credencial/emitente e origem pelo transporte confiável. A API futura
   não pode autorizar uma operação com protocolo fornecido por sistema cliente.
4. **Concorrência e recuperação:** fencing controla transições locais, mas não
   cancela I/O já em voo na SEFAZ. EF-12 deve cobrir trabalhador antigo retomando,
   resposta tardia e duplicidade, preservando revisão/XML e reconciliação. Um
   217 isolado não permite presumir ausência definitiva nem reenviar.
5. **Reprodução e evidência compartilhável — resolvido em EF-04:** locks NuGet
   versionados, restore bloqueado e CI Windows 2025 comprovado em execução real,
   com artefatos positivos e diagnósticos de falha. Checkout/cache novos foram
   validados; evidências e limites em [VALIDATION.md](VALIDATION.md).
6. **Documentação sincronizada:** o JSON do briefing continha afirmações antigas
   sobre ausência de código, Git e stack. Os campos afetados foram corrigidos
   nesta revisão, mantendo o contrato de 15 campos e as decisões `TBD`.

## Execução por issues

As 18 issues foram criadas com problema/evidência, resultado, escopo e exclusões,
dependências, critérios de aceite e validações. Corpos e links foram lidos após
a criação; não foram definidos responsáveis, milestones ou prazos. Não houve
implementação do backlog naquela criação. EF-04 foi implementado posteriormente,
conforme evidências de 05/10/2026; nenhuma transmissão fiscal foi realizada.

EF-04 — #1 está concluído. Prosseguir com a pesquisa de
[EF-05 — #2](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/2).
O fechamento das regras do piloto depende das decisões de
[EF-06 — #3](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/3); a arquitetura de
[EF-07 — #4](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/4) usa esse contrato.
Depois das decisões, EF-08/EF-09 podem avançar em paralelo, seguindo as
convergências de numeração, API e reconciliação registradas nas tabelas.

As issues de preparação real, homologação e operação mantêm seus pré-requisitos
e decisões `TBD`. [EF-21 — #18](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/18)
é somente planejamento futuro, fora da implementação NFC-e atual; não autoriza
modelo 55 nem outras UFs. A criação das issues não autoriza deploy ou produção.

## Próximo incremento recomendado

O pedido posterior de levantamento/modelo foi implementado como [preparação de entrada](NFCE-INPUT-MODEL.md), `nfce-preparation/0.1`: venda/perfil/contexto, exemplos CRT 1/3 e XML novo com assinatura sintética/XSD. É base técnica offline; não conclui EF-06, EF-11 ou o piloto, nem altera o contrato executável da API simulada. Usar os exemplos para preparar casos reais, sem promover seus códigos/alíquotas a padrões fiscais. A seleção da empresa pode ocorrer em paralelo ao trabalho técnico autorizado.

A decisão de atender varejo amplo (mercados, padarias, distribuidoras de bebidas,
lanchonetes, armarinhos, agropecuárias) foi organizada na
[matriz de cobertura tributária do varejo](RETAIL-COVERAGE-MATRIX.md), de 17/10/2026:
variantes por segmento, estado no preparador e priorização P1 (ST retido:
`ICMSSN500`/`ICMS60` + CEST condicional), P2 (isenções/não incidência), P3
(redução de base) e P4 (genéricos/`Outr`). É documento de priorização; cada
incremento P1–P4 exige issue própria, matriz de validação do dicionário e fontes
antes da implementação. A decisão de transmissão permanece a direta via
SEFAZ/SVRS, já coberta por EF-14, sem mudança de aceite.

A pesquisa de **EF-05** foi incorporada; a compatibilidade depende de material oficial. Preencher a ficha **EF-06/0.2**, identificar empresa/CRT/sistema/autoridades/delegação, comprovar série exclusiva/histórico e validar casos tributários com fontes. O dicionário separa limites do leiaute e limites da fixture. Reconciliar regras DF/RTC dos casos escolhidos sem habilitar grupo não suportado.
Resolver **EF-06** antes de
especificar o contrato operacional; **EF-07** fecha as escolhas necessárias para
API/persistência. Não iniciar transmissão antes de concluir suas dependências.

Fontes de serviços e normas são a
[relação oficial NFC-e/SVRS](https://dfe-portal.svrs.rs.gov.br/Nfce/Servicos) e a
[matriz do projeto](TECHNICAL-SCOPE.md), com conferência adicional do catálogo
NF-e nesta revisão. Acesso a catálogo não comprova ativação ou comunicação fiscal.

## Manutenção e histórico

Atualizar o estado por evidência, conservar IDs/dependências e registrar novos
commits, issues e decisões quando existirem. Conclusão local, CI, homologação e
produção são marcos distintos. Não converter recomendações/TBD em decisões sem
fonte, nem reabrir EF-01/EF-02 sem regressão ou mudança de recorte comprovada.

| Data | Mudança verificada |
| --- | --- |
| 04/10/2026 | Prova offline concluída: build sem avisos/erros, 70 testes e CLI aprovados; SDK instalado pelo usuário. |
| 04/10/2026 | Código publicado em repositório privado, `main`, commit `d981b92`. |
| 04/10/2026 | Roadmap consolidado após revisão do projeto; briefing corrigido e requisitos de origem do retorno/fencing esclarecidos. Nenhum código fiscal, CI, banco ou transporte implementado nesta atualização. |
| 04/10/2026 | Pendências EF-04 a EF-21 convertidas em 18 issues verificadas (#1 a #18), com vínculos neste roadmap. EF-21 permanece diferido; EF-01 a EF-03 não foram reabertos. Sem implementação fiscal ou deploy. |
| 05/10/2026 | EF-04 concluído: três locks NuGet, CI Windows 2025 com 70/70 testes, artefatos conferidos e cenários negativos comprovados. [PR #19](https://github.com/BrunoMNoronha/techlab-emissorfiscal/pull/19). Sem mudança no núcleo fiscal/schemas, transmissão ou deploy. |
| 17/10/2026 | [Matriz de cobertura tributária do varejo](RETAIL-COVERAGE-MATRIX.md) publicada: variantes por segmento, estado do preparador e priorização P1–P4; fontes externas pendentes em `evidence/retail-coverage/`. Decisão de transmissão direta SEFAZ/SVRS mantida em EF-14. Sem mudança de código, contratos ou issues. |

### EF-05 — incremento documental e regressões

Pesquisa de 05/10 incorporada à [matriz](EF05-NORMATIVE-MATRIX.md), com proveniência durável e limites da baseline. A issue #2 continua aberta: falta pacote oficial de consulta com alertas e fechamento das regras do piloto pela EF-06. As regressões preservam rejeição segura; não houve nova baseline nem homologação.

### EF-06 — histórico da proposta 0.1 em 05/10/2026

[Contrato conceitual](PILOT-CONTRACT.md), decisões e revisão do PO incorporados. Treze cenários futuros, sem execução ou payload fiscal completo. EF-06 parcial; EF-07 depende das decisões e políticas pendentes. EF-05 mantém bloqueio de consulta com alertas. Issues #2/#3 abertas; sem API, banco, transporte ou homologação.

### EF-06 — preparação documental 0.2 em 05/10/2026

Decisões humanas H08–H16 incorporadas, ficha obrigatória e dicionário fiscal preparados; cenários C01–C13 revisados com estados previsto/adiado/regressão offline existente. [Pesquisa 0.2](evidence/ef06/FONTES-v0.2.md) preserva tentativas históricas e condições DF/ativação pendentes. A preparação não identifica empresa/autoridades nem valida casos reais; issue #3 permanece parcial/aberta. Provas e limites desta entrega em [VALIDATION](VALIDATION.md). Extração somente documental a partir de 952c74c, sem implementação fiscal ou transmissão; alterações locais da integração simulada preservadas no checkout original.

### Modelo de entrada — pedido posterior em 05/10/2026

[Levantamento oficial](evidence/input-model/FONTES.md) e modelo separado da simulação, com grupos tributários explícitos e exemplos sintéticos de dois regimes. Parser estrito e preparação XML, QR, assinatura sintética, envio/consulta locais; testes e CI registrados separadamente em [VALIDATION](VALIDATION.md). Empresa/CRT/autoridades/série/casos reais continuam pendentes e os aceites DANFE/QR, A1 real, transporte e homologação permanecem nas respectivas issues.

# EF-07 — opções para a arquitetura operacional

Preparação para decisão em 18/10/2026. A issue [#4](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/4)
continua **bloqueada** pelo aceite da EF-06 e por escolhas de infraestrutura e
custódia. Bruno escolheu PostgreSQL próprio em Docker como referência em 18/10/2026;
versão, hospedagem, responsáveis e topologia continuam TBD. Este documento
não escolhe fornecedor, política de retenção ou uso real de A1 e não autoriza provisionamento.
Fontes técnicas consultadas ao final; custos e volumes permanecem `TBD`.

## Restrições já conhecidas

O piloto previsto usa 65/DF/homologação e emissão normal. A identidade fiscal
canônica pertence ao estabelecimento/emitente; aliases autorizados compartilham o
pool por emitente/modelo/ambiente/série, mas cada cliente acessa apenas suas
próprias operações. Uma concessão do operador central depende de delegação do
emitente e não concede leitura de todo conteúdo fiscal. A entrada tributária é
fornecida pelo cliente; alterações em normas não alteram bytes de uma revisão
assinada. Estas são decisões de produto registradas no
[contrato 0.2](PILOT-CONTRACT.md), sujeitas à comprovação dos fatos da EF-06.

A [rastreabilidade dos cenários do piloto](PILOT-SCENARIO-COVERAGE.md) registra
um vínculo ainda não demonstrado no núcleo em memória: a identidade opaca do
pool não é comparada ao CNPJ do XML ao congelar uma operação. O desenho futuro
precisa definir e testar essa relação usando uma origem de identidade confiável.

## Alternativas a comparar

| Decisão | Alternativas e condições | Proposta para avaliação, não aprovada |
| --- | --- | --- |
| Autenticação | Provedor de identidade externo pode concentrar ciclo de credenciais; solução própria exige operar emissão, revogação, rotação e auditoria. Em ambos os casos, identidade autenticada não equivale a permissão sobre um emitente. | Avaliar provedor existente e políticas do ASP.NET Core; provar autorização no servidor por cliente, ação, emitente e ambiente, inclusive worker. Nenhum fornecedor selecionado. |
| Banco | Relacional cliente/servidor com constraints e transações; múltiplos trabalhadores exigem prova de concorrência. SQLite tem limites de escrita e serve apenas como referência comparativa. | Bruno escolheu PostgreSQL próprio em Docker para a arquitetura. Versão, hospedagem, topologia, operação e implantação `TBD`; provar transações e bloqueios no PostgreSQL escolhido. |
| Artefatos | Bytes XML no banco transacional, ou armazenamento de objetos com metadados no banco. No banco há uma transação de dados mais simples, mas tamanho, backup e desempenho precisam ser medidos. Em objetos há duas falhas independentes e necessidade de reconciliação de gravação/remoção. | Comparar ambos com cargas sintéticas; definir integridade, versionamento, acesso, consistência e restauração antes de escolher. Não presumir que um nome de objeto é prova de conteúdo ou isolamento. |
| Segredos e A1 | Serviço dedicado de segredos sob identidade controlada, ou custódia externa ao serviço com operação explicitamente delegada. Não presumir que um cofre aceite assinatura não exportável ou que o serviço possa operar sem acesso a material privado; validar a capacidade efetiva e a política do emitente. | Avaliar custódia com acesso mínimo, registro por emitente/ambiente, ciclo de revogação e rotação; modelo, localização e responsável `TBD`. Autorização e validação de certificado real pertencem também à EF-13. |
| Trabalhadores | Processo separado ou executado junto à API; em ambos, estado e intenção duráveis são necessários e uma fila isolada não substitui histórico no banco. | Começar avaliação com poucos componentes, sem fixar topologia; provar dois trabalhadores, perda de lease e recuperação após falha. |
| Ambientes e operação | Implantação gerenciada ou mantida pela equipe; cada uma distribui operação, custo, acesso e recuperação de forma diferente. | Comparar separação de ambientes, logs, backup e restauração com operadores e custos reais ainda `TBD`. Homologação não é produção. |

[PostgreSQL](https://www.postgresql.org/docs/current/explicit-locking.html)
oferece bloqueios de linha e [constraints](https://www.postgresql.org/docs/current/ddl-constraints.html);
isso não prova por si a atomicidade das operações da aplicação. A documentação
do [SQLite](https://www.sqlite.org/whentouse.html) descreve a limitação de um
escritor simultâneo por arquivo e recomenda considerar cliente/servidor em
cenários com muitos escritores. As [políticas ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)
são mecanismo de autorização, não substituem o modelo de concessões. Gestão de
segredos exige acesso mínimo e ciclo de vida controlado conforme
[OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html).
Links consultados em 18/10/2026; versões, características e custos do serviço
efetivamente escolhido devem ser reconferidos antes da decisão.

## Fluxo e invariantes a preservar

Fluxo proposto: autenticar cliente → verificar concessão e recorte → registrar
operação e reservar número atomicamente → preservar revisão e XML assinado →
registrar intenção e tentativa antes do I/O → persistir resposta correlacionada
ou estado incerto → consultar/reconciliar quando necessário. A origem de uma
resposta fiscal precisa ser confiável; estado/protocolo fornecido pelo cliente
não prova autorização. Não reenviar automaticamente emissão incerta.

- Operação/revisão/tentativa e baseline possuem histórico imutável e vínculo ao
  emitente, ao ambiente e ao cliente proprietário. Chave, CNPJ e protocolo são
  strings; resposta, protocolo e alertas devem ser preservados literalmente.
- `requestHash` versionado, SHA-256 dos bytes XML e digest fiscal têm entradas e
  finalidades distintas; nenhum substitui outro. Replay usa a revisão/baseline
  previamente registradas e não gera nova reserva ou XML.
- Garantias de numeração, idempotência e intenção precisam de transações e
  constraints demonstradas no armazenamento escolhido. Commit de banco e
  gravação em objeto externo não são automaticamente uma única transação.
- Lease/fencing impedem transição local obsoleta; não cancelam I/O externo já em
  voo. Resposta tardia deve ser guardada e reconciliada, sem presumir ausência de
  processamento pelo autorizador.
- Autorização também vale para download, busca, worker, uso de credencial e
  revogação; negar acesso cruzado e não registrar XML ou segredos em logs.

## Decisão → requisito → prova planejada

| Frente | Requisito | Prova a executar após escolha, com dados sintéticos |
| --- | --- | --- |
| EF-08 identidade/acesso | Concessão por cliente, emitente, ação e ambiente; aliases não ampliam conteúdo | Clientes A/B no mesmo e em diferentes emitentes; revogação antes da ação e antes do uso assíncrono; acesso cruzado negado em consultas, arquivos e erros |
| EF-09 dados/artefatos | Bytes, hashes, baseline, resposta e alertas íntegros; isolamento | Reinício, corrupção e vínculo trocado; falha entre escrita do artefato e commit; restaurar backup e comparar bytes/hashes por emitente |
| EF-10 numeração | Unicidade e reserva/idempotência atômicas | Conexões independentes competindo pelo mesmo pool e aliases; rollback/crash; replay após mudança de baseline; conflito por conteúdo |
| EF-12 workers | Intenção anterior ao I/O, histórico durável e fencing local | Dois workers, perda de lease, crash antes/depois do envio simulado, 204/539/217 e resposta tardia; consulta sem nova reserva ou reenvio indevido |
| EF-13 A1 | Acesso, titularidade, validade e revogação conforme política e autorização reais | Antes do uso real, provar controles e rotação com material sintético; comprovações reais e credenciamento seguem gate próprio |
| EF-14 transporte | Destino, ambiente e origem confiável; limites e correlação | Adaptador simulado: timeout, ambiente/chave/digest/protocolo divergentes e indisponibilidade; homologação fiscal depende de autorização e EF-13 |

## Dados para escolha e custo

Antes de selecionar serviços, obter volume de empresas, operações e tamanho de
artefatos, pico de concorrência, disponibilidade, RPO/RTO, período de retenção
fundamentado, região, responsáveis pela operação e orçamento: todos `TBD`.
Para cada alternativa, estimar separadamente computação, banco, armazenamento,
operações de leitura/escrita, tráfego, backup/restauração, identidade/segredos,
observabilidade e esforço operacional; registrar preço, data, premissas, limites
e cenário de crescimento. Sem esses dados não há comparação financeira válida.

O aceite da EF-07 exige decisões efetivas, fonte e autoridade, provas planejadas
e dependências resolvidas. Esta preparação não altera o estado da issue.

O [contrato arquitetural para avaliação](EF07-OPERATIONAL-BLUEPRINT.md) detalha
as fronteiras de confiança, invariantes de dados, transações, falhas, custódia e
provas para as issues dependentes. Além da referência PostgreSQL própria em Docker escolhida
por Bruno, as demais escolhas e condições de aceite acima continuam pendentes.

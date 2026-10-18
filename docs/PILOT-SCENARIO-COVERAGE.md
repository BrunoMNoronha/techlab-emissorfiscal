# EF-06 — rastreabilidade dos cenários do piloto

Revisão do contrato 0.2 em 18/10/2026. Este mapa liga os cenários C01–C13
às provas locais existentes e às dependências ainda abertas. Testes usam dados,
certificados e respostas sintéticos; não são homologação, não definem tributação
de uma empresa e não substituem as decisões ou a ficha da EF-06.

| Cenário | Evidência local identificada | Lacuna para o aceite do piloto |
| --- | --- | --- |
| C01 — emissão e artefatos | [Preparação](../tests/EmissorFiscal.Core.Tests/NfcePreparationTests.cs), `DeclaredValuesGroupsUnitsDiscountsPaymentsAndEmitterArePreserved`; [DANFE](../tests/EmissorFiscal.Core.Tests/DanfeTests.cs), `ModelIsBoundToKeyEnvironmentProtocolAndDeclaredValuesOfTheSameProcessedXml` | Caso tributário e CRT comprovados, emissão/consulta reais, protocolo e DANFE/QR em homologação; os testes isolados não provam o fluxo integrado real |
| C02 — presença e entrega | [PreparationJson](../src/EmissorFiscal.Core/Preparation/PreparationJson.cs) exige `indPres=1` e `modFrete=9` no preparador | Provar recusa de contratação remota e entrega em todas as entradas operacionais, inclusive combinações ambíguas; uma validação local não é política da API |
| C03 — elegibilidade e crédito | [Preparação](../tests/EmissorFiscal.Core.Tests/NfcePreparationTests.cs), `RecipientMayBeOmittedAndCreditIntentIsNeverInferredFromCnpj` | Não há decisão de elegibilidade real: crédito/contribuinte desconhecidos e casos que exigem modelo 55 precisam ser avaliados e bloqueados no fluxo operacional |
| C04 — acesso cruzado | Nenhuma prova de autorização de cliente no núcleo offline | Identidade autenticada, concessão e propriedade em consulta, arquivos, credencial e erros; cenários A/B dependem de EF-08/11/12/13 |
| C05 — aliases e pool | [Numeração](../tests/EmissorFiscal.Core.Tests/NumberingTests.cs), `SeriesIsNormalizedSoTheSameCanonicalIdentitySharesOnePool` | Resolver aliases a partir de identidade confiável e comprovar propriedade e isolamento por cliente; uma string igual no teste não prova resolução nem autorização |
| C06 — replay e conflito | [Numeração](../tests/EmissorFiscal.Core.Tests/NumberingTests.cs), `SameOperationAndContentReplaysTheSameReservationEvenUnderConcurrency` e `SameOperationWithDifferentContentConflictsWithoutConsumingANumber`; [hash](../tests/EmissorFiscal.Core.Tests/RequestHashTests.cs) | Persistência de operação, bytes e histórico, namespace de cliente e prova transacional entre processos; reserva em memória não é recuperação durável |
| C07 — série e histórico | [Numeração](../tests/EmissorFiscal.Core.Tests/NumberingTests.cs), `ReserveWithoutApprovedNextNumberFails` e `ReinitializingAPoolFailsBecauseItWouldAllowReuse` | Histórico, admissibilidade e exclusividade comprovados pela empresa; os testes não consultam legado nem coordenam outros emissores |
| C08 — rejeição e correção | [Ciclo de vida](../tests/EmissorFiscal.Core.Tests/OperationLifecycleTests.cs), `RejectionIsTerminalAndKeepsTheConsumedNumber` | Persistir e controlar acesso aos artefatos e motivo; correção continua indisponível no piloto inicial |
| C09 — incerteza | [Ciclo de vida](../tests/EmissorFiscal.Core.Tests/OperationLifecycleTests.cs), `TimeoutAfterSubmissionNeverResendsAndResolvesByConsultation`, `LateResponseFromAWorkerThatLostItsLeaseIsRefusedAndTheStoreIsUnchanged` e `IsolatedNotFoundNeedsManualReconciliationWithoutNewReservationOrResend` | Processo durável, concorrência real, resposta tardia e falhas de transporte em ambiente integrado; fencing local não cancela I/O em andamento |
| C10 — consulta incompatível | [Protocolo](../tests/EmissorFiscal.Core.Tests/ProtocolTests.cs), `AlertExtensionMissingInPinnedConsultationSchemaFailsClosed`, e [ciclo de vida](../tests/EmissorFiscal.Core.Tests/OperationLifecycleTests.cs), `ConsultationWithAlertsOutsidePinnedSchemaKeepsEvidenceAndCannotResubmit` | Prova offline da recusa não comprova resposta do autorizador nem substitui reconciliação operacional |
| C11 — fronteiras do recorte | [Wire/XSD](../tests/EmissorFiscal.Core.Tests/XsdAndWireTests.cs), `OutsideScopeFailsEvenWhenXsdAccepts` e `ConsultationOfAnotherModelIsRejected`; [preparação](../tests/EmissorFiscal.Core.Tests/NfcePreparationTests.cs), `ContextRejectsMissingComponentsProductionIntentAndInvalidCodeTypes` | Provar a recusa em todas as rotas operacionais; eventos/inutilização têm núcleo offline próprio, mas continuam indisponíveis no fluxo inicial do piloto |
| C12 — revogação e trabalhador obsoleto | [Ciclo de vida](../tests/EmissorFiscal.Core.Tests/OperationLifecycleTests.cs), `LateResponseFromAWorkerThatLostItsLeaseIsRefusedAndTheStoreIsUnchanged` | Fencing em memória não é revogação de concessão; falta revalidação de acesso na tarefa, nas credenciais e na gravação do resultado |
| C13 — entrada tributária | [Preparação](../tests/EmissorFiscal.Core.Tests/NfcePreparationTests.cs), `UnsupportedTaxGroupsAreRejectedRatherThanDropped`, `CrtAndTaxGroupsCannotBeSubstitutedAndMissingRtcNeverBecomesZero` e `InvalidRepresentationAndPrecisionAreRejected` | Aprovação de grupo/código/valores para o caso real e regras de totais aplicáveis; validação estrutural não é cálculo nem cobertura integral de regras fiscais |

## Contrato de identidade pendente para EF-07

O [pool](../src/EmissorFiscal.Core/Numbering/NumberPoolKey.cs) recebe uma
identidade fiscal **opaca**, cuja resolução por aliases é responsabilidade de
uma camada futura. Em [FreezeXml](../src/EmissorFiscal.Core/Operations/OperationLifecycle.cs),
a reserva é comparada à série e ao número da chave; o método não relaciona a
identidade opaca ao CNPJ do XML. Assim, a união entre reserva, estabelecimento
e XML não está demonstrada pelo núcleo atual. A arquitetura e a implementação
futuras devem estabelecer e provar esse vínculo com fonte de identidade confiável,
inclusive com outro CNPJ de mesma série/número, antes do uso operacional.
Também devem aplicar autorização por cliente/ação/ambiente e isolamento de conteúdo;
compartilhar um pool não equivale a autorização entre clientes.

## Próximas evidências necessárias

1. Identificar empresa e estabelecimento e obter CRT, cliente, autoridades e
   delegações documentados em registro autorizado, conforme a
   [ficha](PILOT-REGISTRATION.md).
2. Comprovar série, histórico e próximo número; preparar e revisar com responsável
   tributário os casos aplicáveis e suas fontes, sem promover fixtures a decisões.
3. Na EF-07, definir o contrato de identidade e os componentes para persistência,
   autorização, credenciais e trabalhadores; planejar as provas de C04–C09/C12.
4. Executar provas de integração e, quando houver condições e autorização,
   homologação fiscal. Registrar os resultados como marcos distintos.

Este mapa não altera o recorte nem conclui as issues #3 e #4.

Validação local em 18/10/2026: 249 testes das classes de preparação, numeração,
hash, ciclo de vida, DANFE, wire/XSD e protocolo passaram em Docker com SDK
10.0.401. A checagem documental verificou os 13 IDs, links locais e nomes de
testes citados. Esta execução não é homologação nem teste de autorização real.

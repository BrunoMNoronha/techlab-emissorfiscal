# EF-06 — seleção e preparação do primeiro piloto

Atualização: 18/10/2026. Bruno informou que **ainda não há empresa definida**.
Este roteiro operacionaliza a [ficha obrigatória](PILOT-REGISTRATION.md); não a
substitui nem comprova dados de um emitente. A issue [#3](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/3)
continua parcial e aberta.

## Ponto de partida

O recorte já aprovado é NFC-e 65/DF em homologação, emissão normal, venda
presencial com retirada para consumo próprio; o CRT será o da empresa escolhida.
O sistema cliente fornece os dados tributários, o operador central atua mediante
delegação e cada cliente vê seu próprio conteúdo. Série exclusiva depende de
verificação. A aprovação do piloto completo requer emissão, consulta, XML,
protocolo, DANFE e QR; correção, eventos e contingência ficam para etapas
posteriores. A fonte e o alcance das decisões estão em
[DECISIONS.md](evidence/ef06/DECISIONS.md) e no [contrato](PILOT-CONTRACT.md).

## Selecionar uma candidata

1. Identificar candidata com estabelecimento no DF e operação compatível com o
   recorte; confirmar interesse e disponibilidade para colaborar na homologação.
2. Identificar o interlocutor autorizado pela empresa e o responsável que poderá
   validar os casos tributários; verificar como serão comprovados os respectivos
   vínculos e poderes. Interlocução inicial não constitui delegação.
3. Identificar o sistema cliente, o histórico de emissão e quem pode verificar a
   disponibilidade e admissibilidade de uma série exclusiva. Não escolher uma
   série nem calcular o próximo número a partir do maior número observado.
4. Confirmar se a empresa dispõe de registro privado já autorizado, sob sua guarda,
   com acesso aprovado aos revisores. Nele ficam os documentos e fatos reais;
   no repositório, registrar apenas referências publicáveis e o estado da análise.
5. Quando houver candidata, preencher a ficha por evidência: fonte, responsável,
   data, condição e impacto das lacunas. `TBD` continua desconhecido; resposta
   verbal ou exemplo sintético não comprova cadastro, delegação ou tributação.

Não enviar no chat ou versionar PFX, senha, CSC, tokens, documentos privados ou
dados de compradores. Identificar uma candidata não autoriza uso de certificado
ou transmissão; a preparação real segue EF-07 e EF-13 em diante.

## Triagem reproduzível por candidata

Use uma cópia **privada** da ficha para cada candidata. Uma referência pública
sem dados identificáveis pode apontar para esse registro; a referência não é
prova do conteúdo. Em cada linha registre `TBD`, `em análise`, `comprovado` ou
`não atende`, acompanhado de fonte/autoridade e data no registro privado.

| Verificação | Informação ou evidência a obter | Consequência se faltar |
| --- | --- | --- |
| Estabelecimento e recorte | Cadastro do estabelecimento no DF e operação presencial com retirada para consumo próprio | Não se determina elegibilidade para este piloto |
| CRT e situação | CRT vigente e situação cadastral, conferidos pelo responsável | Não se escolhe regime ou grupo tributário |
| Colaboração | Interesse da empresa e interlocutor com vínculo verificável | Não há candidata comprometida para execução |
| Tributação | Responsável tributário e disponibilidade para revisar pelo menos um caso | Caso sem aprovação não entra no piloto |
| Sistemas e acesso | Cliente efetivo, dono do conteúdo, autorizante e possibilidade de delegação documentada | Não se configura acesso nem autoridade |
| Série e histórico | Admissibilidade, exclusividade, coordenação com outros emissores, histórico e próximo número aprovado | Não se inicializa pool real |
| Evidências | Meio privado existente, guarda e acesso autorizado aos revisores | Não se comprova a seleção |
| Condições posteriores | Caminho para verificar credenciamento e custódia de A1 na EF-13, sem pedir credencial agora | Seleção não libera homologação |

Uma candidata somente passa à preparação do caso quando as condições
indispensáveis forem comprovadas ou tiverem um responsável e um próximo passo
registrados. A ficha completa e aprovada continua sendo condição para encerrar
EF-06; a triagem não dispensa seus demais campos.

## Primeiro caso fiscal a preparar com o responsável

Preparar um caso por operação/CRT, usando a seção 3 da ficha como autoridade de
conteúdo. Registrar produto(s), NCM, CFOP, origem e grupos tributários declarados
pelo cliente; condições de destinatário e eventual crédito; valores, pagamento,
totais, benefícios e RTC quando aplicáveis. Identificar fonte, vigência,
equações, limites e decisão do responsável para cada ponto relevante. A falta
de decisão sobre elegibilidade ou crédito bloqueia o caso. Não inferir
classificação a partir de segmento, CNPJ ou fixture.

Depois da revisão privada, preparar exemplo público sintético rastreável ao
caso e executar as validações estruturais pertinentes. A aprovação tributária
deve ficar identificada no registro privado; passar no XSD não a substitui.

## Próximas ações e estados

| Ação | Estado atual | Condição de conclusão |
| --- | --- | --- |
| Indicar empresa candidata | Pendente — nenhuma empresa definida, informado por Bruno em 18/10/2026 | Empresa e estabelecimento identificados em registro autorizado |
| Nomear autorizante e responsável tributário | `TBD` | Vínculo e alcance comprovados |
| Preencher ficha, histórico e série | `TBD` | Evidência e aprovação conforme ficha |
| Definir primeiro caso fiscal | `TBD` | Caso e tributação aprovados pelo responsável |
| Revisar contrato e cenários C01–C13 | Pendente dos fatos | Contrato, cenários e ficha consistentes |
| Decidir arquitetura e custódia | EF-07 permanece bloqueada | Decisões e provas conforme [comparação preparatória](ARCHITECTURE-OPTIONS.md) |

Essas ações não autorizam produção, NF-e 55, contingência, certificado real ou
I/O fiscal. O roteiro pode ser usado antes de escolher uma candidata.

# Incremento local NFC-e - escopo técnico e contrato futuro

**Preparação fiscal documental 0.2, 05/10/2026:** [contrato](PILOT-CONTRACT.md), [dicionário](PILOT-DATA-DICTIONARY.md) e [ficha](PILOT-REGISTRATION.md) atualizam decisões de recorte/acesso/série para o piloto real. Empresa/CRT, autoridades, série/histórico e casos tributários comprovados seguem pendentes. Sem novo DTO/transporte/validação fiscal implementados; distinguir este contrato do simulador v1. As recomendações históricas abaixo são subordinadas às decisões vigentes 0.2.

Referência de pesquisa: **04/10/2026, America/Sao_Paulo**. Prova offline executada
com SDK .NET 10.0.401/runtime 10.0.12: restore, build, 70 testes e CLI passaram
(contagem histórica; estado vigente em 17/10/2026 é 152/152 com runtime 10.0.13,
ver [VALIDATION.md](VALIDATION.md)).
Esta prova não implementa regras tributárias completas nem determina
operações/regimes aceitos. Evidências e limites estão em `VALIDATION.md`.

## Matriz normativa

Publicação no catálogo, revisão escrita no PDF, cronograma anunciado e implantação
efetiva são evidências distintas. As datas abaixo são do material oficial consultado;
nenhum autorizador foi contatado para provar ativação. A SVRS alerta sobre possível
revisão das datas dos documentos publicados em 01/10/2026. Reconferir antes de
qualquer incremento com transmissão.

| Fonte oficial/revisão | Publicação/referência | Teste/homologação anunciada | Produção anunciada | Efeito nesta prova |
| --- | --- | --- | --- | --- |
| [MOC 7 - visão geral](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=1&nomeArquivo=moc7-visao-geral.pdf), [Anexo I](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=1&nomeArquivo=moc7-anexo-i-leiaute-e-rv.pdf) | Catálogo SVRS: 16/12/2020 | Base técnica; não é cronograma único atual | Complementada por NTs posteriores | Namespace/leiaute 4.00; referência a infNFe; RSA-SHA1, SHA1, C14N sem comentários e transform enveloped. DV numérico, seção 2.2.6.2. |
| [MOC 7 - Anexo IV](https://dfe-portal.svrs.rs.gov.br/NFCE/DownloadArquivoEstatico/?sistema=NFCE&tipoArquivo=1&nomeArquivo=moc7-anexo-iv-manual-contingencia-nfc-e.pdf) | Catálogo: 16/12/2020 | Sem implantação provada | Sem implantação provada | Contingência operacional fica fora. Offline aqui significa ausência de transporte, não emissão em contingência. |
| [Manual DANFE NFC-e/QR 6.0](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=k%2FIuuaW4YiY%3D) | Revisão 6.0 no catálogo/PDF nacional | Não é um cronograma de autorizador | Sem ativação provada | Manual contém QR 2; QR 3 segue a NT 2025.001. Nenhum DANFE é renderizado. |
| [NT 2023.002 v1.01](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=T3QyeMfpios%3D) | Catálogo: 18/11/2025 | Mudança v1.01: 19/01/2026 | Até 02/02/2026 | Evolução da NFC-e/eliminação de denegação e lote fiscal. Mantém-se o envelope XSD enviNFe com uma nota e indSinc=1. Emitente CPF/nota avulsa não é suportado nesta prova. |
| [NT 2025.001 v1.01](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=3&nomeArquivo=NT2025.001_v1.01%20-%20NFCe_qrCode_3.pdf), reconferida pela [v1.03 corrigida](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=NvuzQGYd6E8%3D) | v1.01: 26/06/2025; v1.03: 29/09/2025 | QR 3: até 02/06/2025; ajustes v1.03: até 20/10/2025 | QR 3: até 01/09/2025; ajustes v1.03: 03/11/2025 | Online: chave, versão 3, ambiente. Nenhum CSC/hash/assinatura de QR contingência. As revisões posteriores não são omitidas da matriz. |
| [NT conjunta CNPJ 2025.001 v1.00](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=5ZkvIZt10mQ%3D) | PDF: 25/04/2025; catálogo: 08/05/2025 | 06/04/2026 | 06/07/2026 | Strings alfanuméricas; ASCII menos 48 e módulo 11. Exemplo oficial CNPJ 12ABC34501DE35, pp.5-6; algoritmos anexos I/II. Restrições adicionais de letras mencionadas como pendentes na NT não são inventadas. |
| [NT 2026.004 v1.01](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=BTZQzgsO9Ws%3D) | Catálogo: 08/06/2026 | Até 15/06/2026 | 01/07/2026 | Schema de chave/CNPJ alfanuméricos. Seu cronograma específico difere do cronograma anterior da NT conjunta; ambos ficam registrados. |
| [RTC 2025.002 v1.52](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=HXPO8VLbh4o%3D) | Catálogo: 01/10/2026; controle do PDF: 09/2026 | Correção VC02-50: até 05/10/2026 | Até 03/11/2026; VC02-14 adiada para 03/11/2026 | Matrizes/regras RTC são mais amplas que XSD. Preservar tributação recebida; não calcular enquadramento nem declarar cobertura integral da RTC. |
| [NT 2026.002 v1.11](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=m8aACnFLK%2FY%3D) | Catálogo: 01/10/2026; PDF: 09/2026 | Estrutura de alertas: 01/09/2026 | Alertas: 05/10/2026; RVs I08-180, BA02-35 e VC02-40 prorrogadas para 14/12/2026 | 100, 120 e 150 separados; guardar cMsg/xMsg. Alerta não gera correção/reenvio automático. Não implementar modelo 55 ou tpEmis=9. |
| [NT 2026.006 v1.00](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=k7zG06n5M6I%3D) | Catálogo: 25/08/2026 | 05/10/2026 | 03/11/2026 | Campos/evento preparatórios de vinculação de pagamento; o PDF situa ativação efetiva de split payment em 2027. Não implementar grupo YC/evento nem inferir obrigação operacional em 2026. |
| [NT 2026.007 v1.10](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=fpJlwzhffcQ%3D) | Catálogo: 01/10/2026; PDF: 09/2026 | v1.00: 01/09/2026; v1.10: até 05/10/2026 | 03/11/2026 | Evolução cadastral/LCC e validações. Não prova situação cadastral de nenhuma empresa nem autoriza novos regimes. |
| [NT 2026.008 v1.00](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=qAoMot6VyiY%3D) | Catálogo: 01/10/2026 | Campos/RVs: 05/10/2026; NB01-30: 01/02/2027 | 03/11/2026; NB01-30: 01/03/2027 | Campos de valores líquidos e alterações exigirão revisão de schema/provas; não incorporar silenciosamente ao PL010f fixado. |
| [IT 2025.002 v1.70](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=h9o7idH%2BOcI%3D) | Catálogo: 01/10/2026; PDF: 09/2026 | Até 16/10/2026 | Até 16/10/2026 | Tabelas cClassTrib/cCredPres versionadas. Nenhuma tabela de tributação ou escolha de classificação é implementada. |
| [NT 2019.001 v1.64](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=uZcHEjJMQF4%3D), [v1.70](https://www.nfe.fazenda.gov.br/portal/exibirArquivo.aspx?conteudo=suszdTDFNOk%3D), [regras DF](http://static.fazenda.df.gov.br//arquivos/servico-771/Nota_Tecnica_2019.001_regras_adotadas_pelo_DF.pdf) | v1.64: 03/09/2024; v1.70: 18/08/2025; anexo DF identifica v1.20 | Ativação por regra/UF, sem comprovação ao vivo | Não resumir as revisões a uma data única | Anexo DF é histórico e sinaliza adoção de regras de benefício fiscal. Não tratá-lo como cobertura atual completa nem inventar cBenef/enquadramento. Reconciliação normativa pendente. |
| [Ajuste SINIEF 19/16 compilado - CONFAZ](https://www.confaz.fazenda.gov.br/legislacao/ajustes/2016/AJ_019_16) | Texto oficial consultado em 04/10/2026 | Não é teste de endpoint | Efeitos devem ser examinados por alteração/operação | Base jurídica NFC-e; não define operações/regimes da API nem substitui habilitação/certificação reais. |
| [Portaria DF 387/2019 compilada](https://ww1.receita.fazenda.df.gov.br/legislacao/visualizar-legislacao?txtNumero=387&txtAno=2019&txtTipo=7&txtParte=.) | Ato: 20/12/2019; DODF: 23/12/2019; compilação consultada em 04/10/2026 inclui Portaria 998/2025, DODF 16/12/2025 e retificação de 23/12/2025 | Norma local, sem calendário XSD separado | Vigência original na publicação; alterações conforme dispositivo | Arts.2-3 exigem credenciamento; arts.5-B/8 tratam identidade e guarda; art.9 revogado pela 998/2025. Cancelamento/contingência e alcance tributário continuam fora da implementação; não converter normas em operação/regime piloto definido. |
| [AD DF 1/2026 - Assuntos Econômicos](https://ww1.receita.fazenda.df.gov.br/legislacao/visualizar-legislacao?txtNumero=1&txtAno=2026&txtTipo=734&txtParte=.) | Ato: 29/09/2026; DODF: 30/09/2026 | Norma local de benefício fiscal, sem calendário XSD separado | Efeitos a partir de 30/09/2026, art.4 | Altera/consolida anexo do AD 2/2025: acrescenta códigos cBenef e encerra DF814104/DF814151 em 30/09/2026. Portal informa consolidação em atualização e remete ao DODF. Não implementar tabela/enquadramento nesta prova; anexo completo localizado no DODF em 05/10; normalização integral e aplicação por operação exigem revisão antes do uso real. O catálogo também tem AD SUREC 1/2026 sobre GNRE, distinto deste ato. |
| [Receita DF, serviço 781](https://www.receita.fazenda.df.gov.br/aplicacoes/CartaServicos/servico.cfm?codCategoriaServico=96&codServico=781&codSubCategoria=218&codTipoPessoa=7) | Página informa alteração em 19/05/2026 | URL publicada, sem navegação/consulta fiscal | URL publicada | QR: http://www.fazenda.df.gov.br/nfce/qrcode? ; consulta por chave: http://www.fazenda.df.gov.br/nfce/consulta. Mantidos exatamente como publicados. |

Os PDFs públicos usados na pesquisa ficaram em `tmp/pdfs`, fora dos artefatos do
núcleo. Fontes e versões da matriz foram reconferidas com Portal Nacional/SVRS e
legislação oficial DF. As páginas de legislação DF entregam inicialmente o app
Angular; o texto da Portaria compilada e o corpo do AD foram lidos pelo serviço
público de consulta usado por esse app, sem autenticação. HTTP 200 com o app sozinho
não é prova de leitura normativa. O anexo completo de benefícios do AD não foi
reconciliado, e sua aplicação aos futuros regimes/operações continua pendente.

## Baseline XSD reproduzível

| Ordem | ZIP e recorte | SHA-256 do ZIP | Publicação no catálogo |
| --- | --- | --- | --- |
| 1 | PL_009n_NT2023_004_v101_e_NT2019_001_v162.zip | 8e269cd5d5d60532b709c8a88bf590323b94002efd3a49935fb5fd2db5957a62 | 19/03/2024 |
| 2 | PL_010d_v1.03.zip, subárvore Evento (leiaute de eventos 1.00; fonte `PL010dEvento`, acrescentada em 18/10/2026) | 45ceefe4dfbbfec93958283b650a2f1e1734784f4770d070b9907754de081d9b | 10/07/2026 |
| 3 | PL_010d_v1.03.zip, subárvore NFe | 45ceefe4dfbbfec93958283b650a2f1e1734784f4770d070b9907754de081d9b | 10/07/2026 |
| 4 | PL_010f_v1.04.zip | b8589490a58a09a993a80e6ac4d7ed10f20892061ecfc56719337098d4b95998 | 31/08/2026 |

Aplicar sobreposição por nome nessa ordem e guardar somente o fechamento das
dependências dos doze entrypoints (seis da NFC-e, três de evento, três de
inutilização). O manifest lista 20 XSDs, com os hashes de
todos os bytes selecionados e a proveniência das versões sobrescritas. O nome
literal do ZIP 009n contém `v101`; o catálogo descreve NT 2023.004 v1.11. Preservar
ambas as referências sem renomear ou corrigir arquivo oficial.

`nfe`, `enviNFe`, `retEnviNFe` e `procNFe` compartilham leiauteNFe do PL010f.
`consSitNFe` e `retConsSitNFe` usam leiauteConsSitNFe do PL010d/NFe.
`inutNFe`/`retInutNFe` vêm do PL009n e `procInutNFe`/`leiauteInutNFe` do PL010d/NFe;
`envEvento`, `retEnvEvento` e `procEventoNFe` usam leiauteEvento 1.00 e
`tiposBasico_v1.03` da subárvore Evento do PL010d (desde 18/10/2026). Compilar cada
entrypoint separadamente: juntar ambos os leiautes cria tipos homônimos.
O resolver só entrega bytes locais previamente verificados, usando URIs virtuais
sem rede. DTD, resolução externa, schemaLocation na instância, dependência fora
do manifest e reparse points são proibidos.

**Limitação concreta:** o leiaute de consulta fixado não admite cMsg/xMsg em infProt,
embora o leiaute do PL010f de autorização admita até cinco pares. Esta prova aceita
120/mensagens/protocolo de 17 posições no retorno de envio/nfeProc e rejeita extensão
de consulta incompatível. Não editar XSD oficial para contornar isso. Uma baseline
futura deve resolver essa diferença com pacote oficial revisado e testes de regressão.

**Reconferência EF-05 em 18/10/2026:** os 11 XSDs da baseline anterior ao
incremento de evento/inutilização são idênticos às entradas dos três ZIPs oficiais
com hashes conferidos; a NT 2026.002 v1.11 obtida pela SVRS
continua descrevendo alertas também para consulta. Os catálogos nacional e SVRS
foram acessados, sem solução oficial comprovada para a consulta. Fontes,
cronogramas e limites em [EF-05](EF05-NORMATIVE-MATRIX.md#reconferência--18102026).

## Contrato local do núcleo

- `SafeXml.Load`: parsing com DTD proibido, resolver nulo, preservação de whitespace e limite de tamanho.
- `SchemaManifest.Load`/`XsdValidator`: origem/hash/dependências confiáveis, doze conjuntos XSD e recorte adicional 65/53/2/1.
- `AccessKey`: strings, DV numérico/alfanumérico, CNPJ e consistência entre chave e infNFe. Não consulta cadastros.
- `A1Signer.ImportPkcs12`: somente bytes/senha em memória, limites do loader e EphemeralKeySet; exige chave privada RSA de pelo menos 2048 bits, validade temporal e uso digital quando indicado. Esse mínimo é política técnica da prova, não uma escolha de custódia real.
- `A1Signer.Sign`/`Verify`: SignedXml, URI exata #NFe+chave, uma referência, IDs únicos, algoritmos/transforms explícitos e certificado embutido. `SignElement`/`VerifyElement` aplicam as mesmas regras a `evento`/`infEvento` (leiaute 1.00) e `inutNFe`/`infInut`, com a Signature anexada ao contêiner direto. Sem algoritmos alternativos automáticos. Verificação local com verifySignatureOnly=true não consulta cadeia/revogação nem comprova ICP-Brasil/titularidade.
- `QrCode3`: somente URL online DF/chave|3|2; não renderiza a imagem e não pede CSC.
- `NfceMessages`: enviNFe com uma nota/indSinc=1 e consSitNFe; valida assinatura após envelope.
- `NfceProtocol`: respostas completas, schema válido, chave/ambiente e digVal compatíveis; nProt string de 15 ou 17 posições. nfeProc só é montado depois dessa correlação. XML/protocolo histórico de outra nota não autoriza o original.

100, 120 e 150 viram estados técnicos distintos, preservando mensagens. No envio,
o contêiner deve informar 104 com protocolo individual. Na consulta, códigos do
contêiner e protocolo devem concordar; estado diferente exige reconciliação de
eventos, fora deste incremento. Uma allowlist limitada de rejeições conhecidas é
tratada como `Rejected`; 204/539 exigem reconciliação e códigos não mapeados são
`Unknown`. 103/105 sem protocolo ficam `Processing`; 217 sem protocolo fica
`NotFound`, sem permissão de reenvio. XML truncado/incompatível é `InvalidResponse`.

Autorização técnica neste código é interpretação de uma resposta fornecida, não
prova de origem/autenticidade de uma resposta da SEFAZ. Transporte autenticado,
persistência e reconciliação durável terão seus próprios gates no incremento real.
HTTP 200, mensagem positiva, protocolo de outra chave ou retorno sem digVal não
substituem a correlação. Não há caminho de reenvio automático.

Na integração futura, somente o transporte confiável pode fornecer retornos para
transições fiscais, vinculados à tentativa durável. O sistema cliente não pode
autorizar uma operação fornecendo XML/protocolo próprio. O aceite da API deve
incluir um teste de injeção desse retorno sem mudança para estado autorizado.

Três hashes têm finalidades separadas:

| Valor | Finalidade | Representação |
| --- | --- | --- |
| requestHash futuro | Vincular conteúdo estável de uma solicitação à idempotência; canonicalização/versão devem ser definidas antes de implementar API | SHA-256 do contrato canonicalizado; não implementado nesta prova |
| XmlSha256 local | Integridade dos bytes UTF-8 do XML serializado específico; envelope/nfeProc têm bytes distintos | SHA-256 hexadecimal |
| DigestBase64 fiscal | Digest de infNFe calculado pelos transforms da referência SignedXml; comparado a digVal | SHA-1 em Base64, conforme MOC |

## API futura - contrato mínimo proposto, sem implementação

Não há endpoint ou banco nesta tarefa. Antes de expor API, fechar autenticação,
autorização por emitente, identidade canônica, custódia de A1, regimes/operações,
destino de execução e política de dados. Esses itens seguem `TBD`.

Uma solicitação futura de emissão precisa, no mínimo, de:

- Identidade do sistema cliente autenticado e empresa/emitente autorizado, derivados do contexto servidor; jamais confiar no tenant informado só no corpo.
- Identificador estável da operação, ambiente/modelo, emitente fiscal canônico e referência do estabelecimento; resolução de aliases com trilha de auditoria.
- Dados do documento e tributação recebida do sistema cliente; sem reclassificação/calculadora implícita. Contrato/versionamento desses dados ainda precisa ser fechado.
- Referência à configuração fiscal/credencial autorizada, sem senha, PFX ou CSC em logs, exemplos e respostas públicas. Custódia real permanece TBD.
- Versão normativa imutável, requestHash e fingerprint da revisão de conteúdo. Mesma operação com conteúdo diferente gera conflito, não nova transmissão.

A resposta precisa identificar operação/revisão, estado local/fiscal, códigos e
motivos literais, mensagens de alerta, chave e protocolo como strings, versão
normativa e integridade dos artefatos. Recuperação de XML exige autorização por
empresa. Consulta futura recebe referência da operação/chave e não pode iniciar
nova emissão por conta própria. Nenhum nome de rota/contrato publicado é definido
por este documento.

## Estados, isolamento, numeração e reconciliação do próximo incremento

Persistência futura deve separar preparação/reserva/assinatura de envio, resultado
fiscal e reconciliação. Transições propostas: recebido -> validado localmente ->
numeração reservada -> assinado -> tentativa registrada -> resultado conhecido ou
desconhecido -> reconciliação. Autorizações, alertas e fora do prazo preservam seus
detalhes. Rejeição não apaga tentativas. LocalInvalid, Processing, NotFound e Unknown
não se tornam Authorized por heurística. Falhas de infraestrutura não são rejeições
fiscais.

A reserva exige transação/uniqueness sobre identidade fiscal canônica, modelo,
ambiente, série e número; aliases do mesmo CNPJ não podem gerar pools independentes.
Adicionar controles de isolamento por empresa em todas as operações/artefatos.
Política de propriedade de um CNPJ em múltiplos cadastros/tenants, séries já usadas,
importação de numeração legada e reutilização após rejeição seguem `TBD`.
`idLote=1`/`nNF=1` das fixtures não é algoritmo de reserva.

Trabalhadores futuros devem manter lease com fencing e registrar intenção/tentativa
duravelmente antes do I/O. Guardar XML assinado e parâmetros da tentativa sem
alteração. Perda de lease, timeout ou crash após envio pode deixar resultado
desconhecido; outro trabalhador não pode inferir falha nem transmitir revisão nova.
Reconciliar chave/protocolo/digest/ambiente antes de definir qualquer repetição.
Política de repetição, consultas, intervalos e limites dependerá da matriz vigente.

Fencing protege transições locais e impede que um trabalhador obsoleto altere o
estado como se ainda fosse o dono. Não cancela I/O já em voo no autorizador nem
garante sozinho exclusão de efeitos externos. Ao recuperar uma operação, manter
o XML/revisão, registrar respostas tardias e reconciliar possível duplicidade,
inclusive quando um trabalhador antigo retoma após perder o lease.

| Prova obrigatória futura | Cenário e aceite mínimo |
| --- | --- |
| Isolamento entre empresas | Cliente A tenta emitir, consultar, baixar XML/protocolo ou usar credencial de B: negar em todos os caminhos, inclusive processamento assíncrono e erro. |
| Aliases do mesmo CNPJ | Dois aliases/clientes autorizados usam identidade/pool únicos; conforme EF-06/0.2, cada cliente vê suas próprias operações, sem compartilhamento automático de conteúdo. Delegação/concessões reais ainda precisam de evidência. |
| Reserva concorrente | Emissões simultâneas disputam a mesma série/ambiente: exclusividade e atomicidade, sem reserva dupla ou perda de vínculo entre número e operação. |
| Replay após mudança normativa | Repetir operação antiga retorna o mesmo snapshot/artefatos/versão normativa; não regenerar XML pela norma nova. Novo conteúdo na mesma operação gera conflito. |
| Perda de lease durante I/O | Trabalhador antigo não confirma nova transição local nem envia nova revisão depois de perder lease. Testar retomada do trabalhador, envio já em voo e resposta tardia/duplicidade; fencing local não cancela I/O externo, e a mesma revisão/XML permanece disponível para reconciliação. |
| Crash após envio | Intenção/XML/tentativa sobrevivem ao crash. Recuperação inicia reconciliação; não reserva novo número nem altera o XML automaticamente. |
| Resultado desconhecido | Timeout, resposta truncada, 217 isolado ou código novo mantêm incerteza auditável. Protocolo/chave/ambiente/digest incompatíveis nunca autorizam. |
| Correção após rejeição | Conforme EF-06/0.2, indisponível no primeiro piloto: preservar rejeição/hashes/tentativas e recusar correção/reutilização. Revisão explícita permanece evolução futura com política de número fundamentada; não sobrescrever original. |

## Decisões/TBDs preservados

.NET 10 é a stack desta prova local, escolhida no pedido; infraestrutura completa,
banco e stack operacional futura não foram escolhidos. Operações, regimes,
permissões, custódia real, retenção, credenciamento, numeração legada, produção e
autorização de uso de empresas/certificados reais continuam TBD. Não há migração,
API, evento fiscal, DANFE, contingência operacional ou transporte nesta entrega.

## Revisão EF-05 — 05/10/2026

A [matriz EF-05](EF05-NORMATIVE-MATRIX.md) atualiza a triagem normativa anterior. Nove ZIPs examinados não trouxeram consulta oficial compatível com alertas. Baseline e comportamento preservados; issue #2 permanece aberta. O anexo DF foi localizado integralmente, mas sua reconciliação linha a linha e aplicação dependem da EF-06. Há divergências PDF/XSD sobre cMsg e entre N12-90 nacional/DF; aceitação estrutural não resolve semântica fiscal. Nenhuma ativação real foi comprovada.

## Contrato documental do piloto — EF-06/0.2

[PILOT-CONTRACT.md](PILOT-CONTRACT.md) registra as decisões humanas vigentes: presencial/retirada/consumo próprio, regime conforme empresa comprovada, série exclusiva validada, conteúdo próprio por cliente e operador central delegado. Correção/eventos/contingência adiados; emissão/consulta/XML/protocolo/DANFE/QR no marco futuro. [Dicionário](PILOT-DATA-DICTIONARY.md) deriva precisão/cardinalidade dos XSDs sem copiar limites da fixture; equações/tolerâncias/regras precisam de fonte e caso aprovado. [Ficha](PILOT-REGISTRATION.md) exige fatos/autoridades/série/casos reais. Pesquisa 0.2 e tentativa histórica coexistem. EF-06 parcial; nenhuma mudança no núcleo, baseline, resolver ou interpretação.

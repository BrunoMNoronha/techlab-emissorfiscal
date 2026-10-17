# Mapeamento documental TISS → NFS-e (de-para de campos)

Documento preparado em 17/10/2026 (America/Sao_Paulo). Define o de-para conceitual entre um lote TISS 4.01.00 (faturamento de prestador de saúde para operadora de plano) e os campos exigidos para emissão de **NFS-e no padrão nacional** (leiaute DPS — Declaração de Prestação de Serviços). É insumo de planejamento para uma futura funcionalidade de importação; **não publica DTO, JSON Schema, rotas, códigos HTTP, enums de API ou persistência; não implementa parser, banco, transporte ou emissão real.**

**Privacidade (revisão de 17/10/2026, conforme decisão T01):** os identificadores do caso real de referência (código do prestador na operadora, registro ANS, CNES, números de lote/guia e totais) foram substituídos neste documento por placeholders estruturalmente equivalentes; os valores reais permanecem apenas no registro privado do emitente, fora do Git. O arquivo XML de origem não é versionado. **Limite conhecido:** revisões anteriores deste documento no histórico Git ainda contêm os identificadores originais; o expurgo do histórico é ação destrutiva e permanece decisão futura, não executada.

## 1. Escopo, caso analisado e advertências

**Fonte analisada:** mensagem `ans:mensagemTISS` versão de padrão `4.01.00`, transação `ENVIO_LOTE_GUIAS`, contendo exclusivamente guias `ans:guiaSP-SADT`. Caso real de referência (identificadores substituídos por placeholders; valores reais no registro privado T01): lote `L-0001` (arquivo `L0001_001.xml`), com dezenas de guias SP-SADT de laboratório de patologia/citologia, prestador código `NNNNNNNNNN` na operadora de registro ANS `NNNNNN`, CNES `NNNNNNN`, UF 53 (DF), procedimentos da tabela TUSS 22 e epílogo com hash MD5.

**Fora do escopo deste mapeamento:** guias de consulta e de resumo de internação, anexos clínicos, recurso de glosa, demonstrativos de pagamento/análise de conta e qualquer outra transação TISS. Odontologia e demais versões do padrão TISS não foram avaliadas.

**Alvo:** NFS-e do Sistema Nacional NFS-e, leiaute **DPS**. Conforme comunicado da Secretaria de Economia do GDF, a emissão no padrão nacional tornou-se obrigatória no DF em **01/10/2026**, com o padrão ABRASF 2.04 do ISSNet deixando de ser aceito (os dois conviveram até 30/09/2026). Fontes: [comunicado GDF de 04/11/2025](https://static.fazenda.df.gov.br/arquivos/servico-1202/comunicado_NFSe_padrao_nacional_04-11-2025.pdf) e [notícia COAD sobre o ambiente de produção](https://coad.com.br/home/noticias-detalhe/139146/governo-do-distrito-federal-informa-sobre-o-ambiente-de-producao-da-nfs-e-no-padrao-nacional). **Gate de implementação:** confirmar no portal da Secretaria de Economia do DF o ambiente vigente (emissor nacional × webservice próprio compartilhando com o Ambiente Nacional de Dados) e a versão do leiaute antes de qualquer código. Os nomes de grupos/campos DPS citados abaixo seguem o leiaute 1.00 e são **indicativos**: a grafia e a obrigatoriedade exatas devem ser conferidas no Anexo I (leiaute DPS) da documentação técnica oficial em gov.br/nfse, inclusive as adequações da reforma tributária (IBS/CBS, NT 004) quando aplicáveis à competência emitida.

**Este faturamento não gera NF-e nem NFC-e.** Serviço de laboratório prestado a operadora de plano de saúde é fato gerador de ISS (LC 116/2003); o documento é a NFS-e. O recorte atual deste repositório (NFC-e modelo 65/DF, mercadorias — ver [ROADMAP](ROADMAP.md) e [contrato do piloto](PILOT-CONTRACT.md)) não cobre NFS-e; este documento não altera aquele recorte.

**Conclusão central:** o lote TISS **não é suficiente** para emitir a NFS-e. Ele fornece o conteúdo do faturamento (valores, datas, guias, procedimentos). Identificação fiscal do prestador e do tomador, enquadramento tributário, alíquotas e retenções vêm de **cadastro** e de **decisão tributária parametrizada**, classificados separadamente na seção 4.

## 2. Granularidade da nota

| Opção | Comportamento | Avaliação |
| --- | --- | --- |
| **1 NFS-e por lote TISS** (recomendada como padrão) | Uma nota com `vServ` = soma dos `ans:valorTotalGeral` das guias; guias discriminadas em `xDescServ` (número do lote, guias, datas, TUSS, valores) | Prática comum no faturamento a operadoras; menor volume de notas; exige regra para lotes com competências mistas (seção 6) |
| 1 NFS-e por guia | Uma nota por `ans:guiaSP-SADT`, com `vServ` = `ans:valorTotalGeral` da guia | Rastreabilidade máxima guia↔nota; volume alto (uma nota por guia — dezenas no caso de referência); facilita glosa parcial |

A escolha é do emitente com seu contador (`TBD`); a futura importação deve tratá-la como configuração explícita, nunca como default silencioso. O de-para da seção 3 indica a agregação no modo por lote; no modo por guia, os mesmos campos valem com escopo de uma guia.

## 3. De-para principal — campos com origem no TISS

XPaths relativos a `ans:mensagemTISS` (cabeçalho) e a cada `ans:guiaSP-SADT` (guias). Todos os campos abaixo foram conferidos no arquivo real do caso de referência.

| Campo NFS-e (grupo DPS) | Origem TISS (XPath) | Observações/transformação |
| --- | --- | --- |
| `infDPS/dCompet` (competência) | `ans:procedimentosExecutados/ans:procedimentoExecutado/ans:dataExecucao`; subsidiariamente `ans:cabecalho/.../ans:dataRegistroTransacao` | Competência = mês da prestação. Lotes podem misturar meses (seção 6); a regra de desempate (data de execução × data do envio do lote) é decisão do emitente, registrada por importação. |
| `serv/cServ/xDescServ` (discriminação) | `ans:loteGuias/ans:numeroLote`; por guia: `ans:cabecalhoGuia/ans:numeroGuiaPrestador`, `ans:dadosAutorizacao/ans:numeroGuiaOperadora`, `ans:dataExecucao`, `ans:procedimento/ans:codigoTabela` + `ans:codigoProcedimento` + `ans:descricaoProcedimento`, `ans:quantidadeExecutada`, `ans:valorUnitario`, `ans:valorTotal` | Texto montado pela importação (ex.: "Lote TISS L-0001 — guia NNNNNNNNN, 14/07/2025, TUSS 22/40601293 ..."). Respeitar o limite de tamanho do campo no leiaute (conferir no Anexo I); lote grande pode exigir truncamento com referência externa. **Não incluir dados de beneficiário** (seção 5). |
| `valores/vServPrest/vServ` (valor do serviço) | Soma de `ans:valorTotal/ans:valorTotalGeral` das guias do lote (modo por guia: o valor da guia) | Conferir, por guia, `valorTotalGeral` = `valorProcedimentos` = soma dos `ans:procedimentoExecutado/ans:valorTotal`, e cada item `valorTotal` = `valorUnitario` × `quantidadeExecutada` × `reducaoAcrescimo`. Divergência bloqueia a importação; a soma calculada não substitui silenciosamente o valor declarado. Decimal exato (duas casas), nunca ponto flutuante binário. |
| Referência de conciliação (metadado interno, fora do XML da NFS-e) | `ans:identificacaoTransacao/ans:sequencialTransacao`, `ans:numeroLote`, `ans:epilogo/ans:hash` | Vincula nota ↔ lote para auditoria e idempotência da importação (reimportar o mesmo lote não pode gerar nota duplicada). O hash MD5 do epílogo é integridade do padrão TISS, não evidência fiscal. |
| Identificação do tomador — chave de resolução | `ans:cabecalho/ans:destino/ans:registroANS` (seis dígitos, ex. fictício `NNNNNN`) | O TISS só identifica a operadora pelo registro ANS. A importação resolve registro ANS → cadastro do tomador (CNPJ, razão social, endereço, IM — seção 4). Não inferir CNPJ do registro ANS. |
| Identificação do prestador — chave de resolução | `ans:origem/ans:identificacaoPrestador/ans:codigoPrestadorNaOperadora` (ex. fictício `NNNNNNNNNN`); `ans:dadosExecutante/ans:CNES` | Código interno na operadora e CNES confirmam **qual** prestador/estabelecimento fatura, mas não substituem CNPJ/IM do cadastro (seção 4). Divergência entre o prestador configurado e o do lote bloqueia a importação. |

Campos monetários e quantidades do TISS no caso de referência usam ponto decimal e até duas casas (ex. fictício `123.45`, `001`); a importação deve preservar o valor exato recebido.

## 4. Campos sem origem no TISS — cadastro e decisão tributária

Nada abaixo existe no lote TISS; preencher por default ou herdar de exemplo é proibido.

### 4.1 Cadastro (evidência cadastral do emissor)

| Campo NFS-e (grupo DPS) | Conteúdo | Fonte esperada |
| --- | --- | --- |
| `prest/CNPJ`, `prest/IM` | CNPJ e inscrição municipal (CF/DF) do laboratório | Cadastro do emitente; comprovante CNPJ/CF-DF |
| `prest/regTrib` (`opSimpNac`, `regApTribSN`, `regEspTrib`) | Opção pelo Simples Nacional e regime especial | Comprovação do regime efetivo da empresa; não presumir |
| `infDPS/cLocEmi`, `serv/locPrest/cLocPrestacao` | Código IBGE do município emissor/da prestação (DF: `5300108`) | Cadastro do estabelecimento; conferir com endereço comprovado |
| `infDPS/serie`, `infDPS/nDPS`, `infDPS/tpAmb`, `infDPS/tpEmit` | Série, numeração, ambiente (produção/homologação) e tipo de emitente (prestador) | Configuração do emissor; numeração controlada pelo sistema, nunca pelo lote |
| `toma/CNPJ`, `toma/xNome`, `toma/end`, `toma/IM` | Dados da operadora tomadora | Tabela auxiliar **registro ANS → cadastro do tomador**, mantida como dado cadastral com fonte (consulta ANS/CNPJ). Para o caso de referência: registro ANS no registro privado T01, cadastro `TBD` |
| Certificado digital e credenciamento | Certificado ICP-Brasil do prestador e acesso ao ambiente de emissão do DF | Custódia própria; fora do conteúdo da nota e deste mapeamento |

### 4.2 Decisão tributária (parametrização com responsável; tudo `TBD` até aprovação do contador)

| Campo NFS-e (grupo DPS) | Decisão necessária | Observações |
| --- | --- | --- |
| `serv/cServ/cTribNac` | Código de tributação nacional (item LC 116/2003 + desdobro) | Serviços de análises clínicas/patologia enquadram-se tipicamente no item **4.02** da LC 116; o código de 6 dígitos exato é decisão do contador, a confirmar |
| `serv/cServ/cTribMun` | Código de tributação municipal do DF, quando exigido | Lista do DF; depende do `cTribNac` escolhido |
| `trib/tribMun/pAliq` | Alíquota de ISS aplicável no DF | Depende do enquadramento e do regime (Simples × normal); nunca constante de código |
| `trib/tribMun/tribISSQN`, `cLocIncid` | Situação de incidência e local | Regra geral: ISS devido no local do estabelecimento prestador; exceções da LC 116 avaliadas por caso |
| `trib/tribMun/tpRetISSQN` | ISS retido ou não pelo tomador | Operadoras frequentemente retêm ISS conforme a lei municipal; decisão por tomador |
| `trib/tribFed` (`vRetIRRF`, `vRetCSLL`, PIS/COFINS e `vRetCP` quando couber) | Retenções federais no pagamento por PJ (IR; CSRF — PIS/COFINS/CSLL) | Usuais no faturamento a operadoras; bases, alíquotas e dispensas (ex.: Simples) são decisão do contador |

## 5. Campos do TISS deliberadamente sem destino fiscal

| Grupo/campo TISS | Motivo do descarte |
| --- | --- |
| `ans:dadosBeneficiario` (`numeroCarteira`, `atendimentoRN`) | **Dado pessoal de saúde (LGPD, art. 11): não deve constar da NFS-e** nem da discriminação do serviço |
| `ans:profissionalSolicitante` (nome, conselho, número, UF, `CBOS`) | Dado assistencial/pessoal, irrelevante para o fato gerador do ISS |
| `ans:dadosSolicitacao` (`caraterAtendimento`, `indicacaoClinica`) e `ans:dadosAtendimento` (`tipoAtendimento`, `indicacaoAcidente`, `regimeAtendimento`) | Classificação assistencial TISS, sem correspondente fiscal |
| `ans:dadosAutorizacao/ans:dataAutorizacao`, `ans:horaInicial`/`ans:horaFinal` | Operacionais; a data fiscal relevante é a de execução |
| `ans:reducaoAcrescimo` | Fator já refletido no `valorTotal` do item; usado apenas na conferência aritmética da seção 3 |
| `ans:epilogo/ans:hash` | Integridade do arquivo TISS; guardado só como metadado de conciliação |

## 6. Regras de consistência e pendências

Validações mínimas que a futura importação deve prever (bloqueantes, com mensagem identificando a guia):

1. **Aritmética por guia e por lote** — item: `valorTotal` = `valorUnitario` × `quantidadeExecutada` × `reducaoAcrescimo`; guia: `valorProcedimentos` = soma dos itens e `valorTotalGeral` coerente; lote: `vServ` = soma das guias. No caso de referência todas as somas conferiram, sem divergência entre `valorTotalGeral` e `valorProcedimentos` (conferido em 17/10/2026 sobre o arquivo real; totais no registro privado T01; a implementação deve recalcular, não herdar valores).
2. **Guias duplicadas** — `numeroGuiaPrestador` repetido no mesmo lote, ou lote/`sequencialTransacao` já importado (idempotência), bloqueia.
3. **Competências mistas** — o caso de referência contém execuções de julho/2025 e de novembro/2025 no mesmo lote. No modo "1 nota por lote", a importação deve recusar ou dividir por competência conforme regra configurada; não escolher silenciosamente um mês.
4. **Prestador coerente** — `codigoPrestadorNaOperadora` e `CNES` do lote devem corresponder ao emitente configurado.
5. **Versão do padrão** — `ans:Padrao` diferente de `4.01.00` (ou da faixa homologada) é recusado, não interpretado por aproximação.

Pendências (`TBD`) que condicionam qualquer implementação: confirmação do ambiente/leiaute vigente no DF e dos nomes exatos dos campos no Anexo I oficial; granularidade escolhida (seção 2); parametrização tributária aprovada por contador (seção 4.2); cadastro do tomador por registro ANS (seção 4.1); política para competências mistas; tratamento posterior de glosas (nota emitida × pagamento glosado pela operadora) — fora deste recorte.

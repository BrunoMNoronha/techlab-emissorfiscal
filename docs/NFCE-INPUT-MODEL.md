# Modelo de entrada e dados de preparação de NFC-e

Preparado em 05/10/2026; revisado em 17/10/2026 com o incremento P1 (ST retido
`ICMSSN500`/`ICMS60` com CEST condicional e exemplo `sale-st-crt1.json`,
issue [#24](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/24)).

Contrato executável local **`nfce-preparation/0.1`**, para preparar XML de NFC-e 65/DF em homologação, com emissão normal e dados sintéticos. O novo modelo separa venda, perfil do emitente e contexto de teste; não usa a antiga fixture como molde de uma venda recebida. Permite avançar na preparação e nos testes antes da seleção da empresa real. Não cadastra contribuinte, reserva numeração fiscal, transmite, consulta ou obtém autorização; `fiscalIo` permanece desabilitado.

O [contrato documental `nfce-df-homologacao/0.2`](PILOT-CONTRACT.md) registra as decisões e condições do piloto real. O `SaleRequest` v1 da integração simulada local anterior é outro contrato. `nfce-preparation/0.1` não substitui essas versões nem habilita implicitamente seus fluxos. O [dicionário fiscal](PILOT-DATA-DICTIONARY.md) e o [manifesto XSD](../schemas/nfe/4.00/manifest.json) continuam referências de precisão, estrutura e limites da baseline.

## 1. Três modelos e responsabilidade pelos dados

Os três arquivos exigem `version = "nfce-preparation/0.1"`. O parser usa `PreparationJson.ParseSale`, `ParseEmitter` e `ParseContext`; `NfcePreparation.BuildUnsigned(sale, emitter, context)` devolve `PreparedNfce`, contendo `AccessKey` e `UnsignedXml`. Essa separação é um contrato local de preparação, sem autenticação ou concessões de uma API pública.

Os nomes e tipos públicos estão em [PreparationModels.cs](../src/EmissorFiscal.Core/Preparation/PreparationModels.cs). A venda contém `reference`, `emitterProfileId`, `issuedAt`, `operation`, `recipient` quando presente, `items`, `totals`, `payments`, `change` e `additionalInfo` opcionais. O perfil contém `profileId`, `cnpj`, `name`, `fantasyName` opcional, `stateRegistration`, `crt`, `series`, `fiscalMunicipality`, `processVersion` e `address`. O contexto contém `syntheticOnly`, `number` e `numericCode`, além da versão. `emitterProfileId` referencia o perfil informado; não comprova autenticação ou concessão.

| Modelo | Quem fornece | Conteúdo | Limite desta entrega |
| --- | --- | --- | --- |
| Venda | Sistema cliente; nos exemplos, autor do teste | Referência, data/hora, natureza/operação, destinatário e sua condição, itens, valores comerciais/tributáveis, tributação declarada, totais, pagamentos e informações complementares | Valores recebidos são preservados. O preparador não escolhe enquadramento, NCM, CFOP, CST/CSOSN, benefício ou alíquota |
| Perfil do emitente | Cadastro/configuração do servidor; no teste, arquivo sintético explícito | Identidade de estabelecimento, CNPJ/IE, nomes, endereço, CRT e série | Não é cadastro comprovado ou configuração de empresa real. Perfil sintético não concede acesso nem comprova série disponível |
| Contexto de teste | Executor local de teste | Marcação sintética, número da nota e código numérico usados na chave | Número não é reserva durável, série não vem da venda e nenhum valor autoriza transporte fiscal |

Em uma integração operacional futura, identidade do cliente, propriedade, emitente autorizado, concessões, ambiente, série, número reservado, baseline e referência de credencial pertencem ao servidor. O cliente informa a referência autorizada e os dados da venda. Ler arquivos locais não demonstra esse vínculo; aliases e delegação continuam sujeitos ao contrato documental.

## 2. Levantamento de campos e destino no XML

**O** significa obrigatório no recorte de preparação; **C**, condicionado ao caso, grupo ou presença de dados. As linhas descrevem conceitos fiscais; a forma exata dos arquivos segue os modelos e exemplos versionados. Um campo opcional no XSD pode ser obrigatório por regra fiscal. A variante que exige grupo não representado precisa ser recusada, sem omitir a informação para obter um XML válido.

| Campo/conceito | Dono/origem | Necessidade | Destino XML / uso |
| --- | --- | --- | --- |
| Versão e referência da venda | Cliente | O | Versão interpreta o arquivo; referência identifica o teste/negócio, sem substituir `nNF` ou a chave |
| Data/hora de emissão `issuedAt` | Cliente | O, com segundos e deslocamento explícito | `ide/dhEmi`; ano/mês participam da chave. Preservar o deslocamento, sem completar pelo relógio do servidor |
| Natureza da operação | Cliente | O | `ide/natOp`; texto recebido não determina por si só elegibilidade fiscal |
| Modelo, UF, ambiente e tipo de emissão | Servidor/recorte | O: `65`, `53`, `2`, `1` | `ide/mod`, `ide/cUF`, `ide/tpAmb`, `ide/tpEmis`; parâmetros não abrem produção, outra UF ou contingência |
| Saída, destino, consumidor final e presença | Cliente classifica; servidor confronta | O no recorte presencial/retirada/consumo próprio | `ide/tpNF`, `idDest`, `finNFe`, `indFinal`, `indPres`; retirada não permite inferir compra presencial |
| Município do fato gerador | Perfil/configuração servidor | O | `ide/cMunFG`; município IBGE, sem derivar arbitrariamente de CEP ou texto do endereço |
| Série `series`, número `number` e código `numericCode` | Série: perfil servidor; número/código: contexto de teste | O | `ide/serie`, `nNF`, `cNF`; chave/`cDV` derivados dos dados. Contexto sintético não inicializa pool real |
| Processo emissor e versão do programa | Servidor | O | `ide/procEmi`, `verProc`; não representam credenciamento ou homologação do software |
| CNPJ `cnpj` e identidade `profileId` do estabelecimento | Perfil servidor | O | `emit/CNPJ` e chave; ID do perfil vincula os arquivos. Identificador fiscal textual; preservar formato numérico/alfanumérico suportado pela baseline |
| Nome `name` e fantasia `fantasyName` do emitente | Perfil servidor | Nome O; fantasia C | `emit/xNome`, `xFant`; exemplos públicos indicam explicitamente identidade sintética |
| IE `stateRegistration` e CRT `crt` | Perfil servidor | O no recorte; CRT 1 e 3 têm casos separados | `emit/IE`, `CRT`; ausência de IE estruturalmente admitida não autoriza NFC-e sem IE. CRT não é escolhido pela presença de um grupo tributário |
| Endereço `address` do emitente | Perfil servidor | O, incluindo `postalCode` no XSD fixado | `street`, `number`, `district`, `municipalityCode`, `municipalityName`, `uf`, `postalCode` mapeiam `enderEmit/xLgr`, `nro`, `xBairro`, `cMun`, `xMun`, `UF`, `CEP`; `complement`, `countryCode`, `countryName`, `phone` são condicionados |
| Destinatário `recipient`: `documentType`, `document`, `name` | Cliente | C conforme fonte/caso; tipo CPF/CNPJ explícito | `dest/CPF` ou `CNPJ`, `xNome`; nome/indicadores independentes. CPF/CNPJ não determina direito a crédito |
| `recipient.indIEDest` e `creditIntent` | Cliente declara a hipótese do teste | O quando há destinatário no modelo; admite somente não contribuinte `9` no recorte implementado | `dest/indIEDest`; `creditIntent` é metadado local, sem tag ou aprovação tributária. Intenção `yes`/condição `unknown` não é elegibilidade aprovada; a variante real exige classificação fundamentada, além da intenção |
| Endereço do destinatário | Cliente | C conforme fonte/caso | `dest/enderDest`; os casos desta entrega são presenciais com retirada. Contratação remota/entrega não é habilitada por aceitar endereço |
| Código e descrição do produto | Cliente | O | `det/prod/cProd`, `xProd`; ordem do item preservada e `nItem` sequencial. Homologação tem regra específica para a descrição do primeiro item |
| GTIN comercial e tributável | Cliente | O na estrutura; declaração/exceção conforme produto | `cEAN`, `cEANTrib`; declarar `SEM GTIN` no exemplo sem GTIN não o torna valor padrão para produtos reais |
| NCM, CFOP, origem e classificação complementar | Cliente | O/C conforme caso | `NCM`, `CFOP`, `imposto/ICMS/*/orig`; `CEST`, `cBenef` e outros condicionados não são inventados ou ignorados |
| Unidade, quantidade e preço comercial | Cliente | O | `uCom`, `qCom`, `vUnCom`; quantidade até 4 casas e preço unitário até 10 conforme tipos XSD |
| Unidade, quantidade e preço tributável | Cliente | O, distintos dos comerciais | `uTrib`, `qTrib`, `vUnTrib`; sem pressupor unidade igual ou calcular fator de conversão |
| Valor bruto e inclusão no total | Cliente | O | `vProd`, `indTot`; campos explicitamente declarados, sem substituir valor bruto por cálculo local |
| Desconto, frete, seguro e outras despesas do item | Cliente | C | `vDesc`, `vFrete`, `vSeg`, `vOutro`; ausência e zero não são intercambiáveis em todos os tipos XSD |
| `items[].taxes.ICMS` | Cliente | O no caso representado | `group`, `orig`, CST/CSOSN e campos do grupo em `imposto/ICMS`; CRT 1 não implica CSOSN 102 e CRT 3 não implica CST 00 |
| `items[].taxes.PIS` e `COFINS` | Cliente | O no recorte de preparação; campos conforme grupo | `group`, CST e base/alíquota/valor em `imposto/PIS/*`, `COFINS/*`; presença de `minOccurs=0` não decide dispensa. Preservar dados declarados |
| `items[].taxes.IBSCBS` e classificação RTC | Cliente | Conforme CRT/data/modelo e regras aplicáveis | `imposto/IBSCBS`: CST, `cClassTrib`, `gIBSCBS/vBC`, `gIBSUF`, `gIBSMun`, `vIBS` e `gCBS`; não omitir porque o XSD permite ausência |
| `items[].taxes.vTotTrib` e `items[].vItem` | Cliente | C; `vItem` exigido no subconjunto RTC desta preparação | `imposto/vTotTrib`, `det/vItem`; valores declarados, sem transformar a exigência local de exemplo completo em RV universal |
| Totais ICMS e componentes da nota | Cliente | O/C conforme leiaute/caso | `total/ICMSTot`: bases, ICMS/ST/FCP, produtos, frete, seguro, desconto, II/IPI/PIS/COFINS, outras despesas e `vNF` |
| Totais RTC e valor total com RTC | Cliente | C; presentes nos casos RTC | `IBSCBSTot` e `vNFTot`, com seus grupos/valores; não assumir que são idênticos a `vNF` em qualquer operação |
| Modalidade de frete | Cliente | O no grupo de transporte | `transp/modFrete`; retirada sem transporte no exemplo é declaração de teste, sem abrir entrega ou contratar transportador |
| Pagamentos e troco | Cliente | O/C conforme caso | `pag/detPag`: `indPag`, `tPag`, `vPag`; dados eletrônicos em `card` quando aplicáveis e `vTroco` quando declarado |
| `additionalInfo` da venda ou item | Cliente | C | Venda em `infAdic/infCpl`; item em `det/infAdProd`. Não usar texto livre para decidir tributo, concessão ou transporte |
| Referência de credencial e A1/CSC | Servidor, futuramente | Fora dos arquivos de venda/preparação públicos | Referência interna autorizada; PFX, chave privada, senha, tokens e CSC não entram nos JSONs. Assinatura sintética local, quando executada, é prova distinta |
| Baseline, chave, XML e hashes | Servidor/preparador | O para rastrear o resultado local | Baseline fixada; `infNFe/@Id`, `cDV`, XML gerado e evidência dos bytes. Não fornecer protocolo autorizador ou XML processado sintético como prova fiscal |

## 3. Regras da preparação e exemplos sintéticos

Decimais devem manter representação exata compatível com seu tipo XSD. Quantidades e valores unitários têm precisão própria; valores monetários usam duas casas quando fracionários. Não usar ponto flutuante binário, notação científica, separador de milhar ou arredondamento silencioso. Comparar totais/tributos para validar é diferente de substituir os valores recebidos. Regras aritméticas e tolerâncias aplicáveis precisam de referência identificada; os valores dos exemplos não são uma tabela tributária.

Na representação JSON 0.1, códigos e decimais são **strings**; valores monetários são escritos com duas casas, inclusive `"0.00"` e `"100.00"`. Essa exigência local é mais restrita que alguns tipos XSD que admitem inteiros. O parser exige nomes/capitalização declarados, propriedades obrigatórias e `syntheticOnly=true`; recusa campos desconhecidos, repetidos, `null` explícito e espaços externos em textos/códigos/valores, sem aparar a entrada. Campos opcionais ausentes são omitidos, sem preencher zero. Os limites locais incluem 1–990 itens, 1–100 pagamentos e referência de 1–100 caracteres; esses limites não comprovam carga, elegibilidade ou cadastro.

A validação implementada verifica representação, vínculo dos três arquivos, recorte, compatibilidade CRT/grupos e presença dos grupos/totais RTC; a CLI acrescenta a validação XSD e provas de assinatura. Não executa todas as equações/RVs/tolerâncias tributárias nem decide se uma omissão ou um valor é fiscalmente correto. Os exemplos têm consistência numérica declarada e rastreável; não usar a saída local como declaração de validação fiscal completa.

Os exemplos públicos contêm os três arquivos completos, marcando CNPJ/IE/endereço/nomes como dados de teste e declarando as escolhas fiscais de cada item. Um identificador com formato/DV válido não comprova empresa existente, titularidade, IE, CRT ou credenciamento. Os nomes dos casos Simples e normal designam testes separados; não afirmam que um emitente foi selecionado ou que a empresa terá esses regimes.

| Caso de preparação | Venda | Perfil do emitente | Contexto |
| --- | --- | --- | --- |
| Simples sintético, CRT 1 | [sale-crt1.json](../samples/preparation/sale-crt1.json) | [emitter-crt1.json](../samples/preparation/emitter-crt1.json) | [test-context.json](../samples/preparation/test-context.json) |
| Normal sintético, CRT 3 com RTC | [sale-crt3.json](../samples/preparation/sale-crt3.json) | [emitter-crt3.json](../samples/preparation/emitter-crt3.json) | O mesmo contexto, em execução/diretório distintos; não é pool fiscal compartilhado |
| ST retido sintético ("caixa de cerveja"), CRT 1 | [sale-st-crt1.json](../samples/preparation/sale-st-crt1.json) | O mesmo [emitter-crt1.json](../samples/preparation/emitter-crt1.json) | O mesmo contexto, em execução/diretório distintos; não é pool fiscal compartilhado |

O caso CRT 1 usa dois itens com unidades comercial/tributável diferentes: CX/UN, quantidades 2/6 e preço comercial com 10 casas; PCT/UN, quantidades 1/5. Produtos somam 100,00, descontos por item 5,00 + 2,00, nota 93,00, dois pagamentos em dinheiro 50,00 + 48,00 e troco 5,00. O caso CRT 3 usa CX/UN, quantidades 2/4 e nota 100,00; pagamentos 50,00 + 55,00 e troco 5,00. Ambos identificam destinatário; no CRT 3, `creditIntent="unknown"` preserva a condição ainda não determinada e impede tratá-lo como caso real elegível, sem impedir a prova local sintética. Série 17/18 e número 123/código 87654321 são parâmetros explícitos de teste, sem reserva ou disponibilidade fiscal comprovadas.

O caso ST retido reutiliza o perfil CRT 1 e declara um item único "caixa de cerveja" com unidades CX/UN (1 caixa, 12 unidades), produto 48,00, pagamento 50,00 e troco 2,00. O grupo `ICMSSN500` declara as três sequências completas: retenção 40,00/18%/4,00/7,20, FCP-ST 40,00/2%/0,80 e efetivos 10%/43,20/12,5%/5,40; `vFCPSTRet` de `ICMSTot` repete 0,80 por declaração. NCM 22030000, CEST 0302103 (Anexo IV do Convênio ICMS 142/18, cerveja em lata) e CFOP 5405 são **hipóteses de teste declaradas no próprio arquivo**, não enquadramento aprovado; PIS/COFINS usam CST 04 via grupos NT como hipótese de monofásico, com a base legal ainda pendente na [matriz de cobertura](RETAIL-COVERAGE-MATRIX.md). Nenhum valor é recalculado ou conferido contra MVA/pauta/alíquota real.

| Alternativa implementada | Campos admitidos / limite |
| --- | --- |
| CRT 1 + `ICMSSN102` | `orig` e `CSOSN=102`; sem CST, base/alíquota/valor de ICMS00 nem campos de ST retido. Escolha explícita do teste, sem tornar 102 padrão de Simples |
| CRT 3 + `ICMS00` | `orig`, `CST=00`, `modBC`, `vBC`, `pICMS`, `vICMS`; sem campos de ST retido. Outros grupos e adicionais específicos ainda não têm representação |
| CRT 1 + `ICMSSN500` | `orig`, `CSOSN=500` e as três sequências opcionais do XSD, cada uma completa ou ausente: retenção `vBCSTRet`+`pST`+`vICMSSubstituto`?+`vICMSSTRet`, FCP-ST `vBCFCPSTRet`+`pFCPSTRet`+`vFCPSTRet` e efetivos `pRedBCEfet`+`vBCEfet`+`pICMSEfet`+`vICMSEfet`. Valores declarados preservados; `TDec_0302a04Opc` não admite `"0"` inteiro |
| CRT 3 + `ICMS60` | `orig`, `CST=60` e as mesmas sequências de ST retido do `ICMSSN500`; sem `modBC`/`vBC`/`pICMS`/`vICMS` |
| CEST condicional a ST | `CEST` (7 dígitos) obrigatório quando o grupo declarado é `ICMSSN500`/`ICMS60`, conforme Convênio ICMS 142/18, cláusula vigésima, I ([fonte baixada](evidence/retail-coverage/FONTES.md)). O preparador não escolhe o código |
| `PISNT` / `COFINSNT` | CST 04–09, sem base/alíquota/valor de grupo tributado |
| `PISAliq` / `COFINSAliq` | CST 01–02, base/alíquota/valor explícitos, sem converter NT em tributado ou o inverso |
| RTC regular | `CST=000`, `cClassTrib=000001`, `gIBSCBS` com IBS UF/município/CBS e seus totais. Sem redução, diferimento, monofásico ou crédito presumido; demais CST/classificações/grupos são recusados |
| Destinatário | CPF/CNPJ, nome, `indIEDest=9`, `creditIntent` e endereço/email condicionados. IE de contribuinte e identificação estrangeira não estão implementadas |

O subconjunto é limite da preparação executável. Não descreve cobertura fiscal integral, nem autoriza remover dados de um caso real que demande grupo diferente. Quantidades, bases, alíquotas, códigos e totais são valores explícitos de cada JSON; não há uma tabela fiscal embutida escolhendo esses campos para o usuário.

O [MOC 7, Anexo I](https://www.confaz.fazenda.gov.br/legislacao/arquivo-manuais/moc7-anexo-i-leiaute-e-rv.pdf), regras I04-10 e E04-20, determina respectivamente os textos abaixo no primeiro item e no destinatário identificado em homologação:

- `det[@nItem="1"]/prod/xProd`: `NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL`.
- `dest/xNome`: `NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL`.

Esses textos devem estar presentes nos campos recebidos: `items[0].xProd` do primeiro item `nItem="1"` e `recipient.name` quando o destinatário existir. O preparador exige a igualdade literal e recusa divergências; escreve os dados recebidos, sem substituir descrição/nome silenciosamente. As descrições dos demais itens e o nome do emitente permanecem os valores informados. Omitir o destinatário exige fundamento do caso; ausência estrutural no preparador não comprova dispensa fiscal.

A RTC do caso normal contempla a exigência de `IBSCBS` em homologação para CRT 3 desde **01/07/2026**, segundo [NT 2025.002 v1.52](https://dfe-portal.svrs.rs.gov.br/NFE/DownloadArquivoEstatico/?sistema=NFE&tipoArquivo=3&nomeArquivo=NT_2025.002_v1.52_RTC_NF-e_IBS_CBS_IS.pdf), p.40, UB12-10. Nessa revisão, a data de produção 03/08/2026 e a de CRT 1/2/4 em 04/01/2027 estão riscadas; a redação vigente da regra indica implementação futura em produção. Não tratar essas datas riscadas como ativação. O caso Simples é distinto; opcionalidade estrutural não constitui dispensa genérica nem autoriza ampliar grupos/classificações por aproximação.

O caso normal usa a hipótese sintética de produto de R$ 100,00, ICMS CST 00 com base 100,00/alíquota 18%/valor 18,00; PIS CST 01 com base 82,00/alíquota 1,65%/valor 1,35; COFINS CST 01 com base 82,00/alíquota 7,6%/valor 6,23. Essas escolhas **não são enquadramento ou alíquotas reais aprovados para o DF ou para um produto**. A base RTC exemplificada é 74,42, conforme valores explicitamente declarados, e seus grupos têm IBS UF 0,1000%/valor 0,07, IBS municipal 0,0000%/valor 0,00 e CBS 0,9000%/valor 0,67. As alíquotas RTC de 2026 são rastreadas às regras UB18-10, UB37-10 e UB56-10; a [classificação 000001/CST 000](https://dfe-portal.svrs.rs.gov.br/CFF/ClassificacaoTributaria) foi conferida quanto ao indicador de modelo NFC-e e aos grupos admitidos, sem comprovar sua aplicabilidade a mercadoria real.

Nesse exemplo de 2026, `vItem`, `vNF` e `vNFTot` são 100,00, considerando a exceção da regra VB01-10 para não somar IBS/CBS/IS ao valor do item naquele ano. A presença dos novos campos no XML não prova ativação de todas as RVs de totais. Fontes, hashes, datas e limites da pesquisa em [FONTES](evidence/input-model/FONTES.md) e [sources.json](evidence/input-model/sources.json); os casos conservam os dados recebidos, sem recalcular ou substituir tributos.

## 4. Preparação pela CLI e evidência local

Após compilar a solução, a CLI recebe os três JSONs e um diretório de saída novo:

```powershell
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- prepare samples/preparation/sale-crt1.json samples/preparation/emitter-crt1.json samples/preparation/test-context.json .validation/preparation-crt1
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- prepare samples/preparation/sale-crt3.json samples/preparation/emitter-crt3.json samples/preparation/test-context.json .validation/preparation-crt3
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- prepare samples/preparation/sale-st-crt1.json samples/preparation/emitter-crt1.json samples/preparation/test-context.json .validation/preparation-st-crt1
```

Escolher outro diretório novo ao repetir cada comando; a CLI recusa sobrescrever saída existente.

O resultado reúne `unsigned.xml`, `signed.xml`, `submission.xml`, `consultation.xml` e `summary.json`. A chave e o XML derivam dos dados dos arquivos; a CLI acrescenta o suplemento de QR 3 online antes de assinar. A assinatura usa certificado sintético gerado/importado em memória; o envelope de envio e a consulta são preparados localmente. Nenhuma resposta autorizadora, protocolo ou `nfeProc` é fabricado; o comando não gera DANFE e não executa transmissão. O resumo distingue essa evidência dos testes antigos e informa `fiscalIo = false`.

Os modelos .NET tipados e os XSDs oficiais constituem o contrato executável desta preparação; esta entrega não publica um novo JSON Schema nem uma rota HTTP. O parser e o preparador devem recusar entrada incompatível ou variante sem suporte, sem aproveitar dados fiscais da fixture para completar o XML.

## 5. Condições para emissão real

Preparar modelo, gerar XML a partir dos três arquivos e exercitar casos sintéticos é trabalho implementável agora. A falta da empresa real não impede esses testes. XSD válido, assinatura matemática ou XML local não comprovam adequação tributária, certificado ICP-Brasil, consulta/autorização SEFAZ ou DANFE renderizado.

Antes de qualquer transmissão real, fechar a [ficha do piloto](PILOT-REGISTRATION.md): empresa/estabelecimento/CRT, sistema cliente e propriedade, responsáveis/autoridades/delegação, série admissível/histórico e casos fiscais aprovados. Concluir os gates de normas/schema, arquitetura/custódia/dados, isolamento, persistência, reserva/idempotência, trabalhadores, A1/credenciamento e transporte autenticado previstos no [roadmap](ROADMAP.md). O XML deve preservar a mesma revisão em resultado incerto; 217 isolado não libera reenvio.

O escopo mantém produção, NF-e 55, outras UFs, entrega/remoto, correção, cancelamento, inutilização e contingência fora desta preparação. Caso que exige modelo 55 recebe recusa, sem conversão automática. A consulta com alertas continua dependente da solução oficial documentada na EF-05. `fiscalIo = false`; nenhuma transmissão integra esta entrega, e o novo modelo não fecha #3 sozinho.

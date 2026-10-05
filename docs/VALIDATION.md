# Relatório do incremento NFC-e offline

O relatório de 04/10/2026 abaixo é histórico. A validação de CI/locks EF-04 está
registrada ao final deste documento, sem substituir a prova original.

Data da execução: **04/10/2026**. Diretório: `C:/Development/Projects/techlab-emissorfiscal`.

## Status e resultado

**Concluído para o recorte da prova local offline.** Restore, build Release,
**70/70 testes**, CLI `schemas` e CLI `demo` passaram com o SDK fixado.
O build terminou com **zero avisos e zero erros**. Foram comprovados os seis
entrypoints em .NET, importação PKCS#12 sintética, assinatura/canonicalização e
correlação de resultados simulados. Nenhuma homologação fiscal foi obtida.

O bloqueio inicial por ausência do SDK foi resolvido pela instalação informada
pelo usuário. Foi confirmado **SDK 10.0.401** em `C:/Program Files/dotnet/sdk`,
com **runtime 10.0.12**. `global.json` mantém `rollForward=disable` e sem preview.
A versão selecionada foi efetivamente usada. O PATH herdado pela sessão Codex
foi complementado somente nos processos de validação. Não houve instalação de
SDK pelo agente, mudança de PATH persistente ou troca de stack.

Há código para parser seguro, seis validadores XSD, assinatura de `infNFe`, importação
PKCS#12 em memória, chave/CNPJ numéricos e alfanuméricos, QR 3 online DF, envio
síncrono unitário, consulta, interpretação de protocolo e construção de `nfeProc`.
O recorte local é modelo 65/cUF 53/tpAmb 2/tpEmis 1. A prova permanece técnica e
sintética: não atesta operações tributárias, origem SEFAZ, credenciamento ou uso
operacional de certificados/empresas reais.

## Arquivos

| Área | Arquivos criados/alterados |
| --- | --- |
| Solução/configuração | `global.json`, `EmissorFiscal.slnx`, `Directory.Build.props`, `NuGet.Config`, `.gitignore`, `README.md` |
| Núcleo | `src/EmissorFiscal.Core/EmissorFiscal.Core.csproj`; `Xml/SafeXml.cs`; `Xsd/SchemaManifest.cs`, `Xsd/XsdValidator.cs`; `Domain/AccessKey.cs`; `Signature/A1Signer.cs`; `QrCode/QrCode3.cs`; `Wire/NfceMessages.cs`; `Protocols/NfceProtocol.cs` |
| CLI | `src/EmissorFiscal.Cli/EmissorFiscal.Cli.csproj`, `Program.cs` |
| Dados técnicos | `samples/OfflineFixtures.cs`; fixtures XML/README em `tests/EmissorFiscal.Core.Tests/Fixtures` |
| Testes | Projeto `EmissorFiscal.Core.Tests`; `OfflineProof`, `SafeXmlTests`, `AccessKeyAndQrTests`, `A1SignatureTests`, `XsdAndWireTests`, `ProtocolTests`, `ManifestTests`, `OfflineBoundaryTests` |
| Schemas | `schemas/nfe/4.00/manifest.json` e 11 XSD oficiais em `files/` |
| Execução/manutenção | `scripts/verify.ps1`, `scripts/review-schemas.py`, `scripts/verify-materials.py` |
| Documentação | `docs/TECHNICAL-SCOPE.md`, este relatório e atualização técnica em `docs/PROJECT.md` |

O briefing de negócio `docs/briefing.dados.json` foi preservado. Downloads públicos
e extrações ficaram em `tmp/`; evidências complementares em `.validation/`, ambos
ignorados. Não foi gerado arquivo de chave privada/PFX/PEM, lockfile Node ou pacote
de instalação. As dependências NuGet e o home do CLI ficaram em `.validation/`;
binários em `bin/obj`, resultados TRX em `TestResults/`, também ignorados.

## Comandos e evidências observadas

| Comando/conferência | Resultado real |
| --- | --- |
| `git status --short --branch` e consulta inicial de remoto | Diretório não é repositório Git; nenhuma inicialização realizada |
| `dotnet --version` / `dotnet --list-sdks` | 10.0.401 confirmado em Program Files; o runtime da CLI foi 10.0.12 |
| `dotnet restore EmissorFiscal.slnx` | Exit 0; três projetos restaurados; sem avisos NuGet emitidos |
| `dotnet build EmissorFiscal.slnx -c Release --no-restore` | Exit 0; zero avisos/erros na execução final |
| `dotnet test EmissorFiscal.slnx -c Release --no-build --no-restore --logger trx --results-directory TestResults` | Exit 0; total 70, aprovados 70, falhas 0, ignorados 0 |
| `dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- schemas` | Exit 0; seis entrypoints compilados, baseline e hash do manifest conferidos |
| `dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- demo` | Exit 0; PFX em memória, CNPJ/chave alfanuméricos, assinatura após serialização/envelope/nfeProc e resultado sintético 120 com dois alertas/nProt de 17 posições |
| `& .\scripts\verify.ps1` | Exit 0; fluxo completo restore -> build -> testes -> schemas -> demo concluído |
| Python `scripts/review-schemas.py` com ZIPs oficiais locais | Exit 0; composição do fechamento de 11 arquivos e manifest, sem editar XSD |
| Python `scripts/verify-materials.py` | Exit 0; compilação complementar dos seis entrypoints com lxml/libxml2, seis positivos e seis negativos |
| Leitura de XML de configuração/JSON e revisão final de vínculos | 6 XML de solução/projetos/configuração e 3 JSON parseados; nenhum vínculo local ausente; sintaxe PowerShell sem erros; não é compilação C# |
| Revisão estática de I/O em `src`, `samples`, `tests` | Sem implementação de transporte fiscal, acesso à Store ou gravação de chave privada; URLs de QR são somente strings |
| Revisão independente de segurança | Correção do resolver e evidências build/TRX/CLI revisadas, sem apontamentos acionáveis; nenhum teste repetido pelo revisor |

Evidência .NET atual: `.validation/dotnet-validation.json` e
`.validation/dotnet-validation-final.log`. Resultados da CLI:
`.validation/cli-schemas.json` e `.validation/cli-demo.json`. TRX final:
`TestResults/bruno_NOTEBOOK-LENOVO_2026-10-04_13_42_47_net10.0.trx`.
O JSON atual registra o SHA-256 desse TRX e os contadores extraídos dele.

Histórico inicial preservado: `.validation/dotnet-attempts.json` registra os
comandos bloqueados por `CommandNotFoundException` antes da instalação do SDK.
A primeira compilação após a instalação apontou dois CS8625 em `Resolver=null`:
`SignedXml.Resolver` recebe tipo não anulável no pacote fixado. A correção usa
`RejectingXmlResolver.GetEntity`, que sempre lança `XmlException`, nos caminhos
de assinar/verificar. Não houve supressão de nulabilidade nem relaxamento de
resolução externa. Logs iniciais e da correção também foram preservados.

Uma chamada de build no sandbox falhou sem diagnósticos de compilação. O build
diagnóstico com um worker, `-m:1 -nodeReuse:false -p:UseSharedCompilation=false`,
passou; o script final com os parâmetros padrão também passou no executor com
permissões para o SDK. `dotnet --info` no sandbox listou as versões, mas sua
consulta de workloads recebeu acesso negado ao Service Control Manager; não foi
necessário instalar workload ou modificar permissões do sistema.

As validações usaram `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` e
`DOTNET_CLI_TELEMETRY_OPTOUT=1`. Não se gerou certificado de desenvolvimento ASP.NET.
NuGet/restauração de dependências públicas não constitui I/O fiscal. Núcleo/testes
não contêm transporte fiscal; os testes de fronteira, referências externas e
resolver passaram. Não foi realizada captura de tráfego como prova separada.

Auditoria complementar anterior: `.validation/materials.json`, identificada como
`SUPPLEMENTAL_MATERIAL_AUDIT_NOT_DOTNET`. Conferência estática anterior:
`.validation/static-review.json`, com os hashes e vínculos, sem execução de testes
naquela etapa. Esses documentos históricos não substituem o resultado .NET atual.
O interpretador usado foi o Python fornecido pelo runtime local do Codex. Para
reproduzir essa auditoria em outro host, usar Python com lxml; ela é auxiliar e
não altera a stack .NET do núcleo.

Manifest auditado: SHA-256
`9a3d723f602816bc5188ea9b5e01fe324fbc8165f479d06dac8261a6d570e72c`.
Os três hashes de ZIP coincidiram com a baseline solicitada. Precedência:
PL009n < PL010d v1.03/somente NFe < PL010f v1.04. Origem, data do catálogo,
dependências, entradas e hashes por arquivo estão no manifest. Cadastros/eventos
do PL010d não foram achatados no diretório NFe.

A auditoria lxml também validou estrutura de 120/alertas/protocolo de 17 posições,
CNPJ/chave alfanuméricos, bloqueio de DTD, XML truncado e resolver externo. Confirmou
que os XSD aceitam modelo 55, UF 35, ambiente 1, tpEmis 9 e várias notas no envelope;
o núcleo tem controles adicionais e testes C# aprovados para negar esses casos.
As assinaturas dessa auditoria foram **placeholders estruturais em memória**, sem
certificado/chave, nunca prova criptográfica.

## Aceite comprovado e limites

Foram escritos **33 métodos Fact e 11 Theory**, expandidos em **70 casos executados
e aprovados**, sem falhas ou casos ignorados.

| Aceite do pedido | Cobertura preparada | Prova obtida nesta máquina |
| --- | --- | --- |
| Seis entrypoints, hashes, dependências e resolver restrito | Manifest/XsdValidator e ManifestTests | Compilação dos seis em .NET/CLI e testes negativos de hash/dependência aprovados |
| Positivos/negativos e recorte 65/53/2/1, uma nota | XsdAndWireTests e parser/chave | Positivos numéricos/alfanuméricos e negativos de modelo/UF/ambiente/emissão/lote aprovados |
| PFX sintético, senha incorreta, sem chave privada, não RSA | A1SignatureTests; geração/exportação/importação somente em memória | Importação válida e três caminhos negativos aprovados |
| Assinatura após serialização/envelope e adulteração/ID/ref externa | A1SignatureTests/XsdAndWireTests; RSA-SHA1/SHA1/C14N explícitos | Verificação e negativos aprovados; CLI demonstra preservação também no nfeProc |
| Protocolo incompatível/incompleto e resposta truncada | ProtocolTests; chave/tpAmb/digVal, status e número | Casos aprovados; nenhum retorno incompatível/incompleto autorizou |
| 120, mensagens, nProt de 17 posições; 100/150/desconhecidos | ProtocolTests e CLI demo | Estados separados, mensagens preservadas, códigos desconhecidos/duplicidades não autorizaram |
| DV numérico/alfanumérico e QR 3 online | AccessKeyAndQrTests | Exemplos/vetores, parâmetros do QR e rejeição de variantes aprovados |
| Ausência de I/O fiscal | Núcleo sem transporte; testes de fronteira e referências | Inspeção estática e suíte aprovadas. Nenhuma transmissão realizada |

Vetores: exemplo MOC 7, base numérica
`5206043300991100250655012000000780026730161`: soma 644, resto 6, DV 5.
Exemplo oficial da NT conjunta CNPJ: `12ABC34501DE35`, somas 459/424, DVs 3/5.
Vetor de chave **derivado**, não exemplo literal publicado:
`53261012ABC34501DE3565001000000001112345678`, com DV 2 pelo algoritmo ASCII-48
e pesos 2..9 da direita para a esquerda. Chave completa e CNPJ são strings.

Uma limitação da baseline está demonstrada: `leiauteConsSitNFe` do PL010d não aceita
`cMsg/xMsg` em `infProt`; `leiauteNFe` do PL010f aceita. O núcleo rejeita consulta
com extensão incompatível e tem teste negativo específico. Novo pacote oficial
e regressões serão necessários para ampliar isso; nenhum XSD foi modificado.

Verificação com `verifySignatureOnly=true` não comprova ICP-Brasil, cadeia,
revogação ou titularidade. A resposta interpretada é fornecida pelo chamador;
esta prova não atesta origem SEFAZ nem oferece transporte autenticado. Retornos
simulados com status autorizado não são autorizações fiscais reais.

## Fontes, versões e decisões

A matriz completa, com URLs primárias, publicação e ativação por ambiente/regra,
está em `docs/TECHNICAL-SCOPE.md`. Foram lidos MOC 7/Anexos I e IV, Manual DANFE 6.0,
as NTs solicitadas, IT 2025.002 v1.70 e legislação oficial CONFAZ/DF. Revisões
posteriores NT 2025.001 v1.03 corrigida e NT 2019.001 v1.70 também foram registradas.
Datas de catálogo e controle interno dos PDFs são distinguidas. Documentos novos
de 01/10/2026 não foram convertidos automaticamente em uma baseline XSD nova.

A Portaria DF 387 compilada e o corpo do AD 1/2026 **Assuntos Econômicos** foram
lidos pelo serviço público `WPI/api/legislacao` que abastece o app oficial.
A compilação inclui Portaria 998/2025. O AD tem efeitos em 30/09/2026 e altera
benefícios fiscais; o anexo completo remetido ao DODF ainda exige reconciliação.
Foi distinguido do AD SUREC 1/2026 sobre GNRE. O arquivo histórico de regras DF
2019.001 identifica v1.20, portanto não comprova cobertura atual integral.

Microsoft: SDK selecionado/usado 10.0.401, runtime correspondente 10.0.12, publicação
08/09/2026, conforme página oficial de download. Dependências diretas com versões
exatas, verificadas no índice público oficial NuGet: Cryptography.Xml 10.0.12,
Test SDK 17.14.1, xUnit 2.9.3, runner 3.1.4. Restore com auditoria NuGet habilitada
passou sem avisos; o grafo observado está em `.validation/resolved-dependencies.json`
e inclui Cryptography.Pkcs 10.0.12 e xUnit analyzers 1.18.0. Build/testes comprovaram
compatibilidade deste grafo no host. Não foi criado lockfile NuGet; uma alteração
futura de dependências deve repetir restore/auditoria/build/testes.

Decisões técnicas: manifest local confiável/revisado, schemas oficiais sem edição,
parser com DTD/resolução externa proibidos, assinatura MOC explícita, RSA >=2048
como política local, PFX sintético efêmero, QR DF publicado no serviço 781, hashes
de pedido/XML/digest fiscal separados e nenhuma repetição automática.
Dados tributários existentes no XML são preservados; não há cálculo ou escolha
de enquadramento. CRT/CFOP/CSOSN/CST/NCM da fixture não definem operações aceitas.

Continuam `TBD`: operações, regimes, permissões/autenticação, custódia real,
retenção, empresas piloto, credenciamento, legado de numeração, infraestrutura,
banco e produção. A API futura, estados, isolamento, aliases do mesmo CNPJ,
reserva concorrente, replay normativo, lease, crash, resultado desconhecido e
correção após rejeição estão documentados; não foram implementados.

## Entrega e próxima ação

A etapa da prova foi concluída somente com arquivos locais. **Nenhuma transmissão
fiscal**, certificado de contribuinte, segredo, CSC, Windows Store, API, banco ou
migração. Naquela etapa não houve Git, PR ou deploy. Em seguida, o pedido “suba pro
github” autorizou publicar o código em repositório privado na branch main:
[BrunoMNoronha/techlab-emissorfiscal](https://github.com/BrunoMNoronha/techlab-emissorfiscal).
A publicação não autoriza deploy nem I/O fiscal. Nenhum comando npm/npx foi
executado; pnpm continua obrigatório se surgir código Node.

`.gitattributes` impede conversão dos finais de linha dos 11 XSDs oficiais e do
manifest para preservar seus hashes em clones/checkouts. `.validation/`,
`TestResults/`, `tmp/` e binários não são publicados; as evidências detalhadas
continuam locais e os comandos do README permitem reproduzir a prova.

O incremento offline está fechado com evidência executável. Para reproduzir,
usar **SDK .NET 10.0.401 no PATH**, as variáveis de sessão indicadas no README e
`./scripts/verify.ps1`. O script faz restore, build Release, testes xUnit/TRX,
CLI `schemas` e CLI `demo`. Os comandos individuais estão no README.

O incremento seguinte depende da revisão das lacunas normativas e das decisões
de negócio/custódia. Exigirá identidade canônica,
isolamento, idempotência, reserva transacional, XML imutável e reconciliação
durável antes de qualquer I/O fiscal em homologação.

## EF-04 — CI offline e locks NuGet — 05/10/2026

**Concluído no recorte de CI offline.** Validação local, execução real positiva
e reprovação controlada de Actions tiveram resultados e artefatos conferidos.
Implementação em `a00361b6d7a4363431708d8e17a5a172b3192c26`, documentação de
reprodução em `409735bf7c4115f9cecf8e4a78bee86a56304674`,
[PR #19](https://github.com/BrunoMNoronha/techlab-emissorfiscal/pull/19).

### Implementação e decisões

- Workflow `ci-offline.yml`: PRs/pushes para `main` e execução manual;
  `windows-2025`, pwsh, SDK exato de `global.json`, timeout de 15 minutos,
  `contents: read`, credenciais Git não persistidas e sem cache anterior.
- Ações oficiais revisadas no GitHub e fixadas por SHA: checkout v7.0.1
  (`3d3c42e5aac5ba805825da76410c181273ba90b1`), setup-dotnet v6.0.0
  (`a98b56852c35b8e3190ac28c8c2271da59106c68`) e upload-artifact v7.0.1
  (`043fb46d1a93c77aae656e7c1c64a875d1fc6a0a`).
- Locks dos três projetos preservam o grafo observado anteriormente: Core/CLI
  com dois pacotes cada; testes com 15. Dependências de projeto são registradas
  separadamente. Versões diretas não foram alteradas.
- `RestorePackagesWithLockFile`/`RestoreLockedMode` por padrão; o script exige
  todos os locks e usa `--locked-mode`. Auditoria NuGet e avisos como erros
  preservados. Atualização explícita do grafo documentada no README.
- Script compartilhado produz diretório único por execução, captura logs sem
  mascarar códigos nativos e exige TRX/CLIs/artefatos completos para sucesso.
  O CI publica somente a lista selecionada, inclusive diagnósticos de falha,
  com retenção de 30 dias. Não são publicados caches ou `.validation/`.

### Validação local em checkout limpo

Clone novo em `tmp/ef04-validation/clean`, sem bin/obj/resultados prévios e com
cache NuGet/CLI home novos. Executado com
`pwsh -NoProfile -File scripts/verify.ps1` e SDK 10.0.401/runtime 10.0.12.
Restore bloqueado, build Release (zero avisos/erros), **70/70 testes** sem
ignorados e CLI schemas/demo passaram com exit 0.

Evidência local:
`tmp/ef04-validation/clean/TestResults/ci/180f68d6e2d0413aaa642b48f3007175/summary.json`.
TRX, nove artefatos com hashes, 11 hashes XSD e três hashes de lock conferidos.
Manifest SHA-256:
`9a3d723f602816bc5188ea9b5e01fe324fbc8165f479d06dac8261a6d570e72c`.
Baseline `nfe-4.00-offline-2026-10-04`. Actionlint 1.7.12 e parser PowerShell
aprovados; binário actionlint oficial teve checksum conferido antes do uso.

### Falhas controladas locais

Cada cenário usou outro clone descartável. Nenhuma mutação entrou na entrega.

| Cenário | Mutação temporária | Resultado observado |
| --- | --- | --- |
| Restore | Referência Cryptography.Xml 10.0.11 mantendo lock de 10.0.12 | NU1004; exit 1; última etapa restore; sem build/testes/CLI. |
| Build | Arquivo C# com `#error EF04_CONTROLLED_BUILD_FAILURE` | CS1029; exit 1; última etapa build; sem testes/CLI. |
| Testes | Caso existente tenta obter XmlException para XML válido | Uma falha real de xUnit; exit 1; última etapa test; TRX preservado; sem CLI. |
| Schema | Uma quebra de linha acrescentada ao nfe_v4.00.xsd | SHA-256 divergente; exit 1 durante test; TRX preservado; sem CLI. |

Resumos/códigos em `.validation/ef04-negative-results.json`; consoles e clones
isolados em `tmp/ef04-validation/negative-*`. As etapas seguintes não foram
executadas e o summary marcou `failed` com o mesmo código nativo.

### Execuções reais de Actions

- [PR — execução 37303850088](https://github.com/BrunoMNoronha/techlab-emissorfiscal/actions/runs/37303850088):
  **success**, head `409735bf7c4115f9cecf8e4a78bee86a56304674`, checkout de merge
  `cf5e083f3ce7886ea50238f519a8c62712ad5f4c`. SDK 10.0.401/runtime 10.0.12,
  pwsh 7.6.6, imagem `win25-vs2026`/`20260925.250.1` (Windows 2025/X64).
  Restore/build/testes/CLIs exit 0; zero avisos/erros; 70/70 sem ignorados.
  [Artefato 11342851662](https://github.com/BrunoMNoronha/techlab-emissorfiscal/actions/runs/37303850088/artifacts/11342851662):
  dez arquivos selecionados, incluindo summary e nove evidências com hashes
  recalculados após download; TRX/CLIs, 11 schemas e três locks conferidos.
- [Prova negativa — execução 37303880971](https://github.com/BrunoMNoronha/techlab-emissorfiscal/actions/runs/37303880971):
  commit `5a4d9e8c8f8d5e3cf265f198546054c59d71a31f` na branch descartável
  `codex/ef-04-prova-falha`, com divergência de lock deliberada: **failure**,
  restore NU1004/exit 1; sem build/testes/CLIs. Etapas de resumo e publicação
  concluíram com sucesso; summary, sdk.log e restore.log foram baixados e
  conferidos. A branch remota descartável foi removida, sem merge.

O checkout padrão de PR testa o commit de merge temporário do GitHub;
`summary.commit` identifica esse checkout, enquanto o head da PR é registrado
separadamente. Push na main identifica diretamente seu commit.

Nenhum certificado de contribuinte, segredo, alteração fiscal, transmissão,
homologação ou deploy. O CI valida o recorte técnico offline; decisões do piloto,
infraestrutura, credenciais reais e cobertura normativa seguem seus próprios aceites.

## EF-05 — documentação e regressões — 05/10/2026

Incremento da issue #2, ainda aberta. [Matriz normativa](EF05-NORMATIVE-MATRIX.md) e [proveniência](evidence/ef05/FONTES.md) incorporadas. Nove pacotes examinados sem consulta oficial compatível com alertas; nenhuma nova baseline foi integrada. Acesso integral ao anexo DF comprovado no levantamento; transcrição/reconciliação das 270 linhas e aplicação ao piloto continuam pendentes da EF-06.

Execução atual: `pwsh -NoProfile -File scripts/verify.ps1`, exit 0, SDK 10.0.401/runtime 10.0.12, restore `--locked-mode`, NuGetAudit/all preservado, build Release zero avisos/erros, **84/84 testes**, zero ignorados, seis entrypoints e CLI demo aprovados. Quatorze casos novos cobrem protocolo 15/16/18 conforme lacunas anteriores, limites 0/1/5/6 mensagens, ordem/preservação no processado e pares incompletos. Casos de 17 posições já existiam. PFX sintético em memória; nenhuma transmissão ou homologação.

[Resumo local e hashes](evidence/ef05/local-validation.json). TRX/logs integrais ignorados em `TestResults/ci/8b24a49dfb904d9fb93aad8b97d9e7e7`; reprodução usa os testes versionados. A execução tem base 4a475c4 mais o diff deste incremento, não o checkout limpo daquele commit. Primeira execução passou 83/83; após acrescentar a borda de 16 posições na consulta, repetiu-se o fluxo completo com 84/84. Inicialmente dotnet não estava no PATH da sessão; o executável instalado foi encontrado em C:/Program Files/dotnet e o ambiente foi configurado, sem instalar dependências novas ou alterar SDK/locks.

Pesquisa histórica distinta: 40 testes selecionados e 150 casos estruturais lxml; o [resumo estrutural](evidence/ef05/structural-summary.json) declara explicitamente ausência de prova criptográfica. Primeira tentativa histórica de testhost anulada por timeout e download SVRS vazio não contam como aprovação. CI EF-04 com 70/70 permanece histórica; CI desta PR deve ser identificada separadamente. Nenhum resultado prova ativação fiscal.

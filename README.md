# TechLab+ Emissor Fiscal - prova NFC-e offline

Primeiro incremento local em **.NET 10**, restrito a **modelo 65, DF (`cUF=53`),
homologação (`tpAmb=2`), emissão normal (`tpEmis=1`)**. O núcleo lê XML de forma
segura, valida schemas oficiais fixados, prepara assinatura A1/PKCS#12 em memória,
QR Code 3 online, mensagens de envio/consulta e correlação de protocolos.

**Estado em 04/10/2026: prova local concluída.** Após a instalação do SDK pelo
usuário, restore, build Release, **70 testes** e CLI `schemas`/`demo` passaram.
O build terminou com zero avisos/erros. Os seis entrypoints compilaram em .NET,
e a assinatura/PKCS#12 sintética foi verificada após serialização, envelope e
nfeProc. Evidências e limites estão em `docs/VALIDATION.md`.

O [roadmap](docs/ROADMAP.md) registra as entregas, dependências, decisões pendentes
e critérios para evoluir da prova local à homologação e operação.

Nenhuma transmissão fiscal foi feita. Não houve homologação fiscal nem liberação
para operação. Fixtures, certificado sintético e retornos simulados não têm
validade fiscal. Operações, regimes, permissões e custódia real continuam `TBD`.

## Pré-requisito e execução

Requer o **SDK .NET 10.0.401**, publicado pela Microsoft em 08/09/2026, acessível
no PATH. Essa versão foi **executada neste host**, com runtime 10.0.12.
`global.json` exige exatamente essa versão e não permite roll-forward ou preview.
Fonte: [download oficial .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Na raiz do projeto, após disponibilizar o SDK:

```powershell
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet --version
dotnet restore EmissorFiscal.slnx --locked-mode
dotnet build EmissorFiscal.slnx -c Release --no-restore
dotnet test EmissorFiscal.slnx -c Release --no-build --no-restore --logger trx --results-directory TestResults
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- schemas
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- demo
```

Ou executar `pwsh -NoProfile -File ./scripts/verify.ps1`, que verifica o SDK e faz
essas etapas em ordem, interrompendo no primeiro erro e preservando o código de
saída nativo. O script configura as duas variáveis acima. Restore consulta NuGet
para dependências públicas; build/testes/CLI usam os arquivos locais. Se o SDK não
estiver no PATH, adicionar `C:\Program Files\dotnet` somente à sessão atual.
Não é necessário acessar certificados reais ou o Windows Store.

Os três projetos versionam `packages.lock.json`, incluindo dependências transitivas
dos executáveis CLI/testes. `Directory.Build.props` habilita lock e modo bloqueado
por padrão: mudanças de dependências incompatíveis falham em vez de regenerar o
grafo. O script também exige a presença de todos os locks. A auditoria NuGet de
dependências diretas/transitivas e os avisos como erros permanecem habilitados.
O lock do Core não substitui os locks de seus consumidores.

Uma atualização intencional exige revisar a mudança de dependências e executar:

```powershell
dotnet restore EmissorFiscal.slnx --use-lock-file --force-evaluate -p:RestoreLockedMode=false
git diff -- '**/*.csproj' '**/packages.lock.json'
pwsh -NoProfile -File ./scripts/verify.ps1
```

Versionar os locks revisados junto com a mudança. Não usar esse procedimento para
contornar falhas do CI; SDK e versões diretas atuais permanecem preservados.

## CI e evidências

`.github/workflows/ci-offline.yml` reproduz o mesmo script em `windows-2025`/pwsh,
para PRs destinadas à `main`, pushes na `main` e execução manual. Instala o SDK de
`global.json`; ações oficiais têm SHA completo revisado e `contents: read` é a
única permissão concedida. Não há cache restaurado de execuções anteriores nem
credenciais Git persistidas. As dependências são obtidas de nuget.org.

Cada invocação escreve em `TestResults/ci/<id-único>/`: TRX, logs das etapas, JSONs
`cli-schemas`/`cli-demo` e `summary.json` com commit, ambiente, SDK/runtime,
contadores, baseline e hashes. Resultados históricos não são reutilizados.
Sucesso exige ao menos os 70 casos existentes, todos executados/aprovados, as
duas CLIs e todos os artefatos esperados. Falhas preservam resumo e logs disponíveis.

Actions publica apenas essa lista de evidências no artefato
`offline-<run-id>-<tentativa>`, inclusive em falha, com retenção de 30 dias.
Caches, PFX/chaves, downloads e `.validation/` não são publicados. O relatório
de [validação](docs/VALIDATION.md) registra as execuções reais e seus limites.
CI comprova a prova técnica offline; homologação fiscal continua pendente.

`schemas` verifica o manifest, os 11 hashes/dependências e compila os seis entrypoints.
`demo` usa uma NFC-e sintética com CNPJ alfanumérico, gera/importa PFX em memória,
assina, verifica após serialização/envelope/nfeProc e interpreta retornos simulados
100 e 120 com protocolo de 17 posições. Escreve somente o resumo no console. Não
recebe A1 real, senha, CSC, endpoint, comando de transmissão ou parâmetros tributários.

## Estrutura

- `src/EmissorFiscal.Core`: parser, manifest/XSD, chave/CNPJ, assinatura, QR, mensagens e protocolo.
- `src/EmissorFiscal.Cli`: demonstração/verificação exclusivamente offline.
- `tests/EmissorFiscal.Core.Tests`: casos positivos, negativos, adulteração e falhas de correlação.
- `samples/OfflineFixtures.cs`: dados e certificados sintéticos compartilhados somente pela CLI e testes.
- `schemas/nfe/4.00`: manifest e 11 arquivos XSD oficiais preservados byte a byte.
- `docs/TECHNICAL-SCOPE.md`: matriz normativa, limites e contrato mínimo futuro.
- `docs/VALIDATION.md`: evidências, comandos, pendências e relatório deste incremento.
- `docs/ROADMAP.md`: andamento verificado e próximos incrementos com aceite e dependências.

## Atualizações de schemas

Precedência explícita: **PL009n < PL010d v1.03/subárvore NFe < PL010f v1.04**.
O manifest registra o hash de cada ZIP, origem, data do catálogo, entrada no arquivo,
hash por XSD, dependências e versões sobrescritas. Nenhum XSD oficial foi editado.

O runtime não baixa schemas e não aceita dependências externas, schemas indicados
pela instância, arquivos sem hash válido, arquivos ausentes ou baseline nova sem
revisão. O manifest é material local confiável e revisado junto com o código; não
deve ser recebido de sistemas clientes. Novo pacote exige rever manifest e provas.

`scripts/review-schemas.py` é ferramenta de manutenção separada, sem rede. Requer
Python/lxml e os três ZIPs com os hashes fixados em `tmp/official`, disponíveis nas
URLs registradas no manifest. Só deve ser executada durante revisão explícita.
`scripts/verify-materials.py` faz auditoria complementar local de XSD com lxml e
deixa evidência em `.validation/materials.json`. Seus placeholders de assinatura
são apenas estruturais, criados em memória; não são prova de assinatura.

Não há API, banco, Windows Certificate Store, certificado de contribuinte,
transporte fiscal, cancelamento, inutilização, DANFE renderizado, contingência
operacional ou produção. Em eventual código Node, o gerenciador obrigatório é pnpm.

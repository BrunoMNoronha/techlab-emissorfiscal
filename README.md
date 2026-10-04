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
dotnet restore EmissorFiscal.slnx
dotnet build EmissorFiscal.slnx -c Release --no-restore
dotnet test EmissorFiscal.slnx -c Release --no-build --no-restore --logger trx --results-directory TestResults
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- schemas
dotnet run --project src/EmissorFiscal.Cli -c Release --no-build --no-restore -- demo
```

Ou executar `./scripts/verify.ps1`, que verifica o SDK e faz essas etapas em ordem,
interrompendo no primeiro erro. Restore consulta NuGet para dependências públicas;
build/testes/CLI usam os arquivos locais. As dependências diretas estão fixadas por
versões exatas. O grafo transitivo resolvido nesta execução foi registrado em
`.validation/resolved-dependencies.json`; não foi criado lockfile NuGet. As duas
variáveis acima evitam gerar certificado de desenvolvimento ASP.NET e desabilitam
telemetria do SDK nesta sessão. Se a instalação ainda não estiver no PATH da sessão
atual, adicionar `C:\Program Files\dotnet` somente ao PATH dessa sessão antes de
executar. Não é necessário acessar certificados reais ou o Windows Store.

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

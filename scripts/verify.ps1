# Execute com pwsh. Cada invocação tem evidências próprias, sem reutilizar resultados.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
Set-StrictMode -Version Latest
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskEvidence = Join-Path $taskRoot "TestResults/ci/$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $taskEvidence
$taskExitCode = 1
$taskSummary = [ordered]@{
    kind = 'DOTNET_OFFLINE_VALIDATION'
    startedAtUtc = [DateTime]::UtcNow.ToString('o')
    finishedAtUtc = $null
    status = 'failed'
    exitCode = 1
    commit = $null
    runUrl = $null
    runner = [ordered]@{
        name = [Environment]::GetEnvironmentVariable('RUNNER_NAME')
        image = [Environment]::GetEnvironmentVariable('ImageOS')
        imageVersion = [Environment]::GetEnvironmentVariable('ImageVersion')
        os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        powershell = $PSVersionTable.PSVersion.ToString()
    }
    sdk = $null
    runtime = $null
    steps = @()
    testCounters = $null
    baseline = $null
    manifestSha256 = $null
    schemaHashes = @()
    lockHashes = @()
    modelPreparations = @()
    artifacts = @()
    error = $null
    fiscalIo = $false
    taxpayerCertificate = $false
    fiscalHomologationObtained = $false
}

function Invoke-ValidationStep {
    param([string]$Name, [string[]]$Arguments)
    Write-Host "Validação: $Name"
    $taskLog = Join-Path $taskEvidence "$Name.log"
    $taskOutput = @(& dotnet @Arguments 2>&1 | Tee-Object -FilePath $taskLog)
    $taskNativeExit = $LASTEXITCODE
    $taskOutput | ForEach-Object { Write-Host $_ }
    $taskSummary.steps += [ordered]@{
        name = $Name
        arguments = $Arguments
        exitCode = $taskNativeExit
    }
    if ($taskNativeExit -ne 0) {
        $script:taskExitCode = $taskNativeExit
        throw "Etapa $Name falhou com código $taskNativeExit. Consulte $Name.log."
    }
}

Push-Location -LiteralPath $taskRoot
try {
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $taskSummary.commit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível identificar o commit do checkout.' }
    if ([Environment]::GetEnvironmentVariable('GITHUB_RUN_ID')) {
        $taskSummary.runUrl = "$env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID"
    }
    $taskSdk = (Get-Content -LiteralPath 'global.json' -Raw | ConvertFrom-Json).sdk.version
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        $taskExitCode = 2
        throw "Pré-requisito ausente: disponibilizar SDK .NET $taskSdk no PATH."
    }
    Invoke-ValidationStep 'sdk' @('--version')
    $taskSummary.sdk = (Get-Content -LiteralPath (Join-Path $taskEvidence 'sdk.log') -Raw).Trim()
    if ($taskSummary.sdk -ne $taskSdk) { throw "SDK exigido: $taskSdk. Encontrado: $($taskSummary.sdk)." }

    [xml]$taskSolution = Get-Content -LiteralPath 'EmissorFiscal.slnx' -Raw
    foreach ($taskProject in $taskSolution.SelectNodes('//Project')) {
        $taskLock = Join-Path (Split-Path $taskProject.Path -Parent) 'packages.lock.json'
        if (-not (Test-Path -LiteralPath $taskLock -PathType Leaf)) { throw "Lock obrigatório ausente: $taskLock" }
        $taskSummary.lockHashes += [ordered]@{ path = $taskLock; sha256 = (Get-FileHash -LiteralPath $taskLock -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    Invoke-ValidationStep 'restore' @('restore', 'EmissorFiscal.slnx', '--locked-mode')
    Invoke-ValidationStep 'build' @('build', 'EmissorFiscal.slnx', '-c', 'Release', '--no-restore')
    Invoke-ValidationStep 'test' @('test', 'EmissorFiscal.slnx', '-c', 'Release', '--no-build', '--no-restore', '--logger', 'trx;LogFileName=test-results.trx', '--results-directory', $taskEvidence)
    [xml]$taskTrx = Get-Content -LiteralPath (Join-Path $taskEvidence 'test-results.trx') -Raw
    $taskCounters = $taskTrx.TestRun.ResultSummary.Counters
    $taskSummary.testCounters = [ordered]@{
        total = [int]$taskCounters.total
        executed = [int]$taskCounters.executed
        passed = [int]$taskCounters.passed
        failed = [int]$taskCounters.failed
        notExecuted = [int]$taskCounters.notExecuted
    }
    if ($taskSummary.testCounters.total -lt 70 -or
        $taskSummary.testCounters.passed -ne $taskSummary.testCounters.total -or
        $taskSummary.testCounters.executed -ne $taskSummary.testCounters.total -or
        $taskTrx.TestRun.ResultSummary.outcome -ne 'Completed') {
        throw 'O TRX não comprova ao menos 70 casos, todos executados e aprovados.'
    }
    foreach ($taskCommand in @('schemas', 'demo')) {
        Invoke-ValidationStep $taskCommand @('run', '--project', 'src/EmissorFiscal.Cli', '-c', 'Release', '--no-build', '--no-restore', '--', $taskCommand)
        $taskJson = Get-Content -LiteralPath (Join-Path $taskEvidence "$taskCommand.log") -Raw
        $null = $taskJson | ConvertFrom-Json
        [System.IO.File]::WriteAllText((Join-Path $taskEvidence "cli-$taskCommand.json"), $taskJson)
    }
    $taskSchemas = Get-Content -LiteralPath (Join-Path $taskEvidence 'cli-schemas.json') -Raw | ConvertFrom-Json
    $taskDemo = Get-Content -LiteralPath (Join-Path $taskEvidence 'cli-demo.json') -Raw | ConvertFrom-Json
    $taskManifest = Get-Content -LiteralPath 'schemas/nfe/4.00/manifest.json' -Raw | ConvertFrom-Json
    $taskSummary.baseline = $taskManifest.baselineId
    $taskSummary.manifestSha256 = (Get-FileHash -LiteralPath 'schemas/nfe/4.00/manifest.json' -Algorithm SHA256).Hash.ToLowerInvariant()
    $taskSummary.runtime = $taskDemo.stack
    if ($taskSchemas.manifestSha256 -ne $taskSummary.manifestSha256 -or
        $taskSchemas.baseline -ne $taskSummary.baseline -or $taskDemo.baseline -ne $taskSummary.baseline -or
        @($taskSchemas.compiledEntrypoints).Count -ne 6 -or @($taskDemo.compiledEntrypoints).Count -ne 6 -or
        $taskSchemas.fiscalIo -ne $false -or $taskDemo.fiscalIo -ne $false -or
        $taskDemo.taxpayerCertificate -ne $false -or $taskDemo.fiscalHomologationObtained -ne $false -or
        $taskDemo.signatureAfterSerialization -ne $true -or $taskDemo.signatureInEnvelope -ne $true -or
        $taskDemo.signatureInProcessed -ne $true) { throw 'Resumos das CLIs incompatíveis com a prova offline.' }
    foreach ($taskFile in $taskManifest.files) {
        $taskHash = (Get-FileHash -LiteralPath (Join-Path 'schemas/nfe/4.00' $taskFile.path) -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($taskHash -ne $taskFile.sha256) { throw "Hash de schema incompatível: $($taskFile.name)" }
        $taskSummary.schemaHashes += [ordered]@{ path = $taskFile.path; sha256 = $taskHash }
    }
    foreach ($taskCase in @(
        @{ regime = 'crt1'; sale = 'sale-crt1'; emitter = 'emitter-crt1' },
        @{ regime = 'crt3'; sale = 'sale-crt3'; emitter = 'emitter-crt3' },
        @{ regime = 'st-crt1'; sale = 'sale-st-crt1'; emitter = 'emitter-crt1' },
        @{ regime = 'exempt-crt1'; sale = 'sale-exempt-crt1'; emitter = 'emitter-crt1' },
        @{ regime = 'red-crt3'; sale = 'sale-red-crt3'; emitter = 'emitter-crt3' }
    )) {
        $taskRegime = $taskCase.regime
        $taskPreparationDir = Join-Path $taskEvidence "preparation-$taskRegime"
        Invoke-ValidationStep "prepare-$taskRegime" @('run', '--project', 'src/EmissorFiscal.Cli', '-c', 'Release', '--no-build', '--no-restore', '--', 'prepare', "samples/preparation/$($taskCase.sale).json", "samples/preparation/$($taskCase.emitter).json", 'samples/preparation/test-context.json', $taskPreparationDir)
        $taskPreparation = Get-Content -LiteralPath (Join-Path $taskPreparationDir 'summary.json') -Raw | ConvertFrom-Json
        if ($taskPreparation.kind -ne 'NFCE_INPUT_MODEL_PREPARATION' -or
            $taskPreparation.modelVersion -ne 'nfce-preparation/0.1' -or
            $taskPreparation.baseline -ne $taskSummary.baseline -or
            $taskPreparation.manifestSha256 -ne $taskSummary.manifestSha256 -or
            $taskPreparation.environment -ne '2' -or $taskPreparation.model -ne '65' -or
            $taskPreparation.uf -ne '53' -or $taskPreparation.emissionType -ne '1' -or
            $taskPreparation.signatureInSubmissionVerified -ne $true -or
            $taskPreparation.fiscalIo -ne $false -or $taskPreparation.taxpayerCertificate -ne $false -or
            $taskPreparation.fiscalHomologationObtained -ne $false -or
            @($taskPreparation.artifacts).Count -ne 4) { throw "Preparação $taskRegime incompatível com a prova offline." }
        foreach ($taskModelArtifact in $taskPreparation.artifacts) {
            $taskModelPath = Join-Path $taskPreparationDir $taskModelArtifact.name
            $taskModelHash = (Get-FileHash -LiteralPath $taskModelPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($taskModelHash -ne $taskModelArtifact.sha256) { throw "Hash de artefato incompatível: $taskRegime/$($taskModelArtifact.name)" }
            $taskSummary.artifacts += [ordered]@{ name = "preparation-$taskRegime/$($taskModelArtifact.name)"; sha256 = $taskModelHash }
        }
        $taskSummary.modelPreparations += [ordered]@{ regime = $taskRegime; key = $taskPreparation.key; signatureInSubmissionVerified = $taskPreparation.signatureInSubmissionVerified; fiscalIo = $taskPreparation.fiscalIo }
        foreach ($taskModelLog in @("prepare-$taskRegime.log", "preparation-$taskRegime/summary.json")) {
            $taskSummary.artifacts += [ordered]@{ name = $taskModelLog; sha256 = (Get-FileHash -LiteralPath (Join-Path $taskEvidence $taskModelLog) -Algorithm SHA256).Hash.ToLowerInvariant() }
        }
    }
    foreach ($taskArtifact in @('sdk.log', 'restore.log', 'build.log', 'test.log', 'schemas.log', 'demo.log', 'test-results.trx', 'cli-schemas.json', 'cli-demo.json')) {
        $taskPath = Join-Path $taskEvidence $taskArtifact
        if ((Get-Item -LiteralPath $taskPath).Length -eq 0) { throw "Evidência vazia: $taskArtifact" }
        $taskSummary.artifacts += [ordered]@{ name = $taskArtifact; sha256 = (Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    $taskSummary.status = 'passed'
    $taskExitCode = 0
} catch {
    $taskSummary.error = $_.Exception.Message
    [Console]::Error.WriteLine($taskSummary.error)
} finally {
    $taskSummary.finishedAtUtc = [DateTime]::UtcNow.ToString('o')
    $taskSummary.exitCode = $taskExitCode
    $taskSummary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $taskEvidence 'summary.json') -Encoding utf8
    Write-Host "Evidências: $taskEvidence"
    Pop-Location
}
exit $taskExitCode

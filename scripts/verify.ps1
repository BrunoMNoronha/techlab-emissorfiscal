$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $taskRoot
try {
    $taskSdk = (Get-Content -LiteralPath 'global.json' -Raw | ConvertFrom-Json).sdk.version
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        [Console]::Error.WriteLine("Pré-requisito ausente: instalar SDK .NET $taskSdk e disponibilizar dotnet no PATH. Nenhuma validação .NET executada.")
        exit 2
    }
    $taskVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $taskVersion -ne $taskSdk) {
        throw "SDK exigido: $taskSdk. Encontrado: $taskVersion. Sem roll-forward automático."
    }
    $taskSteps = @(
        @{Name='restore';Args=@('restore','EmissorFiscal.slnx')},
        @{Name='build';Args=@('build','EmissorFiscal.slnx','-c','Release','--no-restore')},
        @{Name='test';Args=@('test','EmissorFiscal.slnx','-c','Release','--no-build','--no-restore','--logger','trx','--results-directory','TestResults')},
        @{Name='schemas';Args=@('run','--project','src/EmissorFiscal.Cli','-c','Release','--no-build','--no-restore','--','schemas')},
        @{Name='demo';Args=@('run','--project','src/EmissorFiscal.Cli','-c','Release','--no-build','--no-restore','--','demo')}
    )
    foreach ($taskStep in $taskSteps) {
        Write-Output "Validação: $($taskStep.Name)"
        $taskArgs = $taskStep.Args
        & dotnet @taskArgs
        if ($LASTEXITCODE -ne 0) { throw "Etapa $($taskStep.Name) falhou com código $LASTEXITCODE." }
    }
} finally { Pop-Location }

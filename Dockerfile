# syntax=docker/dockerfile:1
# O SDK precisa ser exatamente o do global.json (rollForward: disable).
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.401
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:10.0.12
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.12

FROM ${DOTNET_SDK_IMAGE} AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false
WORKDIR /source

# Manifests e locks primeiro, para aproveitar o cache de camadas no restore.
COPY global.json NuGet.Config Directory.Build.props EmissorFiscal.slnx ./
COPY src/EmissorFiscal.Core/EmissorFiscal.Core.csproj src/EmissorFiscal.Core/packages.lock.json src/EmissorFiscal.Core/
COPY src/EmissorFiscal.Cli/EmissorFiscal.Cli.csproj src/EmissorFiscal.Cli/packages.lock.json src/EmissorFiscal.Cli/
COPY src/EmissorFiscal.Monitor/EmissorFiscal.Monitor.csproj src/EmissorFiscal.Monitor/packages.lock.json src/EmissorFiscal.Monitor/
COPY tests/EmissorFiscal.Core.Tests/EmissorFiscal.Core.Tests.csproj tests/EmissorFiscal.Core.Tests/packages.lock.json tests/EmissorFiscal.Core.Tests/
RUN dotnet restore EmissorFiscal.slnx --locked-mode

COPY . .
RUN dotnet build EmissorFiscal.slnx -c Release --no-restore
# A imagem só é produzida com a suíte completa aprovada.
RUN dotnet test EmissorFiscal.slnx -c Release --no-build --no-restore
RUN dotnet publish src/EmissorFiscal.Cli -c Release --no-build -o /app
RUN dotnet publish src/EmissorFiscal.Monitor -c Release --no-build --no-restore -o /monitor

# Estágio para rodar scripts/verify.ps1 com o repositório montado em /workspace
# (precisa de .git para o commit e grava evidências em TestResults/ no host).
FROM ${DOTNET_SDK_IMAGE} AS verify
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false
RUN apt-get update \
    && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/* \
    && git config --system --add safe.directory /workspace
WORKDIR /workspace
ENTRYPOINT ["pwsh", "-NoProfile", "-File", "./scripts/verify.ps1"]

# Painel local. O Compose publica a porta exclusivamente em 127.0.0.1 no host.
FROM ${DOTNET_ASPNET_IMAGE} AS monitor
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    MONITOR_IN_CONTAINER=1
WORKDIR /app
COPY --from=build /monitor .
RUN mkdir -p /app/.validation/monitor && chown -R app:app /app/.validation
USER app
EXPOSE 5088
ENTRYPOINT ["dotnet", "EmissorFiscal.Monitor.dll"]

# Último estágio: é o alvo padrão de `docker build` sem --target.
FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app
COPY --from=build /app .
# Exemplos de entrada do `prepare` (JSONs sintéticos públicos), referenciados
# por caminho relativo como na execução a partir da raiz do repositório.
COPY --from=build /source/samples/preparation ./samples/preparation
USER app
ENTRYPOINT ["dotnet", "EmissorFiscal.Cli.dll"]
CMD ["demo"]

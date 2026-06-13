# syntax=docker/dockerfile:1
# AgentPort / DefterPort — platform-api production image.
# Multi-stage: .NET 10 SDK builds a trimmed publish output, aspnet:10 runs it as non-root.
# Build context MUST be the repo root (the .sln references sibling projects), e.g.:
#   docker build -f src/platform-api/Dockerfile -t agentport/platform-api .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the csproj first so layer caching survives source-only edits.
COPY src/platform-api/AgentPort.PlatformApi.csproj src/platform-api/
RUN dotnet restore src/platform-api/AgentPort.PlatformApi.csproj

# Bring in the rest of the project and publish a self-contained-free framework build.
COPY src/platform-api/ src/platform-api/
RUN dotnet publish src/platform-api/AgentPort.PlatformApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is added solely for the container HEALTHCHECK (the aspnet image ships no
# HTTP client and has no bash for /dev/tcp tricks). ~1 MB, no app dependency.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Non-root: the aspnet image ships an "app" user (uid 1654). Run as it.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:5001 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

EXPOSE 5001
USER app

HEALTHCHECK --interval=15s --timeout=10s --retries=10 --start-period=60s \
    CMD curl -fsS http://127.0.0.1:5001/health/live || exit 1

# Program.cs runs EF Core MigrateAsync on startup and serves /health/live + /health/ready.
ENTRYPOINT ["dotnet", "AgentPort.PlatformApi.dll"]

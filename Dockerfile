# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/PipeRAG.Core/PipeRAG.Core.csproj src/PipeRAG.Core/
COPY src/PipeRAG.Infrastructure/PipeRAG.Infrastructure.csproj src/PipeRAG.Infrastructure/
COPY src/PipeRAG.Api/PipeRAG.Api.csproj src/PipeRAG.Api/
RUN dotnet restore src/PipeRAG.Api/PipeRAG.Api.csproj

# Copy everything and publish
COPY src/ src/
RUN dotnet publish src/PipeRAG.Api/PipeRAG.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Security: run as non-root
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PipeRAG.Api.dll"]
